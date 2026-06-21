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
/// <summary>
/// Transforms and formats coordinate ordinates for textual output, rounding to a
/// fixed decimal precision. The base class applies an identity transform; nested
/// subclasses convert between Web Mercator and longitude/latitude.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer</c>.</remarks>
internal class CoordinateTransformer
{

    readonly double _scale;

    /// <summary>
    /// Creates a transformer that rounds formatted output to the given number of
    /// decimal digits.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer(int)</c>.</remarks>
    public CoordinateTransformer(int precision)
    {
        _scale = Math.Pow(10, precision);
    }

    /// <summary>
    /// Transforms an X ordinate; the base implementation returns it unchanged.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.transformX(double)</c>.</remarks>
    public virtual double TransformX(double x)
    {
        return x;
    }

    /// <summary>
    /// Transforms a Y ordinate; the base implementation returns it unchanged.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.transformY(double)</c>.</remarks>
    public virtual double TransformY(double y)
    {
        return y;
    }

    /// <summary>
    /// Rounds the value to the configured precision and formats it, emitting an
    /// integer string when the rounded value has no fractional part.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.toString(double)</c>.</remarks>
    public string ToString(double v)
    {
        v = Math.Floor(v * _scale + 0.5) / _scale;
        var lv = (long)v;
        if (lv == v)
            return lv.ToString(CultureInfo.InvariantCulture);
        return v.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Transforms and writes an X ordinate to the given text writer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.writeX(Appendable, double)</c>.</remarks>
    public void WriteX(TextWriter outp, double x)
    {
        outp.Write(ToString(TransformX(x)));
    }

    /// <summary>
    /// Transforms and writes a Y ordinate to the given text writer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.writeY(Appendable, double)</c>.</remarks>
    public void WriteY(TextWriter outp, double y)
    {
        outp.Write(ToString(TransformY(y)));
    }

    /// <summary>
    /// A transformer that converts Web Mercator coordinates to longitude/latitude
    /// degrees as it writes them.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator</c>.</remarks>
    public class FromMercator : CoordinateTransformer
    {

        /// <summary>
        /// Creates a Mercator-to-lon/lat transformer rounding output to the given
        /// number of decimal digits.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator(int)</c>.</remarks>
        public FromMercator(int precision)
            : base(precision)
        {
        }

        /// <summary>
        /// Converts a Mercator X coordinate to longitude in degrees.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator.transformX(double)</c>.</remarks>
        public override double TransformX(double x)
        {
            return Mercator.LonFromX(x);
        }

        /// <summary>
        /// Converts a Mercator Y coordinate to latitude in degrees.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.FromMercator.transformY(double)</c>.</remarks>
        public override double TransformY(double y)
        {
            return Mercator.LatFromY(y);
        }

    }

    /// <summary>
    /// A transformer that converts longitude/latitude degrees to Web Mercator
    /// coordinates as it writes them.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator</c>.</remarks>
    public class ToMercator : CoordinateTransformer
    {

        /// <summary>
        /// Creates a lon/lat-to-Mercator transformer at the standard OSM precision.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator()</c>.</remarks>
        public ToMercator() : base(7)
        {
            // TODO: technically, imps are always integer
        }

        /// <summary>
        /// Converts a longitude in degrees to a Mercator X coordinate.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator.transformX(double)</c>.</remarks>
        public override double TransformX(double x)
        {
            return Mercator.XFromLon(x);
        }

        /// <summary>
        /// Converts a latitude in degrees to a Mercator Y coordinate.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.util.CoordinateTransformer.ToMercator.transformY(double)</c>.</remarks>
        public override double TransformY(double y)
        {
            return Mercator.YFromLat(y);
        }

    }

}
