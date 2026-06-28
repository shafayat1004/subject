# EggShell Modernization Runbook (executor edition)

> **Who this is for:** an executor model doing the mechanical bulk of the .NET 10 + Fable 5 +
> react-native-web modernization. It is a step-by-step companion to the strategy doc
> `FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md` (read that for the *why*; this is the *how*).
>
> **Scope:** this is the **later** initiative (architecture goals F/G/H). The repo's *current* phase is
> A-E on Fable 4 (see `subject/CLAUDE.md`). **Do not start this runbook unless explicitly told the
> modernization phase has begun.**
>
> **Proven on spikes (do not re-litigate):** the whole path is already validated end to end in
> `/Volumes/HomeX/shafayat/Code/eggshell-rnw-spike` (frontend: Fable 5 + RNW + Reanimated + RNGH + Moti +
> SignalR) and `/Volumes/HomeX/shafayat/Code/orleans-net10-spike` (backend: Orleans 3.7.2 on .NET 10).
> Use those as copy-from references.

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
   `Meta/*`). Do not touch `App*`/`Suite*` unless explicitly told (per `CLAUDE.md` rule 8).
5. **No em-dash in prose. No banned NuGet packages (Moq, AutoMapper).** (Org rules.)
6. **Environment:** always `export DOTNET_ROOT="$HOME/.dotnet"` before dotnet/fable/eggshell commands
   (the toolchain apphosts need it; see `LEARNINGS.md`).
7. **Keep `LEARNINGS.md` updated** with anything you got wrong and corrected (CLAUDE.md rule 1).

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

---

## Phase 5 - Full-stack TODO example app + scaffold modernization (goal B + the validation benchmark)

**Owner:** weaker model for the mechanical bulk; escalate on the codec-gen / Orleans-codegen wiring and
any framework-API mismatch. **Sequencing:** this is goal-B work and the migration's validation benchmark;
do it AFTER Phases 1-3 (Fable 5 + net10 + SignalR) and BEFORE Phase 4 (RNW). **Build order within the
phase: build the concrete app to green FIRST, then templatize it** (a correct template is, by definition,
the canonical modern app).
**Goal:** a working full-stack TODO app - a backend Todo subject lifecycle (CRUD + a timer auto-update)
and a frontend that does CRUD and subscribes for live updates - then folded into `eggshell create-app` as
the default modern app.
**Reference:** model the backend on the in-repo **SuiteJobs** ecosystem (current framework API). Do NOT
add references to any out-of-repo example project anywhere in this repo; the conventions are spelled out
inline below.
**Domain:** Subject `{ Id; CreatedOn; Title: NonemptyString; Done: bool; ArchivedOn: Option<DateTimeOffset> }`;
Constructor `New of Title`; Actions `SetTitle | ToggleDone | Archive | Delete`; LifeEvents
`Created | TitleChanged | DoneToggled of bool | Archived`; OpError `EmptyTitle`; a View listing todos; a
timer auto-archiving todos that have been Done for > N minutes (the automated-update signal).

### 5A. Backend Todo ecosystem (build to green first)
Mirror `SuiteJobs/Ecosystem` shape: `SuiteTodo/Ecosystem/{Todo.Types, LifeCycles, Tests}`, three net7.0
`.fsproj` with the same package/project references SuiteJobs uses.
1. **Types** (`Todo.Types/`): `Common.fs` (the `TodoId` wrapper) + `Todo.fs` with HAND-WRITTEN
   declarations only (mirror `SuiteJobs/.../RecurringJob.fs` shapes):
   `[<AutoOpen; CodecLib.CodecAutoGenerate>] module SuiteTodo.Types.Todo`; the `Todo` record with
   `interface Subject<TodoId> with member SubjectCreatedOn / SubjectId`; the Action/Constructor/LifeEvent/
   OpError DUs each `with interface LifeAction/Constructor/LifeEvent/OpError`; the five index types
   (start `NoNumericIndex`/`NoSearchIndex`/etc.) + `type TodoIndex() = inherit SubjectIndex<...>()`;
   `EcosystemDef.fs` = `newEcosystemDef "Todo"` + `addLifeCycleDef ... "Todo"` + the
   `{| EcosystemDef = ...; LifeCycles = {| todo = ... |} |}` record (mirror SuiteJobs `EcosystemDef.fs`).
2. **GENERATE CODECS - do NOT hand-write them.** Each Types file carries a generated
   `#if !FABLE_COMPILER ... codec { ... } ... #endif` block produced by the codec-gen tool. Wire a
   `TypesCodecGen` Dev launcher for SuiteTodo (mirror `SuiteJobs/Launchers/Dev/TypesCodecGen`) and run it;
   it appends the codec block. **ESCALATE-IF the codec-gen wiring is unclear - it is the fiddliest part of
   standing up a new ecosystem.**
3. **LifeCycle** (`LifeCycles/`): a builder module (mirror `JobsLifeCycleBuilder.fs`:
   `LifeCycleBuilder.newLifeCycle<...,NoSession,NoRole> def` and `ViewBuilder.newView<...,NoSession,NoRole>`);
   `TodoLifeCycle.fs` with `construction { ... }` (reject empty title -> `TodoOpError.EmptyTitle`; emit
   `Created`) and `transition { ... }` (SetTitle/ToggleDone/Archive/Delete -> new subject + LifeEvents); a
   **View** projecting the todo list; a **Timer** auto-archiving todos Done > N minutes; `AllLifeCycles.fs`
   registration.
4. **Validate:** `export DOTNET_ROOT="$HOME/.dotnet"`; `dotnet build` each project green; run the Tests
   project simulation suite (mirror `SuiteJobs/Ecosystem/Tests` `Simulation.fs` + a test): construct,
   act ToggleDone/SetTitle and assert state + LifeEvent; assert `EmptyTitle` on bad input; a
   `moveTimeForwardAndRunReminders` test asserting the auto-archive timer fires (backend-level real-time proof).

### 5B. Backend host/launcher (serve the V1 API + realtime SignalR)
Add a Dev launcher that hosts the Todo ecosystem on the framework host (`LibLifeCycleHost`), exposing the
V1 generic HTTP API (request/response) and the realtime SignalR endpoint (subscriptions). Mirror an
existing in-repo Dev launcher. **Validate:** silo starts; the SignalR negotiate endpoint responds; a view
query returns over HTTP.

### 5C. Frontend TODO app (CRUD + live subscriptions) - the connection standard
Encode these conventions (do not name any external project):
- **`Config.fs`**: a `ConfigSource` record with at least `AppUrlBase` and `BackendUrl`, a `.Base` default,
  and `withOverrides` reading `configSourceOverrides.*.js`. `BackendUrl` points the app at 5B.
- **`AppServices.fs` `initialize config`**: build `EventBus()`; construct
  `HttpService(eventBus, <staticResourceSettings>, (fun url -> url.StartsWith config.BackendUrl), <hashedDirPrefix>)`;
  `LibClient.ServiceInstances.provideInstances { EventBus; Date; Http; ThothEncodedHttp; PageTitle; Image }`.
  For live data: `RealTimeService(eventBus, config.BackendUrl)` + the Todo ecosystem's subject services
  (`makeSubjectServices`) + that ecosystem's `provideInstances`.
- **Components** (pure F# `[<Component>]`, NO `.render`): a TODO list route that subscribes to the Todo
  view via `LC.With.Subject` / `With.Subjects` and renders `AsyncData<'T>`
  (`Uninitialized | Fetching | Available | Error`); an add-todo input; per-row toggle/edit/delete that call
  the backend actions through the subject service. CRUD goes through the HTTP request/response path; live
  updates arrive via the subscription with no manual refresh.
- **Validate:** `eggshell dev-web` green; in-browser add/toggle/edit/delete works; two tabs show live
  propagation; the auto-archive timer visibly updates a Done item with no refresh (the subscription proof).

### 5D. Templatize into the scaffold (modernize `create-app`)
Fold 5A-5C into `Meta/LibScaffolding/templates` as the DEFAULT generated app, modernizing the scaffold
(see strategy doc Section 8):
- `eggshell.json.template`: flat `renderDependencies` -> nested
  `render.{ dependenciesToRtCompile, additionalModulesToOpen, componentLibraryAliases, componentLibraryPaths, componentAliases }`.
- routes/components/dialogs: pure-F# templates; DELETE the `.render` templates (`route/Route.render.template`,
  `dialog/Dialog.render.template`, the `.typext.fs`/`.styles.fs` pairs) and emit pure F#.
- decouple from Chaldal: remove `Bananas`/`Mangoes`/`Landing`/`DoSomething` and company-lib coupling; the
  generated app is the generic TODO.
- `package.json.template`: add the `webpack-dev-server` dev-web start path; drop dead `file:` deps + mobile-only bloat.
- update the scaffolding TS tasks (`Meta/LibScaffolding/src/tasks`) for changed placeholders/structure;
  auto-update the `.fsproj` on `create-component`.

**Disposition of the EXISTING template files** (in `Meta/LibScaffolding/templates/`) - do not start from
scratch; most of the app shell is reusable. Three buckets:

- **KEEP + modernize** (the app shell / chrome / connection wiring - update to current conventions, make
  sure each is pure F#, and re-point at the Todo routes/ecosystem): `app/src/App.fsproj` (fix the
  `<Compile>` includes; remove demo + any `.render`/`_autogenerated_` entries), `Bootstrap.fs.template`,
  `Config.fs.template` (add `BackendUrl`/`AppUrlBase` per 5C), `Services.fs.template` (the `initialize`
  connection wiring per 5C), `Navigation.fs.template` (point at the Todo routes), `Components/App.fs.template`,
  `Components/AppContext.fs.template`, `Components/Nav/Top.fs.template`, `Components/Sidebar.fs.template`,
  `Components/With/Subjects.fs.template` (adapt to subscribe to the Todo view), `ComponentsHierarchy`,
  `ComponentsTheme`, `Colors`, `Icons` (+ `IconSources/`), `ErrorMessages`, `I18n/{En,Bn}`,
  `Services/SessionService.fs.template`.
- **REPLACE with TODO content:** `Components/Route/Landing.fs.template` -> the Todo list route (subscribe to
  the Todo view, render `AsyncData`, add/toggle/edit/delete); **delete** `Components/Route/Bananas.fs.template`
  and `Mangoes.fs.template` and `Components/Dialog/DoSomething.fs.template` (replace the dialog with an
  edit-todo dialog only if useful); `PlaceholderTypes.fs.template` -> drop or replace with the small client
  types the app needs (most types come from the shared Todo ecosystem types).
- **DELETE outright:**
  - the **six `.render` templates** (`route/Route.render.template`, `dialog/Dialog.render.template`, and
    `component/type/{estateful,pstateful,stateless,functional}/Component.render.template`) and their
    `.typext.fs`/`.styles.fs` pairs - replace each with a **pure-F# template**: the `functional` variant
    becomes the `[<Component>]` form; the stateful variants become `[<Component>]` + hooks (per the
    `LEARNINGS.md` conversion recipe). After this, `create-route`/`create-dialog`/`create-component` emit
    pure F#, never `.render`.
  - the **untracked `app/src/obj/` and `app/src/bin/`** build artifacts (they are local junk, not in git);
    remove them and make sure the template `.gitignore.template` excludes `obj/`+`bin/` so they never get
    shipped in a scaffold.

### 5E. Smoke test (so it cannot rot again)
Add a CI check: `eggshell create-app <name>` -> `eggshell dev-web` must build green (ideally render the
TODO list). This is the goal-B regression gate.

**Escalate-if:** codec-gen / Orleans-codegen wiring for the new suite; any framework-API mismatch the
SuiteJobs reference does not resolve; non-trivial scaffolding TS task changes.

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

- Frontend spike: `/Volumes/HomeX/shafayat/Code/eggshell-rnw-spike/` (`fsharp/Bindings.fs`, `App.fs`,
  `SignalRProbe.fs`; `verify-web.mjs`; native + web screenshots).
- Backend spike: `/Volumes/HomeX/shafayat/Code/orleans-net10-spike/` (`Program.cs`,
  `Types/SubjectExceptions.fs`).
- Strategy + verdicts + full findings: `/Volumes/HomeX/shafayat/Code/FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md`.
- Existing conversion recipe + past pitfalls: `subject/LEARNINGS.md` (the `[<Component>]` recipe,
  2026-06-26).
