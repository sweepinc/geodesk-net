/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;

using GeoDesk.Common.Util;

using Xunit;

namespace GeoDesk.Feature.Query;

/// <summary>
/// Ported from Java <c>com.geodesk.feature.query.PatternMatcherTest</c>. The Java version was a
/// <c>main()</c> that printed results; here it asserts the substring search behaviour of
/// <see cref="Bytes.IndexOf(byte[], byte[])"/>.
/// </summary>
public class PatternMatcherTest
{

    static int Match(string candidate, string match)
    {
        var cBytes = Encoding.UTF8.GetBytes(candidate);
        var mBytes = Encoding.UTF8.GetBytes(match);
        return Bytes.IndexOf(cBytes, mBytes);
    }

    [Fact]
    public void FindsSubstrings()
    {
        Assert.True(Match("monkeykey", "keykey") >= 0);
        Assert.Equal(3, Match("monkey", "key"));
        Assert.True(Match("monkey", "money") < 0);
        Assert.True(Match("money", "key") < 0);
    }

}
