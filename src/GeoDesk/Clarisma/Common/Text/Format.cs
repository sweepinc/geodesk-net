/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;

namespace Clarisma.Common.Text;

/// <remarks>Ported from Java <c>com.clarisma.common.text.Format</c>.</remarks>
public static class Format
{

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Format.formatTimespan(long)</c>.</remarks>
    public static string FormatTimespan(long ms)
    {
        var c = CultureInfo.InvariantCulture;
        if (ms < 1000) return string.Format(c, "{0}ms", ms);
        if (ms < 60_000)
        {
            return string.Format(c, "{0}s {1}ms", ms / 1000, ms % 1000);
        }
        if (ms < 60 * 60 * 1000)
        {
            var s = (ms + 500) / 1000;
            return string.Format(c, "{0}m {1}s", s / 60, s % 60);
        }
        if (ms < 24 * 60 * 60 * 1000L)
        {
            var m = (ms + 30 * 1000) / (60 * 1000);
            return string.Format(c, "{0}h {1}m", m / 60, m % 60);
        }
        var h = (ms + 30 * 60 * 1000) / (60 * 60 * 1000);
        return string.Format(c, "{0}d {1}h", h / 24, h % 24);
    }

}
