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

/// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter</c>.</remarks>
internal class FabWriter
{

    readonly TextWriter @out;
    readonly int tabSize = 4;
    Item current;
    readonly Stack<Item> stack;

    // Public because the public KeyValue methods return it (in Java this is a private
    // nested class, but Java permits public methods to return package-private types).
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.Item</c>.</remarks>
    public sealed class Item
    {

        public string? key;
        public string? value;
        public string? comment;
        public List<Item>? children;

        /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.Item(String, String, String)</c>.</remarks>
        public Item(string? k, string? v, string? c)
        {
            key = k;
            value = v;
            comment = c;
        }

        /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.Item.add(Item)</c>.</remarks>
        public void Add(Item item)
        {
            if (children == null) children = new List<Item>();
            children.Add(item);
        }

    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter(Appendable)</c>.</remarks>
    public FabWriter(TextWriter @out)
    {
        this.@out = @out;
        current = new Item(null, null, null);
        stack = new Stack<Item>();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.beginKey(String)</c>.</remarks>
    public void BeginKey(string key)
    {
        BeginKey(key, null, null);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.beginKey(String, String, String)</c>.</remarks>
    public void BeginKey(string key, string? value, string? comment)
    {
        var item = KeyValue(key, value, comment);
        stack.Push(current);
        current = item;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.keyValue(String, String)</c>.</remarks>
    public Item KeyValue(string key, string? value)
    {
        return KeyValue(key, value, null);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.keyValue(String, String, String)</c>.</remarks>
    public Item KeyValue(string key, string? value, string? comment)
    {
        var item = new Item(key, value, comment);
        current.Add(item);
        return item;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.endKey()</c>.</remarks>
    public void EndKey()
    {
        current = stack.Pop();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.indent(int)</c>.</remarks>
    void Indent(int count)
    {
        for (var i = 0; i < count; i++) @out.Write('\t');
    }

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

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter.endDocument()</c>.</remarks>
    public void EndDocument()
    {
        Debug.Assert(stack.Count == 0);
        if (current.children != null) WriteItems(0, current.children);
        current.children = null;
    }

}
