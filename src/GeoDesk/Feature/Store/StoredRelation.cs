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
using GeoDesk.Feature.Polygons;
using GeoDesk.Feature.Query;
using NetTopologySuite.Geometries;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Store;

internal class StoredRelation : StoredFeature, IRelation
{
    public StoredRelation(FeatureStore store, NioBuffer buf, int ptr)
        : base(store, buf, ptr)
    {
    }

    public override FeatureType Type => FeatureType.Relation;

    public bool IsRelation => true;

    public override string ToString()
    {
        return "relation/" + Id;
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

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.isEmpty(int)</c>.</remarks>
    bool IsEmpty(int pMembers)
    {
        return buf.GetInt(pMembers) == 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        int ppMembers = ptr + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers)) return Enumerable.Empty<IFeature>().GetEnumerator();
        return new MemberIterator(store, buf, pMembers, TypeBits.ALL, Matcher.ALL, null);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.iterator(int, Matcher)</c>.</remarks>
    public IEnumerator<IFeature> GetEnumerator(int types, Matcher matcher)
    {
        int ppMembers = ptr + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers)) return Enumerable.Empty<IFeature>().GetEnumerator();
        return new MemberIterator(store, buf, pMembers, types, matcher, null);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.toGeometry()</c>.</remarks>
    public override Geometry ToGeometry()
    {
        if (IsArea) return PolygonBuilder.Build(store.GeometryFactory(), this);
        return ToGeometryCollection();
    }

    /// <summary>
    /// Recursively gathers the geometries of the relation's members.
    /// </summary>
    /// <param name="geoms">list where to add the member geometries</param>
    /// <param name="processedRelations">
    /// set of relations (IDs) we've already processed (used to guard against circular refs)
    /// </param>
    /// <param name="commonType">the common geometry type discovered so far (or null)</param>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.gatherGeometries(List, MutableLongSet, Class)</c>.</remarks>
    Type? GatherGeometries(List<Geometry> geoms, HashSet<long> processedRelations, Type? commonType)
    {
        processedRelations.Add(Id);

        foreach (var member in this)
        {
            if (member is StoredRelation memberRel && !memberRel.IsArea)
            {
                // Gather geometries from sub-relations that aren't areas
                if (!processedRelations.Contains(memberRel.Id))
                {
                    // avoid endless recursion in case relations are in a reference cycle
                    commonType = memberRel.GatherGeometries(geoms, processedRelations, commonType);
                }
            }
            else
            {
                // Add points, lines, (multi)polygons
                var g = member.ToGeometry();
                var geomType = g.GetType();
                if (geomType != commonType)
                {
                    commonType = (commonType == null) ? geomType : typeof(Geometry);
                    // TODO: This won't work if spec changed so Way returns LinearRing as well as
                    //  LineString (but for now, it always returns LineString) See Issue #58
                }
                geoms.Add(g);
            }
        }
        return commonType;
    }

    /// <summary>
    /// Creates a GeometryCollection (used to represent non-area relations).
    /// </summary>
    /// <returns>a GeometryCollection</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.toGeometryCollection()</c>.</remarks>
    Geometry ToGeometryCollection()
    {
        var geoms = new List<Geometry>();
        var commonType = GatherGeometries(geoms, new HashSet<long>(), null);
        var factory = store.GeometryFactory();
        if (commonType == typeof(LineString))
            return factory.CreateMultiLineString(geoms.Cast<LineString>().ToArray());
        if (commonType == typeof(Point))
            return factory.CreateMultiPoint(geoms.Cast<Point>().ToArray());

        // TODO: should a collection of polygons be treated as a MultiPolygon, even though it is not
        //  a relation with type=multipolygon ?

        return store.GeometryFactory().CreateGeometryCollection(geoms.ToArray());
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members()</c>.</remarks>
    public IFeatures Members()
    {
        return Members(TypeBits.ALL, Matcher.ALL, null);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members(int, String)</c>.</remarks>
    IFeatures Members(int types, string query)
    {
        Matcher matcher = store.GetMatcher(query);
        return Members(types & matcher.AcceptedTypes(), matcher, null);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members(int, Matcher, Filter)</c>.</remarks>
    public IFeatures Members(int types, Matcher matcher, IFilter? filter)
    {
        int ppMembers = ptr + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers)) return EmptyView.Any;
        return new MemberView(store, buf, pMembers, types, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members(String)</c>.</remarks>
    public IFeatures Members(string q)
    {
        return Members(TypeBits.ALL, q);
    }
}
