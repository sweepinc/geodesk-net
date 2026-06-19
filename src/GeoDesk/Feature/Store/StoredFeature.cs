/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using GeoDesk.Common.Math;
using GeoDesk.Common.Util;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Store;

/// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature</c>.</remarks>
internal abstract class StoredFeature : IFeature
{

    protected readonly FeatureStore store;
    protected readonly NioBuffer buf;
    protected readonly int ptr;
    protected string? role;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public StoredFeature(FeatureStore store, NioBuffer buf, int ptr)
    {
        this.store = store;
        this.buf = buf;
        this.ptr = ptr;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.store()</c>.</remarks>
    public FeatureStore Store => store;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.buffer()</c>.</remarks>
    public NioBuffer Buffer()
    {
        return buf;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.pointer()</c>.</remarks>
    public int Pointer()
    {
        return ptr;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.id()</c>.</remarks>
    public long Id => IdAt(buf, ptr);

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.id(ByteBuffer, int)</c>.</remarks>
    public static long IdAt(NioBuffer buf, int ptr)
    {
        return (long)((ulong)buf.GetLong(ptr) >> 12);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.typeCode(ByteBuffer, int)</c>.</remarks>
    public static int TypeCode(NioBuffer buf, int ptr)
    {
        return (buf.GetInt(ptr) >> 3) & 3;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.flags()</c>.</remarks>
    public int Flags()
    {
        return buf.GetInt(ptr);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.type()</c>.</remarks>
    public abstract FeatureType Type { get; }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.x()</c>.</remarks>
    public virtual int X => (buf.GetInt(ptr - 16) + buf.GetInt(ptr - 8)) / 2;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.y()</c>.</remarks>
    public virtual int Y => (buf.GetInt(ptr - 12) + buf.GetInt(ptr - 4)) / 2;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.equals(Object)</c>.</remarks>
    public override bool Equals(object? other)
    {
        if (other is not IFeature o)
            return false;
        return Type == o.Type && Id == o.Id;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    // value encoding: bit0 type(0=number,1=string), bit1 size(0=narrow,1=wide),
    // bits16-31 narrow value, bits32-63 pointer to wide value. 0 = not found.

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.getCommonKeyValue(int, int)</c>.</remarks>
    protected long GetCommonKeyValue(int pTags, int key)
    {
        var keyBits = key << 2;
        var p = pTags;
        for (; ; )
        {
            var tag = buf.GetInt(p);
            if ((char)tag >= keyBits)
            {
                if ((tag & 0x7ffc) != keyBits)
                    return 0;
                return ((long)(p + 2) << 32) | ((long)tag & 0xffff_ffffL);
            }
            p += 4 + (tag & 2);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.getKeyValue(String)</c>.</remarks>
    protected long GetKeyValue(string keyString)
    {
        var key = store.CodeFromString(keyString);
        var ppTags = ptr + 8;
        var pTags = buf.GetInt(ppTags);
        var uncommonKeysFlag = pTags & 1;
        pTags = ppTags + (pTags ^ uncommonKeysFlag);
        var p = pTags;

        if (key > 0 && key <= TagValues.MAX_COMMON_KEY)
        {
            return GetCommonKeyValue(pTags, key);
        }
        if (uncommonKeysFlag == 0)
            return 0;
        var origin = pTags & unchecked((int)0xffff_fffc);
        p -= 6;
        for (; ; )
        {
            var tag = buf.GetLong(p);
            var rawPointer = (int)(tag >> 16);
            var flags = rawPointer & 7;
            var pKey = ((rawPointer ^ flags) >> 1) + origin;
            if (Bytes.StringEquals(buf, pKey, keyString))
            {
                return ((long)(p - 2) << 32) | flags | (((long)((char)tag)) << 16);
            }
            if ((flags & 4) != 0)
                return 0;
            p -= 6 + (flags & 2);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.valueAsString(long)</c>.</remarks>
    string ValueAsString(long value)
    {
        if (value == 0)
            return "";

        var typeAndSize = (int)value & 3;
        if (typeAndSize == 1)
        {
            return store.StringFromCode((char)(value >> 16));
        }

        if (typeAndSize == 3)
        {
            var ppValue = (int)(value >> 32);
            var pValueString = buf.GetInt(ppValue) + ppValue;
            return Bytes.ReadString(buf, pValueString);
        }

        if (typeAndSize == 0)
        {
            var number = (char)(value >> 16) + TagValues.MIN_NUMBER;
            return number.ToString(CultureInfo.InvariantCulture);
        }

        var wide = buf.GetInt((int)(value >> 32));
        var mantissa = (int)((uint)wide >> 2) + TagValues.MIN_NUMBER;
        var scale = wide & 3;
        if (scale == 0)
            return mantissa.ToString(CultureInfo.InvariantCulture);

        return DecimalCodec.ToString(DecimalCodec.Of(mantissa, scale));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.valueAsInt(long)</c>.</remarks>
    int ValueAsInt(long value)
    {
        return (int)ValueAsLong(value);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.valueAsLong(long)</c>.</remarks>
    long ValueAsLong(long value)
    {
        if (value == 0)
            return 0;

        var typeAndSize = (int)value & 3;
        if (typeAndSize == 0)
        {
            return (char)(value >> 16) + (long)TagValues.MIN_NUMBER;
        }

        if (typeAndSize == 2)
        {
            var wide = buf.GetInt((int)(value >> 32));
            var mantissa = (int)((uint)wide >> 2) + TagValues.MIN_NUMBER;
            var scale = wide & 3;
            return DecimalCodec.ToLong(DecimalCodec.Of(mantissa, scale));
        }

        if (typeAndSize == 3)
        {
            var ppValue = (int)(value >> 32);
            var pValueString = buf.GetInt(ppValue) + ppValue;
            var s = Bytes.ReadString(buf, pValueString);
            return TagValues.ToLong(s);
        }

        var gs = store.StringFromCode((char)(value >> 16));
        return TagValues.ToLong(gs);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.valueAsDouble(long)</c>.</remarks>
    double ValueAsDouble(long value)
    {
        if (value == 0)
            return 0;

        var typeAndSize = (int)value & 3;
        if (typeAndSize == 0)
            return (char)(value >> 16) + (double)TagValues.MIN_NUMBER;

        if (typeAndSize == 2)
        {
            var wide = buf.GetInt((int)(value >> 32));
            var mantissa = (int)((uint)wide >> 2) + TagValues.MIN_NUMBER;
            var scale = wide & 3;
            return DecimalCodec.ToDouble(DecimalCodec.Of(mantissa, scale));
        }

        if (typeAndSize == 3)
        {
            var ppValue = (int)(value >> 32);
            var pValueString = buf.GetInt(ppValue) + ppValue;
            var s = Bytes.ReadString(buf, pValueString);
            return MathUtils.DoubleFromString(s);
        }

        var gs = store.StringFromCode((char)(value >> 16));
        return MathUtils.DoubleFromString(gs);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.valueAsObject(long)</c>.</remarks>
    object ValueAsObject(long value)
    {
        if (value == 0)
            return "";

        var typeAndSize = (int)value & 3;
        if (typeAndSize == 1)
            return store.StringFromCode((char)(value >> 16));

        if (typeAndSize == 3)
        {
            var ppValue = (int)(value >> 32);
            var pValueString = buf.GetInt(ppValue) + ppValue;
            return Bytes.ReadString(buf, pValueString);
        }

        if (typeAndSize == 0)
            return (char)(value >> 16) + TagValues.MIN_NUMBER;

        var wide = buf.GetInt((int)(value >> 32));
        return TagValues.WideNumberToDouble(wide);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.stringValue(String)</c>.</remarks>
    public string StringValue(string key)
    {
        return ValueAsString(GetKeyValue(key));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.intValue(String)</c>.</remarks>
    public int IntValue(string key)
    {
        return ValueAsInt(GetKeyValue(key));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.longValue(String)</c>.</remarks>
    public long LongValue(string key)
    {
        return ValueAsLong(GetKeyValue(key));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.doubleValue(String)</c>.</remarks>
    public double DoubleValue(string key)
    {
        return ValueAsDouble(GetKeyValue(key));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.booleanValue(String)</c>.</remarks>
    public bool BooleanValue(string key)
    {
        var value = GetKeyValue(key);
        if (value == 0)
            return false;

        var typeAndSize = (int)value & 3;
        if (typeAndSize == 1)
            return !store.StringFromCode((char)(value >> 16)).Equals("no");

        return true;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.tag(String)</c>.</remarks>
    public string Tag(string key)
    {
        return StringValue(key);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.hasTag(String)</c>.</remarks>
    public bool HasTag(string key)
    {
        return GetKeyValue(key) != 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.hasTag(String, String)</c>.</remarks>
    public bool HasTag(string key, string value)
    {
        return StringValue(key).Equals(value);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.isArea()</c>.</remarks>
    public bool IsArea => (buf.GetInt(ptr) & IFeatureFlags.AREA_FLAG) != 0;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.bounds()</c>.</remarks>
    public virtual Box Bounds => new Box(
        buf.GetInt(ptr - 16), buf.GetInt(ptr - 12),
        buf.GetInt(ptr - 8), buf.GetInt(ptr - 4));

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.role()</c>.</remarks>
    public string? Role => role;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.setRole(String)</c>.</remarks>
    public void SetRole(string? role)
    {
        this.role = role;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.belongsTo(Feature)</c>.</remarks>
    public bool BelongsTo(IFeature parent)
    {
        if (parent.IsRelation)
            return Parents().Relations().Contains(parent);

        if (parent.IsWay)
            return Parents().Ways().Contains(parent);

        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.area()</c>.</remarks>
    public virtual double Area
    {
        get
        {
            if (!IsArea)
                return 0;

            var avgY = (buf.GetInt(ptr - 12) + buf.GetInt(ptr - 4)) / 2;
            var scale = Mercator.MetersAtY(avgY);
            return ToGeometry().Area * scale * scale;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.toXY()</c>.</remarks>
    public abstract int[] ToXY();

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.toGeometry()</c>.</remarks>
    public abstract Geometry ToGeometry();

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.tags()</c>.</remarks>
    public TagCollection Tags => new TagCollection(this);

    // --- Internal hooks used by TagCollection / TagCollection.Enumerator to walk and decode tags. ---

    internal int Ptr => ptr;

    /// <summary>True if this feature has no tags (O(1) empty-table-marker check).</summary>
    internal bool HasNoTags()
    {
        var ppTags = ptr + 8;
        var rawTagsPtr = buf.GetInt(ppTags);
        var uncommonKeysFlag = rawTagsPtr & 1;
        var pTagTable = (rawTagsPtr ^ uncommonKeysFlag) + ppTags;
        return buf.GetInt(pTagTable) == TagValues.EMPTY_TABLE_MARKER && uncommonKeysFlag == 0;
    }

    /// <summary>Looks up the raw encoded value of a tag by key (0 if absent).</summary>
    internal long GetTagValue(string key) => GetKeyValue(key);

    /// <summary>Decodes a raw encoded tag value to its string / numeric forms (lazily, on demand).</summary>
    internal string DecodeTagValue(long value) => ValueAsString(value);
    internal int DecodeTagInt(long value) => ValueAsInt(value);
    internal long DecodeTagLong(long value) => ValueAsLong(value);
    internal double DecodeTagDouble(long value) => ValueAsDouble(value);

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.belongsToRelation()</c>.</remarks>
    public bool BelongsToRelation => (buf.GetInt(ptr) & IFeatureFlags.RELATION_MEMBER_FLAG) != 0;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.parents()</c>.</remarks>
    public virtual IFeatureQuery Parents()
    {
        return BelongsToRelation ?
            new Query.ParentRelationView(store, buf, GetRelationTablePtr()) : Query.EmptyView.Any;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.parents(String)</c>.</remarks>
    public virtual IFeatureQuery Parents(string query)
    {
        if (BelongsToRelation)
        {
            var matcher = store.GetMatcher(query);
            if ((matcher.AcceptedTypes & Match.TypeBits.RELATIONS) != 0)
            {
                // PORT: faithful to the Java source, which constructs this view but does
                // not return it (the method always falls through to EmptyView.Any).
                _ = new Query.ParentRelationView(store, buf, GetRelationTablePtr(),
                    matcher.                    AcceptedTypes, matcher, null);
            }
        }
        return Query.EmptyView.Any;
    }

    /// <summary>Retrieves the pointer to the feature's relation table.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.getRelationTablePtr()</c>.</remarks>
    public virtual int GetRelationTablePtr()
    {
        var ppBody = ptr + 12;
        var pBody = buf.GetInt(ppBody) + ppBody;
        var ppRelTable = pBody - 4;
        return buf.GetInt(ppRelTable) + ppRelTable;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.matches(Matcher)</c>.</remarks>
    public bool Matches(Match.Matcher filter)
    {
        return filter.Accept(buf, ptr);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.iterator()</c>.</remarks>
    public abstract IEnumerator<IFeature> GetEnumerator();

    /// <remarks>Port-only adapter (no direct Java counterpart): the non-generic IEnumerable.GetEnumerator.</remarks>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
