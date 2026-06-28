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
- Copy the binding pattern from `eggshell-rnw-spike/fsharp/SignalRProbe.fs` (the `HubConnection` /
  `HubConnectionBuilder` interfaces + `emitJsExpr ... "new $0()"` + `conn.on/start/stop`).
- Add `@microsoft/signalr` to the relevant lib's `package.json` / `initialize`.
- Replace `Fable.SignalR` client usages in `LibClient`/`LibUiSubject` with the direct binding, preserving
  the existing F# call surface where possible.
- Remove the `Fable.SignalR` PackageReference (`LibUiSubject`).
**VALIDATE:** `dotnet build` + `eggshell build-lib`; then exercise a live subscription in `dev-web`.

### 2b. Server (backend)
- Replace `Fable.SignalR.AspNetCore` (in `LibLifeCycleHost`) with stock `Microsoft.AspNetCore.SignalR`
  (a `Hub` + `IHubContext`, see `eggshell-rnw-spike/signalr-server/Program.fs` for the minimal shape).
- Keep the same wire contract the client expects (`/api/v1/realTime` endpoints, message names).
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
