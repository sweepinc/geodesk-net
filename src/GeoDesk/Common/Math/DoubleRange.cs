/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Math;

/// <summary>
/// An immutable interval of <see cref="double"/> values defined by a start and end bound, each of
/// which may be inclusive or exclusive. Ranges are comparable and support intersection.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange</c>.</remarks>
internal class DoubleRange : IComparable<DoubleRange>
{

    readonly double _start;
    readonly double _end;
    readonly bool _includeStart;
    readonly bool _includeEnd;

    /// <summary>
    /// Creates a range from <paramref name="start"/> to <paramref name="end"/>, with each bound
    /// inclusive or exclusive as specified by <paramref name="includeStart"/> and
    /// <paramref name="includeEnd"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange(double, double, boolean, boolean)</c>.</remarks>
    public DoubleRange(double start, double end, bool includeStart, bool includeEnd)
    {
        _start = start;
        _end = end;
        _includeStart = includeStart;
        _includeEnd = includeEnd;
    }

    /// <summary>
    /// The lower bound of the range.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.start()</c>.</remarks>
    public double Start => _start;

    /// <summary>
    /// The upper bound of the range.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.end()</c>.</remarks>
    public double End => _end;

    /// <summary>
    /// True if the lower bound is inclusive.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.includeStart()</c>.</remarks>
    public bool IncludeStart => _includeStart;

    /// <summary>
    /// True if the upper bound is inclusive.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.includeEnd()</c>.</remarks>
    public bool IncludeEnd => _includeEnd;

    /// <summary>
    /// Value equality: two ranges are equal when their bounds and inclusiveness flags all match.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.equals(Object)</c>.</remarks>
    public override bool Equals(object? other)
    {
        if (other is not DoubleRange o) return false;
        return _start == o._start && _includeStart == o._includeStart &&
            _end == o._end && _includeEnd == o._includeEnd;
    }

    /// <summary>
    /// Returns a hash code derived from both bounds and their inclusiveness flags, consistent with
    /// <see cref="Equals(object?)"/>.
    /// </summary>
    /// <remarks>Port-only override paired with <c>Equals</c> (Java's DoubleRange does not override hashCode).</remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(_start, _end, _includeStart, _includeEnd);
    }

    /// <summary>
    /// Orders ranges by start bound, then by start inclusiveness, then by end bound, then by end
    /// inclusiveness. A null other sorts first.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.compareTo(DoubleRange)</c>.</remarks>
    public int CompareTo(DoubleRange? other)
    {
        if (other is null) return 1;
        var comp = _start.CompareTo(other._start);
        if (comp != 0) return comp;
        if (_includeStart && !other._includeStart) return -1;
        if (!_includeStart && other._includeStart) return 1;
        comp = _end.CompareTo(other._end);
        if (comp != 0) return comp;
        if (!_includeEnd && other._includeEnd) return -1;
        if (_includeEnd && !other._includeEnd) return 1;
        return 0;
    }

    /// <summary>
    /// Renders the range in interval notation, using square brackets for inclusive bounds and
    /// parentheses for exclusive ones (for example <c>[1.0,2.0)</c>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.toString()</c>.</remarks>
    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1:F6},{2:F6}{3}",
            _includeStart ? '[' : '(', _start,
            _end, _includeEnd ? ']' : ')');
    }

    /// <summary>
    /// Returns the overlap of two ranges as a new range, correctly combining the inclusiveness of
    /// coinciding bounds, or null if the ranges do not intersect.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.math.DoubleRange.intersection(DoubleRange, DoubleRange)</c>.</remarks>
    public static DoubleRange? Intersection(DoubleRange a, DoubleRange b)
    {
        if (b._start > a._end) return null;
        if (b._start == a._end && !b._includeStart && !a._includeEnd) return null;
        if (a._start > b._end) return null;
        if (a._start == b._end && !a._includeStart && !b._includeEnd) return null;
        double start;
        double end;
        bool includeStart;
        bool includeEnd;
        if (a._start > b._start)
        {
            start = a._start;
            includeStart = a._includeStart;
        }
        else if (b._start > a._start)
        {
            start = b._start;
            includeStart = b._includeStart;
        }
        else
        {
            start = a._start;
            includeStart = a._includeStart && b._includeStart;
        }
        if (a._end < b._end)
        {
            end = a._end;
            includeEnd = a._includeEnd;
        }
        else if (b._end < a._end)
        {
            end = b._end;
            includeEnd = b._includeEnd;
        }
        else
        {
            end = a._end;
            includeEnd = a._includeEnd && b._includeEnd;
        }
        return new DoubleRange(start, end, includeStart, includeEnd);
    }

}
