# Migration Execution

> **Companion to [../modernization/reactxp-to-rnw.md](./modernization/reactxp-to-rnw.md) (the why). This is the how.**

Step-by-step executor manual for the .NET 10 + Fable 5 + react-native-web modernization (Phases 1-6). Written for an executor model doing the mechanical bulk of the work.

See also: [Modernization overview](./modernization/index.md) | [Dev loop](./runbooks/dev-loop.md) | [Build and rebuild](./runbooks/build-rebuild.md) | [Troubleshooting](./runbooks/troubleshooting.md) | [Accessibility](./accessibility/index.md)

---

## Golden rules (apply to every step, no exceptions) {#golden-rules}

1. **Validate after every change. Never claim done on an unverified change.** Each step has a VALIDATE block; the build/run must be green before you move on.
2. **Escalate, do not guess.** If a step fails in a way not listed under its PITFALLS, or a fix is not obvious in one attempt, STOP and hand back to a stronger model with the exact error.
3. **One change at a time.** Bump one thing, build, confirm green, then the next.
4. **Work framework-only** (`Lib*`, `LibUi*`, `LibRouter`, `LibAutoUi`, `LibLifeCycleUi`, `ThirdParty`, `Meta/*`) by default. Exception: Phases 5-6 explicitly create `SuiteTodo/` + `AppTodo/` as the reference implementation.
5. **No em-dash in prose. No banned NuGet packages (Moq, AutoMapper).**
6. **Environment:** always `export DOTNET_ROOT="$HOME/.dotnet"` before dotnet/fable/eggshell commands.
7. **Keep the [Engineering Log](./knowledge-base/engineering-log.md) updated** with anything you got wrong and corrected.
8. **Accessibility is the default and mandatory.** Every UI you build or port ships accessible across the full spectrum. Follow the [Accessibility docs](./accessibility/index.md) (pit-of-success principles + recipes). Never silently skip a11y.
9. **Use the dev runbook for any device/build/observe loop.** See [Dev loop](./runbooks/dev-loop.md), [Android](./runbooks/android.md), [iOS](./runbooks/ios.md), [Web](./runbooks/web.md). Debug with raw adb/simctl/browser (Tier 1); verify/gate with the `audit/` toolkit (Tier 2).

**Build/validate commands (memorize):**

```bash
# Framework lib type-check:
dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"

# Full frontend app + gallery:
cd AppEggShellGallery && ../eggshell dev-web    # then Playwright gallery audit

# Backend lib:
dotnet build <proj>.fsproj

# A LibClient change is NOT validated until dev-web is green and the gallery renders.
```

---

## Current branch status {#branch-status}

Branch `modernization/rnw` (as of 2026-06-29):
- Toolchain: **Fable 5.4.0 on .NET 10 (10.0.301)** -- `Fable.Core` 5.0.0, `Fable.React` 10.0.0-alpha.1; native + web build green.
- **Phase 1 done** (Fable 5 + .NET 10 build host).
- **Phase 2 done** (modular `eggshell-signalr`).
- **Phase 4 (RNW seam) is the live frontier.** Primitives still render through `@chaldal/reactxp`; Phase 4 adds `react-native-web`, the webpack alias, and `LibClient` seam scaffolding before re-pointing the first primitive.
- **Phase 3** (backend net10 TFMs): SDK is net10; per-project TFMs partially flipped; simulation runner wiring still open.
- **Phase 5** (`SuiteTodo` reference app): largely built on the ReactXP seam; finish 5C gaps (EditTodo dialog, Settings route) in parallel if needed before templatizing (5E).

---

## Phase 1: frontend platform bump -- Fable 4 to Fable 5 (+ .NET 10 build host) {#phase1}

**Owner:** weaker model OK, except step 1b (plugin) which may need escalation.  
**Goal:** the F# frontend compiles and runs under Fable 5 on the .NET 10 SDK, with the same behavior.

### 1a. Install the .NET 10 SDK (one-time)

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
```

**VALIDATE:** `export DOTNET_ROOT="$HOME/.dotnet"; dotnet --list-sdks` must list a `10.0.x` AND `dotnet --version` returns cleanly (rc 0).  
**PITFALL:** on some machines the freshly installed .NET 10 host is SIGKILL'd (exit 137) and breaks all `dotnet` calls. If `dotnet --version` returns 137, **STOP and escalate** (it is an environment problem, not a code problem).  
**NOTE:** `subject/global.json` pins `7.0.300` with `rollForward: minor`, so existing .NET 7 work is unaffected by a side-by-side .NET 10 install until you change `global.json` (step 1e).

### 1b. Port the custom Fable plugin (`Meta/FablePlugins`) -- THE GATE

Nothing else frontend compiles under Fable 5 until this loads. Three known edits:

1. `Meta/FablePlugins/FablePlugins.fsproj`: `Fable.AST` PackageReference `4.5.0` to `5.0.0`.
2. `Meta/FablePlugins/ReactComponent.fs`: `FableMinimumVersion` `"4.0"` to `"5.0"`.
3. `Meta/FablePlugins/AstUtils.fs`, the `makeIdent` `Ident` record literal: add `IsInlineIfLambda = false` (Fable 5's `Ident` record added this field; omitting a record field is a compile error).

**VALIDATE:** `export DOTNET_ROOT="$HOME/.dotnet"; dotnet build Meta/FablePlugins/FablePlugins.fsproj` must be green (stays `netstandard2.0`).  
**ESCALATE-IF:** it does not build after these 3 edits (means the Fable 5 AST drifted further than expected). Do NOT improvise AST changes.

### 1c. Bump the Fable tool and ecosystem packages

- `.config/dotnet-tools.json`: `fable` `4.18.0` to `5.4.0`. Then `dotnet tool restore`; confirm `dotnet fable --version` prints `5.4.0`.
- Also bump **nested** `global.json` files: `LibStandard/global.json`, `AppEggShellGallery/global.json`, `Meta/FablePlugins/global.json` (all pinned 7.0.300; `eggshell dev-web` runs `dotnet fable` from `LibStandard/` and picks up the nested file).
- Across frontend `.fsproj`s, bump (one lib at a time, rebuild between):
  - `Fable.Core` `4.x` to `5.x`
  - `Fable.React` `9.4.0` to **`10.0.0-alpha.1`** (no 10.0.0 stable exists yet)
  - `Fable.Promise`, `Thoth.Json`, `Thoth.Json.Net`, `Fable.Browser.*`, `Fable.Date`: bump to the latest that restores
- `Directory.Build.targets`: bump the `FSharp.Core` central pin to `9.0.201` (needed by `Fable.React 10.x`)

**VALIDATE:** `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"` green, then `eggshell build-lib` on each touched lib.  
**ESCALATE-IF:** a package has no Fable 5 build at all. For **Fable.SignalR specifically, do NOT hunt for a version** -- it is abandoned (see Phase 2; you remove it).

### 1d. Verify the plugin + a real component end to end

```bash
export DOTNET_ROOT="$HOME/.dotnet"
dotnet fable LibClient/src/LibClient.fsproj -o /tmp/fable-validate --noCache
```

**VALIDATE:** "Fable compilation finished" and `.js` emitted for components. A `FablePlugins` transpile error at the very end is EXPECTED/benign -- the plugin's own source is not meant to be transpiled.

### 1e. Flip the frontend build TFM/SDK

- `subject/global.json`: bump SDK to `10.0.x` (and widen `rollForward` if needed). This changes what the whole repo resolves; coordinate with Phase 3.
- Frontend Fable projects can stay `net7.0`/`netstandard2.0` for Fable's purposes; the SDK must be present.

### 1f. Full-app validation

`eggshell dev-web` on `AppEggShellGallery` must compile and serve; run the Playwright gallery audit.

**PITFALLS (watch-items):**
- **`#241` FunctionComponent.Of cache** keyed by source file+line: if two distinct components render from the same source line (generic/reused code), they collide. EggShell's normal `[<Component>]` path likely avoids this; if you see the wrong component rendering, escalate.
- **Array-equality change:** Fable 5.0 changed `Array`/`ResizeArray` equality. If logic comparing arrays with `=` misbehaves, escalate.
- **`voidEl`/void DOM elements** (`br`/`img`/`input`) may emit a trailing `[]` that React rejects on web. Workaround: inline the helper.
- After ANY LibClient change, **restart `eggshell dev-web`** (stale `LibStandard/.build/.../fable` is what webpack serves).

---

## Phase 2: SignalR -- replace the dead wrapper with direct bindings {#phase2}

**Owner:** weaker model OK (template exists).  
**Goal:** remove `Fable.SignalR`/`Fable.SignalR.AspNetCore` (abandoned, Fable 3-era) and use Microsoft's maintained `@microsoft/signalr` (client) + stock ASP.NET Core SignalR (server).

### 2a. Client (frontend)

- Source lives in sibling repo `../eggshell-signalr/src/LibSignalRClient/`. Clone `eggshell-signalr` next to `subject/`.
- `@microsoft/signalr` remains in `LibClient/package.json`.
- `LibUiSubject` gets a `ProjectReference` to `../../eggshell-signalr/src/LibSignalRClient/LibSignalRClient.fsproj`.

**VALIDATE:** `dotnet build ../eggshell-signalr/EggShellSignalR.sln`; `dotnet build LibLifeCycleHost`; `dotnet build LibUiSubject -c "Web Debug"`; `eggshell build-lib` (LibUiSubject); gallery `dev-web` Fable watch green. E2E live push still required before merge.

### 2b. Server (backend)

- Source lives in `../eggshell-signalr/src/LibSignalRServer/`.
- `LibLifeCycleHost` gets a `ProjectReference` to `../../eggshell-signalr/src/LibSignalRServer/LibSignalRServer.fsproj`.
- Keep wire contract (`/api/v1/realTime`, message names) unchanged.

**VALIDATE:** backend builds; a client connects and receives a pushed update end to end.  
**ESCALATE-IF:** the typed-hub message contract does not map cleanly to a stock hub.

---

## Phase 3: backend .NET 7 to .NET 10 (Orleans FROZEN at 3.7.2) {#phase3}

**Owner:** weaker model OK for the bumps; escalate on runtime/serialization or ASP.NET Core breaks.  
**Goal:** backend targets `net10.0` with Orleans still `3.7.2`.

### 3a. TFM + SDK

- `global.json` SDK to `10.0.x` (shared with Phase 1e).
- Flip every backend `<TargetFramework>` from `net7.0` to `net10.0`, one project at a time, building between.
- Leave `Meta/FablePlugins` at `netstandard2.0`.

### 3b. Known package fixes (do these first)

- `LibLangFsharp/src/LibLangFsharp.fsproj`: remove or bump the stray `System.Text.Json 5.0.2` pin (let the framework supply STJ, or bump to the net10 version).
- `FSharp.Core` pin in `Directory.Build.targets`: bump if the net10 SDK warns.
- `Giraffe 6.2.0` vs ASP.NET Core 10: bump Giraffe to **8.2.0** (removes Newtonsoft; re-wire DI via `Json.ISerializer`).
- `Microsoft.Data.SqlClient 5.2.3`: bump to **7.0.2** (`Encrypt=true` default since 4.0; set `TrustServerCertificate=True` for dev or `Encrypt=False`).
- Resolve `Microsoft.Extensions.*` version-unification conflicts (Orleans 3.7 wants 6.x; net10 host wants 10.x). Use Central Package Management (`Directory.Packages.props`) with `CentralPackageTransitivePinningEnabled=true` and pin ALL `Microsoft.Extensions.*` to one 10.x version.

**VALIDATE:** each backend project builds with `dotnet build`.  
**PITFALL:** Orleans 3.7.2's legacy `OrleansCodeGenerator.MSBuild` + `*Build.csproj` codegen projects are the "blocked at the door" risk. The spike proved they DO build on the net10 SDK; if they fail here it is likely a local SDK/config issue -- escalate with the build error.

### 3c. Runtime validation

Run the `LibLifeCycleTest` simulation suite (in-memory Orleans `TestingHost`, no SQL needed).

**VALIDATE:** grain activation, transitions, subscriptions, and especially **exception propagation across grains** all pass with no `PlatformNotSupportedException`/BinaryFormatter error.  
**ESCALATE-IF:** any BinaryFormatter/serialization runtime failure.  
**OUT OF SCOPE:** SQL Server storage/clustering on this machine (Mac ARM SQL issues) and the PostgreSQL conversion. Use in-memory storage for validation here.

### 3d. net10 specifics -- apply in this order

1. **Tooling first:** confirm the whole solution still builds on net7 under the net10 SDK before flipping any TFM (isolates SDK issues).
2. **Central Package Management:** pin ALL `Microsoft.Extensions.*` to one 10.x version to prevent `MissingMethodException` on `BuildServiceProvider`.
3. **Silo host (the #1 runtime risk):** net9 P7+ turns on `ValidateOnBuild` + `ValidateScopes` by default in Development; Orleans 3.7's open-generic / scoped-in-singleton registrations will throw at startup. Add `UseDefaultServiceProvider(fun o -> o.ValidateScopes <- false; o.ValidateOnBuild <- false)` BEFORE first run.
4. **F# default LangVersion jumps to F# 10** on the net10 SDK. Expect: `FS0842` (misapplied attribute), `FS0058` (pseudo-nested modules in types), `FS3873` (bare `{ 1..10 }`), and tightened `#nowarn` syntax. Per-project escape hatch: `<LangVersion>9</LangVersion>`.
5. **`--warnaserror` will go red:** new SYSLIB obsoletions (SYSLIB0050/0051 legacy serialization, 0057/0058-0062) + warning waves + NuGetAudit on Orleans 3.7's transitive graph become errors. Stage a `<WarningsNotAsErrors>` list rather than `<NoWarn>`; suppress SYSLIB by exact id.
6. **Package cascade:** Giraffe 8.2.0, `Microsoft.Data.SqlClient` 7.0.2, `FSharp.Core` SDK-bundled 10.x (do not pin), STJ 10.x in-fx (cache `JsonSerializerOptions`). **Fleece 0.10.0 is stale (2022, no net10 build) -- smoke-test it.** BinaryFormatter stays removed/throws; do NOT add the compat package.
7. **Sequence:** SDK-first, CPM/Extensions pinning + WarningsNotAsErrors, pilot one leaf lib, flip TFMs leaf-first / host-last, fix cascade, flip silo host last (with the DI-validation opt-out), full test suite + live silo smoke.
8. **Re-confirm via CI gate:** Orleans 3.7.2 has no vendor-documented net10 support; pin the proven build behind a CI smoke test.

---

## Phase 4: ReactXP to react-native-web seam {#phase4}

**Owner:** STRONG model sets the pattern + does the first primitive + the style/animation layers. Weaker model fans out the remaining primitive wrappers ONCE the pattern is proven.  
**Goal:** re-implement `LibClient/src/ReactXP/*` against RN (native) + react-native-web (web), keeping the F# public surface (`RX.*`/`LC.*`, the `makeViewStyles` DSL) identical.

**Do NOT start the fan-out until a stronger model has:**
1. Re-pointed `ReactXPBindings.fs` from `@chaldal/reactxp` to `react-native` (+ bundler alias `react-native` to `react-native-web` for web).
2. Ported ONE primitive end to end (e.g. `View`) as the reference pattern, validated in `dev-web` + native.
3. Re-targeted the `Styles/{Legacy,New}` DSL to emit RN style objects.
4. Established the animation layer (Reanimated 4 / RNGH 3 / Moti) F# wrappers. **Worklets are NOT authored in F#** (settled 2026-06-29, see [worklet rule](#worklet-rule) below).

### Proven stack and version pins {#version-pins}

The spike (`eggshell-rnw-spike`) validated these exact versions. Install RN libs with `npx expo install`, not raw `npm install`, so versions stay Expo-compatible:

| Package | Version | Note |
|---|---|---|
| `react` / `react-dom` | **19.2.3** | React 19 (form Actions, `<title>` metadata, ref-as-prop) |
| `react-native` | **0.85.3** | New Architecture is default (Fabric/TurboModules/JSI) |
| `react-native-web` | **^0.21.2** | maintenance mode (Oct 2024 latest); stable, production-proven |
| `react-native-reanimated` | **4.3.1** | v4 = New-Arch only; UI-thread animation |
| `react-native-worklets` | **0.8.3** | NEW split package -- Reanimated 4 moved its worklet runtime + Babel plugin here |
| `react-native-gesture-handler` | **~2.31.1** | replaces RN `PanResponder` (UI-thread gestures) |
| `moti` | **^0.30.0** | declarative animation (props); works on web via RNW -- **the default animation path** |
| `expo` (+ `@expo/metro-runtime`, `babel-preset-expo`) | **~56.x** | toolchain: Metro for native AND web |

**Tooling changes vs today's ReactXP/webpack web build:**
- **babel.config.js:** `presets: ['babel-preset-expo']`, `plugins: ['react-native-worklets/plugin']` -- the worklets plugin **must be listed last**.
- **App root:** wrap the tree in `GestureHandlerRootView` (RNGH requirement) at the JS root.
- The current `LibClient/vendor/reactxp-native-common/` + `postinstall` copy mechanism is deleted with ReactXP.

### Worklet rule (empirically settled 2026-06-29) {#worklet-rule}

**Fable worklets do NOT run on the UI thread.** A dev-build probe produced a definitive result:
- `runOnJS` (or any host-function call) inside a Fable-emitted worklet ABORTS `libworklets` -- `Fatal signal 6 (SIGABRT)` at `facebook::jsi::Function::getHostFunction`.
- `useAnimatedStyle (fun () -> ...)` runs on the JS thread, not the UI thread (verified by blocking JS for 3s: animation froze mid-way, confirming JS-thread execution).

**Therefore: author animation declaratively -- Moti (`from`/`animate`/`transition` props) or Reanimated's declarative/CSS API.** For any animation that genuinely needs a UI-thread worklet, write that worklet in a tiny vetted JS shim (`ThirdParty`/`worklets.js` is the template) and call it from F#.

### Per-primitive mapping table {#primitive-mapping}

| EggShell wrapper | RN/RNW target | Effort | Key notes |
|---|---|---|---|
| `View` | RN `View` (RNW: `<div>`) | Low | 1:1 flex/spacing/color. Web-only style features (hover, nested selectors, breakpoints) become RNW-web-only. |
| `Text` / `UiText` | RN `Text` (RNW: `<span>`) | Low | `allowFontScaling`/`maxFontSizeMultiplier` carry over. **RNW `Text` lacks `onLongPress`** -- plan around. |
| `Button` | RN **`Pressable`** | Low | EggShell already routes taps through `LC.Pressable`. Keep the F# `LC.Button` surface; back it with `Pressable`. |
| `TextInput` | RN `TextInput` | Med | `placeholderTextColor`, `secureTextEntry`, `keyboardType` map. **RNW lacks rich text.** Preserve themeable bg/label fields. |
| `ScrollView` | RN `ScrollView` | Low | Maps. **`RefreshControl` is NOT implemented on RNW** -- gate pull-to-refresh to native. |
| `Image` | RN `Image` | Low | `source`/`resizeMode` map; check remote-vs-local source shapes. |
| `ActivityIndicator` | RN `ActivityIndicator` | Low | 1:1 (`size`, `color`). |
| `GestureView` | **RNGH `Gesture` + `GestureDetector`** | **High** | Biggest behavior change: RN `PanResponder` is gone. Every gesture must keep a non-gesture alternative (a11y 2.5.1). |
| `Picker` | custom (View/Pressable/Modal) or `@react-native-picker/picker` | Med | EggShell's Picker is already a custom Field+Popup; mostly rides the View/Pressable ports. |
| `Link` | router `Link` (web: react-router-dom, native: react-router-native) | Low | No RN `Link` primitive; keep `LibRouter` as-is. |
| `VirtualListView` | RN **`FlatList`/`VirtualizedList`** | **Med-High** | Real port: ReactXP VirtualListView API differs from FlatList. Re-map the F# surface to FlatList semantics. |
| `WebView` | **`react-native-webview`** | Low-Med | Native solid; RNW web support is partial (iframe-style) -- verify. |
| `Animatable{View,Text,Image,TextInput}` | **Reanimated `Animated.*` + Moti** | **High** | Default to Moti. Honor reduce-motion. |

**RNW limitations to design around (no clean native equivalent on web):**
- `KeyboardAvoidingView`, `StatusBar`, `LayoutAnimation`, `NativeModules`: mocked/partial on web.
- `RefreshControl`, `TouchableNativeFeedback`, `Alert`: not implemented on RNW.
- `Text` lacks `onLongPress`; `TextInput` lacks rich text.

If a primitive depends on one of these, branch with `LC.With.ScreenSize`/platform or provide a web alternative -- do not assume parity.

### Per-component recipe (follow verbatim) {#phase4-recipe}

Do ONE primitive at a time. Do not start the next until the current is green on web + iOS + Android. Run `export DOTNET_ROOT="$HOME/.dotnet"` once per shell.

1. **Scope / STOP check.** If the component is `GestureView` or any `Animatable*`, STOP -- that is the animation/gesture workstream (design work, strong model; see escalation triggers below).
2. **Read reference, then target.** Read `LibClient/src/ReactXP/Components/View/View.fs` (the pattern to imitate), `LibClient/src/ReactXP/RNSeam.fs` (the adapters to call), and the current `<Primitive>.fs` (note its import and every optional arg / DU / record type).
3. **Keep the F# signature byte-identical.** Do not rename, drop, or reorder any public param. Keep `?xLegacyStyles`, every `accessibility*`/`aria*`/`testId`/`liveRegion`/`importantForAccessibility` param, public DU/record paths, and themeable fields (`Input.Text.Theme`, `PickerInternals.Field.Theme`). If RN/RNW cannot express a param, the param STAYS and is gated/no-op'd -- never silently dropped.
4. **Repoint import + port styles.** Change `ReactXPRaw?X` / `@chaldal/reactxp*` to the RN target from the mapping table. Route style construction through `RNSeam.createViewStyle` / `createTextStyle`.
5. **Wire seam adapters** (in `View.fs` order), before `createElement`:  
   `assignPointerEvents` then `assignTestId` then `assignOnLayout` (MANDATORY if the primitive has `onLayout` -- raw forwarding zeroes width/height) then `assignAccessibility` (role via `accessibilityRole |> Option.bind RNSeam.mapAccessibilityRole |> Option.map box`, never the raw int enum) then if pressable `assignPressableFeedback` then `RNSeam.createElement RNSeam.<Target> __props kids`.
6. **Build `Web Debug` + defeat the Fable false-green:**

```bash
touch LibClient/src/ReactXP/Components/<Primitive>/<Primitive>.fs
rm -rf LibClient/.build/web/fable
dotnet build LibClient/src/LibClient.fsproj -c "Web Debug" 2>&1 | tee /tmp/build-<p>.log
rg "Started Fable compilation" /tmp/build-<p>.log    # MUST be present
rg "error FS" /tmp/build-<p>.log                      # MUST be empty
```

7. **Verify WEB.** `cd AppEggShellGallery && ../eggshell dev-web` (restart after any LibClient change); `npm run observe -- snapshot -p web` and `--orientation landscape`. No console/health errors, correct in both orientations.
8. **Verify iOS.** `npx react-native run-ios --simulator "iPhone 16" --no-packager`; relaunch the app; `xcrun simctl io booted screenshot`; read `xcrun simctl spawn booted log show --last 2m`. Repeat rotated. No red box, clean log.
9. **Verify Android.** `adb reverse tcp:8081 tcp:8081`; force-stop + monkey-launch; `screencap`/`pull`; `adb logcat -d | grep -iE "Uncaught|Error|Warning|unique .key.|Color is expected"`. If the change is absent:

```bash
curl -s "http://localhost:8081/index.bundle?platform=android&dev=true" | grep -c <marker>
# 0 => restart Metro --reset-cache
```

10. **Update the gallery page** (`AppEggShellGallery/src/Components/Content/` page must use the new F# API in pure F#; add one if missing). Re-run step 7.
11. **Update the [Engineering Log](./knowledge-base/engineering-log.md)** (dated, newest at top): symptom, cause, fix, files for anything corrected.

### Phase 4 pitfall checklist {#phase4-pitfalls}

If your symptom is here, apply the fix. If it is not here and not obvious in one attempt, STOP.

| Symptom | Cause | Fix |
|---|---|---|
| `onLayout` width/height 0; drawer "always open"; responsive collapses | RNW delivers `{nativeEvent:{layout}}`; ReactXP flattened it; raw forwarding drops it | `RNSeam.assignOnLayout __props onLayout` |
| `role="16"` in HTML; a11y role ignored | `AccessibilityRole` is an int enum boxed raw | `accessibilityRole |> Option.bind RNSeam.mapAccessibilityRole |> Option.map box` |
| Header view ~2em; children inherit big font | `Header -> "header" -> role="heading" -> <h1>` default font size | Map `Header -> "banner"` (RNW: `<header>` landmark) |
| `lineHeight` huge (e.g. 384px) | Numeric `lineHeight` read as CSS unitless multiplier | Route through `RNSeam.createTextStyle` (appends `px`) |
| `flex 0` element collapses to 0 | Identity style fn lets `flex:0` hit CSS as `0 1 0%` | Use `RNSeam.createViewStyle` (replicates ReactXP flex expansion) |
| Pure-F# `[<Component>]` ignores its theme | Pure-F# components don't call `ComponentRegistry.GetStyles` | Bake themed values into `makeViewStyles`/`makeTextStyles` |
| Absolute overlay/drawer wrapper swallows clicks + wheel | Wide/tall absolute wrapper stays a pointer target | `blockPointerEvents = true` (RN `box-none`); park closed overlay fully off-screen |
| Build "green" but edit ignored | Fable cache false-green | `touch` `.fs` / `rm -rf .build/<plat>/fable`; confirm `Started Fable compilation` |
| vendor/node_modules patch has no effect | Metro bundles built JS, not `src` | Patch the built file; verify `curl ...index.bundle... | grep -c marker`; prefer an F# seam fix |
| Runtime `Color is expected to match #[0-oa-f]{6}` | Uppercase hex in `Color.Hex` | Lowercase all hex |
| Filled rounded view shows square corners (Android) | Missing `Overflow.Hidden` | Add `Overflow.Hidden` |
| Responsive row won't stack on handheld | `LC.Row` appends `FlexDirection.Row` last | Use `RX.View` + a direction-correct style |
| Dark-mode inputs render white | Input theme hardcoded white / not applied per appearance | Keep + apply themeable bg/label per appearance |
| Dev-only "unique key" warning | Fable.React `contextProvider` doesn't key children | Wrap children with `tellReactArrayKeysAreOkay` (F#), not a node_modules patch |
| `.fsproj` edit ignored by watch | Fable doesn't watch `.fsproj` | Restart `dev-native`/`dev-web` |
| Web blank page + `ReferenceError: map is not defined` | Fable partial-write left a stray `map` line | Delete the `.js`, `dotnet fable ... --noCache`, restart |

### Phase 4 STOP-and-escalate triggers {#phase4-escalate}

Hand back to a strong model; do not guess.

- **A. Seam design.** A prop/event whose RN shape has no existing `RNSeam` adapter and you would design one. Report: primitive, full F# signature, the prop, and the ReactXP-vs-RN shape difference.
- **B. Worklet / SIGABRT.** `Fatal signal 6 (SIGABRT)` / `getHostFunction`, or anything needing a UI-thread worklet. `Animatable*` and `GestureView` are pre-flagged. Report the animation/gesture intent + crash log.
- **C. Fable AST / plugin.** `Meta/FablePlugins` or any transpile/AST/plugin error. Do not edit the AST. Report the full Fable error + file.
- **D. Build red after 2 attempts** on something not in the pitfall checklist. Report exact `error FS.../red-box/logcat` text, file, and both attempts tried.
- **E. Any new gesture/animation design question** (PanResponder to RNGH mapping, gesture-state fields, Reanimated/Moti layer, reduce-motion, a gesture lacking a non-gesture alternative).
- **F. No clean RN/RNW equivalent**, or a param/style feature that will not map and the fallback is unclear. Report the primitive + the RNW limitation it hits.
- **G. Environment breakage** (`dotnet` killed/137, emulator/sim won't boot, Metro dead after `--reset-cache` + clean). Report the failing command + output.

### Phase 4 command crib {#phase4-commands}

All blocks assume `export DOTNET_ROOT="$HOME/.dotnet"` first.

```bash
# Build one framework lib (custom config, NOT plain Debug):
dotnet build LibClient/src/LibClient.fsproj -c "Web Debug" 2>&1 | tee /tmp/build.log
# Force-recompile / defeat Fable false-green:
touch LibClient/src/ReactXP/Components/<Primitive>/<Primitive>.fs
rm -rf LibClient/.build/web/fable        # .build/native/fable for native
rg "Started Fable compilation" /tmp/build.log   # present
rg "error FS" /tmp/build.log                    # empty
# Dev-web (gallery; restart after any LibClient change):
cd AppEggShellGallery && ../eggshell dev-web    # :8082 ; open http://127.0.0.1:8082
# Dev-native (one watch + one Metro = both platforms):
cd SuiteTodo/AppTodo ; ../../eggshell dev-native & ; npx react-native start --port 8081 &
# Screenshot WEB (Tier 2, both orientations):
npm run observe -- snapshot -p web ; npm run observe -- snapshot -p web --orientation landscape
# Screenshot iOS:
npx react-native run-ios --simulator "iPhone 16" --no-packager
xcrun simctl io booted screenshot /tmp/ios.png
xcrun simctl spawn booted log show --last 2m --style compact | tail -300
# Screenshot ANDROID:
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB reverse tcp:8081 tcp:8081
$ADB shell am force-stop com.eggshell.apptodo
$ADB shell monkey -p com.eggshell.apptodo -c android.intent.category.LAUNCHER 1
$ADB shell screencap -p /sdcard/s.png && $ADB pull /sdcard/s.png /tmp/and.png
$ADB logcat -d | grep -iE "Uncaught|Error|Warning|unique .key.|Color is expected"
# Verify a patch reached the bundle:
curl -s "http://localhost:8081/index.bundle?platform=android&dev=true" | grep -c "<marker>"  # 0 => Metro --reset-cache
# Stale-process cleanup:
pkill -f "eggshell dev-native"; pkill -f "dotnet fable"; pkill -f "fable.dll"
pkill -f "react-native start"; pkill -f "qemu-system-aarch64"; pkill -f "emulator -avd"
```

### Phase 4 progress table {#phase4-progress}

A component is **done** only when web + iOS + Android (portrait + landscape) are green AND the gallery page + LEARNINGS row are updated.

Already ported through `RNSeam`: View, Text, Button/Pressable, Image, TextInput, ScrollView, ActivityIndicator, VirtualListView (FlatList), AccessibilityInfo, NetInfo.

| Component | web | ios | android | gallery | learnings |
|---|---|---|---|---|---|
| UiText | -- | -- | -- | -- | -- |
| Platform APIs / AccessibilityInfo | -- | -- | -- | n/a | -- |
| NetInfo | -- | -- | -- | n/a | -- |
| SVG | -- | -- | -- | -- | -- |
| Link | -- | -- | -- | -- | -- |
| Picker | -- | -- | -- | -- | -- |
| WebView | -- | -- | -- | -- | -- |
| VirtualListView | done | -- | -- | -- | done |
| Motion (Moti surface) | -- | -- | -- | -- | -- |
| GestureView | -- | -- | -- | -- | -- |
| Scrim / Carousel / SegmentedControl / Draggable | -- | -- | -- | -- | -- |

---

## Phase 5: full-stack TODO reference app + templatized bootstrap (goal B) {#phase5}

**Owner:** weaker model for mechanical bulk; escalate on codec-gen / Orleans wiring / native template parameterization.  
**Sequencing:** after Phases 1-3 (Fable 5 + net10 + SignalR). **Before** Phase 4 (RNW seam). Phase 5 proves the stack on the current ReactXP seam; Phase 4 re-points the seam without re-proving app architecture.  
**North star:** one command from fresh scaffold to running web stack; documented three-terminal recipe for native; CI runs simulation + Playwright.

### Feature acceptance checklist

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
| Scaffold | `eggshell create-app` reproduces the above |

### 5A. Backend Todo ecosystem

Mirror `SuiteJobs/Ecosystem` layout (`SuiteTodo/Ecosystem/Todo.Types/`, `LifeCycles/`, `Tests/`, `Launchers/Dev/`).

Types: `TodoId`, `Todo` record, `TodoAction`, `TodoConstructor`, `TodoLifeEvent`, `TodoOpError`, `TodoIndex` (with `TodoSearchIndex.Title` for full-text search), `EcosystemDef`.

LifeCycle: reject empty title; emit `Created`; transitions update subject + emit LifeEvents; `TodoListView` projection sorted by `CreatedOn`; auto-archive timer (`withTimers`).

Tests: mirror `SuiteJobs/Ecosystem/Tests/Simulation.fs` (construct, toggle, assert LifeEvent + state; EmptyTitle rejection; timer auto-archive; IndexPredicate.Matches on Title).

**VALIDATE:** `dotnet build` each project green; `dotnet test SuiteTodo/Ecosystem/Tests` passes.

### 5B. Backend Dev host (V1 HTTP + SignalR)

`SuiteTodo/Launchers/Dev/Host/` mirroring an existing in-repo Dev launcher: registers the Todo ecosystem on `LibLifeCycleHost`; exposes V1 generic HTTP API + `/api/v1/realTime` SignalR; in-memory Orleans storage; `appsettings.Development.json` with URLs the frontend `Config.BackendUrl` expects.

**VALIDATE:** host starts; negotiate endpoint responds; HTTP view query returns JSON; SignalR push works (add todo in one client, subscription updates in another).

### 5C. Frontend TODO app (`SuiteTodo/AppTodo/`)

Pure F# only. Config, services, components: `Route/Todos.fs` (subscribes to `TodoListView` via `LC.With.Subjects`), `Dialog/EditTodo.fs` (validation feedback), `Route/Settings.fs`. Every interactive control: `?testId` via `A11ySlug.testId "todo-*" ...`. User actions logged via `UiActionLog`.

**VALIDATE:**

```bash
cd SuiteTodo/AppTodo && eggshell dev-web    # web build
# Browser: add/toggle/edit; second tab updates without refresh
# eggshell dev-native + Metro + npx react-native run-android (emulator)
# eggshell dev-native + Metro + npx react-native run-ios --no-packager (simulator)
```

For the native dev/observe loop, follow the [Android](./runbooks/android.md) and [iOS](./runbooks/ios.md) runbooks rather than improvising.

### 5D. UI automation (`SuiteTodo/AppTodo/audit/`)

The `audit/` toolkit already exists. The minimum the template must ship:

1. `audit-todo-web.mjs` (Playwright): add todo; toggle done; open edit dialog, change title, save; assert no console errors.
2. `.github/workflows/todo-app.yml`: `dotnet test` simulation; scaffold smoke (`eggshell create-app TodoCI` in temp dir); Playwright against reference `SuiteTodo/AppTodo`.

**VALIDATE:** `node audit/audit-todo-web.mjs http://127.0.0.1:9080` passes with stack running.

### 5E. Templatize into `Meta/LibScaffolding` (after 5A-5D green)

Template buckets: **KEEP + modernize:** app shell, Bootstrap, Config, Services, Navigation, I18n, Icons, ErrorMessages. **REPLACE:** Landing/Bananas/Mangoes routes with Todo routes + EditTodo dialog. **DELETE:** all `.render` templates; Chaldal-specific lib refs. **ADD:** `audit/`, `docker-compose.yml.template`, `android/`/`ios/` skeletons, `configSourceOverrides.native.js.template`, `dev-stack.sh.template`, GitHub workflow template.

**VALIDATE:** `eggshell create-app TodoSmoke` in temp directory, then `./initialize`, then `./dev-stack up`, then Playwright smoke passes.

---

## Phase 6: Docker SQL Server + persistent dev stack {#phase6}

**Owner:** weaker model OK.  
**Sequencing:** after Phase 5 reference app is green on in-memory storage.  
**Goal:** `./dev-stack up` brings up SQL Server in Docker, Orleans uses ADO.NET persistence, Mac ARM dev is unblocked without native SQL install.

### 6a. Docker compose

```yaml
services:
  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "EggShell_Dev_123!"   # dev only
    ports:
      - "1433:1433"
    volumes:
      - todo-sql-data:/var/opt/mssql
volumes:
  todo-sql-data:
```

**Mac ARM note:** use Azure SQL Edge if the standard SQL Server image fails on ARM (`mcr.microsoft.com/azure-sql-edge`).

### 6b. Orleans ADO.NET clustering + storage

Update Dev host `appsettings.Development.json` connection string to `localhost,1433`. Wire Orleans ADO.NET clustering + grain storage. Run DB init script `sql/init.sql`.

**VALIDATE:** `docker compose up -d sql`; host starts; grain state survives host restart; simulation tests still pass (may use in-memory override in Tests project).

### 6c. Fold into one command

Update `dev-stack.sh`:
1. `docker compose up -d sql` + wait-for-health script
2. Start backend host
3. Start `eggshell dev-web`

**VALIDATE:** fresh clone, `./initialize`, `./dev-stack up`, todo CRUD persists across host restart.

**OUT OF SCOPE:** Postgres migration, production K8S/Fabric deploy templates, SQL Server on CI.

---

## Consolidated escalation triggers {#escalation}

Hand back to a stronger model for any of these; do not guess.

- `Meta/FablePlugins` does not build after the 3 known edits (AST drift).
- Any Fable compile error not in a PITFALL list; the `#241` cache or array-equality symptoms.
- Backend runtime serialization / BinaryFormatter failure; exotic `ISerializable` types.
- ASP.NET Core 10 API breaks beyond a version bump.
- The SignalR typed-hub contract not mapping cleanly to a stock hub.
- ANY part of the Phase 4 seam design (pattern-setting, style DSL, animation layer, a primitive with no clean equivalent). See the full [Phase 4 escalation triggers](#phase4-escalate) list.
- Anything where the fix is not obvious in one attempt.

---

## Reference artifacts {#reference-artifacts}

| Resource | Location |
|---|---|
| SignalR (modular) | `../eggshell-signalr/` (`LibSignalRClient`, `LibSignalRServer`) |
| Backend patterns | in-repo `SuiteJobs/` (lifecycle, view, timer, simulation tests, codec-gen) |
| Frontend + native + audit | in-repo `AppEggShellGallery/` |
| Target output | `SuiteTodo/` (Phase 5 reference; becomes scaffold source) |
| Frontend spike | `../eggshell-rnw-spike/` (transport + animation probes only) |
| Backend spike | `../orleans-net10-spike/` (Orleans 3.7.2 on net10) |
| Strategy doc | [ReactXP to RNW](./modernization/reactxp-to-rnw.md) |
