/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

/// <remarks>Ported from Java <c>com.geodesk.geom.BoundedItem</c>.</remarks>
internal class BoundedItem<T> : Box
{

    readonly T _item;

    /// <remarks>Ported from Java <c>com.geodesk.geom.BoundedItem(Bounds, T)</c>.</remarks>
    public BoundedItem(IBounds b, T item)
        : base(b)
    {
        _item = item;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.BoundedItem.get()</c>.</remarks>
    public T Get()
    {
        return _item;
    }

}
