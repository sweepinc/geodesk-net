/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

/// <summary>
/// A <see cref="Box"/> that carries an associated item of type <typeparamref name="T"/>, letting a
/// spatial index store arbitrary payloads keyed by their bounding box.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.BoundedItem</c>.</remarks>
internal class BoundedItem<T> : Box
{

    readonly T _item;

    /// <summary>
    /// Creates a bounded item with the given bounding box and payload.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoundedItem(Bounds, T)</c>.</remarks>
    public BoundedItem(IBounds b, T item)
        : base(b)
    {
        _item = item;
    }

    /// <summary>
    /// Returns the payload item carried by this bounded item.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoundedItem.get()</c>.</remarks>
    public T Get()
    {
        return _item;
    }

}
