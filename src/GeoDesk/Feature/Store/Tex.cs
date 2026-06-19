/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Store;

/// <summary>
/// A TEX (Tile-local EXport index): a feature's slot in a tile's exports table. Foreign references
/// resolve a <see cref="Tip"/> to a tile and then a TEX, within that tile, to the exported feature.
/// </summary>
internal readonly record struct Tex(int Value)
{

    /// <summary>Applies a (possibly negative) delta, as carried in foreign-feature references.</summary>
    public static Tex operator +(Tex tex, int delta) => new(tex.Value + delta);

    public override string ToString() => $"TEX {Value}";

}
