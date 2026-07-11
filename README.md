# EggShell

**A full-stack F# application framework.** Write your backend and frontend in one language, share
the same domain types across the wire, model behavior as state machines, and test it all
deterministically. EggShell compiles the same F# to a Microsoft Orleans backend and a
React Native / react-native-web frontend that runs on web, Android, and iOS.

> New here? The fastest orientation is to run the gallery (see [Quick start](#quick-start)) and read
> its live docs, or start with
> [`architecture/index.md`](AppEggShellGallery/public-dev/docs/architecture/index.md).

## Why EggShell

Most stacks make you re-describe your domain three times: once for the database, once for the API,
once for the UI, and then keep the three in sync by hand. EggShell collapses that:

- **One language, one type system.** The record or union you persist on the server is the type you
  render on the client. No DTO drift, no hand-written serializers. Types cross the wire as JSON via
  Fleece codecs, with codec-evolution validation to catch breaking changes.
- **Behavior as state machines.** You model each entity as a *lifecycle*: a Subject (its state), the
  LifeActions that transition it, and the LifeEvents it emits. The framework runs these as Orleans
  grains and handles distribution, persistence, synchronization, and access control. You write the
  business logic and the tests.
- **Deterministic testing as a first-class citizen.** A `simulation { }` harness with a virtual
  clock and connector interception lets you test distributed, time-dependent behavior reproducibly,
  not as an afterthought.
- **Truly cross-platform frontend.** Pure F# components (`[<Component>]` + hooks) compile through
  Fable to react-native-web on the browser and React Native on device, from a single codebase.
- **Accessible by default.** Accessibility (screen readers, text scaling, contrast, target size,
  gesture alternatives) is a framework concern and a mandatory part of every UI change, not a
  bolt-on. See [`accessibility/index.md`](AppEggShellGallery/public-dev/docs/accessibility/index.md).

## Features

**Backend**
- Lifecycle state machines (Subject / LifeAction / LifeEvent) as Orleans grains
- Views (projections), time series, and connectors for side effects
- Access control and subject indexing / querying
- Deterministic simulation testing (virtual clock, stasis detection)

**Frontend**
- Pure F# component model (`[<Component>]` + hooks); the primitive seam is the `Rn` namespace over
  react-native-web
- Routing (`LibRouter`), subscription data flow (`AsyncData<'T>`), typed styling DSL with
  style-leak prevention, theming, animation and gestures
- Accessibility baked into the primitives

**Shared / tooling**
- Shared F# types over JSON with Fleece codecs and codec-generation
- The `eggshell` CLI: run, build, test, package, and scaffold apps / components / routes
- A live component gallery (`AppEggShellGallery`) that also renders this documentation

## Quick start

### Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | **10.0.301** | pinned in [`global.json`](global.json) |
| Node.js | 18.19+ (LTS) | for Metro, webpack, and the audit toolkit |
| `DOTNET_ROOT` | set in your shell | e.g. `export DOTNET_ROOT="$HOME/.dotnet"` |
| Android tooling | optional | Android SDK + `adb` + an emulator, for native Android |
| Xcode + CocoaPods | optional (macOS) | for native iOS |

### One-time setup

```bash
# From the repo root
./initialize

# Put the CLI on your PATH (add to your shell profile to persist)
export PATH="$PWD:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
```

### Run the component gallery (web)

The gallery is the canonical reference app and hosts the live docs.

```bash
cd AppEggShellGallery
./initialize          # first time only
eggshell dev-web      # watches .fs / .render and serves on :8082
# open http://127.0.0.1:8082/
```

### Run on a device (native)

Native development uses three terminals from the app directory:

```bash
# Terminal 1: Fable watch
cd AppEggShellGallery && eggshell dev-native

# Terminal 2: Metro bundler
cd AppEggShellGallery && npx react-native start --port 8081

# Terminal 3a: Android (run `adb reverse tcp:8081 tcp:8081` once per boot)
adb reverse tcp:8081 tcp:8081
cd AppEggShellGallery && npx react-native run-android

# Terminal 3b: iOS (macOS)
cd AppEggShellGallery/ios && pod install && cd ..
npx react-native run-ios --no-packager
```

Per-platform debug loops (boot, screenshot, logs, taps) are documented in
[`runbooks/`](AppEggShellGallery/public-dev/docs/runbooks/index.md), and wrapped by the project
skills in [`.claude/skills/`](.claude/skills) (`debug-android`, `debug-ios`, `debug-web`,
`verify-feature`, `fable-rebuild-verify`, `release-build`).

### Scaffolding a new app

> **Heads up:** `eggshell create-app` is currently being rebuilt (modernization Goal B) and does
> not yet produce a cleanly-running app. Until it lands, start from an existing app
> (`AppEggShellGallery` or `SuiteTodo/AppTodo`) as a template. See
> [`modernization/scaffolding.md`](AppEggShellGallery/public-dev/docs/modernization/scaffolding.md).

## Repository layout

| Path | What it is |
|---|---|
| `Lib*` | Framework libraries (core, backend, frontend): `LibLifeCycle`, `LibClient`, `LibRouter`, `LibCodecGen`, ... |
| `LibUi*`, `LibAutoUi` | Reusable frontend UI framework libraries |
| `Meta/*` | Toolchain: the `eggshell` CLI, compilers, build and scaffolding infrastructure |
| `Suite*` | Complete reference applications (`SuiteTodo`, `SuitePerformancePlayground`, ...) |
| `App*` | Frontend apps (e.g. `AppEggShellGallery`; `AppTodo` under `SuiteTodo`) |
| `ThirdParty/*` | F# wrappers around JS / React Native libraries |
| `eggshell`, `eggshell.cmd` | CLI launchers (Unix / Windows) |
| `global.json` | pins the .NET SDK |

## Documentation

The full documentation lives in
[`AppEggShellGallery/public-dev/docs/`](AppEggShellGallery/public-dev/docs/) and **renders live
inside the running gallery** (the same Markdown is the in-app "Docs" section, so docs and gallery
stay in sync). Start here:

- [Architecture overview](AppEggShellGallery/public-dev/docs/architecture/index.md): the system model and tech baseline
- [Current status & modernization](AppEggShellGallery/public-dev/docs/modernization/index.md): the authoritative status dashboard
- [Getting started](AppEggShellGallery/public-dev/docs/basics/getting-started.md) and [Dev experience](AppEggShellGallery/public-dev/docs/basics/dev-experience.md)
- [The dev loop](AppEggShellGallery/public-dev/docs/runbooks/dev-loop.md) and [Runbooks index](AppEggShellGallery/public-dev/docs/runbooks/index.md)
- [F# components](AppEggShellGallery/public-dev/docs/fsharp/component.md), [Styling](AppEggShellGallery/public-dev/docs/fsharp/styling.md), [Formatting](AppEggShellGallery/public-dev/docs/fsharp/formatting.md)
- [Backend lifecycles](AppEggShellGallery/public-dev/docs/architecture/backend-lifecycles.md) and the [Subject developer guide](AppEggShellGallery/public-dev/docs/subject/index.md)
- [`eggshell` CLI reference](AppEggShellGallery/public-dev/docs/tools/cli.md)
- [Accessibility](AppEggShellGallery/public-dev/docs/accessibility/index.md) (mandatory, default)

A machine-readable index for LLMs and tools is at
[`llms.txt`](AppEggShellGallery/public-dev/docs/llms.txt).

## Current status

The framework ships and is stable for web and native app development. A modernization effort is in
flight:

- **Toolchain (done):** Fable 5.4.0, .NET 10 SDK (10.0.301), Fable.React 10.0.0-alpha.1;
  React 19.2 + react-native 0.86 + react-native-web 0.21.2 with the New Architecture (Fabric)
  enabled. ReactXP is fully retired; the primitive seam is `LibClient/src/Rn/`.
- **In progress:** the render DSL is retired in framework and gallery code (pure F# components are
  the path forward); `eggshell create-app` scaffolding is being rebuilt; frontend build-speed and
  render-hygiene work is ongoing.
- **Later:** the Orleans 3.x to 7.x and SQL Server to PostgreSQL upgrades are a separate future
  workstream.

See [`modernization/index.md`](AppEggShellGallery/public-dev/docs/modernization/index.md) for the
live dashboard.

## Contributing

- Project conventions and working rules live in [`CLAUDE.md`](CLAUDE.md) (F# formatting, framework
  vs app boundaries, docs-in-the-same-commit, accessibility bar).
- After editing F#, run `dotnet tool run fantomas <file.fs>`. Build framework libraries via
  `./eggshell build-lib` / `eggshell dev-web` / `eggshell dev-native`; **never** run `dotnet fable`
  directly (it emits stray `.fs.js` beside sources).
- When you change code or docs, follow
  [`maintaining-docs.md`](AppEggShellGallery/public-dev/docs/maintaining-docs.md) to keep the docs
  in sync.
- Repeatable dev, debug, audit, and release procedures are packaged as invocable skills in
  [`.claude/skills/`](.claude/skills).
