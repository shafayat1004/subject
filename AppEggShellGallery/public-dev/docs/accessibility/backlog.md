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

## Small (hours)

1. Decorative `prefixIcon` (search magnifier) rendered `importantForAccessibility = No`. `[safe]`
2. `Input.Text` `?accessibilityLabel` passthrough for placeholder-only inputs. `[safe]`
3. Search wrapper `role = Search`. `[safe]`
4. Touch-target audit: icon buttons and `TextButton` must be at least 44px. `[safe]`
5. AppTodo: label toggle/tabs/chips/rows/counts per [Recipes §3](./accessibility/recipes.md#3-per-component-playbook).
   `[safe]`
6. Audit fixed `height`/clipping that breaks text scaling (for example, single-line input `height 21`).
   `[safe]`

---

## Medium (approximately one day each)

7. `With.Accessibility` reactive hook exposing OS flags (reduce-motion, bold, screen-reader,
   transparency, invert/grayscale, font scale) -- see [Platform Settings](./accessibility/platform-settings.md). `[safe]`
8. `LibClient.Accessibility.announce : string -> unit` over `announceForAccessibility` / web live
   region. `[safe]`
9. **Bake role/state into primitives** (`Tab`/`Tabs`, `Checkbox`, `Picker`, `Button`, `Heading`) so
   apps get semantics free. Highest-leverage item. `[safe]`
10. `LC.Group` / `LC.RadioGroup` wrappers (container role plus label in one call). `[safe]`
11. `LC.With.ReducedMotion` (subset of #7) for the animation layer to consume. `[safe]`
12. Honor bold-text / reduce-transparency in theming (heavier weights, solid fills). `[safe]`
13. **Contrast tooling** in `ColorModule`: relative-luminance and contrast-ratio helpers; a dev-time
    palette linter asserting AA on `SemanticPalette`. `[safe]`

---

## Large (multi-day; some gated)

14. RTL / logical-layout pass (`start`/`end` vs `left`/`right`; text expansion). Partly `[rnw-blocked]`
15. Destructive-action **undo** pattern (snackbar plus announce) framework-wide. `[safe]`
16. Programmatic focus management on route change and dialog open/close: basic native binding `[safe]`;
    polished version (roving tabindex, `inert`) `[rnw-blocked]`.
17. Roving-tabindex keyboard navigation for tablists and menus. `[web-only]`
18. Landmarks, skip links, and heading levels. `[web-only]` / `[rnw-blocked]`
19. Media captions and haptic feedback when media/notifications are added. `[lib]`
20. Accessibility CI: axe-core (Playwright, web) plus TalkBack/VoiceOver manual checklist; contrast
    lint in CI.

---

## Deferred / do-not-hand-roll

These items are sequenced, not abandoned. Deferring is a deliberate sequencing decision; the migration
unlocks them cleanly.

| Item | Tag | Why deferred | What unlocks it |
|------|-----|--------------|-----------------|
| Skip links | `[web-only]` | Web-DOM-shaped; depends on the RNW host-element seam | ReactXP to RNW swap |
| `:focus-visible` ring styling | `[web-only]` | CSS-only; no equivalent in ReactXP | ReactXP to RNW swap |
| Landmarks + heading levels | `[web-only]` / `[rnw-blocked]` | Semantic HTML elements; depend on host-element seam | RNW |
| Roving-tabindex mechanics | `[web-only]` | Real DOM focus management | RNW + New Arch |
| Accessible modals (focus trap, `inert`, light-dismiss) | `[rnw-blocked]` | Needs native `<dialog>` / Popover API | RNW |
| Haptics | `[lib]` | Needs `expo-haptics` or equivalent | Post-migration dependency stack |
| Media captions | `[lib]` | Needs `react-native-video` text tracks | Post-migration dependency stack |
| Reduced-motion-aware animation | `[rnw-blocked]` (rich) | Reanimated 4 / Moti on New Arch | Post-migration |
| RTL logical properties | `[rnw-blocked]` (partly) | `margin-inline-start` / `inset-inline` on New Arch | New Arch |

**Never patch vendored ReactXP internals for accessibility.** Fix at the F# seam only (see the context
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
