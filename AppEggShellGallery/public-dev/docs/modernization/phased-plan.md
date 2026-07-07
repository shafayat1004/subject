# Phased Plan

The modernization is structured as seven phases (0 through 6). Phases 0, 1, and 2 are complete.
Phase 3 is designed but not started. Phase 4 is the live frontier. Phases 5 and 6 are designed but
not started.

For the goal-level view (Goals A through H + security), see [Goals & Roadmap](./modernization/goals-and-roadmap.md).
For the motivation behind each step, see [ReactXP to RNW](./modernization/reactxp-to-rnw.md) and
[Architecture](./architecture/index.md).

---

## Phase table

| Phase | What | Validates | Status |
|-------|------|-----------|--------|
| **0** | De-risking spikes: worklet path from F#, SignalR on Fable 5, Orleans 3.7.2 on .NET 10 | Modern stack is viable before committing | **Done** |
| **1** | Fable 5 + .NET 10 SDK. Project TFMs stay on their current targets. | Gallery + libs compile on new toolchain | **Done** |
| **2** | SignalR modular sibling repo (`eggshell-signalr`). Direct `@microsoft/signalr` bindings, no Fable.SignalR wrapper. | Typed streaming transport on Fable 5 | **Done** |
| **3** | Backend TFM to net10. Orleans 3.7.2 stays frozen. | Silo + simulation tests on net10 | Not started |
| **4** | ReactXP to RN + react-native-web seam swap + platform upgrade. | Gallery + native on modern RN (New Architecture, React 19) | **Done (AppTodo verified on device)**; gallery/PP native config + Reanimated remain |
| **5** | Full-stack TODO reference app + templatized scaffold (`eggshell create-app`). | Goal B: `create-app -> dev-web` end to end | Not started |
| **6** | Docker SQL Server + persistent dev stack | Real DB, not just in-memory Orleans | Not started |

Phases 1, 3, and 5 to 6 can overlap once Phase 1 gates are green. Phase 4 stays after the platform
baseline (Phase 1). Phase 5 is the validation benchmark for the whole modernization: if the TODO app
runs on web, iOS, and Android with simulation tests and UI automation green, the framework story is
proven for greenfield work.

---

## Phase 0: De-risking spikes

Three parallel spikes, all run as standalone projects isolated from the main repo.

**Worklet spike.** The key question: can Fable-compiled F# drive Reanimated animations on the UI
thread? Answered: Fable closures run on the JS thread, not the UI thread, because compiled F# depends
on FSharp.Core helpers that do not exist in the isolated worklet runtime. The correct framework
pattern is declarative animation (Moti / Reanimated declarative API) from F#, with any genuine UI-thread
worklet authored as a tiny vetted JS shim via the `ThirdParty` / `TypesJs.fs` recipe. See
[ReactXP to RNW: F#/Fable specific risks](./modernization/reactxp-to-rnw.md#11-f--fable-specific-risks).

**SignalR spike.** The public `Fable.SignalR` package is abandoned and pinned to Fable 3 era;
EggShell's private builds are also Fable-3-era. Solution: bind Microsoft's maintained
`@microsoft/signalr` JS client directly from F# (a ~30-line thin binding). Verified end to end on
Fable 5: F# client over WebSocket, live counter incrementing from a .NET 10 ASP.NET Core hub.

**Orleans spike.** Orleans 3.7.2 builds and runs on .NET 10. Both scary gates pass: the legacy
`Orleans.CodeGenerator` MSBuild task built clean on the net10 SDK (only a `SYSLIB0051` warning); the
silo started on .NET 10.0.9 and grain arg/return round-trips worked. EggShell's custom exception types
(`TransientSubjectException`, etc.) propagated correctly across the grain boundary. BinaryFormatter is
fully removed in .NET 9+, but EggShell's grain state uses gzip-compressed System.Text.Json via Fleece,
so no BinaryFormatter is hit on the app path.

---

## Phase 1: Fable 5 + .NET 10 SDK

**What changed:**

- `global.json` bumped to .NET 10 SDK 10.0.301.
- Fable tool bumped to 5.4.0; `Fable.Core` to 5.0.0; `Fable.React` to 10.0.0-alpha.1.
- `Meta/FablePlugins` (the `[<Component>]` attribute plugin) updated: `FableMinimumVersion "4.0"` to
  `"5.0"`, `Fable.AST` 4.5.0 to 5.0.0, and the new `Ident.IsInlineIfLambda` field in
  `AstUtils.makeIdent`. Plugin base classes and consumer call sites are unchanged.
- Package bumps: `Fable.Core`, `Fable.React`, `Thoth.Json`, `Fable.Promise`, `Fable.Browser.*`,
  `Fable.Date`.
- `FSharp.Core` bumped; stale CLI flags in `Meta/LibFablePlus/src/index.ts` reviewed.
- Fable and precompile caches wiped.

**What did not change:** project TFMs (still targeting their original frameworks). The .NET 10 SDK is
the build host; the per-project TFM bump is Phase 3 on the backend and a separate cleanup pass on the
frontend.

**Known open items from Fable.React 10.0.0-alpha.1:**

- Issue #241: `FunctionComponent.Of` caches by source line, which can collide when multiple distinct
  components share a line. EggShell's normal path uses `[<Component>]` (compile-time, one component
  per declaration), so exposure is low; verify the plugin's keying during any `FunctionComponent.Of`
  usage.
- Issues #239 / #240: `useCallback` not implemented; `forwardRef` not exposed. React 19 ref-as-prop
  softens forwardRef; missing hooks can be bound directly via the ThirdParty pattern.
- Fable 5.0.0 behavioral change: `Array` / `ResizeArray` equality semantics changed. Grep for
  reliance on structural array equality via `=` before bumping existing code.

---

## Phase 2: SignalR modular sibling repo

`Fable.SignalR` (both the public 0.11.5 and EggShell's private 0.16.0 builds) are incompatible with
Fable 5. The solution: bind `@microsoft/signalr` directly from F# in a sibling repo
(`eggshell-signalr`), referenced as a `ProjectReference`. No wrapper package, no vendored copy.

This was verified end to end in the Phase 0 spike and formalized in Phase 2. The server side stays
stock ASP.NET Core SignalR (Microsoft-maintained, tracks .NET). The client binding is a thin F# file.

---

## Phase 3: Backend TFM to net10 (Orleans frozen)

**What to do:**

- Bump `<TargetFramework>` on `LibLifeCycleHost`, `LibLifeCycle`, `LibLifeCycleTypes`, and the host
  entry-points from their current targets to `net10.0`.
- Resolve `Microsoft.Extensions.*` version-unification conflicts that arise against a net10 host.
- Verify `Giraffe 6.2.0` / `Giraffe.TokenRouter 3.0.0-alpha-1` against ASP.NET Core 10.
- Remove the stray `System.Text.Json 5.0.2` pin in `LibLangFsharp.fsproj`.
- Run the full `LibLifeCycleTest` simulation suite against the bumped backend.

**Orleans stays at 3.7.2.** The 3.x to 7.x upgrade (wire protocol change, grain identity change) is a
separate, later coordinated workstream bundled with the Postgres migration (Goal G). It is explicitly
not part of this phase.

**Risk:** ADO.NET clustering + SQL Server reminders + the `SubjectReminderTable` path were not tested
in the Phase 0 spike (intentionally isolated to avoid Mac-ARM SQL Server complexity). These need a
targeted test run with the actual database in Phase 3 or a staging environment.

---

## Phase 4: ReactXP to RN + react-native-web seam swap + platform upgrade (done; AppTodo verified)

> **Status update (session 8):** the ReactXP name/seam is fully retired to `Rn`
> (`LibClient/src/Rn/`, namespace `Rn`, prefix `Rn.`). Upgraded to **React 19.2.3 / RN 0.86.0 /
> RNW 0.21.2 / RNGH 3.0.2 / Reanimated 4 with the New Architecture (Fabric) enabled**. AppTodo builds
> and renders on **Android** (POCO F1, `Running "RnApp" with {"fabric":true}`, debug + release) **and
> iOS** (iPhone 16 simulator, dark mode). The "proven version pins" below are from the
> earlier spike (RN 0.85); the shipped apps are now on RN 0.86. Remaining: gallery +
> PerformancePlayground native config; Reanimated 4 + Moti. See the session-8 engineering-log entry
> for the full recipe + gotchas (render-compiler rebuild, native-module bumps, RNGH shim).

**Shape:** re-implement `LibClient/src/ReactXP` against React Native (native) + react-native-web
(web) instead of `@chaldal/reactxp`, keeping F# constructor signatures and the style DSL surface
identical. Everything above the seam (LibClient components, LibUi*, apps, `makeViewStyles` call
sites, LibRouter) is untouched.

`@chaldal/reactxp` has been removed as a dependency. The seam is `LibClient/src/ReactXP/RNSeam.fs`.
Scroll, gesture, and picker stabilization is the remaining open work.

**Reference primitive:** `View.fs` was ported as the first primitive. Pattern established:

- `import "View" "react-native"` (bundler aliases `react-native` to `react-native-web` on web).
- `makeViewStyles` emits plain RN style objects via `RNSeam.createViewStyle`.
- A11y props preserved (`testId` to `testID`, live region strings, ARIA attributes).
- ReactXP-only props dropped silently with no F# call-site changes.

Gallery `dev-web` webpack bundle is green with `react-native-web` in the bundle at port 8082.

**Fan-out order:** Text, Button (`LC.Pressable`), ScrollView, TextInput, Image, then the Animatable
wrappers and SVG/NetInfo/WebView.

**Animation layer (after primitives):** Reanimated 4 + RNGH 3 bindings from F#, with Moti
declarative API as the primary F# path. UI-thread worklets stay in small vetted JS shims.

**Style DSL retargeting:** ReactXP nested `&&` / `=>` selectors and responsive breakpoints have no
native RN equivalent; keep them web-only (RNW supports some), handle native variants via F# branching
(`LC.With.ScreenSize`, `#if EGGSHELL_PLATFORM_IS_WEB`).

**Proven version pins:** React 19.2.3, RN 0.85.3, RNW 0.21.2, Reanimated 4.3.1 +
`react-native-worklets` 0.8.3, RNGH 2.31.1, Moti 0.30, Expo SDK 56. See [Migration Execution](./runbooks/migration-execution.md)
sections 4.5 to 4.8.

---

## Phase 5: Full-stack TODO reference app + templatized scaffold

Phase 5 is the **validation benchmark** for the whole modernization. A greenfield developer runs:

```bash
eggshell create-app Todo
cd SuiteTodo/AppTodo
./dev-stack up
```

and gets a working full-stack app on web, iOS, and Android, with simulation tests and UI automation
wired.

**Build order:**

1. Build a concrete `SuiteTodo/` in-repo reference implementation: working app, not scaffold output.
2. Get it green on all platforms: web CRUD + live subscription, native smoke, simulation tests pass.
3. Templatize into `Meta/LibScaffolding/templates`: replace Chaldal demo routes with TODO content;
   nested `render` schema in `eggshell.json.template` (or empty render block for pure F# only);
   correct `App.fsproj.template`; `package.json.template` with webpack dev-server and Playwright.
4. Smoke gate: CI runs `eggshell create-app` in a temp dir, then `./dev-stack up`, then Playwright
   pass.

**What the TODO app demonstrates:** `Todo` subject lifecycle (Create, SetTitle, ToggleDone, Archive,
Delete); `TodoListView` read model; timer-driven auto-archive; SignalR real-time push; simulation
tests across virtual time; `LC.Input.Text` with validation; `LC.With.Subjects` on the view;
accessibility with `testId` / `A11ySlug`; Playwright web audit + Android smoke.

This phase also fixes Goal B (scaffolding) by building the correct template from a working concrete
app, not by patching incrementally.

---

## Phase 6: Docker SQL Server + persistent dev stack

`./dev-stack up` folds Docker SQL Server into a one-command dev experience: Orleans backend with real
ADO.NET storage (not in-memory), dev-web, and optionally Metro for native. This is a prerequisite for
the TODO reference app having a realistic, shareable dev environment.

The `docker-compose.yml`, SQL init scripts, and the `Dev` launcher's switch from in-memory to ADO.NET
live in the generated app template (Phase 5), then become `eggshell dev-stack` in the CLI in a later
pass.

---

## Accessibility alignment

The a11y initiative runs alongside all phases and is explicitly migration-safe by design. Anything
expressed through RN-native a11y props (`accessibilityLabel`, `accessibilityRole`,
`accessibilityState`, `accessibilityLiveRegion`, `importantForAccessibility`, `accessibilityActions`)
survives the ReactXP-to-RNW seam swap unchanged. Work that is gated until after Phase 4 (true DOM
landmarks, skip links, `:focus-visible` ring styling, roving-tabindex keyboard mechanics) is tagged
`[rnw-blocked]` in the accessibility plan and should not be hand-rolled before the seam swap.

See [Accessibility](./accessibility/index.md).
