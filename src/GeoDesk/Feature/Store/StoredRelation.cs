/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using GeoDesk.Common.Store;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Polygons;
using GeoDesk.Feature.Query;

using NetTopologySuite.Geometries;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A relation feature read directly from a feature library tile. Enumerates and
/// queries its members, and builds the appropriate geometry (a multipolygon for area
/// relations, otherwise a geometry collection gathered recursively from members).
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation</c>.</remarks>
internal class StoredRelation : StoredFeature, IRelation
{

    /// <summary>
    /// Creates a stored relation backed by the given store, buffer, and pointer to the
    /// relation's record.
    /// </summary>
    /// <param name="store">the feature store the relation was read from</param>
    /// <param name="segment">the segment containing the relation's record</param>
    /// <param name="pFeature">the pointer to the relation's record</param>
    public StoredRelation(FeatureStore store, GeoDesk.Common.Store.Segment segment, int pFeature) :
        base(store, segment, pFeature)
    {

    }

    /// <summary>
    /// The feature type, always <see cref="FeatureType.Relation"/>.
    /// </summary>
    public override FeatureType Type => FeatureType.Relation;

    /// <summary>
    /// Always true; this feature is a relation.
    /// </summary>
    public bool IsRelation => true;

    /// <summary>
    /// Returns a debug string of the form <c>relation/{id}</c>.
    /// </summary>
    public override string ToString()
    {
        return "relation/" + Id;
    }

    /// <summary>
    /// Resolves the absolute pointer to a relation's member table from its record.
    /// </summary>
    public static int BodyPointer(NioBuffer buf, int pFeature)
    {
        int ppMembers = pFeature + 12;
        return ppMembers + buf.GetInt(ppMembers);
    }

    // TODO: Decide what this should return
    /// <summary>
    /// Returns an empty coordinate array; a relation has no inherent coordinate
    /// sequence.
    /// </summary>
    public override int[] ToXY()
    {
        return [];
    }

    /// <summary>
    /// Returns true if the member table at the given pointer is empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.isEmpty(int)</c>.</remarks>
    bool IsEmpty(int pMembers)
    {
        return buf.GetInt(pMembers) == 0;
    }

    /// <summary>
    /// Returns an iterator over all members of this relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        int ppMembers = pFeature + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers))
            return Enumerable.Empty<IFeature>().GetEnumerator();
        return new MemberIterator(store, segment, pMembers, TypeBits.ALL, Matcher.ALL, null);
    }

    /// <summary>
    /// Returns an iterator over the members of this relation that match the given types
    /// and matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.iterator(int, Matcher)</c>.</remarks>
    public IEnumerator<IFeature> GetEnumerator(int types, Matcher matcher)
    {
        int ppMembers = pFeature + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers))
            return Enumerable.Empty<IFeature>().GetEnumerator();
        return new MemberIterator(store, segment, pMembers, types, matcher, null);
    }

    /// <summary>
    /// Builds the geometry of this relation: a polygon/multipolygon for area relations,
    /// otherwise a geometry collection assembled from its members.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.toGeometry()</c>.</remarks>
    public override Geometry ToGeometry()
    {
        if (IsArea)
            return PolygonBuilder.Build(store.GeometryFactory(), this);
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

    /// <summary>
    /// Returns a query over all members of this relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members()</c>.</remarks>
    public IFeatureQuery Members()
    {
        return Members(TypeBits.ALL, Matcher.ALL, null);
    }

    /// <summary>
    /// Returns a query over the members of this relation of the given types that match
    /// the given GOQL query string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members(int, String)</c>.</remarks>
    IFeatureQuery Members(int types, string query)
    {
        Matcher matcher = store.GetMatcher(query);
        return Members(types & matcher.AcceptedTypes, matcher, null);
    }

    /// <summary>
    /// Returns a query over the members of this relation constrained by the given types,
    /// matcher, and optional filter; an empty view when the relation has no members.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members(int, Matcher, Filter)</c>.</remarks>
    public IFeatureQuery Members(int types, Matcher matcher, IFilter? filter)
    {
        int ppMembers = pFeature + 12;
        int pMembers = ppMembers + buf.GetInt(ppMembers);
        if (IsEmpty(pMembers))
            return EmptyView.Any;
        return new MemberView(store, segment, pMembers, types, matcher, filter);
    }

    /// <summary>
    /// Returns a query over the members of this relation that match the given GOQL
    /// query string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredRelation.members(String)</c>.</remarks>
    public IFeatureQuery Members(string q)
    {
        return Members(TypeBits.ALL, q);
    }
}
