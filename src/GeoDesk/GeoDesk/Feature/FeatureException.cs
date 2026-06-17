/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Feature;

public class FeatureException : Exception
{
    public FeatureException(string msg)
        : base(msg)
    {
    }

    public FeatureException(string msg, Exception ex)
        : base(msg, ex)
    {
    }
}
