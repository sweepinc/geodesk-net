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
public static class Tip
{
    public static string ToString(int tip)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:X6}", tip);
    }

    public static string Folder(string rootPath, int tip)
    {
        return Path.Combine(rootPath, string.Format(CultureInfo.InvariantCulture, "{0:X3}", (int)((uint)tip >> 12)));
    }

    public static string PathOf(string root, int tip, string suffix)
    {
        return Path.Combine(Folder(root, tip),
            string.Format(CultureInfo.InvariantCulture, "{0:X3}{1}", tip & 0xfff, suffix));
    }

    public static bool IsWideTipDelta(int tipDelta)
    {
        return (short)(tipDelta << 1) != (tipDelta << 1);
    }
}
