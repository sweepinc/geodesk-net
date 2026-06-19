/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;

using GeoDesk.Common.Pbf;
using GeoDesk.Common.Store;
using GeoDesk.Feature.Match;

using Java.Util.Concurrent;

using NetTopologySuite.Geometries;

using NioBuffer = Java.Nio.ByteBuffer;
using ZoomLevelsUtil = GeoDesk.Feature.Store.ZoomLevels;

namespace GeoDesk.Feature.Store;

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
    NioBuffer? _tileIndexBuf;
    int _tileIndexOfs;
    Dictionary<string, int> _stringsToCodes = new Dictionary<string, int>();
    string[] _codesToStrings = Array.Empty<string>();
    Dictionary<int, int> _keysToCategories = new Dictionary<int, int>();
    MatcherCompiler? _matchers;
    ExecutorService? _executor;
    GeometryFactory? _geometryFactory;
    int _maxPendingTiles;
    readonly object _matchersLock = new object();

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore(Path)</c>.</remarks>
    public FeatureStore(string path)
        : base(path)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.initialize()</c>.</remarks>
    protected override void Initialize()
    {
        base.Initialize();

        ReadStringTable();
        ReadIndexSchema();

        var pSnapshot = 128 + ActiveSnapshot() * 64;
        var tileIndexPage = baseMapping!.GetInt(pSnapshot + SNAPSHOT_TILE_INDEX_OFS);
        _tileIndexBuf = BufferOfPage(tileIndexPage);
        _tileIndexOfs = OffsetOfPage(tileIndexPage);

        EnableQueries();
        var zoomLevels = ZoomLevels;
        _minZoom = ZoomLevelsUtil.MinZoom(zoomLevels);
        _zoomSteps = ZoomLevelsUtil.ZoomSteps(zoomLevels);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tileIndexBuf()</c>.</remarks>
    public NioBuffer TileIndexBuf => _tileIndexBuf!;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tileIndexOfs()</c>.</remarks>
    public int TileIndexOfs => _tileIndexOfs;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.zoomLevels()</c>.</remarks>
    public int ZoomLevels => baseMapping!.GetInt(ZOOM_LEVELS_OFS);

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.readStringTable()</c>.</remarks>
    void ReadStringTable()
    {
        var p = baseMapping!.GetInt(STRING_TABLE_PTR_OFS);
        var count = baseMapping.GetInt(p) & 0xffff;
        var reader = new PbfDecoder(baseMapping, p + 2);
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

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.readIndexSchema()</c>.</remarks>
    void ReadIndexSchema()
    {
        var p = baseMapping!.GetInt(INDEX_SCHEMA_PTR_OFS);
        var count = baseMapping.GetInt(p);
        var map = new Dictionary<int, int>(count);
        for (var i = 0; i < count; i++)
        {
            p += 4;
            var entry = baseMapping.GetInt(p);
            map[(char)entry] = entry >> 16;
        }

        _keysToCategories = map;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.enableQueries()</c>.</remarks>
    void EnableQueries()
    {
        _matchers = new MatcherCompiler(_stringsToCodes, _codesToStrings, _keysToCategories);
        _executor = new ForkJoinPool(); // TODO: ability to set parallelism
        _maxPendingTiles = Environment.ProcessorCount * 2;
        _geometryFactory = new GeometryFactory();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.executor()</c>.</remarks>
    public ExecutorService Executor => _executor!;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.maxPendingTiles()</c>.</remarks>
    public int MaxPendingTiles => _maxPendingTiles;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tileIndexEntry(int)</c>.</remarks>
    public int TileIndexEntry(int tip)
    {
        return _tileIndexBuf!.GetInt(_tileIndexOfs + tip * 4);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.pageFromEntry(int)</c>.</remarks>
    public static int PageFromEntry(int entry)
    {
        return (int)((uint)entry >> 2);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.isTileLoadedAndCurrent(int)</c>.</remarks>
    public static bool IsTileLoadedAndCurrent(int entry)
    {
        return (entry & 2) != 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.tilePage(int)</c>.</remarks>
    public int TilePage(int tip)
    {
        return (int)((uint)_tileIndexBuf!.GetInt(_tileIndexOfs + tip * 4) >> 2);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.isTileReady(int)</c>.</remarks>
    public bool IsTileReady(int tip)
    {
        return (_tileIndexBuf!.GetInt(_tileIndexOfs + tip * 4) & 2) != 0;
    }

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

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.geometryFactory()</c>.</remarks>
    public GeometryFactory GeometryFactory()
    {
        return _geometryFactory!;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.getMatcher(String)</c>.</remarks>
    public Matcher GetMatcher(string query)
    {
        lock (_matchersLock)
        {
            return _matchers!.GetMatcher(query);
        }
    }

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
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.close()</c>.</remarks>
    public new void Close()
    {
        if (_executor != null)
        {
            // Wait for pending tasks to complete before allowing
            // Store.close() to unmap the buffers (otherwise risk of crash)

            _executor.Shutdown();
            try
            {
                _executor.AwaitTermination(24, TimeUnit.Hours);
            }
            catch (InterruptedException)
            {
                // do nothing
            }
        }
        base.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.keysToCategories()</c>.</remarks>
    public IReadOnlyDictionary<int, int> KeysToCategories()
    {
        return _keysToCategories;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.stringsToCodes()</c>.</remarks>
    public IReadOnlyDictionary<string, int> StringsToCodes()
    {
        return _stringsToCodes;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.codesToStrings()</c>.</remarks>
    public string[] CodesToStrings()
    {
        return _codesToStrings;
    }

}
