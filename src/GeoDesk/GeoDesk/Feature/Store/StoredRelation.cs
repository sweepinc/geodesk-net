/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using NetTopologySuite.Geometries;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Store;

public class StoredRelation : StoredFeature, Relation
{
    public StoredRelation(FeatureStore store, NioBuffer buf, int ptr)
        : base(store, buf, ptr)
    {
    }

    public override FeatureType Type() => FeatureType.Relation;

    public bool IsRelation() => true;

    public override string ToString()
    {
        return "relation/" + Id();
    }

    public static int BodyPointer(NioBuffer buf, int ptr)
    {
        int ppMembers = ptr + 12;
        return ppMembers + buf.GetInt(ppMembers);
    }

    // TODO: Decide what this should return
    public override int[] ToXY()
    {
        return new int[0];
    }

    private bool IsEmpty(int pMembers)
    {
        return buf.GetInt(pMembers) == 0;
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        int ppMembers = ptr + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers)) return Enumerable.Empty<Feature>().GetEnumerator();
        return new MemberIterator(store, buf, pMembers, TypeBits.ALL, Matcher.ALL, null);
    }

    public IEnumerator<Feature> GetEnumerator(int types, Matcher matcher)
    {
        int ppMembers = ptr + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers)) return Enumerable.Empty<Feature>().GetEnumerator();
        return new MemberIterator(store, buf, pMembers, types, matcher, null);
    }

    // PORT (polygon cluster): area geometry requires PolygonBuilder (feature/polygon),
    // which is not yet ported; non-area relations require member geometry collection.
    public override Geometry ToGeometry()
    {
        throw new NotImplementedException("PORT: StoredRelation.ToGeometry requires PolygonBuilder / member geometry.");
    }

    public Features Members()
    {
        return Members(TypeBits.ALL, Matcher.ALL, null);
    }

    private Features Members(int types, string query)
    {
        Matcher matcher = store.GetMatcher(query);
        return Members(types & matcher.AcceptedTypes(), matcher, null);
    }

    public Features Members(int types, Matcher matcher, Filter? filter)
    {
        int ppMembers = ptr + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers)) return EmptyView.ANY;
        return new MemberView(store, buf, pMembers, types, matcher, filter);
    }

    public Features Members(string q)
    {
        return Members(TypeBits.ALL, q);
    }
}
