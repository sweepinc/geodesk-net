/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Feature;
using GeoDesk.Geom;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: ConcurTest is a Java main() program (not a JUnit test) that runs every snake_case, no-arg,
// long-returning method as a comprehensive API exercise. The snake_case method names are retained
// deliberately to mirror the Java source. The Java main() becomes the RunAll [Fact], which invokes
// each method directly.
/// <remarks>Ported from Java <c>com.geodesk.tests.ConcurTest</c>.</remarks>
public class ConcurTest : IDisposable
{

    readonly IFeatureQuery world;
    readonly FeatureLibrary _lib;

    public ConcurTest()
    {
        _lib = new FeatureLibrary(TestSettings.GolFile());
        world = _lib;
    }

    public void Dispose() => _lib.Close();

    long italian_restaurant_count()
    {
        return world.Select("na[amenity=restaurant][cuisine=italian]").Count();
    }

    long member_count()
    {
        long count = 0;
        foreach (var parent in world) count += parent.Members().Count();
        return count;
    }

    long member_iter_count()
    {
        long count = 0;
        foreach (var parent in world)
        {
            foreach (var child in parent.Members()) count += 1;
        }
        return count;
    }

    long parent_count()
    {
        long count = 0;
        foreach (var child in world) count += child.Parents().Count();
        return count;
    }

    long parent_iter_count()
    {
        long count = 0;
        foreach (var child in world)
        {
            foreach (var parent in child.Parents()) count += 1;
        }
        return count;
    }

    long parents_of_count()
    {
        long count = 0;
        foreach (var child in world) count += world.ParentsOf(child).Count();
        return count;
    }

    long parent_relations_count()
    {
        long count = 0;
        foreach (var child in world) count += child.Parents().Relations().Count();
        return count;
    }

    long parent_relations_of_count()
    {
        var relations = world.Relations();
        long count = 0;
        foreach (var child in world) count += relations.ParentsOf(child).Count();
        return count;
    }

    long parent_ways_count()
    {
        long count = 0;
        foreach (var child in world) count += child.Parents().Ways().Count();
        return count;
    }

    long parent_ways_of_count()
    {
        var ways = world.Ways();
        long count = 0;
        foreach (var child in world) count += ways.ParentsOf(child).Count();
        return count;
    }

    long waynode_parents_count()
    {
        long count = 0;
        foreach (var way in world.Ways())
        {
            foreach (var node in way.Nodes()) count += node.Parents().Count();
        }
        return count;
    }

    long waynode_parent_ways_count()
    {
        long count = 0;
        foreach (var way in world.Ways())
        {
            foreach (var node in way.Nodes()) count += node.Parents().Ways().Count();
        }
        return count;
    }

    long waynode_count()
    {
        long count = 0;
        foreach (var way in world.Ways()) count += way.Nodes().Count();
        return count;
    }

    long waynode_iter_count()
    {
        long count = 0;
        foreach (var way in world.Ways())
        {
            foreach (var node in way.Nodes()) count += 1;
        }
        return count;
    }

    long nonsense_parent_count()
    {
        long count = 0;
        foreach (var child in world) count += child.Parents().Nodes().Count();
        return count;
    }

    long nonsense_parents_of_count()
    {
        var nodes = world.Nodes();
        long count = 0;
        foreach (var child in world) count += nodes.ParentsOf(child).Count();
        return count;
    }

    long relation_member_role_len()
    {
        long len = 0;
        foreach (var parent in world.Relations())
        {
            foreach (var child in parent.Members()) len += child.Role!.Length;
        }
        return len;
    }

    long street_crossing_count()
    {
        var crossings = world.Select("n[highway=crossing]");
        long count = 0;
        foreach (var street in world.Select("w[highway]")) count += crossings.NodesOf(street).Count();
        return count;
    }

    long street_crossing_iter_count()
    {
        var crossings = world.Select("n[highway=crossing]");
        long count = 0;
        foreach (var street in world.Select("w[highway]"))
        {
            foreach (var node in crossings.NodesOf(street)) count += 1;
        }
        return count;
    }

    long street_crossing_in_count()
    {
        var crossings = world.Select("n[highway=crossing]");
        long count = 0;
        foreach (var street in world.Select("w[highway]"))
        {
            foreach (var node in street.Nodes())
            {
                if (crossings.Contains(node)) count += 1;
            }
        }
        return count;
    }

    long tags_count()
    {
        long count = 0;
        foreach (var f in world) count += f.Tags.Count;
        return count;
    }

    long tags_iter_count()
    {
        long count = 0;
        foreach (var f in world)
        {
            foreach (var _ in f.Tags) count++;
        }
        return count;
    }

    long tags_key_len()
    {
        long totalLen = 0;
        foreach (var f in world)
        {
            foreach (var tag in f.Tags) totalLen += tag.Key.Length;
        }
        return totalLen;
    }

    long tags_str_len()
    {
        long totalLen = 0;
        foreach (var f in world)
        {
            foreach (var tag in f.Tags) totalLen += tag.Value.Length;
        }
        return totalLen;
    }

    long tags_int_sum()
    {
        long sum = 0;
        foreach (var f in world)
        {
            foreach (var tag in f.Tags) sum += tag.LongValue;
        }
        return sum;
    }

    long xy_hash()
    {
        long hash = 0;
        foreach (var f in world)
        {
            hash ^= f.X;
            hash ^= f.Y;
        }
        return hash;
    }

    long lonlat_100nd_hash()
    {
        long hash = 0;
        foreach (var f in world)
        {
            hash ^= (long)(Mercator.LonPrecision7FromX(f.X) * 10000000);
            hash ^= (long)(Mercator.LatPrecision7FromY(f.Y) * 10000000);
        }
        return hash;
    }

    long waynodes_lonlat_100nd_hash()
    {
        long hash = 0;
        foreach (var way in world.Ways())
        {
            foreach (var node in way.Nodes())
            {
                hash ^= (long)(Mercator.LonPrecision7FromX(node.X) * 10000000);
                hash ^= (long)(Mercator.LatPrecision7FromY(node.Y) * 10000000);
            }
        }
        return hash;
    }

    long id_hash()
    {
        long hash = 0;
        foreach (var f in world) hash ^= f.Id;
        return hash;
    }

    static void Run(string name, Func<long> test)
    {
        Console.WriteLine(name + "=" + test());
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ConcurTest.main(String[])</c>.</remarks>
    [Fact]
    public void RunAll()
    {
        Run(nameof(italian_restaurant_count), italian_restaurant_count);
        Run(nameof(member_count), member_count);
        Run(nameof(member_iter_count), member_iter_count);
        Run(nameof(parent_count), parent_count);
        Run(nameof(parent_iter_count), parent_iter_count);
        Run(nameof(parents_of_count), parents_of_count);
        Run(nameof(parent_relations_count), parent_relations_count);
        Run(nameof(parent_relations_of_count), parent_relations_of_count);
        Run(nameof(parent_ways_count), parent_ways_count);
        Run(nameof(parent_ways_of_count), parent_ways_of_count);
        Run(nameof(waynode_parents_count), waynode_parents_count);
        Run(nameof(waynode_parent_ways_count), waynode_parent_ways_count);
        Run(nameof(waynode_count), waynode_count);
        Run(nameof(waynode_iter_count), waynode_iter_count);
        Run(nameof(nonsense_parent_count), nonsense_parent_count);
        Run(nameof(nonsense_parents_of_count), nonsense_parents_of_count);
        Run(nameof(relation_member_role_len), relation_member_role_len);
        Run(nameof(street_crossing_count), street_crossing_count);
        Run(nameof(street_crossing_iter_count), street_crossing_iter_count);
        Run(nameof(street_crossing_in_count), street_crossing_in_count);
        Run(nameof(tags_count), tags_count);
        Run(nameof(tags_iter_count), tags_iter_count);
        Run(nameof(tags_key_len), tags_key_len);
        Run(nameof(tags_str_len), tags_str_len);
        Run(nameof(tags_int_sum), tags_int_sum);
        Run(nameof(xy_hash), xy_hash);
        Run(nameof(lonlat_100nd_hash), lonlat_100nd_hash);
        Run(nameof(waynodes_lonlat_100nd_hash), waynodes_lonlat_100nd_hash);
        Run(nameof(id_hash), id_hash);
    }

}
