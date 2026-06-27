# Learnings

Running log of things initially missed/assumed wrong and later corrected, plus toolchain gotchas.
Newest entries at the top. See `CLAUDE.md` rule 1.

---

## 2026-06-27 — Native Components nav crash + markdown on iOS

**`TypeError: Cannot read property 'add' of undefined` when tapping navbar routes (Components, etc.)** — not MarkdownViewer. Stack: `requestCurrentTransition` → `startTransition` → react-router `push` → EggShell `Navigation.Go`. Cause: `LR.Router` defaulted `future.v7_startTransition = true`; React Router wraps navigations in `React.startTransition`, which breaks on **RN 0.76 bridgeless** (`concurrentRoot: true`). Fix: `LibRouter.Components.Router.defaultFuture` — enable `v7_startTransition` on web only; `None` on native.

**Native gallery markdown (Components/Docs/Tools):** webpack dev-web serves `public-dev/`; Metro also serves it at `/public-dev/...`. Set `MaybeInBundleResourceUrlHashedDirectoryPrefix = "/public-dev"` in `configSourceOverrides.native.js` so `PrepareInBundleResourceUrl` resolves fetchable paths. Use platform-specific `AppUrlBase`: Android emulator `10.0.2.2:8081`, iOS simulator `localhost:8081`.

**MarkdownViewer on native:** use `react-native-render-html` (same pattern as FormatfulText), not `dangerouslySetInnerHTML` on a `div`. Skip inline `onclick` link rewriting on native (render-html ignores it). Metro `extraNodeModules` must include `react-native-render-html` + Showdown deps.

**`ErrorUtils` import:** RN 0.76 bridgeless does not export `ErrorUtils` from `react-native` package entry; use `global.ErrorUtils` in `index.js`. Filter `react-native-render-html` defaultProps deprecation spam from `console.error` (otherwise LogBox red screen on Components page).

---

AppEggShellGallery **`./gradlew assembleDebug`** now succeeds on RN **0.76.5** / Gradle **8.10.2** / compileSdk **35**.
`eggshell dev-web` serves on `http://127.0.0.1:8082/` (WDS 5, HTTP 200). Scaffolding templates updated to match.

**Android fixes (gallery):**
- **`debug.keystore` missing** — generate with standard RN debug params (`storepass`/`keypass` = `android`, alias `androiddebugkey`). Added idempotent `keytool` step to `initialize` scripts (gallery + scaffold template).
- **Flipper removed in RN 0.76** — delete `ReactNativeFlipper.initializeFlipper(...)` and import from `MainApplication.kt`; compile fails with deprecation-as-error otherwise.
- **RN 0.76 native library merging** — `SoLoader.init(this, false)` crashes at runtime with `libhermes_executor.so not found`. Use `SoLoader.init(this, OpenSourceMergedSoMapping)` in `MainApplication.kt` (maps merged libs like `hermes_executor` → `hermestooling`).
- **`settings.gradle`** — use `@react-native/gradle-plugin` autolinking (`autolinkLibrariesFromCommand()`), not legacy `native_modules.gradle`.
- **`react-native.config.js`** — `project.android.packageName` required for autolinking code gen.
- **CodePush autolink** — set `platforms.android.sourceDir` to `.../react-native-code-push/android/app` (not `android/` root).
- **Force `androidx.core:1.15.0`** — newer 1.19.x pulls AGP 9 requirements that conflict with compileSdk 35 toolchain.
- **Pin `react-native-maps@1.20.1`** (ThirdParty/Map) — 1.27.x breaks Android ViewManager codegen on RN 0.76.
- **`react-native-svg@15.11.2`** in LibClient — 13.x incompatible with RN 0.76 native codegen.
- **Push notification AndroidX** — `LibPushNotification/Client/patch-push-notification-android.js` replaces old support-lib appcompat dep.

**Scaffolding template sync:**
- Android gradle files, Kotlin `MainApplication.kt`/`MainActivity.kt`, metro `mergeConfig(getDefaultConfig(...))`, `react-native.config.js` with CodePush `sourceDir`, gradle-wrapper **8.10.2**, `enableJetifier=false`, Hermes on.

**Still deferred:** iOS simulator run blocked on this machine (see iOS entry below).

**Emulator dev workflow (validated 2026-06-27):** `assembleDebug` + install is not enough. Need concurrently: (1) `eggshell dev-native` (Fable → `.build/native/commonjs`), (2) `npx react-native start` on `:8081`, (3) `configSourceOverrides.native.js` (from template; gitignored), (4) `adb reverse tcp:8081 tcp:8081`. Metro `extraNodeModules` must include `@react-native-community/netinfo`, `react-native-svg`, etc. (not just `react-native.config.js` roots). If `copyStaticFiles` produces corrupt PNGs under `.build/native/assets`, recopy from `images/` (symlink target). Success signal in logcat: `Running "RXApp"`.

---

## 2026-06-27 — iOS scaffold for AppEggShellGallery (RN 0.76.5)

The gallery had **no `ios/` directory at all** (only Android was in repo). Created from RN **0.76.5** CLI template, configured for this app:

- Target/scheme: `AppEggshellGallery`
- Bundle ID: `com.eggshell.appgallery` (matches Android)
- RN module name: `RXApp` (matches `MainActivity.kt`)
- Display name: `Egg Shell Gallery`
- **Podfile:** `platform :ios, '15.5'` (CodePush 9.x requires ≥15.5), `$RNFirebaseAnalyticsWithoutAdIdSupport = true`, `use_modular_headers!` (Firebase static libs)
- **`pod install` green** — 97 pods (Hermes, CodePush, RNFB, maps 1.20.1, svg 15.11.2, etc.)
- **`eggshell build-native` green**

**Simulator run blocked here:** Xcode **26.5** SDK requires **iOS 26.5 Simulator** runtime; machine only has 18.2 + 26.2 runtimes. `xcodebuild -downloadPlatform iOS` fails with **Insufficient space (needs ~8.5 GB, ~6.4 GB free)**. Until runtime is installed (or disk freed), `xcodebuild` / `run-ios` show zero eligible simulator destinations.

**Update 2026-06-27:** Deleted old iOS **18.2** + **26.2** runtimes (~31 GB freed), downloaded **iOS 26.5 Simulator (23F77)** successfully. `xcodebuild -showdestinations` now lists iPhone 17 Pro / iPad simulators on OS 26.5. Ready for `run-ios` / gallery iOS build test.

**iOS dev workflow (once simulator runtime available):** same as Android but without `adb reverse`: (1) `eggshell dev-native`, (2) `npx react-native start --port 8081`, (3) `configSourceOverrides.native.js`, (4) `npx react-native run-ios` or open `ios/AppEggshellGallery.xcworkspace`. Use `.xcworkspace`, not `.xcodeproj`.

**Still TODO for iOS:** `GoogleService-Info.plist` (Firebase; Android has `google-services.json`), scaffolding `ios/` template for `eggshell create-app`, physical-device test after installing iOS 26.5 **device** support in Xcode Components.

**Android logcat flood (ReactXP + React 18):** ~1,600 `legacy childContextTypes/contextTypes` warnings from ReactXP `View`/`Text`/`Button`, each printing ~15 stack lines → 100k+ `W ReactNativeJS` lines. React Native's renderer emits these via **`console.error`**, not `console.warn`; `LogBox.ignoreLogs` alone does not suppress adb output. Fix: filter both `console.warn` and `console.error` in app `index.js` (patterns + `/^\s+in /` for component stacks). Also filter noisy `LC.Icon` legacy-styles warnings in gallery. Long-term fix is ReactXP/context migration (goal H later).

**Gallery `Code` component render crash on native:** `GetInitialEstate` tried to read React `children` from the F# `Props` record (no children field) → `Cannot read property 'props' of undefined`. Fix: defer extraction to `ComponentDidMount` via `this.props?children`, with null-safe recursive `tryExtractStringChildren`. **`copyStaticFiles`** copied via gulp through `public-dev/images` symlink and corrupted PNG/JPG binaries for Metro; fixed to `fs.copyFile` from `images/` → `.build/native/assets/public-dev/images/`.

---

## 2026-06-27 — NPM dependency upgrade (framework-wide)

Full framework NPM upgrade for security and performance. Validated with `eggshell build-lib` on LibClient/LibRouter/LibUi*, `eggshell test-build` + `eggshell dev-web` on AppEggShellGallery, and **`assembleDebug`** on Android.

**Key version bumps:**
- `lodash` → **4.18.1** everywhere (4.17.23 still flagged by npm audit for newer CVEs)
- `typescript` toolchain → **5.7.x**; all Meta/Lib* tsconfigs got `skipLibCheck: true` and `target: es2018` (needed for `/s` regex in LibRtCompiler)
- `gulp` → **5.0.1** in LibScaffolding + LibRtCompiler (clears chokidar 2 / braces chain)
- `webpack-dev-server` → **5.2.5+** in Meta/LibFablePlus only; `webpack.config.js` binds to `127.0.0.1` and sets CORP headers
- `glob` → **8.1.0** with `glob-promise@6` (glob 11 conflicts with glob-promise peer `^8`)
- Removed unused `gulp`/`glob` devDeps from LibClient, LibRouter, LibUiSubject, LibPushNotification (were pulling ~300+ transitive packages each)
- AppEggShellGallery: **react-native 0.76.5**, `@react-native/metro-config 0.76.5`, babel 7.26
- ThirdParty: firebase/RN-firebase 21.x, image-picker 7.x, showdown 2.1, recharts 2.15, fbsdk-next 13.x

**Package count impact (approx, from npm audit output):**
| Package | Before | After |
|---------|--------|-------|
| LibClient | 414 | 82 |
| LibUiSubject | 357 | 21 |
| LibScaffolding | 439 | 236 |
| LibRtCompiler | 614 | 386 |
| LibFablePlus | 388 | 420 |
| AppEggshellCli (top-level) | 1102 | 28 |
| ImagePicker | 714 | 4 |
| GoogleAnalytics | 818 | ~10 |

**Gotchas:**
- `glob-promise@6` peer requires `glob@^8`, not glob 11 — do not bump glob past 8 until glob-promise updates
- TypeScript 5 + `@types/gulp` pulls `@types/glob-stream` with broken stream typings — use `skipLibCheck: true`
- `@types/chokidar` is deprecated stub; chokidar 4 ships its own types — remove the @types package
- ThirdParty installs often need `--legacy-peer-deps` (react-router-native vs RN peer ranges)
- Showdown ReDoS advisory still reports on 2.x (`npm audit` "no fix") — acceptable for gallery markdown rendering; replace with `marked` later if needed
- `react-native-fbsdk` → `react-native-fbsdk-next` requires updating F# `import` paths (FacebookPixel/NativeSdk.fs)

---

## 2026-06-27 — AppEggShellGallery bootstrap used the wrong global name, and `docco.css` was missing

The gallery bootstrap was reading `chaldal.AppEggShellGallery.configSourceOverrides`, but the host page only initializes `eggshell`. That produced `ReferenceError: Can't find variable: chaldal` before the app could start. Fix: use the same global name the page defines, `eggshell`, in `AppEggShellGallery/src/Bootstrap.fs`.

The dev HTML also linked `/docco.css` even though `AppEggShellGallery/public-dev/docco.css` did not exist, which triggered the strict MIME warning from a 404 HTML response. Fix: add the missing stylesheet file so the link resolves cleanly.

## 2026-06-27 — Batch-1 parallel conversion gotchas (15 LibClient + 10 gallery, 26 agents)

Converted in one workflow run. Both LibClient and AppEggShellGallery builds green after apply-agent fixes.

**Top-level dotted module name is invalid in F#:**
`module LibClient.Components.Input.UnsignedDecimalStyles` as a TOP-LEVEL declaration is an F# parse error
(dotted module names are only valid as namespace-like qualifiers, not standalone top-level `module X.Y.Z`).
Fix: use `namespace LibClient.Components.Input` followed by `module UnsignedDecimalStyles =` (with `=` sign
and all content indented). This came up in UnsignedDecimal.fs and PositiveInteger.fs when the agents
inlined an auxiliary styles module with a dotted name. Use the `namespace + module Foo =` form exclusively
for auxiliary modules inside a converted component file.

**`ParsedTextStyles` (and other component styles types) not in scope from `namespace LibClient.Components`:**
When a converted file opens with `namespace LibClient.Components`, sibling component types like
`LibClient.Components.Input.ParsedTextStyles` need to be FULLY QUALIFIED inside that namespace — the compiler
sees `ParsedTextStyles` as an unqualified name and fails. Either open `LibClient.Components.Input` explicitly
or write the full path. Came up in Decimal.fs and UnsignedDecimal.fs which reference `ParsedTextStyles`.

**Self-referential FQN inside a module causes "type not defined" or unnecessary cycle:**
Inside `module LibClient.Components.ImageCard`, writing `LibClient.Components.ImageCard.Corners.Rounded` is
self-referential — the compiler already has `ImageCard` as the current scope. Just write `Corners.Rounded`.
Same principle: inside any module `Foo`, `Foo.Bar` is redundant; write `Bar` directly.

**Escaped `\"` before `"""` terminates a triple-quoted string early:**
A triple-quoted string `"""..."""` terminated by `\"""` (backslash before closing quotes) parses
incorrectly — the `\"` is treated as an escaped quote INSIDE the string and the `"""` sequence is not
recognized as the terminator. Remove the backslash: just end with `"""`. Came up in Padded.fs gallery page.

**Nested gallery type extension: check the exact type being extended:**
Gallery pages for `LC.Section.Padded` must extend `type Ui.Content.Section with`, NOT `type Ui.Content with`.
The sub-namespace of the LC component determines which `Ui.Content.*` type to extend. Always match the
nesting: `LC.Foo.Bar` → `type Ui.Content.Foo with [<Component>] static member Bar()`.

---

## 2026-06-27 — Runtime error 1 from stale autogenerated files; DOTNET_ROOT required in .zshrc

**GOTCHA — deleting files from `.fsproj` does NOT stop webpack bundling them (HMR watcher blindspot):**
When converting a gallery content page from render DSL to pure F#, the old
`_autogenerated_/Components/Content/Foo/Foo.TypeExtensions.fs` and `Foo.Render.fs` files must be
DELETED FROM DISK, not just removed from `App.fsproj`. The Fable/webpack HMR watcher does not re-scan
the fsproj on change — it keeps the stale source in its module graph and webpack bundles it. The stale
`TypeExtensions.js` then defines the component constructor (e.g. `Ui.Content.Sidebar()`) as a render DSL
call to `Content.Sidebar.Make`, which no longer exists → calling `undefined` throws the JS value `1` at
runtime. The React error overlay shows `ERROR 1` with a stack pointing at `Sidebar.TypeExtensions.js:31`.
Fix: `rm -rf` the autogenerated dirs; the next `dev-web` start rebuilds only what the fsproj includes.
Affected gallery pages converted this session: Content/Sidebar, Content/Card, Content/Input/PositiveDecimal,
Content/Input/UnsignedInteger — 8 stale files deleted (TypeExtensions.fs + Render.fs per page).

**`DOTNET_ROOT` must be set in `.zshrc` (PATH alone is insufficient for apphost):**
The render DSL compiler is a self-contained .NET apphost binary. When .NET is installed at a non-standard
path (here: `/Volumes/HomeX/shafayat/.dotnet`), the apphost resolves the runtime via `DOTNET_ROOT`, NOT
PATH. Without it the binary exits 131 with ".NET location: Not found" even though `dotnet --version` works
and the .NET 7.0.20 runtime is present. Fix: add `export DOTNET_ROOT="$HOME/.dotnet"` to `~/.zshrc`
(after the existing `export PATH="$HOME/.dotnet:$PATH"` line). The LEARNINGS 2026-06-26 entry noted this as
a per-command inline prefix; the permanent `.zshrc` fix makes it available to all subprocesses including the
eggshell node runner.

**Code sample strings (`LC.Text """..."""`) contain display text, not compiled code:**
When a gallery sample page shows an F# code snippet via `LC.Text """<code here>"""`, the content of that
string is a literal for display only. It has no effect on compilation. Editing it to reference real module
bindings (`Styles.nameLiteral`) does not fix a style issue — it corrupts the example the user sees in the
browser. Never modify the inside of `LC.Text` code-sample strings to reference real bindings; only change
the actual render code above/around the string.

---

## 2026-06-27 — Gallery now BUILDS & RUNS (Opus): orphan `.render`, Fable stack overflow (fixed), conversion-caller breakages, dev-web ops

Reviewed Sonnet's "sonnet run" commit and drove a full `eggshell dev-web` of AppEggShellGallery to green
(webpack "compiled successfully", dev server up). The path there surfaced a chain of issues — all below.
Net: the gallery builds and runs; the converted components are valid; the blockers were a deeply-nested
generated render (Fable stack overflow), a few conversion regressions in app/DSL callers, and toolchain setup.

**GOTCHA — converting a component MUST delete the `.render` too, not just `.typext.fs`/`.styles.fs`/`_autogenerated_`:**
Sonnet converted TopLevelErrorMessage, PositiveDecimal, UnsignedInteger, Legacy.Card but left their
`.render` files on disk (deleted only typext/styles/autogenerated). The registration generator
(`eggshell build-lib`) SCANS `.render` files, so it kept re-emitting `RegisterRender "...Foo"
LibClient.Components.FooRender.render` lines pointing at the now-deleted `*Render` modules → Fable build
failed with FS0039 "FooRender is not defined". The `dotnet build` of LibClient did NOT catch this
(it was an incremental/cached no-op). Fix: `rm` the orphan `.render`, then `eggshell build-lib` regenerates
`ComponentRegistration.fs` cleanly. **Always verify after a conversion:**
`find Components -name '*.render' | while read r; do d=$(dirname $r); b=$(basename $r .render); [ -f "$d/$b.fs" ] && [ ! -f "$d/$b.typext.fs" ] && echo "ORPHAN: $r"; done`
Also note: a failed `build-lib` run can leave stale untracked `_autogenerated_/.../Foo/` dirs — delete those too.

**Fable StackOverflow (exit 134) on a deeply-nested generated render — RESOLVED by de-nesting.**
`eggshell dev-web` reached "Compiled 456/458" then printed `Stack overflow.` and died (exit 134) deep in
Fable's optimizer (`FableTransforms.transformMemberBody` → `getTransformations` → thousands of nested
`AST.visit`/`visitFromInsideOut`). LibClient's own Fable compile finishes clean — the crash is in the gallery
APP emit phase. **Pre-existing AND depth-driven, not caused by the conversions:** reverting LibClient to the
pre-initiative commit reproduces the IDENTICAL crash (456/458, exit 134). Known Fable bug class:
github.com/fable-compiler/Fable/issues/2392.
- **Root cause:** the render DSL compiles a component's WHOLE template into ONE `render` member. The gallery's
  `Sidebar` demo packed ~250 nested `Sidebar.Item`/`Divider`/`Heading` elements (the "Components" route alone
  ~130) into that single member; Fable's optimizer recurses per nested node and blows the ~1 MB compiler
  worker-thread stack.
- **Pinpointing the file:** dev-web deletes its build dir on failure (destroys evidence). Run Fable directly
  with the dev-web flags to preserve `.build`, then diff the emitted set against the project:
  `dotnet fable src -o .build/web/fable --exclude FablePlugins --precompiledLib <LibStandard>/.build/web/fable --noParallelTypeCheck --noCache --define EGGSHELL_PLATFORM_IS_WEB`
  It reaches 457/458 — the ONE project file with no `Compiled N/458` line (ignore `\`-separated false
  positives from fsproj paths) is the culprit.
- **THE FIX (works):** move the long element lists out of the `.render` into plain F# helper functions that
  return `castAsElement [| ... |]`. An F# **array literal is depth-1 (flat)** — Fable maps over it iteratively,
  no deep recursion — even for 130+ items. The `.render` then just calls `{=Helper.fn args}`. Put the helper
  `.fs` BEFORE the component's `.typext.fs` in the `.fsproj` (the generated `_Render.fs` references it; it
  resolves because the generated render `open`s the component's parent namespace). See
  `AppEggShellGallery/src/Components/Sidebar/SidebarContent.fs`.
- **Workarounds that DON'T work:** `ulimit -s` (64 MB; .NET worker threads ignore RLIMIT_STACK) and
  `DOTNET_Thread_DefaultStackSize` (Fable sets its compile-thread stack explicitly; the knob is internal,
  dotnet/runtime #107183). Serializing with `DOTNET_PROCESSOR_COUNT=1` does NOT help either. Only de-nesting does.
- **As we convert more `.render` components, watch for the inverse risk:** retiring the DSL replaces these
  giant generated members with explicit F#; keep using flat `[| ... |]` arrays for long child lists rather than
  deeply nested element trees, and the overflow stays gone.

**Converting a component can break APP/DSL callers in ways `dotnet build LibClient` never catches.**
The LibClient build (and even `dotnet fable LibClient/...`) only validates the framework. App callers (gallery,
Suites) and the gallery's regenerated `_autogenerated_` renders are only checked by a full app build. The full
`eggshell dev-web` surfaced three conversion regressions that had passed every LibClient build:
- **Dropped `?xLegacyStyles` →** the render DSL emits `xLegacyStyles=...` on EVERY component call, so a converted
  component a `.render` still calls MUST keep `?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>` (ignore
  it). `Sidebar.Base` had dropped it → FS495 "no argument 'xLegacyStyles'". (recipe step 4 — easy to forget)
- **Public-type path moved →** the DSL emits the component's FQN-qualified type path, e.g.
  `LibClient.Components.Legacy.Card.Shadowed`. Nesting the types under `[<AutoOpen>] module …Legacy_Card` (so the
  full path became `…Legacy_Card.Legacy.Card.Shadowed`) breaks that. **Keep public DU/record types at the
  canonical module path** the DSL/old callers use: declare `namespace LibClient.Components.Legacy` + `module Card`
  (→ `LibClient.Components.Legacy.Card.Style`/cases), and put the `[<Component>]` extension in a separate
  `namespace LibClient.Components` block in the same file.
- **`Make` removed →** components built on `PureStatelessComponent` expose a module-level `Make` that hand-written
  app bootstraps call (`LibClient.Components.AppShell.TopLevelErrorMessage.Make {Props} [|children|]`). Converting
  to `[<Component>]` deletes `Make`. Either update the caller to the new ctor (did this for gallery `Bootstrap.fs`
  → `LC.AppShell.TopLevelErrorMessage(error=…, retry=…)`) or keep a `Make` compat shim. **`.render` callers
  (`<LC.X .../>`) are fine** — they compile to the new ctor; only hand-written `.Make` callers break. NOTE:
  Suite apps may also call these `.Make`s and aren't built here — converting an app-facing component is a
  cross-repo change.

**Validation rule going forward:** after converting any component that apps or `.render` files reference, do a
full `eggshell dev-web` of AppEggShellGallery, not just the LibClient build. Grep for callers first:
`grep -rnE "ComponentName\.Make|ComponentName\b" --include=*.fs --include=*.render` across apps + framework.

**dev-web operational gotchas (cost real time this session):**
- **JS deps live in the libs, not the app.** webpack `resolve.modules` (Meta/LibFablePlus/webpack.config.js)
  points at `LibClient/node_modules`, `LibRouter/node_modules`, etc. `react`/`react-router` are installed by each
  lib's `./initialize`, NOT the app's. Symptom: Fable compiles 0 errors but webpack throws hundreds of
  `Module not found: Can't resolve 'react'`. Fix: run the ROOT `./initialize` (installs every lib's
  node_modules), not just `AppEggShellGallery/initialize`.
- **`--run webpack-dev-server` only fires after the FIRST successful Fable compile.** If the first compile fails
  (e.g. an FS error in your new code), webpack never launches; watch-mode recompiles afterward do NOT start it.
  After fixing the error, RESTART dev-web from scratch.
- **Stale dev-servers survive `TaskStop`/`pkill` and hold the port.** A leftover `webpack-dev-server` keeps
  serving an OLD bundle on :8080/8082 while your new run dies with `EADDRINUSE`. You think it works but you're
  viewing the pre-fix build. Kill by port: `lsof -nP -iTCP:8082 -sTCP:LISTEN -t | xargs kill -9`, then start one
  clean instance. (webpack-dev-server auto-increments 8080→8081→8082 when a port is taken.)

## 2026-06-27 — Sonnet session: HAS_CONSUMERS cluster + theme + layout gotchas

Converted: `Legacy.Card` + `AppShell.TopLevelErrorMessage` + `Input.UnsignedInteger` + `Input.PositiveDecimal`.
All build green via `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"`.

**`Themes.GetMaybeUpdatedWith` — NOT `Themes.Get`:**
The correct API for reading the global theme in a `[<Component>]` is `Themes.GetMaybeUpdatedWith (optionalOverrideFn: Option<T -> T>)`.
`Themes.Get<T>()` does NOT exist (FS0039). The component signature takes `?theme: Theme -> Theme`; pass it directly:
`let theTheme = Themes.GetMaybeUpdatedWith theme`.

**HAS_CONSUMERS cluster — single-file conversion when only DefaultComponentsTheme.fs calls the legacy Theme:**
If `grep -rn "FooStyles.Theme" --include="*.fs"` across the whole repo returns ONLY `DefaultComponentsTheme.fs`
(no Suite app `.styles.fs`, no other component files), you can convert Foo in one step:
1. Define `type Theme = { ... }` in the new `Foo.fs`.
2. Replace `FooStyles.Theme.All(...)` in `DefaultComponentsTheme.fs` with `Themes.Set<Foo.Theme> { ... }`.
No external callers break. Legacy.Card and AppShell.TopLevelErrorMessage both fit this pattern.

**Public type preservation via nested modules in `[<AutoOpen>]`:**
To keep backward-compatible access for callers that `open LibClient.Components` and use `Legacy.Card.Style.Flat`:
define `module Legacy = module Card = type Style = ...` inside the `[<AutoOpen>] module LibClient.Components.Legacy_Card`.
The nested path resolves to `Legacy.Card.Style` exactly as before. Callers in SuiteProtocol / apps need no changes.

**`LC.With.Layout` — MUST pass `onLayoutOption` as `?onLayout` to the measured view:**
The callback signature is `(Option<ReactXP.Types.ViewOnLayoutEvent -> unit> * Option<Layout>) -> ReactElement`.
The first tuple element is the `onLayout` handler that captures the view's size. It MUST be attached:
`RX.View(?onLayout = onLayoutOption, ...)`. The old `.render` files used `_` for this argument, so `maybeLayout`
was always `None` and `minHeight` was never applied — that was a bug in the originals. Fix it on conversion.

**`makeTextStyles {}` accepts flex/spacing rules:**
`marginTop`, `marginBottom`, and other `RawReactXPFlexStyleRule` values are valid inside `makeTextStyles {}` CE
blocks (verified from `TextStylesBuilder`). No need to wrap in a View just to add spacing to a text element.

**`LC.TapCapture(onPress = f)` — modern press-handler pattern:**
Add as a CHILD of the View, not via `RX.View(onPress = ...)`. Pattern when `onPress` is optional:
```fsharp
children = [|
    yield! children
    match onPress with
    | Some f -> LC.TapCapture(onPress = f)
    | None   -> ()
|]
```

**`LC.Heading` / `LC.Icon` / `LC.Button` accept `styles` directly from modern parents:**
No `xLegacyStyles` bridge or `ReactXP.LegacyStyles.Runtime.prepareStyles...` call needed when the CALLER is
itself a modern `[<Component>]`. Just pass `styles = [| Styles.foo |]`.

**`Button.PropStateFactory.MakeLowLevel (Button.Actionable f)` — simple button state:**
To create actionable button state from a plain function `f: ReactEvent.Action -> unit`:
`Button.PropStateFactory.MakeLowLevel (Button.Actionable f)`.
Pass as `state = Button.PropStateFactory.MakeLowLevel (Button.Actionable myHandler)`.

---

## 2026-06-26 — FULL FABLE BUILD VALIDATION ✅ (+ toolchain gotchas)

Validated the 6 conversions end-to-end. **Result: all 6 converted components emit clean Fable JS**
(`/tmp/fable-libclient-validate/Components/{Tabs,VerticallyScrollable,Sidebar/Base,Section/Padded,
HandheldListItem,TriStateful/Abstract}/*.js`; 644 .js emitted total). No error references any converted
component or LibClient source.

How to run the validations (and gotchas):
1. **`eggshell build-lib` needs the Node toolchain bootstrapped.** Nothing was built initially (no
   `node_modules`/`dist` anywhere, render compiler binary absent). Run root `./initialize` (npm installs +
   builds LibEggshell/LibScaffolding/LibFablePlus/LibRtCompilerFileSystemBindings/AppEggshellCli + the
   render DSL compiler). Needs network (npm) — it's reachable here.
2. **The render-DSL compiler apphost needs `DOTNET_ROOT`.** dotnet lives at `/Volumes/HomeX/shafayat/.dotnet`
   (non-standard) and `DOTNET_ROOT` is unset, so the standalone `AppRenderDslCompiler` apphost reports
   ".NET location: Not found" even though `dotnet build` works and the .NET 7.0.20 runtime IS installed.
   Fix: prefix commands with `DOTNET_ROOT=/Volumes/HomeX/shafayat/.dotnet`. (Bash tool env doesn't persist
   across calls — set it inline each time.)
3. **`eggshell build-lib` for a LIBRARY does render-DSL + TypeExtensions generation, NOT Fable→JS.**
   It exits 0 and regenerates only changed TypeExtensions (here: `Simple.TypeExtensions.fs`); it leaves
   `ComponentRegistration.fs` consistent (no dangling refs to converted comps, no diff → my manual
   registration edits match the generator). Fable→JS only happens in APP builds.
4. **To Fable-validate a lib directly:** `DOTNET_ROOT=... dotnet fable LibClient/src/LibClient.fsproj
   --configuration "Web Debug" --noCache -o <dir>`. It compiles all 649 files ("Fable compilation
   finished") and EMITS JS for every component, then ERRORS on `Meta/FablePlugins/*` (tries to transpile
   the Fable plugin's own source — `System.Linq`, `Fable.AST.*` not available in JS). **That plugin error
   is expected/benign for validation** — the proper app pipeline builds `FablePlugins.dll` and loads it as
   a plugin instead of transpiling it. Confirm success by checking the `-o` dir has the components' `.js`.

## 2026-06-26 — Genuinely-stateful archetype (TriStateful/Abstract converted ✅)

Converted `Components/TriStateful/Abstract/Abstract.fs` (build green) — a real state machine
(`Mode = Initial | InProgress | Error`) driven by an async action. **Reference template: `QuadStateful.fs`.**

**Genuine state (real `useState`) recipe:**
- Class `Estate` + `GetInitialEstate initial` → `let hook = Hooks.useState initial` in the render body.
  Read with `hook.current`; write with `hook.update newValue`.
- Class `Actions` methods doing `this.SetEstate (fun estate _ -> { estate with X = v })` → plain local
  functions (closures over `hook`) calling `hook.update v`. They become the callbacks you hand to the
  render-prop / children.
- Async transitions keep the same shape (capture the hook in the closure):
  `hook.update InProgress; async { let! r = action in hook.update (...r...) } |> startSafely`.
- `open Fable.React` for `Hooks`; `startSafely` from `open LibClient`.
- **`ComponentWillReceiveProps` that SYNCS estate from a prop** (e.g. Form.Base/ChoiceList's
  `ManagedExternally`) → either recompute the value from props each render (if it's pure derivation) or
  `Hooks.useEffect (fun () -> hook.update fromProps) [| fromProps |]` to resync when the prop changes.
- This component was nested + exported a public `Mode` type referenced by a sibling (`TriStateful/Simple`).
  Per strategy A we updated the consumer: `Simple.typext.fs` now `open LibClient.Components` +
  `type Mode = LC.TriStateful.Abstract.Mode` (the new nested path). One-line dependency-aware edit.

## 2026-06-26 — More archetypes + gotchas (Section/Padded, HandheldListItem converted; Draggable/ChoiceList analyzed)

Converted (all build green): `Components/Section/Padded/Padded.fs` (responsive),
`Components/HandheldListItem/HandheldListItem.fs` (rich control flow). Total converted now: Tabs,
VerticallyScrollable+Sidebar.Base (cluster), Section/Padded, HandheldListItem.

**Archetype recipes discovered:**
- **Responsive** (was `class='{screenSize.Class}'` + per-screen style blocks): wrap body in
  `LC.With.ScreenSize (fun screenSize -> ...)` and branch INSIDE the style CE:
  `makeViewStyles { match screenSize with ScreenSize.Desktop -> padding 24 | ScreenSize.Handheld -> padding 16 }`
  (`open LibClient.Responsive` for `ScreenSize`). Validated: Section/Padded.
- **Pseudo-stateful → plain component (NO hooks):** many `EstatefulComponent`s have estate that is either
  `EmptyRecord` (HandheldListItem) or purely derived from props via `GetInitialEstate`/`ComponentWillReceiveProps`
  (Input/ChoiceList's `stateFromProps`). These need NO `useState` — compute the value as a local `let` in the
  render body. Only components with genuinely internal state (toggles, async, timers, mount/unmount effects)
  need `Hooks.useState` / `Hooks.useEffectDisposable` (reference: `QuadStateful.fs`).
- **`rt-match` → `match`, `rt-mapo` → `match opt with Some x -> ... | None -> noElement`**, nested freely.
  Build children as **arrays** `[| (match ...); (match ...) |]` (proven), NOT `elements { match ... }`
  (CE-with-match is unreliable). `castAsElement children` collapses the children array to one element
  (for a `Children` case). Validated: HandheldListItem.
- **Icons:** `LibClient.Icons.Icon` *inherits* `Fable.React.ReactElement`, but F# arrays are invariant, so
  upcast when putting in a children array: `[| (icon 32 :> ReactElement) |]`.
- **Text from non-strings:** `<div>{intValue}</div>` → wrap in `LC.Text (string intValue, styles = [| ... |])`
  (a View can't hold a raw int/string child; `LC.Text` takes a string + `?styles: array<TextStyles>`).
  Color that was on the legacy block belongs on the **text** style (`makeTextStyles { color ... }`), not the View.
- **`onPress`:** `RX.View`/`LC.Text` `onPress` is `PointerEvent -> unit`. A legacy
  `State.Actionable (Browser.Types.Event -> unit)` composes fine (`PointerEvent :> Event`):
  `?onPress = (match state with Actionable f -> Some (fun (e: PointerEvent) -> e.stopPropagation(); f e) | _ -> None)`.

**CRITICAL GOTCHA — don't leak union-case names via `[<AutoOpen>]`:** a converted component that defines
public union types (e.g. `State` with cases `Disabled`/`InProgress`/`Actionable`) MUST nest them under
`module LC = module ComponentName = type State = ...` (the `Tab.fs` pattern), then `open LC.ComponentName`
inside the file. If declared at module top level with `[<AutoOpen>]`, the bare cases leak globally and
collide with other components' same-named cases used unqualified elsewhere (e.g. DefaultComponentsTheme
uses `Disabled`/`InProgress` for ButtonLowLevelState / FloatingActionButtonStyles.State → FS0001). External
path becomes `LC.ComponentName.State` (matches modern convention; update any app callers using the old
`LibClient.Components.X.State` path).

**HARD CASES — flag for bespoke handling, do NOT rush:**
- **Genuinely-stateful w/ imperative refs / animation / `ComponentWillReceiveProps`** (e.g. `Draggable`):
  exposes a public `ref` interface, mutable closures, `AnimatedValue`s, gesture handling. This is a bespoke
  hooks rewrite (useRef/useEffect/imperative-handle), not recipe-mechanical. Convert individually with care
  or defer.
- **Nested-namespace component that exports public types AND is heavily called by apps** (e.g.
  `LC.Input.ChoiceList`): the module-rename needed for the `type LC.Input with` form changes the public
  type path (`Input.ChoiceList.Group` → `LC.Input.ChoiceList.Group`), breaking app callers. Plan the app
  updates as part of the cluster, or use the `module LC = module Input = module ChoiceList` nesting carefully.

## 2026-06-26 — Render-DSL → `[<Component>]` conversion RECIPE (validated on Tabs)

Decision: full modernization to the `[<Component>]` static-member form (not the drop-in convert).
Scope: all framework dirs + AppEggShellGallery. Opus does a few + writes this recipe; Sonnet continues.

**Per-component steps (keep build green at each component):**
1. Read the old trio: `Foo/Foo.typext.fs` (Props + class + Make), `Foo/Foo.styles.fs`, `Foo/Foo.render`
   (and the generated `_autogenerated_/.../Foo.Render.fs` to confirm exact semantics).
2. Find a modern sibling as a template (e.g. `Tab.fs`, `TwoWayScrollable.fs`, `Card.fs`). Match its
   shape exactly.
3. Write a single new `Foo/Foo.fs`:
   - `[<AutoOpen>] module LibClient.Components.Foo`
   - `open Fable.React`, `open LibClient`, `open ReactXP.Components`, `open ReactXP.Styles`.
     **Do NOT `open ReactXP.LegacyStyles`** — its rule fns (flex/Overflow/FlexDirection/backgroundColor)
     shadow the new-dialect ones and break `make*Styles` CEs. Qualify legacy refs fully instead.
   - Port `.styles.fs` class blocks → `module private Styles` with `makeViewStyles`/`makeScrollViewStyles`
     /`makeTextStyles`. One legacy class (e.g. `"view" => [...]`) → one style value/function.
   - `type LibClient.Components.Constructors.LC with` + `[<Component>] static member Foo(...) : ReactElement`.
   - Render body: translate the `.render` tree to `RX.*`/`LC.*` calls, `elements { }` for children lists,
     `element { }` for single, `match` for `rt-match`.
4. **Signature compat (critical for incremental migration):** generated callers still in `.render` emit
   `LC.Foo(?xLegacyStyles = ..., children = [|...|], ...)`. So keep the old TypeExtensions optional args:
   `children` (or `?children`), `?styles`, `?key` (ignore it), and `?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>`.
   Bridge xLegacyStyles into the root element's styles:
   ```fsharp
   let legacyStyles : array<XxxStyles> =
       match xLegacyStyles with
       | Some ls ->
           match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
           | []     -> [||]
           | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<XxxStyles> "ReactXP.Components.Xxx" styles |]
       | None -> [||]
   ```
   (Remove the bridge later once all callers are converted.)
5. **Theming:** legacy `XxxStyles.Theme.Customize`/`makeCustomize` → modern `Themes` pattern:
   - Define `type Theme = { ... }` at module level (full name `LibClient.Components.Foo.Theme`).
   - In the component: `let theTheme = Themes.GetMaybeUpdatedWith theme` (add `?theme: Theme -> Theme`).
   - **Migrate the default** in `LibClient/src/DefaultComponentsTheme.fs`: replace any
     `XxxStyles.Theme.Customize [...]` with `Themes.Set<LibClient.Components.Foo.Theme>({ ... })`.
   - `Themes.GetMaybeUpdatedWith` THROWS if no default was `Themes.Set` → always add the Set.
   - If the legacy `Theme.*` API has **zero callers** (grep both `Foo.Theme` AND `FooStyles.Theme`), drop it.
6. Delete `Foo.typext.fs`, `Foo.styles.fs`, `Foo.render`, and the whole `_autogenerated_/Components/Foo/`.
7. `ComponentRegistration.fs`: remove the `RegisterRender "...Foo"` and `RegisterStyles ("...Foo", ...)` lines.
8. `.fsproj`: replace the 4 `<Compile>` lines (typext, styles, TypeExtensions, Render) with one
   `<Compile Include="Components/Foo/Foo.fs" />`. Keep dependency order (a component must follow ones it
   uses, e.g. `Tabs.fs` after `Tab.fs`).
9. Build: `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"` (~13–35s). Must be 0 errors.

**Gotchas discovered (Tabs):**
- `open ReactXP.LegacyStyles` shadowing broke every `make*Styles` rule (FS0041 Yield overload). Don't open it.
- **`open Fable.React` is MANDATORY in every converted file.** `[<Component>]` resolves by looking for
  `ComponentAttribute` first; without `open Fable.React`, `Fable.React.ComponentAttribute` is not in scope and
  F# falls back to resolving `Component` as `LibClient.ChaldalReact.Component<_,_>` (AutoOpened via `open LibClient`),
  giving FS0033 "expects 2 type argument(s) but is given 0" + FS3242 "does not inherit Attribute".
- **Don't name local vars after `make*Styles` CE members** (e.g. a local `fontSize` or `color` shadows the
  CE function of the same name, causing FS0003 "not a function"). Use distinct names like `fontSz`, `col`.
- `DefaultComponentsTheme.fs` is a central file that references per-component `XxxStyles` modules — grep it
  for the component name before deleting `.styles.fs`, and migrate any default there.
- The new-dialect `make*Styles` CEs DO accept rule-arrays (e.g. `borderBottom w c` returns
  `array<RawReactXPViewStyleRule>`); yielding them is fine.
- `prepareStylesForPassingToReactXpComponent<'T>` is generic — annotate `<ScrollViewStyles>` etc. to get
  the right typed style back.
- A component's legacy `Theme.Customize` may LOOK unused via `Foo.Theme` grep but be used as
  `FooStyles.Theme.Customize` in DefaultComponentsTheme — grep BOTH.

**Converted so far:** `LibClient/src/Components/Tabs/Tabs.fs` ✅ (build green). Tabs was a *clean leaf*
(its only `TabsStyles` consumer was DefaultComponentsTheme, which we migrated).

### CRITICAL STRATEGY FINDING — legacy styles form a dependency graph (not leaf-isolated)

Tried VerticallyScrollable next and **reverted** it. Why: `Sidebar/Base/Base.styles.fs` styles
VerticallyScrollable's *internal* blocks via `VerticallyScrollableStyles.Theme.OneFixedTop/Bottom`
(`"view" ==> VerticallyScrollableStyles.Theme.One...`). The legacy model lets a PARENT style a CHILD's
named internal classes; the new model only injects top-level styles. So deleting `VerticallyScrollableStyles`
breaks Base, and faithful conversion needs either converting Base too (cascade) or adding per-section
style props (`?topStyles`/`?bottomStyles`) + updating Base.

Quantified: **~18 LibClient `*Styles.Theme` APIs are referenced from OTHER components'/apps' `.styles.fs`**
(Avatar, Badge, Button, DateSelector, Dialog.Shell.WhiteRounded.Raw, FloatingActionButton, Heading,
IconButton, IconWithBadge, Input.ParsedText, Input.Text, LabelledFormField, Legacy.TopNav.Base,
Nav.Bottom.Item, Nav.Top.Base, Nav.Top.Item, Nav.Top.ShowSidebarButton, VerticallyScrollable). Also some
are consumed by APPS (e.g. `LabelledFormFieldStyles.Theme.LabelWidth` in AppUserManagement).

**Implication:** the bulk conversion is NOT independent-per-component. Two viable strategies (pick before bulk):
- **(A) Dependency-aware clusters:** convert a producer + all its style-consumers together; producers expose
  modern `?theme` / per-section style props; update consumers to pass styles instead of legacy class cascade.
- **(B) Compat shim:** convert Foo to `[<Component>]` but KEEP a minimal `FooStyles` module exposing only the
  `Theme.*` members external consumers use (producing legacy sheets), so consumers compile unchanged; delete
  the shim once all consumers are converted. Decouples ordering at the cost of temporarily keeping legacy
  styling alive.
- **Clean leaves first (no external `*Styles.Theme` consumers) need neither** — start the bulk there (Tabs was one).

### More gotchas
- **`ComponentRegistration.fs` is AUTO-GENERATED** (header says DO NOT EDIT). It's regenerated by the
  registration generator on `eggshell build-lib` and naturally OMITS converted (pure-F#) components. Hand-editing
  it only keeps the `dotnet build`-only loop green; the real fix is regeneration. After a batch, run
  `eggshell build-lib` to regenerate cleanly (and to run the actual Fable build, the true validation).
- A component is NOT a safe pilot if `grep -rn "FooStyles" --include="*.styles.fs"` (repo-wide, incl. apps)
  shows consumers outside its own dir. Check this BEFORE converting.

### CLUSTER RECIPE (strategy A — chosen) — validated on VerticallyScrollable + Sidebar.Base ✅

When a component's `*Styles.Theme` API is consumed externally, convert the **producer + all its in-scope
consumers together** as one build-green change:
1. **Producer** exposes modern props that replace the legacy cascade:
   - global theming → `?theme: Theme -> Theme` + `Themes` (see Tabs);
   - parent-styles-child-internals → **per-section style params** (e.g. `?topStyles`/`?middleStyles`/
     `?bottomStyles: array<...Styles>`), merged into each section: `[| Styles.section; yield! (defaultArg sectionStyles [||]) |]`.
   - **Keep the existing public ctor arg names** (`?fixedTop`/`?scrollableMiddle`/... — many APPS call them);
     new params are additive/optional so app callers stay green.
2. **Consumer** drops its `.styles.fs` legacy cascade (`"view" ==> ProducerStyles.Theme.One*`) and instead
   passes the styles directly to the producer instance (`topStyles = [| Styles.x |]`, `styles = [| Styles.view; ... |]`).
3. Convert both with the standard per-component recipe (above); build green together.
4. **Out-of-scope app consumers:** if an app references the producer's legacy `*Styles.Theme`, that app's
   `.styles.fs` must get the same treatment (pass styles to the producer instead). Keep the producer's
   *constructor* signature stable so app call sites don't change.

**Nested-namespace components** (e.g. `LC.Sidebar.Base`, `LC.With.ScreenSize`, `LC.Nav.Top.Filler`):
- module named with underscores: `module LibClient.Components.Sidebar_Base` (NOT `Sidebar.Base`, avoids
  clashing with the `LC.Sidebar` type);
- declare via `type LibClient.Components.Constructors.LC.Sidebar with [<Component>] static member Base(...)`.

**Converted so far:**
- `LibClient/src/Components/Tabs/Tabs.fs` ✅ (clean leaf)
- `LibClient/src/Components/VerticallyScrollable/VerticallyScrollable.fs` +
  `LibClient/src/Components/Sidebar/Base/Base.fs` ✅ (cluster: producer + nested consumer)
All build green via `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"`.

**Deferred:** LabelledFormField (consumed by AppUserManagement via `LabelledFormFieldStyles.Theme.LabelWidth`
— a full cluster including that app's dialog styles; good next cluster for Sonnet).

## 2026-06-26 — Kickoff

- **`timeout` is not available on macOS** (no GNU coreutils). Don't wrap commands in `timeout`; rely on
  the Bash tool's own timeout / run long builds in background.
- **Fable docs repo:** `fable-compiler/fable.io` does NOT exist (clone 404s). Docs live in the
  `fable-compiler/Fable` repo under `docs/`; live site is https://fable.io/docs. For v4 docs, clone the
  Fable repo at a `4.x` tag. For now, fetch from fable.io on demand.
- **Framework libs build under `dotnet build` with a custom config.** Plain `Debug` doesn't exist; use
  `-c "Web Debug"`. (Baseline: `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"`.)

- **`eggshell convert-component` does not write files.** It runs the compiler's `RenderConvert` mode
  and only `console.log`s the result (`Meta/LibRtCompilerFileSystemBindings/src/convertComponent.ts`).
  To bulk-convert, we need tooling that writes the `.fs` in place, deletes the `.render`, and updates
  the `.fsproj`.
- **`RenderConvert` output is the readable form.** It emits idiomatic F# (`dom.*`, `RX.*`/`LC.*` calls,
  `elements { }`, `Styles.viewClass` refs), distinct from the ugly build-time `.Render.fs`
  (`__parentFQN`, `makeTextNode2`, `findApplicableStyles`). Use RenderConvert output as the conversion
  source of truth.
- **Framework `.render` count ≈ 118**: LibClient 77, ThirdParty 14, LibAutoUi 5, LibRouter 5,
  LibUiSubject 4, LibUiAdmin/LibUiSubjectAdmin/LibUiChaldal/LibUiChaldalAuth 3 each, LibLifeCycleUi 1.
  (Apps excluded: AppEggShellGallery 85, AppUserManagement 31, plus Suites.)
- **Full `[<Component>]` modernization is a styling-subsystem migration, not drop-in.** Pilot recon of
  Badge/Tabs/VerticallyScrollable/LabelledFormField: each `.render` component carries (1) a `.typext.fs`
  (Props+class+Make), (2) a `.styles.fs` with legacy class-based styles (`asBlocks`/`=>`) AND a public
  `Theme` class using `makeCustomize` for global theming + per-instance `Styles` sheets, (3) render+style
  registration, (4) render relying on `__componentStyles`/`xLegacyStyles` class-string merging. The modern
  `[<Component>]` model (Card.fs) uses `makeViewStyles` + explicit `styles=[||]` arrays + the `Themes`
  registry — a different styling system. So converting each component to `[<Component>]` also requires
  porting its styles + public `Theme.*` API, and preserving the call signature (incl. `?xLegacyStyles`,
  `?children`) so half-migrated trees (still-DSL callers emit `LC.Foo(...)` with legacy styles) keep
  compiling. Legacy styling is shared infra + public surface → big, interdependent migration.
- **zsh gotcha:** unquoted `--include=*.fs` in grep triggers zsh "no matches found" globbing; quote it
  (`--include="*.fs"`).
- **Two valid "pure F#" end-states**: (1) drop-in convert — keep `.typext.fs` + registration, replace
  generated render with hand-checked-in F#; (2) full modernization — single `.fs` with
  `[<Component>]` static member (no typext, no registration). (1) is the true drop-in.
