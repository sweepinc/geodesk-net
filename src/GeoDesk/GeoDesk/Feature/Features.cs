/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Geom;

namespace GeoDesk.Feature;

/// <summary>
/// A collection of features.
/// </summary>
//
// PORT NOTE (Phase 6): the many spatial/filter default methods from the Java interface
// (connectedTo, containing, coveredBy, crossing, disjoint, intersecting, maxMetersFrom*,
// overlapping, touching, within, containingXY/LonLat) and node(id)/way(id)/relation(id)
// construct concrete Filter objects from com.geodesk.feature.filter and are added when the
// filter package is ported. The abstract contract and iteration helpers live here.
public interface Features : IEnumerable<Feature>
{
    /// <summary>Returns a view containing only features matching the given query.</summary>
    Features Select(string query);

    /// <summary>Returns a view that contains only nodes.</summary>
    Features Nodes();

    /// <summary>Returns a view that contains only nodes matching the given query.</summary>
    Features Nodes(string query);

    /// <summary>Returns a view that contains only ways.</summary>
    Features Ways();

    /// <summary>Returns a view that contains only ways matching the given query.</summary>
    Features Ways(string query);

    /// <summary>Returns a view that contains only relations.</summary>
    Features Relations();

    /// <summary>Returns a view that contains only relations matching the given query.</summary>
    Features Relations(string query);

    /// <summary>Returns a sub-view of features that are nodes of the given way.</summary>
    Features NodesOf(Feature parent)
    {
        throw new System.InvalidOperationException("Not implemented for this query.");
    }

    /// <summary>
    /// Returns the features that are nodes of the given way, or members of the given relation.
    /// </summary>
    Features MembersOf(Feature parent)
    {
        throw new System.InvalidOperationException("Not implemented for this query.");
    }

    /// <summary>Returns the features that are parent elements of the given feature.</summary>
    Features ParentsOf(Feature child)
    {
        throw new System.InvalidOperationException("Not implemented for this query.");
    }

    /// <summary>Returns a view of features whose bounding box intersects the given bounds.</summary>
    Features In(Bounds bbox);

    /// <summary>Returns the first feature in the collection, or null if empty.</summary>
    Feature? First()
    {
        using IEnumerator<Feature> iter = GetEnumerator();
        return iter.MoveNext() ? iter.Current : null;
    }

    /// <summary>Returns the number of features in this collection.</summary>
    long Count()
    {
        long count = 0;
        using IEnumerator<Feature> iter = GetEnumerator();
        while (iter.MoveNext())
        {
            count++;
        }
        return count;
    }

    /// <summary>Returns true if this collection contains no features.</summary>
    bool IsEmpty()
    {
        return First() == null;
    }

    /// <summary>Creates a list containing all features in this collection.</summary>
    List<Feature> ToList()
    {
        List<Feature> list = new List<Feature>();
        foreach (Feature f in this) list.Add(f);
        return list;
    }

    /// <summary>Creates an array containing all features in this collection.</summary>
    Feature[] ToArray()
    {
        return ToList().ToArray();
    }

    /// <summary>Checks whether this collection contains the given object.</summary>
    bool Contains(object f)
    {
        using IEnumerator<Feature> iter = GetEnumerator();
        while (iter.MoveNext())
        {
            if (f.Equals(iter.Current)) return true;
        }
        return false;
    }

    /// <summary>Returns a view filtered by the given <see cref="Filter"/>.</summary>
    Features Select(Filter filter);

    /// <summary>Returns the features present in both this collection and <paramref name="other"/>.</summary>
    Features Select(Features other);

    /// <summary>Adds all features in this collection to the given collection.</summary>
    void AddTo(ICollection<Feature> collection)
    {
        foreach (Feature f in this) collection.Add(f);
    }

    /// <summary>Opens the given Geographic Object Library and returns all of its features.</summary>
    static Features Open(string path)
    {
        return new FeatureLibrary(path);
    }
}
