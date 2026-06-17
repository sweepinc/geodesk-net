/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Query;

public class EmptyView : View
{
    public static readonly Features ANY = new EmptyView();

    public EmptyView()
        : base(null!, 0, null!, null)
    {
    }

    protected override Features Select(int newTypes, string query)
    {
        return this;
    }

    protected override Features NewWith(int types, Matcher matcher, Filter? filter)
    {
        return this;
    }

    public bool IsEmpty()
    {
        return true;
    }

    public long Count()
    {
        return 0;
    }

    public bool Contains(object f)
    {
        return false;
    }

    public override Features Select(string filter)
    {
        return ANY;
    }

    public override Features In(Bounds bbox)
    {
        return this;
    }

    public override Features Select(Filter filter) { return this; }

    public override Features NodesOf(Feature parent)
    {
        return this;
    }

    public override Features MembersOf(Feature parent)
    {
        return this;
    }

    public override Features ParentsOf(Feature child)
    {
        return this;
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        return Enumerable.Empty<Feature>().GetEnumerator();
    }
}
