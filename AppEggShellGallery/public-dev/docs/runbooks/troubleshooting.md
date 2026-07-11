# Troubleshooting

Symptom-to-fix catalog organized by theme. Entries come from the dev-loop gotchas and curated durable entries from the [Engineering Log](./knowledge-base/engineering-log.md). If your symptom is here, apply the fix directly. If it is not here and the fix is not obvious after one attempt, escalate.

Related: [Build and rebuild](./runbooks/build-rebuild.md) | [Dev loop](./runbooks/dev-loop.md) | [Android](./runbooks/android.md) | [iOS](./runbooks/ios.md) | [Web](./runbooks/web.md) | [Migration execution: Phase 4 pitfalls](./runbooks/migration-execution.md#phase4-pitfalls)

---

## Accessibility {#accessibility}

| Symptom | Cause | Fix |
|---|---|---|
| VoiceOver/NVDA/JAWS on web never speaks live-region updates | `AccessibilityInfo.announceForAccessibility` is native-only; silently fails on web inside `try/with` | Use `LC.LiveRegion.announcePolite` / `LC.LiveRegion.announce` -- the web branch now updates a hidden `aria-live` DOM node. Never call `announceForAccessibility` directly. |
| `LC.With.Accessibility` `InvertColors` always false on iOS web | Prior code used `(forced-colors: active)` (Windows High Contrast only) | Fixed to `(inverted-colors: inverted)` (CSS MQ Level 5, Safari 14+/iOS 14+). |
| `InvertColors` always false on Android web (Chrome/Firefox) | `(inverted-colors: inverted)` is Safari-only; Chrome and Firefox have no support | `[web-blocked]` -- permanently undetectable on Android web; native branch works correctly. |
| Swipe delete button announced without context ("Delete" only) | `SwipeDeleteLabel` used as-is; no item title included | Use `todoActionLabel todo i18n.t.DeleteActionFormat` (= `"Delete {title}"`). Same pattern as the non-swipe delete button. |
| Screen reader announces button label on activation, drowns out live region | Screen reader reads focused element label when activated; live region fires shortly after and may overlap | Ensure `LC.LiveRegion.announcePolite` fires after the async operation completes, not before. Use `Polite` politeness so it queues rather than interrupts. |
| `React.createElement("a", props, labelText)` gives `FS0001: string not compatible with ReactElement seq` | Third argument to `React.createElement` must be `ReactElement seq`, not `string` | Use `React.createElement("a", props, [| !!labelText |])` -- box the string with `!!`. |
| `:focus-visible` ring not appearing on keyboard-focused buttons | RNW renders buttons as `<div role="button">` which browser doesn't ring by default | `FocusVisibleStyles.injectIfNeeded()` is called by `LC.AppShell.Content` on mount; rings all ARIA interactive roles. If using AppShell, this is automatic. |
| Android crash: `Invalid accessibility role value: banner` (or `group`, `listitem`, `list`, etc.) | `mapRoleToString` returned an ARIA web role not in Android RN's `accessibilityRole` enum | `"banner"` fixed to `"header"`. Web-only ARIA roles (`list`, `listitem`, `listbox`, `group`, `log`, `status`, `dialog`, `option`, `main`, `navigation`, `complementary`) now return `None` on native via `#if EGGSHELL_PLATFORM_IS_WEB` guard. |
| Android crash: `java.lang.Double cannot be cast to java.lang.String` on `accessibilityRole` | Raw F# int enum passed directly to `props?accessibilityRole` | Always pipe through `ReactXP.RNSeam.mapAccessibilityRole` before setting: `accessibilityRole \|> Option.bind RNSeam.mapAccessibilityRole \|> Option.iter (fun v -> props?accessibilityRole <- v)`. |
| Android crash: `java.lang.String cannot be cast to java.lang.Double` on `lineHeight` in `RCTText` | `RNSeam.createTextStyle` appended `"px"` to numeric `lineHeight` on all platforms | Fixed: lineHeight-to-string conversion is now web-only (`#if EGGSHELL_PLATFORM_IS_WEB`). Native path keeps `lineHeight` as a number. |

---

## Build and cache {#build-cache}

| Symptom | Cause | Fix |
|---|---|---|
| `Skipped compilation because all generated files are up-to-date!`, edit ignored | Fable cache false-green; exits 0 without type-checking | `touch` the changed `.fs` / `rm -rf .build/<platform>/fable`; confirm `Started Fable compilation...` and no `error FS`. |
| Build green, but change not visible on device | App not reloaded, or Metro cached old bundle | Reload app; Metro `--reset-cache`; verify with `curl .../index.bundle... | grep -c <marker>`. |
| `.fsproj` edit ignored by watch | Fable does not watch `.fsproj` | Restart `dev-native` (or `dev-web`). |
| `Directory is locked, waiting for max 600s` | Prior `dotnet fable` run left `.build/web/fable/fable.lock` | Delete `LibStandard/.build/web/fable/fable.lock`; kill stray `dotnet fable` processes; confirm `dotnet fable --version` is `5.4.0`; wipe `.build/web/fable` after a tool bump. |
| `dotnet fable --version` returns wrong version after bump | `.config/dotnet-tools.json` bumped but nested `global.json` not updated | Also bump `LibStandard/global.json`, `AppEggShellGallery/global.json`, and `Meta/FablePlugins/global.json`; run `dotnet tool restore`. |
| `stale fable_modules/fable-library-js.4.18.0` under `.build/web/fable` | Fable 4 ran last; switching to 5 left old runtime | Wipe the whole `.build/web/fable` directory; the tool copies `fable-library-js.5.x` on the next run. |
| Fable emits into wrong directory | `dotnet fable` run from wrong cwd without an explicit `-o` | Always pass a project path and explicit `-o`; never run bare `dotnet fable` from `LibClient/` (it compiles the Fake build graph instead). |
| Fable StackOverflow (exit 134) at compile | Deeply nested render DSL generates a single huge `render` member that overflows the optimizer | Break long element lists out of `.render` into flat F# array helpers (`castAsElement [| ... |]`). See LEARNINGS 2026-06-27. |
| `eggshell build-lib` exits green but app still broken | `build-lib` only runs render/typext codegen, not Fable | Force-check with `dotnet fable <src> -o /tmp/... --noCache --define DEBUG --define EGGSHELL_PLATFORM_IS_WEB`. |
| `error FS0039: The value 'jsNative' is not defined` in a file that uses `[<Emit(...)>] let x = jsNative` | Fable.Core 5.0.0 moved `jsNative` to top-level `Fable.Core` (no longer in `Fable.Core.JsInterop`); the file opens only `Fable.Core.JsInterop`, or uses the now-stale fully-qualified `Fable.Core.JsInterop.jsNative` | `open Fable.Core` (in addition to `Fable.Core.JsInterop`); and replace any `Fable.Core.JsInterop.jsNative` with unqualified `jsNative`. LibClient files already `open Fable.Core`, so they are unaffected. |
| `NU1202: Package Fable 5.4.0 is not compatible with net7.0` | `global.json` still pins SDK 7.x | Bump `global.json` to `10.0.x` before `dotnet tool restore`. |
| `FS3880: LangVersion 7.0 is not valid` after SDK bump | Per-project `<LangVersion>7.0</LangVersion>` rejected by SDK 10 | Set `<LangVersion>8.0</LangVersion>` in `Directory.Build.props`; bulk-replace per-project overrides. |
| `FSharp.Core` pin conflict with `Fable.React 10.0.0-alpha.1` | Old pin `7.0.403` conflicts with `>= 9.0.201` requirement | Bump the central pin in `Directory.Build.targets` to `9.0.201`. |
| `FS0046: The identifier 'component' is reserved` | Pojo record field named `component` | Escape it: `` ``component``: string ``. |
| `FS0064: type variable 't has been constrained to be type 'obj'` | Explicit type parameter on an `inline` wrapper over-constrains `'t` | Drop the explicit type parameter list and let inference pick up the upstream constraints. |
| `.fs.js` files appear beside `.fs` sources; edits behave unpredictably | A raw `dotnet fable` run (bypasses eggshell; output belongs under `.build/<platform>/`) | Run `.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh`, then rebuild via `./eggshell build-lib` / `eggshell dev-*`. Never invoke `dotnet fable` directly. |

---

## Stale autogenerated files {#stale-autogen}

| Symptom | Cause | Fix |
|---|---|---|
| Runtime `ERROR 1` or `calling undefined` after converting a component | Old `_autogenerated_/.../Foo.TypeExtensions.js` still in webpack module graph | Delete the autogenerated directories from disk (not just from `.fsproj`); restart `dev-web`. |
| `ComponentRegistration.fs` still references a deleted component | `.render` file left on disk after converting the component | Delete the `.render` file; run `eggshell build-lib` to regenerate `ComponentRegistration.fs`. |
| Orphan `.render` check | Converted component missing its cleanup | `find Components -name '*.render' | while read r; do d=$(dirname $r); b=$(basename $r .render); [ -f "$d/$b.fs" ] && [ ! -f "$d/$b.typext.fs" ] && echo "ORPHAN: $r"; done` |

---

## Styling and theming {#styling-theming}

| Symptom | Cause | Fix |
|---|---|---|
| Pure-F# `[<Component>]` ignores its theme (wrong color/size) | Pure-F# components do not call `ComponentRegistry.GetStyles` | Bake themed values into `makeViewStyles`/`makeTextStyles` directly (or add a `Themes.Set<SomeType>` mechanism). |
| StyleLeakDetector fires on second render | `makeViewStyles`/`makeTextStyles` running inside render body | Move all style calls to top-level `let`; use `ViewStyles.Memoize`/`TextStyles.Memoize` for parametrized styles; key memoize on primitives (Color/int/bool), never on whole `Theme`/`Colors` records or `Union` DU values. |
| `Memoize` cache never hits | Keyed on a `Theme` record, `PointerState`, or a Fable `Union` DU that gets fresh references each render | Extract primitive fields (`Color`, `int`, `bool`, `GetName`) at the call site and key on those. |
| Memoized lambda parameter name fails to compile | CE operation name collision (`height`, `color`, `fontSize`, `top`, `left`, `bottom`) | Rename the parameter (`itemHeight`, `labelColor`, ...). |
| `makeCustomize`/`ComponentRegistry.RegisterStyles` theme has no effect on pure-F# component | Pure-F# components skip `ComponentRegistry.GetStyles` | Bake themed values into the component's `makeViewStyles` directly. |

---

## RNW / ReactXP binding quirks {#rnw-reactxp}

| Symptom | Cause | Fix |
|---|---|---|
| `onLayout` width/height is 0; drawer "always open"; responsive collapses | react-native(-web) delivers layout as `{nativeEvent:{layout:{x,y,width,height}}}`; ReactXP flattened it; raw forwarding drops the values | Use `RNSeam.assignOnLayout __props onLayout` (wraps/adapts the event shape). See LEARNINGS 2026-06-30. |
| `role="16"` in HTML; a11y role ignored by screen reader | `AccessibilityRole` is an integer enum; raw `Option.map box` boxes the integer | Use `accessibilityRole |> Option.bind RNSeam.mapAccessibilityRole |> Option.map box`. |
| Header view renders at ~2em font size; children inherit huge font | `Header` mapped to `"header"` in `mapAccessibilityRole`; RNW maps `"header"` to ARIA `heading` then `<h1>`; browser default is 2em | Map `Header -> "banner"` (RNW maps to `<header>` landmark with no default font size). |
| `lineHeight` huge (e.g. 384px on a 16px font) | Numeric `lineHeight` in `makeTextStyles` passed as CSS unitless multiplier | Route through `RNSeam.createTextStyle` which appends `px` to numeric `lineHeight` values. |
| `flex 0` element collapses to 0 width | Raw `{flex: 0}` hits CSS as `flex: 0 1 0%`; ReactXP expanded it to `{flexGrow:0, flexShrink:0}` | Use `RNSeam.createViewStyle` (replicates ReactXP flex expansion). |
| Patching node_modules has no effect on native | Metro bundles `dist/`, not `src/` | Patch the built file in `dist/`; verify with `curl .../index.bundle... | grep -c <marker>`; prefer an F# seam fix. |
| `useAnimatedStyle` runs on JS thread, not UI thread | Fable worklets do not execute on the UI thread (proven with probe 2026-06-29; `runOnJS` inside a Fable worklet causes SIGABRT) | Use Moti (`from`/`animate`/`transition`) for declarative animation; write genuine UI-thread worklets in a JS shim (`ThirdParty/worklets.js`) and call them from F#. |
| Clicking `LC.Pressable` does nothing on web | `onPressOut` event is not cancelable; old guard blocked the action | Action call must be outside the `e.cancelable` guard. |
| Absolute overlay/drawer swallows clicks and wheel events beneath it | Wide/tall absolute wrapper is a pointer target even when the drawer is off-screen | Set `blockPointerEvents = true` (RN `box-none`); park closed overlay fully off-screen. Locate via `elementFromPoint`/devtools hit-highlighting. |
| `react-native-webview` causes webpack parse error | WebView dummy for web contains JSX; webpack doesn't apply JSX loader to node_modules | Keep the `react-native-webview` import localized to `WebView.fs` under `#if !EGGSHELL_PLATFORM_IS_WEB`; do not import it at the top level of `RNSeam.fs`. |

---

## Layout and measurement {#layout}

| Symptom | Cause | Fix |
|---|---|---|
| Filled rounded view shows square corners on Android | Missing `Overflow.Hidden` on the filled view | Add `Overflow.Hidden` to any view that combines `backgroundColor` with `borderRadius`. |
| Responsive row won't stack on handheld | `LC.Row` appends `FlexDirection.Row` last, overriding your style | Use `RX.View` with a direction-correct style instead of `LC.Row`. |
| `LC.Pre` (code block) renders as a single line | `numberOfLines = 1` set on the `Text` | Remove `numberOfLines = 1`; RNW `Text` defaults to `white-space: pre-wrap`. |
| `LC.Row` / `scrollView` ignores `flexShrink` on children | Pills or items compress rather than overflow | Set `flexShrink 0` on each item; use `FlexWrap.Nowrap` on the row. |
| Sidebar invisible (height 0) after RNW migration | `RX.View` (now `RNSeam.View`) has no in-flow content; RNW gives it `height:0`; `top:0; bottom:0` resolves relative to zero-height parent | Apply an explicit absolute position + height via `?styles` on `LC.Draggable` (see LEARNINGS 2026-06-30). |
| `AnimatableView`/`GestureView` doesn't fill its wrapper | Default `flexGrow:0` collapses to content height | Add `flex 1` to `AnimatableView` and `GestureView` when the parent has a known size. |
| Header / top content sits **behind the status bar** on device (underlaps clock/battery) | The device safe-area inset isn't applied. `Rn.View(useSafeInsets = true)` looks like it handles this but the seam **ignores** the prop (no-op); `react-native-safe-area-context` may not be installed / no `SafeAreaProvider` mounted | Use the `Rn.SafeArea.useInsets()` hook (native `useSafeAreaInsets`, zeros on web) in a `[<Component>]` and apply the inset. `LC.Nav.Top.Base` already grows the bar by `insets.Top` + `paddingTop` so the coloured header fills the status-bar strip. Requires `react-native-safe-area-context` in the app `package.json` and `SafeAreaProvider` at root (`RnPrimitives.setMainView`). Log 2026-07-08 (session 11, RW8-1). |

---

## Color and hex {#color-hex}

| Symptom | Cause | Fix |
|---|---|---|
| Runtime crash `Color is expected to match #[0-oa-f]{6}` | Uppercase hex in `Color.Hex`, or an 8-digit `#RRGGBBAA` literal | Lowercase all hex; use `Color.BlackAlpha (a./255.)` or `Color.WhiteAlpha a` for transparency instead of 8-digit hex. |
| `Color.White.WithOpacity` throws `Cannot adjust opacity of this color` | `WithOpacity` only handles hex/RGB/RGBA/BlackAlpha/WhiteAlpha | Use `Color.WhiteAlpha 0.4` (or the appropriate alpha constructor) instead. |

---

## Dark-mode inputs {#dark-mode-inputs}

| Symptom | Cause | Fix |
|---|---|---|
| White/blank inputs in dark mode (Android, Firefox especially) | Input theme not applied per appearance; or web TextInput keeps browser default white fill | Apply the `EditableBackgroundColor`/`LabelBackgroundColor` theme per appearance; strip browser default via `app.css` (`appearance`/background/outline on `input, textarea`); use transparent fill on the inner `TextInput` and carry `backgroundColor` on the shell `View`. |
| Picker field shows a white bar between selected value and chevron | Full-width `TextInput` native white box showing through when `shouldShowSelectedValue` | Collapse the input with `hiddenTextInput` (opacity 0, zero size, absolute) when showing the selected value overlay. |
| Desktop picker popup "flash" on open | `Popup.show` fires on the same click that triggers `onDismiss` | Defer `Popup.show` to `runOnNextTick`. |
| Dark label contrast too low | `palette.InputBorder` used for label text (token is for strokes) | Use `TextSecondary` for label text color. |
| Picker/Dialog `TypeError: undefined is not a function` at `AndroidTextInput` on open (green-flash / ErrorBoundary remount) -- **native, RN 0.86** | The raw RN/RNW `TextInput` ref has `focus()`/`blur()`/`clear()` but **not** the ReactXP-era `requestFocus()`/`selectAll()` that `ITextInputRef` still promises; a `With.Ref` → `input.requestFocus()` calls `undefined`. (`LC.Input.Text` is unaffected -- it focuses via the `autoFocus` **prop**.) | **Fixed in the `Rn.TextInput` seam** (`TextInputRN.adaptRef`): the seam adapts the instance in place before the caller sees it -- `requestFocus() -> focus()`, `selectAll() -> setSelection(0, huge)` (RN clamps to text bounds). Callers no longer wrap the ref themselves. Log 2026-07-08 (session 12). |
| Storing ref in `useState` causes infinite update loop | Ref callback is a new function every render; new wrapper object each time triggers another state update | Store the ref wrapper in `Hooks.useRef` instead of `useState`. |

---

## Picker and dropdown {#picker}

| Symptom | Cause | Fix |
|---|---|---|
| Picker dropdown does not open on web | Popup list items use `RX.View(onPress=...)` which does not receive DOM click events | Wrap each item in `LC.Pressable`. |
| `RNSeam.Popup.show` receives a function instead of a React element | `renderPopup $ (rect, 0, 0, 0)` applies the curried renderer with a tuple (partial application) | Apply argument-by-argument: `renderPopup $ rect $ 0 $ 0 $ 0`. |
| Gallery picker page throws blank-screen "Exception" | `unionCaseName` throws at runtime in Fable 5 for union types | Replace `unionCaseName` with `.ToString()`. |
| Handheld picker dialog stuck at top; X and selection do not close | `ContentPosition.Free` on handheld means `JustifyContent.Center` never applies | Use `ContentPosition.Center`; dismiss with `DialogCloseMethod.HistoryForward`. |
| Picker double overlay / stuck scrim | Multiple close paths fire together (`dismissDialog` + `onToggle` + `hideDeferred`) | Close only via `hideDeferred` once (`closeOnceRef`); guard `showList` when `maybeDialogHideRef` already set. |

---

## React key warnings {#key-warnings}

| Symptom | Cause | Fix |
|---|---|---|
| Dev-only "Each child in a list should have a unique key" warning | Fable.React `contextProvider` does not key its children | Wrap provider children with `tellReactArrayKeysAreOkay` (calls `React.Children.toArray` to inject stable keys) in F#; do not patch `contextProvider` in node_modules. |
| Key warning from a static sibling array | Static `[| ... |]` passed as children without keys | Route through `castAsElementAckingKeysWarning` / `tellReactArrayKeysAreOkay`. |
| Deleting a row in a dynamic list leaves an empty/stale shell; stateful children (swipe rows) show wrong content | Positional keys (`toArray` `.$0/.$1`) reuse a component instance for a different item on delete; the reused instance keeps its state (open swipe, `translateX`) | Give each item a stable `key = <id>`. On a Fable `[<Component>]`, a `?key: string` arg is emitted as BOTH the React `key` and a `$key` prop, so `Foo(key = id, ...)` sets the reconciliation key; `toArray` then preserves it. The "route through `toArray`" rule is for *static* arrays only. |

---

## Gesture handling {#gestures}

| Symptom | Cause | Fix |
|---|---|---|
| `PanResponder` never fires on web | RN `PanResponder` does not work in this bundle on web | Use direct RN responder props (`onStartShouldSetResponder`, `onResponderGrant`, `onResponderMove`, `onResponderRelease`, `onResponderTerminate`). |
| Horizontal `ScrollView` steals RTL drags | Scroll view captures pointer before responder | Call `preventDefault()` on the responder event and add `onTouchStart`/`onTouchMove` handlers that also prevent default; add CSS `touch-action: pan-y` (or `pan-x`/`none`) via `dataSet` on the `GestureView` root. |
| Swipe false-delete on Safari/mobile: huge delta on gesture complete | `pageX` is `null`/`undefined` on `PanGestureState.isComplete` (also seen in `Draggable.fs`) | Use `lastDragOffsetRef` from live drag frames for the completion delta; ignore completes with `|delta| < activationThreshold`. |
| `Cannot read property 'pageX' of null` crash in `makePanStateFromEvent` on Android `onResponderRelease` | RN pools synthetic events and nulls `nativeEvent` after a handler returns; the release event's `nativeEvent` is already null | Read coordinate primitives (`locationX/Y`, `pageX/Y`) synchronously during grant/move into a ref; never retain the event object; release/terminate use the captured coords. `GestureView.fs`, LEARNINGS 2026-07-02 (session 5). |
| `GestureView` `onTap` picks the wrong target (SegmentedControl always selects segment 0) | `nativeEvent.locationX` is relative to the deepest touched child, not the GestureView | Measure the view's page origin via `measureInWindow` (cached on layout) and report a view-relative `clientX = pageX - originX`. Gate the measure to `OnTap` views only (measuring every GestureView floods the bridge with `measureInWindow` callbacks). |
| Whole page reloads through a loader and scroll jumps to top on every action | `Executor.DisplayErrorsManually` returned a bare element when idle but a 2-element array (page + spinner) while `InProgress`; React remounts the page when the child shape flips | Always return `castAsElementAckingKeysWarning [| pageContent; (if spinner then overlay) |]` so `pageContent` is a stable first child. Suppress the global spinner per app via `AppShell.Context(showTopLevelSpinnerForKeys = Some Set.empty)`. |
| `Fatal signal 6 (SIGABRT)` / `getHostFunction` in animation worklet | `runOnJS` (or any host-function call) inside a Fable-emitted worklet aborts `libworklets`; Fable closures are not real UI-thread worklets | Do not write worklets in F#. Use Moti for declarative animation. Write genuine UI-thread worklets in a JS shim and call from F#. |
| Swipe-in-a-list is janky / stops mid-drag, leaks to vertical scroll, or the finger-lift trips the row's edit -- **native/Android only** | A custom JS-responder `GestureView` cannot beat a **native** `ScrollView`: Android terminates the child responder mid-gesture **even when `onResponderTerminationRequest` returns `false`** (confirmed: `TERMINATE (stolen) isActive true` in the responder trace). The `grant->terminate` ping-pong is the jank. Web is unaffected (`div` + `touch-action` yields the pan). | Use `RX.HorizontalPanArea` (`LibClient`), which on native uses react-native-gesture-handler `PanGestureHandler` with `activeOffsetX`/`failOffsetY` (native arbitration); web keeps the `GestureView` path. Wire `RX.GestureHandlerRootView` at the app root and `import 'react-native-gesture-handler'` first in `index.js`. Log 2026-07-07 (session 7). |
| `TypeError: Cannot define property` (dev) when a component calls into a native module (e.g. RNGH `PanGestureHandler`); message is truncated, stack shows `deepFreezeAndThrowOnMutationInDev` -> `enqueueNativeCall` | A Fable `float[]`/`int[]` compiles to a **typed array** (`Float64Array`), whose indices are non-configurable. RN dev **deep-freezes native-call arguments**, so freezing the typed array throws. | Pass a **plain JS array** to native props, e.g. `[<Emit("[$0, $1]")>] let jsPair (a:float) (b:float) : obj = jsNative`. To read the real error behind React's truncated warning, wrap in a temporary error boundary that `console.log`s `error.stack`. |
| RNGH gesture handler renders but swipe covers a sibling behind it (e.g. tap-to-delete on the revealed delete slot stops working) | The gesture wrapper was placed *around* the animated/translating surface, so the (static) gesture view stays full-width and overlays the sibling | Put the gesture **inside** the translating surface: `AnimatableView(translateX) > HorizontalPanArea > rowContent`, so the whole gesture area moves and reveals (never covers) the sibling. |
| RNGH `PanGestureHandler`/`GestureDetector` throws or the ref/viewTag is null with a `wrapComponent` child | RNGH attaches a `ref` + `collapsable` to its single child; a Fable `wrapComponent` function component cannot receive them | Wrap the child in a raw ref-forwarding RN `View` (`ReactXP.RNSeam.createElement ReactXP.RNSeam.View ...`) as the handler's direct child. |
| `HorizontalPanArea` / any RNGH gesture silently does nothing on native | No `GestureHandlerRootView` ancestor -- RNGH gestures are no-ops without one; also needs `import 'react-native-gesture-handler'` first in `index.js` on Android | Ensure both. **But do not reflexively mount the root app-wide** -- see next row. |
| Adding an app-wide `Rn.GestureHandlerRootView` breaks other gestures (e.g. the sidebar drawer starts closing on a *vertical* scroll) | RNGH's root takes over **native touch arbitration for its whole subtree** and starves react-native's JS-responder gestures (`PanResponder` / the RXP-style `Rn.GestureView` used by `Draggable`/`Scrim`) | **Scope the RNGH root** to just the subtree that uses RNGH (e.g. wrap only the pannable widget: `Rn.GestureHandlerRootView(children, fillParent = false)`), not the whole app. An app that is *all* RNGH (AppTodo) can mount it at the root; an app mixing RNGH with JS-responder gestures (the gallery) must not. Log 2026-07-08 (session 12). |
| Do **not** toggle `blockPointerEvents`/`pointerEvents` on a view mid-gesture to suppress child taps | Changing pointer-events during an active touch **cancels the live native gesture** (swipe dies before threshold) | Suppress child taps out-of-band instead: a small time-window guard (`SwipeTapGuard`) armed on gesture start/update/end that wraps the child's `onPress`. |
| Scrolling a list **fires a tap** on the item under the finger (native/Android) | `LC.Pressable` used to invoke `OnPress` from RN's **`onPressOut`**, which RN calls even when an ancestor `ScrollView` cancels the press on a scroll; and RN reports `onPressOut` coords at the **press-DOWN** point, so a pressIn/pressOut movement guard sees ~0px and cannot tell a scroll from a tap. RN's real `onPress` (cancelled by a scroll) was a no-op. | Drive the press from RN's **`onPress`** on native (scroll-safe); keep `onPressOut` for the web click path only. Fixed in `Pressable.fs` (`firePress`); log 2026-07-08 (session 11, RW8-3). General rule: `onPressOut` is not a reliable "tap happened" signal on native. |

---

## Environment and toolchain {#environment}

| Symptom | Cause | Fix |
|---|---|---|
| Emulator window sideways or landscape | AVD `skin.name` reversed (e.g. `2400x1080`) + landscape default in `config.ini` | Edit `~/.android/avd/<AVD>.avd/config.ini`: `hw.initialOrientation = portrait`, `skin.name = 1080x2400`; delete snapshot cache. See [Android: boot](./runbooks/android.md#boot). |
| `dotnet --version` returns 137 or is killed | .NET 10 host SIGKILL'd; environment problem, not code | Escalate; do not attempt code fixes. |
| `eggshell` or `dotnet fable` cannot find the runtime | `DOTNET_ROOT` not set | `export DOTNET_ROOT="$HOME/.dotnet"` (add to `.zshrc`). |
| Duplicate `dev-native`/Metro instances; racing output; half-old bundle | Stale background processes from a previous session | Kill all stale processes; see [Build and rebuild: killing stale processes](./runbooks/build-rebuild.md#killing-stale-processes). |
| `DevelopmentHost` fails to load `LibLifeCycleHost` assembly on Apple Silicon | `PlatformTarget=x64` in `LibLifeCycleHost` conflicts with AnyCPU host | Use conditional platform: `x64` on Windows, `AnyCPU` elsewhere. |
| `TypeError: Cannot read property 'add' of undefined` tapping navbar (RN 0.76 bridgeless) | `LR.Router` defaulted `future.v7_startTransition = true`; React Router wraps navigation in `React.startTransition`, which breaks on bridgeless | Enable `v7_startTransition` on web only; set `None` on native in `LibRouter.Components.Router.defaultFuture`. |
| `adb connect` "failed to connect" to a phone that pings fine | Not yet paired, or used the pairing port instead of the connect port (they differ and rotate) | `adb pair <ip>:<PAIRING_PORT> <code>` (pairing dialog), then `adb connect <ip>:<CONNECT_PORT>` (main Wireless debugging screen). See [Android: physical device](./runbooks/android.md#physical-device). |
| Release APK will not install ("no signature" / `INSTALL_PARSE_FAILED_NO_CERTIFICATES`) | `android/app/build.gradle` `release` signingConfig is empty unless `MYAPP_RELEASE_*` props are set, so `assembleRelease` is unsigned | Sign with the debug keystore: `./gradlew assembleRelease -PMYAPP_RELEASE_STORE_FILE=debug.keystore -PMYAPP_RELEASE_STORE_PASSWORD=android -PMYAPP_RELEASE_KEY_ALIAS=androiddebugkey -PMYAPP_RELEASE_KEY_PASSWORD=android`. |
| Standalone release app has no todos / needs a backend | A true non-DEBUG build drops `FakeTodoService` (`#if DEBUG`) | The release bundle packages the existing `.build/native/commonjs` (Fable `--define DEBUG` watch output), so fake data stays in; do not recompile Fable without `DEBUG` for a standalone demo build. |
| `adb shell input tap`/`text` does nothing on a native RN build | Taps inside a swipe `GestureView` are swallowed (it claims the responder on touch-start; only real touches negotiate to the child); `input text` sets the native value without firing RN `onChangeText` | Use real touch or the Appium `observe` harness ([Audit toolkit](./runbooks/audit-toolkit.md)); raw adb is fine only for large simple taps. |
| `react-native-gesture-handler` Kotlin compile error `Cannot access 'ViewManagerWithGeneratedInterface'` on `run-android` | RNGH major mismatched to the RN version's codegen | On **RN 0.76** pin `react-native-gesture-handler@2.21.2`. On **RN 0.86** use **RNGH 3.0.2** (2.31.x is JS-incompatible with 0.86 -- see [RN 0.86 upgrade](#rn86-upgrade)). |
| `Failed to free NNN on storage device at /data` on install | Device storage full; a debug APK bundles native libs for **all 4 ABIs** (~600 MB) | Free space and/or build a single ABI: `run-android --active-arch-only` (builds only the connected device's architecture). |

---

## React Native 0.86 / New Architecture upgrade {#rn86-upgrade}

Distilled from the session-8 upgrade (RN 0.76.5 -> 0.86, New Architecture / Fabric). Full recipe +
authoritative version values in the [Engineering Log](./knowledge-base/engineering-log.md) (session 8).

| Symptom | Cause | Fix |
|---|---|---|
| Fable/web build: `The namespace or module 'ReactXP' is not defined` in `_autogenerated_/*.TypeExtensions.fs`, even after the codebase rename; `dotnet build` masks it and git shows those files `MM` | The render-DSL compiler (`Meta/AppRenderDslCompiler`, a prebuilt `net7.0` dll) **regenerates** those files on build from its own emit; its F# source was renamed but the **built dll was stale** | Rebuild the compiler: `dotnet build Meta/AppRenderDslCompiler/compiler/AppRenderDslCompiler.fsproj -c Release`, then rebuild. Any change to what the render compiler emits requires rebuilding the dll. |
| Gradle native build fails compiling a library's Fabric C++: `use of undeclared identifier 'BaseShadowNode'` / `unknown type name 'SharedImageManager'` | The native module's version predates RN 0.86's Fabric C++ API (old codegen) | Bump the native module. Working set for RN 0.86: react-native-svg **15.15.5**, react-native-webview **14.0.1**, @react-native-community/netinfo **12.0.1**, @react-native-picker/picker **2.11.4**, @react-native-async-storage/async-storage **3.1.1**. |
| App red-box / Metro 500 `Unable to resolve module react-native/Libraries/Renderer/shims/ReactNative from react-native-gesture-handler/src/RNRenderer.ts` | **RNGH 2.31.x is JS-incompatible with RN 0.86** -- it imports a Renderer shim RN 0.86 removed | Upgrade to **RNGH 3.0.2** (still exports the imperative `PanGestureHandler`/`GestureHandlerRootView`/`State`, so `HorizontalPanArea.fs` needs no rewrite). |
| Web build: `Module not found` for `react-native-gesture-handler`/`@react-native-async-storage/async-storage`/reanimated after bumping | RNGH 3, async-storage 3, reanimated 4 ship **only `lib/module/`** (ESM) -- the webpack alias pointed at the gone `lib/commonjs/index.js` | Point the aliases in `Meta/LibFablePlus/webpack.config.js` at `lib/module/index.js`. |
| `npm install` ERESOLVE peer-dependency conflict during the RN bump | RN's strict peer deps + the `file:`-linked `react` | Use `npm install --legacy-peer-deps`. |
| Android build fails: missing `SoLoader`/`ReactNativeHost` symbols, or app never mounts after the 0.86 bump | `MainApplication.kt` still on the pre-0.86 `ReactNativeHost` + `SoLoader.init` model | Rewrite to the 0.86 model: `ReactApplication` with `reactHost` via `getDefaultReactHost(context, PackageList(this).packages)` + `loadReactNative(this)` in `onCreate` (no `SoLoader.init`, no `ReactNativeHost`). Native config: Gradle **9.3.1**, Kotlin **2.1.20**, SDK **36**, NDK **27.1.12297006**, `newArchEnabled=true`. |
| `input tap`/screencap shows a **black** frame while the app is clearly running (`fabric:true` in logcat) | The device screen timed out, or the notification shade is stuck focused, during long headless waits (a 14 KB byte-identical PNG = screen off) | `adb shell input keyevent KEYCODE_WAKEUP`, `adb shell cmd statusbar collapse`, then re-launch the activity and wait until `mCurrentFocus` names the app before `screencap`. |
| Fable: `error FABLE: Change declaration of member: <name>` on an imported reanimated/JS function | The import was declared as a **value of function type** (`let f: a -> b -> c = import ...`) -- Fable can't fix the arity | Declare imported functions **with explicit parameters**: `let f (x: a) (y: b) : c = import ...`. dotnet type-check does NOT catch this; run a real Fable compile. |
| Native red-box `Unable to resolve module moti` (or any package) from a `LibClient/src/.../*.js` seam file | Metro (native) resolves per-**app** `node_modules`; the package is only in `LibClient/node_modules`. A module-level `import "X" "pkg"` requires it at load even if unused | Either add the package to **every** app's `package.json` + the scaffold, or (better) don't make the seam depend on it. The `Rn.Reanimated` seam depends only on `react-native-reanimated` + `react-native-worklets` for this reason (Moti was dropped). |
| Native red-box at app root: *"Element type is invalid ... got `<GestureHandlerRootView />`. Did you accidentally export a JSX literal?"* after a **clean** `rm -rf .build/native/fable` + `build-native` | **Fable uncurried a component provider.** `registerComponent("RnApp", fun () -> rootComponent)` (where `rootComponent` is itself `fun _props -> el`) was uncurried to `(unit, props) => el`, so RN's `provider()` returned the *element*, not the component. (NOT an RNGH bug — a device probe showed `RNGH.GestureHandlerRootView` is a valid function. A stale `.build` had a non-uncurried version, so it only showed after a clean rebuild.) | **Fixed** (`RnPrimitives.setMainView`): box the component so the provider is `unit -> obj` (`fun () -> box rootComponent`), emitting `registerComponent("RnApp", () => _props => el)`. General rule: **box any `() -> function` passed across a JS interop boundary** or Fable may collapse the curry. |
| Native red-box *"[Worklets] Tried to synchronously call a Remote Function"* from a Reanimated `useAnimatedStyle` on the UI runtime | A Fable-emitted closure passed to `useAnimatedStyle` is **not** workletized by react-native-worklets/plugin, so it runs as a JS "remote function"; any call inside it (e.g. `createObj`) throws on the UI thread | Don't author worklets in F#. Use Reanimated **inline shared values**: embed the shared-value *object* in a plain style (`{ transform: [{ translateX: sharedValue }] }`) on a Reanimated animated View — it drives the prop on the UI thread with no worklet. This is how `Rn.Reanimated`'s `useAnimated*` helpers work. |
| Native crash `RCTImageView`: *"Value for uri cannot be cast from Double to String"* (release build; images that use a local `require`/`importDefault`) | `Rn.Image` wrapped a **native-imported local asset** (a numeric RN asset id) as `{uri: <number>}`; RN needs a local asset passed as the bare `source`, not under `uri` | Fixed in the framework: `ImageSource.RawNativeAsset` + `Rn.Image` pass the bare asset for `LocalNative` sources ({uri} only for web/URL/data). Only bites native release with local images (AppTodo uses URL images). |
| Native red-box / green flashing loop: *"ReferenceError: Property 'crypto' doesn't exist"* | Native has no global `crypto`; a route used `crypto` (e.g. UUID) with no polyfill | Add `react-native-get-random-values` and `import 'react-native-get-random-values'` as the **first** line of the app's `index.js` (before anything touches `crypto`). Add it to the app's `package.json` so it autolinks. |
| Release APK: `createBundleReleaseJsAndAssets` fails `Unable to resolve module <pkg>` for a LibClient dep (picker, async-storage, …) that debug ran fine | The **debug** APK loads JS from the metro dev server and never bundles; **release** runs `react-native bundle`, which must resolve every dep from the app's `metro.config.js`. LibClient-only deps aren't in the app's `node_modules` | Map the dep in the app `metro.config.js` `extraNodeModules` -> its LibClient path (a watchFolder), like svg/webview. Verify with a **release** build, not debug. |
