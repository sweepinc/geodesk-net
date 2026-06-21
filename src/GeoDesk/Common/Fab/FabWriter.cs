/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GeoDesk.Common.Fab;

/// <summary>
/// Writer that emits a FAB document. Key/value pairs are accumulated as a tree of
/// <see cref="Item"/> nodes while the document is being built, then formatted with aligned columns
/// and tab-based indentation when the document is finished.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter</c>.</remarks>
internal class FabWriter
{

    readonly TextWriter @out;
    readonly int tabSize = 4;
    Item current;
    readonly Stack<Item> stack;

    // Public because the public KeyValue methods return it (in Java this is a private
    // nested class, but Java permits public methods to return package-private types).
    /// <summary>
    /// One node in the FAB document tree being built: a key with an optional value and comment, and
    /// an optional list of child items forming a nested block.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.Item</c>.</remarks>
    public sealed class Item
    {

        public string? key;
        public string? value;
        public string? comment;
        public List<Item>? children;

        /// <summary>
        /// Creates an item with the given key, value, and comment (any of which may be null).
        /// </summary>
        /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.Item(String, String, String)</c>.</remarks>
        public Item(string? k, string? v, string? c)
        {
            key = k;
            value = v;
            comment = c;
        }

        /// <summary>
        /// Appends a child item, lazily allocating the child list on first use.
        /// </summary>
        /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.Item.add(Item)</c>.</remarks>
        public void Add(Item item)
        {
            if (children == null) children = new List<Item>();
            children.Add(item);
        }

    }

    /// <summary>
    /// Creates a writer that will emit the formatted FAB document to the given text writer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter(Appendable)</c>.</remarks>
    public FabWriter(TextWriter @out)
    {
        this.@out = @out;
        current = new Item(null, null, null);
        stack = new Stack<Item>();
    }

    /// <summary>
    /// Opens a nested block under the given key with no value or comment.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.beginKey(String)</c>.</remarks>
    public void BeginKey(string key)
    {
        BeginKey(key, null, null);
    }

    /// <summary>
    /// Opens a nested block under the given key, optionally carrying a value and comment; subsequent
    /// writes are nested until <see cref="EndKey"/> is called.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.beginKey(String, String, String)</c>.</remarks>
    public void BeginKey(string key, string? value, string? comment)
    {
        var item = KeyValue(key, value, comment);
        stack.Push(current);
        current = item;
    }

    /// <summary>
    /// Adds a leaf key/value pair (no comment) to the current block and returns the created item.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.keyValue(String, String)</c>.</remarks>
    public Item KeyValue(string key, string? value)
    {
        return KeyValue(key, value, null);
    }

    /// <summary>
    /// Adds a leaf key/value pair with an optional comment to the current block and returns the
    /// created item.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.keyValue(String, String, String)</c>.</remarks>
    public Item KeyValue(string key, string? value, string? comment)
    {
        var item = new Item(key, value, comment);
        current.Add(item);
        return item;
    }

    /// <summary>
    /// Closes the most recently opened block, returning subsequent writes to its parent.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.endKey()</c>.</remarks>
    public void EndKey()
    {
        current = stack.Pop();
    }

    /// <summary>
    /// Writes the given number of tab characters for indentation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.indent(int)</c>.</remarks>
    void Indent(int count)
    {
        for (var i = 0; i < count; i++) @out.Write('\t');
    }

    /// <summary>
    /// Recursively formats and writes a list of items at the given nesting level, computing key and
    /// value column widths so that values and comments align on tab stops.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.writeItems(int, List)</c>.</remarks>
    void WriteItems(int level, List<Item> items)
    {
        var keyWidth = 0;
        var valWidth = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var keyLen = item.key!.Length;
            var valLen = item.value == null ? 0 : item.value.Length;
            if (keyLen > keyWidth) keyWidth = keyLen;
            if (valLen > valWidth) valWidth = valLen;
        }
        keyWidth += 2; // for the ':' and space
        valWidth += 2; // for two spaces
        var padding = keyWidth % tabSize;
        if (padding > 0) keyWidth += tabSize - padding;
        padding = valWidth % tabSize;
        if (padding > 0) valWidth += tabSize - padding;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var value = item.value;
            Indent(level);
            @out.Write(item.key);
            @out.Write(':');
            if (value != null || item.comment != null)
            {
                padding = keyWidth - item.key!.Length - 1;
                Indent((padding + tabSize - 1) / tabSize);
                if (value != null)
                {
                    // TODO: multi-line
                    @out.Write(value);
                }
                if (item.comment != null)
                {
                    padding = valWidth - (value == null ? 0 : value.Length);
                    Indent((padding + tabSize - 1) / tabSize + 1);
                    @out.Write(item.comment);
                }
            }
            @out.Write('\n');
            if (item.children != null)
            {
                WriteItems(level + 1, item.children);
            }
        }
    }

    /// <summary>
    /// Flushes the accumulated document tree to the output, writing all top-level items and their
    /// descendants, then clears the buffered children.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.endDocument()</c>.</remarks>
    public void EndDocument()
    {
        Debug.Assert(stack.Count == 0);
        if (current.children != null) WriteItems(0, current.children);
        current.children = null;
    }

}
