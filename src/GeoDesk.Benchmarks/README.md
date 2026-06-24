# GeoDesk.Benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) micro-benchmarks for GeoDesk-net. The benchmarks run
real queries against a GOL dataset, so the per-feature query path — tile walk, parallel tile scan,
R-tree descent, and the tag matcher — is exercised end-to-end.

## Running

Always run in **Release** (BenchmarkDotNet refuses to measure a Debug build):

```sh
dotnet run -c Release --project src/GeoDesk.Benchmarks -- --filter *
```

`--filter` selects benchmarks (BenchmarkDotNet syntax), e.g. `--filter *pubs-name-bbox-urban-s*` for one
spatial case or `--filter *WaysHighway*` for one of the simple queries. `--list flat` prints the available
benchmarks without running anything.

The suite is pinned to BenchmarkDotNet's **in-process** toolchain (see `Program.cs`): benchmarks run in
the host process rather than via BDN's default toolchain, which generates and builds a throwaway project
per benchmark. This keeps it a plain executable and avoids the per-benchmark rebuild re-triggering the GOL
fixture generation. As a consequence, **don't pass `--job`** (e.g. `--job dry`) — that adds a separate job
that falls back to the external, project-generating toolchain. To do a quick pass, narrow with `--filter`
(the light, rare-feature cases such as `*lighthouses-count-bbox-urban-xl*` finish in well under a minute).

## The GOL dataset

The benchmarks run against a country-scale dataset (Germany), mirroring upstream geodesk. The dataset
is resolved in this order:

1. The `GEODESK_GOL` environment variable, if set — an explicit path to any `.gol`.
2. Otherwise `germany.gol` in the build output's `Fixtures\` directory, generated **on demand** by the
   project's build (see the GOL fixture targets in `GeoDesk.Benchmarks.csproj`).

On-demand generation downloads the Geofabrik Germany extract (`germany-latest.osm.pbf`, ~4 GB) and runs
`gol build`. This requires the **GOL Tool 2.0** (`gol`, https://github.com/clarisma/geodesk-gol) on
`PATH` and takes substantial time, RAM, and disk. It happens once and is then cached in `obj\`, so the
first build is slow and subsequent builds reuse the result:

```sh
dotnet build src/GeoDesk.Benchmarks -c Release
```

To skip generation — e.g. you already have a `.gol`, or you're offline — disable it and point
`GEODESK_GOL` at an existing file:

```sh
GEODESK_GOL=/path/to/germany.gol \
  dotnet run -c Release --project src/GeoDesk.Benchmarks -p:GenerateGolFixtures=false -- --filter *
```

Any extract works via `GEODESK_GOL` (e.g. monaco for a quick smoke test), but the benchmark workload
(seed queries, box sizes, polygon admin levels) is tuned for Germany.

## What's in the suite

- **`SpatialQueryBenchmarks`** — the port of upstream geodesk's benchmark matrix. Each case (e.g.
  `pubs-name-bbox-urban-s`) selects a named query (`Plan.Queries`), restricts it by a spatial relation
  (bbox / intersects / within / enclosing) over a batch of pre-generated shapes, and applies an action
  (count / name / length / tags) to each result. The full case list lives in `Plan` (the data from
  upstream's `new-benchmark.fab`). One measured operation processes the whole batch; BenchmarkDotNet
  handles repetition and statistics.
- **`BridgesBenchmark`** — the standalone "bridges across the Danube in Bavaria" chained spatial query.
- **`QueryBenchmarks`** — a few simple whole-library queries (count nodes, ways with `highway`, etc.).

### Shapes

`ShapeMaker` generates the query shapes — random boxes/seed-points placed near features matching a seed
query, and polygons taken from admin-area features — and **caches them to disk** (under the output
`shapes\` directory, keyed by the dataset). The cache is what makes comparisons sound: two runs against
the same dataset query the identical shape set. Generation is deterministically seeded, so even the first
run (before the cache exists) is reproducible. Delete the `shapes\` directory to regenerate.

The shape **counts** (in `Plan.Boxes`/`Plan.Circles`) control spatial coverage and the memory regime, not
timing — BenchmarkDotNet owns timing. They're carried over from upstream but are just data; lower them for
a quicker pass.

## Adding benchmarks

Add a case name to `Plan` (and any new query/shape spec it needs), or add a `[Benchmark]` method to an
existing class, or a new class with `[MemoryDiagnoser]`; the `Program` switcher discovers every benchmark
class in the assembly. The project has `InternalsVisibleTo` access to GeoDesk (granted in `GeoDesk.csproj`),
so internal types such as `Matcher`/`MatcherOps`/`Mercator` can be micro-benchmarked directly.
