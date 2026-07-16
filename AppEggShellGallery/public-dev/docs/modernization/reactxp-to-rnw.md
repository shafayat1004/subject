# ReactXP to react-native-web

Why EggShell migrated away from `@chaldal/reactxp`, what the fork audit found, what the modern
React Native / react-native-web ecosystem offers, and how the migration was designed to keep every
F# component untouched.

**Status (session 8 -- done + modernized):** the migration is complete and the ReactXP *name* is
retired. `@chaldal/reactxp` is gone, and the seam is now `LibClient/src/Rn/` (namespace **`Rn`**,
constructor prefix **`Rn.`**, imports in `RnPrimitives.fs`). The stack was upgraded to **React 19.2,
react-native 0.86, react-native-web 0.21.2, RNGH 3, with the New Architecture (Fabric/TurboModules/
Bridgeless) enabled** and verified on device (AppTodo: `Running "RnApp" with {"fabric":true}`).
Reanimated 4 + worklets + Moti are installed and wired (declarative-only from F#; see §11). This page
remains the strategy/research narrative; anywhere below that says "ReactXP fork" / `RX.` / `RNSeam`
describes the pre-migration state -- read it as history. Remaining tail: gallery +
PerformancePlayground native config, and adopting Reanimated in components. For the exact upgrade
recipe + gotchas see the session-8 [Engineering Log](../knowledge-base/engineering-log.md) entry; for
phase status see [Phased Plan](./phased-plan.md) Phase 4.

---

## 1. The three moving pieces (keep them apart)

1. **React** is the UI library (components, state, hooks). Versions 18 then 19. Platform agnostic.
2. **React Native (RN)** is the runtime that turns React components into real native iOS and Android
   widgets, plus the machinery that lets JavaScript talk to native code. The "New Architecture" is a
   rewrite of this machinery.
3. **react-native-web (RNW)** is a separate library that re-implements React Native's components for
   the browser, rendering to HTML and CSS via React DOM. It is the web sibling of RN-iOS and
   RN-Android.

**ReactXP** is a fourth layer that sits on top of React + RN + RNW. It exposes only the "lowest
common denominator," meaning the small set of components and props that behave identically on every
platform. Microsoft built it for Skype around 2017, then archived it. Its own FAQ states: "ReactXP is
a layer that sits on top of React Native, whereas React Native for Web is a parallel implementation
of React Native, a sibling to React Native for iOS and Android."

That "layer on top of a layer" is the strategic problem. The industry settled on the
parallel-implementation approach (RNW), and the extra abstraction no longer earns its keep.

---

## 2. The fork audit

EggShell's frontend rendered through `@chaldal/reactxp` (a fork of Microsoft's archived ReactXP) for
years. The audit below is what motivated migrating off it.

| Dimension | Reality |
|-----------|---------|
| Version | `@chaldal/reactxp` 2.2.0 |
| Last commit | 2024-01-11 ("reactNative0.73.1"), idle roughly 2.5 years |
| Declares | React `>=18.1`, react-dom 18.2, React Native 0.73.2 |
| Type-checks against | `@types/react-native@0.70.4`, already three minors behind what it claims |
| Code style | 100% class components, zero hooks, zero function components |
| Tests | None |
| CI | Dead (`azure-pipelines.yml` pins Ubuntu 16.04 and Node 8, both end-of-life) |
| Native modules | None (no `requireNativeComponent`); native blast radius is smaller because of this |
| Web rendering | A hand-rolled DOM renderer that does NOT use react-native-web. Its own `<div>` output, inline-style CSS-in-JS, and its own animation engine. |

The last row is the important one. On the web, this fork is not built on RNW at all. It is a fully
bespoke renderer plus an inline-style engine plus its own animation engine. EggShell therefore
maintains an entire parallel web UI engine today, not a thin shim.

**EggShell's wrapper seam** lives in `LibClient/src/ReactXP/`:

- `ReactXPBindings.fs`: the single `import "@chaldal/reactxp"` seam.
- `Components/{View, Text, Button, ScrollView, TextInput, Image, GestureView, Picker, Link,
  VirtualListView, WebView, Animatable*}`: the wrapped primitives.
- `Styles/{Legacy, New}` plus `Styles/Animation.fs`: the F# style DSL that compiles to runtime style
  objects.
- `SVG.fs`, `NetInfo.fs`, `Types.fs`, `ComponentsHierarchy.fs`.

Everything above this seam (LibClient components, LibUi*, the apps, `makeViewStyles` call sites,
LibRouter, the subscription and `AsyncData` data model) calls `RX.*` / `LC.*` and is insulated from a
seam swap. That insulation is the lever the whole migration pivots on.

---

## 3. How far behind the fork is

| Axis | Fork is at | Current (mid-2026) | Gap |
|------|-----------|---------------------|-----|
| React Native | 0.73 | 0.86 | 13 minor versions |
| React | 18 | 19.2 | a major version |
| RN architecture | Legacy "Bridge" | New Architecture, default since 0.76; Legacy frozen since 0.80 | on the dead side of the line |
| ReactXP upstream | a fork of it | Microsoft archived it; README says end-of-life, use react-native-web | the upstream is abandoned |
| Animation stack | RN's old `Animated` (2018-era) | Reanimated 4, RNGH 3, Moti, Skia, Motion | a full generation behind |

Two milestones matter most:

- **RN 0.76 (Oct 2024) made the New Architecture the default** and declared it production ready.
- **RN 0.78 (Feb 2025) shipped React 19.** Any move onto a current RN or Expo lands on React 19.

---

## 4. The three structural blockers

These are the concrete reasons the fork cannot simply be nudged forward.

### Blocker A: React 19 removed APIs the fork depends on

| Removed in React 19 | Occurrences in fork | Where |
|---------------------|---------------------|-------|
| Legacy class context (`childContextTypes` / `getChildContext` / `contextTypes`) | ~56 sites across 7 component families | View, Text, Button, RootView, Link, Image, TextInput |
| `findDOMNode` (web) | 21 sites | View, ScrollView, GestureView, Animated, the list-animation engine |
| `propTypes` | 11 imports | only used to wire legacy context |

Legacy context is the spine of how the fork's web components share data internally. Porting to modern
`React.createContext` plus removing `findDOMNode` is a real rewrite of the web layer, not a
find-and-replace.

### Blocker B: New Architecture incompatibilities (native side)

The fork uses `setNativeProps` (39 sites), `findNodeHandle` (13), and direct `UIManager` bridge
measurement calls (6). These are the legacy-bridge surface that Fabric changes or drops. A temporary
interop shim covers some of it today but is fragile and slated for eventual removal. Mitigating
factor: the fork defines no custom native view managers, so the native blast radius is smaller than
the web side.

### Blocker C: possibly already broken on RN 0.76.5

The fork calls `Dimensions.addEventListener` (in `native-common/UserInterface.tsx`), which was
**removed in RN 0.76**. The gallery app reportedly runs RN 0.76.5, and the [Engineering Log](../knowledge-base/engineering-log.md) documents a
flood of ReactXP legacy-context warnings in logcat (which corroborates Blocker A being live). Whether
the `Dimensions` path is crashing, shimmed, or simply not hit on the current build is an action item
to verify directly.

---

## 5. React Native's New Architecture

RN's New Architecture rewrites the systems underneath RN: how components render, how JavaScript talks
to native, and how work is scheduled across threads. Four pillars:

- **JSI (JavaScript Interface):** replaces the asynchronous JSON bridge with a thin C++ layer that
  lets JavaScript hold a direct reference to a native object and call it synchronously. Two offices
  that passed paper notes through a mail room now share a desk.
- **Fabric (the new renderer):** written in C++, shared across platforms, stores the view tree
  immutably (thread-safe), supports multiple in-progress trees, and allows synchronous layout reads.
  Unlocks React concurrent features (Suspense, Transitions) on native.
- **TurboModules:** native modules that load lazily on first use, communicate synchronously, and are
  type-safe across the boundary.
- **Codegen:** build-time type-safe glue generation between JS and native.

**Timeline:** New Architecture default (and production-ready) in RN 0.76 (Oct 2024). Legacy
Architecture frozen in RN 0.80 (Jun 2025).

**What this means for old libraries:** old-architecture libraries run through an interop layer in New
Arch mode. It works for many apps but does not support custom Shadow Nodes, and does not guarantee
`setNativeProps` or `measure*` through it. Full legacy removal is planned but has no committed date.

---

## 6. React 18 to React 19

React 19 shipped stable Dec 5, 2024. New features include: Actions (async transitions that manage
pending/optimistic/error state automatically), `useActionState`, `useOptimistic`, `useFormStatus`,
`use()` (read a promise or context during render), ref as a regular prop (forwardRef becomes
unnecessary), `<Context>` usable directly as a provider, automatic document-metadata hoisting, and the
opt-in React Compiler (auto-memoization, reached v1.0 stable Oct 2025).

**Removals that matter for EggShell:**

| Item | React 19 status | Breaks old code? |
|------|-----------------|------------------|
| Legacy class context | Removed | Yes, silently loses context |
| `findDOMNode` | Removed | Yes |
| `ReactDOM.render` | Removed | Yes, throws |
| `propTypes` | Removed (ignored) | Silently inert |
| `defaultProps` on function components | Removed (ignored) | Silently |
| `UNSAFE_componentWill*` lifecycles | Still supported | No |

The ReactXP fork is exposed on legacy class context and `findDOMNode` (Blocker A above).

---

## 7. react-native-web: what it is and its status

RNW re-implements React Native's components for the browser, rendering to HTML and CSS via React DOM.
It provides RN's primitives (`View`, `Text`, `Image`, `TextInput`, `ScrollView`, `StyleSheet`) for the
browser, so the same component code that produces native widgets on phones produces `<div>` / `<span>`
plus CSS in a browser. It powers the X/Twitter website.

**Maintenance status:** RNW is maintained by Nicolas Gallagher and is effectively in **maintenance
mode** as of late 2025. He will keep reviewing PRs and merging fixes but does not expect major
development; his focus has shifted to React Strict DOM. Latest published version: 0.21.2 (Oct 2024).
RNW is stable and production-proven, making it a safe current target. Treat it as
stable-not-advancing and watch React Strict DOM as the likely long-term successor for the web seam.

**Known limitations to plan around:** `Animated` has no native driver on web; `KeyboardAvoidingView`,
`StatusBar`, `LayoutAnimation`, `NativeModules` are mocked or partial; `RefreshControl`,
`TouchableNativeFeedback`, `Alert` are not implemented; `Text` lacks `onLongPress`; `TextInput` lacks
rich text.

**ReactXP archival (confirmed):** Microsoft's ReactXP repository is archived and read-only. Last code
push 2023-07-18; last npm release `reactxp` 2.0.0 in 2019. The README states: "The ReactXP library
is no longer being maintained and is considered end of life. We recommend alternatives such as React
Native for Web."

---

## 8. The modern animation and gesture stack

The core concept is the **two-thread model**: an RN app runs logic (JavaScript) on a JS thread, while
drawing happens on a separate UI thread. If animation is computed on the JS thread and that thread is
busy, the animation stutters (jank). The modern stack exists to move animation and gesture work onto
the UI thread so motion stays smooth regardless of JS load.

- **React Native Reanimated (v4):** runs animations on the UI thread via worklets (small JS functions
  moved to the UI thread by a Babel plugin). Provides `useSharedValue`, `withTiming`, `withSpring`
  (physics), `withDecay`, declarative Layout Animations (auto enter/exit/reorder), and experimental
  Shared Element Transitions. Reanimated 4 **requires the New Architecture**; v3 is the legacy line.
  v4 added a declarative CSS-compatible animation API as an alternative to hand-written worklets.
- **React Native Gesture Handler (RNGH, v3):** recognizes gestures on the UI thread using each
  platform's native gesture APIs. The `Gesture.Pan()` / `Gesture.Tap()` builder API pairs with
  Reanimated so a finger drag drives an animation without touching the JS thread.
- **Moti (declarative, on top of Reanimated):** pass `from` / `animate` / `exit` style objects as
  props (Framer-Motion-style), dramatically less code. Works on web via RNW.
- **React Native Skia (Shopify):** GPU 2D graphics / canvas engine for custom drawing, shaders, and
  effects. Works on web via WebAssembly.
- **Motion (formerly Framer Motion):** runs animations via the browser's Web Animations API and
  ScrollTimeline for high-framerate web animation.

**Capability gap vs ReactXP fork:**

| Capability | Modern stack | ReactXP fork | Gap |
|------------|-------------|--------------|-----|
| Animations on the UI thread | Default (Reanimated worklets) | JS-thread only; native driver off | Major |
| Spring / physics motion | `withSpring`, `withDecay` | timing/easing only | Major |
| Gesture-driven animation (UI thread) | RNGH + worklets | `PanResponder` on JS thread | Major |
| Layout animations (auto enter/exit/reorder) | Reanimated Layout Animations; Moti | none | Major |
| Shared-element / route transitions | Reanimated (experimental, native only) | none | Major |
| 120Hz high-refresh | targets display refresh | capped by JS-thread jank | Significant |
| GPU 2D graphics / shaders | React Native Skia | none | Major (categorical) |
| Rich web animation | Motion (compositor-driven) | hand-rolled CSS-transition engine | Significant |

The crucial dependency: Reanimated 4, the standard engine, **requires the New Architecture**. The
"real animation story" goal is therefore gated on the New Architecture migration regardless of the
ReactXP decision, and ReactXP's model is on the wrong side of that line.

---

## 9. Capability gap summary

A 2018-era layer built on RN's old `Animated` + `PanResponder`, with the native driver off (and a
no-op on its web renderer), lacks everything in the table above. This is not a minor version gap; it
is a full generational gap in what a modern cross-platform UI framework can do.

---

## 10. The transition design: re-point the seam, keep the surface

**One-sentence shape:** re-implement `LibClient/src/ReactXP` against React Native (native) +
react-native-web (web) instead of `@chaldal/reactxp`, keeping the F# constructor surface and style
DSL identical, so EggShell component code and the "feel" survive untouched.

```
        TODAY                                      AFTER
   F# components (LC.View, LC.Text, ...)       F# components            <- UNCHANGED
   makeViewStyles { ... }  (style DSL)         makeViewStyles { ... }   <- UNCHANGED
   LibRouter (typed F# routes)                 LibRouter                <- UNCHANGED
        |                                           |
   +----+----------------------+               +----+---------------------+
   | LibClient/src/ReactXP      |   <- THE      | LibClient/src/ReactXP     |  <- rewritten
   |  ReactXPBindings.fs         |     SEAM      |  RnBindings.fs            |
   |  Components/*  Styles/*      |  (only this  |  Components/*  Styles/*    |
   +----+----------------------+     changes)   +----+---------------------+
        |                                           |
   @chaldal/reactxp 2.2.0                       react-native (native) +
   (EOL, legacy bridge, React 18)               react-native-web (web), New Arch, React 19
```

Why this is contained: ReactXP and RN share the same conceptual primitives (`View`, `Text`,
`ScrollView`, `Image`, `TextInput`). ReactXP was deliberately a curated subset of RN. Translating
that small, well-understood vocabulary is what the migration is.

**What stays identical:**

- Component call sites: `LC.View(...)`, `LC.Text(...)`, the `[<Component>]` static-member form.
- The style DSL: `makeViewStyles`, `makeTextStyles`, `module private Styles` / `FooStyles` conventions.
  What changes is what the CE emits (ReactXP runtime objects to RN style objects), not how you write it.
- `LibRouter`: web keeps `react-router-dom`; native keeps `react-router-native`.
- The data model: subscription-driven `AsyncData<'T>`, SignalR push, `EntityService`.
- F#-only, shared types: application code stays 100% F#.

**What changes underneath:**

| Layer | Today (ReactXP) | After (RN + RNW) | Effort |
|-------|-----------------|------------------|--------|
| Bindings | `import "@chaldal/reactxp"` | `import "react-native"`; bundler aliases to `react-native-web` for the web build | Low |
| Primitives | `RX.View/Text/Button/...` wrap ReactXP classes | Same F# signatures, wrap RN `View/Text/Pressable/ScrollView/Image/TextInput` | Med (mechanical, per-component) |
| Press/tap | `GestureView`/`TapCapture` on `PanResponder` | RN `Pressable` + RNGH for real gestures (partly done: `LC.Pressable`) | Low-Med |
| Styles | `Styles/{Legacy,New}` emit ReactXP runtime objects | Same CE surface, emit RN style objects | Med |
| Animation | `Styles/Animation.fs` + `Animatable*` over old `Animated` | Reanimated 4 + RNGH 3 (native) and Moti (declarative, web via RNW); Motion for rich web | Med-High (new capability) |
| SVG / NetInfo / WebView / VirtualList | ReactXP extensions | `react-native-svg`, `@react-native-community/netinfo`, `react-native-webview`, RN `FlatList` | Low-Med |
| Toolchain / runtime | RN legacy bridge, React 18 | RN New Architecture (Fabric/JSI/TurboModules), React 19 | Med (rides the platform move) |

The web side gets simpler: today the fork ships a bespoke DOM renderer + inline-style engine + its
own animation engine. RNW replaces all of that with a maintained, production-proven implementation.
Net deletion of code.

---

## 11. F# / Fable specific risks

### Risk 1: Reanimated worklets from F# (RESOLVED)

Reanimated runs animations on the UI thread via worklets. Fable emits JS, then Metro/Babel runs. The
question was whether the Babel plugin recognizes Fable-generated function shapes as worklets.

**Measured result (dev build, 2026-06-29, Expo 56 / RN 0.85 / Reanimated 4.3.1 / Fable 5.4.0,
Android):**

- A `runOnJS` call inside a Fable-emitted worklet **aborts the worklets runtime** (`Fatal signal 6
  SIGABRT` at `jsi::Function::getHostFunction`).
- The simple `useAnimatedStyle (fun () -> opacity = sv.value)` **runs on the JS thread, not the UI
  thread**. Proof: blocking the JS thread for 3 seconds froze the animation at its in-progress value
  (counter still `0`, box still opacity 0.3); only after JS resumed did it move. A genuine UI-thread
  worklet keeps animating while JS is blocked.

**Root cause:** compiled F# depends on FSharp.Core helpers (`List.map`, `sprintf`, structural
equality, option helpers) that do not exist in the isolated worklet runtime. The worklet runtime is a
sealed clean room; a plain-JS worklet only uses basic tools. An F#-compiled worklet keeps reaching for
`List.map` which does not exist there.

**Framework rule:** declarative animation (Moti / Reanimated declarative API) from F#; any genuine
UI-thread worklet authored in a tiny vetted JS shim (~10 to 20 lines, framework-owned) via the
`ThirdParty` / `TypesJs.fs` recipe. F# stays declarative; the worklet lives in a small JS file owned
by the framework, never by app code.

**Could we add a `[<Worklet>]` Fable plugin?** Feasible in-house (same mechanism as `[<Component>]`),
but the FSharp.Core ceiling still caps it to a primitive subset (arithmetic, comparisons, simple
if/then, reading shared values). ROI is low: the high-value cases (gesture/frame math) are already
cheap as small JS shims, and the high-value compute cases (camera ML, big aggregation) are better
served by native modules, the Orleans backend, or Web Workers on web.

### Risk 2: binding Reanimated/RNGH hooks in F#

These are hooks-based. EggShell already uses Fable hooks in the `[<Component>]` path, so this is
"write binding files," not new ground.

### Risk 3: style CE retargeting

RN takes plain style objects in `style={[...]}` arrays with last-wins merging. Mostly mechanical.
ReactXP's nested `&&` / `=>` selectors and responsive breakpoints have no native RN equivalent; keep
them web-only (RNW supports some), handle native variants via F# branching.

### Risk 4: "as close to native" is delivered by RN, not RNW

RNW is the web target (real DOM). True native on iOS/Android comes from RN's New Architecture
(Fabric renders real native views, JSI gives synchronous native calls). Strictly better than
ReactXP's lowest common denominator.

---

## 12. How each goal is delivered

| Goal | Mechanism in the new stack |
|------|---------------------------|
| Performance | New Architecture: JSI (no async bridge), Fabric (concurrent, thread-safe renderer, synchronous layout). Reanimated/RNGH run animation and gestures on the UI thread, so no jank when JS is busy. 120Hz-capable. |
| Efficiency | Faster startup (no bridge init, lazy TurboModules), Metro faster warm builds and resolver, smaller Android binaries. RNW's atomic CSS beats the fork's inline-style engine. |
| Modern features | Layout / enter / exit / shared-element transitions, scroll-linked effects, spring/physics, React 19 (Actions, `use()`, concurrent features via Fabric). |
| Modern libraries | Unlocks the ecosystem currently out of reach: Reanimated 4, RNGH 3, Moti, React Native Skia, Motion on web. |
| As close to native | Fabric = real native views + synchronous native calls on iOS/Android; RNW = faithful RN on web. Curated F# wrapper keeps it type-safe. |
| F#-only FE/BE/shared types | Preserved. App code stays 100% F#. Only the framework's wrapper bindings plus a few small vetted JS animation shims are JS, following the existing `ThirdParty` pattern. |

---

## 13. Decision summary

- **Keeping `@chaldal/reactxp` current is not viable as a strategy.** It would require rewriting the
  web layer for React 19 (legacy context + `findDOMNode`) and the native layer for the New
  Architecture (`setNativeProps` / `findNodeHandle` / `UIManager`), with no tests and dead CI, only
  to arrive at 2018-era feature parity that still cannot use the modern libraries. Microsoft has
  declared ReactXP end-of-life and points at react-native-web.
- **The strategic move is migrating the `LibClient/src/ReactXP` seam to RN + react-native-web.** The
  seam is contained, the F# surface above it stays identical, and it unlocks the New Architecture,
  React 19, and the entire modern animation/gesture/graphics ecosystem.
- **The animation goal is gated on the New Architecture regardless**, because Reanimated 4 requires
  it. So the platform baseline sequences before the seam swap.
- **Greenfield-only means zero migration debt**, making this the cheapest possible version of the work.
- **One caveat to carry:** RNW is in maintenance mode (latest 0.21.2; author moved to React Strict
  DOM). It is a safe current target; swapping the web target again later (for example to React Strict
  DOM) would be the same bounded exercise, not another full rewrite.

---

## 14. React Strict DOM: the web-target forward look

React Strict DOM (RSD) is Meta's successor effort to RNW, from the same author (Nicolas Gallagher).
It inverts RNW's direction: RNW says "write React Native, run it on web"; RSD says "write web
standards (`html.div`, `html.span`, plus CSS through StyleX), run it on native too."

**Maturity (mid-2026):** version 0.0.55, officially experimental, pre-1.0. Production use on web at
Meta (facebook.com). Native target is roughly 60% of the API; no flow/grid/inline layout on native
yet. RNW is a separate codebase (not merged into RSD); it is frozen at 0.21.2.

**Decision for the seam:** keep RNW as the runtime target today and shape the F# public API toward
web-standard idioms (DOM-flavored elements, `onClick`-style events, `aria-*` accessibility) rather
than leaking RNW-specific idioms above the seam. Re-evaluate RSD when it hits two milestones: a
stable 1.0 with an API-stability commitment, and the native layout gaps closing. The contained-seam
design means re-pointing again later is a bounded job, not another rewrite.

---

## 15. Non-React F# alternatives (considered and declined)

For completeness: Avalonia + FuncUI is the strongest all-F# native path (actively maintained, good
desktop story), but its web target is Skia-rendered canvas (WASM), which loses real-DOM web
accessibility and SEO, and the in-process type-sharing prize (no JSON wire) does not survive the web
target (browsers cannot open raw TCP sockets, forcing a gateway anyway). MAUI / Fabulous is F#-second-class
and partly on-hold. Bolero (Blazor) is excellent F# but web-only.

**Bottom line:** stay on F# + Fable + RN/RNW, heavy work on .NET/Orleans. Revisit Avalonia + FuncUI
only if "all-.NET, no-JS" becomes a hard strategic requirement or the product turns desktop-first.

The full non-React-alternatives survey (Avalonia, Flutter, and similar) is condensed above.
