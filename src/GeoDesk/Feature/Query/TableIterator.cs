/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature.Store;
using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

// PORT: Java declares this as TableIterator<Feature> (a type parameter that shadows the Feature
// type). It is currently unused scaffolding in the Java source; the concrete table iterators
// implement their own state. Kept here, mirroring its fields/constants, atop the FeatureIterator
// adapter base.
/// <summary>
/// Abstract base scaffolding for iterators that walk a packed feature reference
/// table, holding the shared foreign-tile tracking state and flag constants. The
/// concrete table iterators currently implement their own state; this mirrors the
/// Java type atop the <see cref="FeatureIterator"/> adapter.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TableIterator</c>.</remarks>
internal abstract class TableIterator : FeatureIterator
{

    // TODO: consolidate these
    protected const int LastFlag = 1;
    protected const int ForeignFlag = 2;
    protected const int DifferentTileFlag = 4;

    protected int tip = FeatureConstants.START_TIP;
    protected NioBuffer foreignBuf;
    protected int pForeignTile;

}
