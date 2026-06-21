/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Pbf;

/// <summary>
/// Wire-type constants for the Protocol Buffers (PBF) encoding: the low three bits of a field tag
/// identify how the following value is encoded (variable-length integer, fixed 64- or 32-bit, or
/// length-delimited string/bytes).
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfType</c>.</remarks>
internal static class PbfType
{

    public const int Varint = 0;
    public const int Fixed64 = 1;
    public const int String = 2;
    public const int Fixed32 = 5;

}
