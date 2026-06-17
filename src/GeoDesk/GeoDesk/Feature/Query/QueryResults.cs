/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults</c>.</remarks>
public class QueryResults
{

    const int DefaultBucketSize = 256;

    public static readonly QueryResults Empty = new QueryResults(null, 0);

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults.merge(QueryResults, QueryResults)</c>.</remarks>
    public static QueryResults Merge(QueryResults a, QueryResults b)
    {
        if (a.size == 0) return b;
        if (b.size == 0) return a;
        a._last.next = b;
        a._last = b._last;
        return a;
    }

    internal readonly NioBuffer? buf;
    internal int[] pointers;
    internal int size;
    internal QueryResults? next;
    QueryResults _last;   // only valid for first QueryResults in a chain

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults(ByteBuffer, int)</c>.</remarks>
    public QueryResults(NioBuffer? buf, int maxSize)
    {
        this.buf = buf;
        pointers = new int[maxSize];
        _last = this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults(ByteBuffer)</c>.</remarks>
    public QueryResults(NioBuffer? buf)
        : this(buf, DefaultBucketSize)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.QueryResults(ByteBuffer, int[], int)</c>.</remarks>
    QueryResults(NioBuffer? buf, int[] pointers, int size)
    {
        this.buf = buf;
        this.pointers = pointers;
        this.size = size;
        _last = this;
    }

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
