# Accessibility Backlog

Tracked work items for accessibility improvements. Items are tagged by migration safety:

- `[safe]` -- uses RN-native APIs; survives the ReactXP to RNW migration unchanged. **Build now.**
- `[web-only]` -- only meaningful on web. Build behind the web seam when the migration lands.
- `[rnw-blocked]` -- blocked until the RNW/New-Architecture host-element seam exists. See
  [ReactXP to RNW](./modernization/reactxp-to-rnw.md). Defer; do not hand-roll.
- `[lib]` -- needs a third-party native library. Wire via the `ThirdParty` recipe after the migration
  settles the dependency stack.

For context on why deferred items are sequenced, not abandoned, see the
[post-migration target architecture](#post-migration-target-architecture) section below.

---

## Field-reported defects (real device, POCO F1, 2026-07-02)

Found during real-touch testing of the standalone build; detail + reproduction in the
[Engineering Log](./knowledge-base/engineering-log.md) (sessions 6-7). Swipe gesture/scroll defects
were fixed in session 7 (native swipe rebuilt on react-native-gesture-handler); a11y-specific items
below remain.

- **TalkBack cannot focus the Priority picker to change selection.** `[safe]` -- the picker field
  (`@react-native-picker/picker` via the `Input.Picker` seam) is not exposing focusable/actionable
  semantics to the screen reader. Ensure the field is a focusable control with a role and an
  `accessibilityLabel`, and that opening/selecting is reachable without sight.
- **Controls go inert under reduce-motion.** `[safe]` -- when reduce-motion is on, `SegmentedControl`
  renders a plain `RX.View` (no `onTap`/`onPan`) and `TodoSwipeShell` renders a static row, so a
  reduce-motion user cannot toggle the theme or use the row actions via the normal path. Reduce-motion
  must remove *animation*, not *interaction* (keep the control tappable; just skip the sliding thumb /
  swipe animation). Violates the "any gesture has a non-gesture alternative" principle in
  [Recipes](./accessibility/recipes.md).
- **Swipe-to-delete has no robust non-gesture alternative** (scroll-hijack part **done**). `[safe]` --
  vertical-scroll hijack and gesture jank were fixed in session 7 by rebuilding the swipe on
  react-native-gesture-handler (`RX.HorizontalPanArea`; native `activeOffsetX`/`failOffsetY`
  arbitration). Still open: add an `accessibilityAction` "delete" (rotor action so a screen-reader
  user can delete without the swipe). See the `accessibilityActions` note in
  [post-migration target architecture](#post-migration-target-architecture).
- **Native text-selection cursor sometimes appears while swiping over a card** (session 7). `[safe]` --
  the OS text-selection/magnifier occasionally engages on the row's `Text` during a horizontal pan.
  Fix: make row text non-selectable (`userSelect: none` / `selectable={false}`) or suppress selection
  while the pan is active.

---

## Small (hours)

5. AppTodo: label toggle/tabs/chips/rows/counts per [Recipes §3](./accessibility/recipes.md#3-per-component-playbook).
   `[safe]` -- **Done** (swipe delete has title context; tabs/chips/counts/rows all have roles wired; `HandheldListItem` now emits `role=ListItem` on outer view)

---

## Medium (approximately one day each)

9. **Bake role/state into primitives** -- **Done.** Tab/Tabs (TabList/Tab), Button, Heading (Header), Checkbox (CheckBox), SegmentedControl (Radio/RadioGroup), HandheldListItem (ListItem) all emit roles automatically. `[safe]`

12. Honor bold-text / reduce-transparency in theming (heavier weights, solid fills). `[safe]` -- **Partially done** (`LC.With.BoldText` / `LC.With.ReduceTransparency` hooks available; auto-application to `LC.Text`/`LC.UiText` requires making them `[<Component>]` or a reactive theme context -- deferred as larger architectural work)

---

## Large (multi-day; some gated)

14. RTL / logical-layout pass (`start`/`end` vs `left`/`right`; text expansion). Partly `[rnw-blocked]` -- not started; logical CSS properties available in RNW but not yet adopted
15. Destructive-action **undo** pattern (snackbar plus announce) framework-wide. `[safe]` -- not started
16. Programmatic focus management: basic utility done (`LC.FocusManager.setFocusTo` / `setFocusById`). `[safe]` -- **Partially done**. Wiring to route changes and dialog open/close still needed.
17. Roving-tabindex keyboard navigation for tablists and menus. `[web-only]` -- not started. Tabs keyboard arrow nav deferred: current Tab.fs emits dual `role="tab"` nodes (outer View + inner Pressable) making `querySelectorAll("[role='tab']")` ambiguous; needs Tab API revision.
18. Landmarks, skip links, and heading levels. `[web-only]` -- **Done.** `role=Main` on content block; `role=Navigation` on topNav/bottomNav; `LC.SkipLink` auto-inserted by AppShell; `:focus-visible` ring CSS injected. Landmark roles added to `AccessibilityRole` enum.
19. Media captions and haptic feedback when media/notifications are added. `[lib]` -- gated on expo-haptics / react-native-video
20. Accessibility CI: axe-core (Playwright, web) plus TalkBack/VoiceOver manual checklist; contrast
    lint in CI. -- not started

---

## Deferred / do-not-hand-roll

These items are sequenced, not abandoned. Deferring is a deliberate sequencing decision; the migration
unlocks them cleanly.

| Item | Tag | Status | What unlocks it |
|------|-----|--------|-----------------|
| Skip links | `[web-only]` | **Done** (`LC.SkipLink` auto-inserted by AppShell; targets `#eggshell-app-content`) | -- |
| `:focus-visible` ring styling | `[web-only]` | **Done** (`FocusVisibleStyles.injectIfNeeded` injects CSS on AppShell mount) | -- |
| Landmarks + heading levels | `[web-only]` | **Done** (`role=Main`, `role=Navigation` on AppShell; `role=Complementary` available) | -- |
| Roving-tabindex mechanics | `[web-only]` | **Deferred** -- Tab.fs dual role=tab nodes (View + Pressable) need API revision first | Tab API cleanup |
| Accessible modals (focus trap, `inert`, light-dismiss) | `[rnw-blocked]` | Deferred | RNW `<dialog>` / Popover API |
| Haptics | `[lib]` | Deferred | `expo-haptics` |
| Media captions | `[lib]` | Deferred | `react-native-video` text tracks |
| Reduced-motion-aware animation | `[rnw-blocked]` (rich) | Deferred | Reanimated 4 / Moti |
| RTL logical properties | `[rnw-blocked]` (partly) | Deferred | New Arch logical CSS properties |

**Never patch vendored RN/RNW internals for accessibility.** Fix at the F# seam only (see the context
key-warning fix in `AppShell/Context/Context.fs`, documented in the Engineering Log (2026-06-29)).

---

## Post-migration target architecture

The ReactXP to RNW migration is not just parity -- it **raises the accessibility ceiling** and retires
most of the deferred items above. Full details in [ReactXP to RNW](./modernization/reactxp-to-rnw.md).

### React Native New Architecture (Fabric / JSI / TurboModules)

- **Synchronous layout (Fabric)** makes reliable focus management possible: accurate measurement and
  `AccessibilityInfo.setAccessibilityFocus(findNodeHandle ...)` on route change and dialog open/close.
  Unblocks backlog #16 toward `[safe]`.
- TurboModule `AccessibilityInfo` exposes the full flag set and change events cheaply, powering the
  `With.Accessibility` hook (#7) on solid ground.

### Modern RN accessibility prop surface

RN converged on a cross-platform vocabulary the wrapper should map once:

- W3C **`role`** (superset of `accessibilityRole`); **`aria-*`** props that work on iOS, Android, and
  web: `aria-label`, `aria-labelledby`, `aria-live`, `aria-modal`, `aria-hidden`, `aria-expanded`,
  `aria-selected`, `aria-checked`, `aria-busy`, `aria-valuenow/min/max`.
- `accessibilityLanguage` (per-node language, WCAG 3.1.2); `accessibilityActions` plus
  `onAccessibilityAction` (custom rotor actions -- for example, "delete" without the swipe); queued
  `announceForAccessibility`.

The F# `LC.Pressable` / `RX.View` call sites stay the same; the **binding** maps them to `role` plus
`aria-*` natively. This is the migration contract paying off (backlog #8, #15 become richer for free).

### react-native-web: real ARIA and DOM

RNW renders true DOM, making the deferred web items first-class:

- **`:focus-visible`** keyboard rings; **landmarks** via semantic host elements; **heading levels**;
  **skip links**; **roving-tabindex** via real focus order. Unblocks backlog #17, #18.
- **`inert`** for modal backgrounds; native **`<dialog>`** and the Popover API (top-layer, focus-trap,
  Esc, light-dismiss) -- accessible modals and menus for free instead of hand-built focus traps.
- **`prefers-reduced-motion` / `prefers-contrast` / `prefers-reduced-transparency` / `color-scheme` /
  `forced-colors`** media queries resolve to real web signals.

### React 19

- **Form Actions / `useActionState`** -- accessible submit with built-in pending/error state to
  announce (pairs with form-error live regions from [Recipes D](./accessibility/recipes.md#d-text-input)).
- **Document metadata / `<title>`** per route -- page titles (WCAG 2.4.2) on web.
- **Ref-as-prop** simplifies focus refs; improved Suspense yields predictable, announceable loading
  states.

### Modern motion / gesture / media stack

- **Reanimated 4 + Moti** run on the UI thread and read the reduce-motion flag declaratively: motion
  accessibility (see [Spectrum](./accessibility/spectrum.md#vestibular--motion-sensitivity)) becomes a property of
  the animation API, not per-screen code. Unblocks backlog #11 fully.
- **RNGH** gives robust swipe/drag with a clean place to attach the required non-gesture alternative
  (tap/keyboard/rotor action) for WCAG 2.5.1. Unblocks the gesture parts of backlog #4 / #15.
- **Captions/haptics** (`react-native-video` text tracks, `expo-haptics`) land the `[lib]` items on a
  supported stack.

### Color and i18n

- Emerging CSS (`color-contrast()`, relative color syntax) on web for guaranteed-contrast theming.
- **Logical properties** (`margin-inline-start`, `inset-inline`) and writing-direction-aware flexbox on
  New Arch make the RTL/logical-layout pass (#14) `[safe]`.

### Net effect

After migration, the deferred set largely resolves: focus management, landmarks, skip links,
`:focus-visible`, roving-tabindex, accessible modals (`<dialog>` / `inert`), reduced-motion-aware
animation, captions/haptics, and RTL all move to **implemented** -- most behind the **same F# call
sites** written today. The investment now in `[safe]` semantics (backlog #9) and the migration contract
is exactly what makes that transition free at the app layer.
