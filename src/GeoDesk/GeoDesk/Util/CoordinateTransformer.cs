/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using System.IO;
using GeoDesk.Geom;

namespace GeoDesk.Util;

// OSM standard precision is 7 digits (100 nano-degrees; 1cm); reasonable precision is 6 digits
// (10cm resolution)
// PORT: Java's Appendable sink is represented as a .NET TextWriter.
/// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer</c>.</remarks>
internal class CoordinateTransformer
{

    readonly double _scale;

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer(int)</c>.</remarks>
    public CoordinateTransformer(int precision)
    {
        _scale = Math.Pow(10, precision);
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.transformX(double)</c>.</remarks>
    public virtual double TransformX(double x)
    {
        return x;
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.transformY(double)</c>.</remarks>
    public virtual double TransformY(double y)
    {
        return y;
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.toString(double)</c>.</remarks>
    public string ToString(double v)
    {
        v = Math.Floor(v * _scale + 0.5) / _scale;
        var lv = (long)v;
        if (lv == v) return lv.ToString(CultureInfo.InvariantCulture);
        return v.ToString(CultureInfo.InvariantCulture);
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.writeX(Appendable, double)</c>.</remarks>
    public void WriteX(TextWriter outp, double x)
    {
        outp.Write(ToString(TransformX(x)));
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.writeY(Appendable, double)</c>.</remarks>
    public void WriteY(TextWriter outp, double y)
    {
        outp.Write(ToString(TransformY(y)));
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator</c>.</remarks>
    public class FromMercator : CoordinateTransformer
    {

        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator(int)</c>.</remarks>
        public FromMercator(int precision)
            : base(precision)
        {
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator.transformX(double)</c>.</remarks>
        public override double TransformX(double x)
        {
            return Mercator.LonFromX(x);
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator.transformY(double)</c>.</remarks>
        public override double TransformY(double y)
        {
            return Mercator.LatFromY(y);
        }

    }

    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator</c>.</remarks>
    public class ToMercator : CoordinateTransformer
    {

        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator()</c>.</remarks>
        public ToMercator()
            : base(7)
        {
            // TODO: technically, imps are always integer
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator.transformX(double)</c>.</remarks>
        public override double TransformX(double x)
        {
            return Mercator.XFromLon(x);
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator.transformY(double)</c>.</remarks>
        public override double TransformY(double y)
        {
            return Mercator.YFromLat(y);
        }

    }

}
