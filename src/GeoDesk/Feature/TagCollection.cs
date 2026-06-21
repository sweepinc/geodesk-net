/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections;
using System.Collections.Generic;
using System.Text;

using GeoDesk.Common.Util;
using GeoDesk.Feature.Store;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature;

/// <summary>
/// The tags of a feature: a read-only collection of <see cref="Tag"/> that is also a by-key map
/// (<see cref="IReadOnlyDictionary{TKey,TValue}"/> of <c>key</c> → <see cref="Tag"/>).
/// </summary>
/// <remarks>
/// The .NET replacement for Java's once-traversable <c>com.geodesk.feature.Tags</c> cursor. It is a
/// <c>readonly struct</c>, so obtaining a feature's tags allocates nothing, and it can be enumerated
/// any number of times — each pass starts fresh. Tag values are decoded lazily by <see cref="Tag"/>,
/// so iterating and reading only the numeric value never materializes a string. OSM tag keys are
/// unique per feature, so the by-key lookups (<see cref="this[string]"/>, <see cref="ContainsKey"/>,
/// <see cref="TryGetValue"/>) are unambiguous.
/// <para>Note: because this is both <c>IEnumerable&lt;Tag&gt;</c> and (via the dictionary)
/// <c>IEnumerable&lt;KeyValuePair&lt;string, Tag&gt;&gt;</c>, LINQ operators on the concrete type are
/// ambiguous — enumerate with <c>foreach</c> (yields <see cref="Tag"/>), or go through
/// <see cref="Values"/> (or a cast) for LINQ.</para>
/// </remarks>
public readonly struct TagCollection : IReadOnlyCollection<Tag>, IReadOnlyDictionary<string, Tag>
{

    readonly StoredFeature? _feature;

    /// <summary>
    /// Creates a tag collection that lazily reads the tags of the given stored feature.
    /// </summary>
    /// <param name="feature">the feature whose tags this collection exposes</param>
    internal TagCollection(StoredFeature feature)
    {
        _feature = feature;
    }

    /// <summary>
    /// True if the feature has no tags (or no backing feature at all).
    /// </summary>
    public bool IsEmpty => _feature == null || _feature.HasNoTags();

    /// <summary>
    /// The number of tags in the feature. Computed by making a full pass over the tag table, since
    /// the count is not stored explicitly.
    /// </summary>
    public int Count
    {
        get
        {
            var n = 0;
            var e = GetEnumerator();
            while (e.MoveNext())
                n++;
            return n;
        }
    }

    /// <summary>
    /// Returns the <see cref="Tag"/> with the given key. OSM keys are unique per feature, so the
    /// lookup is unambiguous.
    /// </summary>
    /// <exception cref="KeyNotFoundException">if the feature has no tag with that key.</exception>
    public Tag this[string key] => TryGetValue(key, out var tag) ? tag : throw new KeyNotFoundException($"No tag with key '{key}'");

    /// <summary>
    /// Checks whether a tag with the given key is present on the feature.
    /// </summary>
    public bool ContainsKey(string key) => _feature != null && _feature.HasTag(key);

    /// <summary>
    /// Tries to get the tag with the given key. Returns true and sets <paramref name="tag"/> when the
    /// key is present; otherwise returns false and sets it to <c>default</c>.
    /// </summary>
    public bool TryGetValue(string key, out Tag tag)
    {
        if (_feature != null)
        {
            var raw = _feature.GetTagValue(key);
            if (raw != 0)
            {
                tag = new Tag(_feature, key, raw);
                return true;
            }
        }

        tag = default;
        return false;
    }

    /// <summary>
    /// The keys of all tags on the feature, in iteration order.
    /// </summary>
    public IEnumerable<string> Keys
    {
        get
        {
            foreach (var tag in this)
                yield return tag.Key;
        }
    }

    /// <summary>
    /// The tags themselves — the values of the by-key map view — in iteration order. Useful as a
    /// LINQ entry point that avoids the dual-<c>IEnumerable</c> ambiguity of the concrete type.
    /// </summary>
    public IEnumerable<Tag> Values
    {
        get
        {
            foreach (var tag in this)
                yield return tag;
        }
    }

    /// <summary>
    /// Returns a struct-typed, allocation-free <see cref="Enumerator"/> over the feature's tags.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(_feature);

    /// <summary>
    /// Explicit <see cref="IEnumerable{T}"/> implementation that boxes the struct enumerator over tags.
    /// </summary>
    IEnumerator<Tag> IEnumerable<Tag>.GetEnumerator() => GetEnumerator();

    // The IReadOnlyDictionary<string, Tag> view enumerates key/Tag pairs (the key duplicates Tag.Key).
    /// <summary>
    /// Explicit dictionary-view enumerator that yields each tag as a key/Tag pair, where the key
    /// duplicates <see cref="Tag.Key"/>.
    /// </summary>
    IEnumerator<KeyValuePair<string, Tag>> IEnumerable<KeyValuePair<string, Tag>>.GetEnumerator()
    {
        foreach (var tag in this)
            yield return new KeyValuePair<string, Tag>(tag.Key, tag);
    }

    /// <summary>
    /// Explicit non-generic <see cref="IEnumerable"/> implementation over the feature's tags.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Renders all tags as <c>key=value</c> pairs joined by <c>, </c>.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var tag in this)
        {
            if (sb.Length != 0)
                sb.Append(", ");

            sb.Append(tag.Key).Append('=').Append(tag.Value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// A forward-only, struct-typed cursor over a feature's tags, yielding <see cref="Tag"/> values.
    /// It walks the encoded tag table directly, distinguishing common (global-string) keys from
    /// uncommon (local-string) keys.
    /// </summary>
    public struct Enumerator : IEnumerator<Tag>
    {

        StoredFeature? _feature;
        NioBuffer _buf;
        int _pTagTable;
        int _uncommonKeysFlag;
        int _pNextTag;
        string _key;
        long _rawValue;

        /// <summary>
        /// Initializes the cursor on the given feature, locating its tag table and positioning the
        /// cursor before the first tag. A null feature produces an immediately-exhausted enumerator.
        /// </summary>
        internal Enumerator(StoredFeature? feature)
        {
            _feature = feature;
            _key = "";
            _rawValue = 0;
            if (feature == null)
            {
                _buf = default;
                _pTagTable = 0;
                _uncommonKeysFlag = 0;
                _pNextTag = -1;
                return;
            }

            _buf = feature.Buffer();
            var ppTags = feature.Ptr + 8;
            var rawTagsPtr = _buf.GetInt(ppTags);
            _uncommonKeysFlag = rawTagsPtr & 1;
            _pTagTable = (rawTagsPtr ^ _uncommonKeysFlag) + ppTags;
            _pNextTag = _pTagTable;
            if (_buf.GetInt(_pNextTag) == TagValues.EMPTY_TABLE_MARKER)
                _pNextTag = (_uncommonKeysFlag != 0) ? (_pTagTable - 6) : -1;
        }

        /// <summary>
        /// The tag at the current cursor position. Its value remains encoded and is decoded on demand
        /// by the <see cref="Tag"/> accessors.
        /// </summary>
        public Tag Current => new Tag(_feature!, _key, _rawValue);

        /// <summary>
        /// The current tag, boxed for the non-generic <see cref="IEnumerator"/> contract.
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the cursor to the next tag, decoding its key and raw value. Returns false once all
        /// common and uncommon tags have been read.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredFeature.TagIterator.next()</c>.</remarks>
        public bool MoveNext()
        {
            if (_pNextTag < 0)
                return false;

            var buf = _buf;
            if (_pNextTag < _pTagTable)
            {
                var tag = buf.GetLong(_pNextTag);
                var rawPointer = (int)(tag >> 16);
                var flags = rawPointer & 7;
                var origin = _pTagTable & unchecked((int)0xffff_fffc);
                var pKey = ((rawPointer ^ flags) >> 1) + origin;
                _key = Bytes.ReadString(buf, pKey);
                _rawValue = ((long)(_pNextTag - 2) << 32) | flags | (((long)((char)tag)) << 16);
                if ((flags & 4) != 0)
                    _pNextTag = -1;
                else
                    _pNextTag -= 6 + (flags & 2);
            }
            else
            {
                var tag = buf.GetInt(_pNextTag);
                _key = _feature!.Store.StringFromCode((tag >> 2) & 0x1fff);
                _rawValue = ((long)(_pNextTag + 2) << 32) | ((long)tag & 0xffff_ffffL);
                if ((tag & 0x8000) != 0)
                    _pNextTag = (_uncommonKeysFlag == 0) ? -1 : (_pTagTable - 6);
                else
                    _pNextTag += 4 + (tag & 2);
            }

            return true;
        }

        /// <summary>
        /// Resets the cursor to before the first tag, so the feature's tags can be enumerated again.
        /// </summary>
        public void Reset()
        {
            this = new Enumerator(_feature);
        }

        /// <summary>
        /// No-op; the enumerator holds no unmanaged resources but implements <c>IDisposable</c>
        /// to satisfy the <see cref="IEnumerator{T}"/> contract.
        /// </summary>
        public readonly void Dispose()
        {

        }

    }

}
