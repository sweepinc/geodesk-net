# GOL test fixtures

The integration tests (ported from the `geodesk-tests` submodule) read a Geographic Object
Library (`.gol`) file at runtime. **No `.gol` is committed** — these are large, externally
provided datasets.

Drop the GOL(s) the tests expect into this directory, e.g.:

```
src/GeoDesk.Tests/Fixtures/monaco.gol
```

The project copies every `Fixtures/**/*.gol` to `Fixtures/` in the test output directory
(via a `<None Include>` item with `CopyToOutputDirectory`). `TestSettings.GolFile()` resolves
the GOL from there.

> The GOL must be built with **GOL Tool 2.0** (v1.x GOL files are not compatible with GeoDesk 2.0).

## Future step
An MSBuild target will generate the required `.gol` fixtures on demand (e.g. download a small
`.osm.pbf` such as Monaco and build the GOL during the build). See `PORT.md` (repo root) for the
overall port status and the test data-coupling notes.
