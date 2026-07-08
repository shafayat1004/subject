# Engineering Log

This is the running engineering log for the EggShell modernization effort (formerly LEARNINGS.md). Newest entries first; append new entries at the top with a date. For the distilled, timeless troubleshooting reference see [Troubleshooting](./runbooks/troubleshooting.md).

---

## 2026-07-08 (session 11 -- RW8 gallery on-device defects 1-3 fixed on POCO F1)

Fixed the first three RW8 defects on the physical POCO F1, iterating with a **debug** dev loop
(`eggshell dev-native` + Metro + `run-android`) instead of the release APK. All three are framework fixes
(benefit every app). Full status in [rn86-upgrade-status.md](./modernization/rn86-upgrade-status.md) (RW8).

### What shipped
- **Defect 1 — header behind status bar → new `Rn.SafeArea` seam.** The shell already asked for
  `Rn.View(useSafeInsets = true)`, but the RN/RNW View seam **silently ignored** `useSafeInsets`, and
  `react-native-safe-area-context` was never installed / no `SafeAreaProvider` mounted. Added
  `LibClient/src/Rn/Components/SafeArea/SafeArea.fs` (`SafeArea.useInsets` hook → native
  `useSafeAreaInsets`, zeros on web via `#if EGGSHELL_PLATFORM_IS_WEB`), mounted `SafeAreaProvider` at the
  native root in `RnPrimitives.setMainView` (guarded import), and made `LC.Nav.Top.Base` apply the top
  inset **on the header** (grow bar height by `insetTop` + `paddingTop insetTop`) so the coloured bar fills
  the status-bar strip with content padded below. Added `react-native-safe-area-context` 5.8.0 to gallery
  + AppTodo + scaffold template; dropped the ignored `useSafeInsets` arg from `AppShell.Content`.
- **Defect 2 — handheld nav icons not centred.** `Nav.Top.Item.renderIcon` applied
  `theme.IconVerticalAdjust` (=10px `bottom` nudge) on both screen sizes; it exists for the desktop
  label/tab-underline geometry. Now applied **desktop-only** (handheld → 0), centring the back/hamburger.
- **Defect 3 — sidebar scroll fired item taps.** Root-caused with on-device `console.log` probes in
  `LC.Pressable`: the caller's `OnPress` ran from RN's **`onPressOut`**, which RN fires even when an
  ancestor ScrollView cancels the press on a scroll, and RN reports `onPressOut` coords at the
  **press-DOWN** point (pressIn ≈ pressOut ⇒ movement guard sees ~0px ⇒ "tap"). RN's real **`onPress`**
  (correctly cancelled by a scroll) was a no-op. Fix: native drives the press from `onPress`; web keeps the
  `onPressOut` + movement-guard path; shared effects extracted to `firePress`. Verified: the flick that
  used to navigate now fires 0 accidental navs.

### Build-unblock
- **Stale `react-native-push-notification` receiver crashed the debug build at boot.**
  `AndroidManifest.xml` still declared `com.dieam.reactnativepushnotification.*` receivers/service (module
  dropped in RW1) → `ClassNotFoundException` `FATAL EXCEPTION: main` on a boot/package-replaced broadcast.
  Removed those manifest entries. Release didn't hit it (boot receiver fires only on specific broadcasts).

### Gotchas / lessons
- **`useSafeInsets` was a documented no-op.** A prop a seam accepts and `ignore`s reads as "supported" at
  the call site. When wiring device insets, verify the seam actually consumes the prop — grep the seam,
  don't trust the signature.
- **Native `onPressOut` is NOT a reliable tap signal.** RN calls it on scroll-cancelled presses and reports
  its coordinates at press-down, so a pressIn/pressOut movement guard cannot detect a scroll. Use RN's
  `onPress` for "a tap happened" on native; it is cancelled by an ancestor ScrollView.
- **Adding a native module needs the dep in every consumer.** A framework `import` of
  `react-native-safe-area-context` requires it in each app's `package.json` (gallery + AppTodo) **and** the
  scaffold template, or that app's native build breaks (cf. the Moti lesson, session 10).
- **Synthetic `adb input` taps/swipes are unreliable for gesture bugs.** They mis-fire the drawer pan and
  can't reproduce the intermittent scroll-tap cleanly. For gesture disambiguation, add JS `console.log`
  probes, let a human reproduce, and read logcat — far faster than guessing at swipe params.

### Also fixed
- **Open sidebar drawer top underlapped the status bar** (handheld drawer anchored at `top:0`).
  `AppShell.Content` `renderHandheldSidebar` now pads the drawer wrapper by `SafeArea.useInsets().Top`
  (white fill) so the first item clears the status bar. Verified on POCO F1.

### Follow-ups (open)
- Horizontal **drawer-close pan is over-sensitive** (near-vertical scroll with slight diagonal starts
  closing the drawer) — separate `Draggable`/`Scrim` `onPanHorizontal` gesture. Reported "better" after the
  defect-3 press fix, not explicitly addressed.
- **Markdown/docs pages still blank on native** (RW8 defect 5) — no native docs content source.

---

## 2026-07-08 (session 10 -- Reanimated overhaul: migrate framework animation off RN-Animated)

Executed the "full Reanimated overhaul" (RW2). Built a new **`Rn.Reanimated`** seam and migrated
**all real animated behaviour** in the framework + AppTodo off the RN-`Animated` seam.

### What shipped
- **New seam** `LibClient/src/Rn/Reanimated/Reanimated.fs`: `SharedValue` wrapper
  (`SetValue`/`AnimateTiming`/`AnimateSpring`, get/set `.Value`), `useSharedValue`, style helpers
  (`useAnimatedTranslateX`/`Y`/`XY`, `useAnimatedOpacity`) that embed the shared value via Reanimated
  **inline shared values** (NOT `useAnimatedStyle` worklets -- see the worklet finding below),
  `Rn.ReanimatedView` (merges normal `styles` with an `animatedStyle`), and `Reanimated.Easing`.
- **Migrated consumers:** `Scrim` (opacity fade), `SegmentedControl` (select-slide + follow-finger
  drag), `Carousel` (parallel two-slide), `Draggable` (two-axis gesture settle), **AppTodo**
  `TodoSwipeShell` swipe (`Todos.fs` + `TodoTheme.swipeContentBase`), and the gallery
  `HorizontalPanArea` demo page. The imperative pattern is uniform: `SetValue` during a gesture,
  token-guarded `AnimateTiming` to settle (assigning a shared value cancels a running animation; a
  monotonic token invalidates a superseded settle's JS-thread completion).
- **The RN-Animated primitives stay as legacy.** `Rn.Animatable{View,Text,Image,TextInput}` +
  `Rn.Styles.Animation` + the `animated*` style rules are **load-bearing for the legacy style runtime**
  (`Rn/Styles/Legacy/Designtime.fs` -> `createAnimatedViewStyle`, used by ~28 components), so they were
  NOT removed. They are now a documented legacy escape hatch; new code uses `Rn.Reanimated`.

### Gotchas (new)
- **Fable import arity.** Declare imported reanimated functions **with explicit parameters**
  (`let f x y = import ...`), never as a value of function type (`let f: a -> b = import ...`) -- the
  value form makes Fable emit `error FABLE: Change declaration of member`. dotnet type-check does NOT
  catch this; only a real Fable compile does.
- **Moti forces a per-app dependency; dropped it.** The first cut used `Rn.MotiView` for Scrim. On
  **native**, Metro resolves modules from each app's `node_modules`, and `moti` lived only in
  `LibClient/node_modules` -> runtime `Unable to resolve module moti`. Worse, the seam's module-level
  `import "MotiView" "moti"` requires it at load even when unused, forcing **every app + the scaffold**
  to add `moti`. Fix: **drop Moti entirely** -- Reanimated (already shipped everywhere) covers the one
  declarative case. Lesson: the seam must only depend on packages every consumer already has
  (reanimated + worklets), or the dep must be added to every app's `package.json` + the scaffold.
- **Stale-cache false green (again).** `eggshell build-native` prints `Skipped compilation because all
  generated files are up-to-date!` and skips Fable after edits. Force it: `touch` the changed `.fs` or
  `rm -rf .build/native/fable`, and confirm `Started Fable compilation...`.

### Two native-runtime bugs found and FIXED (the app now renders on-device)
Running AppTodo on the iPhone 16 simulator after a **clean** `rm -rf .build/native/fable` surfaced two
issues, both now fixed:

**(1) RW7 -- app root `GestureHandlerRootView` "Element type is invalid ... got a JSX literal".**
Redboxed at the app root. **Not RNGH** (a device probe confirmed `RNGH.GestureHandlerRootView` is a
plain function at load). Root cause was in the framework: `RnPrimitives.UserInterface.setMainView`
did `AppRegistry.registerComponent("RnApp", fun () -> rootComponent)` where `rootComponent` is itself
`fun _props -> wrappedElement`. **Fable uncurried the two nested lambdas into one**
`(unit, props) => wrappedElement`, so RN's `provider()` (called with no args) returned the
*element*, not the component -> React got an element as a type. A stale/incremental `.build` had a
non-uncurried version, which is why it only showed after a clean rebuild (and reproduced on the pre-RW2
baseline). **Fix:** box the component so the provider is `unit -> obj`, which Fable cannot uncurry
(`let componentProvider : unit -> obj = fun () -> box rootComponent`) -> emits
`registerComponent("RnApp", () => _props => wrappedElement)`. **Lesson:** when passing a
component *provider* / any `() -> function` across a JS interop boundary, box the inner function or
Fable may collapse the curry; verify the emitted arity.

**(2) Reanimated `useAnimatedStyle` worklets don't work from Fable.** With RW7 fixed, the next redbox
was *"[Worklets] Tried to synchronously call a Remote Function"* from `useAnimatedStyle.ts`. Fable-
emitted closures are **not** recognised by `react-native-worklets/plugin`, so the closure passed to
`useAnimatedStyle` ran as a JS "remote function" and calling `createObj` inside it threw on the UI
runtime. **Fix:** drop `useAnimatedStyle` entirely and use Reanimated **inline shared values** -- embed
the shared-value *object* directly in a plain style (`{ transform: [{ translateX: sharedValue }] }`);
the animated `View` drives the prop on the UI thread with no worklet. Worklet-free, identical on web +
native. This supersedes the earlier "author trivial worklets in F#" idea (the spike's probe C was
optimistic; on RN 0.86 + reanimated 4.5.1 + worklets 0.10.2 a Fable closure is not workletized).

### Verification (native runtime now GREEN)
- **Native:** AppTodo **renders on the iPhone 16 simulator** -- full UI, the migrated
  `SegmentedControl` (Light/Dark toggle) with its thumb bound via the inline shared value, no worklet
  or element-type errors.
- **Web:** gallery `eggshell package-web` green (Fable web + webpack, bundle emitted).
- **Compile:** dotnet type-check green; Fable native compile green (541 files).

### RW1 gallery Android build -- GREEN (the "~20-module workstream" was 2 dead-end drops)
The gallery links **11** native modules (`react-native.config.js`), not ~20. Only **two** had no
RN-0.86-compatible version, and dropping them was the whole fix:
- **`react-native-code-push`** -- App Center CodePush retired (2025); 9.0.1 breaks on 0.86
  (`ChoreographerCompat` removed). Dropped from `react-native.config.js` + the `codepush.gradle` apply.
  OTA replacements (confirmed via web search): **EAS Update / `expo-updates`**, **Revopush** (drop-in),
  self-hosted CodePush Server (archived, community), Hot Updater.
- **`react-native-push-notification`** -- unmaintained since 2022 (`jcenter()`/AGP 3.2.0). Dropped;
  **Notifee** + FCM is the replacement if needed later.

Every other linked module built on RN 0.86 **as-is** (maps 1.20.1, image-picker 7.2.3, device-info
14.1.1, firebase 21.14.0, fbsdk 13.4.3, svg/webview/netinfo/picker/async-storage, RNGH 3, reanimated 4)
-- no bumps required. Verified with a forced clean `./gradlew :app:assembleDebug --rerun-tasks` (329
tasks executed, `BUILD SUCCESSFUL`, fresh ~150 MB APK).
**Lesson:** don't assume "unmaintained ThirdParty = build blocker" -- most modules are recent enough for
0.86; triage by *actually building* and only act on the ones that fail.

**Gallery release APK now builds + RUNS on the POCO F1** (debug-signed for sideload). The gallery had
only ever run on web/debug, so its native *release* path had accumulated gaps -- four, each found by
building + installing + reading logcat (debug hides them: it loads JS from the metro dev server and
never bundles, and dev-mode has different globals):
1. **Release JS bundle can't resolve LibClient deps** -- `createBundleReleaseJsAndAssets` (metro
   `react-native bundle`) failed on `@react-native-picker/picker`, then `@react-native-async-storage/
   async-storage`. Map them in the gallery `metro.config.js` `extraNodeModules` -> LibClient (watchFolder).
2. **Worklets native module missing** -- `NativeWorklets ... 'loadUnpackers' of undefined`. reanimated +
   worklets lived only in LibClient/node_modules, so the gallery didn't autolink them. Add both as direct
   gallery deps (package.json) -> autolinked; also gives an in-tree `react-native-worklets/plugin` +
   `@babel/core` for a `babel.config.js` (resolving the plugin from LibClient failed:
   `[BABEL]: Cannot find module '@babel/core'` because LibClient has no @babel/core).
3. **Local images crash RCTImageView** -- `Value for uri cannot be cast from Double to String`.
   Framework bug: `Rn.Image` wrapped a native-imported asset (a numeric id) as `{uri:number}`. Fixed in
   `ImageSource.RawNativeAsset` + `Rn.Image` (pass the bare asset for native). AppTodo uses URL images so
   never hit it.
4. **No global `crypto`** -- Home route looped `ReferenceError: Property 'crypto' doesn't exist` (green
   flashing). Add `react-native-get-random-values` + `import` it first in `index.js` (AppTodo already did).

**Lesson:** a web/debug-only app carries hidden native-release gaps; the debug APK loading JS from metro
masks all bundler-resolution + missing-polyfill issues. Verify natively with a **release** build early.
RW7's fix (above) was the prerequisite that let any of this render at all.

**RW8 — gallery on-device defects (observed on the POCO F1 release build, to fix):** the app runs and
the home screen renders, but with these bugs (documented, not yet fixed):
1. Header underlaps the phone status bar — needs a top safe-area inset on the AppShell top bar
   (`react-native-safe-area-context`); a framework `AppShell` fix.
2. Sidebar toggle chevron (`>`) not vertically centered.
3. Scrolling the sidebar fires accidental taps that open unintended blades — needs a scroll/tap guard on
   sidebar items.
4. `HorizontalPanArea` "Drag me" slider doesn't drag on device — investigate the RNGH-3 imperative
   `PanGestureHandler` path + inline-shared-value update under New Arch (framework gesture primitive).
5. All markdown/docs pages render blank — on native there's no docs server (web serves `public-dev/docs`),
   so the `.md` content isn't bundled/fetchable; needs a native content-source strategy + confirm the
   `react-native-render-html` renderer.
6. Opening an input Picker green-flashes + jumps to top (no options shown) — same ErrorBoundary-remount
   signature as the `crypto` loop; picker-open throws a JS exception. Capture it with logcat first, then
   fix (likely the `@react-native-picker/picker` modal path / another missing global).
Details + leads in [RN 0.86 status](./modernization/rn86-upgrade-status.md) RW1 "Known on-device defects".

---

## 2026-07-08 (session 9 -- modernization tail: cleanup, gallery page, scaffold, docs, audit tooling; R2/R4 scoped)

Follow-on to session 8. iOS was completed for AppTodo (verified on the iPhone 16 simulator, pod
install only -- RN 0.86 keeps `RCTAppDelegate`, iOS min 15.1; gotcha: the ~94 MB prebuilt React-Core
maven download can `curl (56)` and needs a retry, or `RCT_USE_PREBUILT_RNCORE=0`). Then the remaining
polish items:

- **RXNavigator deleted** -- dead LibRouter component importing the uninstalled `reactxp-navigation`.
- **Gallery `HorizontalPanArea` page** added (rule 10), pure F# on the `Rn` API.
- **Scaffold template -> RN 0.86** (`Meta/LibScaffolding/templates/app`): package.json, babel worklets
  plugin, android gradle/SDK/NDK/Kotlin, MainApplication ReactHost model. Full `create-app` verify is
  gated on Goal B (create-app is separately broken).
- **Docs sweep** -- current-state reference docs to `Rn` / RN 0.86; migration-execution pins
  refreshed; audit tooling reworked.
- **Audit tooling (RNW)** -- ReactXP rendered `<Text>` with `data-text-as-pseudo-element="<text>"`;
  **RNW renders real DOM text nodes** (`div[dir="auto"]`) and maps `testID` -> **`data-testid`** (not
  `data-test-id`). Converted the gallery audits to `getByText` + `data-testid` (verified against the
  live RNW gallery: `data-testid` resolves, clicking "Docs" by text navigates).

### R2 (gallery native) and R4 (Reanimated in components) -- scoped as larger workstreams
- **R2:** gallery `assembleDebug` fails fast at `react-native-push-notification` (`jcenter()` removed
  in Gradle 9); v8.1.1 is the unmaintained latest and needs **patch/replacement**, not a bump. The
  gallery autolinks **~20 ThirdParty native modules**, several unmaintained -- a multi-day
  ThirdParty-modernization workstream. Gallery **web** works. Do NOT expect a quick "bump + build".
- **R4:** `SegmentedControl` **already** slides via RN-Animated; adopting Reanimated means rewriting a
  shared cross-platform control + new Moti bindings + per-platform verification. Defect 5 was on RN
  0.76; re-test on 0.86 first and prefer a small remount/init fix over a risky Reanimated rewrite.

---

## 2026-07-07 (session 8 -- de-ReactXP rename + upgrade to React 19 / RN 0.86 / New Architecture)

Two-track effort on branch `modernization/rnw` (plan: clean the framework of the ReactXP name +
seam, then upgrade to the latest stack and enable Fabric). Phases 1-3 done and verified; Reanimated
(Phase 4) and full docs/scaffolding/gallery-and-PP replication (Phase 5) remain.

### Track A -- de-ReactXP rename (the "seam" is now just `Rn`)
`@chaldal/reactxp` was already gone as a dependency; this stripped the *name*. A scripted codemod
renamed, across ~404 files: namespace `ReactXP` -> **`Rn`**, constructor prefix `RX.` -> **`Rn.`**
(715 sites), dir `LibClient/src/ReactXP/` -> **`LibClient/src/Rn/`**, files `ReactXPBindings.fs` ->
`RnBindings.fs`, `RNSeam.fs` -> **`RnPrimitives.fs`**, `ReactXPHttp.fs` -> `RnHttp.fs`, native
registration `"RXApp"` -> **`"RnApp"`** (F# + every `MainActivity.kt`/`AppDelegate.mm`), 26
`eggshell.json` render configs, 16 scaffold templates, and the lowercase `ReactXp`/`reactXp`
identifier variants. Deleted dead `@chaldal/reactxp*` metro aliases, the dead
`reactxpTextInputPatch.js` + vendored `reactxp-native-common/TextInput.tsx`, and dead
`reactxp-netinfo` `react-native.config.js` blocks + `_autogenerated_/ReactXP/` output.
- **`namespace Rn` + `type Rn` do NOT collide** (verified by a throwaway compile: `Rn.View`
  resolves to the constructor's static member, `Rn.Types` to the namespace).
- **Gotcha -- the render-DSL compiler must be rebuilt.** The compiler (`Meta/AppRenderDslCompiler`,
  a prebuilt `net7.0` dll invoked during builds) *regenerates* `_autogenerated_/*.TypeExtensions.fs`
  from `.render` sources. Its F# source was renamed to emit `Rn`, but until the dll was rebuilt
  (`dotnet build ...AppRenderDslCompiler.fsproj -c Release`) every build **regenerated the
  autogenerated files back to `ReactXP`** (git showed them `MM`). `dotnet build` masks this (it runs
  codegen); the Fable web build is what surfaced `The namespace or module 'ReactXP' is not defined`.
- Verified: LibClient / LibRouter / LibUiAdmin / LibAutoUi / LibLifeCycleUi / gallery all
  type-check 0 errors; framework source is 100% ReactXP-free (remaining refs are docs + the
  `audit-gallery-*.mjs` tooling, which assumes ReactXP web DOM -- see below).

### Track B -- React 19 / RN 0.86 / New Architecture
- **React 18.3.1 -> 19.2.3, react-native-web 0.19.13 -> 0.21.2** (LibClient hub; apps `file:`-link
  React). Only hard React-19 code break in our code: `findDOMNode` (removed) at `RnPrimitives.fs`
  Popup anchor -> replaced with a `getBoundingClientRect`/zero-rect fallback. Gallery web bundle
  compiles + serves + renders under React 19 (headless smoke). RNW 0.20 was the breaking release
  (removed `findNodeHandle`/`findDOMNode`/`hydrate`/`render`).
- **react-native 0.76.5 -> 0.86.0, New Architecture (Fabric/TurboModules/Bridgeless) ON.** AppTodo
  done as the reference app; **renders on both platforms** -- the POCO F1 (Android, debug + standalone
  release) with `Running "RnApp" with {"fabric":true}` and swipe-to-delete working, and the **iPhone 16
  simulator** (iOS) in dark mode under Fabric. iOS needed **only `pod install`** (no AppDelegate/Podfile
  changes: RN 0.86 keeps `RCTAppDelegate`, iOS min 15.1); `react_native_post_install` set
  `RCTNewArchEnabled=true` + `-DRCT_REMOVE_LEGACY_ARCH=1`. **Gotcha:** RN 0.86's prebuilt React-Core is
  a ~94 MB maven download that can fail (`curl 56`) -- retry, or `RCT_USE_PREBUILT_RNCORE=0` to build
  from source. Native config from the `react-native-community/template` `0.86.0`
  tag: Gradle **9.3.1**, Kotlin **2.1.20**, SDK **36**, NDK **27.1.12297006**, `newArchEnabled=true`;
  `MainApplication.kt` rewritten to the `ReactHost`/`loadReactNative` model (no more `SoLoader.init`
  / `ReactNativeHost`). See [Android runbook](./runbooks/android.md) and
  [Troubleshooting](./runbooks/troubleshooting.md) for the full recipe + gotchas.
- **Native modules must be bumped for RN 0.86 Fabric C++** (old ones fail codegen with
  `BaseShadowNode`/`SharedImageManager` undeclared): react-native-svg **15.15.5**, webview
  **14.0.1**, netinfo **12.0.1**, picker **2.11.4**, async-storage **3.1.1**.
- **RNGH 2.31 is JS-incompatible with RN 0.86** -- it imports the removed
  `react-native/Libraries/Renderer/shims/ReactNative` (Metro 500 / DebugServerException). Moved to
  **RNGH 3.0.2**, which still exports the imperative `PanGestureHandler`/`GestureHandlerRootView`/
  `State`, so `HorizontalPanArea.fs` needed no rewrite. RNGH 3 and async-storage 3 ship **only
  `lib/module/`** (ESM) -> the web webpack aliases in `Meta/LibFablePlus/webpack.config.js` were
  changed from `lib/commonjs/index.js` to `lib/module/index.js`. `npm install` needs
  `--legacy-peer-deps`.

### Repo hygiene
- `.fs.js` (Fable output) are **not committed** -- `.gitignore` already declared `**/**/*.fs.js`, but
  54 legacy `_autogenerated_/*.fs.js` were still tracked; `git rm --cached`-ed them. Added `**/.cxx/`
  (Android CMake output) to `.gitignore`.

### Known follow-ups
- Gallery + PerformancePlayground still on RN 0.76 native config (same recipe as AppTodo).
- `audit-gallery-*.mjs` assume ReactXP web DOM (`data-text-as-pseudo-element`, `data-test-id`); RNW
  emits real text nodes + `data-testid` -- the audit tooling needs a rework, not a rename.
- `LibRouter/.../RXNavigator` still imports the uninstalled `reactxp-navigation` (pre-existing dead
  component; rename/removal deferred).
- Reanimated 4 + Moti not yet added (New Arch now unlocks Reanimated 4).

---

## 2026-07-07 (session 7 -- native swipe-to-delete rebuilt on react-native-gesture-handler)

Fixes session-6 field defects **1-4** (swipe janky/stops mid-drag, Y-drift leaks to edit, list scrolls during swipe, delete doesn't fling). Verified on the POCO F1 (release build).

### Root cause: a JS-responder `GestureView` cannot beat a native `ScrollView` on Android
Instrumented `GestureView`'s responder lifecycle on the device. During a swipe the log shows a rapid `grant -> TERMINATE (stolen) -> grant -> ...` ping-pong, and critically **`TERMINATE (stolen) isActive true`** -- the native Android `ScrollView` terminates the child's JS responder **even when `onResponderTerminationRequest` returns `false`**. So the custom `GestureView` (JS responder) cannot win against the page `ScrollView` on native: the ping-pong is the jank, the steal routes the finger-lift to the row's edit press, and the list keeps scrolling. Web was never affected -- there the `ScrollView` is a `div` and `touch-action` CSS already yields the pan, so **web keeps the `GestureView` path**. An earlier attempt to fix this inside `GestureView` (`onResponderTerminationRequest = not isActive`) was **reverted** -- the evidence proved Android ignores it.

### Fix: native swipe via react-native-gesture-handler (RNGH)
- New framework primitive **`RX.HorizontalPanArea`** (`LibClient/src/ReactXP/Components/HorizontalPanArea/HorizontalPanArea.fs`). Native uses RNGH `PanGestureHandler` with `activeOffsetX`/`failOffsetY`, so horizontal-vs-vertical arbitration happens in the **native** gesture system (scroll and swipe stop fighting). Web keeps the `GestureView` responder path. Unified callback API: `onStart` / `onUpdate translationX` / `onEnd translationX`.
- **`RX.GestureHandlerRootView`** (native only) wired at the app root in `Bootstrap.fs` (`setContextWrapper`, `#if` native); `import 'react-native-gesture-handler'` is now the **first line** of `index.js` (required on Android).
- `TodoSwipeShell` rewired onto `HorizontalPanArea`, keeping its `translateX`/settle logic. **Bug 4** fixed: `settleFromOffset` now `animateTo -rowWidth (Some onDelete)` (fling then delete) instead of deleting at the drag position.
- **Edit-trip** mitigation: `SwipeTapGuard` (a module-level time window) suppresses the row's checkbox/edit taps during and ~300 ms after a swipe. An earlier `blockPointerEvents = isDragging` attempt was **reverted** -- toggling `pointerEvents` mid-touch cancels the live gesture and killed the swipe.

### Gotchas (now distilled into troubleshooting)
- **RNGH version vs RN 0.76.** RNGH 2.32 fails to compile against RN 0.76.5 (`Cannot access 'ViewManagerWithGeneratedInterface'`). Pinned **`react-native-gesture-handler@2.21.2`**.
- **"Cannot define property" crash calling into RNGH (dev only).** A Fable `float[]`/`int[]` compiles to a **typed array** (`Float64Array`), whose indices are non-configurable. RN dev deep-freezes native-call arguments (`deepFreezeAndThrowOnMutationInDev`), and RNGH forwards `activeOffsetX`/`failOffsetY` straight into the native config, so freezing the typed array throws. Pass a **plain JS array** (`[<Emit("[$0, $1]")>]`). Diagnosed with a temporary error boundary logging `error.stack` -- React itself only logs the truncated message + component stack.
- **Gesture element must sit INSIDE the animated/translating surface.** Wrapping the animated surface *inside* the gesture wrapper left the static gesture view full-width, covering the delete slot -> tap-to-delete stopped working. Correct layout: `AnimatableView(translateX) > HorizontalPanArea > rowContent`.
- **RNGH child must be ref-forwarding.** `PanGestureHandler` attaches a `ref` + `collapsable` to its single child; a `wrapComponent` function component can't receive them, so the child is wrapped in a raw RN `View`.

### Perf note
The **dev** build feels laggy: Metro lazy bundling (first interactions stream modules, e.g. dark-mode toggle delayed then smooths out), per-frame `deepFreezeAndThrowOnMutationInDev` on the JS-driven `translateX.SetValue`, and unminified JS. A **release** build (single-ABI, debug-signed) is dramatically smoother and is the correct way to judge feel -- see [Android runbook: release perf build](./runbooks/android.md#release-perf-build). If native-thread animation is ever required, `react-native-reanimated` is the next step (not adopted now).

### Field-defect status (session-6 list)
- **1-4 fixed** (RNGH native arbitration + fling).
- **5** (dark-mode toggle jumps on tap) and **6** (TalkBack can't focus Priority picker) **still open**.
- **New open issue:** the native **text-selection cursor sometimes appears while swiping** over a card. Likely fix: `userSelect: none` / non-selectable row text, or suppress selection during the pan. Tracked in the accessibility backlog.
- **Follow-up (rule 10):** `HorizontalPanArea` has no gallery page yet (native-only gesture; hard to demo on web). Add a swipeable-row gallery example when practical.

---

## 2026-07-02 (session 6 -- standalone install on a physical device (POCO F1) + field findings)

### Installing a standalone build on a physical Android device
- **Wireless adb pairing.** `adb pair 192.168.2.223:<PAIRING_PORT> <6-digit-code>` using the values from the phone's **"Pair device with pairing code"** dialog, then `adb connect 192.168.2.223:<CONNECT_PORT>` using the port on the **main "Wireless debugging"** screen. The pairing port and the connect port are **different**, and both (plus the code) rotate every time the dialog reopens and time out quickly, so run `adb pair` where you can read both at once (or have the user run it). `ping` first to confirm the phone is reachable; a bare `adb connect` that "fails to connect" on a reachable host usually means not-yet-paired or wrong (pairing vs connect) port.
- **Signing.** `android/app/build.gradle`'s `release` signingConfig only populates when `MYAPP_RELEASE_STORE_FILE` etc. are set, so a plain `assembleRelease` yields an **unsigned, uninstallable** APK. For a throwaway build, sign with the debug keystore via gradle props:
  ```
  cd android && ./gradlew assembleRelease \
    -PMYAPP_RELEASE_STORE_FILE=debug.keystore -PMYAPP_RELEASE_STORE_PASSWORD=android \
    -PMYAPP_RELEASE_KEY_ALIAS=androiddebugkey -PMYAPP_RELEASE_KEY_PASSWORD=android
  ```
- **Fake data survives the release bundle.** `assembleRelease` bundles whatever JS is already in `.build/native/commonjs` (the Fable watch output, compiled with `--define DEBUG`), so `FakeTodoService` (`#if DEBUG`) stays compiled in and the standalone app has in-memory todos with no backend. `configSourceOverrides.native.js` sets `AppUrlBase = http://10.0.2.2:8081` (emulator-only, unreachable on a real phone), but the todo UI is all in-JS + in-memory so it runs fine; only network-backed resources would be missing.
- **Install + launch:** `adb -s 192.168.2.223:<PORT> install -r app/build/outputs/apk/release/app-release.apk`, then `am start -n com.eggshell.apptodo/.MainActivity`. `versionCode` is still `1`; keep signing with the same key for in-place updates. Verified: launches standalone, fake todos load, UI intact.

### Open issues found on real hardware (POCO F1) -- NOT yet fixed
Reported from real-touch testing (reduce-motion confirmed OFF on the device):
1. **Swipe-to-delete is janky and stops mid-transition** -- the card does not track the finger smoothly. Likely the per-move `isDraggingHook.update true` and the reactive re-renders fight the `AnimatedValue.SetValue`; investigate driving `translateX` purely through the animated value with no React state churn during the drag.
2. **A slight downward drift during a horizontal swipe starts the edit flow (title text box)** -- vertical deviation is not absorbed by the horizontal GestureView; the touch leaks to the row's edit affordance. The horizontal pan should own the gesture once engaged and not hand off on minor Y movement.
3. **The list still scrolls vertically during a horizontal swipe** -- `onResponderTerminationRequest = true` lets the parent `ScrollView` steal the gesture. Lock vertical scroll while a horizontal swipe is active (refuse termination and/or disable ScrollView scroll during the drag).
4. **Delete does not fling the card off-screen** -- it stops at the edge, then the row disappears. `settleFromOffset` calls `onDelete()` immediately at threshold instead of animating `translateX` to `-rowWidth` first. Animate-out, then delete, for a smooth finish.
5. **Dark-mode toggle no longer slides the thumb on tap (it jumps)** -- reduce-motion is OFF, so this is not the reduced-motion path. Suspect the tap path selects without running `animateTo`, or `LC.With.ReducedMotion` mis-reports `true` on this device. Verify `AccessibilityInfo.isReduceMotionEnabled()` on the POCO F1 and that `selectIndex -> animateTo` runs on tap (regression check against the GestureView tap fix from session 5).
6. **TalkBack cannot focus the Priority picker to change selection** -- the picker field is not exposing focusable/actionable semantics to the screen reader. `[safe]` a11y defect; tracked in the accessibility backlog.

## 2026-07-02 (session 5 -- toggle/swipe/flash root causes on Android)

Session 4's GestureView `isActive` direction fix was a partial symptom fix. Real root causes found by turning the emulator's animations back on and reading the actual crash + logcat:

- **Emulator had animations disabled (`animator_duration_scale = 0`) -> RN reports reduce-motion ON -> SegmentedControl and swipe row render their non-interactive fallbacks.** `SegmentedControl.fs` renders a plain `RX.View` (no `onTap`/`onPan`) when `reduceMotion`; `TodoSwipeShell` renders a static row. So *nothing* responded until animations were re-enabled (`adb shell settings put global {window,transition,animator}_*_scale 1`). This is also a real a11y defect: **reduce-motion must disable animation, not interactivity** (violates "gesture needs a non-gesture alternative"). Still open -- flagged.

- **RN pools synthetic events and nulls `nativeEvent` after a handler returns.** `onResponderRelease` handed an event whose `nativeEvent` was already null, so building the pan state by reading `nativeEvent.pageX` threw `Cannot read property 'pageX' of null`, crashing every gesture end. Fix: read coordinate primitives synchronously during grant/move into a ref (`lastCoordsRef`); never retain the event object; release/terminate use the captured coords. In `GestureView.fs`.

- **`nativeEvent.locationX` is relative to the deepest touched child, not the GestureView.** SegmentedControl computed the tapped segment as `int(clientX / cellWidth)` using `locationX`, which is 0..cellWidth within whichever cell was hit -> always index 0. Symptom: tapping Dark from Light did nothing (0 == selected); tapping Light from Dark worked (0 != selected). Fix: GestureView measures its own page origin via `measureInWindow` (cached on layout) and reports a view-relative `clientX = pageX - originX` for taps. `SegmentedControl` is the only `onTap`/`clientX` consumer, so blast radius is nil.

- **`measureInWindow` flood -> 501 pending bridge callbacks -> starved touch delivery.** Measuring every GestureView on every layout (each todo row has one) leaked hundreds of unresolved `measureInWindow` callbacks (visible as `Excessive number of pending callbacks`). Fix: gate measurement to `props.OnTap.IsSome` (only SegmentedControl); swipe rows never measure.

- **Whole page remounts on every executor action -> loader + scroll-to-top (the real cause).** First blamed `listVersion` in the `Subjects` React key; removing it changed nothing. Ground truth via a `SUBJECTS MOUNT` log: the list mounted *twice* per action with a *stable* key -- so the key was never it. Cause: `LC.AppShell.Context` uses `showTopLevelSpinnerForKeys = All`, and `Executor.DisplayErrorsManually` returned a **bare `pageContent`** when idle but **`castAsElementAckingKeysWarning [| pageContent; spinnerOverlay |]`** while any executor key was `InProgress`. Every action toggles idle -> InProgress -> idle, so the returned tree flips bare <-> 2-element array; React sees structurally different children and **remounts the entire page each way** (the two mounts), losing scroll and re-subscribing all data (`Subjects` cycles Uninitialized(tag 0) -> Available(tag 4) = the loader). Fix: in `DisplayErrorsManually.fs` always return `castAsElementAckingKeysWarning [| pageContent; (if spinner then overlay) |]` so `pageContent` is the stable first child whether or not the spinner shows -> no remount. Verified: `SUBJECTS MOUNT` count 0 on delete, item removed in place, no scroll jump.
  - Related, in `Todos.fs`: the indexed `FakeSubjectService` subscription snapshots its matching ids at subscribe time, so **adds** (new id) need a re-subscribe to appear -- keep `listVersion` in `listKey` and bump it only on add. Row mutations (delete/toggle/edit/archive) touch existing ids and update reactively, so their `onMutated` is now a no-op (previously `bumpList`, which needlessly re-keyed). This split keeps adds working without remounting the list on every edit.

- **Deleted rows left an empty swipe-shell artifact -- positional keys on a stateful list.** The todo rows were emitted through `castAsElementAckingKeysWarning (todos |> List.map TodoRow)`, i.e. `React.Children.toArray` positional keys (`.$0`, `.$1`…). Each row owns swipe-animation state (`translateXRef`, open/drag). Deleting a middle row shifted the rest up by one position; React reused the same instances for different todos, so a row left mid-swipe kept its `translateX` and showed as an empty shell with the delete panel. Fix: give each `TodoRow` a stable `key = todo.Id.IdString`. Fable's `[<Component>]` emits both the real React `key` and a `$key` prop, and `React.Children.toArray` preserves existing keys, so reconciliation is now by id and the deleted row unmounts cleanly. **Lesson:** the "route sibling arrays through toArray" rule is for *static* sibling arrays; a dynamic list of stateful children still needs id keys.

- **A full-screen spinner flashed on every todo action -- `showTopLevelSpinnerForKeys = All`.** `AppShell.Context` hard-coded `All`, so any in-progress executor key (every add/toggle/delete/edit) showed the `WhiteAlpha 0.5` + `ActivityIndicator` overlay. Instrumenting the list confirmed its `AsyncData` stays `Available` (tag 4) through a delete -- so this overlay, not `whenFetching`, was the "loader" the user saw. Fix: added an optional `?showTopLevelSpinnerForKeys` to `LC.AppShell.Context` (default `All`, behavior-preserving) and passed `ShowTopLevelSpinnerForKeys.Some Set.empty` from AppTodo's `AppContext.fs` (empty key set never matches -> no spinner). Apps with slow actions can still opt into it.

- **Fable maps a `key` arg to BOTH the React key and a `$key` prop.** Verified in compiled output: `createElement(Comp, { …, key: id, $key: id })`. So `?key: string` + `key |> ignore` on a `[<Component>]` member is the way to set a stable React reconciliation key from F#. (The `$key` prop is just the readable copy; components normally ignore it.)

- **Startup Light->Dark flash.** Route defaulted `appearanceHook` to Light and loaded the saved mode async, painting Light then flipping. Fix: bundle-lifetime `AppearanceStorage.cached` initializes the hook synchronously on warm mounts; on a cold start (cache empty) hold a neutral dark surface (`appearanceResolvedHook`) until the async read returns instead of painting Light. In `Todos.fs`.

- **adb touch caveats (why device verification stalled).** `adb shell input tap` on a control inside the swipe GestureView is swallowed (the GestureView claims the responder on touch-start; real touches negotiate to the child, synthetic ones do not). `adb shell input text` sets the native input value but does not fire RN `onChangeText`, so React state stays empty. Use real touch or the Appium `observe` harness (see runbooks) for gesture/text interactions; raw adb is fine only for large simple taps (e.g. the theme toggle).

## 2026-07-02 (session 4 -- swipe/toggle bug fixes)

- **React key duplication in `TodoRow.todoBodyRow` -- `tellReactArrayKeysAreOkay` applied twice on appended arrays** -- `rowActionButtons` was already processed by `tellReactArrayKeysAreOkay` (producing keys `.$0`, `.$1`…), then `Array.append`-ed to another `tellReactArrayKeysAreOkay`-processed array, causing both sub-arrays to start at `.$0`. React warns and reconciles incorrectly. Fix: removed wrapper from `rowActionButtons` definition; wrap the entire `Array.append` result once at the use site.

- **GestureView `isActive` used OR of dx/dy for all preferred-pan directions -- blocked `onTap` on Android from vertical jitter** -- `onResponderMove` set `isActive = true` when `|dx| > threshold OR |dy| > threshold`, regardless of `preferredPan`. On Android, even a clean tap on a horizontal GestureView (SegmentedControl dark-mode toggle) produced > 8px vertical movement, setting `isActive = true` and suppressing `onTap` at release. Fix: made the movement check direction-aware: `Horizontal` → only `|dx| > threshold` gates `isActive`; `Vertical` → only `|dy|`; else either. Same `isSignificant` helper used for the `onResponderRelease` tap guard. Applied in `LibClient/src/ReactXP/Components/GestureView/GestureView.fs`.

---

## 2026-07-02 (session 3 -- Android native boot fixes)

- **`Bootstrap.fs` used `ReactXPRaw` dynamic dispatch -- replaced with F# API** -- `ReactXPRaw?App?initialize`, `ReactXPRaw?UserInterface?setContextWrapper`, `ReactXPRaw?UserInterface?setMainView` were direct JsInterop calls on the old `@chaldal/reactxp` package global. After RNW migration that global no longer exists. Replaced with the typed F# seam API: `ReactXP.App.initialize`, `ReactXP.UserInterface.setContextWrapper`, `ReactXP.UserInterface.setMainView`.

- **`RNSeam.UserInterface.setMainView` native path implemented** -- was `failwith "not implemented"`. Now calls `AppRegistry.registerComponent("RXApp", fun () -> rootComponent)` wrapping the passed element in a functional component and applying the context wrapper. `AppRegistry` added to the inner `RNSeam` bindings module.

- **`react-dom` imported unconditionally in `RNSeam.Popup` -- crashed native Metro** -- `findDOMNode` and `domRectOf` helpers inside `module Popup` had no `#if EGGSHELL_PLATFORM_IS_WEB` guard, causing Metro to fail resolving `react-dom` in the native bundle. Fixed: wrapped both helpers in `#if EGGSHELL_PLATFORM_IS_WEB`.

- **`@react-native-picker/picker` missing from AppTodo -- added to `package.json` and `metro.config.js`** -- `RNSeam` imports `Picker` from this package. It was listed in `LibClient/package.json` but not in AppTodo's. Added `"@react-native-picker/picker": "^2.11.0"` to AppTodo dependencies and added it to `extraNodeModules` in `metro.config.js`. Also added `buffer` to `extraNodeModules` (needed by `react-native-svg`).

- **`AccessibilityHelpers.mapRoleToString` -- `"banner"` and web-only ARIA roles crashed native Android** -- `Header` was mapped to `"banner"` (the ARIA landmark role, not valid as a native `accessibilityRole`). Fixed to `"header"` which is the correct RN native value (RNW maps it to `role="heading"` on web). Also guarded the following roles as `#if EGGSHELL_PLATFORM_IS_WEB` only (not valid on Android RN 0.76): `List`, `ListItem`, `ListBox`, `Group`, `Log`, `Status`, `Dialog`, `Option`, `Main`, `Navigation`, `Complementary`. On native these now return `None` (no role set), avoiding `Invalid accessibility role value` crashes.

- **`LegacyText.fs` passed raw int enum as `accessibilityRole` -- fixed** -- `accessibilityRole |> Option.iter (fun v -> __props?accessibilityRole <- v)` passed the F# integer enum value directly. Android bridge expects a string; this threw `java.lang.Double cannot be cast to java.lang.String`. Fixed to pipe through `ReactXP.RNSeam.mapAccessibilityRole` before setting.

- **`RNSeam.createTextStyle` converted `lineHeight` to `"Npx"` string unconditionally -- crashed Android** -- The `[<Emit>]` JS that normalized text styles added `'px'` suffix to numeric `lineHeight` values for web CSS compatibility. On Android, `lineHeight` must be a number; the string caused `java.lang.String cannot be cast to java.lang.Double` on `RCTText` creation. Fixed: split into `#if EGGSHELL_PLATFORM_IS_WEB` (keeps px conversion) and native (flex-only normalization, no lineHeight mutation).

## 2026-07-02 (session 2 -- RNW a11y completion pass)

- **Landmark roles added to `Accessibility.fs` and `AccessibilityHelpers.fs`** -- `Main = 38`, `Navigation = 39`, `Complementary = 40` added to the `AccessibilityRole` enum; `mapRoleToString` maps them to `"main"`, `"navigation"`, `"complementary"`. RNW renders `<main>` / `<nav>` / `<aside>` on web; native renders native landmark equivalents.

- **`LC.AppShell.Content` now emits semantic landmark structure automatically** -- `renderContentBlock` gains `accessibilityRole = AccessibilityRole.Main`; `topNav` and `bottomNav` optional blocks gain `accessibilityRole = Navigation` with labels. Skip link (`LC.SkipLink`) is inserted as the first child of `renderShell` so keyboard users can bypass nav before every route change. FocusVisibleStyles CSS is injected on first mount via `Hooks.useEffect`.

- **`LC.SkipLink` component** -- web-only: renders `<a href="#eggshell-app-content" class="eggshell-skip-link">`. Positioned off-screen when not focused; moves on-screen on `:focus-visible`. Native: `noElement`. Used automatically by `LC.AppShell.Content`.

- **`:focus-visible` ring CSS injected by `FocusVisibleStyles.injectIfNeeded`** -- `LibClient/src/FocusVisibleStyles.fs` injects a `<style id="eggshell-a11y-focus-styles">` tag at first AppShell mount. Adds a 2px blue ring (`rgba(0,95,204,0.8)`) on `:focus-visible` for all interactive ARIA roles. Suppresses the ring on mouse/touch focus via `:focus:not(:focus-visible)`. Also includes skip-link CSS.

- **`LC.FocusManager` module** -- `LC.FocusManager.setFocusTo el` (web: `.focus()`; native: `setAccessibilityFocus`) and `LC.FocusManager.setFocusById id` (web-only). Use for programmatic focus on dialog open/close and route change.

- **`HandheldListItem` outer view gets `role=ListItem`** -- the container RX.View now has `accessibilityRole = AccessibilityRole.ListItem`, giving list-navigation context to both actionable (Button overlay inside) and non-actionable (Disabled/InProgress) item states.

- **`React.createElement` with string children in Fable** -- `React.createElement("a", props, labelText)` fails with `FS0001` (string not compatible with ReactElement seq). Correct form: `React.createElement("a", props, [| !!labelText |])`. Distilled to troubleshooting.

## 2026-07-02 (session 1)

- **Web `aria-live` region was completely silent; fixed in `AccessibilityAnnounce.fs`** -- `AccessibilityInfo.announceForAccessibility` is a native-only RN API; on web it throws silently inside the `try/with` wrapper, so every `LC.LiveRegion.announce` call was a no-op for VoiceOver/NVDA on web. Fix: the web branch of `announce` now lazily creates a hidden `<div aria-live="polite|assertive">` (positioned off-screen at `left:-9999px`, `1x1px`) and updates its `textContent`; it clears first then sets after 50ms so screen readers see a fresh DOM mutation even when the same string fires twice. Native branch (`announceForAccessibility`) unchanged. See also: `#if EGGSHELL_PLATFORM_IS_WEB` block in `LibClient/src/AccessibilityAnnounce.fs`. Distilled to troubleshooting.

- **Swipe-delete button announced without item context** -- both swipe-delete `LC.TextButton` calls in `AppTodo/Todos.fs` used the bare `i18n.t.SwipeDeleteLabel` (`"Delete"`), giving VoiceOver no information about which item. Fixed to `todoActionLabel todo i18n.t.DeleteActionFormat` (`"Delete {title}"`), matching the existing non-swipe delete path.

- **`LC.With.Accessibility` web detection matrix (iOS Safari vs Android Firefox)** -- Real-device testing revealed browser support gaps for CSS MQ Level 5 accessibility queries. `(inverted-colors: inverted)` is Safari-only (iOS 14+/macOS); Chrome and Firefox ship no support, so `InvertColors` is permanently `[web-blocked]` on Android. The prior code used `(forced-colors: active)` (Windows High Contrast only) -- fixed to `(inverted-colors: inverted)`. `BoldText` maps to `(prefers-contrast: more)`; on Android this corresponds to "High Contrast Text" (no distinct "Bold Text" toggle exists on Android). Three flags remain permanently web-blocked across all browsers: `ScreenReaderEnabled` (no standard API; intentionally undetectable for privacy), `Grayscale` (no media query), `FontScale` (iOS "Text Size" scales native UIFont only, not web content; Android font scale similarly unavailable). Full matrix:

  | Flag | iOS Safari | Android Firefox | Native (both) |
  |---|---|---|---|
  | ReduceMotion | ✅ | ✅ | ✅ |
  | BoldText / HighContrast | ✅ | ✅ | ✅ |
  | ReduceTransparency | ✅ | ❌ (no Android concept) | ✅ |
  | InvertColors | ✅ (after fix) | ❌ browser support gap | ✅ |
  | ScreenReaderEnabled | ❌ web-blocked | ❌ web-blocked | ✅ |
  | Grayscale | ❌ web-blocked | ❌ web-blocked | ✅ |
  | FontScale | ❌ web-blocked | ❌ web-blocked | ✅ |

---

## 2026-06-30

- **Input.Picker dropdown fixes (web)** — Six compounding bugs blocked the picker: `requestFocus` not a function on raw `TextInput` refs (wrap in an adapter object); storing that adapter in `useState` caused an infinite loop (use `Hooks.useRef`); `RNSeam.Popup.show` received a partially-applied function instead of a React element (apply curried renderer argument-by-argument); `RX.View onPress` does not fire on web (wrap items in `LC.Pressable`); `unionCaseName` throws in Fable 5 (use `.ToString()`); gallery URLs must use underscore DU case names. Files: `Text.fs`, `Field.fs`, `Base.fs`, `Popup.fs`, `RNSeam.fs`, gallery `Picker.fs`.

- **Gallery visual bugs catalogue** — Six visual regressions documented: theme samples appear beside rather than below main samples (horizontal `ScrollView` in `ComponentContent.fs`); picker dropdown silent on web; numeric inputs render too wide; some pages show "Fully Qualified Name not found" (wrong scraper key); Grid pagination misaligned; Tags too wide. Root hypothesis: `width: 100%` on the sample table stretches `flex:1` children.

- **Debug logging recipe** — `printfn` inside `[<Component>]` render functions and event callbacks is the reliable trace technique; `Fable.Core.JS.console.log` with object values throws "Spread syntax requires ...iterable" because Fable compiles it as spread. Use `printfn "%A"` for objects. Future verbose-mode sketch: a `window.__EGGSHELL_DEBUG_UI__` flag with a `UiDebug.log` helper.

- **SegmentedControl drag/tap + code blocks + `mediaMatches` fix** — `With.Accessibility.mediaMatches` was returning the `MediaQueryList` object instead of `.matches` (split into two lines). Replaced `RX.GestureView`'s `PanResponder` with direct RN responder props (`onStartShouldSetResponder`, etc.) because PanResponder never fired on web. Added `preventDefault()` calls and CSS `touch-action: pan-y` via `dataSet` to stop the surrounding horizontal `ScrollView` stealing drags on Firefox Mobile. Fixed `LC.Pre` single-line code by removing `numberOfLines = 1`. Files: `Accessibility.fs`, `GestureView.fs`, `Pre.fs`.

- **Removed `@chaldal/reactxp` core from `LibClient`; web clicks restored** — Deleted `@chaldal/reactxp` from `LibClient/package.json`; added `@react-native-async-storage/async-storage`; fixed `AccessibilityHelpers.mapRoleToString` integer-enum-to-string mapping; fixed `LC.Pressable` web press handling (moved `OnPress` outside the `e.cancelable` guard; replaced `flex 1` overlay with `widthPercent 100; heightPercent 100`); added `window.__DEV__ = true` in `index.html`. Files: `Pressable.fs`, `AccessibilityHelpers.fs`, `RNSeam.fs`, `ReactXPBindings.fs`, `GestureView.fs`, `webpack.config.js`, `index.html`.

- **Ported most remaining ReactXP core APIs to RNSeam** — `LC.Text`/`LegacyText`/`UiText`, `LocalStorageService`, `With.Geolocation`, `ContextMenus`, `LC.Popup`, `Picker.Base`, `UserInterface`/`App`, `Animation.fs`, all `Animatable*` components ported to `react-native` equivalents in `RNSeam.fs`. Key gotchas: `let private mutable` is invalid F#; dynamic `createRoot $ (container)` instead of bare call; `AnimatableTextInput` needs `Animated?createAnimatedComponent(TextInput)`; `react-native-webview` cannot be imported at module top level on web.

- **`LC.VirtualListView` ported to RN `FlatList`** — Public surface unchanged; `data`/`renderItem`/`keyExtractor`/`getItemLayout`/`scrollToOffset` wired; `restoreScroll` preserved. Gotchas: `info?index |> unbox<int>` for dynamic access; JS multi-arg callbacks need an `[<Emit>]` adapter because Fable compiles tupled functions to single-arg; `onScroll` now also attached when `scrollSideEffect` is set. Files: `VirtualListView.fs`, `PaginatedVirtualListView.fs`.

- **Accessibility helpers ported to RN `AccessibilityInfo`** — `AccessibilityAnnounce` and `With.Accessibility` now use `RNSeam.AccessibilityInfoModule` instead of `ReactXPRaw?Accessibility`; `getFontScale` moved to `AccessibilityInfo` (now async); `tryScreenReaderEnabled` removed (async pattern instead). Files: `AccessibilityAnnounce.fs`, `With/Accessibility.fs`.

- **NetInfo web path ported to `@react-native-community/netinfo`** — Both web and native now share one implementation (`NetInfo.fetch()` / `NetInfo.addEventListener`); browser `navigator.onLine` fallback removed. File: `NetInfo.fs`.

- **Build blockers: three compile errors fixed** — (1) F# reserved word `component` as a record field: escape as `` `component` `` in `UiActionLog.fs`. (2) `FS0064` generic over-constraint in `CodecLib.fs`: drop explicit type param list on `toEncoding`. (3) `Window` has no `navigator` member: use dynamic access `Browser.Dom.window?navigator?onLine`.

- **Phase 4 RNW seam: platform APIs ported** — `NativePlatform`/`OS`/`Platform` moved to `RNSeam.fs`; `ReactXP.Runtime`, `UserInterface`, `Linking`, `Clipboard` ported using RN primitives; `RNSeam.assignA11yAndAutomation` added to bundle testID + a11y props; `wrapOnScroll` promoted to public. Files: `RNSeam.fs`, `ReactXPBindings.fs`, `ScrollView.fs`.

- **`react-native-webview` cannot import at top of web-shared module** — Its web dummy uses JSX; webpack cannot process JSX in `node_modules`. Remove the top-level `RNSeam.WebView` import; keep import localized in `WebView.fs` under `#if !EGGSHELL_PLATFORM_IS_WEB`. File: `RNSeam.fs`.

- **Handheld sidebar: root cause was `onLayout` event shape mismatch** — RN delivers layout as `{ nativeEvent: { layout: { x, y, width, height } } }` but ReactXP flattened fields onto the event directly. The zeroed `width` caused `baseOffset = (-0 + 10) = 10`, parking the drawer on-screen. Fix: `RNSeam.assignOnLayout` adapts the RN event shape before invoking the callback (same as `wrapOnScroll`). Also fixed: `LC.Draggable` needed `?styles` for a sized wrapper (Part 1), fill styles on `AnimatableView`/`GestureView` (Part 2), `blockPointerEvents = true` on the closed drawer wrapper so it does not eat pointer/wheel events (Part 4). Files: `RNSeam.fs`, `View.fs`, `ScrollView.fs`, `Draggable.fs`, `Content.fs`.

- **Three more RNW visual regressions** — (1) `AccessibilityRole.Header` mapped to `"header"` → RNW rendered `<h1>` with 2em default font size; fix: map to `"banner"` (`<header>` landmark, no default size). (2) Numeric `lineHeight` passed as unitless CSS became a multiplier; fix: `RNSeam.createTextStyle` appends `"px"` to numeric values. (3) `LC.Nav.Top.Heading` styles missing color/font size because pure-F# components bypass `ComponentRegistry.GetStyles`; fix: bake themed values directly into `makeTextStyles`. Files: `AccessibilityHelpers.fs`, `RNSeam.fs`, `Nav/Top/Heading/Heading.fs`.

---

## 2026-06-29

- **RNW seam: `flex: 0` collapses elements** — CSS shorthand `flex: 0` becomes `flex: 0 1 0%` (zero basis), collapsing sidebar. Fix: `RNSeam.createViewStyle` expands `flex` like ReactXP did (`flex > 0` → `{flexGrow, flexShrink: 1}`, `flex = 0` → `{flexGrow: 0, flexShrink: 0}`). File: `RNSeam.fs`.

- **RNW seam: `AccessibilityRole` integer enum rendered as `role="16"`** — `LibClient.Accessibility.AccessibilityRole` is a plain integer enum; `Option.map box` boxes the integer. Fix: added `RNSeam.mapAccessibilityRole` + `mapImportantForAccessibility` helpers mapping integer values to RNW string names. Files: `RNSeam.fs`, `View.fs`, `Button.fs`, `TextInput.fs`.

- **Phase 4 RNW branch kickoff** — Branch `modernization/rnw` from `modernization/fable5-migration`. First increment: add `react-native-web` to `LibClient/package.json`; alias `react-native$` to `react-native-web` in webpack; spike `RNSeam.fs` with RN style pass-through. `GestureView` deferred (needs `[<Component>]` wrapper for PanResponder); still compiles with `ReactXPRaw?GestureView` fallback.

- **Framework-first UI rule + `LC.SegmentedControl`** — Reusable behavior belongs in `LibClient`/`LibRouter`; apps pass theme tokens only. `LC.SegmentedControl` uses explicit pixel-width segment cells with thumb behind labels; default must be registered via `Themes.Set<LC.SegmentedControl.Theme>` in `DefaultComponentsTheme` or `GetMaybeUpdatedWith` throws at runtime.

- **AppTodo theme toggle: animated sliding thumb** — Pill track with `AnimatableView` + `Animation.Timing` thumb. Fixed `GestureView` shrink-wrapping to label width by using fixed `themeToggleWidth` + `widthPercent 50` segments; do not `SetValue` on layout re-render.

- **AppTodo handheld picker: double-overlay / stuck scrim** — Three close paths firing together; `showList` could stack a second adhoc dialog. Fix: close only via `hideDeferred` once (`closeOnceRef`); `dismissDialog`/`onToggle` only set `ListWasHidden`; guard `showList` when `maybeDialogHideRef` already set.

- **AppTodo swipe row, picker dismiss, search pill** — Done styling must be on title text only (not row opacity). `WhiteRounded.Raw` handheld dialog needed `ContentPosition.Center`, not `Free`. Web focus ring on search pill: strip outline via `app.css` (`input, textarea`).

- **AppTodo dark mode / web inputs** — Firefox keeps white `<input>` fill even when wrapper has `backgroundColor`: fix via transparent fill + zero border on `TextInput`, clip via shell `View` `Overflow.Hidden`, strip `appearance` in `app.css`. Picker field showed a white bar when selected-value overlay was active: collapse input with `hiddenTextInput` (opacity 0, zero size, absolute). Desktop picker popup "flash": defer `Popup.show` to `runOnNextTick`. Dark label contrast: use `TextSecondary` instead of `InputBorder` for `BorderLabelBlurredColor`.

- **AppTodo ui2: phone chrome, category scroll, swipe delete** — Todo row used desktop layout on web (`LC.With.ScreenSize` was `isHandheld = false`): drive from `useCompactUI = usePhoneChrome || isHandheld`. Card stuck left: use `AlignSelf.Center` on `cardShell` when `usePhoneChrome`. Category pills: `LC.ScrollView Scroll.Horizontal` with `FlexWrap.Nowrap` + `flexShrink 0` per pill; `LC.RadioGroup` outside the scroll view. Swipe delete: `RX.GestureView onPanHorizontal` + `AnimatableView animatedTranslateX`; guard Safari's null `pageX` on `isComplete` by using `lastDragOffsetRef`.

- **`Color.Hex` alpha / `Color.White.WithOpacity` crash** — `Color.Hex "#0000001a"` (8-digit) throws at runtime; use `Color.BlackAlpha (26./255.)`. `Color.White.WithOpacity` throws; use `Color.WhiteAlpha 0.4` instead.

- **AppTodo fake services** — Put `#if DEBUG` inside the `| None ->` arm; `ConfigSource.Base.BackendUrl = None`; `initialize` ends with `services () |> ignore`.

- **`build-native`/`package-web` must pass `--define DEBUG`** — Fable stripped `#if DEBUG` fake-service branch on `package` config because `LibFablePlus/src/index.ts` only added `--define DEBUG` for `dev`. After editing `index.ts`, run `npm run build` in `Meta/LibFablePlus` (eggshell loads `dist/index.js`, not TS source).

- **`[<Fable.Core.JS.Pojo>]` in Fable.Core 5.0.0** — Turns a class into a plain JS object literal; `None` optional ctor args are omitted (not `undefined`). Batch-conversion gotchas: wrap ctor in `( ... ) |> box`; F# keywords in JS keys need escaping; curried lambdas need `fun (a) (b) ->` form; precompute `?optional` values in `let` bindings before ctor call. No automated codemod. Reference: `LibClient/src/Accessibility.fs`. Full playbook: `FABLE5_POJO_PLAYBOOK.md`.

- **Fable 5 MSBuild cracker bug** — `dotnet fable <proj>.fsproj --configuration "Web Debug"` fails because the space in the config name is passed unquoted to MSBuild. Do not pass `--configuration "Web Debug"` to Fable; instead compile the `src` directory with `--define` flags directly.

- **Never run bare `dotnet fable` from `LibClient/`** — Without a `.fsproj` path, Fable targets the Fake build graph and emits `.fs.js` next to build scripts. Correct validation: `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"` for type-check; `dotnet fable LibClient/src/LibClient.fsproj ... -o /tmp/... --noCache` for JS emit.

- **Style leaks: never key `Memoize` on `Theme` records or `pointerState` objects** — Fresh record/object references defeat `fast-memoize`; keys must be primitives (Color, int, bool, enum string). The root bug affecting 809 leak reports was `ButtonLowLevelState` carrying a fresh `Actionable` callback ref per render: key on `state.GetName` string instead. `memoize2`-`memoize6` need true N-arg JS functions (Fable compiles multi-arg F# lambdas to length-1 curried JS); use the variadic wrappers in `Memoize.fs`. Full recipe in `docs/fsharp/styling.md`.

- **Gallery audit: `data-test-id` vs `data-testid`** — ReactXP emits `data-test-id` on web; RNW uses `data-testid`. Audit selectors must use `[data-test-id="..."]` or they false-fail.

- **New gallery audit module: `audit-gallery-a11y.mjs`** — Parses `ComponentItem.pageTitle` from `Navigation.fs`; checks `Ui.A11yPanel` (`Font scaling`/`Role` facts) and display heading before interaction handlers run (`runGalleryA11yBaselineAssertions`). Baseline must precede `COMPONENT_HANDLERS` to avoid false failures from checkbox clicks or `ErrorBoundary` bombs.

- **i18n runtime lives in `LibClient`, not `LibUiSubject`** — `LibClient.I18n` holds `Language` DU + `I18n<'T>` runtime. App-local `I18nGlobal` modules wire LocalStorage + EventBus. `LibUiSubject` is for backend-interacting UI. Scaffold template: `Meta/LibScaffolding/templates/app/src/I18n.fs.template`.

- **Agent background terminals are read-only** — Do not ask the user to press `r` in Metro or webpack watch. Agent reloads: `touch` changed `.fs`, restart dev server, `adb`/simctl relaunch.

---

## 2026-06-28

- **Do not fork the user's dev stack** — When the user has `eggshell dev-native` (T1) and Metro (T2) running, do not start competing copies. Read terminal files first; code changes only; confirm T1 compiled the changed file before claiming the bundle is fresh.

- **Fake subject services in `LibUiSubject`** — `FakeSubjectService`/`FakeViewService` under `#if DEBUG` (always listed in `.fsproj`); apps switch real vs fake via `Config.BackendUrl: Option<string>`. Gotchas: do not gate fake `.fs` files on `Web Debug` in `.fsproj` only; `IndexQuery` constructor is private; do not name a helper module `Helpers` inside `SubjectService` (collision).

- **`NU1605` / SDK PATH / `Directory.Build` fixes** — `FSharp.Core` pin must live in `Directory.Build.props` (not only `.targets`) so restore sees it before implicit references. `export PATH="$DOTNET_ROOT:$PATH"` is required alongside `DOTNET_ROOT`. Nested `<!-- -->` inside a comment block in `.targets` causes MSB4024. `LibLifeCycleHost`/`LibLifeCycleHostBuild` needed conditional `PlatformTarget` (`x64` on Windows, `AnyCPU` elsewhere).

- **Fable 5 / SDK 10 migration blockers** — Fable 5.4.0 requires SDK 10 in `global.json` (including nested manifests in `LibStandard/`, `AppEggShellGallery/`, `Meta/FablePlugins/`). SDK 10 rejects `LangVersion 7.0`: set `8.0` in `Directory.Build.props`. `FSharp.Core` central pin must be `>= 9.0.201` for `Fable.React 10`. Stale `fable.lock` hangs Fable — delete `LibStandard/.build/web/fable/fable.lock`. Stale `fable-library-js.4.18.0` means Fable 4 ran last — wipe `.build/web/fable` dir.

- **FablePlugins gate** — `Fable.AST` 5.0.0, `FableMinimumVersion "5.0"`, `Ident.IsInlineIfLambda = false` in `AstUtils.makeIdent`. Fable transpile errors on FablePlugins source are expected and benign.

- **SuiteTodo Phase 5A backend skeleton** — Created `SuiteTodo/` with `Todo.Types`, `LifeCycles`, `Tests`, `Launchers/Dev/DevelopmentHost`. `dotnet test` discovers 0 tests (binding issue; simulation logic in place). Removed `SuiteBenchmark/`.

- **Batch 3 nav/UI testId slugs** — Slug prefixes: `tab-{label}`, `toggle-button-{label}`, `nav-top-item-{label}`, `nav-bottom-item-{label}`, `text-button-{label}`, `scrim-dismiss`, `thumb-{index}`, `context-menu-item-{label}`. Icon-only `ToggleButton`: derive label from `sprintf "%A" value`. Selected `LC.Tab`: expose a11y props on the outer `RX.View` without a Pressable overlay. `Position.Relative` required on overlay-parent containers.

- **Input/form composite testId suffixes** — `Input.Picker Field` root slug + `-open`, `-open-handheld`, `-clear`, `-focus`, `-unselect-{slug}`. Duration/LocalTime: `{root}-{hours|minutes|days}`, `{root}-period`. `Input.Image` thumbs: `{root}-0`, `{root}-1`, ...

- **`LibUiSubject With/*` `[<Component>]` gotchas** — Must `open LibUiSubject.Components.Constructors`; extensions target fully-qualified `type LibUiSubject.Components.Constructors.UiSubject.With with`; do not `open LibLifeCycleTypes_SubjectTypes` in hand-written files.

- **`LibRouter LR.With` extensions** — Must extend `LibRouter.Components.Constructors.LR.With` (fully qualified); call sites need `open LibRouter.Components.With.Location` etc. Delete stale `_autogenerated_/Components/{Dialogs,LogRouteTransitions,...}` when removing the render trio.

- **Dialog `WhiteRounded` invisible** — `Dialog.Confirm`/`Prompt` wrapped `WhiteRounded.Standard` in a zero-size a11y `RX.View`; `Dialog.Base` children used `Position.Absolute; inset 0` which collapsed to zero inside it. Fix: remove extra wrapper; mount `WhiteRounded.Standard` directly. Re-precompile `LibStandard` after changes.

- **`LC.Pressable` overlay zero-width buttons** — When `overlay=true` with no children and a non-flex parent, `flex 1` collapsed hit targets to width 0. Fix: no-children overlay uses `Position.Absolute; trbl 0`. Parent containers need `Position.Relative`. File: `Pressable.fs`.

- **`FRS.input` / `NamedFile` / parallel agent gotcha** — Do not name RefDom callback params `input` when `FRS.input` is in scope (parse conflict). Annotate `(browserFiles: seq<Browser.Types.File>)` to avoid `HttpFile` inference. Parallel agents that run `build-lib` can revert `.fsproj`/render trio; stage sources under `/tmp` and do one atomic copy+patch+build.

- **`[<Component>]` must not use `ref` as a prop name (React 18)** — React 18 treats `ref` as special and does not pass it as a prop. Rename to e.g. `draggableRef` or `scrollViewRef`. File: `Draggable.fs`.

- **Gallery Content batch conversion (69 pages to pure F#)** — All gallery Content pages converted to `[<Component>]` on `type Ui.Content`. Gotchas: `Button.Icon.Left` vs `LC.Buttons.Left` (qualify); ThirdParty `Map` shadows F# `Map` (fully qualify); `position = (...)` must be parenthesized for multi-line.

- **Styling guidance: forward preference** — New files prefer top-level `let foo = makeViewStyles {...}` or `[<RequireQualifiedAccess>] module private FooStyles`. Existing files using `module private Styles` are not mass-migrated. Gallery docs updated: `docs/fsharp/component.md`, `styling.md`, `unsorted/xmldocs.md`.

- **`Input.File` `useEffect` infinite loop** — `useEffect` deps included a fresh `[]` value from `Result.recover`; unconditionally calling `internalValidityHook.update` retriggered forever. Fix: reset only when `value.Length` changes; skip update when already `Valid`. Files: `Input/File.fs`.

- **`AutoUi_InputForm` infinite re-render** — Gallery sample called `FormWrapper.Make` inside `Sample.Render()` (new record every render), causing `useEffect` to re-run on every render. Fix: module-level `let formWrapper = FormWrapper.Make<...>`. LibAutoUi effect deps should be `[||]`. Restart `eggshell dev-web` after LibClient/LibAutoUi changes.

- **Style leak detection in gallery audits** — `audit-gallery-style-leaks.mjs` dedupes `Styles_*` warnings by key per page, reports `STYLE-LEAK:N (M hits)`, rolls up to `pass-N/style-leaks.json`.

- **Style leaks from per-render builders** — Wrap any `makeViewStyles`/`makeTextStyles` called inside render in `ViewStyles.Memoize`/`TextStyles.Memoize` keyed on primitives. Memoized lambda params must not share names with CE operations (`height`, `color`, `fontSize`, `top`, `left`, `bottom`) or they shadow the CE function and cause FS0003/FS0041. File: `docs/fsharp/styling.md`.

- **Input hot-path memo + CE name clashes** — `Input.Text`, `PickerInternals.Field`, `Button`, `LabelledFormField` wrapped in Memoize. Rename helpers named `borderColor` to `outlineColorFor`; rename memo params matching CE ops. `ViewStyles.Memoize` supports at most 6 args.

- **SuiteTodo AppTodo frontend (Phase 5C)** — `SuiteTodo/AppTodo/` auth-free reference app over `todoDef`. List uses `UiSubject.With.View` on `TodoList` with `UseCache.No` + `listVersion` bump after mutations. CI stub: `audit/audit-todo-web.mjs`.

- **SuiteTodo dev-stack + SQL FTS** — `SuiteTodo/dev-stack.sh up` starts Docker SQL + DevelopmentHost + `eggshell dev-web`. `TodoSearchIndex.Title` for full-text search.

- **AppTodo observability (`audit/todo-observe.mjs`)** — CLI: `snapshot|state|add-todo|workflow layout-check|diff|open|logs`. `workflow layout-check` captures before/after add-todo and diffs `todo-card` bounding box. `LC.Constrained` under `AlignItems.Center` caused card shrink-wrap; replaced with `widthPercent 100; maxWidth 560; AlignSelf.Stretch`.

- **AppTodo native scaffold** — `SuiteTodo/AppTodo/scripts/scaffold-native.sh` copies Android template + gallery iOS renamed; adds metro/react-native/babel configs. Android 12+ needs `android:exported="true"` on launcher `MainActivity`.

- **`LC.Input.Checkbox` iconTheme style leak** — `TextStyles.Memoize` keyed on `(iconSize: int)` + `colorCss: string` instead of `Theme` record. File: `Checkbox.fs`.

- **Native Fable 5 Guid needs `crypto` polyfill** — `import 'react-native-get-random-values'` as first line in `index.js` (before Bootstrap). Fable 5's `Guid.js` uses Web Crypto, which RN lacks.

- **iOS `LC.Input.Text` keystrokes wiped** — ReactXP `TextInput.tsx` syncs `props.value` while focused and passes both `value` and `defaultValue` to RN. Fix: vendor patch at `LibClient/vendor/reactxp-native-common/TextInput.tsx` (skip prop sync when `state.isFocused`; drop erroneous `defaultValue`); applied via `postinstall` copy in `LibClient/package.json`.

- **AppTodo ui2 redesign** — `LC.Row`/`LC.Column` ignore `FlexDirection` from `?styles` (component appends its own row style last); use plain `RX.View` with memoized direction style for direction-flipping layouts. `Color.Hex` requires lowercase hex (regex `#[0-9a-f]{6}`). Input theme: added `EditableBackgroundColor`/`LabelBackgroundColor` to `LC.Input.Text.Theme` and `Field.Theme` so dark mode can darken inputs.

- **Android `borderRadius` clip without `Overflow.Hidden`** — A filled, borderless, rounded `RX.View` on Android renders square without `Overflow.Hidden`. Picker floating label: moved to `top -8` so it floats above the field border.

- **`context-provider` React key warning: framework fix** — Root cause was `Fable.React.contextProvider` rendering children without injecting keys. Fix: wrap provider children in `tellReactArrayKeysAreOkay` in `Context.fs` (`Components/AppShell/Context/Context.fs`). Vendor ReactXP patches only when no F# seam exists; Metro bundles from `dist/`, not `src/` — verify patches reach the bundle with `curl`.

- **Fable worklets run on JS thread, not UI thread** — Confirmed via a "jam JS thread for 3s" test: Fable-emitted `useAnimatedStyle` froze mid-animation, proving JS-thread execution. `runOnJS` inside a Fable worklet `SIGABRT`s `libworklets`. Rule: use declarative animations (Moti or Reanimated declarative/CSS API); any genuine UI-thread worklet must be a tiny JS shim in `ThirdParty`. Updated the migration runbook (now [runbooks/migration-execution.md](./runbooks/migration-execution.md) section 4.6).

- **Fable "skipped compilation" is not a green build** — Fable cache can skip type-check and exit 0 without compiling edits. Force recompile by touching changed `.fs` or deleting `.build/*/fable`; grep `error FS` in output.

- **Rules of Hooks: `LC.Input.Picker` inside `LC.With.ScreenSize`** — Calling `renderPickerBase` (uses hooks) inside `LC.With.Context` → `Hooks.useMemo` callback violates Rules of Hooks. Fix: private `[<Component>]` wrapper type `PickerHost` that calls `renderPickerBase` at component top level. `[<Component>]` members must live on a `type`, not a `module`.

- **"Changes don't apply" rule of thumb** — Hot reload / Fable watch often silently serves stale JS. When a fix "should work" but UI is unchanged: stop dev-web, hard-refresh; after LibClient changes restart dev-web entirely; native: kill Metro + `--reset-cache` + rebuild.

---

## 2026-06-27

- **Do not patch `.render`; convert to pure F#** — Editing `.render` or `_autogenerated_/.../Render.fs` is Goal A anti-pattern: files are overwritten by `build-lib`. Correct workflow: hand-write `Foo.fs`, update `ComponentRegistration.fs` + `.fsproj`, remove autogenerated Render, delete `Foo.render`. Rule added to `CLAUDE.md` rule 7.

- **`LC.Pressable`, a11y forwarding, gallery testIds** — `LC.Pressable` replaces `TapCapture` overlay: labeled `RX.Button`, drag-vs-tap threshold, `UiActionLog` press recording. Reserved F# keywords: cannot use `?component` optional arg; use `UiActionInput` record. Android testID maps to `resource-id`, not accessibility id (use `UiSelector().resourceId(...)` and click a clickable child).

- **Stale autogenerated files survive removal from `.fsproj`** — Removing a file from `.fsproj` does not stop Fable/webpack HMR from bundling it (watcher does not rescan fsproj on change). Stale `TypeExtensions.js` defines the constructor as a call to the now-deleted `Make` module, throwing JS value `1` at runtime. Fix: `rm -rf` the autogenerated dirs; restart `dev-web`.

- **`DOTNET_ROOT` must be in `.zshrc`** — The render DSL compiler is a self-contained .NET apphost that resolves the runtime via `DOTNET_ROOT`, not PATH. `export DOTNET_ROOT="$HOME/.dotnet"` in `.zshrc` makes it available to all subprocesses permanently.

- **Gallery app infrastructure converted to pure F#** — App chrome (App, TopNav, Sidebar, ComponentContent, ComponentSample, ComponentProps, Code), all route pages (Home, Docs, Tools, etc.), and supporting components converted. `Ui.Snippets(scope = Scope.One "...")` — bare `One` is ambiguous outside the Snippets module.

- **`eggshell dev-web` operational gotchas** — JS deps live in each lib's `node_modules`, not the app's; run root `./initialize`. `--run webpack-dev-server` only fires after the first successful Fable compile; fix errors before expecting webpack. Stale dev-servers hold the port and serve old bundles; kill by port: `lsof -nP -iTCP:8082 -sTCP:LISTEN -t | xargs kill -9`.

- **Fable StackOverflow on deeply-nested generated render** — Exit 134 deep in `FableTransforms.transformMemberBody` when a single `.render` member had ~250 nested elements (gallery Sidebar). Fix: move long element lists into plain F# helper functions returning `castAsElement [| ... |]` (flat array literals are depth-1; Fable maps them iteratively). `ulimit -s` and `DOTNET_Thread_DefaultStackSize` do not help; only de-nesting does. File: `AppEggShellGallery/src/Components/Sidebar/SidebarContent.fs`.

- **Converting a component can break app callers in ways `dotnet build LibClient` never catches** — Full `eggshell dev-web` surfaced three regressions: dropped `?xLegacyStyles` (`.render` callers always emit it), moved public-type path (FQN-qualified type path), removed `.Make` (hand-written bootstraps call it). After conversion: run full `eggshell dev-web`; grep for `.Make` callers before converting.

- **Gallery Playwright audit scope** — Purpose: visual crawl; assertions target live UI via `[data-text-as-pseudo-element]`. Not covered: code column vs rendered layout parity, animation transitions, selected styling. File picker: never click `Select File`; use `setInputFiles`. Native `window.alert/confirm`: handle via `page.on('dialog')`.

- **Gallery console audit fixes** — `Grid` web: `maybeHeaders` must wrap headers in `dom.tr` and unwrap fragments. `Recharts useRef null`: webpack must alias `react`/`react-dom` to `LibClient/node_modules`. `Map setPosition` errors: guard `getCenter()` until lat/lng valid; `fitBounds` on empty bounds falls back to `dhakaLatLng`. `AsyncData Failed` without `WhenFailed` throws inside `ErrorBoundary` even with `getDerivedStateFromError`; use button-triggered failure demo instead.

- **Gallery `Code`/SyntaxHighlighter crash** — `react-syntax-highlighter` v15 reads input from `children`; passing a React element crashes. Convert `AppEggShellGallery.Components.Code` to `[<Component>]`; extract string synchronously from render-time `children`.

- **`Snippets` `dom.th` crash on Android** — HTML table tags invalid in React Native; wrap in `#if EGGSHELL_PLATFORM_IS_WEB`. `Color.Grey` on native expects two-digit hex (e.g. `"66"`, not `"666"`).

- **`react-router v7_startTransition` crashes on RN 0.76 bridgeless** — `LR.Router defaultFuture` enables `v7_startTransition` on web only; `None` on native. File: `LibRouter.Components.Router`.

- **Native gallery markdown** — Use `react-native-render-html` (not `dangerouslySetInnerHTML`). Set `MaybeInBundleResourceUrlHashedDirectoryPrefix = "/public-dev"` in `configSourceOverrides.native.js`. Metro `extraNodeModules` must include `react-native-render-html` + Showdown deps.

- **Android gallery build (RN 0.76.5)** — Key fixes: generate `debug.keystore`; remove Flipper from `MainApplication.kt`; use `SoLoader.init(this, OpenSourceMergedSoMapping)` (not `false`); autolinking via `@react-native/gradle-plugin`; `pin react-native-maps@1.20.1`; `react-native-svg@15.11.2`; `androidx.core:1.15.0` forced to avoid AGP 9 conflict.

- **iOS scaffold created (RN 0.76.5)** — Gallery had no `ios/` directory. Created from RN 0.76.5 CLI template: target `AppEggshellGallery`, bundle ID `com.eggshell.appgallery`, Podfile `platform :ios, '15.5'`, 97 pods green. Deleted old iOS 18.2/26.2 runtimes (~31 GB) to install iOS 26.5 Simulator runtime. Use `.xcworkspace`, not `.xcodeproj`. `GoogleService-Info.plist` still TODO.

- **NPM dependency upgrade (framework-wide)** — `lodash` to 4.18.1, `typescript` to 5.7.x (`skipLibCheck: true`, `target: es2018`), `gulp` to 5.0.1, `webpack-dev-server` to 5.2.5+, `glob` to 8.1.0 with `glob-promise@6`. Removed unused `gulp`/`glob` devDeps from LibClient/LibRouter/etc. Do not bump `glob` past 8 until `glob-promise` updates.

- **Gallery bootstrap wrong global name + missing `docco.css`** — Bootstrap read `chaldal.AppEggShellGallery.configSourceOverrides` but the host page initialized `eggshell`. Also `docco.css` was missing from `AppEggShellGallery/public-dev/`. Files: `Bootstrap.fs`, `public-dev/docco.css`.

- **Batch-1 parallel conversion gotchas (15 LibClient + 10 gallery)** — Top-level dotted module name is invalid in F# (`module LibClient.X.Y` is a parse error at the top level; use `namespace + module Foo =`). `ParsedTextStyles` needs fully qualified path inside its sibling namespace. Self-referential FQN inside a module is redundant. `\"` before `"""` terminates triple-quoted string early. Nested gallery type extension must match the exact sub-namespace.

- **`LibUiAdmin Grid/QueryGrid` render-DSL retirement** — Native grid crashed on `dom.table`; cross-platform fix uses `RX.View` flex rows on native and `dom.table` on web. Gallery mirror: `Content_Grid/Grid.fs` + `Content_QueryGrid/QueryGrid.fs`. Fragment children in a horizontal `ScrollView` on RN lay out side-by-side (wrap in `FlexDirection.Column` view).

- **Copying `.Render.fs` is not conversion** — Pasting autogenerated render into `Foo.fs` preserves `findApplicableStyles` soup and misses the Goal A modernization. Canonical template: `Tabs.fs`, `Tab.fs`, `TextButton.fs`, `HandheldListItem.fs`.

- **No `.typext.fs` in modern conversions** — Single `Foo.fs` colocates public types and `[<Component>]`. Nested paths use two-part files: `namespace LibClient.Components.Nav.Top` / `module Item` for public API, then `[<AutoOpen>] module Nav_Top_Item` for `[<Component>]`.

- **End-to-end caller upgrade: no parallel legacy shims** — When converting, upgrade every caller in the same change. Remove duplicate `*Styles.Theme.Customize` blocks from `DefaultComponentsTheme.fs`. Delete legacy `*.styles.fs` and drop `RegisterStyles`. Update gallery pages.

- **Gallery Android Appium audit** — Reused web audit recipes via `audit-gallery-android-driver.mjs`. RN `testID` = Android `resource-id` (use `UiSelector().resourceId(...)`). Navigation: top-nav menu tap, not edge-swipe (Android 13 treats edge-swipe as system back). `waitForGalleryAppReady()` polls for menu testId, not just foreground package.

---

## 2026-06-26

- **`timeout` not available on macOS** — No GNU coreutils; rely on the Bash tool's built-in timeout or run long builds in background.

- **Fable docs location** — `fable-compiler/fable.io` repo does not exist; docs live in `fable-compiler/Fable` under `docs/`; live site is https://fable.io/docs.

- **Framework libs require custom build config** — Plain `Debug` does not exist; use `-c "Web Debug"`. Baseline: `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"`.

- **`eggshell convert-component` only prints to stdout** — `RenderConvert` mode logs the readable F# but does not write files. Bulk conversion needs tooling that writes `.fs`, deletes `.render`, and updates `.fsproj`.

- **Framework `.render` count ~118** — LibClient 77, ThirdParty 14, LibAutoUi 5, LibRouter 5, LibUiSubject 4, others. Apps excluded.

- **Full `[<Component>]` modernization requires porting the styling subsystem** — Each component carries a public `Theme` class using `makeCustomize` for global theming and per-instance `Styles` sheets; converting requires porting to `Themes.Set` + `makeViewStyles`, preserving `?xLegacyStyles`/`?children` for half-migrated callers.

- **zsh gotcha: unquoted `--include=*.fs` triggers globbing** — Quote it as `--include="*.fs"`.

- **Full Fable build validation recipe** — (1) root `./initialize` bootstraps all node deps + render compiler. (2) Render DSL apphost needs `DOTNET_ROOT` at non-standard path. (3) `eggshell build-lib` for a lib does render-DSL + TypeExtensions gen only, not Fable. (4) Fable-validate a lib directly: `dotnet fable LibClient/src/LibClient.fsproj --configuration "Web Debug" --noCache -o <dir>`; plugin errors are expected and benign.

- **Genuinely-stateful `[<Component>]` recipe** — Class `Estate` + `GetInitialEstate` → `Hooks.useState initial` in render body; `Actions` methods → plain closures over `hook`; async transitions capture the hook. `ComponentWillReceiveProps` sync → `Hooks.useEffect`. Reference: `QuadStateful.fs`.

- **Render-DSL conversion recipe (validated on Tabs)** — Single `Foo.fs`: `[<AutoOpen>]`, `open Fable.React` (mandatory — without it `[<Component>]` resolves to the wrong attribute type), `module private Styles` with `makeViewStyles`/`makeTextStyles`, `type LC with [<Component>] static member Foo(...)`, bridge `?xLegacyStyles`. Do NOT `open ReactXP.LegacyStyles` (shadows new-dialect CE rules). `Themes.GetMaybeUpdatedWith` requires a prior `Themes.Set` or it throws. `ComponentRegistration.fs` is auto-generated — hand-editing is only temporary. Steps in full at §"Render-DSL → `[<Component>]` conversion RECIPE".

- **Dependency graph: 18 `*Styles.Theme` APIs referenced by other components or apps** — Bulk conversion is not per-component-independent. Two strategies: (A) dependency-aware clusters (producer + all consumers together), (B) compat shim (keep minimal `FooStyles` exposing legacy `Theme.*` while core is modern). Clean leaves (no external `*Styles.Theme` consumers) need neither.

- **Cluster recipe (strategy A) validated on VerticallyScrollable + Sidebar.Base** — Producer exposes `?topStyles`/`?bottomStyles` instead of legacy class cascade; consumer drops its `.styles.fs` and passes styles directly. Convert both together; build green together.

- **Nested-namespace components** — Use underscore module names (`Sidebar_Base`, `Form_Base`, `AppShell_Content`) to avoid the F# rule that a dotted module name hides a same-named static member from sibling files. Public API stays `LC.Sidebar.Base`/`LC.Form.Base`; only the F# module name changes.

- **`LibUiAdmin Grid/QueryGrid` render-DSL retirement (grid layout/native)** — Converted `Grid.fs`/`QueryGrid.fs` to `[<Component>]`; native uses `RX.View` flex rows + `UiAdmin.GridCell`/`UiAdmin.GridRow`; web keeps `dom.table` + global `la-table` CSS. Native grid was invisible without a flex-bounded parent — use desktop-style nav (top + bottom) and explicit `minHeight` on native. Use `unwrapFragmentChildren` to flatten fragment children into flex rows.
