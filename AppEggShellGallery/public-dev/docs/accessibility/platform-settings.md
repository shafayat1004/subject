# Platform Settings

The operating system exposes a set of accessibility preferences that the framework must honor reactively.
Exposing these once, centrally, means every component benefits without per-screen wiring.

All items in this section are `[safe]`: they use RN's `AccessibilityInfo` APIs and React Native's
existing event system, which RNW maps to web media queries. They survive the ReactXP to RNW migration
unchanged -- see [ReactXP to RNW](./modernization/reactxp-to-rnw.md) for the migration context.

---

## Reactive settings table

| Setting | RN API | RNW (web equivalent) | How the framework should respond |
|---------|--------|----------------------|----------------------------------|
| **Screen reader on** | `AccessibilityInfo.isScreenReaderEnabled()` + `change` event | (assume on; no reliable query) | Gate auto-focus and extra announcements; ensure all semantics are correct regardless |
| **Reduce motion** | `AccessibilityInfo.isReduceMotionEnabled()` + `change` event | `prefers-reduced-motion` media query | Disable or replace animations; keep content readable without motion |
| **Bold text** | `AccessibilityInfo.isBoldTextEnabled()` (iOS) | `prefers-contrast` (approximate) | Use heavier font weights (`fontWeight: 700+`) |
| **Reduce transparency** | `AccessibilityInfo.isReduceTransparencyEnabled()` (iOS) | `prefers-reduced-transparency` | Replace translucent/blur fills with solid fills |
| **Invert colors** | `AccessibilityInfo.isInvertColorsEnabled()` | `forced-colors` | Do not fight inversion; avoid color-only signals |
| **Grayscale** | `AccessibilityInfo.isGrayscaleEnabled()` | (CSS `forced-colors`) | Do not fight grayscale; avoid color-only signals |
| **Font scale** | `PixelRatio.getFontScale()` | `rem` zoom level | Layout that reflows; avoid fixed heights that clip text |
| **Cross-fade transitions** | `AccessibilityInfo.prefersCrossFadeTransitions` (iOS) | (not applicable) | Use cross-fade instead of slide/scale transitions |
| **Announce** | `AccessibilityInfo.announceForAccessibility(message)` | Live region update | Programmatic announcement for status messages |
| **Move focus** | `AccessibilityInfo.setAccessibilityFocus(handle)` | `.focus()` | Route focus on route change and dialog open/close |

---

## Proposed `With.Accessibility` hook

The deliverable is a single `With.Accessibility` (or hook) module in `LibClient.Accessibility` that
subscribes to all of the above and returns a record of current flags. Backlog items #7, #8, and #11
cover its implementation.

```fsharp
// Conceptual shape (not yet implemented -- backlog #7)
type AccessibilityFlags = {
    ScreenReaderEnabled : bool
    ReduceMotionEnabled : bool
    BoldTextEnabled     : bool
    ReduceTransparency  : bool
    InvertColors        : bool
    Grayscale           : bool
    FontScale           : float
    PrefersCrossFade    : bool
}
```

Components consuming this hook receive a live-updating record. One place to add; every component
benefits; survives migration (same RN APIs, same F# surface).

---

## How each setting should affect components

### Screen reader on

- Gate auto-focus behavior: when a modal or route opens, move focus to the first meaningful element
  (`setAccessibilityFocus`).
- Enable extra announcements where helpful (for example, announce the new route title on navigation).
- All semantics (role, name, state) must be correct whether the screen reader is on or off -- never
  make correctness conditional on this flag.

### Reduce motion

- The `LC.With.ReducedMotion` helper (backlog #11, `[safe]`) exposes this flag for the animation layer.
- Transitions should cross-fade or cut; spring/slide animations should skip.
- Content remains fully accessible and readable without the animation -- motion is enhancement only.
- Post-migration: the Reanimated 4 / Moti layer reads this flag declaratively, so per-screen code is not
  needed. See [ReactXP to RNW](./modernization/reactxp-to-rnw.md) for the post-migration stack.

### Bold text and reduce transparency

- The theme layer should pick heavier weights when `BoldTextEnabled` is true (backlog #12, `[safe]`).
- The theme layer should substitute solid fills for translucent/blurred surfaces when
  `ReduceTransparency` is true (backlog #12, `[safe]`).
- Both are driven by `SemanticPalette` so the palette, not individual components, adapts.

### Invert colors and grayscale

- Do not override or fight the OS inversion/grayscale. If the app looks broken under grayscale, it is
  most likely because color is being used as the only signal -- fix that root cause (WCAG 1.4.1).
- Do not set image `tintColor` or overlay filters that would double-invert content.

### Font scale

- Keep `allowFontScaling = true` on all text elements (this is the default; do not set it to `false`).
- Use `minHeight` instead of fixed `height` on containers that hold text.
- Use `maxContentSizeMultiplier` only when an extreme multiplier (for example, 3x) genuinely breaks
  layout -- prefer reflow; see [Recipes L](./accessibility/recipes.md#l-any-text).
- `PixelRatio.getFontScale()` can be queried for layout decisions (for example, switching from a
  two-column to a single-column layout above a threshold).

### Programmatic announcements

- Use `AccessibilityInfo.announceForAccessibility(message)` for transient status messages (for example,
  "Item deleted", "Loading complete") that have no persistent on-screen representation.
- Use live regions (`accessibilityLiveRegion = Polite`) for persistent containers that update in place
  (counts, validation messages, progress indicators) -- screen readers pick up the change automatically.
- The `LibClient.Accessibility.announce` helper (backlog #8, `[safe]`) will wrap both paths behind a
  single call.

### Focus management

- On route change: move focus to the new route's first meaningful heading or content element.
- On dialog open: trap focus within the dialog; move focus to the first interactive element.
- On dialog close: return focus to the element that triggered the dialog.
- Basic native binding is `[safe]`; polished roving-tabindex and `inert` mechanics are
  `[rnw-blocked]` (the RNW host-element seam is needed for web). See [Backlog](./accessibility/backlog.md) #16.

---

## Web-specific settings (RNW, post-migration)

These become available once the ReactXP to RNW swap lands. They complement the RN flags above.

| Media query | Triggered by | Framework response |
|-------------|-------------|-------------------|
| `prefers-reduced-motion` | OS reduce-motion | Same as RN flag above; resolved via RNW |
| `prefers-contrast` | OS increase-contrast | Heavier weights, higher-contrast palette tokens |
| `prefers-reduced-transparency` | OS reduce-transparency | Solid fills |
| `forced-colors` (Windows High Contrast) | Windows accessibility mode | Do not override; use `forced-colors` overrides if needed |
| `color-scheme` | OS dark/light mode | Already handled by the theme system |
| `prefers-color-scheme` | Browser dark/light preference | Same |

See [ReactXP to RNW](./modernization/reactxp-to-rnw.md) for the full picture of what the migration
unlocks.
