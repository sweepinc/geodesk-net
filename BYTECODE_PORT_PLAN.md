# Sub-Plan: Porting the Query-Matcher Compiler (the "bytecode" part)

The one PORT-BLOCKED subsystem in the GeoDesk port is the GOQL **query-matcher compiler**,
which in Java generates a JVM class at runtime (via ASM bytecode) per query. This sub-plan
covers replacing that with a .NET-native implementation.

## What the Java code does

```
query string
  → MatcherParser            (GOQL grammar → AST)         [pure logic, portable]
  → Selector / TagClause / Expression chain               [pure logic, portable]
  → MatcherCoder : ExpressionCoder : Coder                [emits JVM bytecode via ASM]
  → ClassLoader.defineClass(...)                          [loads generated class]
  → a Matcher subclass instance                           [accept(ByteBuffer,int): bool]
```

`MatcherCoder` (~2,100 LOC) + `ExpressionCoder` (~480 LOC) emit one method —
`bool accept(ByteBuffer buf, int pos)` — that scans a feature's **tag table** (global keys
forward from the tag-table pointer, local keys backward), checks each `TagClause`'s key
presence, and evaluates its value `Expression`. It is written in **JVM stack-machine style**
(`DUP`/`IADD`/`IF_ICMPGE`/`ISTORE`/`ILOAD`, `Label`s, gotos, local-variable slots
`$buf`/`$pos`/`$tag`/`$tagtable_ptr`/…). The result is a precompiled predicate.

## Feasibility: `System.Linq.Expressions` is the right target

The generated matcher's **control flow maps directly** onto Expression-tree primitives:

| ASM / JVM bytecode | `System.Linq.Expressions` |
|---|---|
| `Label` + `visitJumpInsn(GOTO, l)` | `LabelTarget` + `Expression.Goto(l)` |
| `IF_ICMPGE label` | `Expression.IfThen(GreaterThanOrEqual(a,b), Goto(target))` |
| local slots `$pos`, `$tag`, … | `ParameterExpression` locals in `Expression.Block` |
| `ISTORE` / `ILOAD` | `Expression.Assign` / variable reference |
| `check_next_tag:` loops | backward `Goto` in a block (or `Expression.Loop`) |
| `buf.getInt(pos)`, `Bytes.readString(...)` | `Expression.Call` |
| `IRETURN` | `Expression.Return` to the method's end label |
| whole `accept` method | `Expression.Lambda<Func<ByteBuffer,int,bool>>(body, bufP, posP).Compile()` |

**Caveat (the one real impedance):** the Java emitter is **stack-based** (push a value, consume
it later), while Expression trees are **tree-based** (no operand stack). So this is a
*re-targeted* port — the same algorithm (`matchClause`, `scanGlobalKeys`, `scanLocalKeys`,
`matchString`, `fetchTagValue`, …), but the emit primitives change: value-producing steps
**return an `Expression`**, and consuming/branching steps **take `Expression` arguments** and
build a node — instead of pushing/popping an operand stack. It is **not** a literal
opcode-for-opcode transliteration.

**Bonuses vs. the JVM version:**
- No `ClassLoader`/`defineClass`/class-unloading machinery — `Expression.Compile()` yields a
  delegate; cache `Func`s in a `Dictionary<string, Matcher>`. The Java `Coder`/`Instructions`
  (ASM helpers) and the whole class-loading path in `MatcherCompiler` are dropped.
- The runtime base (`Matcher`/`TagMatcher`) is already ported; a compiled matcher is just a
  `Matcher` subclass holding the compiled `Func` + the `globalStrings` table.

## Components & status

| Component | Role | Port status |
|---|---|---|
| `ast/*` (BinaryExpression, Literal, Operator, …) | value-expression AST | ✅ ported (Phase 3) |
| `match/Matcher`, `TagMatcher`, `MatcherSet`, `TypeBits`, `GlobalString`, type/id/and matchers | runtime matchers | ✅ ported |
| `match/MatcherParser` | GOQL grammar → Selector/TagClause/Expression | ⬜ to port (pure logic) |
| `match/Selector`, `match/TagClause` | query AST | ⬜ to port (pure logic) |
| `ast/ExpressionCoder` | generic boolean-expr → predicate | 🔁 re-target to Expression trees (or interpret) |
| `match/MatcherCoder` | tag-table scan + value checks → predicate | 🔁 re-target to Expression trees (or interpret) |
| `match/MatcherCompiler` | orchestration: parse → compile → cache | 🔁 rework (no ClassLoader) |
| `bytecode/Coder`, `bytecode/Instructions` | ASM helpers | ❌ drop (not needed) |
| `match/MatcherXmlWriter` | debug dump of matcher AST | ⬜ optional |

## Two engine strategies

**Strategy A — Interpreter (lowest risk; recommended as Step 1).**
A single `InterpretedMatcher : TagMatcher` whose `Accept(buf, pos)` walks the
`Selector`/`TagClause`/`Expression` chain directly at runtime (scans the tag table, evaluates
expressions). ~300–500 LOC, no codegen, always correct. Slower per-feature (tree-walk per call)
but trivially validated against the Java tests. Makes **GOQL queries actually work**.

**Strategy B — Expression-tree compiler (perf parity; the headline goal).**
Re-target `ExpressionCoder` + `MatcherCoder` to build an `Expression` tree and `.Compile()` it
to `Func<ByteBuffer,int,bool>`, wrapped in a `CompiledMatcher : TagMatcher`. Faithful to the
Java design (a precompiled per-query matcher), JIT-compiled, fast. Larger, more intricate effort
(re-deriving the stack-based emitter as a tree-builder).

**Recommended order:** A then B. The interpreter gets queries working and serves as a **semantic
oracle** for B (compile a query both ways, assert identical results across a corpus of features).
This de-risks B substantially. B can be added later without changing the public API.

## Sub-phases

- **B0 — Port the query AST/parser** (needed by *both* strategies): `MatcherParser`, `Selector`,
  `TagClause` (+ `query/IndexBits`). Pure logic. Validate against `QueryParserTest`. ✅ **DONE** —
  builds the Selector/TagClause AST; validated by `QueryParserTest` against the real `strings.txt`
  string table (types, value lists, wildcards, negation, error cases). 35/35 tests green.
- **B1 — Interpreter** (Strategy A): `InterpretedMatcher`; wire `MatcherCompiler.GetMatcher`
  (parse → cache an `InterpretedMatcher`). Validate against `MatcherCompilerTest`.
  Concrete build spec (derived from reading `STagTable` + `TagTableTester`):
  - **Tag-table binary format** (from `STagTable.writeTo`): a feature struct is `[long 0][tagtable
    tagged-pointer @ ofs 8][int 0]`; the matcher's `accept(buf,pos)` does `pos += 8`, reads the
    tagged relative pointer (bit 0 = "has local keys"), and resolves the anchor.
    **Global (common) keys** are written forward from the anchor as `short key = (keyCode<<2) |
    string?1 | wide?2 | last?0x8000`, followed by the value (`short` narrow / `int` wide / pointer
    for local string). **Local (uncommon) keys** are written backward *before* the anchor: value
    first, then `int keyPtr = ((keyStringLoc - origin)<<1) | string?1 | wide?2 | firstUncommon?4`,
    where `origin = (tagTablePtr) & ~3`.
  - **Validation harness to port** (test tree): the `soar` framework (`Struct`, `SharedStruct`,
    `SString`, `StructOutputStream`, `Archive`) + `STagTable` (the *writer*) + `TagTableTester`.
    These build the exact tag-table bytes. Ground truth is `queries.fab` (`query → {object: bool}`)
    + `tags.fab` (objects→tags) + `strings.txt` — all already copied to the test output dir.
    Because `queries.fab` expectations are independent of the writer, reader+writer agreeing on all
    ~40 cases is strong validation. **Correctness-critical:** the relative-pointer + anchor + origin
    math must match `soar`'s `writePointer`/`location()`/`anchor()` exactly — port `soar` faithfully
    rather than re-deriving, to avoid a silent format mismatch.
  - **Interpreter semantics to cover** (from `MatcherCoder`): global/local key scan; key
    presence per clause flags (`[k]`/`[!k]`/required/optional) incl. the `"no"` check; value types
    (global-string code, local-string pointer, narrow/wide number); operators EQ/NE/LT/LE/GT/GE,
    AND/OR/NOT, IN/STARTS_WITH/ENDS_WITH (wildcards) and MATCH/NOT_MATCH (regex → `System.Text.
    RegularExpressions`); last-tag flags.
  - ✅ **DONE.** Ported the `soar` writer framework (`Struct`/`SharedStruct`/`SString`/
    `StructOutputStream`/`Archive`) + `STagTable` + `TagTableTester` (test project), built
    `InterpretedMatcher` (reads the tag table, walks the AST), and ported `MatcherCompilerTest`.
    **All 186 queries / 495 object-query checks pass** against `queries.fab`/`tags.fab`/`strings.txt`.
    The interpreter reads global+local keys, all four value types, and handles EQ/NE/LT/LE/GT/GE,
    AND/OR/NOT, wildcards (IN/STARTS_WITH/ENDS_WITH), regex (full-match via .NET `Regex`), the `"no"`
    presence rule, and the explicitly-required (`[k][k!=v]`) case.
- **B2 — Un-stub** `FeatureStore.GetMatcher` and `Features.Open`/the filter defaults that need a
  working matcher; this lets the **feature store readers + query views** (the remaining Phase 5/6
  cluster) execute real GOQL queries end-to-end.
  - ✅ `MatcherCompiler` ported and wired to `InterpretedMatcher` (parse → polyform check → cache).
    Ready to plug into `FeatureStore.GetMatcher` once `FeatureStore` lands. (36 tests green.)
- **B3 — Expression-tree compiler** (Strategy B, optional/perf): re-target `ExpressionCoder` +
  `MatcherCoder` → `Func<ByteBuffer,int,bool>`; `CompiledMatcher`. Validate against the interpreter
  as an oracle + the Java tests; then switch `MatcherCompiler` to use it.

## Validation assets (Java tests to port)

`test/com/geodesk/feature/query/QueryParserTest`, `MatcherCompilerTest`, `PatternMatcherTest`
(currently the ones marked to skip in the main plan). These become the green-light for the
matcher engine — once they pass, GOQL is functional.

## Note on the other bytecode stub

`ast/ExpressionCoder` is shared infrastructure; `MatcherCoder` is its only consumer in this
codebase. Whichever strategy is chosen for the matcher applies to `ExpressionCoder` as well
(interpret the `Expression` AST, or re-target it to Expression trees). There is no separate
second bytecode consumer to worry about.
