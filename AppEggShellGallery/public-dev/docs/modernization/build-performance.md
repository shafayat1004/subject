# Build Performance (Goal E)

Frontend builds are slow. The cost is a **compound of independent stages**, not a single bottleneck.
Pipeline orchestration lives in `Meta/LibFablePlus/src/index.ts` and
`Meta/LibRtCompilerFileSystemBindings/src/index.ts`.

---

## What happens on `eggshell dev-web`

1. **Dependency libs build serially** (`Promises.inSeries`): no parallelism across the dependency
   tree. Cold builds with a wide dependency graph pay the full serial cost.
2. **Clean** bundle and output directories.
3. **Render-DSL compile:** `processAllRender` globs every `.render` file and spawns the F# render
   compiler **once per file** as a subprocess (~548 files repo-wide historically). Parallelized, but
   each is a full process spawn with .NET startup cost. This stage is **eliminated** once Goal A
   (render DSL retirement) is complete.
4. **LibStandard precompile:** a Fable `precompile` pass that runs **before** the app compiles and is
   **not fingerprint-cached**. It reruns even when nothing changed.
5. **Fable compile** with `--noParallelTypeCheck` explicitly set (intentional, to avoid webpack
   thrashing): over a large surface (`LibClient` alone is ~500 `.fs` files) with heavily generic
   lifecycle types (generic instantiation is a known Fable cost).
6. **webpack:** single monolithic bundle (`splitChunks: false`, `LimitChunkCountPlugin maxChunks:1`),
   `eval-source-map` in dev.

---

## Ranked bottlenecks and levers

| Bottleneck | Lever | Effort | Notes |
|------------|-------|--------|-------|
| LibStandard precompile, uncached | Hash-fingerprint the precompiled output; skip when unchanged | Low-Med | Likely the single biggest warm-build win. |
| `--noParallelTypeCheck` | Re-enable parallel type-check; debounce webpack trigger instead | Low | 30 to 50% off the F# phase; verify no watch thrash. |
| 548 render subprocesses | Retiring the DSL (Goal A) **removes this stage entirely**; interim: batch files per compiler invocation | High (retire) / Med (batch) | Best fixed by deleting the DSL, not optimizing it. Now 41 test fixtures only; product stage already gone. |
| Serial dep builds | Topological parallel build | Med | Helps cold builds with wide trees. |
| webpack monolith + `eval-source-map` | `cheap-module-source-map` in dev; consider esbuild/Vite long-term | Med-High | Bundler swap is the big long-term win. |
| Large/generic LibClient | Fable 5 (better caching/codegen, done); optional project split | High | Fable 5 migration already delivers this improvement. |

---

## Status (2026-07-02)

| Lever | Status |
|-------|--------|
| Render subprocess stage removed | **Done** (Goal A product code complete; 41 test fixtures remain but are not on the `dev-web` build path). |
| Fable 5 caching/codegen improvement | **Done** (Fable 5.4.0 migrated). |
| LibStandard precompile fingerprint cache | Not started. |
| Re-enable parallel type-check | Not started. |
| Parallelize dep-tree build | Not started. |
| Lighten dev source maps | Not started. |
| Bundler swap (esbuild/Vite) | Not started. Long-term. |

---

## Quick-win recommendations

The two cheapest, highest-leverage moves that can be done today independently of other goals:

1. **Fingerprint-cache LibStandard precompile.** Hash the input files; if the hash matches the last
   run, skip the precompile pass entirely. This is the single biggest warm-build win because
   precompile currently runs on every `dev-web` invocation regardless of whether any framework file
   changed.

2. **Re-enable `--noParallelTypeCheck`** (restore parallel type-checking) and debounce the webpack
   trigger. The flag was set to avoid webpack thrashing, but the correct fix is debounce, not
   serialization. Parallel type-check can cut the F# phase by 30 to 50%.

Both are isolated changes to `Meta/LibFablePlus/src/index.ts` and
`Meta/LibRtCompilerFileSystemBindings/src/index.ts` with no effect on the framework surface.

---

## Structural wins (arrive with other goals)

- **Goal A (DSL retirement):** deletes the render-compiler subprocess stage entirely. With 548
  subprocesses on a cold build, this is a wall-clock win even though subprocesses run in parallel.
  Fable startup cost per subprocess also disappears.
- **Fable 5 (Goal F, done):** improved caching and codegen from the MSBuild-direct project cracker.
- **Bundler swap (long-term):** replacing webpack with esbuild or Vite would be the largest single
  step change for both cold and hot builds, but it is a larger effort and is not part of the current
  initiative.

---

## Relationship to build correctness

**No stale-cache false greens.** Fable may print `Skipped compilation because all generated files
are up-to-date!` and exit 0 without type-checking edits. Before calling a change done: force
recompile (`touch` changed `.fs`, or clear `.build/<platform>/fable`), confirm
`Started Fable compilation...` appears in output, and confirm `rg "error FS"` is clean. If a
watch/dev terminal is already running, its output is authoritative for that edit.

See [Runbooks](./runbooks/migration-execution.md) for the full build-and-validate commands.
