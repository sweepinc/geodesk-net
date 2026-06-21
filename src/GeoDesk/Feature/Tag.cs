/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature.Store;

namespace GeoDesk.Feature;

/// <summary>
/// A single tag (a <c>key</c> and its <c>value</c>) of a feature.
/// </summary>
/// <remarks>
/// The value is decoded lazily and on demand: read <see cref="Value"/> for the string form, or
/// <see cref="IntValue"/> / <see cref="LongValue"/> / <see cref="DoubleValue"/> for the numeric form.
/// Iterating tags and reading only the numeric value therefore never materializes the string.
/// (All OSM tag values are strings; the numeric accessors parse that value — GeoDesk's compact
/// numeric encoding means the parse is often free.)
/// </remarks>
public readonly struct Tag
{

    readonly StoredFeature _feature;
    readonly string _key;
    readonly long _rawValue; // encoded value; decoded on demand by the accessors below

    /// <summary>
    /// Creates a tag bound to the given feature, key, and still-encoded raw value. The value is left
    /// encoded so the accessors can decode it lazily on demand.
    /// </summary>
    internal Tag(StoredFeature feature, string key, long rawValue)
    {
        _feature = feature;
        _key = key;
        _rawValue = rawValue;
    }

    /// <summary>
    /// The tag's key.
    /// </summary>
    public string Key => _key;

    /// <summary>
    /// The tag's value as a string, in the canonical OSM form. Decodes the raw value on access.
    /// </summary>
    public string Value => _feature.DecodeTagValue(_rawValue);

    /// <summary>
    /// The tag's value parsed as an <see cref="int"/>, decoded directly from the raw value without
    /// first materializing the string.
    /// </summary>
    public int IntValue => _feature.DecodeTagInt(_rawValue);

    /// <summary>
    /// The tag's value parsed as a <see cref="long"/>, decoded directly from the raw value without
    /// first materializing the string.
    /// </summary>
    public long LongValue => _feature.DecodeTagLong(_rawValue);

    /// <summary>
    /// The tag's value parsed as a <see cref="double"/>, decoded directly from the raw value without
    /// first materializing the string.
    /// </summary>
    public double DoubleValue => _feature.DecodeTagDouble(_rawValue);

    /// <summary>
    /// Deconstructs the tag into its key and string value, enabling tuple-style iteration such as
    /// <c>foreach (var (key, value) in …)</c>.
    /// </summary>
    public void Deconstruct(out string key, out string value)
    {
        key = _key;
        value = Value;
    }

    /// <summary>
    /// Renders the tag in its <c>key=value</c> text form.
    /// </summary>
    public override string ToString() => _key + "=" + Value;

}
