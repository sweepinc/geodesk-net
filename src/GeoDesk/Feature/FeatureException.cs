/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Feature;

/// <summary>
/// Thrown when an error occurs while reading or interpreting a feature from a GeoDesk feature
/// library, such as malformed or unexpected data in the underlying store.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.FeatureException</c>.</remarks>
public class FeatureException : Exception
{

    /// <summary>
    /// Creates a new <see cref="FeatureException"/> with the given human-readable error message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureException(String)</c>.</remarks>
    public FeatureException(string msg)
        : base(msg)
    {

    }

    /// <summary>
    /// Creates a new <see cref="FeatureException"/> with the given error message and an underlying
    /// cause.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureException(String, Throwable)</c>.</remarks>
    public FeatureException(string msg, Exception ex)
        : base(msg, ex)
    {

    }

}
