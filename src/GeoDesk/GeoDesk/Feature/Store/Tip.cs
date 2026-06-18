/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using System.IO;

namespace GeoDesk.Feature.Store;

/// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip</c>.</remarks>
internal static class Tip
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.toString(int)</c>.</remarks>
    public static string ToString(int tip)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:X6}", tip);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.folder(Path, int)</c>.</remarks>
    public static string Folder(string rootPath, int tip)
    {
        return Path.Combine(rootPath, string.Format(CultureInfo.InvariantCulture, "{0:X3}", (int)((uint)tip >> 12)));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.path(Path, int, String)</c> (renamed: avoids clash with System.IO.Path).</remarks>
    public static string PathOf(string root, int tip, string suffix)
    {
        return Path.Combine(Folder(root, tip),
            string.Format(CultureInfo.InvariantCulture, "{0:X3}{1}", tip & 0xfff, suffix));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.Tip.isWideTipDelta(int)</c>.</remarks>
    public static bool IsWideTipDelta(int tipDelta)
    {
        return (short)(tipDelta << 1) != (tipDelta << 1);
    }

}
