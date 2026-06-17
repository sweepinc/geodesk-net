/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * ===========================
 *  T E S T   U S E   O N L Y
 * ===========================
 * A stripped-down tag-table writer used to test the Query Matcher. Port of the Java
 * com.geodesk.gol.compiler.STagTable (test tree).
 */

using System;
using System.Collections.Generic;
using Clarisma.Common.Soar;
using GeoDesk.Feature.Store;
using DecimalType = Clarisma.Common.Math.Decimal;

namespace GeoDesk.Gol.Compiler;

public class STagTable : SharedStruct
{
    private int uncommonKeyCount;
    private readonly Entry[] entries;

    internal enum ValueType
    {
        GlobalString, // string, narrow
        LocalString,  // string, wide
        NarrowNumber, // number, narrow
        WideNumber    // number, wide
    }

    private static bool IsString(ValueType t) => t == ValueType.GlobalString || t == ValueType.LocalString;
    private static bool IsWide(ValueType t) => t == ValueType.LocalString || t == ValueType.WideNumber;

    internal class Entry : IComparable<Entry>
    {
        internal string key = "";
        internal int keyCode;
        internal SString? keyString;
        internal ValueType type;
        internal string value = "";
        internal int valueCode;
        internal SString? valueString;

        public int CompareTo(Entry? o)
        {
            if (keyString != null)
            {
                if (o!.keyString == null) return -1;
                return o.keyString.CompareTo(keyString);
            }
            else
            {
                if (o!.keyString != null) return 1;
            }
            return keyCode.CompareTo(o.keyCode);
        }

        internal int EntrySize()
        {
            int size = (keyCode == 0) ? 4 : 2;
            switch (type)
            {
                case ValueType.GlobalString:
                case ValueType.NarrowNumber:
                    return size + 2;
                case ValueType.LocalString:
                case ValueType.WideNumber:
                    return size + 4;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal bool SetDecimalValue(long d)
        {
            if (d == DecimalType.Invalid) return false;
            int scale = DecimalType.Scale(d);
            if (scale > 3) return false;
            long dLong = DecimalType.Mantissa(d);
            if (dLong < TagValues.MIN_NUMBER || dLong > TagValues.MAX_WIDE_NUMBER) return false;
            if (scale > 0 || dLong > TagValues.MAX_NARROW_NUMBER)
            {
                type = ValueType.WideNumber;
                valueCode = (((int)dLong - TagValues.MIN_NUMBER) << 2) | scale;
                return true;
            }
            type = ValueType.NarrowNumber;
            valueCode = (int)dLong - TagValues.MIN_NUMBER;
            return true;
        }
    }

    private static SString GetLocalString(Dictionary<string, SString> localStrings, string s)
    {
        if (!localStrings.TryGetValue(s, out SString? str))
        {
            str = new SString(s);
            localStrings[s] = str;
        }
        return str;
    }

    public STagTable(string[] tags, IReadOnlyDictionary<string, int> globalStrings,
        Dictionary<string, SString> localStrings)
    {
        SetAlignment(1); // always 2-byte aligned

        if (tags == null || tags.Length == 0)
        {
            entries = new Entry[0];
            SetSize(4);
            return;
        }

        int size = 0;
        int uncommonSize = 0;
        entries = new Entry[tags.Length / 2];
        for (int i = 0; i < tags.Length; i += 2)
        {
            string k = tags[i];
            string v = tags[i + 1];
            Entry e = new Entry();
            e.key = k;
            e.value = v;
            e.keyCode = globalStrings.TryGetValue(k, out int kc) ? kc : 0;
            if (e.keyCode == 0 || e.keyCode > TagValues.MAX_COMMON_KEY)
            {
                e.keyCode = 0;
                e.keyString = GetLocalString(localStrings, k);
                e.keyString.SetAlignment(2); // strings used as keys must be 4-byte aligned
                uncommonKeyCount++;
            }
            e.valueCode = globalStrings.TryGetValue(v, out int vc) ? vc : 0;
            if (e.valueCode != 0)
            {
                e.type = ValueType.GlobalString;
            }
            else
            {
                long d = DecimalType.Parse(v, true);
                if (!e.SetDecimalValue(d))
                {
                    e.type = ValueType.LocalString;
                    e.valueString = GetLocalString(localStrings, v);
                }
            }
            entries[i / 2] = e;
            int entrySize = e.EntrySize();
            size += entrySize;
            if (e.keyString != null) uncommonSize += entrySize;
        }

        Array.Sort(entries);
        if (uncommonKeyCount == entries.Length)
        {
            // A tag table that only has uncommon keys must have an empty table marker
            // where the global keys would normally be
            size += 4;
        }
        SetSize(size);
        SetAnchor(uncommonSize);
    }

    public override void WriteTo(StructOutputStream @out)
    {
        if (entries.Length == 0)
        {
            @out.WriteInt(TagValues.EMPTY_TABLE_MARKER);
            return;
        }

        int origin = (Location() + Anchor()) & unchecked((int)0xffff_fffc);
        for (int i = 0; i < entries.Length; i++)
        {
            Entry e = entries[i];
            bool isUncommonKey = e.keyString != null;

            if (!isUncommonKey)
            {
                int key = e.keyCode << 2;
                if (IsString(e.type)) key |= 1;
                if (IsWide(e.type)) key |= 2;
                if (i == entries.Length - 1) key |= 0x8000;
                @out.WriteShort(key);
            }

            if (e.type == ValueType.LocalString)
            {
                @out.WritePointer(e.valueString);
            }
            else if (IsWide(e.type))
            {
                @out.WriteInt(e.valueCode);
            }
            else
            {
                @out.WriteShort(e.valueCode);
            }

            if (isUncommonKey)
            {
                int ptr = e.keyString!.Location() - origin;
                ptr <<= 1;
                if (IsString(e.type)) ptr |= 1;
                if (IsWide(e.type)) ptr |= 2;
                if (i == 0) ptr |= 4;
                @out.WriteInt(ptr);
            }
        }

        if (uncommonKeyCount == entries.Length)
        {
            @out.WriteInt(TagValues.EMPTY_TABLE_MARKER);
        }
    }

    public int UncommonKeyCount()
    {
        return uncommonKeyCount;
    }
}
