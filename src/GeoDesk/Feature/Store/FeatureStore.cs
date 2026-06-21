/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using GeoDesk.Buffers;
using GeoDesk.Common.Pbf;
using GeoDesk.Common.Store;
using GeoDesk.Feature.Match;

using NetTopologySuite.Geometries;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;
using ZoomLevelsUtil = GeoDesk.Feature.Store.ZoomLevels;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A GeoDesk feature library opened from a <c>.gol</c> file. Provides access to the
/// tile index, global string table, index schema, zoom-level configuration, and the
/// matcher/geometry infrastructure used to run spatial queries against the store.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore</c>.</remarks>
internal class FeatureStore : FreeStore
{

    public new const int MAGIC = 0x1CE50D6E; // "geodesic"
    public new const int VERSION = 1_000_000;

    public const int SNAPSHOT_TILE_INDEX_OFS = 24;
    public const int SNAPSHOT_TILE_COUNT_OFS = 28;
    const int STRING_TABLE_PTR_OFS = 84;
    const int INDEX_SCHEMA_PTR_OFS = 88;
    const int PROPERTIES_PTR_OFS = 92;
    public const int ZOOM_LEVELS_OFS = 96;

    int _minZoom;
    int _zoomSteps;
    NioBuffer _tileIndexBuf;
    int _tileIndexOfs;
    Dictionary<string, int> _stringsToCodes = new Dictionary<string, int>();
    string[] _codesToStrings = Array.Empty<string>();
    Dictionary<int, int> _keysToCategories = new Dictionary<int, int>();
    MatcherCompiler? _matchers;
    GeometryFactory? _geometryFactory;
    int _maxPendingTiles;
    readonly object _matchersLock = new object();

    // PORT: replaces the shared ForkJoinPool. Query producers run on the ThreadPool; the store
    // tracks them so Close() can cancel and wait for in-flight tile scans before unmapping buffers.
    readonly CancellationTokenSource _queryCancellation = new CancellationTokenSource();
    readonly object _producersLock = new object();
    readonly HashSet<Task> _producers = new HashSet<Task>();

    /// <summary>
    /// Creates a feature store backed by the file at the given path.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore(Path)</c>.</remarks>
    public FeatureStore(string path) :
        base(path)
    {

    }

    /// <summary>
    /// Initializes the store after opening: reads the string table and index schema,
    /// locates the active snapshot's tile index, enables queries, and caches the
    /// zoom-level configuration.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.initialize()</c>.</remarks>
    protected override void Initialize()
    {
        base.Initialize();

        ReadStringTable();
        ReadIndexSchema();

        var pSnapshot = 128 + ActiveSnapshot() * 64;
        var tileIndexPage = BaseMapping.Memory.Span.GetIntLE(pSnapshot + SNAPSHOT_TILE_INDEX_OFS);
        _tileIndexBuf = new NioBuffer(SegmentOfPage(tileIndexPage).Memory);
        _tileIndexOfs = OffsetOfPage(tileIndexPage);

        EnableQueries();
        var zoomLevels = ZoomLevels;
        _minZoom = ZoomLevelsUtil.MinZoom(zoomLevels);
        _zoomSteps = ZoomLevelsUtil.ZoomSteps(zoomLevels);
    }

    /// <summary>
    /// The buffer containing the active snapshot's tile index.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tileIndexBuf()</c>.</remarks>
    public NioBuffer TileIndexBuf => _tileIndexBuf;

    /// <summary>
    /// The byte offset of the tile index within its buffer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tileIndexOfs()</c>.</remarks>
    public int TileIndexOfs => _tileIndexOfs;

    /// <summary>
    /// The packed zoom-level configuration read from the store header.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.zoomLevels()</c>.</remarks>
    public int ZoomLevels => BaseMapping.Memory.Span.GetIntLE(ZOOM_LEVELS_OFS);

    /// <summary>
    /// Reads the global string table from the store, building the code-to-string array
    /// and string-to-code map.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.readStringTable()</c>.</remarks>
    void ReadStringTable()
    {
        var p = BaseMapping.Memory.Span.GetIntLE(STRING_TABLE_PTR_OFS);
        var count = BaseMapping.Memory.Span.GetIntLE(p) & 0xffff;
        var reader = new PbfDecoder(new NioBuffer(BaseMapping.Memory), p + 2);
        _codesToStrings = new string[count];
        var stringMap = new Dictionary<string, int>(count + (count >> 1));

        for (var i = 0; i < count; i++)
        {
            var s = reader.ReadString();
            _codesToStrings[i] = s;
            stringMap[s] = i;
        }

        _stringsToCodes = stringMap;
    }

    /// <summary>
    /// Reads the index schema from the store, building the map from key codes to index
    /// category bits used to select spatial indexes during queries.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.readIndexSchema()</c>.</remarks>
    void ReadIndexSchema()
    {
        var p = BaseMapping.Memory.Span.GetIntLE(INDEX_SCHEMA_PTR_OFS);
        var count = BaseMapping.Memory.Span.GetIntLE(p);
        var map = new Dictionary<int, int>(count);
        for (var i = 0; i < count; i++)
        {
            p += 4;
            var entry = BaseMapping.Memory.Span.GetIntLE(p);
            map[(char)entry] = entry >> 16;
        }

        _keysToCategories = map;
    }

    /// <summary>
    /// Sets up the query infrastructure: the matcher compiler, the maximum number of
    /// concurrently pending tiles, and the geometry factory.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.enableQueries()</c>.</remarks>
    void EnableQueries()
    {
        _matchers = new MatcherCompiler(_stringsToCodes, _codesToStrings, _keysToCategories);
        _maxPendingTiles = Environment.ProcessorCount * 2;
        _geometryFactory = new GeometryFactory();
    }

    /// <summary>
    /// The maximum number of tiles a query may have pending (being loaded or scanned)
    /// at once.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.maxPendingTiles()</c>.</remarks>
    public int MaxPendingTiles => _maxPendingTiles;

    // PORT: token a Query links its producer to, so Close() can cancel all in-flight scans.
    /// <summary>
    /// A cancellation token that in-flight query producers observe so that
    /// <see cref="Close"/> can stop all running tile scans before unmapping buffers.
    /// </summary>
    internal CancellationToken QueryCancellation => _queryCancellation.Token;

    // PORT: registers a Query's background producer so Close() can wait for it. The task removes
    // itself from the set when it completes.
    /// <summary>
    /// Registers a query's background producer task so <see cref="Close"/> can wait for
    /// it; the task removes itself from the tracking set on completion.
    /// </summary>
    internal void TrackProducer(Task producer)
    {
        lock (_producersLock)
        {
            _producers.Add(producer);
        }
        producer.ContinueWith(static (t, state) =>
        {
            // state is the `this` passed below.
            if (state is FeatureStore self)
            {
                lock (self._producersLock)
                {
                    self._producers.Remove(t);
                }
            }
        }, this, TaskScheduler.Default);
    }

    /// <summary>
    /// Returns the raw tile-index entry for the given tile identifier (TIP), encoding
    /// the tile's page and load state.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tileIndexEntry(int)</c>.</remarks>
    public int TileIndexEntry(int tip)
    {
        return _tileIndexBuf.GetInt(_tileIndexOfs + tip * 4);
    }

    /// <summary>
    /// Extracts the tile's first page from a tile-index entry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.pageFromEntry(int)</c>.</remarks>
    public static int PageFromEntry(int entry)
    {
        return (int)((uint)entry >> 2);
    }

    /// <summary>
    /// Returns true if the tile-index entry indicates the tile is loaded and current.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.isTileLoadedAndCurrent(int)</c>.</remarks>
    public static bool IsTileLoadedAndCurrent(int entry)
    {
        return (entry & 2) != 0;
    }

    /// <summary>
    /// Returns the first page of the tile with the given identifier (TIP).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tilePage(int)</c>.</remarks>
    public int TilePage(int tip)
    {
        return (int)((uint)_tileIndexBuf.GetInt(_tileIndexOfs + tip * 4) >> 2);
    }

    /// <summary>
    /// Returns true if the tile with the given identifier (TIP) is loaded and ready
    /// for querying.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.isTileReady(int)</c>.</remarks>
    public bool IsTileReady(int tip)
    {
        return (_tileIndexBuf.GetInt(_tileIndexOfs + tip * 4) & 2) != 0;
    }

    /// <summary>
    /// Returns the global string for the given code, throwing if the code is out of
    /// range.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.stringFromCode(int)</c>.</remarks>
    public string StringFromCode(int code)
    {
        try
        {
            return _codesToStrings[code];
        }
        catch (IndexOutOfRangeException)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                "Undefined global string code {0}", code));
        }
    }

    /// <summary>Returns the global string code for a given string, or -1 if not in the GST.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.codeFromString(String)</c>.</remarks>
    public int CodeFromString(string s)
    {
        return _stringsToCodes.TryGetValue(s, out var v) ? v : -1;
    }

    /// <summary>
    /// Returns the geometry factory used to build geometry for this store's features,
    /// throwing if queries are not enabled.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.geometryFactory()</c>.</remarks>
    public GeometryFactory GeometryFactory()
    {
        return _geometryFactory ?? throw new InvalidOperationException("Queries are not enabled");
    }

    /// <summary>
    /// Compiles (or returns a cached) matcher for the given GOQL query string,
    /// throwing if queries are not enabled.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.getMatcher(String)</c>.</remarks>
    public Matcher GetMatcher(string query)
    {
        lock (_matchersLock)
        {
            return (_matchers ?? throw new InvalidOperationException("Queries are not enabled")).GetMatcher(query);
        }
    }

    /// <summary>
    /// Constructs the appropriate stored feature (node, way, or relation) for the
    /// record at the given buffer position, dispatching on its type bits.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.getFeature(ByteBuffer, int)</c>.</remarks>
    public StoredFeature GetFeature(NioBuffer buf, int p)
    {
        var flags = buf.GetInt(p);
        var type = (flags >> 3) & 3;
        if (type == 1)
        {
            return new StoredWay(this, buf, p);
        }
        if (type == 0)
        {
            return new StoredNode(this, buf, p);
        }
        System.Diagnostics.Debug.Assert(type == 2);
        return new StoredRelation(this, buf, p);
    }

    // TODO: create an awaitOperations() method
    /// <summary>
    /// Closes the store, first cancelling and awaiting any in-flight query producers
    /// and their tile scans so that buffers are not unmapped while still in use.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.close()</c>.</remarks>
    public new void Close()
    {
        // Cancel and wait for any in-flight query producers (and their tile scans) before allowing
        // Store.close() to unmap the buffers (otherwise risk of crash).
        _queryCancellation.Cancel();
        Task[] pending;
        lock (_producersLock)
        {
            pending = new Task[_producers.Count];
            _producers.CopyTo(pending);
        }
        try
        {
            Task.WaitAll(pending);
        }
        catch
        {
            // producers complete via cancellation; any faults were already surfaced to consumers
        }
        base.Close();
    }

    /// <summary>
    /// Returns the map from key codes to index category bits.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.keysToCategories()</c>.</remarks>
    public IReadOnlyDictionary<int, int> KeysToCategories()
    {
        return _keysToCategories;
    }

    /// <summary>
    /// Returns the map from global strings to their codes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.stringsToCodes()</c>.</remarks>
    public IReadOnlyDictionary<string, int> StringsToCodes()
    {
        return _stringsToCodes;
    }

    /// <summary>
    /// Returns the array mapping global string codes to their strings.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.codesToStrings()</c>.</remarks>
    public string[] CodesToStrings()
    {
        return _codesToStrings;
    }

}
