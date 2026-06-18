/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Geom;

using Xunit;

namespace GeoDesk.Tests.Geom;

public class TileTest
{

    [Fact]
    public void TestFromString()
    {
        Assert.Equal(0, Tile.FromString("0/0/0"));
        Assert.Equal(0x3007_006, Tile.FromString("3/6/7"));
        Assert.Equal(-1, Tile.FromString("12/4367/0"));
        Assert.Equal(-1, Tile.FromString("3/97/-4"));
        Assert.Equal(-1, Tile.FromString("not a valid tile"));
    }

    [Fact]
    public void TestFromXYZ()
    {
        Assert.Equal(Tile.FromString("12/0/0"), Tile.FromXYZ(int.MinValue, int.MaxValue, 12));
        Assert.Equal(Tile.FromString("0/0/0"), Tile.FromXYZ(int.MinValue, int.MaxValue, 0));
        Assert.Equal(Tile.FromString("0/0/0"), Tile.FromXYZ(int.MaxValue, int.MinValue, 0));
        Assert.Equal(Tile.FromString("0/0/0"), Tile.FromXYZ(0, 0, 0));
    }

    [Fact]
    public void TestBounds()
    {
        Assert.Equal(-2147483648, Tile.LeftX(Tile.FromString("0/0/0")));
        Assert.Equal(-1073741824, Tile.LeftX(Tile.FromString("3/2/0")));
        Assert.Equal(-1073741824, Tile.LeftX(Tile.FromString("3/2/1")));
        Assert.Equal(-1073741824, Tile.LeftX(Tile.FromString("3/2/4")));
        Assert.Equal(-787480576, Tile.LeftX(Tile.FromString("12/1297/1162")));
        Assert.Equal(1099956224, Tile.LeftX(Tile.FromString("12/3097/4000")));
        Assert.Equal(-1342177280, Tile.LeftX(Tile.FromString("4/3/15")));
        Assert.Equal(-2013265920, Tile.LeftX(Tile.FromString("6/2/44")));

        Assert.Equal(2147483647, Tile.TopY(Tile.FromString("0/0/0")));
        Assert.Equal(-1, Tile.TopY(Tile.FromString("1/0/1")));
        Assert.Equal(2147483647, Tile.TopY(Tile.FromString("3/2/0")));
        Assert.Equal(1610612735, Tile.TopY(Tile.FromString("3/2/1")));
        Assert.Equal(-1, Tile.TopY(Tile.FromString("3/2/4")));
        Assert.Equal(929038335, Tile.TopY(Tile.FromString("12/1297/1162")));
        Assert.Equal(-2046820353, Tile.TopY(Tile.FromString("12/3097/4000")));
        Assert.Equal(-1879048193, Tile.TopY(Tile.FromString("4/3/15")));
        Assert.Equal(-805306369, Tile.TopY(Tile.FromString("6/2/44")));

        Assert.Equal("12/1297/1162", Tile.ToString(Tile.FromXYZ(-787480576, 929038335, 12)));
        Assert.Equal(Tile.FromString("4/3/15"), Tile.FromXYZ(-1342177280, -1879048193, 4));

        Assert.Equal(-2147483648, Tile.BottomY(Tile.FromString("0/0/0")));
        Assert.Equal(-2147483648, Tile.BottomY(Tile.FromString("1/0/1")));
        Assert.Equal(0, Tile.BottomY(Tile.FromString("1/0/0")));
        Assert.Equal(1610612736, Tile.BottomY(Tile.FromString("3/2/0")));
        Assert.Equal(1073741824, Tile.BottomY(Tile.FromString("3/2/1")));
        Assert.Equal(-536870912, Tile.BottomY(Tile.FromString("3/2/4")));
        Assert.Equal(927989760, Tile.BottomY(Tile.FromString("12/1297/1162")));
        Assert.Equal(-2047868928, Tile.BottomY(Tile.FromString("12/3097/4000")));
        Assert.Equal(-2147483648, Tile.BottomY(Tile.FromString("4/3/15")));
        Assert.Equal(-872415232, Tile.BottomY(Tile.FromString("6/2/44")));

        Assert.Equal(1297, Tile.ColumnFromXZ(-787480576, 12));
        Assert.Equal(4095, Tile.ColumnFromXZ(0x7fff_ffff, 12));
        Assert.Equal(0, Tile.ColumnFromXZ(unchecked((int)0x8000_0000), 12));
        Assert.Equal(1162, Tile.RowFromYZ(927989760, 12));
        Assert.Equal(1162, Tile.RowFromYZ(929038335, 12));
        Assert.Equal(4095, Tile.RowFromYZ(unchecked((int)0x8000_0000), 12));
        Assert.Equal(0, Tile.RowFromYZ(0x7fff_ffff, 12));
        Assert.Equal(3, Tile.ColumnFromXZ(-1342177280, 4));
        Assert.Equal(15, Tile.RowFromYZ(-2147483648, 4));
        Assert.Equal(15, Tile.RowFromYZ(-1879048193, 4));
        Assert.Equal(15, Tile.ColumnFromXZ(0x7fff_ffff, 4));
        Assert.Equal(15, Tile.RowFromYZ(unchecked((int)0x8000_0000), 4));
        Assert.Equal(0, Tile.RowFromYZ(0x7fff_ffff, 4));

        Assert.Equal(0, Tile.ColumnFromXZ(unchecked((int)0x8000_0000), 4));
        Assert.Equal(0, Tile.ColumnFromXZ(0, 0));
        Assert.Equal(0, Tile.ColumnFromXZ(int.MinValue, 0));
        Assert.Equal(0, Tile.ColumnFromXZ(int.MaxValue, 0));
        Assert.Equal(0, Tile.RowFromYZ(0, 0));
        Assert.Equal(0, Tile.RowFromYZ(int.MinValue, 0));
        Assert.Equal(0, Tile.RowFromYZ(int.MaxValue, 0));
        Assert.Equal(1, Tile.ColumnFromXZ(0, 1));
        Assert.Equal(0, Tile.ColumnFromXZ(int.MinValue, 1));
        Assert.Equal(1, Tile.ColumnFromXZ(int.MaxValue, 1));
        Assert.Equal(0, Tile.RowFromYZ(0, 1));
        Assert.Equal(1, Tile.RowFromYZ(-1, 1));
        Assert.Equal(1, Tile.RowFromYZ(int.MinValue, 1));
        Assert.Equal(0, Tile.RowFromYZ(int.MaxValue, 1));
    }

    [Fact]
    public void TestMisc()
    {
        Assert.Equal(0, Tile.Row(Tile.FromString("0/0/0")));
        Assert.Equal(0, Tile.Column(Tile.FromString("0/0/0")));
        Assert.Equal(0, Tile.Zoom(Tile.FromString("0/0/0")));

        Assert.Equal(int.MinValue, Tile.LeftX(Tile.FromString("12/0/0")));
        Assert.Equal(int.MaxValue, Tile.TopY(Tile.FromString("12/0/0")));
        Assert.Equal(int.MaxValue, Tile.TopY(Tile.FromString("12/3567/0")));
        Assert.Equal(2146435072, Tile.BottomY(Tile.FromString("12/4031/0")));
        Assert.Equal(int.MaxValue, Tile.TopY(0));
        Assert.Equal(int.MinValue, Tile.BottomY(0));
    }

}
