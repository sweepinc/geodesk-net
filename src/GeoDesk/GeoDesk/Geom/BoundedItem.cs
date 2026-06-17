/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

public class BoundedItem<T> : Box
{
    private readonly T item;

    public BoundedItem(Bounds b, T item)
        : base(b)
    {
        this.item = item;
    }

    public T Get()
    {
        return item;
    }
}
