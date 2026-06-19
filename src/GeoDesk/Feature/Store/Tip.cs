/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using System.IO;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A TIP (Tile Index Pointer): a tile's slot in the tile index. An index into a different space than
/// a <see cref="GeoDesk.Common.Store.PageIndex"/> — a TIP resolves (via a tile-index entry) to the
/// page that holds the tile.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip</c> (a utility class there; a value
/// type here, with its helpers folded in).</remarks>
internal readonly record struct Tip(int Value)
{

    /// <summary>The initial TIP iterators start from, so a 0..32,767 range fits a narrow 15-bit delta.</summary>
    public static readonly Tip Start = new(FeatureConstants.START_TIP);

    /// <summary>Applies a (possibly negative) delta, as carried in foreign-feature references.</summary>
    public static Tip operator +(Tip tip, int delta) => new(tip.Value + delta);

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.toString(int)</c>.</remarks>
    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:X6}", Value);

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.folder(Path, int)</c>.</remarks>
    public string Folder(string rootPath) =>
        Path.Combine(rootPath, string.Format(CultureInfo.InvariantCulture, "{0:X3}", (int)((uint)Value >> 12)));

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.path(Path, int, String)</c> (renamed: avoids clash with System.IO.Path).</remarks>
    public string PathOf(string root, string suffix) =>
        Path.Combine(Folder(root), string.Format(CultureInfo.InvariantCulture, "{0:X3}{1}", Value & 0xfff, suffix));

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.isWideTipDelta(int)</c>.</remarks>
    public static bool IsWideTipDelta(int tipDelta) => (short)(tipDelta << 1) != (tipDelta << 1);

}
