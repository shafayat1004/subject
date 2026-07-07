# Accessibility

Accessibility is **mandatory and the default** in EggShell. Every interactive element exposes a name,
role, and state; decorative content is hidden; text scales with OS settings; colors meet WCAG AA; touch
targets are at least 44px; every gesture has a non-gesture alternative; and dynamic changes announce via
live regions. This is not optional, not a follow-up task, and not "good enough without it."

> This section is reference and recipe material. For the platform migration context that unblocks
> web-specific items, see [ReactXP to RNW](./modernization/reactxp-to-rnw.md). For the frontend
> component model, see [Frontend Architecture](./architecture/frontend.md).

---

## Why accessibility is a framework concern

Apps should get correct accessibility **by using the primitives normally**. The framework's job is
threefold:

1. **Primitives emit correct semantics by default** -- role, name, state, scalable text, and honored OS
   settings come out of the box, with no per-call ceremony.
2. **Reusable hooks expose OS accessibility state** so that components adapt reactively to reduce-motion,
   bold-text, screen-reader-on, contrast, and font scale signals from the operating system.
3. **This documentation and the gallery** cover the recipes for app-specific bits.

Per-app hand-wiring of ARIA flags is a smell. If a developer writing idiomatic EggShell has to reach past
the framework primitives to wire up accessibility, that is a framework bug.

---

## The benchmark

The `ui2.html` mockup (in `SuiteTodo/AppTodo/suggestions/`) shows the target semantics on web: skip links;
landmarks; heading hierarchy; visually-hidden labels; a theme toggle as a radiogroup; tabs as a WAI-ARIA
tablist with roving tabindex; labelled fields; a category fieldset of real radios; `type=search` with a
decorative icon hidden; rows with native checkboxes, `aria-labelledby`, and screen-reader-only context
("Priority:"); `aria-live` for counts and delete feedback; `:focus-visible` rings;
`prefers-reduced-motion` with a non-motion swipe fallback; 44px targets; keyboard alternatives to every
swipe gesture.

Each pattern in `ui2.html` maps to a framework primitive or recipe -- see [Recipes](./accessibility/recipes.md).

---

## Migration-safety contract

The migration re-pointed the binding under `LibClient/src/Rn` and kept the **F# constructor surface
plus the style DSL identical**. Because the framework already exposes RN-native accessibility APIs, most
accessibility work is `[safe]` and survived the migration unchanged.

Migration tags used throughout this section:

| Tag | Meaning |
|-----|---------|
| `[safe]` | Uses RN-native props/styles/hooks. Survived the ReactXP to RNW migration unchanged. **Build now.** |
| `[web-only]` | Only meaningful on the web target. Build behind the web seam, not in shared logic. |
| `[rnw-blocked]` | Needed the RNW/New-Architecture host-element seam or a modern library the migration brought. Now that the seam plus Reanimated 4 / RNGH 3 exist, most of these are unblocked (see [Backlog](./accessibility/backlog.md)). |
| `[lib]` | Needs a third-party native library (haptics, media captions). Wire via the `ThirdParty` recipe, ideally after the migration settles the dependency stack. |

The safe set (most things): `LC.Pressable(label, role, state, liveRegion, importantForAccessibility,
tabIndex, actions)`; `Rn.View(accessibilityLabel, accessibilityRole, accessibilityState,
accessibilityLiveRegion, importantForAccessibility, accessibilityActions, ariaRoleDescription)`; types in
`LibClient.Accessibility` (`AccessibilityRole`, `AccessibilityStateRecord`, `AccessibilityLiveRegion`,
`ImportantForAccessibility`); `allowFontScaling`/`maxContentSizeMultiplier` on `UiText`/`Text`.

The gated set (small): `:focus-visible` ring styling, true landmarks and heading levels, skip
links, roving-tabindex mechanics, haptics, media captions. The migration has unlocked these (the RNW
seam plus Reanimated 4 / RNGH 3 now exist); several are already built. Don't fake the rest by hand --
see [Backlog](./accessibility/backlog.md#deferred--do-not-hand-roll).

**Hard rule:** never reach past the seam (`document.*`, raw DOM attrs, RN/RNW internals) from app or
framework F#.

---

## Design principles: the "pit of success"

The target is a developer writing **idiomatic** EggShell -- no extra accessibility ceremony -- shipping
WCAG-AA output. Accessibility is a property of the primitives, not a checklist the app re-does each
screen.

1. **Accessible by default, not opt-in.** Primitives emit correct `role`/`name`/`state` with zero extra
   props. There is no "accessible variant" -- there is only the component, and it is accessible. (Baking
   role/state into `Tab`/`Checkbox`/`Picker`/`Button`/`Heading` is the keystone backlog item.)

2. **Infer everything inferable; ask only for what is human.** Derive the accessible name from visible
   text; derive role from the component's identity; derive state from props the component already has.
   The only thing the developer supplies is the label they were going to write as visible text anyway.
   Net new effort is approximately zero.

3. **The easy path is the accessible path.** When a label cannot be inferred (icon-only control), make
   the label **required at the type level** -- an icon-button constructor that takes a mandatory `label`
   -- so "forgot the name" is a compile error, not a silent accessibility bug.

4. **Make missing accessibility hard to ship.** A build-time check and CI flags pressables with no
   derivable name, color used as the only signal, fixed `height` that clips scaled text, and images
   without alt. The palette contrast linter fails the build when a `SemanticPalette`
   foreground/background pair falls below AA -- so low-contrast themes literally cannot merge.

5. **Centralize cross-cutting behavior so apps never wire it.** One reactive `With.Accessibility` hook
   exposes OS settings; primitives consume it internally (heavier weight under bold-text, solid fills
   under reduce-transparency, no animation under reduce-motion). Zero-config live regions for the known
   patterns (counts, toasts, validation, "deleted X"). A standard undo/confirm for destructive actions.
   The app author opts into none of this; it is the default behavior of the building blocks.

6. **Theming guarantees contrast and color-independence by construction.** `SemanticPalette` carries
   validated foreground/background pairs; status/priority/category are expressed as color plus
   text/icon/shape in chip/badge primitives, so an app cannot accidentally convey meaning by color alone.

7. **Scaffolding and the gallery teach the accessible pattern.** `eggshell create-*` emits accessible
   templates; the gallery shows the one canonical, accessible usage per component, with its exposed
   role/name/state and contrast. Copy-paste leads to correct output.

8. **Escape hatches are explicit and rare.** `importantForAccessibility = No` for genuine decoration;
   `accessibilityLabel` override when inference is wrong. These are the exception, visibly so.

9. **Measured, so it cannot silently regress.** axe-core (web) and contrast lint in CI; a
   TalkBack/VoiceOver checklist for native. A regression fails the pipeline, not a future audit.

The friction test for any new primitive or API: *"Does the developer have to do anything beyond writing
the visible content to get an accessible result?"* If yes, redesign until the answer is "no" (or "one
obvious required label").

---

## Definition of done (per component)

A component is accessibility-complete when:

- Name, role, and state are announced correctly by screen readers.
- Decoration is hidden (`importantForAccessibility = No`).
- Dynamic text updates are in a live region.
- Text scales without clipping.
- Colors meet AA contrast and are never the sole signal.
- Touch target is at least 44px.
- Every gesture has a non-gesture alternative.

---

## In this section

- [Spectrum](./accessibility/spectrum.md) -- the full coverage map: vision, motor, hearing, cognitive, motion,
  seizure, speech, situational, i18n, and the WCAG POUR cross-index.
- [Recipes](./accessibility/recipes.md) -- screen-reader semantics recipes and the per-component playbook (buttons,
  toggles, tabs, inputs, pickers, checkboxes, lists, chips, headings).
- [Platform Settings](./accessibility/platform-settings.md) -- reactive OS settings to honor (reduce-motion, text
  scaling, screen reader, bold, high-contrast) and how the framework responds.
- [Backlog](./accessibility/backlog.md) -- the migration-safe vs deferred item list with `[safe]`/`[rnw-blocked]`/
  `[web-only]`/`[lib]` tags, plus the "do-not-hand-roll" list.
