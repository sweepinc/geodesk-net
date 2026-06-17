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

namespace Clarisma.Common.Text;

// TODO: don't shrink column to less than header text width!

/// <remarks>Ported from Java <c>com.clarisma.common.text.Table</c>.</remarks>
public class Table
{
    private readonly List<Column> columns = new List<Column>();
    private readonly List<string> values = new List<string>();
    private int currentCol;
    private int currentRow;
    private int totalWidth;
    private int maxWidth = 100;
    private bool ready;

    public void MaxWidth(int maxWidth)
    {
        this.maxWidth = maxWidth;
    }

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

        public Column Format(string format)
        {
            this.format = format;
            return this;
        }

        public Column Gap(int gap)
        {
            this.gap = gap;
            return this;
        }

        public int CompareTo(Column? other)
        {
            if (other is null) return -1;
            return other.widthVariance.CompareTo(this.widthVariance);
        }
    }

    public Column AddColumn()
    {
        Column c = new Column();
        columns.Add(c);
        return c;
    }

    public Column AddColumn(string header)
    {
        Column c = AddColumn();
        c.header = header;
        return c;
    }

    public Column AddColumn(string header, string format)
    {
        Column c = AddColumn();
        c.header = header;
        c.format = format;
        return c;
    }

    public void SkipColumn()
    {
    }

    private void BeginRow()
    {
        values.Add("");
    }

    public void Add(string s)
    {
        ready = false;
        if (currentCol == 0) BeginRow();
        values.Add(s);
        AdjustColumnSize(currentCol, s.Length);
        currentCol++;
        if (currentCol == columns.Count)
        {
            currentCol = 0;
            currentRow++;
        }
    }

    private void AdjustColumnSize(int col, int w)
    {
        Column c = columns[col];
        if (c.width < w) c.width = w;
    }

    public void Add(double v)
    {
        Column c = columns[currentCol];
        Add(v.ToString(c.format, CultureInfo.InvariantCulture));
    }

    public void Cell(int row, int col, string value)
    {
        int cell = row * (columns.Count + 1) + col + 1;
        while (cell >= values.Count) Add("");
        values[cell] = value;
        AdjustColumnSize(col, value.Length);
    }

    public int CurrentRow => currentRow;

    public int NewRow()
    {
        while (currentCol != 0) Add("");
        return currentRow;
    }

    public void Divider(string div)
    {
        NewRow();
        for (int i = 0; i <= columns.Count; i++) values.Add(div);
        currentRow++;
    }

    protected void Layout()
    {
        NewRow(); // add remaining cells in case last row was not completed
        foreach (Column col in columns)
        {
            totalWidth += col.width + col.gap;
        }
        totalWidth -= columns[columns.Count - 1].gap;
        if (totalWidth > maxWidth)
        {
            ShrinkColumns();
        }
        ready = true;
    }

    private void ShrinkColumns()
    {
        int rowCount = 0;
        int currentCol = -1;
        foreach (string value in values)
        {
            if (currentCol >= 0)
            {
                Column col = columns[currentCol];
                col.averageWidth += value.Length;
            }
            currentCol++;
            if (currentCol == columns.Count)
            {
                currentCol = -1;
                rowCount++;
            }
        }

        // Gather all non-numeric columns (We cannot shrink columns
        // with numeric values)

        List<Column> elasticColumns = new List<Column>();
        foreach (Column col in columns)
        {
            if (col.format == null)
            {
                elasticColumns.Add(col);
                col.averageWidth /= rowCount;
                col.widthVariance = col.width - col.averageWidth;
            }
        }
        if (elasticColumns.Count == 0) return;

        int needToTrim = totalWidth - maxWidth;

        // When shrinking columns, we'll focus on those that have the highest
        // difference between width and the average width of their cells,
        // because these are the columns that will be least degraded (usually,
        // there are one or two outliers that are far wider than the other
        // cells, and only these will appear trimmed)
        // first, we sort the columns, highest variance first

        elasticColumns.Sort();
        Column startCol = elasticColumns[0];
        while (needToTrim > 0)
        {
            int excess = startCol.widthVariance;
            if (excess <= 0) break;
            int trimColCount = 1;
            for (; trimColCount < elasticColumns.Count; trimColCount++)
            {
                Column col = elasticColumns[trimColCount];
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
            int trimNow = System.Math.Min(excess, needToTrim);
            int trimmed = TrimColumns(elasticColumns, trimColCount, trimNow);
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
    private int TrimColumns(List<Column> elasticColumns, int colCount, int trimNow)
    {
        int leftToTrim = trimNow;
        for (int i = colCount - 1; i >= 0; i--)
        {
            Column col = elasticColumns[i];
            int trimThisCol = (i == 0) ? leftToTrim : (trimNow / colCount);
            trimThisCol = System.Math.Min(trimThisCol, col.width - col.minWidth);
            col.width -= trimThisCol;
            col.widthVariance -= trimThisCol;
            leftToTrim -= trimThisCol;
        }
        int trimmed = trimNow - leftToTrim;
        totalWidth -= trimmed;
        return trimmed;
    }

    public void Print(TextWriter @out)
    {
        if (!ready) Layout();
        int n = 0;
        while (n < values.Count)
        {
            string rowType = values[n++];
            if (rowType.Length != 0)
            {
                @out.Write(Repeat(rowType, totalWidth));
                @out.Write("\n");
                n += columns.Count;
                continue;
            }
            foreach (Column col in columns)
            {
                string value = values[n++];
                int len = value.Length;
                int width = col.width;
                if (len < width)
                {
                    string padding = new string(' ', width - len);
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

    public override string ToString()
    {
        StringBuilder buf = new StringBuilder();
        using (var writer = new StringWriter(buf))
        {
            Print(writer);
        }
        return buf.ToString();
    }

    private static string Repeat(string s, int times)
    {
        if (times <= 0) return string.Empty;
        StringBuilder b = new StringBuilder(s.Length * times);
        for (int i = 0; i < times; i++) b.Append(s);
        return b.ToString();
    }
}
