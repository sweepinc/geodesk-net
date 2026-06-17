# GeoDesk Java → .NET Port Plan

A straight, faithful port of the GeoDesk Java library (`ext/geodesk`, v2.0.0) to
C# / .NET. **No .NET-specific optimizations in this pass.** Where a construct
cannot be cleanly expressed in .NET without third-party libraries or IL/Expression
emission, the member is stubbed and annotated rather than reimplemented.

- **Source:** `ext/geodesk/src/main/java` — 181 files, ~27,500 LOC.
- **Tests:** `ext/geodesk/src/test/java` — 24 JUnit 4 files, ~3,000 LOC.
- **Main code target:** `src/GeoDesk`
- **Test target:** `src/GeoDesk.Tests` (xUnit)

## Conventions

- **Packages → namespaces**, PascalCased: `com.geodesk.feature` → `GeoDesk.Feature`,
  `com.clarisma.common.store` → `Clarisma.Common.Store`. Folder layout mirrors namespaces.
  The `GeoDesk.Feature` namespace intentionally contains a `Feature` type (kept as-is for fidelity).
- **Zero-arg accessors → C# properties:** `id()`→`Id`, `x()`→`X`, `bounds()`→`Bounds`, `tags()`→`Tags`.
- **Methods with args / side effects stay methods:** `tag(key)`→`Tag(key)`, `select(q)`→`Select(q)`, `nodes()`→`Nodes()`.
- `Iterable<T>` → `IEnumerable<T>`; `iterator()` → `GetEnumerator()`.
- Java `interface` + `default` methods → C# **default interface methods** (DIM).
  - **Gotcha:** a DIM member is *not* visible on a variable typed as the concrete class — only via
    an interface-typed reference. Where the Java code calls such members directly on the concrete type
    (e.g. `box.contains(...)`, `box.area()`), the concrete class (`Box`) implements those members
    directly. **Do not "forward" them via `((Bounds)this).X`** — because the class method *is* the
    interface implementation, the cast re-dispatches to the same method and recurses infinitely
    (caused a StackOverflow during Phase 2 until implemented directly).
- Static factory methods on interfaces preserved as C# static interface methods (`Features.Open`).
- `assert` → `System.Diagnostics.Debug.Assert`.
- Nullable reference types enabled; Java `null` returns preserved.
- **Can't-map marker:** every stubbed member carries a `// PORT-BLOCKED: <reason>` comment and
  throws `NotImplementedException`, so it is greppable for a future pass.

## Dependency mapping

| Java dependency | .NET strategy |
|---|---|
| JTS `org.locationtech.jts` (41 files) | **NetTopologySuite** (NuGet) — 1:1 C# port of JTS |
| Eclipse Collections (15 files) | BCL generics (`Dictionary<int,int>`, `HashSet<int>`, …) — no boxing for value types |
| ASM `org.ow2.asm` (4 files) | ❌ **No clean mapping — stubbed** (runtime bytecode generation) |
| JUnit 4 | **xUnit** |
| `MappedByteBuffer`/`FileChannel` | `MemoryMappedFile` + `MemoryMappedViewAccessor` |
| `sun.misc.Unsafe` | `System.Runtime.CompilerServices.Unsafe` / `Marshal` / pointers |
| `ByteBuffer` endian reads | `Span<byte>` + `System.Buffers.Binary.BinaryPrimitives` |
| `java.util.zip.CRC32` | `System.IO.Hashing.Crc32` (NuGet) |
| `ForkJoinPool`/`ExecutorService` | TPL `Task` / `ThreadPool` / `Parallel` |
| `LinkedBlockingQueue` | `BlockingCollection<T>` |
| `CountDownLatch` / `ReentrantLock` | `CountdownEvent` / `lock` |

## The query-matcher blocker

GOQL queries are compiled to bytecode in Java:

```
query → MatcherParser → Selector/TagClause AST → MatcherCoder (→ ASM bytecode)
      → MatcherCompiler.defineClass() → Matcher instance
```

- ✅ **Port fully (pure logic):** `MatcherParser`, `MatcherSet`, `Selector`, `TagClause`,
  `TagMatcher`, `TypeMatcher`, `RoleMatcher`, `IdMatcher`, `AndMatcher`, `TypeBits`, `GlobalString`.
- ❌ **Stub (bytecode emission):** `feature/match/MatcherCoder`, `feature/match/MatcherCompiler`,
  `ast/ExpressionCoder`, `bytecode/Coder`, `bytecode/Instructions`.
- Tests that execute compiled matchers (`MatcherCompilerTest`, `PatternMatcherTest`) are ported but
  `[Fact(Skip="depends on bytecode-generated matcher")]`.

> Future pass: replace with an interpreted matcher or `System.Linq.Expressions`.

## Port order (leaf-first)

- **Phase 0 — Infra:** NuGet refs (`NetTopologySuite`, `System.IO.Hashing`); convert tests to xUnit;
  copy test resources; folder skeleton.
- **Phase 1 — `Clarisma.Common` leaf utilities:** `util/*`, `text/*`, `math/*`, `pbf/*` (byte[]/stream-based), `xml/*`.
  - Deferred to Phase 4 (depend on the `ByteBuffer` abstraction): `pbf/PbfDecoder` in full, plus the
    `ByteBuffer` overloads of `Bytes.readString`/`stringEqualsAscii`/`stringEquals`,
    `PbfEncoder.writeVarint(ByteBuffer,…)`, and `PbfOutputStream.writeTo(ByteBuffer)`.
- **Phase 2 — `GeoDesk.Geom`** (22 files): Box, Bounds, Mercator, Coordinates, XY, Tile, Hilbert,
  RTree, etc. Cleanest port; most unit tests live here. JTS→NTS maps cleanly.
  - Deferred (forward deps): `Measure` (needs `Feature`, Phase 5); `Box.ToGeometry` and
    `Tile.Polygon` (need `WayCoordinateSequence`/`BoxCoordinateSequence`/`GeometryBuilder`,
    Phase 5/7); `PointInPolygon.TestFast(StoredWay.XYIterator,…)` (needs `StoredWay`, Phase 5).
- **Phase 3 — Parsing & AST:** `parser/*`, `fab/*`, `ast/*` (minus `ExpressionCoder`). ✅
  - `parser/*` fully ported (`Parser` uses `System.Text.RegularExpressions` for the Java
    `Pattern`/`Matcher`; ordinal `List.BinarySearch` preserves Java token ordering).
  - `ast/*` ported except `ExpressionCoder` → **PORT-BLOCKED** stub (bytecode/IL emission).
    The `bytecode/*` package (`Coder`, `Instructions`) is *not* ported; `ExpressionCoder` is a
    standalone stub. `bytecode/*` is revisited in Phase 6 alongside `MatcherCoder`.
  - `fab/*` ported except `SaxFabReader` → **PORT-BLOCKED** stub (no .NET `org.xml.sax` equivalent;
    it is unused elsewhere). `FabReader` uses `TextReader`; `FabWriter` uses `TextWriter`.
  - Java generic visitor `<R> R accept(AstVisitor<R>)` → C# generic method `R Accept<R>(IAstVisitor<R>)`.
    `AstVisitor<Void>` implementors use `object?` as the (ignored) result type.
- **Phase 4 — Store layer:** `clarisma/common/store/*` (memory-mapped Store, BlobStore, FreeStore). 🚧 IN PROGRESS
  - ✅ **`ByteBuffer` abstraction** (`Clarisma.Common.Nio`): the .NET stand-in for `java.nio.ByteBuffer`.
    Abstract `ByteBuffer` (position/limit/capacity/order, absolute+relative get/put of byte/int/long/
    short/char, slice/duplicate) with two backings: `HeapByteBuffer` (byte[]) and `MappedByteBuffer`
    (memory-mapped via `MemoryMappedViewAccessor` + acquired pointer). Endianness via `BinaryPrimitives`.
    Validated by `ByteBufferTest`.
  - ✅ Wired up the deferred Phase-1 `ByteBuffer` members: `PbfDecoder` (full), and the `ByteBuffer`
    overloads of `Bytes`, `PbfEncoder`, `PbfOutputStream`.
  - ✅ `StoreException`, `BlobStoreConstants`.
  - ✅ `Store` (memory-mapped, journaled, transactional), `FreeStore` (read-only reader),
    `BlobStore` (free-list blob alloc/coalesce), `BlobStoreChecker` (integrity verify).
    `Downloader` (HTTP tile fetch) → **PORT-BLOCKED** stub (network/threading; not needed offline,
    not tested).
  - ✅ Validated end-to-end by `BlobStoreTest` (`TestAllocFree` with exact page-number assertions +
    `TestBigAllocFree` with `BlobStoreChecker`), which crosses 1 GB segment boundaries.
  - Java→.NET mappings used: `FileChannel`+`MappedByteBuffer[]` → `FileStream` + `MemoryMappedFile`
    with one view accessor per 1 GB segment; `force()` → `Flush()`; `Unsafe.invokeCleaner` unmapping
    → deterministic `Dispose()`; `RandomAccessFile` journal → `FileStream` (big-endian I/O);
    `CRC32` → `System.IO.Hashing.Crc32`; `MutableLongObjectMap`/`IntHashSet` → BCL `Dictionary`/`HashSet`.
  - **PORT-LIMITATIONS (documented in code):** (1) shared byte-range file locks have no BCL
    equivalent, so the multi-process shared-lock protocol is downgraded to best-effort (single-process
    correct); (2) sparse files marked via P/Invoke on Windows (Unix sparse by default); (3) growing the
    mapping across a 1 GB boundary recreates view accessors — handled by re-fetching mappings at point
    of use rather than caching a buffer across a transaction.
- **Phase 5 — Feature store & model:** `geodesk/feature/store/*`, then `Feature`/`Node`/`Way`/
  `Relation`/`Tags`/`Features`/`FeatureLibrary`. 🚧 IN PROGRESS
  - ✅ **Model interfaces:** `Feature`, `Node`, `Way`, `Relation`, `Tags`, `Filter`, `Features`
    (abstract contract + iteration default methods). `FeatureType` (enum + `FeatureTypes` helper,
    since C# enums can't hold static methods), `FeatureId`, `FeatureException`, `MissingTileException`.
  - ✅ **Store constants/helpers:** `FeatureFlags`, `FeatureConstants`, `FeatureStoreConstants`
    (standalone, not extending BlobStoreConstants), `Tip`, `GlobalStrings`, `ZoomLevels`, `TagValues`,
    `EmptyTags`, `Match/TypeBits` (pulled forward — needed by `Filter`/`Features`).
  - ✅ **Coordinate sequences:** `WayCoordinateSequence`, `BoxCoordinateSequence` — subclass NTS's
    abstract `CoordinateSequence` (NTS uses an abstract class, not an interface, so `BoxCoordinateSequence`
    uses composition instead of also extending `Box`). This **resolved the Phase-2 `Box.ToGeometry` deferral.**
  - `IConsumable.IsEmpty` changed from property → method so `Tags` (which redeclares it) stays consistent.
  - ✅ **Feature-store readers** (reading layer): `FeatureStore` (extends `FreeStore`; opens a GOL,
    reads the global string table / index schema / tile index, wires `GetMatcher` → the working
    interpreter), and `StoredFeature`/`StoredNode`/`StoredWay`/`StoredRelation` (tag reading,
    coordinates, node/way geometry, ids, bounds). **Validated against a real `monaco.gol`** by
    `FeatureStoreTest` (opens the GOL, round-trips global strings, reads zoom levels). This resolved
    the Phase-2 `PointInPolygon.TestFast(StoredWay.XYIterator)` deferral (XYIterator is ported).
  - 🚧 **Remaining (the query-view cluster):** `feature/query/*` (`View`, `WorldView`, `TableView`,
    `WayNodeView`, `MemberView`/`MemberIterator`, `ParentRelationView`, `NodeParentView`, `Query`/
    `QueryTask`/`TileQueryTask`/`RTreeQueryTask`, `TileIndexWalker`), `AnonymousWayNode`,
    `feature/filter/*`, `feature/polygon/*`, and `FeatureLibrary` (extends `WorldView`). Until these
    land, the `Stored*` view-returning methods (`Parents`/`Nodes`/`Members`/enumeration), relation
    geometry, `Features` filter defaults, and `Features.Open` are stubbed. Also still deferred:
    `Measure`, `Tile.Polygon`. Needs the tile-query executor (Java `ForkJoinPool` → TPL).
- **Phase 6 — Query & match:** `feature/match/*`, `feature/query/*`, `feature/filter/*`, `feature/polygon/*`. 🚧 STARTED
  - ✅ **Matcher runtime** (self-contained, portable part of `feature/match`): `Matcher` (made a concrete
    base — Java's abstract class only existed to host bytecode subclasses), `QueryException`,
    `GlobalString`, `MatcherSet`, `IdMatcher`, `TypeMatcher`, `AndMatcher`, `TagMatcher`, plus `TypeBits`
    (ported in Phase 5). These are the runtime matchers a compiled/interpreted query uses.
  - 🚧 **Remaining:** `MatcherParser`/`Selector`/`TagClause` (GOQL parser → AST, pure logic, portable),
    `MatcherCoder`/`MatcherCompiler` (**PORT-BLOCKED** — AST→bytecode), `MatcherXmlWriter`; the whole
    `feature/query/*` (views, tile query tasks, iterators), `feature/filter/*` (spatial filters),
    `feature/polygon/*`. These plus the Phase-5 `Stored*` readers + `FeatureStore` form one tightly
    coupled cluster (readers return query views; the store opens a `MatcherCompiler`).
  - **Consequence of the bytecode block:** GOQL queries can be *parsed* but not *compiled* to a runtime
    `Matcher` without reimplementing `MatcherCoder` via `System.Reflection.Emit` or an interpreter, so
    end-to-end query execution stays stubbed in this pass (`Matcher.ALL` covers "match everything").
- **Phase 7 — IO:** `io/*`, `io/osm/*`.
- **Phase 8 — Tests:** port all 24 JUnit files to xUnit; skip bytecode-dependent ones.
- **Phase 9 — Integration tests** (`ext/geodesk-tests` submodule): duplicate the ~40 integration
  tests into the xUnit project. They read a `.gol` at runtime via `TestSettings.GolFile()`, resolved
  from `src/GeoDesk.Tests/Fixtures/*.gol` (copied to the output dir via `<None CopyToOutputDirectory>`;
  no GOL committed — see `Fixtures/README.md`). These require the full feature-reader API
  (`FeatureLibrary`/`Features`/`Feature` + query views + spatial filters), so they come online as
  that cluster lands.
  - **FUTURE STEP:** add an MSBuild target that generates the required `.gol` fixtures on demand
    (e.g. download a small `.osm.pbf` like Monaco and build it with the GOL tool) so the integration
    tests are runnable from a clean checkout without manually supplying a GOL.
