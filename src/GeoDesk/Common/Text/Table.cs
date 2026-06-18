/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace GeoDesk.Common.Text;

// TODO: don't shrink column to less than header text width!

/// <remarks>Ported from Java <c>com.clarisma.common.text.Table</c>.</remarks>
internal class Table
{

    readonly List<Column> _columns = new List<Column>();
    readonly List<string> _values = new List<string>();
    int _currentCol;
    int _currentRow;
    int _totalWidth;
    int _maxWidth = 100;
    bool _ready;

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.maxWidth(int)</c>.</remarks>
    public void MaxWidth(int maxWidth)
    {
        _maxWidth = maxWidth;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.Column</c>.</remarks>
    public class Column : IComparable<Column>
    {

        internal int span = 1;
        internal int gap = 2;
        internal string? header;
        // NOTE: Java's DecimalFormat pattern is stored verbatim and applied via
        // double.ToString(pattern). Basic patterns ("#,##0.00") map cleanly; exotic
        // DecimalFormat features are not reproduced in this straight port.
        internal string? format;
        internal int minWidth = 8;
        internal int width;
        internal int averageWidth;
        internal int widthVariance;

        /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.Column.format(String)</c>.</remarks>
        public Column Format(string format)
        {
            this.format = format;
            return this;
        }

        /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.Column.gap(int)</c>.</remarks>
        public Column Gap(int gap)
        {
            this.gap = gap;
            return this;
        }

        /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.Column.compareTo(Column)</c>.</remarks>
        public int CompareTo(Column? other)
        {
            if (other is null) return -1;
            return other.widthVariance.CompareTo(this.widthVariance);
        }

    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.column()</c>.</remarks>
    public Column AddColumn()
    {
        var c = new Column();
        _columns.Add(c);
        return c;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.column(String)</c>.</remarks>
    public Column AddColumn(string header)
    {
        var c = AddColumn();
        c.header = header;
        return c;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.column(String, String)</c>.</remarks>
    public Column AddColumn(string header, string format)
    {
        var c = AddColumn();
        c.header = header;
        c.format = format;
        return c;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.skipColumn()</c>.</remarks>
    public void SkipColumn()
    {
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.beginRow()</c>.</remarks>
    void BeginRow()
    {
        _values.Add("");
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.add(String)</c>.</remarks>
    public void Add(string s)
    {
        _ready = false;
        if (_currentCol == 0) BeginRow();
        _values.Add(s);
        AdjustColumnSize(_currentCol, s.Length);
        _currentCol++;
        if (_currentCol == _columns.Count)
        {
            _currentCol = 0;
            _currentRow++;
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.adjustColumnSize(int, int)</c>.</remarks>
    void AdjustColumnSize(int col, int w)
    {
        var c = _columns[col];
        if (c.width < w) c.width = w;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.add(double)</c>.</remarks>
    public void Add(double v)
    {
        var c = _columns[_currentCol];
        Add(v.ToString(c.format, CultureInfo.InvariantCulture));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.cell(int, int, String)</c>.</remarks>
    public void Cell(int row, int col, string value)
    {
        var cell = row * (_columns.Count + 1) + col + 1;
        while (cell >= _values.Count) Add("");
        _values[cell] = value;
        AdjustColumnSize(col, value.Length);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.currentRow()</c>.</remarks>
    public int CurrentRow => _currentRow;

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.newRow()</c>.</remarks>
    public int NewRow()
    {
        while (_currentCol != 0) Add("");
        return _currentRow;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.divider(String)</c>.</remarks>
    public void Divider(string div)
    {
        NewRow();
        for (var i = 0; i <= _columns.Count; i++) _values.Add(div);
        _currentRow++;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.layout()</c>.</remarks>
    protected void Layout()
    {
        NewRow(); // add remaining cells in case last row was not completed
        foreach (var col in _columns)
        {
            _totalWidth += col.width + col.gap;
        }
        _totalWidth -= _columns[_columns.Count - 1].gap;
        if (_totalWidth > _maxWidth)
        {
            ShrinkColumns();
        }
        _ready = true;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.shrinkColumns()</c>.</remarks>
    void ShrinkColumns()
    {
        var rowCount = 0;
        var currentCol = -1;
        foreach (var value in _values)
        {
            if (currentCol >= 0)
            {
                var col = _columns[currentCol];
                col.averageWidth += value.Length;
            }
            currentCol++;
            if (currentCol == _columns.Count)
            {
                currentCol = -1;
                rowCount++;
            }
        }

        // Gather all non-numeric columns (We cannot shrink columns
        // with numeric values)

        var elasticColumns = new List<Column>();
        foreach (var col in _columns)
        {
            if (col.format == null)
            {
                elasticColumns.Add(col);
                col.averageWidth /= rowCount;
                col.widthVariance = col.width - col.averageWidth;
            }
        }
        if (elasticColumns.Count == 0) return;

        var needToTrim = _totalWidth - _maxWidth;

        // When shrinking columns, we'll focus on those that have the highest
        // difference between width and the average width of their cells,
        // because these are the columns that will be least degraded (usually,
        // there are one or two outliers that are far wider than the other
        // cells, and only these will appear trimmed)
        // first, we sort the columns, highest variance first

        elasticColumns.Sort();
        var startCol = elasticColumns[0];
        while (needToTrim > 0)
        {
            var excess = startCol.widthVariance;
            if (excess <= 0) break;
            var trimColCount = 1;
            for (; trimColCount < elasticColumns.Count; trimColCount++)
            {
                var col = elasticColumns[trimColCount];
                if (col.widthVariance < excess)
                {
                    // In this round, the maximum we trim off these columns
                    // is the *difference* between the excess over their
                    // averages and the excess over the next-excessive column
                    // (Otherwise, trimming would be too greedy and column
                    // widths would become imbalanced)
                    excess -= col.widthVariance;
                    break;
                }
            }
            excess *= trimColCount;
            var trimNow = System.Math.Min(excess, needToTrim);
            var trimmed = TrimColumns(elasticColumns, trimColCount, trimNow);
            if (trimmed == 0) break;
            needToTrim -= trimmed;
        }
        if (needToTrim > 0)
        {
            // As final step, trim all columns by equal amount
            TrimColumns(elasticColumns, elasticColumns.Count, needToTrim);
        }
    }

    /// <summary>
    /// Shrinks a set of columns by trimming each width. The width of a column
    /// will never be reduced to less than Column.minWidth.
    /// </summary>
    /// <param name="elasticColumns">the columns that can be trimmed</param>
    /// <param name="colCount">the number of columns to actually trim (counted from the start)</param>
    /// <param name="trimNow">the total number of characters to trim</param>
    /// <returns>the actual number of characters trimmed</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.trimColumns(List, int, int)</c>.</remarks>
    int TrimColumns(List<Column> elasticColumns, int colCount, int trimNow)
    {
        var leftToTrim = trimNow;
        for (var i = colCount - 1; i >= 0; i--)
        {
            var col = elasticColumns[i];
            var trimThisCol = (i == 0) ? leftToTrim : (trimNow / colCount);
            trimThisCol = System.Math.Min(trimThisCol, col.width - col.minWidth);
            col.width -= trimThisCol;
            col.widthVariance -= trimThisCol;
            leftToTrim -= trimThisCol;
        }
        var trimmed = trimNow - leftToTrim;
        _totalWidth -= trimmed;
        return trimmed;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.print(Appendable)</c>.</remarks>
    public void Print(TextWriter @out)
    {
        if (!_ready) Layout();
        var n = 0;
        while (n < _values.Count)
        {
            var rowType = _values[n++];
            if (rowType.Length != 0)
            {
                @out.Write(Repeat(rowType, _totalWidth));
                @out.Write("\n");
                n += _columns.Count;
                continue;
            }
            foreach (var col in _columns)
            {
                var value = _values[n++];
                var len = value.Length;
                var width = col.width;
                if (len < width)
                {
                    var padding = new string(' ', width - len);
                    if (col.format != null)
                    {
                        @out.Write(padding);
                        @out.Write(value);
                    }
                    else
                    {
                        @out.Write(value);
                        @out.Write(padding);
                    }
                }
                else if (len > width)
                {
                    @out.Write(value.Substring(0, width - 3));
                    @out.Write("...");
                }
                else
                {
                    @out.Write(value);
                }
                @out.Write(new string(' ', col.gap));
            }
            @out.Write("\n");
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Table.toString()</c>.</remarks>
    public override string ToString()
    {
        var buf = new StringBuilder();
        using (var writer = new StringWriter(buf))
        {
            Print(writer);
        }
        return buf.ToString();
    }

    /// <remarks>Port-only helper for Java's <c>String.repeat(int)</c>.</remarks>
    static string Repeat(string s, int times)
    {
        if (times <= 0) return string.Empty;
        var b = new StringBuilder(s.Length * times);
        for (var i = 0; i < times; i++) b.Append(s);
        return b.ToString();
    }

}
