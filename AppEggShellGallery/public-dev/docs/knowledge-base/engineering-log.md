# Engineering Log

This is the running engineering log for the EggShell modernization effort (formerly LEARNINGS.md). Newest entries first; append new entries at the top with a date. For the distilled, timeless troubleshooting reference see [Troubleshooting](./runbooks/troubleshooting.md).

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
