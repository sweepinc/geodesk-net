/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature.Store;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

// PORT: Java declares this as TableIterator<Feature> (a type parameter that shadows the
// Feature type). It is currently unused scaffolding in the Java source; the concrete table
// iterators implement their own state. Kept here, mirroring its fields/constants, atop the
// FeatureIterator adapter base.
public abstract class TableIterator : FeatureIterator
{
    protected int tip = FeatureConstants.START_TIP;
    protected NioBuffer? foreignBuf;
    protected int pForeignTile;

    // TODO: consolidate these
    protected const int LAST_FLAG = 1;
    protected const int FOREIGN_FLAG = 2;
    protected const int DIFFERENT_TILE_FLAG = 4;
}
