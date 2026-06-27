# EggShell Frontend Modernization Plan

Consolidated reference for **render-DSL retirement (Goal A)**, **accessibility + UI observability**, and **automation/OS support**. Synthesized from `EGGSHELL_ARCHITECTURE.md` §12, `CLAUDE.md`, `LEARNINGS.md`, `LibClient/ACCESSIBILITY.md`, and gallery audit work (through 2026-06-27).

---

## 1. North-star goals (current initiative)

From `EGGSHELL_ARCHITECTURE.md` §12 and `CLAUDE.md`. **Goals F–H are explicitly deferred** (stay on Fable v4, current ReactXP fork).

| Goal | Summary | Status |
|------|---------|--------|
| **A** | Retire render DSL; convert framework `.render` → pure F# `[<Component>]` | **In progress** (~40 `.render` left in LibClient; see §5) |
| **B** | Fix `eggshell create-app` scaffolding (modern app, no `.render`) | Not started (this phase) |
| **C** | Reduce component verbosity (hooks, fewer Estate/Pstate/Actions) | Incremental alongside A |
| **D** | Standardize frontend directory structure | Incremental alongside A |
| **E** | Speed up frontend build | Partial wins; big win when DSL retired |

**Cross-cutting (same phase, not separate goals):**

- **Accessibility (a11y):** semantic press targets, roles/labels/state, live regions, keyboard/focus (later).
- **Observability:** dev-only action log + UI snapshot for debugging and automation.
- **UI automation:** stable `testId`s, gallery audits (web Playwright + Android Appium), testId-first navigation.

These reinforce Goal A: every conversion should land Pressable + labels + testIds, not copy legacy Render.fs soup.

---

## 2. Two parallel workstreams

```mermaid
flowchart TB
  subgraph goalA [Goal A - Render DSL to F#]
    ReadTrio[Read typext + styles + render]
    WriteFs[Single Foo.fs with Component attribute]
    Theme[Migrate Themes.Set + callers end-to-end]
    Delete[Delete render/typext/styles/autogen]
    Build[dotnet build Web Debug green]
    ReadTrio --> WriteFs --> Theme --> Delete --> Build
  end

  subgraph a11y [A11y + Observability + Automation]
    Pressable[LC.Pressable primitive]
    RxBind[RX a11y prop forwarding]
    UiLog[UiActionLog + uiSnapshot]
    TestIds[Gallery testIds]
    Audits[Web + Android audit scripts]
    Pressable --> RxBind --> UiLog --> TestIds --> Audits
  end

  goalA --> a11y
```

**Shared root cause:** interactive UI was **visual layer + invisible hit target** (`LC.Pointer.State` + `LC.TapCapture` → unlabeled `RX.Button`). Fix once with `LC.Pressable`, then convert components to pure F# that use it.

---

## 3. Accessibility and observability plan (phases)

### Phase 0 — Source review (done)

- `@chaldal/reactxp` already exposes `CommonAccessibilityProps` (role, state, label, live region, etc.).
- Work is **additive F# forwarding** on `RX.View` / `RX.Button` / `RX.ScrollView`, not a fork patch.
- `accessibilityHint` is **not** in this fork (skip for v1).
- `testId` was missing on some bindings; added where needed.

### Phase 1 — Core infrastructure (done)

| Artifact | Path | Role |
|----------|------|------|
| Accessibility types | `LibClient/src/Accessibility.fs` | Roles, state record, live region enums |
| Prop helpers | `LibClient/src/AccessibilityHelpers.fs` | Apply a11y props to JS objects |
| **Pressable** | `LibClient/src/Components/Pressable.fs` | Labeled semantic button; overlay + semantic modes; drag threshold; dev logging |
| TapCapture shim | `LibClient/src/Components/TapCapture.fs` | Thin wrapper → Pressable (delete when call sites migrated) |
| Live region | `LibClient/src/Components/LiveRegion.fs` | `LC.LiveRegion.announce` |
| **UiActionLog** | `LibClient/src/UiActionLog.fs` | Ring buffer, interactive registry, route tracking |
| RX forwarding | `LibClient/src/ReactXP/Components/View/View.fs`, `Button.fs`, ScrollView | `accessibilityLabel`, `accessibilityRole`, `accessibilityState`, `testId` |
| Route logging | `LibRouter/.../LogRouteTransitions.typext.fs` | `UiActionLog.setCurrentRoute` |
| Docs | `LibClient/ACCESSIBILITY.md` | API + migration checklist |

**Dev hook (DEBUG only):**

```fsharp
LibClient.UiActionLog.installGlobalHook Fable.Core.JS.globalThis "YourAppName"
// window.__eggshell.YourAppName.uiLog()
// window.__eggshell.YourAppName.uiSnapshot()
```

### Phase 2 — Tier-1 press-target migration (done)

Completed items:

- **IconButton:** required `label` at hand-written F# call sites (Quantity, Date, Carousel, ImageViewer, Legacy TopNav IconButton).
- **TextButton:** migrated to `LC.Pressable` with label + role.
- **Button, Nav.Top.Item, Sidebar.Item:** full **`[<Component>]` conversion** (not `.render` patches) with `LC.Pressable`, accessibility labels, component names for logging.
- **ShowSidebarButton:** pure F#; `testId="eggshell-sidebar-menu"`.
- **ToggleButton:** Pressable arg-order fix (FS0691).

**Deferred from Phase 2:** Focus/keyboard v1 (dialog focus trap, drawer trap) — cancelled for v1; Escape on dialogs still works via existing `Dialog.Base`.

### Phase 3 — Gallery testIds + observability hooks (mostly done)

| testId | Element |
|--------|---------|
| `eggshell-sidebar-menu` | Handheld sidebar menu (`Nav.Top.ShowSidebarButton`) |
| `sidebar-blade-components` | Components blade (fixed-top sidebar) |
| `sidebar-component-{CaseName}` | Component nav item |
| `sidebar-scroll-middle` | Middle scroll region in sidebar |
| `aesg-sample-visuals` | Component sample wrapper (`ComponentSample`) |

Gallery sidebar blade testIds live in `SidebarContent.fs` (pure F#). **`Sidebar.render` is still render DSL** — full gallery sidebar page conversion pending.

### Phase 4 — Audit harness (partial)

- **Web:** `AppEggShellGallery/audit-gallery-interactive.mjs`, assertions, visual archive, full crawl — documented in `GALLERY-AUDIT.md`.
- **Android:** Appium/WebdriverIO facade; testId → `resource-id` mapping; menu-tap nav (no edge-swipe); drawer reopen per navigation.
- **Blocked until testIds exist** on all nav paths; Phase 3 unblocked primary flows.
- **Not done:** full audit rewrite to prefer `uiSnapshot()` over heuristics; interactive Android audit with testId nav not fully validated end-to-end.

### Phase 5+ (future)

- Migrate remaining `LC.TapCapture` call sites → `LC.Pressable` (~15+ in LibClient `.render` trees).
- Delete `TapCapture.fs` when zero callers.
- Keyboard/focus: `restrictFocusWithin`, roving tabindex on nav.
- Optional: sampled production telemetry (tiered sinks); v1 is **dev-only** `UiActionLog`.

---

## 4. Render-DSL → F# conversion: best practices

**Authoritative detail:** `LEARNINGS.md` (2026-06-26 recipe, 2026-06-27 anti-patterns). **Rules:** `CLAUDE.md` §7–10.

### 4.1 Target end-state (Goal A)

One file per component:

```fsharp
[<AutoOpen>]
module LibClient.Components.Foo   // or Nav_Top_Item for nested paths

open Fable.React                // MANDATORY for [<Component>]
open LibClient
open ReactXP.Components
open ReactXP.Styles             // NOT ReactXP.LegacyStyles (shadows make*Styles CEs)

module private Styles = ...       // makeViewStyles / makeTextStyles

type LC.Foo.Theme = { ... }     // or nested under module LC

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Foo(...) : ReactElement = ...
```

**Delete:** `Foo.render`, `Foo.typext.fs`, `Foo.styles.fs`, `_autogenerated_/Components/Foo/*`, `RegisterRender` / `RegisterStyles` entries (regenerated by `eggshell build-lib`).

### 4.2 What NOT to do

| Anti-pattern | Why |
|--------------|-----|
| Copy `_autogenerated_/.../Foo.Render.fs` into `Foo.fs` | Keeps `findApplicableStyles`, `__parentFQN`; not modernization; no Pressable/a11y |
| Keep `.typext.fs` beside new `Foo.fs` | Old trio; public types belong in the same file |
| Patch `.render` for a11y/testIds/behavior | Rule 7: convert to pure F# instead |
| Leave `FooStyles.Theme.Customize` in `DefaultComponentsTheme` while also adding `Themes.Set` | Parallel legacy shims; upgrade **all callers** in the same change |
| `open ReactXP.LegacyStyles` in modern files | Breaks `makeViewStyles` CE (FS0041 yield errors) |
| Declare `State`/`Actionable` at module top with `[<AutoOpen>]` | Leaks union cases globally; collides with `DefaultComponentsTheme` |

### 4.3 Per-component checklist

1. **Preflight:** `grep -rn "FooStyles" --include="*.styles.fs"` repo-wide. If external consumers exist, plan a **cluster** (§4.5) or end-to-end caller migration — not a compat shim left indefinitely.
2. Read trio: `.typext.fs`, `.styles.fs`, `.render`, generated `.Render.fs` (semantic reference only).
3. Pick template: `Tabs.fs`, `Tab.fs`, `HandheldListItem.fs`, `Nav/Top/Heading/Heading.fs`, `Sidebar/Base/Base.fs`, `Button.fs`.
4. Write single `Foo.fs`: port styles to `module private Styles`; translate render tree to `RX.*` / `LC.*`, `elements { }`, `match` for `rt-match`.
5. **Theming:** `Themes.Set<LC.Foo.Theme>({ ... })` in `DefaultComponentsTheme.fs`; component uses `Themes.GetMaybeUpdatedWith theme`.
6. **End-to-end callers:** migrate every `FooStyles.Theme.*` consumer to `?theme` or direct style arrays; then delete `Foo.styles.fs`.
7. **Pressable:** interactive components use `LC.Pressable` (label, role, optional testId, pointerState when using `LC.Pointer.State`).
8. **Signature compat (temporary):** optional `?xLegacyStyles` bridge until all callers converted; remove when grep is clean.
9. Update `.fsproj` compile order; run `eggshell build-lib` + `dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"`.
10. **Gallery:** mirror in `AppEggShellGallery/src/Components/Content/` as pure F# (see `Nav/Top.fs`, `Content_Grid/Grid.fs`).

### 4.4 Nested namespace pattern

For `LC.Nav.Top.Item`, `LC.Sidebar.Item`:

```fsharp
namespace LibClient.Components.Nav.Top
module Item =
    type State = ...
    type Style = ...
    let iconOnly = ...

namespace LibClient.Components
[<AutoOpen>]
module Nav_Top_Item =
    module LC.Nav.Top.Item =
        type Theme = ...
    type Constructors.LC.Nav.Top with
        [<Component>] static member Item(...) = ...
```

Module name uses **underscores** (`Nav_Top_Item`), not dots, to avoid `LC` module clashes (FS0248).

### 4.5 Dependency strategies

Legacy `*Styles.Theme` APIs form a **graph** (~18 producers consumed from other `.styles.fs` files).

| Strategy | When | Rule today |
|----------|------|------------|
| **Clean leaf** | Zero external `FooStyles` consumers | Convert alone (e.g. early `Tabs`) |
| **Cluster (A)** | Parent styles child internals via class cascade | Convert producer + consumers together; producer gets `?topStyles` / `?theme` / section style params |
| **Compat shim (B)** | — | **Rejected** for new work; migrate all callers, delete shim in same PR |

**Validated cluster:** `VerticallyScrollable` + `Sidebar/Base`.

**Deferred clusters:** `LabelledFormField` (AppUserManagement), `Nav.Top.Base` + nav items, `Input.Text` / form fields, `Badge` / `Button` consumers across dialogs.

### 4.6 Archetype recipes (from conversions)

| Archetype | Pattern | Example |
|-----------|---------|---------|
| Responsive | `LC.With.ScreenSize` + branch inside `makeViewStyles` | `Section/Padded` |
| Pseudo-stateful | No hooks; derive from props each render | `HandheldListItem`, many form wrappers |
| Genuinely stateful | `Hooks.useState` / `useEffectDisposable` | `TriStateful/Abstract`, `Grid`/`QueryGrid` |
| Rich control flow | `match` + child arrays `[| ... |]`, not `elements { match }` | `HandheldListItem` |
| Native vs web | `ReactXP.Runtime.isNative()` branches; no `dom.table` on RN | `LibUiAdmin/Grid` |
| FontWeight | Legacy `RulesRestricted.FontWeight` in `.styles.fs` / themes; modern `ReactXP.Styles.RulesRestricted.FontWeight` in `[<Component>]` | BackButton mapper |

### 4.7 Hard cases (bespoke or defer)

- **Draggable:** refs, gestures, animations — hooks rewrite, not mechanical.
- **Input.ChoiceList:** public type path changes break apps — plan app updates with cluster.
- **Dialogs / forms still on `.render`:** convert cluster when touching (e.g. ContextMenu Dialog uses `ButtonThemes` + `?theme` on `LC.Button` while dialog shell remains render DSL).

### 4.8 Build and validate

```bash
# From lib directory (LibClient, LibRouter, …)
DOTNET_ROOT=~/.dotnet ../eggshell build-lib    # render codegen + registration

dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"
dotnet build LibRouter/src/LibRouter.fsproj -c "Web Debug"

# Gallery render file after .render edit
cd AppEggShellGallery && ../eggshell renderdsl src/Components/Content/Button/Button.render

dotnet build AppEggShellGallery/src/App.fsproj -c "Web Debug"
```

**Gotchas:** `ComponentRegistration.fs` is auto-generated; `eggshell build-lib` omits pure-F# components. macOS has no `timeout`. Fable plugin errors when running `dotnet fable` directly on lib are expected; use app pipeline for full JS emit.

---

## 5. Current state (2026-06-27)

### 5.1 LibClient render DSL remaining

**~40** `.render` files under `LibClient/src/Components/`, including:

- **Inputs:** Text, Checkbox, ChoiceList, ChoiceListItem, Picker cluster (Picker, Field, Dialog, Popup, Base), Image, File, NamedFile, ParsedText, WeeklyCalendar
- **Dialogs:** Base, Confirm, Prompt, Shell variants, FullScreen
- **Nav (legacy layout):** Nav.Top.Base, Nav.Bottom.{Base,Item,Button}
- **Chrome:** AppShell/Content, Badge, FloatingActionButton, IconWithBadge, DateSelector, ContextMenu.{Dialog,Popup}, Draggable, Form/Base, LabelledFormField
- **Legacy:** Legacy.Sidebar.*, Legacy.TopNav.Base, Legacy.Input.*

### 5.2 LibClient converted to pure F# `[<Component>]` (representative)

**Layout / chrome:** Tabs, Tab, VerticallyScrollable, Sidebar.{Base, Item, Heading, Divider, WithClose}, Section/Padded, HandheldListItem, Card, FlexFiller, ScrollView, ItemList, …

**Nav (modern):** Nav.Top.{Item, Heading, Filler, Image, ShowSidebarButton}, Nav.Bottom.Filler

**Inputs (modern leaf):** Many `Input/*.fs` helpers (Date, Quantity, Guid, PhoneNumber, Duration, …) — not all composite pickers

**Interactive / state:** TriStateful.{Abstract, Simple}, QuadStateful, Pointer.State, Pressable, TextButton, ToggleButton, **Button**, IconButton

**Infrastructure:** Accessibility.*, UiActionLog, LiveRegion, AsyncData, With.*, …

**LibUiAdmin:** Grid, QueryGrid (native table fix + pure F#)

**LibRouter:** BackButton (uses `?theme` → `LC.Nav.Top.Item`)

### 5.3 End-to-end caller upgrades (latest)

**Button** and **Nav.Top.Item** no longer have parallel legacy modules:

- Deleted `Button.styles.fs`, `Nav/Top/Item/Item.styles.fs`
- Removed duplicate `ButtonStyles` / `ItemStyles` blocks from `DefaultComponentsTheme.fs`
- **Callers migrated:** ContextMenu Dialog (`ButtonThemes`), PickerInternals Dialog (dead styles removed), Nav.Bottom.Button (`BadgeStyles`), LibRouter BackButton, ShowSidebarButton, gallery Button (`SampleThemes`), gallery Nav/Top (`?theme` for icon adjust)

### 5.4 Gallery modernization

| Area | State |
|------|--------|
| Pure F# content pages | Nav/Top.fs, Grid, QueryGrid, Heading, Code, … |
| Still `.render` | Most `Components/Content/*`, Sidebar, TopNav, Route shells |
| Audit tooling | Web + Android scripts; testId-first nav documented |
| Known build issue | `Bootstrap.fs` `globalThis` (pre-existing) |
| Stale autogen | `_autogenerated_/Content/Nav/Top/Top.Render.fs` orphaned after Top.fs conversion |

### 5.5 TapCapture → Pressable

- **Done on:** Button, Nav.Top.Item, Sidebar.Item, TextButton, IconButton (component internals), Pressable/TapCapture shim
- **Still on TapCapture / invisible overlay:** many `.render` components (Nav.Bottom.Item, picker rows, HandheldListItem was converted — verify), ~15+ sites per last grep

### 5.6 Phase summary table

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | ReactXP a11y source review | Done |
| 1 | Pressable, UiActionLog, RX bindings, core types | Done |
| 2 | Tier-1 labels + Button/Nav.Top.Item/Sidebar.Item Pressable + F# conversion | Done |
| 3 | Gallery testIds + route/action logging | Mostly done |
| 4 | Audit harness testId-first | Partial (scripts exist; full validation pending) |
| — | End-to-end caller upgrade (no legacy shims) | Done for Button + Nav.Top.Item |
| — | Goal A bulk `.render` retirement | ~50% LibClient by file count; dependency clusters remain |

---

## 6. Recommended sequencing (next work)

1. **Cluster converts** (high fan-out): `Badge`, `Nav.Top.Base` + remaining top nav, `Input.Text` + LabelledFormField, `FloatingActionButton`.
2. **Gallery mirrors:** convert Content pages when touching LibClient twins; remove orphaned `_autogenerated_` pages.
3. **TapCapture sweep:** each `.render` conversion should finish with Pressable; grep `LC.TapCapture` until zero, delete shim.
4. **Automation:** run interactive Android audit with testId nav; extend assertions to use `uiSnapshot()` where possible.
5. **Do not start:** Fable 5, .NET 10, ReactXP swap, Orleans/Postgres (Goals F–H).

---

## 7. Key reference files

| Topic | Location |
|-------|----------|
| Architecture roadmap | `EGGSHELL_ARCHITECTURE.md` §12 |
| Agent/project rules | `CLAUDE.md` |
| Conversion gotchas + dated log | `LEARNINGS.md` |
| A11y API + checklist | `LibClient/ACCESSIBILITY.md` |
| Gallery audit how-to | `AppEggShellGallery/GALLERY-AUDIT.md` |
| Default theming | `LibClient/src/DefaultComponentsTheme.fs` |
| Templates | `LibClient/src/Components/Tabs/Tabs.fs`, `HandheldListItem/HandheldListItem.fs`, `Nav/Top/Heading/Heading.fs`, `Button/Button.fs`, `Nav/Top/Item/Item.fs` |

---

## 8. Definition of done (single component)

- [ ] Single `Foo.fs` with `[<Component>]`, no `.render` / `.typext.fs` / `.styles.fs`
- [ ] `Themes.Set<LC.Foo.Theme>` in `DefaultComponentsTheme.fs`; no parallel `FooStyles.Theme.Customize`
- [ ] All repo callers migrated (framework, LibRouter, gallery)
- [ ] Interactive surfaces use `LC.Pressable` with `label` (+ `testId` if automation-relevant)
- [ ] Gallery content page pure F# if component is showcased
- [ ] `dotnet build` Web Debug green for affected projects
- [ ] `LEARNINGS.md` entry if a new gotcha was discovered
