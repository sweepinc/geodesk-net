/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Clarisma.Common.Math;
using Clarisma.Common.Util;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;
using DecimalType = Clarisma.Common.Math.Decimal;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Store;

public abstract class StoredFeature : Feature
{
    protected readonly FeatureStore store;
    protected readonly NioBuffer buf;
    protected readonly int ptr;
    protected string? role;

    public StoredFeature(FeatureStore store, NioBuffer buf, int ptr)
    {
        this.store = store;
        this.buf = buf;
        this.ptr = ptr;
    }

    public FeatureStore Store => store;

    public NioBuffer Buffer()
    {
        return buf;
    }

    public int Pointer()
    {
        return ptr;
    }

    public long Id()
    {
        return Id(buf, ptr);
    }

    public static long Id(NioBuffer buf, int ptr)
    {
        return (long)((ulong)buf.GetLong(ptr) >> 12);
    }

    public static int TypeCode(NioBuffer buf, int ptr)
    {
        return (buf.GetInt(ptr) >> 3) & 3;
    }

    public int Flags()
    {
        return buf.GetInt(ptr);
    }

    public abstract FeatureType Type();

    public virtual int X()
    {
        return (buf.GetInt(ptr - 16) + buf.GetInt(ptr - 8)) / 2;
    }

    public virtual int Y()
    {
        return (buf.GetInt(ptr - 12) + buf.GetInt(ptr - 4)) / 2;
    }

    public override bool Equals(object? other)
    {
        if (other is not Feature o) return false;
        return Type() == o.Type() && Id() == o.Id();
    }

    public override int GetHashCode()
    {
        return Id().GetHashCode();
    }

    // value encoding: bit0 type(0=number,1=string), bit1 size(0=narrow,1=wide),
    // bits16-31 narrow value, bits32-63 pointer to wide value. 0 = not found.

    protected long GetCommonKeyValue(int pTags, int key)
    {
        int keyBits = key << 2;
        int p = pTags;
        for (; ; )
        {
            int tag = buf.GetInt(p);
            if ((char)tag >= keyBits)
            {
                if ((tag & 0x7ffc) != keyBits) return 0;
                return ((long)(p + 2) << 32) | ((long)tag & 0xffff_ffffL);
            }
            p += 4 + (tag & 2);
        }
    }

    protected long GetKeyValue(string keyString)
    {
        int key = store.CodeFromString(keyString);
        int ppTags = ptr + 8;
        int pTags = buf.GetInt(ppTags);
        int uncommonKeysFlag = pTags & 1;
        pTags = ppTags + (pTags ^ uncommonKeysFlag);
        int p = pTags;

        if (key > 0 && key <= TagValues.MAX_COMMON_KEY)
        {
            return GetCommonKeyValue(pTags, key);
        }
        if (uncommonKeysFlag == 0) return 0;
        int origin = pTags & unchecked((int)0xffff_fffc);
        p -= 6;
        for (; ; )
        {
            long tag = buf.GetLong(p);
            int rawPointer = (int)(tag >> 16);
            int flags = rawPointer & 7;
            int pKey = ((rawPointer ^ flags) >> 1) + origin;
            if (Bytes.StringEquals(buf, pKey, keyString))
            {
                return ((long)(p - 2) << 32) | flags | (((long)((char)tag)) << 16);
            }
            if ((flags & 4) != 0) return 0;
            p -= 6 + (flags & 2);
        }
    }

    private string ValueAsString(long value)
    {
        if (value == 0) return "";
        int typeAndSize = (int)value & 3;
        if (typeAndSize == 1)
        {
            return store.StringFromCode((char)(value >> 16));
        }
        if (typeAndSize == 3)
        {
            int ppValue = (int)(value >> 32);
            int pValueString = buf.GetInt(ppValue) + ppValue;
            return Bytes.ReadString(buf, pValueString);
        }
        if (typeAndSize == 0)
        {
            int number = (char)(value >> 16) + TagValues.MIN_NUMBER;
            return number.ToString(CultureInfo.InvariantCulture);
        }
        int wide = buf.GetInt((int)(value >> 32));
        int mantissa = (int)((uint)wide >> 2) + TagValues.MIN_NUMBER;
        int scale = wide & 3;
        if (scale == 0) return mantissa.ToString(CultureInfo.InvariantCulture);
        return DecimalType.ToString(DecimalType.Of(mantissa, scale));
    }

    private int ValueAsInt(long value)
    {
        return (int)ValueAsLong(value);
    }

    private long ValueAsLong(long value)
    {
        if (value == 0) return 0;
        int typeAndSize = (int)value & 3;
        if (typeAndSize == 0)
        {
            return (char)(value >> 16) + (long)TagValues.MIN_NUMBER;
        }
        if (typeAndSize == 2)
        {
            int wide = buf.GetInt((int)(value >> 32));
            int mantissa = (int)((uint)wide >> 2) + TagValues.MIN_NUMBER;
            int scale = wide & 3;
            return DecimalType.ToLong(DecimalType.Of(mantissa, scale));
        }
        if (typeAndSize == 3)
        {
            int ppValue = (int)(value >> 32);
            int pValueString = buf.GetInt(ppValue) + ppValue;
            string s = Bytes.ReadString(buf, pValueString);
            return TagValues.ToLong(s);
        }
        string gs = store.StringFromCode((char)(value >> 16));
        return TagValues.ToLong(gs);
    }

    private double ValueAsDouble(long value)
    {
        if (value == 0) return 0;
        int typeAndSize = (int)value & 3;
        if (typeAndSize == 0)
        {
            return (char)(value >> 16) + (double)TagValues.MIN_NUMBER;
        }
        if (typeAndSize == 2)
        {
            int wide = buf.GetInt((int)(value >> 32));
            int mantissa = (int)((uint)wide >> 2) + TagValues.MIN_NUMBER;
            int scale = wide & 3;
            return DecimalType.ToDouble(DecimalType.Of(mantissa, scale));
        }
        if (typeAndSize == 3)
        {
            int ppValue = (int)(value >> 32);
            int pValueString = buf.GetInt(ppValue) + ppValue;
            string s = Bytes.ReadString(buf, pValueString);
            return MathUtils.DoubleFromString(s);
        }
        string gs = store.StringFromCode((char)(value >> 16));
        return MathUtils.DoubleFromString(gs);
    }

    private object ValueAsObject(long value)
    {
        if (value == 0) return "";
        int typeAndSize = (int)value & 3;
        if (typeAndSize == 1)
        {
            return store.StringFromCode((char)(value >> 16));
        }
        if (typeAndSize == 3)
        {
            int ppValue = (int)(value >> 32);
            int pValueString = buf.GetInt(ppValue) + ppValue;
            return Bytes.ReadString(buf, pValueString);
        }
        if (typeAndSize == 0)
        {
            return (char)(value >> 16) + TagValues.MIN_NUMBER;
        }
        int wide = buf.GetInt((int)(value >> 32));
        return TagValues.WideNumberToDouble(wide);
    }

    public string StringValue(string key)
    {
        return ValueAsString(GetKeyValue(key));
    }

    public int IntValue(string key)
    {
        return ValueAsInt(GetKeyValue(key));
    }

    public long LongValue(string key)
    {
        return ValueAsLong(GetKeyValue(key));
    }

    public double DoubleValue(string key)
    {
        return ValueAsDouble(GetKeyValue(key));
    }

    public bool BooleanValue(string key)
    {
        long value = GetKeyValue(key);
        if (value == 0) return false;
        int typeAndSize = (int)value & 3;
        if (typeAndSize == 1)
        {
            return !store.StringFromCode((char)(value >> 16)).Equals("no");
        }
        return true;
    }

    public string Tag(string key)
    {
        return StringValue(key);
    }

    public bool HasTag(string key)
    {
        return GetKeyValue(key) != 0;
    }

    public bool HasTag(string key, string value)
    {
        return StringValue(key).Equals(value);
    }

    public bool IsArea()
    {
        return (buf.GetInt(ptr) & IFeatureFlags.AREA_FLAG) != 0;
    }

    public virtual Box Bounds()
    {
        return new Box(
            buf.GetInt(ptr - 16), buf.GetInt(ptr - 12),
            buf.GetInt(ptr - 8), buf.GetInt(ptr - 4));
    }

    public string? Role()
    {
        return role;
    }

    public void SetRole(string? role)
    {
        this.role = role;
    }

    public bool BelongsTo(Feature parent)
    {
        if (parent.IsRelation())
        {
            return Parents().Relations().Contains(parent);
        }
        else if (parent.IsWay())
        {
            return Parents().Ways().Contains(parent);
        }
        return false;
    }

    public virtual double Area()
    {
        if (!IsArea()) return 0;
        int avgY = (buf.GetInt(ptr - 12) + buf.GetInt(ptr - 4)) / 2;
        double scale = Mercator.MetersAtY(avgY);
        return ToGeometry().Area * scale * scale;
    }

    public abstract int[] ToXY();

    public abstract Geometry ToGeometry();

    public Tags Tags()
    {
        return new TagIterator(this, ptr + 8);
    }

    private sealed class TagIterator : Tags
    {
        private readonly StoredFeature owner;
        private readonly int pTagTable;
        private readonly int uncommonKeysFlag;
        private int pNextTag;
        private string? key;
        private long value;

        public TagIterator(StoredFeature owner, int ppTags)
        {
            this.owner = owner;
            int rawTagsPtr = owner.buf.GetInt(ppTags);
            uncommonKeysFlag = rawTagsPtr & 1;
            pTagTable = (rawTagsPtr ^ uncommonKeysFlag) + ppTags;
            Reset();
        }

        private void Reset()
        {
            pNextTag = pTagTable;
            if (owner.buf.GetInt(pNextTag) == TagValues.EMPTY_TABLE_MARKER)
            {
                pNextTag = (uncommonKeysFlag != 0) ? (pTagTable - 6) : -1;
            }
        }

        public bool Next()
        {
            if (pNextTag < 0) return false;
            if (pNextTag < pTagTable)
            {
                long tag = owner.buf.GetLong(pNextTag);
                int rawPointer = (int)(tag >> 16);
                int flags = rawPointer & 7;
                int origin = pTagTable & unchecked((int)0xffff_fffc);
                int pKey = ((rawPointer ^ flags) >> 1) + origin;
                key = Bytes.ReadString(owner.buf, pKey);
                value = ((long)(pNextTag - 2) << 32) | flags | (((long)((char)tag)) << 16);
                if ((flags & 4) != 0)
                {
                    pNextTag = -1;
                }
                else
                {
                    pNextTag -= 6 + (flags & 2);
                }
            }
            else
            {
                int tag = owner.buf.GetInt(pNextTag);
                key = owner.store.StringFromCode((tag >> 2) & 0x1fff);
                value = ((long)(pNextTag + 2) << 32) | ((long)tag & 0xffff_ffffL);
                if ((tag & 0x8000) != 0)
                {
                    pNextTag = (uncommonKeysFlag == 0) ? -1 : (pTagTable - 6);
                }
                else
                {
                    pNextTag += 4 + (tag & 2);
                }
            }
            return true;
        }

        public string? Key()
        {
            return key;
        }

        public object? Value()
        {
            return owner.ValueAsObject(value);
        }

        public string? StringValue()
        {
            return owner.ValueAsString(value);
        }

        public int IntValue()
        {
            return owner.ValueAsInt(value);
        }

        public long LongValue()
        {
            return owner.ValueAsLong(value);
        }

        public double DoubleValue()
        {
            return owner.ValueAsDouble(value);
        }

        public IDictionary<string, object?> ToMap()
        {
            var map = new Dictionary<string, object?>();
            int pOld = pNextTag;
            Reset();
            while (Next())
            {
                map[Key()!] = Value();
            }
            pNextTag = pOld;
            return map;
        }

        public bool IsEmpty()
        {
            return owner.buf.GetInt(pTagTable) == TagValues.EMPTY_TABLE_MARKER &&
                uncommonKeysFlag == 0;
        }

        public int Size()
        {
            int pOld = pNextTag;
            Reset();
            int count = 0;
            while (Next()) count++;
            pNextTag = pOld;
            return count;
        }
    }

    public bool BelongsToRelation()
    {
        return (buf.GetInt(ptr) & IFeatureFlags.RELATION_MEMBER_FLAG) != 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.parents()</c>.</remarks>
    public virtual Features Parents()
    {
        return BelongsToRelation() ?
            new Query.ParentRelationView(store, buf, GetRelationTablePtr()) : Query.EmptyView.Any;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.parents(String)</c>.</remarks>
    public virtual Features Parents(string query)
    {
        if (BelongsToRelation())
        {
            Match.Matcher matcher = store.GetMatcher(query);
            if ((matcher.AcceptedTypes() & Match.TypeBits.RELATIONS) != 0)
            {
                // PORT: faithful to the Java source, which constructs this view but does
                // not return it (the method always falls through to EmptyView.Any).
                _ = new Query.ParentRelationView(store, buf, GetRelationTablePtr(),
                    matcher.AcceptedTypes(), matcher, null);
            }
        }
        return Query.EmptyView.Any;
    }

    /// <summary>Retrieves the pointer to the feature's relation table.</summary>
    public virtual int GetRelationTablePtr()
    {
        int ppBody = ptr + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        int ppRelTable = pBody - 4;
        return buf.GetInt(ppRelTable) + ppRelTable;
    }

    public bool Matches(Match.Matcher filter)
    {
        return filter.Accept(buf, ptr);
    }

    public abstract IEnumerator<Feature> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
