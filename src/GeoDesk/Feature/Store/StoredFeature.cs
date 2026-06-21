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

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Store;

/// <summary>
/// Abstract base for features read directly from a feature library tile. Decodes the
/// shared parts of the stored representation (id, type, flags, bounds, tags, parents)
/// from the backing buffer; concrete subclasses add type-specific behaviour.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature</c>.</remarks>
internal abstract class StoredFeature : IFeature
{

    protected readonly FeatureStore store;
    protected readonly NioBuffer buf;
    protected readonly int ptr;
    protected string? role;

    /// <summary>
    /// Creates a stored feature backed by the given store, buffer, and pointer to its
    /// record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public StoredFeature(FeatureStore store, NioBuffer buf, int ptr)
    {
        this.store = store;
        this.buf = buf;
        this.ptr = ptr;
    }

    /// <summary>
    /// The feature store this feature was read from.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.store()</c>.</remarks>
    public FeatureStore Store => store;

    /// <summary>
    /// Returns the buffer backing this feature's record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.buffer()</c>.</remarks>
    public NioBuffer Buffer()
    {
        return buf;
    }

    /// <summary>
    /// Returns the buffer pointer to this feature's record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.pointer()</c>.</remarks>
    public int Pointer()
    {
        return ptr;
    }

    /// <summary>
    /// The OSM identifier of this feature.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.id()</c>.</remarks>
    public long Id => IdAt(buf, ptr);

    /// <summary>
    /// Decodes the OSM identifier from the feature record at the given buffer position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.id(ByteBuffer, int)</c>.</remarks>
    public static long IdAt(NioBuffer buf, int ptr)
    {
        return (long)((ulong)buf.GetLong(ptr) >> 12);
    }

    /// <summary>
    /// Decodes the 2-bit type code from the feature record at the given buffer
    /// position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.typeCode(ByteBuffer, int)</c>.</remarks>
    public static int TypeCode(NioBuffer buf, int ptr)
    {
        return (buf.GetInt(ptr) >> 3) & 3;
    }

    /// <summary>
    /// Returns the raw flags word from this feature's record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.flags()</c>.</remarks>
    public int Flags()
    {
        return buf.GetInt(ptr);
    }

    /// <summary>
    /// The kind of feature this is (node, way, or relation).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.type()</c>.</remarks>
    public abstract FeatureType Type { get; }

    /// <summary>
    /// The representative X coordinate, the midpoint of the feature's bounding box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.x()</c>.</remarks>
    public virtual int X => (buf.GetInt(ptr - 16) + buf.GetInt(ptr - 8)) / 2;

    /// <summary>
    /// The representative Y coordinate, the midpoint of the feature's bounding box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.y()</c>.</remarks>
    public virtual int Y => (buf.GetInt(ptr - 12) + buf.GetInt(ptr - 4)) / 2;

    /// <summary>
    /// Two features are equal when they have the same type and id.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.equals(Object)</c>.</remarks>
    public override bool Equals(object? other)
    {
        if (other is not IFeature o)
            return false;
        return Type == o.Type && Id == o.Id;
    }

    /// <summary>
    /// Returns a hash derived from the feature's id.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    // value encoding: bit0 type(0=number,1=string), bit1 size(0=narrow,1=wide),
    // bits16-31 narrow value, bits32-63 pointer to wide value. 0 = not found.

    /// <summary>
    /// Scans the common-key portion of the tag table for the given key code and
    /// returns its encoded value, or 0 if the key is absent.
    /// </summary>
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

    /// <summary>
    /// Looks up the encoded value of the tag with the given key string, searching the
    /// common-key table for known keys and the uncommon-key table otherwise; returns 0
    /// when the key is absent.
    /// </summary>
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

    /// <summary>
    /// Decodes an encoded tag value to its string representation, handling global and
    /// local strings as well as narrow and wide numbers.
    /// </summary>
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

    /// <summary>
    /// Decodes an encoded tag value to an int by truncating its long representation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.valueAsInt(long)</c>.</remarks>
    int ValueAsInt(long value)
    {
        return (int)ValueAsLong(value);
    }

    /// <summary>
    /// Decodes an encoded tag value to a long, parsing string-encoded values where
    /// necessary.
    /// </summary>
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

    /// <summary>
    /// Decodes an encoded tag value to a double, parsing string-encoded values where
    /// necessary.
    /// </summary>
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

    /// <summary>
    /// Decodes an encoded tag value to a boxed object: a string for string values or a
    /// number for numeric values.
    /// </summary>
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

    /// <summary>
    /// Returns the value of the given tag as a string, or an empty string if absent.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.stringValue(String)</c>.</remarks>
    public string StringValue(string key)
    {
        return ValueAsString(GetKeyValue(key));
    }

    /// <summary>
    /// Returns the value of the given tag as an int, or 0 if absent.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.intValue(String)</c>.</remarks>
    public int IntValue(string key)
    {
        return ValueAsInt(GetKeyValue(key));
    }

    /// <summary>
    /// Returns the value of the given tag as a long, or 0 if absent.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.longValue(String)</c>.</remarks>
    public long LongValue(string key)
    {
        return ValueAsLong(GetKeyValue(key));
    }

    /// <summary>
    /// Returns the value of the given tag as a double, or 0 if absent.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.doubleValue(String)</c>.</remarks>
    public double DoubleValue(string key)
    {
        return ValueAsDouble(GetKeyValue(key));
    }

    /// <summary>
    /// Returns the value of the given tag interpreted as a boolean: false when absent
    /// or equal to "no", true otherwise.
    /// </summary>
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

    /// <summary>
    /// Returns the value of the given tag as a string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.tag(String)</c>.</remarks>
    public string Tag(string key)
    {
        return StringValue(key);
    }

    /// <summary>
    /// Returns true if this feature has a tag with the given key.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.hasTag(String)</c>.</remarks>
    public bool HasTag(string key)
    {
        return GetKeyValue(key) != 0;
    }

    /// <summary>
    /// Returns true if this feature has a tag with the given key and value.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.hasTag(String, String)</c>.</remarks>
    public bool HasTag(string key, string value)
    {
        return StringValue(key).Equals(value);
    }

    /// <summary>
    /// True if this feature is an area (a closed way or area relation).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.isArea()</c>.</remarks>
    public bool IsArea => (buf.GetInt(ptr) & FeatureFlags.AREA_FLAG) != 0;

    /// <summary>
    /// The feature's bounding box, read from the four extent words preceding its
    /// record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.bounds()</c>.</remarks>
    public virtual Box Bounds => new Box(
        buf.GetInt(ptr - 16), buf.GetInt(ptr - 12),
        buf.GetInt(ptr - 8), buf.GetInt(ptr - 4));

    /// <summary>
    /// The role this feature plays within a relation, or null when not viewed as a
    /// member.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.role()</c>.</remarks>
    public string? Role => role;

    /// <summary>
    /// Sets the relation-member role for this feature instance.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.setRole(String)</c>.</remarks>
    public void SetRole(string? role)
    {
        this.role = role;
    }

    /// <summary>
    /// Returns true if this feature is a member of the given relation or a node of the
    /// given way.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.belongsTo(Feature)</c>.</remarks>
    public bool BelongsTo(IFeature parent)
    {
        if (parent.IsRelation)
            return Parents().Relations().Contains(parent);

        if (parent.IsWay)
            return Parents().Ways().Contains(parent);

        return false;
    }

    /// <summary>
    /// The area of this feature in square meters when it is an area, or 0 otherwise;
    /// computed from its geometry scaled by the Mercator distortion at its latitude.
    /// </summary>
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

    /// <summary>
    /// Returns this feature's coordinates as a flat X/Y array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.toXY()</c>.</remarks>
    public abstract int[] ToXY();

    /// <summary>
    /// Builds the NTS geometry representing this feature.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.toGeometry()</c>.</remarks>
    public abstract Geometry ToGeometry();

    /// <summary>
    /// A collection over this feature's tags.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.tags()</c>.</remarks>
    public TagCollection Tags => new TagCollection(this);

    // --- Internal hooks used by TagCollection / TagCollection.Enumerator to walk and decode tags. ---

    /// <summary>The buffer pointer to this feature's record.</summary>
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

    /// <summary>Decodes a raw encoded tag value to its string form (lazily, on demand).</summary>
    internal string DecodeTagValue(long value) => ValueAsString(value);

    /// <summary>Decodes a raw encoded tag value to its int form.</summary>
    internal int DecodeTagInt(long value) => ValueAsInt(value);

    /// <summary>Decodes a raw encoded tag value to its long form.</summary>
    internal long DecodeTagLong(long value) => ValueAsLong(value);

    /// <summary>Decodes a raw encoded tag value to its double form.</summary>
    internal double DecodeTagDouble(long value) => ValueAsDouble(value);

    /// <summary>
    /// True if this feature is a member of at least one relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.belongsToRelation()</c>.</remarks>
    public bool BelongsToRelation => (buf.GetInt(ptr) & FeatureFlags.RELATION_MEMBER_FLAG) != 0;

    /// <summary>
    /// Returns a query over the relations this feature belongs to, or an empty view
    /// when it is not a relation member.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.parents()</c>.</remarks>
    public virtual IFeatureQuery Parents()
    {
        return BelongsToRelation ?
            new Query.ParentRelationView(store, buf, GetRelationTablePtr()) : Query.EmptyView.Any;
    }

    /// <summary>
    /// Returns a query over the parent relations of this feature that satisfy the given
    /// GOQL query string. (Mirrors the Java source, which always yields an empty view.)
    /// </summary>
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

    /// <summary>
    /// Returns true if this feature satisfies the given matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.matches(Matcher)</c>.</remarks>
    public bool Matches(Match.Matcher filter)
    {
        return filter.Accept(buf, ptr);
    }

    /// <summary>
    /// Returns an enumerator over this feature's members (way nodes or relation
    /// members); a node yields nothing.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.iterator()</c>.</remarks>
    public abstract IEnumerator<IFeature> GetEnumerator();

    /// <summary>
    /// Returns the non-generic enumerator over this feature's members.
    /// </summary>
    /// <remarks>Port-only adapter (no direct Java counterpart): the non-generic IEnumerable.GetEnumerator.</remarks>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
