# EggShell Frontend Modernization: ReactXP to React Native for Web

> **Living document.** Started 2026-06-28. This captures the audit of the `@chaldal/reactxp`
> fork, the research into modern React Native / React / react-native-web, the animation and gesture
> ecosystem, and the proposed transition design for the EggShell F# frontend. We update this file as
> we learn more (see the Update Log at the bottom). It is intentionally self-contained so it can
> outlive `EGGSHELL_ARCHITECTURE.md` once that file is retired.

**Author context:** the maintainer is an F# developer who is not deeply familiar with React web or
React Native internals, so concepts are explained from first principles.

**Hard non-negotiable for everything below:** the frontend, backend, and shared types stay
**F#-only** (Fable on the frontend). No JavaScript or TypeScript in application code. The only JS the
framework itself owns is a small set of vetted interop shims, exactly like the existing `ThirdParty`
wrapper pattern.

**Goals driving this initiative:** performance, efficiency, modern features, access to modern
libraries, and "as close to native as possible," while keeping the existing F# component feel and
familiarity.

---

## Table of Contents

1. [Orientation: the moving pieces](#1-orientation-the-moving-pieces)
2. [What ReactXP is, and the fork's exact state](#2-what-reactxp-is-and-the-forks-exact-state)
3. [How far behind the fork is](#3-how-far-behind-the-fork-is)
4. [The three structural blockers](#4-the-three-structural-blockers)
5. [Modern React Native: the New Architecture](#5-modern-react-native-the-new-architecture)
6. [React 18 to React 19](#6-react-18-to-react-19)
7. [react-native-web: what it is and its status](#7-react-native-web-what-it-is-and-its-status)
8. [The modern animation / gesture / graphics stack](#8-the-modern-animation--gesture--graphics-stack)
9. [Capability gap: ReactXP vs the modern stack](#9-capability-gap-reactxp-vs-the-modern-stack)
10. [The transition design: re-point the seam, keep the surface](#10-the-transition-design-re-point-the-seam-keep-the-surface)
11. [F# / Fable specific risks](#11-f--fable-specific-risks)
12. [How each goal is delivered](#12-how-each-goal-is-delivered)
13. [Sequencing and the phased plan](#13-sequencing-and-the-phased-plan)
14. [The de-risking spike](#14-the-de-risking-spike)
15. [Decision summary](#15-decision-summary)
16. [React Strict DOM and the web-target choice](#16-react-strict-dom-and-the-web-target-choice)
17. [Fable 4 vs 5, .NET 10, and decoupling from Orleans](#17-fable-4-vs-5-net-10-and-decoupling-from-orleans)
18. [Non-React F# alternatives (food for thought)](#18-non-react-f-alternatives-food-for-thought)
19. [Spike status and the confirmed Fable 5 / .NET 10 gate](#19-spike-status-and-the-confirmed-fable-5--net-10-gate)
20. [Sources](#20-sources)
21. [Update Log](#21-update-log)

---

## 1. Orientation: the moving pieces

Three separate things get conflated constantly. Keep them apart:

1. **React** is the UI library (components, state, hooks). Versions 18 then 19. Platform agnostic.
2. **React Native (RN)** is the runtime that turns React components into real native iOS and Android
   widgets, plus the machinery that lets JavaScript talk to native code. Versions 0.73 through 0.86.
   The "New Architecture" is a rewrite of *this machinery*.
3. **react-native-web (RNW)** is a separate library that re-implements React Native's components *for
   the browser*, rendering to HTML and CSS via React DOM. It is the web sibling of RN-iOS and
   RN-Android.

**ReactXP** is a *fourth* layer that sits **on top of** React + RN + RNW. It exposes only the
"lowest common denominator," meaning the small set of components and props that behave identically on
every platform. Microsoft built it for Skype around 2017. Its own FAQ states the distinction plainly:
"ReactXP is a layer that sits on top of React Native, whereas React Native for Web is a parallel
implementation of React Native, a sibling to React Native for iOS and Android."

That "layer on top of a layer" is the strategic problem. The industry settled on the
parallel-implementation approach (RNW), so the extra abstraction no longer earns its keep.

---

## 2. What ReactXP is, and the fork's exact state

EggShell's frontend renders through `@chaldal/reactxp`, a fork of Microsoft's now-archived ReactXP.
A read-only analysis of the fork (cloned to `/Volumes/HomeX/shafayat/Code/reactxp-fork`) found:

| Dimension | Reality |
|---|---|
| Version | `@chaldal/reactxp` **2.2.0** |
| Last commit | **2024-01-11** ("reactNative0.73.1"), idle roughly 2.5 years |
| Declares | React `>=18.1`, react-dom `18.2`, **React Native `0.73.2`** |
| Type-checks against | **`@types/react-native@0.70.4`**, already three minors behind what it claims |
| Code style | **100% class components, zero hooks, zero function components** |
| Tests | **None** (no test files, no runner) |
| CI | **Dead** (`azure-pipelines.yml` pins Ubuntu 16.04 and Node 8, both end-of-life) |
| Native modules of its own | **None** (no `requireNativeComponent`), which keeps the native blast radius small |
| Web rendering | **A hand-rolled DOM renderer that does NOT use react-native-web.** Its own `<div>` output, inline-style CSS-in-JS, and its own animation engine |

The last row is the surprising and important one. On the web, this fork is not built on RNW at all.
It is a fully bespoke renderer plus an inline-style engine plus its own animation engine. So today the
framework maintains an entire parallel web UI engine, not a thin shim.

**EggShell's own wrapper seam** lives in `LibClient/src/ReactXP/`:

- `ReactXPBindings.fs`, the single `import "@chaldal/reactxp"` seam
- `Components/{View, Text, Button, ScrollView, TextInput, Image, GestureView, Picker, Link,
  VirtualListView, WebView, Animatable*}`, the wrapped primitives
- `Styles/{Legacy, New}` plus `Styles/Animation.fs`, the F# style DSL that compiles to runtime style
  objects
- `SVG.fs`, `NetInfo.fs`, `Types.fs`, `ComponentsHierarchy.fs`

Everything *above* this seam (the `LibClient` components, `LibUi*`, the apps, the `makeViewStyles`
call sites, `LibRouter`, the subscription and `AsyncData` data model) calls `RX.*` / `LC.*` and is
insulated from a seam swap. That insulation is the lever the whole migration pivots on.

---

## 3. How far behind the fork is

| Axis | Fork is at | Current (mid-2026) | Gap |
|---|---|---|---|
| React Native | 0.73 | **0.86** | 13 minor versions |
| React | 18 | **19.2** | a major version |
| RN architecture | Legacy "Bridge" | **New Architecture, default since 0.76**, Legacy frozen since 0.80 | on the dead side of the line |
| ReactXP upstream | a fork of it | **Microsoft archived it; README says end-of-life, use react-native-web** | the upstream is abandoned |
| Animation stack | RN's old `Animated` (2018-era) | Reanimated 4, RNGH 3, Moti, Skia, Motion | a full generation behind |

Two milestones matter most:

- **RN 0.76 (Oct 2024) made the New Architecture the default** and declared it production ready.
- **RN 0.78 (Feb 2025) shipped React 19.** Any move onto a current RN or Expo lands you on React 19.

---

## 4. The three structural blockers

These are the concrete reasons the fork cannot simply be nudged forward.

### Blocker A: React 19 *removed* APIs the fork depends on

These do not warn-then-work. They break or silently no-op.

| Removed in React 19 | Occurrences in fork | Where |
|---|---|---|
| **Legacy class context** (`childContextTypes` / `getChildContext` / `contextTypes`) | **~56 sites across 7 component families** | View, Text, Button, RootView, Link, Image, TextInput |
| **`findDOMNode`** (web) | **21 sites** | View, ScrollView, GestureView, Animated, the list-animation engine |
| `propTypes` | 11 imports | only used to wire legacy context |
| `ReactDOM.render`, string refs | not used / already migrated | fork is clean here |

Legacy context is the spine of how the fork's web components share data internally. Porting it to
modern `React.createContext` plus removing `findDOMNode` (used by the measurement and the FLIP
list-animation code) is a real rewrite of the web layer, not a find-and-replace. It is mandatory for
React 19, and you cannot stay on React 18 forever because RN 0.78+ requires React 19.

### Blocker B: New Architecture incompatibilities (native side)

The fork uses `setNativeProps` (39 sites), `findNodeHandle` (13), and direct `UIManager` bridge
measurement calls (6). These are exactly the legacy-bridge surface that Fabric changes or drops. A
temporary interop shim covers some of it today, but it is fragile and slated for eventual removal.
Mitigating factor: the fork defines no custom native view managers, so the native blast radius is
smaller than the web side.

### Blocker C: it may already be subtly broken on RN 0.76.5

The fork calls `Dimensions.addEventListener` (in `native-common/UserInterface.tsx`), which was
**removed in RN 0.76**. The gallery app reportedly runs RN 0.76.5, and `LEARNINGS.md` documents a
flood of ReactXP legacy-context warnings in logcat (which corroborates Blocker A being live).
**Action item to verify directly:** whether the `Dimensions` path is crashing, shimmed, or simply not
hit on the current build.

---

## 5. Modern React Native: the New Architecture

React Native's "New Architecture" is a rewrite of the systems underneath RN: how components render,
how JavaScript talks to native, and how work is scheduled across threads. It has four pillars plus an
operating mode.

- **The Bridge to JSI shift (the foundation).** The *old* way ran JavaScript and native code in
  separate worlds that exchanged JSON messages over an asynchronous "Bridge" (slow, serialized,
  no synchronous returns). **JSI (JavaScript Interface)** is a thin C++ layer that lets JavaScript
  hold a direct reference to a native object and call it synchronously, with no serialization. Think
  of two offices that passed paper notes through a mail room now sharing a desk.
- **Fabric (the new renderer).** A renderer turns your component tree into on-screen views. Fabric is
  written in C++, shared across platforms, stores the view tree immutably (thread-safe), supports
  multiple in-progress trees, and allows synchronous layout reads. It unlocks React's concurrent
  features (Suspense, Transitions) on native and removes layout "jump" glitches.
- **TurboModules (the new native module system).** Native modules (camera, filesystem, etc.) now load
  lazily on first use, communicate synchronously, are type-safe across the boundary, and can be shared
  in C++ across iOS, Android, Windows, macOS.
- **Codegen.** A build-time tool that reads typed "spec" files and generates the safe glue between JS
  and native, enforcing a strong contract and removing a common class of crashes.
- **Bridgeless mode.** The operating mode in which the Bridge is never created. Experimental opt-in in
  RN 0.73, default (when the New Architecture is enabled) in RN 0.74.

**Timeline of the default:** experimental opt-in in RN 0.68, then **default for all projects and
production-ready in RN 0.76 (Oct 23, 2024)**.

**What this means for old libraries.** When the New Architecture is on, old-architecture libraries run
through an automatic interop (compatibility) layer. It works for many apps but explicitly does **not**
support custom Shadow Nodes, and does not guarantee `setNativeProps` or `measure*` through it, and
does not expose concurrent features to unmigrated components. **RN 0.80 (Jun 2025) officially froze
the Legacy Architecture** (no new fixes or features) and added in-app warnings for non-New-Arch APIs.
Full removal of the legacy path is planned "in the future" with no committed date.

**RN release timeline (condensed):**

| Version | Date | React | Headline |
|---|---|---|---|
| 0.73 | Dec 2023 | 18.x | New JS debugger; stable symlinks; bridgeless experimental opt-in |
| 0.74 | Apr 2024 | 18.x | Yoga 3.0; bridgeless default when New Arch enabled |
| 0.75 | Aug 2024 | 18.x | Yoga 3.1; New Arch stabilization |
| **0.76** | **Oct 2024** | 18.3.1 | **New Architecture ON BY DEFAULT (production ready)**; React Native DevTools; Metro ~4x faster warm builds |
| 0.77 | Jan 2025 | 18.3.x | New web-aligned styling props; Android 16 KB pages; Swift template |
| **0.78** | **Feb 2025** | **React 19** | **First RN to ship React 19**; React Compiler support |
| 0.79 | Apr 2025 | 19.x | Much faster Metro startup |
| 0.80 | Jun 2025 | 19.1 | **Legacy Architecture frozen**; deep imports deprecated |
| 0.81 | Aug 2025 | 19.x | Android 16 target, edge-to-edge; experimental precompiled iOS builds |
| 0.83 | Dec 2025 | 19.2 | First release with no user-facing breaking changes (pairs with Expo SDK 55) |
| **0.86** | **Jun 2026** | 19.x | **Latest stable as of this writing.** Repo moved from the `facebook` to the `react` GitHub org |

**The modern default stack (2026):** the RN team now officially recommends a framework and names
**Expo**. The consensus is **Expo + Expo Router + react-native-web + Metro + EAS** (Expo's cloud
build service). Current stable is **Expo SDK 55 (RN 0.83 / React 19.2)**.

> Note on Expo Router: it is file-based and JS-centric, which would fight EggShell's typed F# routing
> in `LibRouter`. The plan keeps `LibRouter` (react-router under the hood) rather than adopting Expo
> Router, preserving the F#-typed-routes non-negotiable. Expo is still useful for the build toolchain
> (prebuild, EAS) even without Expo Router.

---

## 6. React 18 to React 19

React 19 shipped stable Dec 5, 2024.

**New features (accessible summary):** Actions (async transitions that manage pending/optimistic/error
state automatically), `useActionState`, `useOptimistic`, `useFormStatus`, function-as-form-action,
`use()` (read a promise or context during render, may be conditional), **ref as a regular prop**
(forwardRef becomes unnecessary, though it still works), ref cleanup functions, `<Context>` usable
directly as a provider, automatic document-metadata hoisting, and the opt-in **React Compiler**
(auto-memoization; reached v1.0 stable Oct 2025, separate from the runtime).

**Removals that break old libraries:**

| Item | React 19 status | Breaks old code? |
|---|---|---|
| Legacy class context (`childContextTypes` / `getChildContext` / `contextTypes`) | **Removed** | **Yes, silently loses context** |
| String refs (`ref="foo"`) | **Removed** | **Yes** |
| `propTypes` | **Removed** (ignored) | Silently inert |
| `defaultProps` on **function** components | **Removed** (ignored) | Silently |
| `defaultProps` on **class** components | Still supported | No |
| `ReactDOM.render` / `hydrate` / `unmountComponentAtNode` | **Removed** | **Yes, throws** |
| `findDOMNode` | **Removed** | **Yes** |
| `createFactory` | **Removed** | **Yes** |
| `UNSAFE_componentWill*` lifecycles | Supported (discouraged) | No |

Key nuance: the old `componentWill*` lifecycles were **not** removed (the `UNSAFE_` forms still run).
The genuinely removed things that hurt legacy libraries are legacy class context, string refs,
function-component `defaultProps` and `propTypes`, `findDOMNode`, `ReactDOM.render`, and
`createFactory`. The ReactXP fork is exposed on context and `findDOMNode` (see Blocker A).

---

## 7. react-native-web: what it is and its status

**What it is.** The official docs call RNW "a compatibility layer between React DOM and React Native"
that "uses React DOM to accurately render React Native compatible JavaScript code in a web browser."
It provides RN's primitives (`View`, `Text`, `Image`, `TextInput`, `ScrollView`, `StyleSheet`)
targeting the browser, so the *same* component code that produces native widgets on phones produces
`<div>` / `<span>` + CSS in a browser. It is "a parallel implementation of React Native, a sibling to
React Native for iOS and Android." It powers the X/Twitter website.

**RNW vs the New Architecture.** Fabric, TurboModules, and JSI are *native-side* concepts (how RN
talks to iOS/Android). RNW *is itself the renderer for the web* (its target is React DOM); it has no
bridge and no native modules, so there is nothing to "migrate." RNW tracks the public JS component
contract (the props and behavior of `<View>`, `<Text>`, `StyleSheet`), not RN's native internals. It
documents itself as best used with React Native >= 0.68.

**Maintenance status (important).** RNW is maintained by Nicolas Gallagher and is effectively in
**maintenance mode**. In a Dec 2025 discussion he said he will keep reviewing PRs and merging fixes
but does not expect to put significant time into major development, and his focus has moved to a newer
project, **React Strict DOM**. Latest published version is **0.21.2 (Oct 2024)**. RNW is stable and
production-proven, so it is a safe *current* target, but treat it as stable-not-advancing and watch
React Strict DOM as the likely successor for the web seam.

**Known limitations (plan around these):** Animated has no native driver on web (it is a no-op there);
`KeyboardAvoidingView`, `StatusBar`, `LayoutAnimation`, `NativeModules` and a few others are mocked or
partial; `RefreshControl`, `TouchableNativeFeedback`, `Alert` are not implemented; `Text` lacks
`onLongPress`; `TextInput` lacks rich text.

**ReactXP archival status (confirmed).** Microsoft's ReactXP repository is archived and read-only.
Last code push 2023-07-18; last npm release `reactxp` 2.0.0 in 2019. The README states: "The ReactXP
library is no longer being maintained and is considered end of life. We recommend alternatives such as
React Native for Web." That is Microsoft's own recommendation pointing at RNW.

---

## 8. The modern animation / gesture / graphics stack

The core concept for a newcomer is the **two-thread model**. An RN app runs your logic (JavaScript) on
a **JS thread**, while the actual drawing happens on a separate **UI thread**. If an animation is
computed on the JS thread and that thread is busy, the animation stutters; this is **jank**. The whole
modern stack exists to move animation and gesture work onto the **UI thread** so motion stays smooth
regardless of what JS is doing. On the **web** there is no native thread, so the equivalent is to hand
animation to the browser's compositor (CSS / Web Animations API).

- **React Native Reanimated (v4, the modern standard, by Software Mansion).** Runs animations on the
  UI thread via "worklets" (small JS functions a Babel plugin moves to the UI thread). Provides
  `useSharedValue`, `withTiming`, `withSpring` (physics), `withDecay`, declarative Layout Animations
  (auto enter / exit / reorder), and experimental Shared Element Transitions. **Reanimated 4 supports
  ONLY the New Architecture (Fabric)**; Reanimated 3 is the legacy line for old-architecture apps.
  v4 added a declarative, CSS-compatible animation API as an alternative to hand-written worklets.
- **React Native Gesture Handler (RNGH, v3).** Recognizes gestures on the **UI thread** using each
  platform's native gesture APIs, replacing RN's JS-thread `PanResponder`. The `Gesture.Pan()` /
  `Gesture.Tap()` / etc. builder API pairs with Reanimated so a finger drag drives an animation
  without touching the JS thread.
- **Moti (declarative, on top of Reanimated).** You pass `from` / `animate` / `exit` style objects as
  props (a Framer-Motion-style model), drastically less code, and it works on **web via RNW**. This is
  the path that needs no hand-written worklets.
- **React Native Skia (Shopify).** A GPU 2D graphics / canvas engine (the Skia engine behind Chrome
  and Flutter) for custom drawing, shaders, and effects. Works on web via a WebAssembly build.
- **Web: Motion (formerly Framer Motion).** Runs animations via the browser's Web Animations API and
  ScrollTimeline for high-framerate performance, with a JS fallback. The common pairing for rich web
  animation alongside RNW.

---

## 9. Capability gap: ReactXP vs the modern stack

A 2018-era layer built on RN's old `Animated` + `PanResponder`, with the native driver off (and a
no-op on its web renderer), lacks the following. This profile matches ReactXP's `RX.Animated`.

| Capability | Modern stack | ReactXP fork | Gap |
|---|---|---|---|
| Animations on the UI thread | Default (Reanimated worklets) | JS-thread only; native driver off / no-op on web | **Major** |
| Animate layout props (width/height/top/left/flex) | Yes (worklets) | JS-thread only | **Major** |
| Spring / physics motion | `withSpring`, `withDecay` | timing/easing only | **Major** |
| Gesture-driven animation (UI thread) | RNGH + worklets | `PanResponder` on JS thread | **Major** |
| Layout animations (auto enter/exit/reorder) | Reanimated Layout Animations; Moti | none (manual at best) | **Major** |
| Shared-element / route transitions | Reanimated (experimental, native only) | none | **Major** |
| Scroll-linked effects | `useAnimatedScrollHandler`; Motion on web | janky, JS-thread-bound | **Significant** |
| Coordinated timelines / stagger / loop | full set | `sequence`/`parallel` only | **Moderate** |
| Declarative animation ergonomics | Moti; Reanimated v4 CSS API | imperative wiring | **Moderate** |
| 120Hz high-refresh | targets display refresh | capped by JS-thread jank | **Significant** |
| GPU 2D graphics / shaders | React Native Skia | none | **Major (categorical)** |
| Rich web animation | Motion (compositor-driven) | hand-rolled CSS-transition engine | **Significant** |

The crucial dependency: Reanimated 4, the standard engine, **requires the New Architecture**. So the
"real animation story" goal is gated on the New Architecture migration regardless of the ReactXP
decision, and ReactXP's model is on the wrong side of that line.

---

## 10. The transition design: re-point the seam, keep the surface

**One-sentence shape:** re-implement `LibClient/src/ReactXP` against React Native (native) +
react-native-web (web) instead of `@chaldal/reactxp`, keeping the **F# constructor surface and style
DSL identical**, so EggShell component code and the "feel" survive untouched while the engine
underneath becomes modern, New-Architecture, native-grade RN.

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
`ScrollView`, `Image`, `TextInput`). ReactXP was deliberately a curated subset of RN. You are
translating a small, well-understood vocabulary, not inventing one.

### What stays identical (the familiarity requirement)

- **Component call sites.** `LC.View(...)`, `LC.Text(...)`, the `[<Component>]` static-member form.
- **The style DSL.** `makeViewStyles`, `makeTextStyles`, the `module private Styles` / `FooStyles`
  conventions. You re-target what the CE *emits* (ReactXP runtime objects to RN style objects), not how
  you write it.
- **`LibRouter`.** Web keeps `react-router-dom`; native keeps `react-router-native`. Not forced onto
  Expo Router.
- **The data model.** Subscription-driven `AsyncData<'T>`, SignalR push, `EntityService`.
- **F#-only, shared types.** Application code stays 100% F#. Only the framework wrapper bindings plus a
  few tiny JS interop shims are JS, following the existing `ThirdParty` recipe.

The design choice that preserves the feel: the F# wrapper layer becomes, again, the place where you
curate "the EggShell primitives." ReactXP gave you a safe subset; RN/RNW expose a larger surface where
a few props differ web vs native; your wrapper re-imposes the curated, type-safe subset. Familiarity
is a design output, not luck.

### What changes underneath, layer by layer

| Layer | Today (ReactXP) | After (RN + RNW) | Effort |
|---|---|---|---|
| Bindings | `import "@chaldal/reactxp"` | `import "react-native"`; bundler aliases `react-native` to `react-native-web` for the web build (standard ecosystem trick, no `#if` split needed for primitives) | Low |
| Primitives | `RX.View/Text/Button/...` wrap ReactXP classes | Same F# signatures, wrap RN `View/Text/Pressable/ScrollView/Image/TextInput` | Med (mechanical, per-component) |
| Press/tap | `GestureView`/`TapCapture` on `PanResponder` | RN `Pressable` (EggShell already moved to `LC.Pressable`) + RNGH for real gestures | Low-Med (partly done) |
| Styles | `Styles/{Legacy,New}` emit ReactXP runtime objects | Same CE surface, emit RN style objects. Flexbox/spacing/color map ~1:1; web-only features (hover, nested `&&`/`=>`) become RNW-web-only | Med |
| Animation | `Styles/Animation.fs` + `Animatable*` over old `Animated`, driver off | Reanimated 4 + RNGH 3 (native, UI-thread) and Moti (declarative, web via RNW); Motion for rich web | Med-High (new capability) |
| SVG / NetInfo / WebView / VirtualList | ReactXP extensions | `react-native-svg`, `@react-native-community/netinfo`, `react-native-webview`, RN `FlatList`/`VirtualizedList` (several already pinned in `ThirdParty`) | Low-Med |
| Toolchain / runtime | RN legacy bridge, React 18 | RN New Architecture (Fabric/JSI/TurboModules), React 19; optionally Expo + EAS | Med (rides the platform move) |

The web side gets *simpler*: today the fork ships a bespoke DOM renderer + inline-style engine + its
own animation engine. RNW replaces all of that with a maintained, production-proven implementation.
Net deletion of code.

---

## 11. F# / Fable specific risks

Because the non-negotiable is F#-only, the real risk is not the primitives, it is how F# / Fable meets
the modern animation libraries.

- **Risk 1: Reanimated "worklets" from F# (the one to de-risk first).** Reanimated runs animations on
  the UI thread by compiling specially-marked JS functions ("worklets") via a Babel plugin. Fable
  emits JS, then Metro/Babel runs. The open question is whether the Reanimated Babel plugin reliably
  recognizes Fable-generated function shapes as worklets.
  - **Mitigation (and probably the right pattern regardless):** do not author worklets in F#. Use the
    declarative APIs that need no hand-written worklets, namely Reanimated 4's CSS-style / Layout
    Animation API and **Moti** (style objects as props, works on web via RNW). For the rare imperative
    gesture-worklet, write a tiny hand-written JS/TS helper, let Babel process it normally, and wrap it
    in F# via the existing `ThirdParty` recipe (`TypesJs.fs` marshalling). F# stays declarative; the
    worklet lives in ~20 lines of JS owned by the framework, never by app code.
- **Risk 2: binding Reanimated/RNGH hooks in F#.** These are hooks-based. EggShell already uses Fable
  hooks in the `[<Component>]` path, so this is "write binding files," not new ground.
- **Risk 3: style CE retargeting.** RN takes plain style objects in `style={[...]}` arrays with
  last-wins merging (which ReactXP deliberately mirrors). Mostly mechanical. The real decision:
  ReactXP's nested `&&`/`=>` selectors and responsive breakpoints have no native RN equivalent; keep
  them web-only (RNW supports some), handle native variants via F# branching (already done with
  `LC.With.ScreenSize`).
- **Risk 4: "as close to native" is delivered by RN, not RNW.** RNW is the web target (real DOM). True
  native on iOS/Android comes from RN's New Architecture (Fabric renders real native views, JSI gives
  synchronous native calls). The "close to native" goal is met on phones by the New Architecture and on
  web by RNW being a faithful RN implementation. Strictly better than ReactXP's lowest common
  denominator.

---

## 12. How each goal is delivered

| Goal | Mechanism in the new stack |
|---|---|
| Performance | New Architecture: JSI (no async bridge), Fabric (concurrent, thread-safe renderer, synchronous layout). Reanimated/RNGH run animation and gestures on the UI thread, so no jank when JS is busy. 120Hz-capable. |
| Efficiency | Faster startup (no bridge init, lazy TurboModules), Metro ~4x faster warm builds and ~15x faster resolver (RN 0.76), smaller Android binaries. RNW's atomic CSS beats the fork's inline-style engine. |
| Modern features | Layout / enter / exit / shared-element transitions, scroll-linked effects, spring/physics, React 19 (Actions, `use()`, concurrent features via Fabric). |
| Modern libraries | Unlocks the ecosystem currently out of reach: Reanimated 4, RNGH 3, Moti, React Native Skia, Motion on web. All gated on the New Architecture, which you would be on. |
| As close to native | Fabric = real native views + synchronous native calls on iOS/Android; RNW = faithful RN on web. Curated F# wrapper keeps it type-safe. |
| F#-only FE/BE/shared types | Preserved. App code stays 100% F#. Only the framework's wrapper bindings plus a few small vetted JS animation shims are JS, the existing `ThirdParty` pattern. Backend and shared types unaffected. |

---

## 13. Sequencing and the phased plan

This migration is gated on the platform move and should ride it, not precede it. Reanimated 4 requires
the New Architecture (default in RN 0.76+); React 19 ships in RN 0.78+. So the natural order is:
**platform baseline (Fable 5 / .NET 10 / modern RN with New Arch + React 19) first, then the
ReactXP-to-RNW seam swap with animation.**

Because the framework is used for **greenfield projects only** (no legacy apps to maintain), there is
**zero migration debt**. This is the cheapest possible version of the project: rebuild one framework
seam and set the template for new apps, rather than porting hundreds of live screens.

**Phased plan:**

1. **Spike the worklet question (1-2 weeks, do first).** A throwaway standalone project (see Section
   14) that answers: can Fable F# drive Reanimated declaratively, and where exactly do we need a JS
   shim? De-risk the only genuine unknown before committing.
2. **Land the platform baseline.** Fable 5, .NET 10, RN bumped to a New-Architecture release, React 19.
3. **Re-implement the primitives seam.** `ReactXPBindings.fs` to RN/RNW; port `Components/*` keeping F#
   signatures; bundler-alias `react-native` to `react-native-web` for web. Validate with the gallery
   and its existing audit harness.
4. **Re-target the style DSL.** `Styles/{Legacy,New}` emit RN style objects; decide web-only vs native
   for nested selectors and breakpoints.
5. **Build the animation layer.** F# wrappers over Reanimated 4 + RNGH 3 + Moti; Motion for web; Skia
   as an optional `ThirdParty` wrapper. This is where the modern-features payoff lands.
6. **Bake it into scaffolding.** The corrected `create-app` template ships the RNW/RN stack as the
   canonical modern EggShell app, so every greenfield project starts here.

---

## 14. The de-risking spike

**Principle: the spike does not touch the EggShell repo (`subject/`) and does not break anything.** It
is a standalone throwaway project under `/Volumes/HomeX/shafayat/Code/` (a sibling of `subject/` and
`reactxp-fork/`), with its own `package.json` and its own minimal Fable project. It never imports from
or modifies `subject/`. Different directory, different processes, different ports, so active testing of
the freshly migrated F# components in `subject/` can continue in parallel.

**What the spike answers (the unknowns), in priority order:**

1. Can Fable-compiled F# bind to and *declaratively* drive Reanimated (for example a `Moti`
   `animate`-prop component, and a Reanimated `withSpring` on a shared value) on a New-Architecture RN
   app? (Highest value, the only true unknown.)
2. Does the imperative worklet path work directly on Fable output, or do we need a small hand-written
   JS worklet shim called from F#?
3. Does the full toolchain assemble: Fable (F# to JS) -> Metro -> Reanimated Babel plugin -> New
   Architecture RN, on both native and web (RNW)?
4. Does an RNGH gesture drive a shared value from F#?

**Shape of the spike (proposed):**

- A minimal **Expo** app (Expo gives the New Architecture by default, RNW for web built in, and EAS
  builds, with the least config). Bare RN 0.76+ is an alternative if we want to avoid Expo.
- A tiny **Fable** F# project whose output is bundled by Expo's Metro. One screen, a handful of
  components:
  - (a) an RN `View` + `Text` rendered from F# bindings (proves primitive binding from Fable),
  - (b) a **Moti** component animated via the `animate` prop from F# (proves the no-worklet declarative
    path, and that it works on web via RNW),
  - (c) a **Reanimated** `useSharedValue` + `withSpring` + `useAnimatedStyle` from F# (probes the
    worklet path; if the plugin does not recognize Fable output we learn we need a JS shim),
  - (d) an **RNGH** `Gesture.Pan()` driving the shared value from F# (probes gesture-to-UI-thread).
- If (c) or (d) fail because Fable functions are not recognized as worklets, add a ~20-line
  hand-written JS worklet helper and call it from F# via the `ThirdParty`-style marshalling, then
  confirm that path works. That outcome is itself a successful spike result: it tells us the framework
  pattern (declarative in F#, worklets in tiny vetted JS shims).

**Deliverable:** a short findings write-up appended to this document's Update Log, stating which of
(a)-(d) worked directly, where a shim was needed, and any toolchain friction. That determines the F#
binding strategy for the real seam work in phase 3+.

---

## 15. Decision summary

- **Keeping `@chaldal/reactxp` current is not viable as a strategy.** It would require rewriting the
  web layer for React 19 (legacy context + `findDOMNode`) and the native layer for the New
  Architecture (`setNativeProps` / `findNodeHandle` / `UIManager`), with no tests and dead CI, only to
  arrive at 2018-era feature parity that still cannot use the modern libraries. Microsoft has declared
  ReactXP end-of-life and points at react-native-web.
- **The strategic move is migrating the `LibClient/src/ReactXP` seam to RN + react-native-web.** In
  this codebase that seam is contained (a handful of files), the F# surface above it can stay
  identical (familiarity preserved), and it unlocks the New Architecture, React 19, and the entire
  modern animation/gesture/graphics ecosystem.
- **The animation goal is gated on the New Architecture regardless**, because Reanimated 4 requires it.
  So the platform baseline (Fable 5 / .NET 10 / modern RN) sequences before the seam swap.
- **Greenfield-only means zero migration debt**, making this the cheapest possible version of the work.
- **One caveat to carry:** RNW is itself in maintenance mode (its author moved to React Strict DOM,
  latest 0.21.2). It is a safe current target, but because everything is contained to this one wrapper,
  swapping the web target again later (for example to React Strict DOM) would be the same bounded
  exercise, not another rewrite.

---

## 16. React Strict DOM and the web-target choice

react-native-web (RNW) is the obvious web target today, but it is in maintenance mode, so we
researched its likely successor, **React Strict DOM (RSD)**, before committing the seam.

**What RSD is.** RSD is Meta's successor effort to RNW, from the same author (Nicolas Gallagher). It
inverts RNW's direction. RNW says "write React Native, run it on web." RSD says "write web standards
(`html.div`, `html.span`, plus CSS through StyleX), run it on native too." On web it compiles to real
DOM + static atomic CSS (near zero runtime cost). On native it translates the same source into React
Native primitives (a genuinely native app, not a WebView) plus a couple of small polyfills.

**Maturity (mid-2026).**

| Dimension | Finding |
|---|---|
| Version | **0.0.55**, still pre-1.0, officially **experimental** |
| Peer deps | `react-native >=0.79.5`, `react`/`react-dom ~19.0.0` |
| Production use | **Yes, on web at Meta** (facebook.com; Facebook/Instagram on VR) |
| Web target | mature |
| Native target | **lags, roughly 60% of the API**; notably **no flow/grid/inline layout on native yet**, plus acknowledged runtime overhead |
| RNW status | frozen, last release 0.21.2 (Oct 2024), maintenance only, separate codebase (not merged into RSD) |

**Decision for our seam.** Keep **RNW as the runtime target today** (it has full flexbox layout on web
and native and a stable API), but make two forward-looking moves so a later RSD switch is cheap:

1. **Shape the F# public API toward web-standard idioms** (DOM-flavored elements, `onClick`-style
   events, `aria-*` accessibility, a `css.create`/StyleX-shaped styling surface) rather than leaking
   RNW-specific idioms (`onPress`, RN `StyleSheet` semantics, RN-only props) above the seam. RSD's API
   is closer to plain React DOM than RNW's, so a DOM-flavored F# surface ages better.
2. **Re-evaluate RSD when it hits two milestones:** a stable 1.0 with an API-stability commitment, and
   the native layout gaps (flow/grid/inline-flex) closing.

The contained-seam design pays off here too: when RSD matures, re-pointing one thin wrapper is a
bounded job, not another rewrite. Risk framing: RNW's risk is stagnation (works, will not break under
you); RSD's risk is immaturity (pre-1.0 churn, incomplete native). For a thin wrapper, carrying RNW's
stagnation now is the cheaper risk.

---

## 17. Fable 4 vs 5, .NET 10, and decoupling from Orleans

This section answers the question: can the frontend and backend move to .NET 10 + Fable 5 **without**
doing the hard Orleans 3.7 to 7 migration yet?

### Fable 4 vs Fable 5

What Fable 5 adds over Fable 4 (~4.18): the tool now **targets net10.0**; project cracking was
rewritten from Buildalyzer to **invoking MSBuild directly** (more robust); `TreatWarningsAsErrors`
support; a maturing **TypeScript output** target; plus non-JS backends (Rust, Python, Dart, Erlang)
that are not relevant to our JS frontend. Fable 5 is described as "compatible with Fable 4 projects,
except that it now targets net10.0."

**The real upgrade risk is the compiler plugin API.** It **broke between Fable 4 and 5**. Feliz needed
a major-version update for Fable 5, and **any custom Fable plugin must be rebuilt against Fable 5's
API.** EggShell ships a custom Fable plugin (the `[<Component>]` plugin in `Meta/FablePlugins`), so
that plugin is the single most likely blocker for the full frontend migration. `Fable.React`,
`Thoth.Json`, and `Fable.Promise` are expected to need only version bumps rebuilt for Fable 5, but each
dependency must be confirmed to have a Fable-5-compatible release.

### The .NET 10 gate (confirmed empirically)

The **Fable 5.4.0 tool targets net10.0 and requires the .NET 10 SDK to run.** Confirmed on this machine
(only .NET 7.0.410 and 8.0.408 installed): `dotnet tool restore` of Fable 5.4.0 fails with
`Settings file 'DotnetToolSettings.xml' was not found in the package`, which is how a net10-targeted
tool fails to install on an SDK that cannot read net10 assets. A newer-major runtime is never selected
downward and roll-forward only goes forward, so .NET 8 cannot run a net10 tool. **Installing the .NET
10 SDK is a prerequisite for Fable 5.** This is additive and does not disturb `subject/`, which pins
7.0.300 via its own `global.json`.

### Decoupling: .NET 10 + Fable 5 with Orleans staying at 3.7.2

- **Frontend and backend platform decisions are separable.** Fable 5 needs the .NET 10 SDK on the
  **build machine**, but that does not force the **backend** to net10. You can adopt Fable 5 on the
  frontend while leaving the Orleans backend on net6/net8.
- **The genuinely hard, unsupported coupling is specifically Orleans 3.7.2 on net10.0**, and the reason
  is **BinaryFormatter**. Orleans 3.x uses BinaryFormatter on some serialization paths. .NET 8 disables
  BinaryFormatter by default (re-enable-able via a compat switch); **.NET 9+ removes it entirely.** So a
  naive net10 backend bump with Orleans 3.7.2 will likely produce **runtime serialization failures**,
  not clean compile errors.
- **Orleans 3.7.2 assemblies load on net8/net10** (they target netstandard2.0), and the package is still
  on NuGet, but expect `Microsoft.Extensions.*` version-unification conflicts against a net10 host.

**Verdict: decoupling is feasible but moderate-to-hard on the backend, and net8.0 is the safer
intermediate than net10.0.** Recommended shape: move the **frontend to Fable 5** (install the .NET 10
SDK) and, if bumping the backend now, take it to **net8.0 + Orleans 3.7.2** (where the BinaryFormatter
compat switch still exists) rather than net10.0. Defer net10-on-the-backend to the coordinated Orleans
upgrade. The priority validation item is auditing whether any grain message or persisted state hits the
BinaryFormatter path.

### EggShell-specific feasibility (from a 2026-06-28 codebase audit)

**Frontend (Fable 4 -> Fable 5): MODERATE, doable.** The custom `Meta/FablePlugins` plugin (the
`[<Component>]` attribute, used in **326 files / 455 call sites**) is the gating item but is *small*:
three known edits make it Fable-5-compatible: `FableMinimumVersion "4.0"` -> `"5.0"`, `Fable.AST` 4.5.0
-> 5.0.0, and add the new `Ident.IsInlineIfLambda` field in `AstUtils.makeIdent`. The plugin base
classes (`MemberDeclarationPluginAttribute`, `Transform`/`TransformCall`), `PluginHelper`, and
`ScanForPlugins` are unchanged 4->5, and consumers only use the attribute, so zero call-site changes.
Real aggregate effort is routine package bumps (Fable.Core 4.3->5, Fable.React 9.4->10, Thoth.Json,
Fable.Promise, Fable.Browser.*, Fable.Date) plus one watch-item: **Fable.SignalR 0.16.0 /
Fable.SignalR.AspNetCore 0.14.0** (low-maintenance; confirm a Fable-5 build or vendor it), the SDK pin
bump (`global.json` 7.0.300 -> 10.x), `FSharp.Core` bump, wiping Fable/precompile caches, and verifying
stale CLI flags in `Meta/LibFablePlus/src/index.ts` (e.g. `--noParallelTypeCheck`). The render DSL
compiler is not on the Fable build path, so it does not gate.

**Backend (.NET 7 -> net10, Orleans frozen 3.7.2): MODERATE-leaning-HARD.** App serialization is clean
(no BinaryFormatter anywhere; grain state is gzip System.Text.Json via Fleece). But two real hazards:
(1) Orleans 3.7.2's **own** fallback serializer (`ILBasedSerializer`) can route unregistered/framework
types and the `ISerializable` exceptions in `LibLifeCycleTypes/src/Exceptions.fs` to BinaryFormatter,
which **throws on .NET 9+** and cannot be proven safe statically (runtime validation only); (2) the
legacy `OrleansCodeGenerator.MSBuild` + the C# `*Build.csproj` codegen may not build under the net10
SDK at all, plus ASP.NET Core 10 vs `Giraffe 6.2.0` / `Giraffe.TokenRouter 3.0.0-alpha-1` /
`Fable.SignalR.AspNetCore 0.14.0`. Quick win: a stray `System.Text.Json 5.0.2` pin in
`LibLangFsharp.fsproj` should be removed/bumped first. **Recommended path: net7 -> net8 (validate, with
the BinaryFormatter escape hatch available) -> net10, with the Orleans 8.x upgrade slotted before the
net9/net10 hop.** Plain `PackageReference` throughout (no Paket / no Central Package Management), so
upgrades are per-fsproj edits.

**Consequence for the spike:** the spike is frontend-only with no Orleans backend, and Fable 5 on the
frontend does not force the backend to net10, so the hard backend story does not block proceeding with
Fable 5 in the spike.

---

## 18. Non-React F# alternatives (food for thought)

We evaluated whether any non-React path serves the goals better. Honest conclusion up front: **no single
non-React path cleanly satisfies all of {good F#, true cross-platform including web, close to native}.**

- **The literal "F# transpiled to Kotlin / Scala / Swift" idea does not exist.** There is no mature
  (or even immature) F#-to-Kotlin/Scala/Swift transpiler. Set this aside.
- **Fable to Dart to Flutter is not viable.** F#-to-Dart (the language) is real but beta. The Flutter UI
  bindings (`Fable.Flutter`) are **abandonware, last touched 2022.** Fable's own maintainers call the
  non-JS backends "distractions"; only F#-to-JS is first-class. Fable-to-Rust is alpha and logic-only.
- **The one real contender is Avalonia + Avalonia.FuncUI** (an F#-first MVU layer, actively maintained,
  FuncUI 1.5.2 Oct 2025). Avalonia draws its own widgets via Skia (like Flutter), cross-platform.
  Desktop is production-grade, mobile is viable and improving, **web (WASM) is the weak target** (heavy
  download, below-native performance).
- **MAUI and Uno are F#-hostile** (XAML/C#-first, F# UI unsupported or second-class). Bolero/Blazor is
  excellent F# but **web only.**

**The headline appeal of the .NET-native path (Avalonia/FuncUI) is sharing F# domain types in-process
with the Orleans backend, no Fable and no JSON wire. But that prize does not survive the web target:**
browsers cannot open raw TCP sockets, so a WASM app cannot be an Orleans client and is forced back to an
HTTP/WebSocket gateway, which is exactly the wire layer the premise wanted to avoid. (You still share
the F# types through the gateway, you just lose the in-process call.) Additionally, iOS bans JIT, so an
Orleans client under iOS NativeAOT with F# DU/record serialization is unverified and would be the
highest-risk thing to prototype.

**Native-ness nuance that matters for the "close to native" goal:** React Native actually renders
**true native OS controls** on mobile, whereas Avalonia, Flutter, and Compose Multiplatform all **draw
their own pixels with Skia.** So on the specific axis of "as close to native as possible on mobile,"
React Native is arguably ahead of the self-drawn frameworks. Avalonia wins on language purity and
backend type-sharing, not on native fidelity.

**Verdict:** the react-native-web direction remains the better route to "native mobile plus a real web
target," accepting the Fable/JSON wire as its price. **Avalonia + FuncUI is the only non-React F# path
worth a time-boxed spike**, and only if web-via-WASM weight is acceptable and an iOS-AOT-plus-Orleans
prototype passes. If web is a first-class requirement (it is), this is a hard sell. Keep it on the radar,
do not pursue MAUI, Uno, or Fable-to-Flutter.

---

## 19. Spike status and the confirmed Fable 5 / .NET 10 gate

**Scaffolded** at `/Volumes/HomeX/shafayat/Code/eggshell-rnw-spike` (standalone, isolated from
`subject/`, its own git repo and `node_modules`).

- Expo template pinned the modern stack: **Expo SDK 56, React Native 0.85.3, React 19.2.3** (New
  Architecture default).
- F# project under `fsharp/` (`Bindings.fs`, `App.fs`) with the four probes (A primitives, B Moti
  declarative, C Reanimated worklet, D RNGH gesture). Tiny JS root (`App.js`), worklet Plan-B shim
  (`js/worklets.js`), `babel.config.js` with `react-native-worklets/plugin`, Fable tool pinned in
  `.config/dotnet-tools.json`.
- **Blocked on the Fable 5 / .NET 10 gate (confirmed):** Fable 5.4.0 will not install/run on the
  current .NET 7/8 SDKs (see Section 17). To run the spike with Fable 5, install the .NET 10 SDK first.
  Alternative: temporarily pin the tool to Fable 4.x to exercise the JS/RN toolchain now and revisit
  Fable-5 specifics after .NET 10 is installed.

**Open decision:** install .NET 10 SDK now (run the spike on Fable 5 as intended), or run the spike on
Fable 4.x first and add .NET 10 later.

---

## 20. Sources

**React Native New Architecture and releases**
- New Architecture deep-dive: https://reactnative.dev/blog/2024/10/23/the-new-architecture-is-here
- RN 0.76 (New Arch default): https://reactnative.dev/blog/2024/10/23/release-0.76-new-architecture
- RN 0.73: https://reactnative.dev/blog/2023/12/06/0.73-debugging-improvements-stable-symlinks
- RN 0.74: https://reactnative.dev/blog/2024/04/22/release-0.74
- RN 0.78 (React 19): https://reactnative.dev/blog/2025/02/19/react-native-0.78
- RN 0.80 (legacy frozen): https://reactnative.dev/blog/2025/06/12/react-native-0.80
- RN 0.81: https://reactnative.dev/blog/2025/08/12/react-native-0.81
- Architecture landing page (JSI): https://reactnative.dev/architecture/landing-page
- Codegen: https://reactnative.dev/docs/the-new-architecture/what-is-codegen
- Interop known issues: https://github.com/reactwg/react-native-new-architecture/discussions/237
- Support tracker: https://github.com/reactwg/react-native-releases/blob/main/docs/support.md

**React 19**
- Release post: https://react.dev/blog/2024/12/05/react-19
- Upgrade guide: https://react.dev/blog/2024/04/25/react-19-upgrade-guide
- Component reference (legacy lifecycles): https://react.dev/reference/react/Component
- React Compiler v1.0: https://react.dev/blog/2025/10/07/react-compiler-1

**react-native-web and ReactXP**
- RNW docs: https://necolas.github.io/react-native-web/docs/
- RNW compatibility: https://necolas.github.io/react-native-web/docs/react-native-compatibility/
- RNW future discussion: https://github.com/necolas/react-native-web/discussions/2816
- ReactXP repo (archived): https://github.com/microsoft/reactxp
- ReactXP FAQ: https://microsoft.github.io/reactxp/docs/faq.html

**Animation / gesture / graphics**
- Reanimated: https://docs.swmansion.com/react-native-reanimated/
- Reanimated 4 stable: https://blog.swmansion.com/reanimated-4-stable-release-the-future-of-react-native-animations-ba68210c3713
- Reanimated migration from 3.x: https://docs.swmansion.com/react-native-reanimated/docs/guides/migration-from-3.x/
- Gesture Handler: https://docs.swmansion.com/react-native-gesture-handler/docs/fundamentals/installation
- Moti: https://moti.fyi/
- React Native Skia: https://shopify.github.io/react-native-skia/
- Native driver / built-in Animated: https://reactnative.dev/docs/animations
- Motion (web): https://motion.dev/docs/react

**Ecosystem / Expo**
- RN environment setup (recommends Expo): https://reactnative.dev/docs/environment-setup
- Expo Router: https://docs.expo.dev/router/introduction/
- Expo Web: https://docs.expo.dev/workflow/web/
- Expo SDK 55: https://expo.dev/changelog/sdk-55

**React Strict DOM / StyleX (Section 16)**
- RSD repo and README: https://github.com/facebook/react-strict-dom
- RSD production-readiness discussion #270: https://github.com/facebook/react-strict-dom/discussions/270
- RSD npm (0.0.55, peer deps): https://registry.npmjs.org/react-strict-dom/latest
- RNW maintenance-mode discussion #2816: https://github.com/necolas/react-native-web/discussions/2816
- StyleX docs: https://stylexjs.com/docs/learn/
- Gallagher, "One React for Web and Native": https://nicolasgallagher.com/one-react-for-web-and-native/

**Fable 4 vs 5 / .NET 10 / Orleans (Section 17)**
- Fable 5 RC: https://fable.io/blog/2026/2026-02-27-Fable_5_release_candidate.html
- Fable 5 alpha: https://fable.io/blog/2024/2024-12-18-Fable_5_alpha.html
- Fable .NET compatibility: https://fable.io/docs/dotnet/compatibility.html
- Feliz #663 (plugin API break for Fable 5): https://github.com/fable-hub/Feliz/issues/663
- .NET roll-forward rules: https://learn.microsoft.com/en-us/dotnet/core/versions/selection
- BinaryFormatter disabled in .NET 8: https://learn.microsoft.com/en-us/dotnet/core/compatibility/serialization/8.0/binaryformatter-disabled
- BinaryFormatter removed in .NET 9: https://github.com/dotnet/runtime/issues/98245
- Orleans serialization: https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/serialization

**Non-React F# options (Section 18)**
- Fable backends and maturity: https://fable.io/docs/ ; https://github.com/fable-compiler/Fable/discussions/3351
- Fable.Flutter (abandonware, 2022): https://github.com/fable-compiler/Fable.Flutter
- Avalonia.FuncUI: https://github.com/fsprojects/Avalonia.FuncUI ; https://funcui.avaloniaui.net/
- Avalonia WASM: https://docs.avaloniaui.net/docs/deployment/webassembly
- Orleans F# serialization: https://www.nuget.org/packages/Microsoft.Orleans.Serialization.FSharp/
- Bolero: https://fsbolero.io/

> **Confidence flags from the research:** New Arch default in 0.76, React 19 first in 0.78, legacy
> frozen in 0.80, the React 19 removals, ReactXP EOL, RNW maintenance mode and latest 0.21.2, and the
> Reanimated 4 New-Architecture requirement are all High confidence. Exact patch numbers for
> Reanimated/Moti/Skia, the precise RN 0.82/0.85 dates, and the exact ReactXP archive date are
> approximate or secondary-sourced.

---

## 21. Update Log

- **2026-06-28 (1)** Document created. Incorporates: the `@chaldal/reactxp` fork audit (read-only
  analysis of the clone at `/Volumes/HomeX/shafayat/Code/reactxp-fork`), the modern RN / React 19 /
  react-native-web research, the animation/gesture/graphics ecosystem research, the
  ReactXP-to-RNW transition design, the goal mapping, the phased plan, and the de-risking spike plan.
- **2026-06-28 (16)** **Executor recipe for the TODO app + scaffold modernization written.** Added
  **`MIGRATION_RUNBOOK.md` Phase 5** - a thorough, step-by-step recipe a lower-cost executor can follow to
  build the full-stack TODO and modernize the `create-app` scaffold: 5A backend Todo ecosystem (modeled on
  the in-repo SuiteJobs, with the explicit codec-gen step called out as the fiddly escalate-if), 5B backend
  host serving the V1 API + realtime SignalR, 5C the frontend CRUD + subscription app with the
  connection standard spelled out inline (`Config.BackendUrl` -> `AppServices.initialize` wiring
  `HttpService` + `RealTimeService` + subject-service `provideInstances` -> components subscribe via
  `With.Subject(s)` rendering `AsyncData`), 5D templatize into `Meta/LibScaffolding` (nested render schema,
  pure F#, decouple from Chaldal), 5E `create-app -> dev-web` smoke test. Connection standards were derived
  from study of external example apps but are encoded generically; **nothing in this repo references the
  external projects** (constraint honored). Build order: concrete app to green first, then templatize.
- **2026-06-28 (15)** **Decision: the benchmark becomes a full-stack TODO app that also fixes scaffolding
  (goal B).** Instead of a throwaway Suite, modernize the `eggshell create-app` templates
  (`Meta/LibScaffolding/templates`, currently broken per Section 8: flat `renderDependencies`, `.render`
  routes, Chaldal-coupled `Bananas`/`Mangoes`, net7) AND make the default generated app a working
  **full-stack TODO**: a backend Todo subject lifecycle (Create/SetTitle/ToggleDone/Delete + a
  timer-driven auto-update to exercise real-time push) plus a frontend that does CRUD and **subscribes**
  for live updates. This doubles as the modern-conventions reference, the goal-B scaffold fix, and the
  subscriptions/SignalR validation benchmark. **Build order:** (A) build the concrete full-stack TODO app
  in-repo and get `dev-web` green with live CRUD + subscriptions (backend modeled on the in-repo SuiteJobs
  API); (B) templatize it into the scaffold, modernizing config (nested `render` schema, pure F#, decoupled
  from Chaldal) and updating the scaffolding tasks; (C) add a `create-app -> dev-web` smoke test.
  **Constraints:** F# only; nothing in `subject/` references the old Egg.Shell repo; subagents here have no
  Bash so the compile/validate loop is done directly. Todo domain (draft): Subject `{ Id; CreatedOn;
  Title; Done; ArchivedOn? }`; Constructor `Create of title`; Actions `SetTitle | ToggleDone | Archive |
  Delete`; LifeEvents `Created | TitleChanged | DoneToggled | Archived`; OpError `EmptyTitle`; a View
  listing todos; a timer auto-archiving long-completed todos (the automated-update signal).
- **2026-06-28 (14)** **Build-hygiene quick wins applied (post-migration sweep).** Ran a full build of
  the modularized repo and triaged warnings the Fable-5 migration left. Fixed (framework): FS3370
  deprecated refcell ops in `LibRouter/.../LogRouteTransitions.fs` (`!`/`:=` -> `.Value`/`.Value <-`);
  FS1182 unused-value cleanups in `LibUiAdmin/Grid.fs` (cosmetic `_content`), `LibAutoUi/ValueConstruction.fs`
  (`| _ ->`), `LibAutoUi/InputFormElement.fs` (removed redundant outer bindings). **One real bug found and
  fixed:** `UiAdmin.GridRow`'s `showBottomBorder` was honored on native but silently ignored on web (the
  `dom.tr` className never reflected it) — wired it into the web className (`no-bottom-border` token; a
  matching CSS rule should be confirmed). Left alone: `LibAutoUi/TypeExtensions.fs` unused bindings are
  inside a deliberate `QQQ` stub (unfinished feature, not migration breakage); FS842 on `SubjectTypes.fs`
  is a pre-existing `[<SkipCodecAutoGenerate>]`-on-interface (verify the codec-skip still takes effect).
  **Flagged for owner (App*, not auto-fixed per framework-only rule):** `pstoreKey` unused in
  `AppEggShellGallery` `Route/Home|Settings|Docs` smells like dropped persistent-store wiring; `Code.fs`
  `language` unused. Process note: the unused/deprecated warnings only surface in the eggshell/Fable build
  path (config-gated `--warnon:1182`), not a plain `dotnet build -c "Web Debug"`, which is how they slipped
  in; surface them in CI and, once cleared, drop `--warnaserror-:1182`.
- **2026-06-28 (13)** **Prerequisites locked BEFORE the RNW upgrade (goal H).** The following must be
  done and signed off before starting the react-native-web seam swap; each to be discussed in its own
  session: (a) **Libraries used** — audit + upgrade the JS and NuGet dependency surface; (b)
  **Performance improvements** — the .NET 10 + Fable 5 wins to measure/claim; (c) **Fable 5 new syntax &
  defaults** — adopt the new language/compiler defaults (incl. the array-equality change, TS-output, etc.);
  (d) **our SignalR** — the vendored `LibSignalRClient`/`LibSignalRServer` (verify build+runtime, add MIT
  attribution, decide vendor-vs-thin); (e) **.NET 10** — the full runtime TFM flip net7->net10 with Orleans
  frozen (research in progress). RNW (Phase 4) does not start until (a)-(e) land.
- **2026-06-28 (12)** **Backend subject-service benchmark planned.** Need a real example backend Subject
  service in-repo (a Suite: `Ecosystem/{Types, LifeCycles, Tests}` + a Dev launcher) to serve as both a
  worked example AND the runnable validation + performance benchmark for the net10 / Fable 5 / SignalR
  migration (a genuine grain + views + simulation tests, not the throwaway spike). Built fresh and
  generic; NOT derived from or referencing any existing Suite.
- **2026-06-28 (11)** **Executor runbook written.** Created `MIGRATION_RUNBOOK.md` (this folder): a
  step-by-step, prescriptive companion for a weaker/executor model to do the mechanical bulk of the
  migration, with hard validate-after-each-step gates, inline pitfalls (from the spikes + `LEARNINGS.md`),
  and explicit "stop and escalate" triggers. Division of labor: weaker model does Phases 1-3 bumps + the
  Phase 4 primitive fan-out; a stronger model owns the `FablePlugins` port fallback, all debugging, and
  the Phase 4 seam pattern-setting. This strategy doc remains the *why*; the runbook is the *how*.
- **2026-06-28 (10)** **Fable 5 / Fable.React alpha issue scan (GitHub).** Correction: the Fable 5
  **compiler is GA** (5.0.0 Apr 2026, now 5.4.0 Jun 24 2026), not alpha. The only alpha is the
  `Fable.React 10.0.0-alpha.1` binding (stable Fable.React is 9.x, for Fable 4). Open Fable.React issues
  to plan around (all pre-existing 9.x issues that ride into the alpha, not alpha-specific breakage):
  - **#241 (open):** `FunctionComponent.Of` caches by `sourceFile + "#L" + line`, so the same source
    line producing multiple distinct components collides and the wrong cached component runs. **Observed
    this exact mechanism in the spike's emitted JS.** EggShell's normal path uses the custom
    `Meta/FablePlugins` `[<Component>]` plugin (compile-time, one component per declaration) rather than
    runtime `FunctionComponent.Of`, so exposure is likely low, but the plugin's keying must be verified
    against this during the port.
  - **#243 (open):** `voidEl` emits `createElement("br", {}, [])`; React rejects the trailing `[]` for
    void elements. Web-only (br/img/input/hr); RN/RNW primitives mostly avoid it. Workaround exists.
  - **#239 / #240 / #177 (open):** `useCallback` not implemented; no ref to function components;
    `forwardRef` not exposed. React 19 ref-as-prop softens forwardRef; missing hooks can be bound
    directly (ThirdParty pattern).
  - **Compiler 4->5 behavioral change (not alpha):** Fable 5.0.0 changed `Array`/`ResizeArray` equality
    semantics. Grep for reliance on structural array equality via `=` before bumping.
  None are blockers (the spike proved render + hooks + interop + SignalR on the alpha). Two concrete
  port-time verifications: (a) the `[<Component>]` plugin's cache keying vs #241, (b) the array-equality
  change.
- **2026-06-28 (9)** **Real EggShell exception types confirmed on net10; Postgres thought parked.**
  Extended the Orleans spike with the actual `TransientSubjectException` / `PermanentSubjectException` /
  `InconclusiveSubjectException` (copied verbatim from `LibLifeCycleTypes/src/Exceptions.fs` into a
  separate F# net10 assembly with no Orleans codegen, so they take the exact fallback serialization
  path). All three **propagated across the Orleans 3.7.2 grain boundary on .NET 10 as the correct typed
  exceptions, messages intact, no BinaryFormatter throw**. Note: these types carry no custom fields and
  no `GetObjectData` override (they just delegate the `ISerializable` ctor to `base`), which is why they
  are safe; any *future* EggShell exception that adds custom serialized fields should be re-checked, but
  the current ones are clear. This closes the exception-edge item from entry (8).

  **Postgres (parked thought, NOT a blocker):** a Postgres backend is attractive (and would also dodge
  the Mac-ARM SQL Server pain at dev time), but EggShell leans on **SQL-Server-specific features that
  will need Postgres workarounds**: full-text search (SQL Server full-text catalog -> `tsvector`/
  `tsquery`), `ROWVERSION`/rowversion optimistic concurrency -> `xmin`/`bigserial`, geography indices ->
  PostGIS, T-SQL stored procs -> PL/pgSQL, `NVARCHAR ... COLLATE` -> `text`/collation. This is the
  storage/dialect layer, separate from the Orleans-runtime-on-net10 question proven above, and belongs
  with the coordinated Orleans-upgrade + Postgres workstream (see Section 3.3-equivalent seams). Revisit
  later; explicitly out of scope for the .NET 10 + Fable 5 move.
- **2026-06-28 (8)** **Backend spike: Orleans 3.7.2 builds AND runs on .NET 10. Both scary gates pass.**
  Built a minimal Orleans 3.7.2 app (`/Volumes/HomeX/shafayat/Code/orleans-net10-spike`, C#) with **no
  SQL Server** (localhost clustering + in-memory grain storage, sidestepping the Mac-ARM SQL concern).
  **Build gate PASS:** Orleans 3.7.2's legacy `Orleans.CodeGenerator` MSBuild task ran on the net10 SDK
  and built clean (only `SYSLIB0051`, the obsolete `ISerializable` ctor warning, not an error). **Runtime
  gate PASS:** silo started on **.NET 10.0.9**, a grain arg/return round-trip worked
  (`Payload Count 41 -> 42`), and a custom `[Serializable]` exception with the `ISerializable` ctor
  **propagated back typed across the grain boundary with no BinaryFormatter throw**. That exception path
  was the prime suspect, because **BinaryFormatter is fully removed on .NET 9+** and would have thrown
  `PlatformNotSupportedException` if Orleans 3.7 hit it. Corollary: **net10 is the *harder* target** (BF
  removed entirely vs merely disabled-by-default on net8), so passing on net10 means the net8 "safer
  intermediate" is comfortably safe and likely not a required stepping stone.

  **Revised backend verdict: "TFM bump to net10, Orleans frozen at 3.7.2" is VIABLE for the Orleans
  runtime** — the two gates that looked most dangerous in static analysis both pass. Remaining items are
  narrower than the original MODERATE-HARD framing and are mostly *not* about Orleans-on-net10:
  (1) **SQL Server storage + ADO.NET clustering + reminders** on the target platform/DB were deliberately
  not tested (Mac-ARM SQL caveat; and this layer is really the Postgres/Orleans-upgrade workstream);
  (2) **EggShell's full type surface** — though its custom `IExternalSerializer` (gzip STJ) shields app
  types, so the spike confirms the riskier *fallback* path is fine; exotic `ISerializable`-with-custom-
  fields exceptions are the one thing worth a targeted check against the real codebase;
  (3) **ASP.NET Core 10** vs `Giraffe 6.2` / SignalR server (independent of Orleans).
- **2026-06-28 (7)** **SignalR end-to-end verified, and the wrapper question resolved.** Investigation:
  the public `Fable.SignalR` is abandoned at **0.11.5, pinned to `Fable.Core [3.2.8,4.0.0)`** (Fable
  3-era). EggShell's referenced `Fable.SignalR 0.16.0` / `Fable.SignalR.AspNetCore 0.14.0` are **not on
  public NuGet** — they are private builds from the (now commented-out) internal Azure feed, sitting in
  the global cache, and even those depend on **`Fable.Core 3.7.1`** (still Fable 3-era). So both the
  public and the private wrappers predate Fable 4, let alone Fable 5. Conclusion: **do not depend on the
  Fable.SignalR wrapper for Fable 5; bind Microsoft's maintained `@microsoft/signalr` client directly
  from F#** (a ~30-line thin binding). **Verified end to end:** built a minimal **.NET 10 ASP.NET Core
  SignalR server** (`signalr-server/`, stock `Microsoft.AspNetCore.SignalR`) that broadcasts a `tick`
  every second, and an **F#/Fable 5 client** (`fsharp/SignalRProbe.fs`, direct `@microsoft/signalr`
  bindings + `Hooks.useEffectDisposable`). On web the client connected over **WebSocket to the hub**,
  `status: connected (live push)`, and the rendered counter incremented **251 -> 254 over 3.5s** (the
  server's 1/sec rate). No wrapper, no JS shim, F#-only client code. **So the real-time subscription
  transport is fully viable on the modern stack**; the migration cost is writing thin direct bindings
  (server side stays stock ASP.NET Core SignalR, which tracks .NET). This supersedes the
  "Fable.SignalR Fable-5 availability to confirm" caveat in entry (6).
- **2026-06-28 (6)** **Native Android self-verified. SPIKE CONCLUSIVELY PASSES.** Booted the
  `Medium_Phone_API_35` emulator, ran the app in **Expo Go (SDK 56, RN 0.85, React 19, New
  Architecture)** with no native build needed (stock modules only), and drove it via `adb` (input +
  screencap, no Appium required). Screenshots confirmed the full round trip on a real device: red box
  **translucent pink (opacity 0.3) at load** (worklet applies the initial shared value), **solid red
  (~1.0) while held-dragging** (RNGH pan gesture drives the shared value), and **back to pink after
  release** (`onFinalize` spring-back). Blue Moti box solid; all F# text rendered. Only the benign
  `fable-library-js` `Require cycle` LogBox warning. **Definitive result: a Reanimated worklet AND an
  RNGH gesture authored entirely in F# run on the UI thread on native, no JS shim.** Combined with the
  web run, every probe (A primitives, B Moti declarative, C Reanimated worklet, D gesture) passes on
  **both web (react-native-web) and native**.

  **Spike conclusion: GO.** The F# -> Fable 5 -> React Native (New Architecture) + react-native-web +
  Reanimated 4 + RNGH 3 + Moti path is validated end to end. App code can stay 100% F#; no JS worklet
  shim is required for this pattern. Known costs to carry into the real migration (none blocking):
  `Fable.React` Fable-5 build is prerelease (`10.0.0-alpha.1`); the custom `Meta/FablePlugins` needs its
  ~3 small Fable-5 edits; `Fable.SignalR` Fable-5 availability to confirm; and the backend net10 + Orleans
  story remains the separately-hard, deferred piece (Section 17). Spike folder
  `/Volumes/HomeX/shafayat/Code/eggshell-rnw-spike` can be deleted once this entry is reviewed.
- **2026-06-28 (5)** **Web runtime self-verified with Playwright. All four probes pass.** Ran the app
  in a headless browser (`verify-web.mjs`) and read computed styles: Probe A, all F# `Text` labels
  render via RNW; Probe B, the Moti blue box sprang in (opacity 1, width 120); Probe C/D, the red box's
  Reanimated-driven opacity measured **0.3 at load** (worklet applies the initial shared value),
  **0.999 while dragging** (RNGH pan gesture drives the shared value), and **0.3 after release**
  (`onFinalize` spring-back). **Headline: a hand-authored Reanimated worklet written in F# works end to
  end on web with no JS shim.** Only benign console noise (`Require cycle` warnings from
  `fable-library-js`, a known harmless Fable artifact). Note: the earlier "red box stays opaque" was a
  demo bug (no reset on release), not a toolchain failure; fixed by adding `onFinalize`. Tooling note:
  Playwright for web self-verification works well; native probes would use Appium (Android) and an iOS
  UI-automation path (XCUITest via Appium, or Maestro). **Remaining: native simulator run to confirm
  the worklet executes on the UI thread (web uses Reanimated's JS fallback, so web proves the API
  wiring and that the Fable-emitted worklet is accepted, but not literal UI-thread execution).**
- **2026-06-28 (4)** **Web bundle succeeded.** `npx expo export --platform web` produced a working
  bundle (`Web Bundled, 1082 modules, ~2MB`). The full pipeline assembled end to end: F# -> Fable 5 ->
  JS, plus react-native-web + Reanimated 4.3.1 + react-native-gesture-handler 2.31.1 +
  react-native-worklets 0.8.3 + Moti 0.30 + react-dom 19.2.3. **Key result: the Reanimated worklets
  Babel plugin transformed the Fable-emitted `useAnimatedStyle(() => ({...}))` closure during bundling
  without error** (the biggest build-phase unknown, now cleared). Minor fix needed: `babel-preset-expo`
  had to be added explicitly. **Remaining (runtime, not build):** (a) open the web bundle in a browser
  to visually confirm probes A (primitives) and B (Moti spring) render and animate; (b) run a
  simulator for the true UI-thread worklet probes C and D (does the Fable-authored worklet actually run
  on the UI thread, or do we need the `js/worklets.js` Plan-B shim). Conclusion so far: **the F# -> RNW
  + modern-animation toolchain is viable on Fable 5 / .NET 10; no blocker found in the build path.**
- **2026-06-28 (3)** Spike toolchain proven on Fable 5 / .NET 10. Installed the .NET 10 SDK (10.0.301,
  runtime 10.0.9) via Microsoft's `dotnet-install.sh` side-by-side in `~/.dotnet`. **Environment note:**
  on first install the .NET 10 host SIGKILLed (exit 137) on this machine and temporarily broke all
  `dotnet` invocations (the CLI rolls forward to the newest runtime); it was resolved and .NET 10 now
  runs. Once working, **Fable 5.4.0 installs and runs on .NET 10**, and `dotnet fable` compiled the F#
  spike (`fsharp/Spike.fsproj`) to clean JS in ~1.8s. Emitted output is idiomatic: all RN / Moti /
  Reanimated / RNGH bindings resolved, and the worklet probe compiled as `useAnimatedStyle(() => ({...}))`
  (a bare arrow function, the shape the Reanimated Babel plugin needs to auto-detect; the
  does-it-run-as-a-worklet question is now purely a device-runtime check). **Ecosystem friction
  confirmed:** the Fable-5-compatible `Fable.React` is currently a prerelease, `10.0.0-alpha.1` (no
  10.0.0 stable yet); `Fable.Core` 5.0.0 resolved fine. Next: `npm install` + `npx expo install` the RN
  animation deps, then run web (RNW) to exercise probes A and B, and a simulator for the native worklet
  probes C and D.
- **2026-06-28 (2)** Added Sections 16-19. New research folded in: React Strict DOM (keep RNW as the
  runtime target now, shape the F# API DOM-flavored so a later RSD switch is cheap); Fable 4 vs 5 plus
  the .NET 10 / Orleans decoupling analysis (frontend Fable 5 and backend Orleans are separable; the
  hard coupling is Orleans 3.7.2 on net10 due to BinaryFormatter; net8 is the safer intermediate; the
  custom `Meta/FablePlugins` plugin is the main frontend upgrade risk); non-React F# alternatives
  (Avalonia + FuncUI is the only real contender, but its in-process type-sharing prize does not survive
  the web target, and RN is actually more native on mobile than the self-drawn frameworks). Scaffolded
  the spike at `/Volumes/HomeX/shafayat/Code/eggshell-rnw-spike` (Expo SDK 56 / RN 0.85.3 / React 19.2,
  four probes). **Confirmed empirically** that Fable 5.4.0 cannot install/run on the .NET 7/8-only
  machine (needs the .NET 10 SDK). Next step: decide .NET 10 SDK install vs Fable 4.x fallback, then run
  the spike.
