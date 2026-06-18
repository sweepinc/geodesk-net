/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Text;

using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Tests.Common.Util;

public class FormatTest
{
    private readonly ITestOutputHelper output;

    public FormatTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void TestFormatTimespan()
    {
        output.WriteLine(Format.FormatTimespan(9));
        output.WriteLine(Format.FormatTimespan(35 * 60 * 1000 + 42_000));
        output.WriteLine(Format.FormatTimespan(4 * 60 * 60 * 1000 + 13 * 60 * 1000 + 42_000));
    }
}
