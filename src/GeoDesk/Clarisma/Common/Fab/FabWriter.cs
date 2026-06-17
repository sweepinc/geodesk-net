/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Clarisma.Common.Fab;

/// <remarks>Ported from Java <c>com.clarisma.common.fab.FabWriter</c>.</remarks>
public class FabWriter
{
    private readonly TextWriter @out;
    private readonly int tabSize = 4;
    private Item current;
    private readonly Stack<Item> stack;

    // Public because the public KeyValue methods return it (in Java this is a private
    // nested class, but Java permits public methods to return package-private types).
    public sealed class Item
    {
        public string? key;
        public string? value;
        public string? comment;
        public List<Item>? children;

        public Item(string? k, string? v, string? c)
        {
            key = k;
            value = v;
            comment = c;
        }

        public void Add(Item item)
        {
            if (children == null) children = new List<Item>();
            children.Add(item);
        }
    }

    public FabWriter(TextWriter @out)
    {
        this.@out = @out;
        current = new Item(null, null, null);
        stack = new Stack<Item>();
    }

    public void BeginKey(string key)
    {
        BeginKey(key, null, null);
    }

    public void BeginKey(string key, string? value, string? comment)
    {
        Item item = KeyValue(key, value, comment);
        stack.Push(current);
        current = item;
    }

    public Item KeyValue(string key, string? value)
    {
        return KeyValue(key, value, null);
    }

    public Item KeyValue(string key, string? value, string? comment)
    {
        Item item = new Item(key, value, comment);
        current.Add(item);
        return item;
    }

    public void EndKey()
    {
        current = stack.Pop();
    }

    private void Indent(int count)
    {
        for (int i = 0; i < count; i++) @out.Write('\t');
    }

    private void WriteItems(int level, List<Item> items)
    {
        int keyWidth = 0;
        int valWidth = 0;
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            int keyLen = item.key!.Length;
            int valLen = item.value == null ? 0 : item.value.Length;
            if (keyLen > keyWidth) keyWidth = keyLen;
            if (valLen > valWidth) valWidth = valLen;
        }
        keyWidth += 2; // for the ':' and space
        valWidth += 2; // for two spaces
        int padding = keyWidth % tabSize;
        if (padding > 0) keyWidth += tabSize - padding;
        padding = valWidth % tabSize;
        if (padding > 0) valWidth += tabSize - padding;
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            string? value = item.value;
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

    public void EndDocument()
    {
        Debug.Assert(stack.Count == 0);
        if (current.children != null) WriteItems(0, current.children);
        current.children = null;
    }
}
