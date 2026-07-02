# Architecture

A ground-up technical overview of the EggShell framework as it exists today: **Microsoft Orleans** on
the backend, **Fable + React Native for Web** on the frontend, a **shared F# type system** marshalled
over JSON, and a **state-machine programming model** where developers write business logic plus tests
while the framework handles distribution, persistence, synchronization, and access control.

> This section is reference material derived from the framework source under `Lib*` / `Meta*`. The
> frontend primitive layer has been migrated off the archived `@chaldal/reactxp` fork to
> **react-native-web** (stabilization is ongoing); for that story and the rest of the platform work
> (Postgres/Orleans, .NET 10 TFMs), see [Modernization & Current Status](./modernization/index.md).

## 1. Big picture

EggShell lets you build cross-platform applications (web, Android, iOS) where **both backend and frontend
are written in F#**, and **the domain types are shared** between them. The same record or union a grain
persists on the server is the type a component renders on the client; the wire is just a JSON
encoding/decoding of those shared types.

```
        ┌──────────────── SHARED F# TYPES (Subjects, Actions, Events, Views) ────────────────┐
        │                                                                                     │
   ┌────┴────────────────────────┐                                  ┌────────────────────────┴──────┐
   │   FRONTEND (Fable → JS)      │   JSON over HTTP (req/response)   │   BACKEND (Orleans)            │
   │   RN / RNW components        │◀────────────────────────────────▶│   SubjectGrain (state machine) │
   │   LibClient / LibUi*         │   SignalR (subscription push)     │   Views / TimeSeries / Connectors │
   │   LibRouter                  │◀────────────────────────────────▶│   SQL Server storage + clustering │
   └──────────────────────────────┘                                  └────────────────────────────────┘
```

The two halves share a single mental model:

- **Backend** = a set of **lifecycles** (state machines). Each lifecycle owns a **Subject** (the state),
  reacts to **LifeActions** (transitions), emits **LifeEvents**, and produces side effects (create other
  subjects, call connectors, schedule timers, raise actions on self). See
  [Lifecycles / State Machines](./architecture/backend-lifecycles.md).
- **Frontend** = React Native (for Web) components that **subscribe** to backend subjects and re-render
  as state changes flow in as `AsyncData<'T>`. See [Frontend](./architecture/frontend.md).
- **Tests** = deterministic simulations of lifecycles across virtual time — a first-class reliability
  mechanism, not an afterthought. See [Testing Framework](./architecture/testing-framework.md).

## Repository layout conventions

| Prefix       | Meaning                                   | Examples |
|--------------|-------------------------------------------|----------|
| `Lib*`       | Framework library                         | `LibLifeCycle`, `LibClient`, `LibCodecGen` |
| `LibUi*`     | Frontend framework library                | `LibUiAdmin`, `LibUiIdentityAuth`, `LibAutoUi` |
| `Meta*`      | Toolchain (CLI, compilers, build)         | `Meta/AppEggshellCli`, `Meta/AppRenderDslCompiler` |
| `Suite*`     | A complete application                    | `SuiteTodo` |
| `App*`       | A frontend project (usually inside a Suite) | `AppEggShellGallery`, `AppTodo` |
| `ThirdParty` | F# wrappers around JS/RN libraries        | `Map`, `ReCaptcha`, `Recharts`, ... |

## Tech baseline

The toolchain has been migrated to **Fable 5 / .NET 10 SDK**, and the frontend primitive layer has been
migrated to **react-native-web**. Several backend concerns (Orleans version, SQL Server) are still on
their original footing and are tracked in [Modernization](./modernization/index.md).

| Layer     | In use today                    | Note |
|-----------|---------------------------------|------|
| .NET SDK  | 10.0.301 (`global.json`)        | Build host is net10; many project TFMs still target older frameworks (full TFM migration is later). |
| Fable     | 5.4.0 (tool)                    | `Fable.Core` 5.0.0, `Fable.React` 10.0.0-alpha.1; net10 target, MSBuild-direct cracking, `Pojo` bindings. |
| Frontend primitives | `react-native` 0.7x + **`react-native-web`** 0.19.x | Migrated off the archived `@chaldal/reactxp` fork (now removed as a dependency). The web build aliases `react-native` → `react-native-web`. Stabilization ongoing (scroll, gestures, pickers). |
| React     | 18.2.0                          | React 19 upgrade is a separate later step. |
| Orleans   | 3.7.2                           | 3.x → 7.x is a hard, non-rolling migration (wire protocol + grain identity changed); not started. |

The strategic backstory: **ReactXP was archived upstream by Microsoft**, which points people at React
Native for Web. EggShell ran on the `@chaldal/reactxp` fork for years and has now migrated the primitive
layer (`LibClient/src/ReactXP`, a legacy-named directory) to react-native-web. See
[ReactXP → RNW](./modernization/reactxp-to-rnw.md) for the full story.

## In this section

- [Lifecycles / State Machines](./architecture/backend-lifecycles.md) — the backend programming model.
- [Hosting & Persistence](./architecture/backend-hosting-persistence.md) — Orleans silo, SQL Server storage, and the
  Postgres seams.
- [Shared Types & Codecs](./architecture/shared-types-codecs.md) — Fleece codecs, evolution validation, the wire.
- [Testing Framework](./architecture/testing-framework.md) — the `simulation { }` harness and virtual clock.
- [Frontend](./architecture/frontend.md) — components, react-native-web interop, routing, data flow, animation.
- [Render DSL & Toolchain](./architecture/render-dsl-and-toolchain.md) — the `.render` DSL, its compiler, and the
  `eggshell` CLI.
- [Key File Map](./architecture/file-map.md) — where each concern lives in the source tree.
