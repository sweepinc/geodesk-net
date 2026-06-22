/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace GeoDesk.Feature.Store;

/// <summary>
/// The store's global string table: maps a global-string code to its UTF-8 bytes (zero-copy slices into the
/// mapped store) and, lazily, to a decoded <see cref="string"/>. It also provides the reverse lookup
/// (UTF-8 → code) the query parser needs — keyed by bytes, so resolving a query literal never forces the
/// table to be decoded. A <see cref="string"/> is materialized only when <see cref="Text"/> is called, i.e.
/// for regex matching and the user-facing tag API.
/// </summary>
/// <remarks>
/// Port-only: replaces the plain <c>String[]</c> + string-keyed map Java passes around (Java decodes the whole
/// table to UTF-16 at load). Here a planet-scale table (up to ~64K entries) is loaded with zero string
/// decoding; only the codes a query actually touches are ever turned into <see cref="string"/>s.
/// </remarks>
internal sealed class GlobalStringTable
{

    /// <summary>
    /// An empty table, used where a store has no global strings yet (or none were supplied).
    /// </summary>
    public static readonly GlobalStringTable Empty = FromStrings(Array.Empty<string>());

    /// <summary>
    /// Test-only: builds a table from already-decoded strings, encoding each to UTF-8 and pre-filling the
    /// string cache so <see cref="Text"/> returns the originals without re-decoding.
    /// </summary>
    internal static GlobalStringTable FromStrings(string[] strings)
    {
        var utf8 = new ReadOnlyMemory<byte>[strings.Length];
        for (var i = 0; i < strings.Length; i++)
            utf8[i] = Encoding.UTF8.GetBytes(strings[i]);
        var table = new GlobalStringTable(utf8);
        for (var i = 0; i < strings.Length; i++)
            table._text[i] = strings[i];
        return table;
    }

    readonly ReadOnlyMemory<byte>[] _utf8;                 // per code: UTF-8 bytes (no length prefix)
    readonly string?[] _text;                              // per code: lazily decoded UTF-16
    readonly Dictionary<ReadOnlyMemory<byte>, int> _codes; // reverse: UTF-8 → code, byte-keyed

    /// <summary>
    /// Builds a table over the given per-code UTF-8 slices (typically zero-copy slices into the mapped store),
    /// indexing them for the reverse (bytes → code) lookup. The string cache starts empty; entries are decoded
    /// lazily by <see cref="Text"/>.
    /// </summary>
    public GlobalStringTable(ReadOnlyMemory<byte>[] utf8)
    {
        _utf8 = utf8;
        _text = new string?[utf8.Length];
        _codes = new Dictionary<ReadOnlyMemory<byte>, int>(utf8.Length, Utf8Comparer.Instance);
        for (var i = 0; i < utf8.Length; i++)
            _codes[utf8[i]] = i; // global strings are unique; on the off chance they aren't, last wins
    }

    /// <summary>
    /// The number of strings in the table (codes run from 0 to <c>Count - 1</c>).
    /// </summary>
    public int Count => _utf8.Length;

    /// <summary>
    /// The UTF-8 bytes of the string with the given code, as a zero-copy slice into the mapped store — no
    /// allocation and no decoding.
    /// </summary>
    public ReadOnlyMemory<byte> Utf8(int code)
    {
        return _utf8[code];
    }

    /// <summary>
    /// The decoded UTF-16 string with the given code, materialized from its UTF-8 bytes and cached on first
    /// use. This is the only member that allocates a <see cref="string"/>.
    /// </summary>
    public string Text(int code)
    {
        return _text[code] ??= Encoding.UTF8.GetString(_utf8[code].Span);
    }

    /// <summary>
    /// Looks up the code for the given UTF-8 bytes via the byte-keyed reverse map, returning <c>false</c> if
    /// the bytes are not a global string.
    /// </summary>
    public bool TryGetCode(ReadOnlySpan<byte> utf8, out int code)
    {
        return _codes.GetAlternateLookup<ReadOnlySpan<byte>>().TryGetValue(utf8, out code);
    }

    /// <summary>
    /// Looks up the code for the given string by encoding it to UTF-8 (into a stack or pooled buffer) and
    /// probing the byte-keyed map; returns <c>false</c> if it is not a global string. Callers apply their own
    /// not-found sentinel (the parser uses 0, <see cref="FeatureStore.CodeFromString"/> uses -1).
    /// </summary>
    public bool TryGetCode(string s, out int code)
    {
        var max = Encoding.UTF8.GetMaxByteCount(s.Length);
        byte[]? rented = null;
        Span<byte> buf = max <= 256 ? stackalloc byte[256] : (rented = ArrayPool<byte>.Shared.Rent(max));
        try
        {
            var n = Encoding.UTF8.GetBytes(s, buf);
            return TryGetCode(buf.Slice(0, n), out code);
        }
        finally
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Content equality and hashing over UTF-8 byte sequences, with a <see cref="ReadOnlySpan{Byte}"/>
    /// alternate-lookup so the reverse map can be probed with a transient span (the parser's stack-encoded
    /// literal) without allocating a key. The stored keys are the table's <see cref="ReadOnlyMemory{Byte}"/>
    /// slices; the span and memory hashes are computed identically so an alternate probe lands in the same
    /// bucket as the matching key.
    /// </summary>
    sealed class Utf8Comparer : IEqualityComparer<ReadOnlyMemory<byte>>, IAlternateEqualityComparer<ReadOnlySpan<byte>, ReadOnlyMemory<byte>>
    {

        /// <summary>The shared, stateless instance.</summary>
        public static readonly Utf8Comparer Instance = new Utf8Comparer();

        /// <summary>
        /// Computes a content hash over a UTF-8 byte sequence; used for both the span and memory key forms so
        /// they hash consistently.
        /// </summary>
        static int Hash(ReadOnlySpan<byte> s)
        {
            var hc = new HashCode();
            hc.AddBytes(s);
            return hc.ToHashCode();
        }

        /// <summary>Compares two stored keys by their UTF-8 byte content.</summary>
        public bool Equals(ReadOnlyMemory<byte> a, ReadOnlyMemory<byte> b)
        {
            return a.Span.SequenceEqual(b.Span);
        }

        /// <summary>Hashes a stored key by its UTF-8 byte content.</summary>
        public int GetHashCode(ReadOnlyMemory<byte> m)
        {
            return Hash(m.Span);
        }

        /// <summary>Compares a probe span against a stored key by UTF-8 byte content (the alternate lookup).</summary>
        public bool Equals(ReadOnlySpan<byte> alternate, ReadOnlyMemory<byte> other)
        {
            return alternate.SequenceEqual(other.Span);
        }

        /// <summary>Hashes a probe span the same way as a stored key, so alternate lookups find the right bucket.</summary>
        public int GetHashCode(ReadOnlySpan<byte> alternate)
        {
            return Hash(alternate);
        }

        /// <summary>
        /// Materializes a stored key from a probe span. Required by the interface but only used when the
        /// dictionary is mutated through the alternate lookup, which this table never does (it only reads).
        /// </summary>
        public ReadOnlyMemory<byte> Create(ReadOnlySpan<byte> alternate)
        {
            return alternate.ToArray();
        }

    }

}
