/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Math;

/// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange</c>.</remarks>
public class DoubleRange : IComparable<DoubleRange>
{
    private readonly double start;
    private readonly double end;
    private readonly bool includeStart;
    private readonly bool includeEnd;

    public DoubleRange(double start, double end, bool includeStart, bool includeEnd)
    {
        this.start = start;
        this.end = end;
        this.includeStart = includeStart;
        this.includeEnd = includeEnd;
    }

    public double Start => start;

    public double End => end;

    public bool IncludeStart => includeStart;

    public bool IncludeEnd => includeEnd;

    public override bool Equals(object? other)
    {
        if (other is not DoubleRange o) return false;
        return start == o.start && includeStart == o.includeStart &&
            end == o.end && includeEnd == o.includeEnd;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(start, end, includeStart, includeEnd);
    }

    public int CompareTo(DoubleRange? other)
    {
        if (other is null) return 1;
        int comp = start.CompareTo(other.start);
        if (comp != 0) return comp;
        if (includeStart && !other.includeStart) return -1;
        if (!includeStart && other.includeStart) return 1;
        comp = end.CompareTo(other.end);
        if (comp != 0) return comp;
        if (!includeEnd && other.includeEnd) return -1;
        if (includeEnd && !other.includeEnd) return 1;
        return 0;
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1:F6},{2:F6}{3}",
            includeStart ? '[' : '(', start,
            end, includeEnd ? ']' : ')');
    }

    public static DoubleRange? Intersection(DoubleRange a, DoubleRange b)
    {
        if (b.start > a.end) return null;
        if (b.start == a.end && !b.includeStart && !a.includeEnd) return null;
        if (a.start > b.end) return null;
        if (a.start == b.end && !a.includeStart && !b.includeEnd) return null;
        double start;
        double end;
        bool includeStart;
        bool includeEnd;
        if (a.start > b.start)
        {
            start = a.start;
            includeStart = a.includeStart;
        }
        else if (b.start > a.start)
        {
            start = b.start;
            includeStart = b.includeStart;
        }
        else
        {
            start = a.start;
            includeStart = a.includeStart && b.includeStart;
        }
        if (a.end < b.end)
        {
            end = a.end;
            includeEnd = a.includeEnd;
        }
        else if (b.end < a.end)
        {
            end = b.end;
            includeEnd = b.includeEnd;
        }
        else
        {
            end = a.end;
            includeEnd = a.includeEnd && b.includeEnd;
        }
        return new DoubleRange(start, end, includeStart, includeEnd);
    }
}
