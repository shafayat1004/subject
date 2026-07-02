# Accessibility Spectrum

Accessibility is not just screen readers. This page covers the full spectrum of needs EggShell targets,
plus the WCAG POUR cross-index. For how to implement each category, see [Recipes](./accessibility/recipes.md). For the
platform settings that map to these categories, see [Platform Settings](./accessibility/platform-settings.md).

---

## Full-spectrum coverage map

Each row gives: who it helps, what to do, the relevant WCAG criteria, the RN/RNW API surface, the current
framework status, and the migration tag.

### Vision -- blind (screen readers)

**Who it helps:** users who are blind or have severe visual impairment and navigate by TalkBack, VoiceOver,
or NVDA/VoiceOver on web.

**What to do:** every control has a name, role, and state; reading order matches visual order; dynamic
changes are announced; decorative content is hidden; images have text alternatives.

**WCAG:** 1.1.1 (non-text content), 1.3.1 (info and relationships), 4.1.2 (name/role/value), 4.1.3
(status messages), 2.4.3 (focus order).

**API:** `accessibility*` props; live regions; `AccessibilityInfo.announceForAccessibility`;
`setAccessibilityFocus`.

**Status:** primitives have the props (`[safe]`). Gaps: not all primitives set role/state by default
(backlog #9); no `announce` helper yet (backlog #8). See [Recipes §1-2](./accessibility/recipes.md).

---

### Vision -- low vision (text scaling, zoom, bold, transparency)

**Who it helps:** users who rely on OS Dynamic Type / large-font settings, pinch-zoom, bold text, or
reduced transparency.

**What to do:** text must scale up to approximately 200% without clipping or content loss; do not pin text
containers to fixed heights; reflow instead of truncate; honor OS bold-text and increase-contrast /
reduce-transparency settings; support pinch-zoom on web.

**WCAG:** 1.4.4 (resize text), 1.4.10 (reflow), 1.4.12 (text spacing), 1.4.3 / 1.4.11 (contrast).

**API:** RN respects OS font scale by default; `allowFontScaling` (keep `true`);
`maxFontSizeMultiplier` / `maxContentSizeMultiplier` to cap when necessary;
`PixelRatio.getFontScale()`; `AccessibilityInfo.isBoldTextEnabled` /
`isReduceTransparencyEnabled` (RN); `prefers-contrast` / `prefers-reduced-transparency` /
`forced-colors` media queries (RNW).

**Status:** `allowFontScaling` / `maxContentSizeMultiplier` exist on `UiText` / `Text` (`[safe]`). Gaps:
fixed `height` in some styles (for example, single-line input `height 21`) can clip scaled text -- audit
needed; no bold/contrast/transparency flag wiring yet (backlog #7/#12). Reflow is already helped by the
responsive `LC.With.ScreenSize`.

---

### Vision -- color (contrast and color-independence)

**Who it helps:** users with low vision, color blindness, or users in poor lighting.

**What to do:** text contrast at least 4.5:1 (at least 3:1 for large text/UI/icons); never use color as
the **only** way to convey meaning (priority, status, required must also have text, icon, or shape); use
color-blind-safe hues; do not fight OS invert/grayscale.

**WCAG:** 1.4.1 (use of color), 1.4.3 (contrast minimum), 1.4.11 (non-text contrast).

**API:** no built-in contrast helper currently; `isInvertColorsEnabled` / `isGrayscaleEnabled` to detect
(mostly: just do not hardcode around them).

**Status:** gap -- no contrast tooling in `ColorModule` yet. Backlog #13 (`[safe]`) adds a
relative-luminance and contrast-ratio helper plus a dev-time palette linter asserting AA on
`SemanticPalette`. Status/priority chips already pair color with text labels (keep that rule); earthy
palettes need AA verification.

---

### Motor / dexterity

**Who it helps:** users with limited fine motor control, one-handed users, users using switch access or
keyboard navigation.

**What to do:** touch targets at least 44x44pt (iOS) / 48dp (Android) with spacing; activate on
pointer-up and allow cancel by dragging off; provide a **single-pointer alternative** to every
swipe/multipoint/path gesture (swipe-to-delete must also have a tap/button plus keyboard path); no action
requiring device motion only without an alternative; full keyboard operability on web; voice-control
friendliness -- the accessible name must contain the visible text (label in name).

**WCAG:** 2.5.5 / 2.5.8 (target size), 2.5.1 (pointer gestures), 2.5.2 (pointer cancellation), 2.5.3
(label in name), 2.5.4 (motion actuation), 2.1.1 (keyboard), 2.2.1 (timing).

**API:** `LC.Pressable` (up-event plus overlay hit area); sizing via styles; RNGH for rich gestures
(post-migration); keyboard is RNW/web.

**Status:** target sizing `[safe]` (audit, backlog #4); pointer-up/cancel is `LC.Pressable`'s model
(`[safe]`); gesture alternatives for planned swipe-to-delete are required -- keep a visible delete
affordance (icon button) so the gesture is an enhancement, not the only path (`[safe]` now; rich swipe
itself `[rnw-blocked]` via RNGH); keyboard mechanics `[web-only]`.

---

### Hearing

**Who it helps:** users who are deaf or hard of hearing.

**What to do:** never convey information by sound alone; provide captions/transcripts for video and
transcripts for audio; provide visual plus optional haptic equivalents for audio cues and alerts.

**WCAG:** 1.2.x (captions/alternatives), 1.4.2 (audio control).

**API:** media component caption tracks (`react-native-video` text tracks / web `<track>`); haptics via a
library.

**Status:** AppTodo has no audio/video today -- N/A for now. The framework's future media and notification
components must support captions and visual alerts when added (`[lib]` for captions/haptics). Recorded as a
forward requirement so audio-only feedback is never shipped later.

---

### Cognitive / learning / attention

**Who it helps:** users with cognitive disabilities, learning differences, attention difficulties, or
memory limitations.

**What to do:** plain, concise language; consistent and predictable navigation and component behavior;
prevent errors and make recovery easy (clear messages, undo for destructive actions, confirm or undo
delete); minimize memory load; no unexpected context changes on focus or input; show progress/status; give
enough time.

**WCAG:** 3.2.x (predictable), 3.3.x (input assistance/error prevention), 2.2.x (timing), 3.1.5
(reading level).

**API:** mostly design and content discipline plus the form-error and live-region primitives.

**Status:** `[safe]` and largely app-level. The framework can help with a standard undo/confirm pattern
for destructive operations and consistent error display (the Executor error surface already centralizes
this). Backlog #15 (undo affordance), and "confirm destructive actions" as a convention.

---

### Vestibular / motion sensitivity

**Who it helps:** users with vestibular disorders who experience nausea or disorientation from animations.

**What to do:** honor reduce-motion; never convey information by motion alone; offer non-animated
fallbacks (the mockup's reduced-motion swipe fallback); no autoplay or parallax that cannot be disabled.

**WCAG:** 2.3.3 (animation from interactions), 1.4.2.

**API:** `AccessibilityInfo.isReduceMotionEnabled()` plus change event (RN) /
`prefers-reduced-motion` (web); `AccessibilityInfo.prefersCrossFadeTransitions` (iOS).

**Status:** gap -- no wiring yet. Backlog #7 / #11 adds `LC.With.ReducedMotion` hook (`[safe]`); the
future Reanimated/Moti layer (post-migration) reads it to disable animation automatically. Until then,
keep motion non-essential to content.

---

### Seizure / photosensitivity

**Who it helps:** users with photosensitive epilepsy.

**What to do:** nothing flashes more than 3 times per second; avoid large flashes or red flashes; give
controls to stop motion.

**WCAG:** 2.3.1 (three flashes or below threshold).

**API:** design constraint plus reduce-motion.

**Status:** `[safe]` (a rule, not code). Add to design review and gallery "don't" notes; no current
offenders.

---

### Speech

**Who it helps:** users who use voice control software.

**What to do:** never require voice input as the only path; provide typed/tap alternatives.

**WCAG:** related to 2.1 / 2.5.

**Status:** N/A today (no voice features); recorded as a forward rule.

---

### Situational / temporary

Users who are temporarily impaired -- one-handed use, bright sunlight (contrast), noisy rooms (captions),
cracked screen, motor fatigue -- are covered transitively by the vision, motor, hearing, and cognitive
dimensions above. No separate work item; framed here so reviewers consider context, not just permanent
disability.

---

### Internationalization adjacency (RTL and text expansion)

**Who it helps:** users in right-to-left locales (Arabic, Hebrew, Farsi) and users whose language
produces longer translated strings.

**What to do:** support RTL mirroring (use logical start/end, not left/right where possible); allow text
to grow approximately 30-40% for translations without truncation; locale-aware number/date formatting.

**API:** `I18nManager.isRTL` / `forceRTL` plus writing-direction-aware flexbox (New Arch); RNW `dir`.

**Status:** gap -- styles currently use `left`/`right` (for example, label `left 10`); no RTL story yet.
Partly `[rnw-blocked]` (logical props land cleaner post-New-Arch). Tracked as backlog #14; not urgent for
a single-locale app, but document so new components prefer logical layout.

---

## WCAG POUR quick cross-index

| Principle | Criteria | Covered by |
|-----------|----------|------------|
| **Perceivable** | 1.1.1 text alternatives | Screen-reader section above; [Recipes](./accessibility/recipes.md) |
| **Perceivable** | 1.2.x captions/alternatives | Hearing section above |
| **Perceivable** | 1.3.1 info and relationships | [Recipes](./accessibility/recipes.md) role/name/state |
| **Perceivable** | 1.3.4 orientation | Done -- app no longer locks orientation |
| **Perceivable** | 1.4.1 use of color | Color section above |
| **Perceivable** | 1.4.3 / 1.4.11 contrast | Color section above; [Backlog](./accessibility/backlog.md) #13 |
| **Perceivable** | 1.4.4 / 1.4.10 resize/reflow | Low-vision section above |
| **Perceivable** | 1.4.12 text spacing | Low-vision section above |
| **Operable** | 2.1.1 keyboard | Motor section above; `[web-only]` |
| **Operable** | 2.2.x timing | Cognitive section above |
| **Operable** | 2.3.1 three flashes | Seizure section above |
| **Operable** | 2.3.3 animation from interactions | Motion section above; [Platform Settings](./accessibility/platform-settings.md) |
| **Operable** | 2.4.3 focus order | Screen-reader section above; [Recipes](./accessibility/recipes.md) |
| **Operable** | 2.4.7 focus visible | `[web-only]`/`[rnw-blocked]`; [Backlog](./accessibility/backlog.md) |
| **Operable** | 2.5.1 pointer gestures | Motor section above |
| **Operable** | 2.5.2 pointer cancellation | Motor section above (`LC.Pressable`) |
| **Operable** | 2.5.3 label in name | [Recipes](./accessibility/recipes.md); Motor section above |
| **Operable** | 2.5.5 / 2.5.8 target size | [Backlog](./accessibility/backlog.md) #4 |
| **Understandable** | 3.1.1 language of page | Forward requirement |
| **Understandable** | 3.2.x predictable | Cognitive section above |
| **Understandable** | 3.3.x input assistance | Cognitive section above; [Recipes](./accessibility/recipes.md) D |
| **Robust** | 4.1.2 name/role/value | Screen-reader section above; [Recipes](./accessibility/recipes.md) |
| **Robust** | 4.1.3 status messages | [Recipes](./accessibility/recipes.md) live regions |
