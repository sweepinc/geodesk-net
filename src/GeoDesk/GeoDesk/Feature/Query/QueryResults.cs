/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

public class QueryResults
{
    internal readonly NioBuffer? buf;
    internal int[] pointers;
    internal int size;
    internal QueryResults? next;
    private QueryResults last;   // only valid for first QueryResults in a chain

    private const int DEFAULT_BUCKET_SIZE = 256;
    public static readonly QueryResults EMPTY = new QueryResults(null, 0);

    public QueryResults(NioBuffer? buf, int maxSize)
    {
        this.buf = buf;
        pointers = new int[maxSize];
        last = this;
    }

    public QueryResults(NioBuffer? buf)
        : this(buf, DEFAULT_BUCKET_SIZE)
    {
    }

    private QueryResults(NioBuffer? buf, int[] pointers, int size)
    {
        this.buf = buf;
        this.pointers = pointers;
        this.size = size;
        last = this;
    }

    public void Add(int ptr)
    {
        if (size == pointers.Length)
        {
            last = last.next = new QueryResults(buf, pointers, size);
            pointers = new int[pointers.Length];
            size = 0;
        }
        pointers[size++] = ptr;
    }

    public static QueryResults Merge(QueryResults a, QueryResults b)
    {
        if (a.size == 0) return b;
        if (b.size == 0) return a;
        a.last.next = b;
        a.last = b.last;
        return a;
    }
}
