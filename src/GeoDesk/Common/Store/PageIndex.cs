/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Store;

/// <summary>
/// A blob's starting page — the 32-bit handle a <see cref="BlobStore"/> uses to address a blob.
/// Deliberately distinct from a byte offset within a segment and from a <em>count</em> of pages.
/// Page 0 is the root/header block, so it never names a blob and is reused as the "no page"
/// sentinel (<see cref="Nil"/>).
/// </summary>
internal readonly record struct PageIndex(int Value)
{

    /// <summary>The "no page" sentinel (page 0 — the header block, never a blob).</summary>
    public static readonly PageIndex Nil = new(0);

    /// <summary>
    /// True when this is the "no page" sentinel (page 0).
    /// </summary>
    public bool IsNil => Value == 0;

    /// <summary>
    /// Advances by a number of pages (e.g. to the page following a blob of that length).
    /// </summary>
    public static PageIndex operator +(PageIndex page, int pages) => new(page.Value + pages);

    /// <summary>
    /// Moves back by a number of pages.
    /// </summary>
    public static PageIndex operator -(PageIndex page, int pages) => new(page.Value - pages);

    /// <summary>
    /// Returns true if the left page precedes the right page in the store.
    /// </summary>
    public static bool operator <(PageIndex a, PageIndex b) => a.Value < b.Value;

    /// <summary>
    /// Returns true if the left page follows the right page in the store.
    /// </summary>
    public static bool operator >(PageIndex a, PageIndex b) => a.Value > b.Value;

    /// <summary>
    /// Returns true if the left page precedes or equals the right page in the store.
    /// </summary>
    public static bool operator <=(PageIndex a, PageIndex b) => a.Value <= b.Value;

    /// <summary>
    /// Returns true if the left page follows or equals the right page in the store.
    /// </summary>
    public static bool operator >=(PageIndex a, PageIndex b) => a.Value >= b.Value;

    /// <summary>
    /// Returns a debug string of the form <c>page#{value}</c>.
    /// </summary>
    public override string ToString() => $"page#{Value}";

}
