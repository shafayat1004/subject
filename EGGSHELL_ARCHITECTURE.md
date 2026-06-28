# EggShell — Framework Deep-Dive & Forward Roadmap

> A full-stack F# application framework: Microsoft Orleans on the backend, Fable + ReactXP on the
> frontend, a shared type system marshalled over JSON, and a state-machine programming model where
> developers write business logic + tests and the framework handles distribution, persistence,
> synchronization, and access control.

This document is a ground-up technical writeup of the framework as it exists today, plus an
assessment of the maintainer's forward goals (retire the render DSL, modernize the frontend,
standardize structure, add Postgres, fix security issues). It is derived entirely from the framework
source under `Lib*` / `Meta*`; example applications were used only to confirm conventions.

---

## 1. Big Picture

EggShell lets you build cross-platform applications (web, Android, iOS) where **both backend and
frontend are written in F#**, and **the domain types are shared** between them. The same record or
union that a grain persists on the server is the type a component renders on the client; the wire is
just a JSON encoding/decoding of those shared types.

```
        ┌─────────────────────── SHARED F# TYPES (Subjects, Actions, Events, Views) ───────────────────────┐
        │                                                                                                   │
   ┌────┴───────────────────────┐                                       ┌───────────────────────────┴──────┐
   │   FRONTEND (Fable → JS)     │                                       │   BACKEND (Orleans, .NET 7)       │
   │                             │   JSON over HTTP (request/response)    │                                   │
   │   ReactXP components        │◀─────────────────────────────────────▶│   SubjectGrain (state machine)    │
   │   LibClient / LibUi*        │   SignalR (subscription push)          │   Views / TimeSeries / Connectors │
   │   LibRouter                 │◀─────────────────────────────────────▶│   SQL Server storage + clustering │
   └─────────────────────────────┘                                       └───────────────────────────────────┘
```

The two halves share a single mental model:

- **Backend** = a set of **lifecycles** (state machines). Each lifecycle owns a **Subject** (the
  state), reacts to **LifeActions** (transitions), emits **LifeEvents**, and produces side effects
  (create other subjects, call connectors, schedule timers, raise actions on self).
- **Frontend** = ReactXP components that **subscribe** to backend subjects and re-render as state
  changes flow in as `AsyncData<'T>`.
- **Tests** = deterministic simulations of lifecycles across virtual time. This is treated as a
  first-class reliability mechanism, not an afterthought.

### Repository layout conventions

| Prefix     | Meaning                          | Examples |
|------------|----------------------------------|----------|
| `Lib*`     | Framework library                | `LibLifeCycle`, `LibClient`, `LibCodecGen` |
| `LibUi*`   | Frontend framework library       | `LibUiAdmin`, `LibUiIdentityAuth`, `LibAutoUi` |
| `Meta*`    | Toolchain (CLI, compilers, build)| `Meta/AppEggshellCli`, `Meta/AppRenderDslCompiler` |
| `Suite*`   | A complete application           | (used here only to confirm conventions) |
| `App*`     | A frontend project (usually inside a Suite) | (used here only to confirm conventions) |
| `ThirdParty` | F# wrappers around JS/RN libs  | `Map`, `ReCaptcha`, `Recharts`, ... |

### Tech baseline (and how current it is)

| Layer        | In use today        | Latest available | Note |
|--------------|---------------------|------------------|------|
| .NET SDK     | 7.0.300 (`global.json`) | .NET 10        | .NET 7 is **out of support** (EOL May 2024). Migration to a current LTS is overdue. |
| Orleans      | 3.7.2               | 7.x / 8.x        | 3.x → 7.x is a **hard, non-rolling** migration (wire protocol + grain identity changed). |
| Fable        | 4.3.0               | 5.x              | 5.x targets net10, MSBuild-direct cracking, `Pojo` bindings. |
| ReactXP      | `@chaldal/reactxp` 2.2.0 | (none)       | Microsoft archived ReactXP; **this fork is the only living copy.** React 18.2 underneath. |
| React        | 18.2.0              | 19.x             | |

The single most strategic fact in this whole document: **ReactXP is dead upstream.** Microsoft
archived it and points people at React Native for Web. The `@chaldal/reactxp` fork is now the de
facto maintainer of record. Every frontend modernization goal flows from this reality.

---

## 2. Backend: the Lifecycle / State-Machine Core

### 2.1 What a lifecycle is

A **lifecycle** is a strongly-typed state machine over a **Subject**. It is a record of functions,
defined in `LibLifeCycle/src/LifeCycle.fs`:

```fsharp
type LifeCycle<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent,
               'SubjectIndex, 'SubjectId, 'AccessPredicateInput, 'Session, 'Role, 'Env> = {
    IdGeneration:    'Env -> 'Constructor -> IdGenerationResult<'SubjectId, 'OpError>
    Construction:    'Env -> 'SubjectId -> 'Constructor -> ConstructionResult<...>
    Transition:      'Env -> 'Subject -> 'LifeAction -> TransitionResult<...>

    Subscriptions:   'Subject -> Map<SubscriptionName, Subscription<'LifeAction>>
    Timers:          'Subject -> list<Timer<'LifeAction>>
    Indices:         'Subject -> seq<'SubjectIndex>

    Storage:         LifeCycleStorage
    MaybeApiAccess:  Option<LifeCycleApiAccess<...>>
    ResponseHandler: SideEffectResponse -> seq<SideEffectResponseDecision<'LifeAction>>
    // ...
}
```

Core type vocabulary (`LibLifeCycleTypes/src/SubjectTypes.fs`):

| Concept       | Role |
|---------------|------|
| `Subject`     | The persisted state (a record). Must carry a `SubjectId` and a creation timestamp. |
| `LifeAction`  | A discriminated union of commands that drive transitions. |
| `LifeEvent`   | Emitted when a transition completes; other subjects can subscribe to these. |
| `Constructor` | The "create" payload — input to `IdGeneration` + `Construction`. |
| `OpError`     | Domain error type for a lifecycle. |
| `SubjectIndex`| Promoted, queryable projections of subject state (string / numeric / geo / full-text). |

Transitions are written in computation expressions (`transition { ... }`, `construction { ... }`,
`operation { ... }`, `idGeneration { ... }`). A transition returns the new subject plus
**side effects**:

```fsharp
type TransitionSideEffects<'Constructor, 'LifeEvent, 'LifeAction> = {
    Constructors:    List<'Constructor>                       // create other subjects
    LifeEvents:      List<'LifeEvent>                         // publish events
    LifeActions:     List<'LifeAction>                        // enqueue actions on self
    ExternalActions: List<ExternalOperation<'LifeAction>>     // act on other lifecycles / connectors
}
```

Timers are declarative and time-relative:

```fsharp
type Schedule = Now | On of DateTimeOffset | AfterLastTransition of TimeSpan
type TimerAction<'LifeAction> = RunAction of 'LifeAction | DeleteSelf
```

Subscriptions wire one subject's events to another's actions:

```fsharp
type Subscription<'LifeAction> =
| ForSubject    of SubjectSubscription * ActionToRaise: 'LifeAction
| ForSubjectMap of SubjectSubscription * (LifeEvent -> Option<'LifeAction>)
```

> **The "QQQ" convention.** Builder CEs (`TransitionBuilder.fs`, etc.) define a `TODO` sentinel that
> raises `NotImplementedException "QQQ Not implemented yet"`. Paired with custom warning codes
> (`10666` = not-implemented, `10667` = long-term-development not-implemented in
> `Directory.Build.props`), this lets devs leave typed holes that warn in dev and **error in Release**.
> It's a clean way to ship-block on stubs.

### 2.2 Other first-class building blocks (all in `LibLifeCycle/src`)

- **Views** (`View.fs`) — read-only projections over subjects, with their own access control. This is
  the primary read path the frontend hits.
- **TimeSeries** (`TimeSeries.fs`) — units-of-measure-typed, time-indexed data streams ingested via
  side effects; support bucketing/aggregation.
- **Connectors** (`Services.fs`) — the integration boundary to the outside world (HTTP APIs, gateways,
  messaging). Single-response (`ResponseChannel`) or streaming (`MultiResponseChannel`). Crucially,
  connectors are the seam the **test harness intercepts** to mock external systems.
- **Ecosystem** (`Ecosystem.fs`) — a named bundle of lifecycles + views + time series + connectors.
  An ecosystem maps onto an Orleans cluster identity (ClusterId/ServiceId = ecosystem name).
- **Default services** (`DefaultServices.fs`) — ambient capabilities injected into `Env`:
  `ISubjectRepo`, `IBlobRepo`, `ITimeSeriesRepo`, `ICryptographer`, `ISequence`, `Clock`, etc. The
  `Clock` service is what makes time mockable (see §5).

### 2.3 Mapping onto Orleans

Each lifecycle is hosted as an Orleans **stateful grain**:

- `ISubjectClientGrain` (`LibLifeCycleCore/src/GrainClientInterface.fs`) — the callable surface:
  `Construct`, `Act`, `ActMaybeConstruct`, `Get`, etc., all `Task<Result<...>>`.
- `SubjectGrain` (`LibLifeCycleHost/src/SubjectGrain.fs`) — the activation that holds state and runs
  the lifecycle's `Transition` / `Construction` functions.
- `SubjectGrainModel.fs` — the serializable grain state, including a **persistent side-effect queue**
  (`Persisted` vs `Transient` effects: RPCs, timer triggers, subscription responses, self-actions,
  transactional steps, time-series ingestion).
- Supporting grains: `ISubjectIdGenerationGrain`, `ISubjectRepoGrain` (queries/indices),
  `DynamicSubscriptionDispatcherGrain` (pub-sub fan-out), and reminder integration.

The side-effect queue is the heart of reliability: a transition's effects are persisted alongside the
state change, then drained asynchronously with retries. This is what gives the framework
exactly-once-ish semantics across grain calls and what the test harness's "stasis" detection waits on.

---

## 3. Backend: Hosting & Persistence

### 3.1 Silo configuration

Configured in `LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs`. Orleans **3.7.2**, clustering via
**ADO.NET on SQL Server** (`.UseAdoNetClustering(...)`, invariant hardcoded to
`"Microsoft.Data.SqlClient"`, v5.2.3). There are several silo "shapes":
`Proper` (prod), `ProperSiloDev`, `Test` (in-memory reminders), `TestDataSeeding`.

Startup tasks run in order (`HostExtensions.fs`): `SqlServerSetupStartupTask` (schema/migrations) →
`CustomStorageInitStartupTask` → `LifeCycleHostStartupTask` (runs the ecosystem `_Startup` grain).

Host entry points exist for **Kubernetes** (`Host/K8S/...`), **Service Fabric** (`Host/Fabric/...`),
and **Development** (single in-memory node).

### 3.2 Storage abstraction

The clean seam is `IGrainStorageHandler<...>` in `LibLifeCycleHost/src/GrainStorageHandler.fs`:

```fsharp
abstract member TryReadState      : pKey -> Task<Option<ReadStateResult<...>>>
abstract member InitializeSubject : pKey -> dedup -> data -> Task<Result<...>>
abstract member UpdateSubject     : pKey -> dedup -> updateData -> Task<Result<...>>
abstract member PrepareInitializeSubject / CommitInitializeSubject  // 2-phase commit
```

Storage type per lifecycle (`LifeCycle.fs`): `Persistent` (with history retention policy) | `Volatile`
| `Custom`.

The SQL Server implementation lives in `LibLifeCycleHost/src/Storage/SqlServer/`:

- `SqlServerGrainStorageHandler.fs` — state as **gzip-compressed JSON**, optimistic concurrency via
  ETags/rowversion, dedup cache, 2-phase commit.
- `SqlServerSubjectRepo.fs`, `SqlServerTimeSeriesRepo.fs` — query/index reads.
- `SqlServerSetup.fs` — idempotent schema management: embedded `.sql` resources + a hash-tracked
  upgrade folder (`Upgrades_Auto/*.sql`) applied via `__ApplyScriptIfNewOrChanged`.
- `SqlServerDataProtectionXmlRepository.fs` — ASP.NET Data Protection key store.
- `SqlServerTransientErrorDetection.fs` — SQL-Server-specific retry classification.

Per lifecycle the schema generates: main table, `_History`, `_Index`, `_Subscription`, `_SideEffect`,
`_Prepared` tables, plus a `_Version` sequence.

### 3.3 Adding PostgreSQL — the concrete seams

This is feasible but non-trivial. The abstraction (`IGrainStorageHandler`) is already clean; the work
is dialect + provider + Orleans clustering. Exact touch points:

1. **New storage handlers** implementing the existing interfaces:
   `PostgresGrainStorageHandler.fs`, `PostgresSubjectRepo.fs`, `PostgresTimeSeriesRepo.fs`,
   `PostgresTransferBlobHandler.fs`, `PostgresDataProtectionXmlRepository.fs`.
2. **Provider selection** in `HostExtensions.fs` — today it directly instantiates the SQL Server
   handlers. Introduce a DB-dialect switch on the existing `EcosystemStorageSetup` union.
3. **Connection factory** — `SqlConnection`/`Microsoft.Data.SqlClient` is hardcoded in
   `SqlServerConfiguration.fs` and the handlers. Abstract behind a factory; Postgres uses `Npgsql`.
4. **SQL dialect** — the DDL/stored-proc `.sql` files are T-SQL. Postgres equivalents needed for:
   `ROWVERSION` → `xmin`/`bigserial`; `BIT` → `boolean`; `NVARCHAR ... COLLATE` → `text`/collation;
   full-text catalog → `tsvector`/`tsquery`; geography indices → PostGIS; stored procs → PL/pgSQL.
5. **Orleans clustering** — Orleans 3.7 ADO.NET clustering can target Postgres, but membership SQL
   (`SetupOrleans.sql`) must be ported and the invariant changed to the Npgsql provider.
6. **Transient-error detection** — Postgres error codes differ from SQL Server's.

Rough estimate: the F# handler layer is a moderate rewrite (~40%); the bulk of the effort is porting
and testing the SQL/stored-proc surface and the membership tables. The core lifecycle/Orleans layers
are untouched.

> Note: a `.NET 7` → current-LTS upgrade and the Postgres effort interact. If an Orleans 7.x upgrade is
> also on the table, do the storage rewrite **once**, against the target Orleans version, to avoid
> doing the membership/clustering port twice.

---

## 4. Shared Types, Codecs & the Wire

### 4.1 Codecs

Serialization is built on **Fleece** codecs, wrapped in `LibLangFsharp/src/CodecLib.fs`
(`Codec<'Encoding,'T>`, the `codec { ... }` CE, lossless ISO-8601 datetime formats). Codecs for record
and union types are **generated** rather than hand-written:

- `LibCodecGen/src/CodecGen.Common.fs` walks types via the F# Compiler Service and emits
  `get_Codec<'Encoding>()` members for everything marked `[<CodecAutoGenerate>]`
  (`[<SkipCodecAutoGenerate>]` opts out).
- Union cases carry a `__v1` version marker for forward compatibility.

On the server's HTTP edge, encoders/decoders are produced by reflection
(`generateAutoEncoder<'T>` / `generateAutoDecoder<'T>` in
`LibLifeCycleHost/src/Web/Api/V1/JsonEncoding.fs`).

### 4.2 Codec evolution validation

`LibCodecValidation` + `validate-codec.sh` exist to prevent **breaking the wire/storage format**
across releases. The tool extracts each type's codec shape into a `JsonNode` schema (terminal /
record / choice / option / array / tuple / any-one-of), serializes it per git branch, and runs an
**evolution checker** (`EvolutionCheckerLib.fs`) comparing old vs new. It enforces rules like: you may
make a required field optional but not vice-versa; you may not change a terminal's type; union case
counts must align. This is a genuinely strong safety net given that subjects are persisted as JSON.

```bash
./validate-codec.sh --suite Logistics --fromcodec releases/logistics --tocodec master
```

### 4.3 Request path and subscriptions

- **Request/response:** client `HttpService` (`LibClient/src/Services/HttpService/HttpService.fs`)
  encodes an `ApiEndpoint`'s payload to JSON, hits the V1 generic HTTP handler
  (`Web/Api/V1/GenericHttpHandler.fs`), which decodes input, runs session/access checks, executes a
  view/action on the grain, and encodes the result (200 / 422 OpError / 403 denied / 400 bad request).
- **Subscriptions (live push):** real-time updates use **SignalR** (`Fable.SignalR`; endpoints
  `/api/v1/realTime`). The server builds an `IObservable<SubjectChange<...>>` per subject
  (`Web/RealTime.fs`), backed by Rx with `Replay(1)` + ref-counted disposal, and pushes changes to
  subscribed clients. On the client, `EntityService` turns those into `Subscription<'T>` streams that
  emit `AsyncData<'T>` (`Uninitialized | Fetching | Available | Error`), which components consume.

### 4.4 Lang libraries

- `LibLangFsharp` — the F# "prelude": codecs plus `NonemptyList/Set/Map`, `EmailAddress`,
  `PhoneNumber`, `Positive<_>`, `GeoLocation`, async/result/option extensions, etc.
- `LibLangTypeScript` — the runtime counterparts Fable-compiled code relies on at JS runtime
  (`Option`, `Lazy`, promise helpers, pattern-matching helpers).

---

## 5. The Test Framework (a core reliability feature)

`LibLifeCycleTest` is what makes EggShell apps trustworthy. Tests are written as `simulation { ... }`
computation expressions (`SimulationBuilder.fs`) and discovered via a custom xUnit
`[<Simulation>]` attribute.

What it gives you:

- **A real in-memory Orleans cluster** (`TestCluster.fs`, Orleans `TestingHost`, volatile storage by
  default) — so tests exercise actual grain activation/concurrency, not mocks.
- **A virtual clock** (`ClockSimulation.fs`). The `Clock` default service is replaced by a per-partition
  simulated clock that only advances when you tell it to:
  `Ecosystem.moveTimeForwardAndRunReminders`, `setNewTimeAndRunReminders`,
  `overrideInitialSimulatedTime`. Each read ticks 100 ticks forward for monotonicity. Reminders/timers
  fire deterministically as the clock crosses their due time.
- **Connector interception** (`Ecosystem.interceptConnector`) — external calls are answered by the test,
  so integrations are simulated precisely.
- **Stasis detection** — after each action/time-move, the harness waits until the side-effect queue is
  fully drained and no timers are overdue before asserting (`waitForSystemStasis`,
  `SideEffectTracking.fs`). This eliminates the usual async-test flakiness.
- **Rich assertions** — `thenAssertOk`, `thenAssertEvent(Triggered)`, `thenAssertEventually(Within)`,
  `thenAssertSome`, `thenAssertNoBadLogs` (fails the test if the system logged warnings/errors).

Operations like `construct/act/get/readView` come in immediate and `...Wait` (await a specific
`LifeEvent`) flavors, so you can assert "this action eventually causes that event" across simulated
time.

**Scope:** backend only. There is no UI/component test harness. Known rough edges: a forward-only
clock, an explicit `hackDelay` workaround for async subscription timing, and default test parallelism
capped at 2 (serialization makes tests CPU-heavy).

---

## 6. Frontend: Components, ReactXP, Routing

### 6.1 Component model

Components are F# classes inheriting one of three bases (`LibClient/src/ChaldalReact.fs`):

| Base | State it carries |
|------|------------------|
| `PureStatelessComponent<Props, Actions, Self>` | none |
| `EstatefulComponent<Props, Estate, Actions, Self>` | ephemeral (lost on unmount) |
| `PstatefulComponent<Props, Estate, Pstate, Actions, Self>` | ephemeral **and** persistent (localStorage-backed `PersistentStore`) |

`Props` are immutable records. A component is split across files:

```
Components/Button/Button.typext.fs    # Props + class + Actions
Components/Button/Button.styles.fs    # styles (F# CSS-in-JS DSL)
_autogenerated_/.../Button.TypeExtensions.fs   # generated ctor overloads (LC.Button(...))
_autogenerated_/.../Button.Render.fs           # generated render function
```

There is also a **modern, lighter** path using `[<Component>]` + hooks for simple components (e.g.
`FlexFiller.fs`), with no `.render` and minimal boilerplate. The two styles coexist today.

### 6.2 ReactXP interop

ReactXP is imported through Fable JS interop (`LibClient/src/ReactXP/ReactXPBindings.fs`,
`import "*" "@chaldal/reactxp"`). Platform is detected at runtime (`Web | Native Android | Native iOS`)
and also at compile time via `#if EGGSHELL_PLATFORM_IS_WEB`. Wrapped primitives: `View`, `Text`,
`Button`, `ScrollView`, `TextInput`, `Image`, `VirtualListView`, `GestureView`, `WebView`, plus
`AnimatableView/Text/TextInput` and SVG.

Styles are a typed F# DSL (`ReactXP/Styles/Legacy/` and a newer `New/` dialect) compiled to runtime
style objects, with nesting (`&&`, `=>`), responsive breakpoints, and themes.

### 6.3 Routing

`LibRouter` provides typed routes: a `Location` ↔ `NavigationFrame<'Route,'Dialog>` conversion, route
parts (`Integer/Guid/String/NonemptyString/Json/JsonBase64`), and React Router v6 bindings (web:
`react-router-dom`; native: `react-router-native`). Components: `Router`, `Link`, `With.Location`,
`With.Route`, `With.Navigation`.

### 6.4 State / data flow

The dominant pattern is **subscription-driven**, not Redux-style global state: a component subscribes
to a backend subject/view and re-renders on `AsyncData<'T>` updates. A `UniDirectionalDataFlow`
executor context exists for action dispatch with built-in in-progress/error states.

### 6.5 UI libraries & ThirdParty

- General-purpose framework UI: `LibUiAdmin` (grids/tables/pagination), `LibUiIdentityAuth`
  (login/verify/token), `LibAutoUi` (forms auto-generated from F# types via reflection in
  `FormConstruction.fs`).
- `ThirdParty/` — ~20 wrappers around JS/RN libraries (Google Maps, Leaflet, Recharts, reCAPTCHA,
  QR, camera, image picker, device info, CodePush, analytics, ...). The pattern per wrapper is:
  `TypesJs.fs` (marshal F# → JS with `==>`/`==?>`/`==!>`), a `.typext.fs` component (often split
  `Web/`, `Native/`, `With/`), and `ComponentRegistration.fs` to register render+styles. This is the
  established, repeatable recipe for "manually supporting" a JS library.

### 6.6 Animation: current state

Animation is genuinely thin. What exists is ReactXP's `Animation.Timing` over `AnimatedValue` with a
small set of easings, plus three `Animatable*` wrappers — used in a few places (e.g. `Scrim.fs`
opacity fades, a carousel). What's missing for a modern feel: spring/physics motion, gesture-driven
animation, layout/shared-element transitions, route transitions, scroll-linked effects, and any
coordinated timeline/stagger system. `useNativeDriver` is disabled. There is no Framer-Motion-class
abstraction.

---

## 7. The Render DSL and the Toolchain

### 7.1 What the DSL is

Historically components were authored as `.render` files — an XML-ish template language
(`rt-if`, `rt-let`, `rt-map`/`rt-mapi`, `rt-match`, `rt-class`, `{expr}` interpolation, `{=fsharp}`
escapes, `<dom.div>` and `<Component>` tags). These were **never JS** — they always compiled to F#
(`*.Render.fs` under `_autogenerated_/`). Later, the team began writing render functions directly in
F#, and the DSL is now being retired.

### 7.2 The compiler

`Meta/AppRenderDslCompiler` is an **F#** program (built on `SuiteDsl/LibDsl`) with a parse → codegen →
emit pipeline (`Render/Parsing.fs`, `Render/CodeGeneration.fs`, `Render/Compiler.fs`,
`Program.fs`). It reads `.render` on stdin and writes an F# module on stdout. It has three modes:
`Render` (normal build output), `RenderConvert` (human-readable F# for migration), and
`RecordsWithDefaults`.

### 7.3 The `eggshell` CLI

`Meta/AppEggshellCli` (TypeScript/Node) is the developer entry point, delegating to:

- `Meta/LibEggshell` — project discovery (`eggshell.json` walk-up, project types).
- `Meta/LibScaffolding` — `create-app/component/route/dialog`, `rename-component`,
  `create-third-party-wrapper`, plus `convert-component`.
- `Meta/LibRtCompilerFileSystemBindings` — drives the render compiler over the filesystem
  (`processAllRender`, watch mode), TypeExtensions generation, webpack/native build orchestration.
- `Meta/LibFablePlus` — runs Fable (F# → JS) and bundling.

Commands include `create-app`, `dev-web`, `dev-native`, `dev-android`, `dev-ios`, `build-lib`,
`build-native`, `package-web`, `package-android`, `renderdsl`, `convert-component`.

### 7.4 Retiring the render DSL — what it takes

Current `.render` footprint (framework + apps): **~548 files**, concentrated in a few app suites and
in `LibClient` (~77) and a handful of `LibUi*`/`LibRouter`/`ThirdParty` files. The framework-owned
ones are the ones in scope for this repo's goals.

Mechanics:

1. `eggshell convert-component <Name>` already emits readable F# from a `.render` (via the compiler's
   `RenderConvert` mode) — **but it prints to stdout and is not auto-written**. The first concrete
   improvement is to make conversion write the `.fs` in place and delete the `.render`.
2. After all `.render` files are converted, remove the render steps from the build
   (`processAllRender`, the watch hooks in `LibRtCompilerFileSystemBindings`), drop the `renderdsl`
   command and the `render` section of `eggshell.json`, and retire `Meta/AppRenderDslCompiler` +
   `SuiteDsl/LibDsl` + `Meta/LibRenderDSL`.
3. Update `initialize` and the `build-*.fsx` scripts that build the DSL compiler.

The conversion itself is largely mechanical because the DSL has always been a thin sugar over the same
F# render shape — the risk is volume and reviewing the generated F# for readability, not semantics.

---

## 8. Scaffolding (`eggshell create-app`) — Currently Broken

`Meta/LibScaffolding` (TypeScript + `templates/`) backs the `create-*` CLI commands. It is a
**snapshot of an older framework state** and a freshly scaffolded app does **not** run
`eggshell dev-web` cleanly. The templates have drifted from the known-good reference apps in several
concrete ways:

| Area | Template generates | Current/working apps expect | Effect |
|------|--------------------|-----------------------------|--------|
| `eggshell.json` | flat `renderDependencies` key | nested `render.{dependenciesToRtCompile, additionalModulesToOpen, componentLibraryAliases, componentLibraryPaths, componentAliases}` | render compile misconfigured / fails |
| `App.fsproj` | hardcoded `.fs` includes, **no `_autogenerated_` entries**, no `<Content Include="**/*.render"/>` | each component pairs source with `_autogenerated_/.../*.TypeExtensions.fs` + `*.Render.fs` | F# build fails on unresolved generated modules |
| `package.json` | RN/mobile-centric, **no webpack / dev-web `start` script**, hardcoded `file:` paths to libs that won't exist | a `webpack-dev-server` start path | `dev-web` can't start |
| Components / routes / dialogs | still emit **`.render`** files (deprecated DSL) | pure-F# is the modern path | perpetuates the DSL you want to retire |
| Boilerplate | Chaldal-specific (`LibUiChaldalAuth`, `LibUiChaldal`, hardcoded `Landing/Bananas/Mangoes/DoSomething`) | generic minimal app | new app drags in company libs + references files that don't exist |
| `index.html` / entry | references `/bundle.js`, `.build/native/...` with no build wiring | webpack-produced bundle | runtime "cannot find module" |

**Net:** scaffolding fails at multiple independent layers (config schema, project file, web tooling,
deprecated DSL, Chaldal coupling). Fixing it is also the *cleanest place to set the new conventions*:
a corrected template is, by definition, the canonical "modern EggShell app." Work needed:

1. Rewrite `eggshell.json.template` to the current nested `render` schema (and make it generic, not
   Chaldal-coupled).
2. Rewrite `App.fsproj.template`: minimal references (`LibClient`, `LibRouter`, `FablePlugins`),
   correct `_autogenerated_` + `Content` includes — or, post-DSL, no autogenerated render at all.
3. Fix `package.json.template`: add the web dev-server path, drop dead `file:` deps and mobile-only
   bloat.
4. Stop emitting `.render` from `create-component/route/dialog`; emit pure F# (ties directly into §7.4
   and the verbosity goal).
5. Auto-update the `.fsproj` on `create-component` (today it's a manual, error-prone step).
6. Add a CI smoke test: `create-app` → `dev-web` must build green. Without this, scaffolding will
   silently rot again.

This should be treated as a **first-class deliverable**, not a chore: it is the front door for any new
developer and currently it's locked.

---

## 9. Build / Compile Performance (frontend)

Frontend builds are slow, and it's a **compound** of independent costs rather than one culprit.
Pipeline orchestration lives in `Meta/LibFablePlus/src/index.ts` and
`Meta/LibRtCompilerFileSystemBindings/src/index.ts`. What happens on `eggshell dev-web`:

1. **Dependency libs build serially** (`Promises.inSeries`) — no parallelism across the dep tree.
2. **Clean** bundle/output dirs.
3. **Render-DSL compile**: `processAllRender` globs every `.render` and spawns the F# render compiler
   **once per file** as a subprocess (~548 files repo-wide). Parallelized, but each is a full process
   spawn with .NET startup cost.
4. **LibStandard precompile**: a Fable `precompile` pass that runs **before** the app compiles and is
   **not fingerprint-cached** — it reruns even when nothing changed.
5. **Fable compile** with `--noParallelTypeCheck` explicitly set (intentional, to avoid webpack
   thrashing) — over a large surface (`LibClient` alone is ~500 `.fs` files) with heavily generic
   lifecycle types (generic instantiation is a known Fable cost).
6. **webpack**: single monolithic bundle (`splitChunks: false`, `LimitChunkCountPlugin maxChunks:1`),
   `eval-source-map` in dev.

Ranked bottlenecks and the levers:

| Bottleneck | Lever | Effort | Notes |
|-----------|-------|--------|-------|
| LibStandard precompile, uncached | Hash-fingerprint the precompiled output; skip when unchanged | Low–Med | Likely the single biggest warm-build win |
| `--noParallelTypeCheck` | Re-enable parallel type-check; debounce webpack trigger instead | Low | 30–50% off the F# phase; verify no watch thrash |
| 548 render subprocesses | Retiring the DSL (§7.4) **removes this stage entirely**; interim: batch files per compiler invocation | High (retire) / Med (batch) | Best fixed by deleting the DSL, not optimizing it |
| Serial dep builds | Topological parallel build | Med | Helps cold builds with wide trees |
| webpack monolith + `eval-source-map` | `cheap-module-source-map` in dev; consider esbuild/Vite | Med–High | Bundler swap is the big long-term win |
| Large/generic `LibClient` | Fable 5 (better caching/codegen); optional project split | High | Comes "for free-ish" with the Fable upgrade |

The two highest-leverage moves overlap with goals you already have: **retiring the render DSL** deletes
an entire build stage (and 548 subprocess spawns), and **upgrading Fable 4→5** improves caching and
codegen. The cheapest immediate wins are fingerprint-caching LibStandard and re-enabling parallel
type-check.

---

## 10. Modernization Paths (Fable, Orleans, .NET 10, ReactXP)

This section assesses each upgrade: what it buys, and what work it implies.

### 10.1 .NET 7 → .NET 10 (and F# 10)
- **Why:** .NET 7 is **out of support** (since May 2024) — this is a security/maintenance liability on
  its own. .NET 10 is the current LTS. F# 10 brings a type-subsumption cache (faster type-checking,
  snappier IDE), scoped warning suppression, more consistent CE syntax. .NET 10 JIT and runtime perf
  improvements benefit the Orleans host directly.
- **Work:** bump `global.json` + all `<TargetFramework>` to `net10.0`; resolve package upgrades
  (Orleans, Giraffe, SqlClient, etc.); fix any F# deprecations turned errors by
  `Directory.Build.props` (`--warnaserror+`). Frontend F# compiles via Fable, so the TFM bump there is
  mostly about the codegen toolchain (see Fable below).
- **Verdict:** non-negotiable baseline. Everything else is easier *after* this.

### 10.2 Fable 4.3 (tool 4.18) → Fable 5
- **Why:** Fable 5 **targets net10**, replaces the Buildalyzer project cracker with direct MSBuild
  invocation (more robust), supports `TreatWarningsAsErrors`, adds `Pojo` bindings (cleaner JS-object
  interop — directly useful for the ThirdParty wrappers and any ReactXP/RN-Web binding work), and
  improves caching/codegen (build-time win, §9).
- **Work:** Fable 5 is "compatible with Fable 4 projects" but the net10 target means it pairs with the
  .NET 10 move; verify the custom `Meta/FablePlugins` (the `[<Component>]` plugin) still compiles
  against Fable 5's plugin API, and that `Fable.React`/`Thoth.Json`/`Fable.Promise` versions line up.
- **Verdict:** do it together with .NET 10. Moderate risk concentrated in the custom Fable plugin.

### 10.3 Orleans 3.7 → 7.x/8.x
- **Why:** 3.x is four majors behind. 7.x delivers large throughput gains (Microsoft cites ~1.5M→4.5M
  msg/s), string-based grain/stream identities, package modularization (`Orleans.Reminders`,
  `Orleans.Streaming`, `Orleans.Transactions` split out), and modern source-gen serialization.
- **Cost — this is the hard one:** Orleans 7 **changes the wire protocol and grain identity format
  incompatibly**. No rolling upgrade — you stand up a new cluster and decommission the old one. Grain
  state must be migrated (activate → load → patch id to new format → save; JSON payloads themselves are
  fine). Reminders/streams APIs moved off the `Grain` base class to extension methods, so framework
  code that calls them needs updating. ADO.NET providers already require `Microsoft.Data.SqlClient`
  (you're already there).
- **Where it bites in this codebase:** `LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs` (silo/clustering
  setup), `SubjectGrain.fs` / `GrainClientInterface.fs` (grain identity + lifecycle method signatures —
  `OnActivateAsync`/`OnDeactivateAsync` gained `CancellationToken`/`DeactivationReason`), the custom
  `SubjectReminderTable`, and the SQL membership schema (`SetupOrleans.sql`).
- **Verdict:** highest-effort, highest-reward backend item. Sequence it as its own project, and — per
  §3.3 — **fold the Postgres storage rewrite into the same effort** so the membership/clustering port
  isn't done twice.

### 10.4 ReactXP: update the fork vs. swap it
ReactXP is **archived/EOL upstream**; `@chaldal/reactxp` is the living fork. Two viable directions:

**Option A — Keep and invest in the fork.**
- *Pros:* no disruption to ~hundreds of components; ReactXP's narrow cross-platform API is exactly what
  the F# wrapper layer targets; you fully control the release cadence.
- *Cons:* you are now the *sole* maintainer of a cross-platform abstraction over React + RN; modern
  features (animation, new RN/web APIs) must be backfilled by hand; you inherit RN-version upgrade
  treadmill alone.
- *Work:* ongoing; targeted feature backfills (animation first).

**Option B — Migrate the primitive layer to React Native for Web (Microsoft's own recommendation).**
- *Pros:* rejoins a maintained ecosystem (`react-native-web` + RN proper), unlocks the modern RN
  library landscape (animation: Reanimated/Moti; gestures: RNGH), better long-term footing.
- *Cons:* RN-Web and ReactXP are **architecturally different** — RN-Web is a parallel RN implementation,
  ReactXP is a thin "lowest common denominator" layer on top of RN+React. APIs differ (styles, some
  props, `Animated`). It's a refactor, not a drop-in.
- *Crucial mitigant in this codebase:* F# components target the **wrapped `RX.*` primitives** in
  `LibClient/src/ReactXP`, not ReactXP directly. That wrapper layer is the **single seam** to
  re-implement against RN-Web. If the wrappers preserve their F# signatures, most component code and
  the entire `.typext.fs`/styles model stay put. The blast radius is the wrapper layer + style runtime,
  not every component.
- *Work:* re-implement `LibClient/src/ReactXP/Components/*` and the style runtime against RN-Web;
  reconcile the `Animated`/style differences; validate web + native; this is a multi-month initiative.

**Recommendation:** decide this explicitly and early, because it gates the entire frontend
modernization budget. Given ReactXP is dead upstream, **Option B is the strategically correct
direction** — but only *after* the cheaper wins (DSL retirement, scaffolding, Fable 5, build speed),
and structured around the fact that the wrapper layer is the contained seam. If team bandwidth is the
constraint, Option A (invest in the fork, animation first) is a legitimate holding pattern, not a dead
end — but it should be a conscious "buy time" choice, not the default.

---

## 11. Security Review (candidate issues — verify before acting)

A focused pass over the framework's auth, SQL, crypto, and transport surfaces surfaced the following.
Treat the high/critical items as **leads to confirm**, not yet-proven exploits — they need a careful
read of the exact branches before any change, because access-control defaults often have a subtle
intended path.

| # | Area | Finding | Severity | Direction |
|---|------|---------|----------|-----------|
| 1 | AuthZ | Possible **default-allow** when a subject/view has no session handling configured (`maybeSessionHandling = None` → `Grant`) in the host `AccessControl` access path. | **Critical (verify)** | Confirm the branch; if real, flip default to `Deny` and require explicit `ApiAccess` for external exposure. |
| 2 | SQL  | Schema/lifecycle/ecosystem **names interpolated** into SQL via `sprintf` (e.g. `SqlServerSetup.fs`, `SqlServerDataProtectionXmlRepository.fs`, `SqlServerTransferBlobHandler.fs`). Data values are parameterized; identifiers are not. | **High** | These names are framework/admin-controlled today, so impact is bounded — but add identifier whitelisting (`^[A-Za-z0-9_]+$`) / `QUOTENAME`-style escaping to be safe. |
| 3 | Transport | CORS uses `SetIsOriginAllowedToAllowWildcardSubdomains()` **with** `AllowCredentials()` (`Host/K8S/Api/Startup.fs`). | Medium | Pin explicit subdomains, or drop credentials for wildcard origins. |
| 4 | Serialization | Reflection + `MakeGenericMethod` on projection types in `JsonEncoding.fs`. | Medium | Fine as long as projection types come only from compiled lifecycle defs (they do); add a guard if that ever changes. |
| 5 | Session | Cookies correctly set `HttpOnly`/`Secure`(non-dev)/`SameSite=Lax`, but **2-year expiry** is long. | Low | Shorten + revalidate. |
| 6 | Crypto | `SssXmlEncryptor.fs` (currently untracked) uses AES-GCM correctly (CSPRNG nonce, 16-byte tag, verified on decrypt). | None | Self-labeled "foundational example" — harden error handling before production wiring. |

The architecture has real strengths here: codec evolution validation, optimistic concurrency, typed
access predicates, and the fact that access control is centralized rather than per-handler. The work
is mostly verifying defaults and hardening the SQL identifier surface.

---

## 12. Forward Roadmap (mapped to your goals)

### A. Retire the render DSL  *(well-scoped, low risk; also a build-speed win)*
1. Upgrade `convert-component` to write `.fs` in place + delete the `.render` (and re-run the
   TypeExtensions generation).
2. Convert framework-owned `.render` files first (`LibClient`, `LibUi*`, `LibRouter`, `ThirdParty`),
   reviewing generated F# for readability.
3. Strip render steps from the build, delete the compiler + `LibDsl` + `LibRenderDSL`, simplify
   `eggshell.json`/`initialize`. *(This deletes the 548-subprocess render stage — see §9.)*

### B. Fix scaffolding  *(first-class deliverable — the front door is currently broken)*
Per §8: rewrite the `eggshell.json` / `App.fsproj` / `package.json` templates to current schema + web
tooling, stop emitting `.render` (emit pure F#), decouple from Chaldal libs, auto-update `.fsproj` on
`create-component`, and add a `create-app → dev-web` CI smoke test so it can't silently rot again. A
correct template *is* the canonical modern app, so this also anchors goals C and D.

### C. Reduce component verbosity  *(incremental, ships alongside A/B)*
The hooks/`[<Component>]` path already points the way. After the DSL is gone, lean into it: scaffolding
templates for the common shapes, helpers for the subscribe-render pattern, and fewer mandatory
`Estate/Pstate/Actions` declarations for trivial components.

### D. Standardize frontend directory structure  *(cheap, do it during A/B)*
Structure is *already* fairly consistent (`Components/<Name>/<Name>.typext.fs|.styles.fs`,
`_autogenerated_/` mirror, `ComponentRegistration.fs`, ordered `.fsproj`). Divergences: `ThirdParty`
adds `TypesJs.fs` + `Web/Native/With`. Removing `_autogenerated_` (post-DSL) is itself a big
simplification. Codify the remaining conventions in a written guide + the corrected scaffolder (B).

### E. Speed up the build  *(quick wins are cheap and independent)*
Per §9: fingerprint-cache the LibStandard precompile, re-enable parallel type-check, parallelize the
dep-tree build, and lighten dev source maps — all near-term. The structural wins (retire DSL = A,
Fable 5 = G, bundler swap) land as those projects land.

### F. Modernize the platform: .NET 10 + Fable 5  *(baseline; do together)*
Per §10.1–10.2: .NET 7 is EOL, so this is also a security item. Bump TFMs to net10, move to Fable 5
(verify the custom `Meta/FablePlugins`), resolve package upgrades. Everything else is easier afterward.

### G. PostgreSQL + Orleans upgrade  *(one coordinated backend workstream)*
Per §3.3 + §10.3: the Orleans 3.7→7 jump is a hard, non-rolling migration; the Postgres storage rewrite
touches the same membership/clustering/storage seams. Do them **once, together**, against the target
Orleans version. Order: decide .NET-LTS + Orleans target → abstract connection factory + provider
selection → port DDL/procs + membership → Postgres handlers → run the full simulation test suite in
`TestDataSeeding` mode against Postgres.

### H. ReactXP decision  *(gates the frontend modernization budget)*
Per §10.4: ReactXP is dead upstream. Strategically, migrating the wrapper layer in
`LibClient/src/ReactXP` to React Native for Web (Option B) is the right long-term call and the wrapper
is the contained seam — but only after the cheap wins. Investing in the fork (Option A), animation
first, is a legitimate "buy time" holding pattern. Either way, a real **animation story** is the
highest-visibility frontend feature win.

### F'. Security fixes  *(start now; independent of everything else)*
Verify and fix §11 #1 (default-allow) and #2 (SQL identifier interpolation) first; the rest are
hardening.

### I. Frontend render hygiene — key warnings + style leaks  *(incremental, ships alongside A/C; low risk)*
Two classes of dev-console warnings recur across the frontend and the gallery. Both have cheap,
mechanical fixes and a "make-it-unforgettable" follow-up.

**Unique "key" prop warnings.** Cause: a static `[| el1; el2; ... |]` array of sibling elements is handed
to a component as children via a bare cast (`castAsElement` / implicit `!!`), so React renders a keyless
list. The fix already exists in `LibClient/src/EggShellReact.fs`: `tellReactArrayKeysAreOkay`
(`React.Children.toArray`, which injects stable keys) and its element-returning wrapper
`castAsElementAckingKeysWarning`. Plan:
1. **Convert offenders** to route static child arrays through `castAsElementAckingKeysWarning` instead of
   `castAsElement`/`[| |]`. Start with the shared gallery shells that fan out to every page —
   `ComponentContent` (its top-level `children` array) and `ComponentSample` — then grep the gallery +
   framework for `castAsElement`/raw children arrays and convert.
2. **Make it structural (preferred long-term).** Have the `element`/`elements` CE builder auto-key its
   multi-child output so authors physically cannot emit a keyless sibling list. This folds into goal C
   (verbosity) and the DSL retirement (A); once the builder keys for you, rule 1 becomes unnecessary.
3. **Don't mass-suppress.** `castAsElementAckingKeysWarning` assigns real keys (good); never silence the
   warning by other means without keys.

**Style leaks.** Cause: a `makeViewStyles`/`makeTextStyles` runs inside render (inline in a `styles`
array, or in a component/`Pointer.State`/`With.ScreenSize` callback), so ReactXP re-allocates an
uncacheable style every render. Worst when amplified by a re-render loop (one offender then looks like
"tons"). Plan:
1. **Codify the rule** (done): see `docs/fsharp/styling.md` "Avoiding style leaks" and the CLAUDE.md hard
   rule — static → top-level `let`; parametrized → `*.Memoize` keyed on primitives, never whole `Theme`
   records; param names must not collide with CE operations.
2. **Sweep the framework** for leaky per-render builders and memoize them (already done for `Sidebar.Item`,
   `Nav.Top.Item`, `ToggleButton`, `DateSelector`, `GalleryHeadings`; see `LEARNINGS.md` 2026-06-28).
3. **Keep the audit honest:** the gallery audit scripts already dedupe/track `STYLE-LEAK:N` per page
   (`audit-gallery-style-leaks.mjs`); use a clean run as the regression gate so new leaks can't creep in.

### Suggested sequencing
1. **Now (independent, high value):** security verification/fixes (§11 #1, #2); build quick-wins (E);
   start the scaffolding fix (B).
2. **Near-term (reinforce each other, low risk):** retire render DSL (A) + scaffolding (B) + structure
   standardization (D) + verbosity wins (C) + render hygiene (I). DSL retirement also speeds builds, and
   the `element`/`elements` auto-keying in (I) rides along with C/A.
3. **Platform baseline:** .NET 10 + Fable 5 (F) — unblocks the rest and clears the EOL liability.
4. **Decision point:** ReactXP fork-vs-migrate (H).
5. **Larger initiative:** Orleans upgrade + PostgreSQL (G) as one coordinated storage workstream, on
   top of the .NET 10 baseline.

---

## 13. Key File Map

| Concern | Entry points |
|---------|--------------|
| Lifecycle core | `LibLifeCycle/src/LifeCycle.fs`, `LibLifeCycleTypes/src/SubjectTypes.fs`, builders in `LibLifeCycle/src/*Builder.fs` |
| Views / TimeSeries / Connectors / Ecosystem | `LibLifeCycle/src/{View,TimeSeries,Services,Ecosystem,DefaultServices}.fs` |
| Orleans grains | `LibLifeCycleHost/src/SubjectGrain.fs`, `SubjectGrainModel.fs`, `LibLifeCycleCore/src/GrainClientInterface.fs` |
| Silo / hosting | `LibLifeCycleHost/src/OrleansEx/SiloBuilder.fs`, `HostExtensions.fs`, `Host/{K8S,Fabric,Development}` |
| SQL storage | `LibLifeCycleHost/src/Storage/SqlServer/*` |
| Codecs | `LibLangFsharp/src/CodecLib.fs`, `LibCodecGen/src/CodecGen.Common.fs`, `LibCodecValidation/*`, `validate-codec.sh` |
| HTTP / realtime | `LibLifeCycleHost/src/Web/Api/V1/{GenericHttpHandler,JsonEncoding}.fs`, `Web/RealTime.fs`, `LibClient/src/Services/{HttpService,EntityService}` |
| Tests | `LibLifeCycleTest/{SimulationBuilder,ClockSimulation,TestCluster,Ecosystem,SideEffectTracking}.fs` |
| Frontend core | `LibClient/src/ChaldalReact.fs`, `ReactXP/*`, `LibRouter/src/*` |
| Toolchain | `Meta/AppEggshellCli`, `Meta/AppRenderDslCompiler`, `Meta/Lib{Eggshell,Scaffolding,RtCompilerFileSystemBindings,FablePlus}` |

---

### Sources (external context)
- [Orleans 3.x → 7.0 migration guide](https://learn.microsoft.com/en-us/dotnet/orleans/migration-guide) · [What's new in Orleans 7](https://devblogs.microsoft.com/dotnet/whats-new-in-orleans-7/)
- [ReactXP (archived) on GitHub](https://github.com/microsoft/reactxp) · [React Native for Web compatibility](https://necolas.github.io/react-native-web/docs/react-native-compatibility/) · [ReactXP FAQ (RN-Web positioning)](https://microsoft.github.io/reactxp/docs/faq.html)
- [Fable 5 release](https://fable.io/blog/2026/2026-02-27-Fable_5_release_candidate.html) · [Fable releases](https://github.com/fable-compiler/Fable/releases) · [Fable .NET compatibility](https://fable.io/docs/dotnet/compatibility.html)
- [What's new in F# 10](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-10) · [Performance improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/)
