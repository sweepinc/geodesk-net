# GeoDesk for .NET

[![NuGet](https://img.shields.io/nuget/v/GeoDesk.svg?logo=nuget)](https://www.nuget.org/packages/GeoDesk)

GeoDesk is a fast and storage-efficient geospatial database for OpenStreetMap data. This package is a
C# port of the [GeoDesk Java library](https://github.com/clarisma/geodesk), bringing the same query
model to .NET. (GeoDesk is also available [for Java](https://github.com/clarisma/geodesk),
[for C++](https://github.com/clarisma/libgeodesk) and [for Python](https://github.com/clarisma/geodesk-py).)

You query a **GOL** (Geographic Object Library) file — a compact, pre-indexed snapshot of OSM data —
and get back `Node`, `Way` and `Relation` features with their tags and geometry.

> **GOL files are read, not built, by this library.** Build a `.gol` from an `.osm.pbf` extract with
> [GOL Tool 2.0](https://github.com/clarisma/geodesk-gol), then open it here. This package targets the
> GOL 2.0 format.

## Why GeoDesk?

- **Small storage footprint** — GOL files are only 20% to 50% larger than the original OSM data in
  PBF format — less than a tenth of what a traditional SQL-based database consumes.

- **Fast queries** — typically 50 times faster than SQL.

- **Intuitive API** — no object-relational mapping; queries return `INode`, `IWay` and `IRelation`
  objects. Discover tags, way-nodes and relation members; get a feature's geometry; measure its
  length or area.

- **Proper handling of relations** — traditional geospatial databases deal in geometric shapes and
  need workarounds for this unique and powerful aspect of OSM data.

- **Seamless integration with [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite) (NTS)**
  — the .NET port of JTS — for advanced geometric operations: buffer, union, simplify, convex and
  concave hulls, Voronoi diagrams, and more.

- **Modest hardware requirements** — runs anywhere .NET runs: Windows, Linux and macOS.

> **Status:** this is a faithful in-progress port. The full **read + query** path is implemented and
> validated against real GOL data. Writing/building GOLs is handled by the external GOL Tool, not this
> library.

## Get Started

Install from NuGet:

```sh
dotnet add package GeoDesk
```

…or add a `PackageReference` to your project (the package targets **.NET 10**):

```xml
<PackageReference Include="GeoDesk" Version="1.0.0" />
```

To build the latest version from source instead:

```sh
git clone https://github.com/alethic/geodesk-net.git
cd geodesk-net
dotnet build
```

### Example Application

```csharp
using GeoDesk.Feature;
using GeoDesk.Geom;

using FeatureLibrary library = FeatureLibrary.Open("switzerland.gol");   // 1

foreach (IFeature pub in library                                         // 2
    .Select("na[amenity=pub]")                                           // 3
    .In(Box.OfWSEN(8.53, 47.36, 8.55, 47.38)))                           // 4
{
    Console.WriteLine(pub.StringValue("name"));                          // 5
}                                                                        // 6
```

What's going on here?

1. We open a feature library (`switzerland.gol`). `FeatureLibrary` is `IDisposable`, so `using`
   closes it for us.
2. We iterate through all the features ...
3. ... that are pubs ([GeoDesk query language](https://docs.geodesk.com/goql) — *similar to MapCSS*).
4. ... in downtown Zurich (*a bounding box with West/South/East/North coordinates, in degrees*).
5. We print the name of each pub.
6. The `using` block disposes the library when we're done.

That's it — your first GeoDesk application!

### More Examples

Find all movie theaters within 500 meters of a given point:

```csharp
IFeatures movieTheaters = library
    .Select("na[amenity=cinema]")
    .MaxMetersFromLonLat(500, myLon, myLat);
```

*Remember, OSM uses British English for its terminology.*

Discover the bus routes that traverse a given street:

```csharp
foreach (IFeature route in street.Parents("[route=bus]"))
{
    Console.WriteLine($"- {route.StringValue("ref")} " +
        $"from {route.StringValue("from")} to {route.StringValue("to")}");
}
```

Count the number of entrances of a building:

```csharp
long numberOfEntrances = building.Nodes("[entrance]").Count();
```

## Differences from the Java edition

The API mirrors the Java library closely; the main adaptations are idiomatic .NET:

| Java | .NET |
| --- | --- |
| `Features.open(path)` | `FeatureLibrary.Open(path)` |
| `library.close()` | `using` / `IDisposable` |
| `feature.stringValue("name")` | `feature.StringValue("name")` (PascalCase throughout) |
| `Feature`, `Features` | `IFeature`, `IFeatures` (interfaces) |
| Java Topology Suite (JTS) | NetTopologySuite (NTS) — `Geometry`, `GeometryFactory`, … |
| Coordinates as `int` "imps" | identical (integer Web-Mercator projection) |

The [GeoDesk query language (GOQL)](https://docs.geodesk.com/goql) is identical.

## Documentation

The Java API is mirrored closely, so the official GeoDesk documentation applies (translate method
names to PascalCase):

- [GeoDesk Developer's Guide](https://docs.geodesk.com)
- [GeoDesk Query Language (GOQL)](https://docs.geodesk.com/goql)

## Related Repositories

- [geodesk-gol](https://github.com/clarisma/geodesk-gol) — command-line tool for building and querying GOL files
- [geodesk](https://github.com/clarisma/geodesk) — GeoDesk for Java (the upstream this port mirrors)
- [libgeodesk](https://github.com/clarisma/libgeodesk) — GeoDesk for C++
- [geodesk-py](https://github.com/clarisma/geodesk-py) — GeoDesk for Python

## License

Licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0). GeoDesk is a
project of [Clarisma](https://github.com/clarisma); this .NET port is maintained separately.
