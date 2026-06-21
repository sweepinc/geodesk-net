/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

/// <summary>
/// Accumulates the feature pointers produced by scanning a tile, as a linked chain
/// of fixed-size pointer buckets. Results from multiple buckets or tiles can be
/// merged cheaply by linking their chains.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults</c>.</remarks>
internal class QueryResults
{

    const int DefaultBucketSize = 256;

    public static readonly QueryResults Empty = new QueryResults(default, 0);

    /// <summary>
    /// Concatenates two result chains, returning the combined chain. An empty input
    /// is returned as the other chain unchanged.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults.merge(QueryResults, QueryResults)</c>.</remarks>
    public static QueryResults Merge(QueryResults a, QueryResults b)
    {
        if (a.size == 0) return b;
        if (b.size == 0) return a;
        a._last.next = b;
        a._last = b._last;
        return a;
    }

    internal readonly NioBuffer buf;
    internal int[] pointers;
    internal int size;
    internal QueryResults? next;
    QueryResults _last;   // only valid for first QueryResults in a chain

    /// <summary>
    /// Creates an empty result bucket backed by the given buffer with room for
    /// <paramref name="maxSize"/> pointers.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults(ByteBuffer, int)</c>.</remarks>
    public QueryResults(NioBuffer buf, int maxSize)
    {
        this.buf = buf;
        pointers = new int[maxSize];
        _last = this;
    }

    /// <summary>
    /// Creates an empty result bucket backed by the given buffer with the default
    /// bucket capacity.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults(ByteBuffer)</c>.</remarks>
    public QueryResults(NioBuffer buf)
        : this(buf, DefaultBucketSize)
    {
    }

    /// <summary>
    /// Creates a result bucket that adopts an already-filled pointer array; used
    /// when a full bucket is rolled over into the chain and a fresh array allocated.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults(ByteBuffer, int[], int)</c>.</remarks>
    QueryResults(NioBuffer buf, int[] pointers, int size)
    {
        this.buf = buf;
        this.pointers = pointers;
        this.size = size;
        _last = this;
    }

    /// <summary>
    /// Appends a feature pointer to the result set, rolling the current bucket into
    /// the chain and starting a fresh bucket when the current one is full.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults.add(int)</c>.</remarks>
    public void Add(int ptr)
    {
        if (size == pointers.Length)
        {
            _last = _last.next = new QueryResults(buf, pointers, size);
            pointers = new int[pointers.Length];
            size = 0;
        }
        pointers[size++] = ptr;
    }

}
