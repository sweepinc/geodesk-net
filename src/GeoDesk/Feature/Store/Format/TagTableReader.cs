/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;
using GeoDesk.Common.Util;

namespace GeoDesk.Feature.Store.Format;

/// <summary>
/// A forward-only cursor over a feature's tag table, decoding one tag per <see cref="MoveNext"/>.
/// It first walks the common (global-string-keyed) tags forward from the tag-table pointer, then the
/// uncommon (local-string-keyed) tags backward from it — so it needs the whole tile/segment span plus
/// the feature's anchor position, not a slice to the feature (the tag table is shared between features
/// and addressed by a signed relative pointer, so it may sit anywhere in the tile).
/// </summary>
/// <remarks>
/// <para>
/// One signed, self-relative pointer (<c>taggedPtr</c>, at <c>anchorPos + 8</c>) locates a pivot
/// (<c>_tagTablePtr</c>): common (global-keyed) tags read forward from it, uncommon (local-keyed)
/// tags read backward. Value kinds: 0 = narrow number, 1 = global string, 2 = wide number,
/// 3 = local string.
/// </para>
/// <code>
///   feature record                          tag table (shared; anywhere in the tile)
///   +----------------+---------------+        ───────────── _tagTablePtr ─────────────
///   | flags + id (8) | taggedPtr (4) | ──►    ◄── uncommon (local keys) │ common (global keys) ──►
///   +----------------+---------------+              backward entries     │     forward entries
///   ^ anchorPos      ^ anchorPos+8
///
///   taggedPtr   32-bit @ anchorPos+8
///   +----------------------------------------------------+---+
///   |        signed offset to tag table           [31:1] | L |   L = hasLocalKeys (bit 0)
///   +----------------------------------------------------+---+   offset = taggedPtr &amp; ~1
///
///   key16       16-bit, common entry @ _p   (value follows: 2 bytes narrow / 4 bytes wide)
///   +---+--------------------------------------------+--------+
///   | F |        global key code          [14:2]     |  kind  |   F = last-common (bit 15)
///   +---+--------------------------------------------+--------+   kind [1:0]
///   keyCode = (key16 >> 2) &amp; 0x1fff    kind = key16 &amp; 3    F = key16 &amp; 0x8000
///
///   keyPtr      32-bit, uncommon entry @ _p  (value precedes the key word, growing downward)
///   +--------------------------------------------+---+--------+
///   |      rel. ptr to key string      [31:3]    | F |  kind  |   F = last/first-in-mem (bit 2)
///   +--------------------------------------------+---+--------+   kind [1:0]
///   strPtr = (keyPtr &amp; ~7) >> 1  (from _origin)    kind = keyPtr &amp; 3    F = keyPtr &amp; 4
/// </code>
/// <para>
/// Port-only cursor: encapsulates the tag-table traversal that the Java <c>MatcherCoder</c> emits as
/// bytecode and that <c>AstTagMatcher</c> previously inlined.
/// </para>
/// </remarks>
internal ref struct TagTableReader
{

    readonly ReadOnlySpan<byte> _segment; // the whole tile/segment span
    readonly bool _hasLocalKeys;
    readonly int _tagTablePtr;

    int _p;        // position of the current entry
    int _origin;   // base for local key-string pointers (set when entering the local phase)
    bool _inLocals;
    bool _done;

    /// <summary>The current tag's global-string key code, or 0 for a local (uncommon) key.</summary>
    public int KeyCode;

    /// <summary>The current tag's local key name as raw UTF-8 bytes, or empty for a global key.</summary>
    public ReadOnlySpan<byte> KeyBytes;

    /// <summary>The current tag's value kind (one of the <see cref="TagValues"/> kind constants).</summary>
    public int Kind;

    /// <summary>The current tag's value code (narrow number, global-string code, or wide-number code).</summary>
    public int ValueCode;

    /// <summary>The current tag's value as raw UTF-8 bytes, when it is a local string; otherwise empty.</summary>
    public ReadOnlySpan<byte> ValueBytes;

    /// <summary>The absolute position of the current tag's local-string value, when it is a local string; otherwise 0.</summary>
    public int ValueStringPos;

    /// <summary>
    /// Positions the cursor before the first tag of the feature whose anchor (flags word) is at
    /// <paramref name="pFeature"/> within the segment span <paramref name="segment"/>.
    /// </summary>
    public TagTableReader(ReadOnlySpan<byte> segment, int pFeature)
    {
        _segment = segment;
        var pTagTable = pFeature + 8;
        var taggedPtr = segment.GetIntLE(pTagTable);
        _hasLocalKeys = (taggedPtr & 1) != 0;
        _tagTablePtr = pTagTable + (taggedPtr & ~1);
        _p = _tagTablePtr;
        _origin = 0;
        _inLocals = false;
        _done = false;
        KeyCode = 0;
        KeyBytes = default;
        Kind = 0;
        ValueCode = 0;
        ValueBytes = default;
        ValueStringPos = 0;
    }

    /// <summary>
    /// Advances to the next tag, decoding its key and value into the public fields. Returns false once
    /// both the common and uncommon tags are exhausted.
    /// </summary>
    public bool MoveNext()
    {
        KeyCode = 0;
        KeyBytes = default;
        Kind = 0;
        ValueCode = 0;
        ValueBytes = default;
        ValueStringPos = 0;

        if (_done)
            return false;

        if (!_inLocals)
            return ReadCommon();

        return ReadUncommon();
    }

    // Reads one common (global-keyed) tag forward from the anchor; transitions to the uncommon phase
    // at the empty-table marker or after the last common tag.
    bool ReadCommon()
    {
        int key16 = _segment.GetCharLE(_p);
        var keyCode = (key16 >> 2) & 0x1fff;
        if (keyCode == 0)
        {
            // empty-table marker / no (more) common keys
            EnterLocals();
            return MoveNext();
        }

        Kind = key16 & 3;
        KeyCode = keyCode;
        var wide = (Kind & 2) != 0;
        int next;
        if (wide)
        {
            var w = _segment.GetIntLE(_p + 2);
            if (Kind == TagValues.LOCAL_STRING)
            {
                ValueStringPos = (_p + 2) + w;
                ValueBytes = Bytes.ReadUtf8String(_segment, ValueStringPos);
            }
            else
                ValueCode = w;
            next = _p + 6;
        }
        else
        {
            ValueCode = _segment.GetCharLE(_p + 2);
            next = _p + 4;
        }

        if ((key16 & 0x8000) != 0)
            EnterLocals(); // this was the last common tag
        else
            _p = next;

        return true;
    }

    // Reads one uncommon (local-keyed) tag backward from the tag-table pointer.
    bool ReadUncommon()
    {
        var keyPtr = _segment.GetIntLE(_p);
        Kind = keyPtr & 3;
        var wide = (Kind & 2) != 0;
        var valueSize = wide ? 4 : 2;
        var valuePos = _p - valueSize;
        if (wide)
        {
            var w = _segment.GetIntLE(valuePos);
            if (Kind == TagValues.LOCAL_STRING)
            {
                ValueStringPos = valuePos + w;
                ValueBytes = Bytes.ReadUtf8String(_segment, ValueStringPos);
            }
            else
                ValueCode = w;
        }
        else
        {
            ValueCode = _segment.GetCharLE(valuePos);
        }

        var relPtr = (keyPtr & ~7) >> 1;
        KeyBytes = Bytes.ReadUtf8String(_segment, _origin + relPtr);

        if ((keyPtr & 4) != 0)
            _done = true; // this was the first (= last visited) uncommon key
        else
            _p = valuePos - 4;

        return true;
    }

    // Switches the cursor into the uncommon-key (backward) phase, or ends iteration if there are none.
    void EnterLocals()
    {
        _inLocals = true;
        if (!_hasLocalKeys)
        {
            _done = true;
            return;
        }

        _origin = _tagTablePtr & ~3;
        _p = _tagTablePtr - 4;
    }

}
