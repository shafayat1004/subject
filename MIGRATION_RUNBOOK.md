# EggShell Modernization Runbook (executor edition)

> **Who this is for:** an executor model doing the mechanical bulk of the .NET 10 + Fable 5 +
> react-native-web modernization. It is a step-by-step companion to the strategy doc
> `FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md` (read that for the *why*; this is the *how*).
>
> **Scope:** Phases 1-4 are framework modernization (goals F/G/H). Phases 5-6 are **goal B**: the full-stack
> TODO reference app, templatized `create-app`, multi-platform dev, UI automation, and docker SQL. The
> repo's *current* initiative A-E on Fable 4 (see `subject/CLAUDE.md`) continues in parallel until
> modernization is explicitly started.
>
> **Proven on spikes (do not re-litigate):** the whole path is already validated end to end in
> `/Volumes/HomeX/shafayat/Code/eggshell-rnw-spike` (frontend: Fable 5 + RNW + Reanimated + RNGH + Moti +
> SignalR) and `/Volumes/HomeX/shafayat/Code/orleans-net10-spike` (backend: Orleans 3.7.2 on .NET 10).
> Use those as copy-from references.

> **CURRENT BRANCH STATUS (`modernization/fable5-migration`, 2026-06-29):** the toolchain is **already
> Fable 5.4.0 on .NET 10 (10.0.301)** — `Fable.Core` 5.0.0, `Fable.React` 10.0.0-alpha.1; native + web
> build green this session. So **Phase 1 (Fable 5 + .NET 10 build host) is effectively DONE on this
> branch**, and SignalR is on the modular `eggshell-signalr` bindings (**Phase 2 done**). **Phase 4 (RNW
> seam) is NOT started — still `@chaldal/reactxp`** (this session even patched vendored ReactXP, then
> reverted in favor of an F# fix). Phase 3 (backend net10 TFMs) status: SDK is net10; per-project TFMs
> unverified here. Treat Phases 1-2 as recipe/history for re-runs; the live frontier is Phase 4 (+ Phase
> 5 app work, ongoing). NOTE: `subject/CLAUDE.md`'s "stay on current Fable (v4)" line is **stale on this
> branch** — confirm with the owner before treating v4 as current.

---

## 0. Golden rules (apply to every step, no exceptions)

1. **Validate after every change. Never claim done on an unverified change.** Each step below has a
   VALIDATE block; the build/run must be green before you move on. Paste real output when reporting.
2. **Escalate, do not guess.** If a step fails in a way NOT listed under its PITFALLS, or a fix is not
   obvious in one attempt, **STOP and hand back to a stronger model** with the exact error. Plausible-
   but-wrong edits are the main failure mode (see `LEARNINGS.md` for past examples).
3. **One change at a time.** Bump one thing, build, confirm green, then the next. Do not batch unrelated
   edits.
4. **Work framework-only** (`Lib*`, `LibUi*`, `LibRouter`, `LibAutoUi`, `LibLifeCycleUi`, `ThirdParty`,
   `Meta/*`) by default. **Exception:** Phase 5-6 explicitly create `SuiteTodo/` + `AppTodo/` as the
   reference implementation and update scaffolding; that is the allowed app/suite touch.
5. **No em-dash in prose. No banned NuGet packages (Moq, AutoMapper).** (Org rules.)
6. **Environment:** always `export DOTNET_ROOT="$HOME/.dotnet"` before dotnet/fable/eggshell commands
   (the toolchain apphosts need it; see `LEARNINGS.md`).
7. **Keep `LEARNINGS.md` updated** with anything you got wrong and corrected (CLAUDE.md rule 1).
8. **Accessibility is the default and mandatory — do not be lazy.** Every UI you build or port ships
   accessible across the full spectrum (name+role+state, text scaling, AA contrast and never
   color-alone, ≥44px targets, gesture alternatives, live-region announcements, reduce-motion). Follow
   `ACCESSIBILITY_PLAN.md` (read §13 "pit of success" + §7–§8 recipes). Prefer baking semantics into the
   primitive over per-call patching. Never silently skip a11y; for `[rnw-blocked]`/`[web-only]` bits use
   the portable subset and say what's deferred. (CLAUDE.md rule 12, `.cursor/rules/accessibility-default.mdc`.)
9. **Use the dev runbook for any device/build/observe loop.** Before running/launching/screenshotting/
   tapping/rotating a device, reading runtime errors, killing stale watches, or targeted rebuilds, follow
   `DEV_RUNBOOK.md` (Android/iOS/web inner loop, "verify a patch reached the bundle", gotchas, decision
   tree). Debug with raw `adb`/`simctl`/browser (Tier 1); verify/gate with the `audit/` toolkit
   (`npm run observe -- …`, Appium/Playwright — Tier 2). (CLAUDE.md rule 11,
   `.cursor/rules/runbooks-first.mdc`.)

**Build/validate commands (memorize):**
- Frontend lib type-check/build: `cd <lib> && ../eggshell build-lib` (or from repo per project layout).
- Single framework lib via dotnet: `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"`
  (framework libs use custom configs `Web Debug`/`Web Release`/`Native Debug`/`Native Release`, NOT
  plain `Debug`).
- Full frontend app + gallery: `eggshell dev-web` on `AppEggShellGallery`, then the Playwright gallery
  audit. A LibClient change is NOT validated until `dev-web` is green and the gallery renders.
- Backend lib: `dotnet build <proj>.fsproj`. Backend runtime: the `LibLifeCycleTest` simulation suite.

---

## Phase 1 - Frontend platform bump: Fable 4 -> Fable 5 (+ .NET 10 build host)

**Owner:** weaker model OK, EXCEPT step 1c (plugin) which may need escalation.
**Goal:** the F# frontend compiles and runs under Fable 5 on the .NET 10 SDK, with the same behavior.

### 1a. Install the .NET 10 SDK (one-time, machine setup)
```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
```
**VALIDATE:** `export DOTNET_ROOT="$HOME/.dotnet"; dotnet --list-sdks` must list a `10.0.x` AND
`dotnet --version` must return cleanly (rc 0).
**PITFALL:** on some machines the freshly installed .NET 10 host was SIGKILLed (exit 137) and broke all
`dotnet` calls (the CLI rolls forward to the newest runtime). If `dotnet --version` returns 137 / is
killed, **STOP and escalate** (it is an environment problem, not a code problem).
**NOTE:** `subject/global.json` pins `7.0.300` with `rollForward: minor`, so existing .NET 7 work is
unaffected by a side-by-side .NET 10 install until you change `global.json` (step 1e).

### 1b. Port the custom Fable plugin (`Meta/FablePlugins`) - THE GATE
Nothing else frontend compiles under Fable 5 until this loads. Three known edits:
1. In `Meta/FablePlugins/FablePlugins.fsproj`: `Fable.AST` PackageReference `4.5.0` -> `5.0.0`.
2. In `Meta/FablePlugins/ReactComponent.fs`: `FableMinimumVersion` `"4.0"` -> `"5.0"`.
3. In `Meta/FablePlugins/AstUtils.fs`, the `makeIdent` `Ident` record literal: add the new field
   `IsInlineIfLambda = false` (Fable 5's `Ident` record added it; omitting a record field is a compile
   error).

**VALIDATE:** `export DOTNET_ROOT="$HOME/.dotnet"; dotnet build Meta/FablePlugins/FablePlugins.fsproj`
must be green (stays `netstandard2.0`).
**ESCALATE-IF:** it does not build after these 3 edits (means the Fable 5 AST drifted further than
expected). Do NOT improvise AST changes. Hand back with the FS error. The fallback (re-forking from
upstream Feliz.CompilerPlugins) is a strong-model task.

### 1c. Bump the Fable tool and ecosystem packages
- `.config/dotnet-tools.json`: `fable` `4.18.0` -> `5.4.0`. Then `dotnet tool restore`; confirm
  `dotnet fable --version` prints `5.4.0`.
- Across the frontend `.fsproj`s, bump (one lib at a time, rebuild between):
  - `Fable.Core` `4.x` -> `5.x`
  - `Fable.React` `9.4.0` -> **`10.0.0-alpha.1`** (no 10.0.0 stable exists yet; the alpha is the
    Fable-5 build)
  - `Fable.Promise`, `Thoth.Json` / `Thoth.Json.Net`, `Fable.Browser.*`, `Fable.Date`: bump to the
    latest that **restores**. If a restore fails, find the Fable-5-compatible version on nuget.org;
    do not pin a version you have not confirmed exists.
- `Directory.Build.targets`: bump the pinned `FSharp.Core` if the net10 SDK complains of version skew.
**VALIDATE:** `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"` green, then `eggshell
build-lib` on each touched lib.
**ESCALATE-IF:** a package has no Fable-5 build at all. For **Fable.SignalR specifically, do NOT hunt
for a version** - it is abandoned (see Phase 2; you remove it).

### 1d. Verify the plugin + a real component end to end
Run Fable over a real `LibClient` slice and confirm `[<Component>]` components emit.
```bash
export DOTNET_ROOT="$HOME/.dotnet"
dotnet fable LibClient/src/LibClient.fsproj -o /tmp/fable-validate --noCache
```
**VALIDATE:** "Fable compilation finished", and `.js` emitted for components. (A `FablePlugins` transpile
error at the very end is EXPECTED/benign - the plugin's own source is not meant to be transpiled; see
`LEARNINGS.md` 2026-06-26.)

### 1e. Flip the frontend build TFM/SDK
- `subject/global.json`: bump SDK to `10.0.x` (and widen `rollForward` if needed). Note: this changes
  what the *whole repo* resolves; coordinate with Phase 3.
- Frontend Fable projects can stay `net7.0`/`netstandard2.0` for Fable's purposes, but the SDK must be
  present. `Meta/FablePlugins` stays `netstandard2.0`.

### 1f. Full-app validation
`eggshell dev-web` on `AppEggShellGallery` must compile and serve; run the Playwright gallery audit.
**PITFALL (watch-items, from the issue scan):**
- **`#241` FunctionComponent.Of cache** keyed by source file+line: if two distinct components render
  from the same source line (generic/reused code), they collide. EggShell's normal `[<Component>]` path
  likely avoids this, but if you see the wrong component rendering, this is why - escalate.
- **Array-equality change:** Fable 5.0 changed `Array`/`ResizeArray` equality. If logic that compares
  arrays with `=` misbehaves, this is why - escalate.
- **`voidEl`/void DOM elements** (`br`/`img`/`input`) may emit a trailing `[]` that React rejects on
  web. Workaround: inline the helper.
- After ANY LibClient change, **restart `eggshell dev-web`** (stale `LibStandard/.build/.../fable` is
  what webpack serves; see `LEARNINGS.md`).

---

## Phase 2 - SignalR: replace the dead wrapper with direct bindings

**Owner:** weaker model OK (template exists).
**Goal:** remove `Fable.SignalR` / `Fable.SignalR.AspNetCore` (abandoned, Fable-3-era, private builds)
and use Microsoft's maintained `@microsoft/signalr` (client) + stock ASP.NET Core SignalR (server).

### 2a. Client (frontend)
- Source lives in sibling repo **`../eggshell-signalr/src/LibSignalRClient/`** (not in `subject/Meta/`). Clone `eggshell-signalr` next to `subject/`; see that repo's `README.md` and `NOTICE.md` (MIT, Shmew/Fable.SignalR).
- `@microsoft/signalr` remains in `LibClient/package.json`.
- `LibUiSubject` → `ProjectReference` to `../../eggshell-signalr/src/LibSignalRClient/LibSignalRClient.fsproj`.
**VALIDATE:** `dotnet build ../eggshell-signalr/EggShellSignalR.sln`; `dotnet build LibLifeCycleHost`; `dotnet build LibUiSubject -c "Web Debug"`; `eggshell build-lib` (LibUiSubject); gallery `dev-web` fable watch green. E2E live push still required before merge.

### 2b. Server (backend)
- Source lives in **`../eggshell-signalr/src/LibSignalRServer/`** (typed `FableHub` + streaming; EggShell
  `CancellationToken` on `streamFrom`). Not the thin stock hub from `eggshell-rnw-spike/signalr-server/`
  (that spike only proved transport viability).
- `LibLifeCycleHost` → `ProjectReference` to `../../eggshell-signalr/src/LibSignalRServer/LibSignalRServer.fsproj`.
- Keep wire contract (`/api/v1/realTime`, message names) unchanged.
**VALIDATE:** backend builds; a client connects and receives a pushed update end to end (the spike
proved counter 251 -> 254). 
**ESCALATE-IF:** the typed-hub message contract was relied on in a way that does not map cleanly to a
stock hub. This is a small design call - escalate if unsure.

---

## Phase 3 - Backend .NET 7 -> .NET 10 (Orleans FROZEN at 3.7.2)

**Owner:** weaker model OK for the bumps; escalate on runtime/serialization or ASP.NET Core breaks.
**Goal:** backend targets `net10.0` with Orleans still `3.7.2`. (Proven viable by the spike: builds and
runs, grain round-trip + your real exception types propagate, no BinaryFormatter throw.)

### 3a. TFM + SDK
- `global.json` SDK -> `10.0.x` (shared with Phase 1e).
- Flip every backend `<TargetFramework>` `net7.0` -> `net10.0` (one project at a time, build between).
  Leave `Meta/FablePlugins` at `netstandard2.0`.

### 3b. Known package fixes (do these first)
- `LibLangFsharp/src/LibLangFsharp.fsproj`: remove or bump the stray `System.Text.Json` `5.0.2` pin
  (let the framework supply STJ, or bump to the net10 version). **Highest-value, do first.**
- `FSharp.Core` pin in `Directory.Build.targets`: bump if the net10 SDK warns (and `--warnaserror+` is
  on, so warnings can fail the build).
- `Giraffe` `6.2.0` (+ `Giraffe.TokenRouter` alpha) vs ASP.NET Core 10: bump Giraffe to a version that
  targets ASP.NET Core 10; expect possible source-level API fixes.
- `Microsoft.Data.SqlClient` `5.2.3`: bump to current if needed.
- Resolve `Microsoft.Extensions.*` version-unification conflicts (Orleans 3.7 wants 6.x; the net10 host
  wants 10.x). Let the higher win; fix DI/hosting abstraction mismatches if they appear.
**VALIDATE:** each backend project builds with `dotnet build`.
**PITFALL:** Orleans 3.7.2's legacy `OrleansCodeGenerator.MSBuild` + the C# `*Build.csproj` codegen
projects are the "blocked at the door" risk. The spike proved they DO build on the net10 SDK, so if they
fail here it is likely a local SDK/config issue - escalate with the build error.

### 3c. Runtime validation
Run the `LibLifeCycleTest` simulation suite (in-memory Orleans `TestingHost`, no SQL needed).
**VALIDATE:** grain activation, transitions, subscriptions, and especially **exception propagation
across grains** all pass with no `PlatformNotSupportedException` / BinaryFormatter error.
**ESCALATE-IF:** any BinaryFormatter / serialization runtime failure. The spike showed the common paths
and your real exception types are fine, but an exotic `ISerializable` type with custom serialized fields
could still hit it - that is a design call, escalate.
**OUT OF SCOPE (do NOT attempt):** SQL Server storage/clustering on this machine (Mac ARM SQL issues),
and the PostgreSQL conversion. Those belong to the separate, later Orleans-upgrade + Postgres workstream
(see strategy doc Section 17 / Update Log 9). Use in-memory storage for validation here.

### 3d. net10 specifics from research (2026-06-28) - apply these, in this order

1. **Tooling first:** keep `global.json` `10.0.301` (`rollForward: latestFeature`). Confirm the whole
   solution still builds on net7 under the net10 SDK before flipping any TFM (isolates SDK issues).
2. **Add Central Package Management** (`Directory.Packages.props`) with
   `CentralPackageTransitivePinningEnabled=true`, and **pin ALL `Microsoft.Extensions.*` to one 10.x
   version** (DI, Hosting, Logging, Options, Configuration, ObjectPool, all `.Abstractions`). Orleans 3.7
   pulls 6.x transitively; NuGet picks lowest-applicable, so without this you get `MissingMethodException`
   on `BuildServiceProvider`. This is the #2 risk.
3. **Silo host (the #1 runtime risk):** net9 P7+ turns on `ValidateOnBuild` + `ValidateScopes` by default
   in Development; Orleans 3.7's open-generic / scoped-in-singleton registrations were never validated and
   will throw at startup. Add `UseDefaultServiceProvider(fun o -> o.ValidateScopes <- false; o.ValidateOnBuild <- false)`
   BEFORE first run.
4. **F# default LangVersion jumps to F# 10** on the net10 SDK. Expect source breaks: **FS0842**
   (misapplied attribute, e.g. `[<Fact>] let x = ...` needs `()`; this is the same family as the existing
   `SubjectTypes.fs` FS842), **FS0058** (pseudo-nested modules in types), **FS3873** (bare `{ 1..10 }`),
   and tightened `#nowarn` syntax. Per-project escape hatch: `<LangVersion>9</LangVersion>`. NRT stays OFF
   by default (a retarget does not introduce nullability errors).
5. **`--warnaserror` will go red:** retargeting bumps analysis level; new SYSLIB obsoletions
   (SYSLIB0050/0051 legacy serialization, 0057 X509 ctors, 0058-0062) + warning waves + NuGetAudit on
   Orleans 3.7's transitive graph become errors. Stage a `<WarningsNotAsErrors>` list (keeps them visible)
   rather than `<NoWarn>`; suppress SYSLIB by exact id (not CS0618).
6. **Package cascade:** Giraffe -> **8.2.0** (removed Newtonsoft, STJ default, JSON via `Json.ISerializer` -
   re-wire DI), `Microsoft.Data.SqlClient` -> **7.0.2** (`Encrypt=true` default since 4.0 -> set
   `TrustServerCertificate=True` dev or `Encrypt=False`), `FSharp.Core` SDK-bundled 10.x (do not pin),
   `System.Text.Json` 10.x in-fx (cache `JsonSerializerOptions`), **Fleece 0.10.0 is stale (2022, no net10
   build) - smoke-test it**, BinaryFormatter stays removed/throws (you are shielded; do NOT add the compat
   package).
7. **Sequence:** SDK-first -> CPM/Extensions pinning + WarningsNotAsErrors -> pilot one leaf lib ->
   flip TFMs leaf-first / host-last -> fix cascade -> flip silo host last (with the DI-validation opt-out)
   -> full test suite + live silo smoke (grain round-trip, exception propagation, SIGTERM shutdown; note
   net10 no longer installs a default SIGTERM handler).
8. **Re-confirm via CI gate:** Orleans 3.7.2 has no vendor-documented net10 support, so pin the proven
   build + grain round-trip behind a CI smoke test (`OrleansCodeGenLogLevel=Trace`) so a transitive bump
   cannot silently regress it. Full research with sources is in the strategy doc / research notes.

---

## Phase 4 - ReactXP -> react-native-web seam

**Owner:** STRONG model sets the pattern + does the first primitive + the style/animation layers.
Weaker model fans out the remaining primitive wrappers ONCE the pattern is proven.
**Goal:** re-implement `LibClient/src/ReactXP/*` against RN (native) + react-native-web (web), keeping
the F# public surface (`RX.*`/`LC.*`, the `makeViewStyles` DSL) identical.

**Do NOT start the fan-out until a stronger model has:**
1. Re-pointed `ReactXPBindings.fs` from `@chaldal/reactxp` to `react-native` (+ bundler alias
   `react-native` -> `react-native-web` for web).
2. Ported ONE primitive end to end (e.g. `View`) as the reference pattern, validated in `dev-web` +
   native.
3. Re-targeted the `Styles/{Legacy,New}` DSL to emit RN style objects, and decided web-only vs native
   handling for nested selectors/breakpoints.
4. Established the animation layer (Reanimated 4 / RNGH 3 / Moti) F# wrappers - keep app code
   declarative; hand-authored worklets, if any, follow the spike pattern (they worked from F# directly,
   no JS shim needed).

**Then, fan-out recipe per primitive** (`Text`, `Button`->`Pressable`, `ScrollView`, `Image`,
`TextInput`, `GestureView`, `Picker`, `Link`, `VirtualListView`, `WebView`, `Animatable*`):
1. Match the reference primitive's structure exactly.
2. Keep the F# signature identical (same optional args, including `?xLegacyStyles` if still bridged -
   **do not drop it**; dropping `?xLegacyStyles` broke callers before, see `LEARNINGS.md`).
3. Keep public DU/record type paths unchanged (moving them breaks `.render`/app callers - see
   `LEARNINGS.md`).
4. VALIDATE in `dev-web` + a native run before the next primitive.
**ESCALATE-IF:** a primitive has no clean RN/RNW equivalent, or a style feature does not map. Design
call.
**FORWARD NOTE:** keep the F# API DOM-flavored (onClick/aria/css-style) so a later switch to React
Strict DOM stays a thin re-point (strategy doc Section 16).

**Knowledge from the a11y/theming work done on the current ReactXP seam (already Fable 5.4.0 / .NET 10;
carry into the seam port):**
- **Preserve the accessibility prop surface verbatim.** The current bindings already expose RN-native
  a11y props — `LC.Pressable(label, role, state, liveRegion, importantForAccessibility, tabIndex,
  actions)` and `RX.View(accessibilityLabel/Role/State/LiveRegion, importantForAccessibility,
  accessibilityActions, ariaRoleDescription)` plus `LibClient.Accessibility` types. The RNW port maps
  these to RN `role`+`aria-*` (cross-platform) / real ARIA; **do not rename or drop them** (it is the
  a11y migration-safety contract — `ACCESSIBILITY_PLAN.md` §3/§14). Adopting RN's newer unified `role` +
  `aria-*` props underneath is a seam-internal upgrade the call sites never see.
- **Preserve themeable input fields.** `Input.Text.Theme` gained `EditableBackgroundColor` /
  `LabelBackgroundColor`; `PickerInternals.Field.Theme` gained `BackgroundColor` / `BorderRadius` /
  `LabelBackgroundColor` (Fable-4 change so dark mode works — they were hardcoded `Color.White`). Keep
  these fields in the RN port; default them to white for back-compat (`LEARNINGS.md` 2026-06-28).
- **Metro bundles ReactXP from `dist/native-common/*.js`, NOT `src/*.tsx`.** When re-pointing
  `ReactXPBindings.fs` and removing `@chaldal/reactxp`, verify the bundle actually changed
  (`DEV_RUNBOOK.md` §7.4). The old `LibClient/vendor/reactxp-native-common/` + `postinstall` copy
  mechanism goes away with ReactXP.
- **Fix render-hygiene at the F# seam, never by patching vendored ReactXP.** Example: the dev-only
  "unique key" warning was Fable.React `contextProvider` not keying children — fixed in
  `AppShell/Context/Context.fs` with `tellReactArrayKeysAreOkay`, not in node_modules
  (`LEARNINGS.md` 2026-06-29).
- **Android `borderRadius` needs `Overflow.Hidden`** on filled views to clip the background; **`LC.Row`
  appends its own `FlexDirection.Row` last** (use `RX.View` + a direction-correct style for responsive
  stacking). Re-verify these behave the same (or better) on RN/RNW after the port.

---

## Phase 5 - Full-stack TODO reference app + templatized bootstrap (goal B)

**Owner:** weaker model for mechanical bulk; escalate on codec-gen / Orleans wiring / native template
parameterization.
**Sequencing:** after Phases 1-3 (Fable 5 + net10 + SignalR). **Before** Phase 4 (RNW seam). Phase 5
proves the stack on the current ReactXP seam; Phase 4 re-points the seam without re-proving app architecture.
**North star:** one command from fresh scaffold to running web stack; documented three-terminal recipe for
native; CI runs simulation + Playwright.
**Strategy doc:** `FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md` Section 21 (feature matrix + DX target).
**In-repo references only:** `SuiteJobs` (backend), `AppEggShellGallery` (frontend, native, Playwright audits).
Do not name or link external legacy repos in generated template code or app sources.

### Feature coverage checklist (must demonstrate)

Use this as the acceptance checklist for the reference `SuiteTodo` app AND the templatized output:

| Area | Must show |
|---|---|
| Lifecycle | Subject CRUD, OpErrors, LifeEvents, constructor validation |
| Projection | `View` returning a list read model consumed by the frontend |
| Timer | Auto-archive after N minutes Done; proven via `moveTimeForwardAndRunReminders` |
| Simulation | `LibLifeCycleTest` tests for actions + timer (no UI) |
| HTTP API | V1 generic endpoints for actions |
| Real-time | SignalR view subscription; two browser tabs stay in sync |
| Frontend subscription | `AsyncData`, `With.Subjects`, reconnect on session change |
| Routing | Typed routes, nav shell |
| Dialog / forms | Edit todo; empty-title validation |
| Accessibility | `A11ySlug.testId` on list items, inputs, buttons, dialog actions |
| Observability | `UiActionLog` on FE; structured backend logs on lifecycle transitions |
| Web | `eggshell dev-web` green |
| Native | `eggshell dev-native` + Metro + Android emulator + iOS simulator smoke |
| UI automation | Playwright web audit script; simulation tests in CI |
| Scaffold | `eggshell create-app` reproduces the above (minus SuiteTodo-specific naming) |

---

### 5A. Backend Todo ecosystem (build to green first)

Mirror `SuiteJobs/Ecosystem` layout:

```
SuiteTodo/
  Ecosystem/
    Todo.Types/          # types + EcosystemDef + codec-gen input
    LifeCycles/          # TodoLifeCycle, AllLifeCycles, View
    Tests/               # Simulation + unit tests
  Launchers/Dev/
    TypesCodecGen/       # codec generation launcher
    Host/                # Dev silo + V1 API + SignalR (Phase 5B)
```

1. **Types** (`Todo.Types/`): hand-written declarations only (mirror `SuiteJobs/.../Job.fs` shapes):
   - `TodoId`, `Todo` record (`Title: NonemptyString`, `Done`, `ArchivedOn`, `CreatedOn`)
   - `TodoAction`: `SetTitle | ToggleDone | Archive | Delete`
   - `TodoConstructor`: `New of Title`
   `TodoLifeEvent`: `Created | TitleChanged | DoneToggled of bool | Archived`
   - `TodoOpError`: `EmptyTitle`
   - Index types + `TodoIndex()` inheriting `SubjectIndex<...>` — include **`TodoSearchIndex.Title`**
     (SQL Server full-text; not `NoSearchIndex`) so Dev host creates FTS catalog/tables
   - `EcosystemDef.fs`: `newEcosystemDef "Todo"` + life cycle registration
2. **Codec gen:** wire `TypesCodecGen` Dev launcher (mirror `SuiteJobs/Launchers/Dev/TypesCodecGen`).
   Run it; commit generated `#if !FABLE_COMPILER` codec blocks. **ESCALATE-IF** wiring is unclear.
3. **LifeCycle** (`LifeCycles/`):
   - `LifeCycleBuilder` + `ViewBuilder` (mirror `JobsLifeCycleBuilder.fs`)
   - `construction`: reject empty title; emit `Created`
   - `transition`: actions update subject + emit LifeEvents
   - **View** `TodoListView`: projection of active todos (sorted by `CreatedOn`)
   - **Timer**: when `Done && not Archived`, schedule archive after N minutes (`LifeCycleBuilder.withTimers`)
   - `AllLifeCycles.fs` registration
4. **Tests** (`Tests/`): mirror `SuiteJobs/Ecosystem/Tests/Simulation.fs`:
   - Simulation builder with ecosystem initializer
   - Test: construct todo, toggle, assert LifeEvent + state
   - Test: `EmptyTitle` rejected
   - Test: `Ecosystem.moveTimeForwardAndRunReminders` fires auto-archive timer
   - Test: `IndexPredicate.Matches` on `TodoSearchIndex.Title` (full-text search)

**VALIDATE:** `dotnet build` each project green; `dotnet test SuiteTodo/Ecosystem/Tests` passes.

---

### 5B. Backend Dev host (V1 HTTP + SignalR)

Add `SuiteTodo/Launchers/Dev/Host/` mirroring an existing in-repo Dev launcher pattern:

- Registers `SuiteTodo` ecosystem on `LibLifeCycleHost`
- Exposes V1 generic HTTP API + `/api/v1/realTime` SignalR (via `eggshell-signalr` server)
- **Phase 5:** in-memory Orleans storage (fast, no Docker)
- `appsettings.Development.json` with URLs the frontend `Config.BackendUrl` expects

**VALIDATE:** host starts; negotiate endpoint responds; HTTP view query returns JSON; SignalR push works
(add todo in one client, subscription updates in another).

---

### 5C. Frontend TODO app (`SuiteTodo/AppTodo/`)

Pure F# only. Connection standard (encode inline, no external references):

**Config.fs:** `AppUrlBase`, `BackendUrl`, `withOverrides` from `configSourceOverrides.*.js`.

**AppServices.fs `initialize`:**
- `EventBus()`, `HttpService`, `ThothEncodedHttp`, `PageTitle`, `Image`
- `RealTimeService(eventBus, config.BackendUrl)` (Phase 2 SignalR client)
- Todo subject services via `makeSubjectServices` + `provideInstances`
- `LibClient.ServiceInstances.provideInstances { ... }`

**Components:**
- `Route/Todos.fs`: subscribe to `TodoListView` via `LC.With.Subjects`; render `AsyncData` (loading,
  empty, error, list); add-todo input; per-row toggle/edit/archive/delete
- `Dialog/EditTodo.fs`: edit title with validation feedback
- `Route/Settings.fs`: minimal settings (archive delay display, backend URL readout for dev)
- Every interactive control: `?testId` via `A11ySlug.testId "todo-*" ...`
- User actions: log via `UiActionLog` (mirror gallery patterns)

**Native files:** copy gallery `android/` + `ios/` structure into `AppTodo/`, parameterized at template
time (`AppName`, bundle id placeholders). Ship `configSourceOverrides.native.js.template`.

**VALIDATE:**

| Check | Command / action |
|---|---|
| Web build | `cd SuiteTodo/AppTodo && eggshell dev-web` |
| CRUD + live push | Browser: add/toggle/edit; second tab updates without refresh |
| Timer | Mark done, wait or shorten N in dev; item archives via subscription |
| Native Fable | `eggshell dev-native` compiles |
| Android smoke | Metro + `npx react-native run-android` (emulator) |
| iOS smoke | Metro + `npx react-native run-ios --no-packager` (simulator) |

For the **native dev/observe loop** (boot emulator/sim, `adb reverse`, launch/reload, screenshot,
tap/rotate, read `logcat`/`simctl log`, kill stale watches, targeted rebuilds, fix the AVD
sideways-orientation gotcha) follow `DEV_RUNBOOK.md` rather than improvising. Confirm a green Fable build
actually reached the device (reload; Metro `--reset-cache` if stale).

---

### 5D. UI automation (`SuiteTodo/AppTodo/audit/`)

> **A richer observe toolkit already exists** in `SuiteTodo/AppTodo/audit/` (`todo-observe.mjs`,
> `npm run observe -- doctor|snapshot|add-todo|workflow|diff`, plus `audit-todo-web.mjs`). It drives
> native via **Appium** and web via **Playwright**, by **`testId`**, emitting structured JSON
> (layout-metrics, DOM/hierarchy, health, classified logs) for LLM/CI consumption — see `DEV_RUNBOOK.md`
> §0/§6 (Tier 2) and `audit/README.md`. Use/extend it; the scripts below are the minimum the template
> must ship.

Mirror `AppEggShellGallery` audit scripts (adapt selectors to todo testIds):

1. **`audit-todo-web.mjs`** (Playwright):
   - Preconditions: dev-web running on `:9080`, backend host running
   - Add todo, assert row appears
   - Toggle done, assert UI state
   - Open edit dialog, change title, save
   - Assert no console errors; capture screenshot on failure
2. **`audit-todo-android.mjs`** (optional v1): adb launch + logcat grep for `Running "RXApp"` (gallery pattern)
3. **`.github/workflows/todo-app.yml`** (in template):
   - Job 1: `dotnet test` simulation
   - Job 2: scaffold smoke (`eggshell create-app TodoCI` in temp dir -> build)
   - Job 3: Playwright against reference `SuiteTodo/AppTodo` (not ephemeral scaffold until stable)

**VALIDATE:** `node audit/audit-todo-web.mjs http://127.0.0.1:9080` passes with stack running.

---

### 5E. Templatize into `Meta/LibScaffolding` (after 5A-5D green)

**Rule:** templatize the working `SuiteTodo/AppTodo`, do not patch old templates incrementally.

**`dev-stack` script (template `dev-stack.sh.template`):**

```bash
#!/usr/bin/env bash
# Starts docker SQL (Phase 6), backend host, dev-web — one command for web dev
set -euo pipefail
docker compose up -d sql          # no-op until Phase 6; stub with in-memory message
./Launchers/Dev/Host/run.sh &     # backend
eggshell dev-web                  # foreground
```

Native variant documents the three-terminal flow (gallery README table).

**Template buckets** (see prior Phase 5D disposition list in runbook history):

- **KEEP + modernize:** app shell, Bootstrap, Config, Services, Navigation, I18n, Icons, ErrorMessages
- **REPLACE:** Landing/Bananas/Mangoes/DoSomething -> Todo routes + EditTodo dialog
- **DELETE:** all `.render` templates; Chaldal-specific lib refs; stale `obj/`/`bin/` in templates
- **ADD:** `audit/`, `docker-compose.yml.template` (Phase 6), `android/`/`ios/` skeletons,
  `configSourceOverrides.native.js.template`, `dev-stack.sh.template`, GitHub workflow template

**Scaffolding TS updates** (`Meta/LibScaffolding/src/tasks/createApp.ts`):
- Parameterize suite name (`SuiteTodo` default)
- Emit pure F# route/component templates only
- Auto-update `.fsproj` on `create-component` / `create-route`

**VALIDATE:** `eggshell create-app TodoSmoke` in temp directory -> `./initialize` -> `./dev-stack up`
-> Playwright smoke passes.

---

### 5F. Accessibility and observability standards (template enforces)

**Accessibility (required in generated components — full spectrum, not just testIds):**
testIds are for *automation hooks*, not accessibility. Generated apps must be **accessible by default**
per `ACCESSIBILITY_PLAN.md` (mandatory — Golden Rule 8). Required in every generated component:
- **Name + role + state** on every control via `LC.Pressable(label, role, state)` /
  `RX.View(accessibilityLabel/Role/State)`; the visible text is contained in the accessible name.
- **Decorative icons hidden** (`importantForAccessibility = No`); never the sole content of a control.
- **Text scales** with OS font size without clipping (keep `allowFontScaling`; no clipping fixed heights).
- **Color meets WCAG AA** and is never the only signal (priority/status pair color with text/icon).
- **Targets ≥44px**; **every gesture has a non-gesture alternative** (tap/keyboard/rotor action).
- **Dynamic changes announce** via a live region (counts, validation, "deleted X").
- **Plus** the automation hooks: `?testId = Some (A11ySlug.testId "<slug>" labelOrValue)`; dialog actions
  use stable slugs (`todo-edit-save`, `todo-edit-cancel`); skeleton/empty states expose `testId`.
- **Highest leverage:** bake role/state into the primitives (`Tab`/`Checkbox`/`Picker`/`Button`/
  `Heading`) so generated apps get semantics free (`ACCESSIBILITY_PLAN.md` §9 #9). This is `[safe]` —
  do it on the current ReactXP seam; it carries through Phase 4.
- **Benchmark:** `SuiteTodo/AppTodo/suggestions/ui2.html` (a11y-rich light+dark mockup).
- Reference: `ACCESSIBILITY_PLAN.md` §7–§8 (recipes + per-component playbook), gallery converted
  components, `LEARNINGS.md` testId slugs.

**Observability (required in generated app):**
- Frontend: `UiActionLog.logUserAction` on create/toggle/edit/archive (with testId)
- Backend: lifecycle transition logs at Debug for action + LifeEvent (use existing `IFsLogger` patterns)
- Dev host: optional Application Insights stub in `appsettings` (connection string empty by default)
- Template README section: "Tracing a user action end to end"

---

### 5G. Smoke gate (CI must not rot)

Minimum CI for the monorepo (add to existing pipeline or new workflow):

1. `dotnet build eggshell-signalr/EggShellSignalR.sln`
2. `dotnet test SuiteTodo/Ecosystem/Tests`
3. `eggshell create-app TodoCI` (temp dir) -> `dotnet build` + `eggshell build-lib`
4. Playwright audit against pinned `SuiteTodo/AppTodo` with services up

**ESCALATE-IF:** codec-gen / Orleans codegen for new suite; Giraffe/API wiring; native template
parameterization beyond search-replace; Playwright flakes without deterministic testIds.

---

## Phase 6 - Docker SQL Server + persistent dev stack

**Owner:** weaker model OK.
**Sequencing:** after Phase 5 reference app is green on in-memory storage.
**Goal:** `./dev-stack up` brings up SQL Server in Docker, Orleans uses ADO.NET persistence, Mac ARM dev
is unblocked without native SQL install.

### 6a. Docker compose

Add to `SuiteTodo/` (and template `docker-compose.yml.template`):

```yaml
services:
  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "EggShell_Dev_123!"   # dev only; document in README
    ports:
      - "1433:1433"
    volumes:
      - todo-sql-data:/var/opt/mssql
volumes:
  todo-sql-data:
```

**Mac ARM note:** use Azure SQL Edge if standard SQL Server image fails on ARM (`mcr.microsoft.com/azure-sql-edge`).

### 6b. Orleans ADO.NET clustering + storage

- Update Dev host `appsettings.Development.json` connection string -> `localhost,1433`
- Wire Orleans ADO.NET clustering + grain storage (mirror K8S/Fabric patterns simplified for Dev)
- Run DB init/migrations script in `sql/init.sql` (create database, Orleans tables if not auto-created)

**VALIDATE:** `docker compose up -d sql`; host starts; grain state survives host restart; simulation
tests still pass (may use in-memory override in Tests project).

### 6c. Fold into one command

Update `dev-stack.sh`:
1. `docker compose up -d sql` + wait-for-health script
2. Start backend host (SQL connection)
3. Start `eggshell dev-web`

Optional `eggshell dev-stack` CLI command wraps the same (later).

**VALIDATE:** fresh clone -> `./initialize` -> `./dev-stack up` -> todo CRUD persists across host restart.

**OUT OF SCOPE:** Postgres migration, production K8S/Fabric deploy templates, SQL Server on CI (use
in-memory or container service in CI if needed).

---

## Consolidated escalation triggers (hand back to a stronger model)

- `Meta/FablePlugins` does not build after the 3 known edits (AST drift).
- Any Fable compile error not in a PITFALL list; the `#241` cache or array-equality symptoms.
- Backend runtime serialization / BinaryFormatter failure; exotic `ISerializable` types.
- ASP.NET Core 10 API breaks beyond a version bump.
- The SignalR typed-hub contract not mapping cleanly to a stock hub.
- ANY part of the Phase 4 seam design (pattern-setting, style DSL, animation layer, a primitive with no
  clean equivalent).
- Anything where the fix is not obvious in one attempt. Do not guess.

## Reference artifacts (copy-from)

- **SignalR (modular):** `../eggshell-signalr/` (`LibSignalRClient`, `LibSignalRServer`, MIT + NOTICE)
- **Backend patterns:** in-repo `SuiteJobs/` (lifecycle, view, timer, simulation tests, codec-gen)
- **Frontend + native + audit:** in-repo `AppEggShellGallery/` (`GALLERY-AUDIT.md`, `audit-gallery-*.mjs`,
  `android/`, `ios/`, native README)
- **Target output:** `SuiteTodo/` (Phase 5 reference; becomes scaffold source)
- Frontend spike: `../eggshell-rnw-spike/` (transport + animation probes only)
- Backend spike: `../orleans-net10-spike/` (Orleans 3.7.2 on net10)
- Strategy: `FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md` Section 21 (feature matrix + one-command DX)
- Conversion recipe + pitfalls: `LEARNINGS.md`
