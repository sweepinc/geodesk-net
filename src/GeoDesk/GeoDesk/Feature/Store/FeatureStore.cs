/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Clarisma.Common.Pbf;
using Clarisma.Common.Store;
using GeoDesk.Feature.Match;
using Java.Util.Concurrent;
using NetTopologySuite.Geometries;
using NioBuffer = Java.Nio.ByteBuffer;
using ZoomLevelsUtil = GeoDesk.Feature.Store.ZoomLevels;

namespace GeoDesk.Feature.Store;

public class FeatureStore : FreeStore
{
    private int minZoom;
    private int zoomSteps;
    private NioBuffer? tileIndexBuf;
    private int tileIndexOfs;

    public FeatureStore(string path)
        : base(path)
    {
    }

    private Dictionary<string, int> stringsToCodes = new Dictionary<string, int>();
    private string[] codesToStrings = Array.Empty<string>();
    private Dictionary<int, int> keysToCategories = new Dictionary<int, int>();
    private MatcherCompiler? matchers;
    private ExecutorService? executor;
    private GeometryFactory? geometryFactory;
    private int maxPendingTiles;
    private readonly object matchersLock = new object();

    public new const int MAGIC = 0x1CE50D6E; // "geodesic"
    public new const int VERSION = 1_000_000;

    public const int SNAPSHOT_TILE_INDEX_OFS = 24;
    public const int SNAPSHOT_TILE_COUNT_OFS = 28;
    private const int STRING_TABLE_PTR_OFS = 84;
    private const int INDEX_SCHEMA_PTR_OFS = 88;
    private const int PROPERTIES_PTR_OFS = 92;
    public const int ZOOM_LEVELS_OFS = 96;

    protected override void Initialize()
    {
        base.Initialize();

        ReadStringTable();
        ReadIndexSchema();

        int pSnapshot = 128 + ActiveSnapshot() * 64;
        int tileIndexPage = baseMapping!.GetInt(pSnapshot + SNAPSHOT_TILE_INDEX_OFS);
        tileIndexBuf = BufferOfPage(tileIndexPage);
        tileIndexOfs = OffsetOfPage(tileIndexPage);

        EnableQueries();
        int zoomLevels = ZoomLevels();
        minZoom = ZoomLevelsUtil.MinZoom(zoomLevels);
        zoomSteps = ZoomLevelsUtil.ZoomSteps(zoomLevels);
    }

    public NioBuffer TileIndexBuf()
    {
        return tileIndexBuf!;
    }

    public int TileIndexOfs()
    {
        return tileIndexOfs;
    }

    public int ZoomLevels()
    {
        return baseMapping!.GetInt(ZOOM_LEVELS_OFS);
    }

    private void ReadStringTable()
    {
        int p = baseMapping!.GetInt(STRING_TABLE_PTR_OFS);
        int count = baseMapping.GetInt(p) & 0xffff;
        PbfDecoder reader = new PbfDecoder(baseMapping, p + 2);
        codesToStrings = new string[count];
        var stringMap = new Dictionary<string, int>(count + (count >> 1));

        for (int i = 0; i < count; i++)
        {
            string s = reader.ReadString();
            codesToStrings[i] = s;
            stringMap[s] = i;
        }
        stringsToCodes = stringMap;
    }

    private void ReadIndexSchema()
    {
        int p = baseMapping!.GetInt(INDEX_SCHEMA_PTR_OFS);
        int count = baseMapping.GetInt(p);
        var map = new Dictionary<int, int>(count);
        for (int i = 0; i < count; i++)
        {
            p += 4;
            int entry = baseMapping.GetInt(p);
            map[(char)entry] = entry >> 16;
        }
        keysToCategories = map;
    }

    private void EnableQueries()
    {
        matchers = new MatcherCompiler(stringsToCodes, codesToStrings, keysToCategories);
        executor = new ForkJoinPool(); // TODO: ability to set parallelism
        maxPendingTiles = Environment.ProcessorCount * 2;
        geometryFactory = new GeometryFactory();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.executor()</c>.</remarks>
    public ExecutorService Executor()
    {
        return executor!;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStore.maxPendingTiles()</c>.</remarks>
    public int MaxPendingTiles()
    {
        return maxPendingTiles;
    }

    public int TileIndexEntry(int tip)
    {
        return tileIndexBuf!.GetInt(tileIndexOfs + tip * 4);
    }

    public static int PageFromEntry(int entry)
    {
        return (int)((uint)entry >> 2);
    }

    public static bool IsTileLoadedAndCurrent(int entry)
    {
        return (entry & 2) != 0;
    }

    public int TilePage(int tip)
    {
        return (int)((uint)tileIndexBuf!.GetInt(tileIndexOfs + tip * 4) >> 2);
    }

    public bool IsTileReady(int tip)
    {
        return (tileIndexBuf!.GetInt(tileIndexOfs + tip * 4) & 2) != 0;
    }

    public string StringFromCode(int code)
    {
        try
        {
            return codesToStrings[code];
        }
        catch (IndexOutOfRangeException)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                "Undefined global string code {0}", code));
        }
    }

    /// <summary>
    /// Returns the global string code for a given string, or -1 if not in the GST.
    /// </summary>
    public int CodeFromString(string s)
    {
        return stringsToCodes.TryGetValue(s, out int v) ? v : -1;
    }

    public GeometryFactory GeometryFactory()
    {
        return geometryFactory!;
    }

    public Matcher GetMatcher(string query)
    {
        lock (matchersLock)
        {
            return matchers!.GetMatcher(query);
        }
    }

    public StoredFeature GetFeature(NioBuffer buf, int p)
    {
        int flags = buf.GetInt(p);
        int type = (flags >> 3) & 3;
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
        if (executor != null)
        {
            // Wait for pending tasks to complete before allowing
            // Store.close() to unmap the buffers (otherwise risk of crash)

            executor.Shutdown();
            try
            {
                executor.AwaitTermination(24, TimeUnit.Hours);
            }
            catch (InterruptedException)
            {
                // do nothing
            }
        }
        base.Close();
    }

    public IReadOnlyDictionary<int, int> KeysToCategories()
    {
        return keysToCategories;
    }

    public IReadOnlyDictionary<string, int> StringsToCodes()
    {
        return stringsToCodes;
    }

    public string[] CodesToStrings()
    {
        return codesToStrings;
    }
}
