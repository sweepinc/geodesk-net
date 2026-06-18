/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Common.Parser;
using GeoDesk.Common.Util;

using Xunit;

namespace GeoDesk.Tests.Common.Parser;

public class SimpleParserTest
{
    public class SchemaMaker
    {
        public long lower;
        public long upper;

        public void Add(char ch)
        {
            Assert.True(ch > ' ' && ch < 128);
            if (ch < 64)
            {
                lower |= 1L << ch;
            }
            else
            {
                upper |= 1L << ch;
            }
        }

        public void AddRange(char chStart, char chEnd)
        {
            Assert.True(chEnd > chStart);
            for (char ch = chStart; ch <= chEnd; ch++) Add(ch);
        }

        public void Print()
        {
            Log.Debug("Lower = %s", Convert.ToString(lower, 2));
            Log.Debug("Upper = %s", Convert.ToString(upper, 2));
        }
    }

    [Fact]
    public void PrepareSchema()
    {
        SchemaMaker schema = new SchemaMaker();
        schema.Add('_');
        schema.Add(':');
        schema.AddRange('0', '9');
        schema.AddRange('a', 'z');
        schema.AddRange('A', 'Z');
        schema.Print();
    }

    [Fact]
    public void TestParser()
    {
        SimpleParser parser = new SimpleParser("  this = 21.7352672112  -.33333333");
        Assert.True(parser.Literal("this"));
        Assert.True(parser.Literal('='));
        Assert.Equal(21.7352672112d, parser.Number(), 11);
        Assert.Equal(-.33333333d, parser.Number(), 11);
    }
}
