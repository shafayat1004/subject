# EggShell Accessibility: Full-Spectrum Plan, Recipes, and Migration-Safe Backlog

Status: living doc. Companion to `FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md` (the seam/migration
strategy) and `APP_STRUCTURE.md` (app layout). Web benchmark:
`SuiteTodo/AppTodo/suggestions/ui2.html` (a11y-rich light + dark mockup).

Accessibility is **not** just screen readers. This doc covers the whole spectrum: vision (blind,
low-vision, color), motor/dexterity, hearing, cognitive, vestibular/motion, seizure/photosensitivity,
speech, and situational/temporary impairments — plus the platform settings to honor and the
internationalization adjacency (RTL/text expansion). Each item is tagged for migration safety.

---

## 0. How to use this doc

- **Building UI?** §7 (screen-reader semantics recipes) + §8 (per-component playbook, written for bulk
  conversion) are the copy-paste core. But also skim §4 so you cover the non-screen-reader dimensions.
- **Planning?** §4 is the full-spectrum catalog; §9 is the tagged backlog; §5 cross-indexes WCAG.
- **Deciding if something survives the migration?** §3 (the contract) + the tag on each item.

> **The two lenses that drive this plan:** §13 — *make the accessible path the default and lowest-effort
> path* (accessibility-by-default; a developer writing idiomatic EggShell ships WCAG-AA without thinking
> about it). §14 — *what the post-migration stack (New-Arch RN, RNW, React 19, modern web) unlocks*, so
> we build toward it and move `[rnw-blocked]` items to done. Read those two before planning work.

Migration tags:
- `[safe]` — expressed on the F# component surface (props/styles/hooks) using RN-native APIs. Survives
  the ReactXP→RNW migration unchanged (RN has the prop; RNW maps to web). **Build now.**
- `[web-only]` — only meaningful on the web target. Build behind the web seam, not in shared logic.
- `[rnw-blocked]` — needs the RNW/New-Architecture host-element seam or a modern library that the
  migration brings. Document intent; defer implementation; do **not** hand-roll a throwaway.
- `[lib]` — needs a third-party native library (haptics, media captions); wire via the `ThirdParty`
  recipe, ideally after the migration settles the dependency stack.

---

## 1. Why accessibility is a framework concern

Apps should get correct accessibility by **using the primitives normally**. The framework's job:
(a) primitives emit correct semantics by default (role/name/state, scalable text, honored OS settings);
(b) reusable hooks/helpers expose OS accessibility state (reduce-motion, bold-text, screen-reader-on,
contrast) so components adapt; (c) this doc + the gallery document the recipe for the app-specific bits.
Per-app hand-wiring of ARIA/flags is a smell.

## 2. The benchmark, condensed

`ui2.html` shows (web): skip links; landmarks; heading hierarchy; visually-hidden labels; theme toggle
as radiogroup; tabs as a WAI-ARIA tablist with roving tabindex; labelled fields; category as a fieldset
of real radios; `type=search` + decorative icon hidden; rows with native checkbox, `aria-labelledby`,
sr-only context ("Priority:"); `aria-live` for counts and delete; `:focus-visible` rings;
`prefers-reduced-motion` with a non-motion swipe fallback; 44px targets; keyboard alternatives to swipe.
Treat it as the **target semantics**; §4/§7 map each to the framework.

---

## 3. The migration-safety contract (read once)

`FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md` §10: the migration re-points the binding under
`LibClient/src/ReactXP` and keeps the **F# constructor surface + style DSL identical**. The framework
already exposes **RN-native** a11y APIs, so most a11y work is `[safe]`:

- Semantics: `LC.Pressable(label, role, state, liveRegion, importantForAccessibility, tabIndex, actions)`;
  `RX.View(accessibilityLabel, accessibilityRole, accessibilityState, accessibilityLiveRegion,
  importantForAccessibility, accessibilityActions, ariaRoleDescription)`; types in
  `LibClient.Accessibility` (`AccessibilityRole`, `AccessibilityStateRecord`, `AccessibilityLiveRegion`,
  `ImportantForAccessibility`).
- Text scaling: `LC.UiText/Text(allowFontScaling, maxContentSizeMultiplier)` already exist.
- These names ARE RN's; RNW maps them to ARIA / CSS. Swapping the binding doesn't touch the call site.

Gated set (small): `:focus-visible` ring **styling**, true landmarks/heading levels, skip links,
roving-tabindex mechanics, haptics, media captions. The migration *unlocks* these (single host-element
seam for ARIA/landmarks, New-Arch focus/announce, modern libraries). Don't fake them now.

Never reach past the seam (no `document.*`, raw DOM attrs, ReactXP internals) from app/framework F#.

---

## 4. The full accessibility spectrum

Each dimension: **who it helps**, **what to do**, **WCAG hooks**, **RN/RNW API**, **framework status**,
**tag**. Recipes for the screen-reader-heavy parts live in §7–§8; this section is the coverage map so
nothing is forgotten.

### 4.1 Vision — blind (screen readers)
- **Do:** every control has a name + role + state; reading order matches visual order; dynamic changes
  announced; decorative content hidden; images have text alternatives.
- **WCAG:** 1.1.1, 1.3.1, 4.1.2, 4.1.3, 2.4.3.
- **API:** the `accessibility*` props above; live regions; `AccessibilityInfo.announceForAccessibility`;
  `setAccessibilityFocus`.
- **Status / tag:** primitives have the props (`[safe]`); **gap:** not all primitives set role/state by
  default (backlog §9 #9); no `announce` helper yet (§9 #8). Recipes §7–§8.

### 4.2 Vision — low vision (text scaling, zoom, bold, transparency)
- **Do:** text must scale with OS Dynamic Type / font-size setting up to ~200% without clipping or loss;
  don't pin text containers to fixed heights; reflow instead of truncate; honor OS **bold text** and
  **increase-contrast / reduce-transparency**; support pinch-zoom (web) / large-text.
- **WCAG:** 1.4.4 (resize text), 1.4.10 (reflow), 1.4.12 (text spacing), 1.4.3/1.4.11 (contrast).
- **API:** RN respects OS font scale by default; `allowFontScaling` (keep true), `maxFontSizeMultiplier`
  / `maxContentSizeMultiplier` to cap, `PixelRatio.getFontScale()`; OS flags
  `AccessibilityInfo.isBoldTextEnabled` / `isReduceTransparencyEnabled` (RN) and
  `prefers-contrast`/`prefers-reduced-transparency`/`forced-colors` media queries (RNW).
- **Status / tag:** `allowFontScaling`/`maxContentSizeMultiplier` exist on `UiText`/`Text` (`[safe]`);
  **gaps:** fixed `height` in some styles (e.g. single-line inputs `height 21`) can clip scaled text —
  audit; no bold/contrast/transparency flag wiring (backlog §9 #7/#12). Reflow already helped by the
  responsive `LC.With.ScreenSize`.

### 4.3 Vision — color (contrast & color-independence)
- **Do:** text contrast ≥ 4.5:1 (≥ 3:1 for large/UI/icons); never use color as the **only** way to
  convey meaning (priority/status/required must also have text/icon/shape); pick color-blind-safe hues;
  don't fight OS invert/grayscale.
- **WCAG:** 1.4.1 (use of color), 1.4.3, 1.4.11 (non-text contrast).
- **API:** none built-in; needs a contrast helper. RN `isInvertColorsEnabled`/`isGrayscaleEnabled` to
  detect (mostly: just don't hardcode around them).
- **Status / tag:** **gap** — no contrast tooling in `ColorModule`. Add a luminance/contrast-ratio helper
  to lint `SemanticPalette`s (backlog §9 #13, `[safe]`). AppTodo already pairs color with text labels on
  chips (good — keep that rule). Verify the earthy palettes meet AA (verification §12).

### 4.4 Motor / dexterity
- **Do:** touch targets ≥ 44×44 (iOS) / 48dp (Android) with spacing; activate on **pointer-up** and
  allow cancel (drag off to abort); provide a **single-pointer alternative** to any swipe/multipoint/
  path gesture (e.g. swipe-to-delete must also have a tap/button + keyboard path — the mockup does this);
  no action that requires device motion only (shake) without an alternative; full keyboard operability
  on web; voice-control friendliness ("label in name" — the accessible name must contain the visible
  text); generous/adjustable timeouts.
- **WCAG:** 2.5.5/2.5.8 (target size), 2.5.1 (pointer gestures), 2.5.2 (pointer cancellation), 2.5.3
  (label in name), 2.5.4 (motion actuation), 2.1.1 (keyboard), 2.2.1 (timing).
- **API:** `LC.Pressable` (up-event + overlay hit area); sizing via styles; RNGH for rich gestures
  (post-migration); keyboard is RNW/web.
- **Status / tag:** target sizing `[safe]` (audit, §9 #4); pointer-up/cancel is `LC.Pressable`'s model
  `[safe]`; **gesture alternatives** for the planned swipe-to-delete are required — keep a visible
  delete affordance (icon button) so the gesture is an enhancement, not the only path (`[safe]` now;
  rich swipe itself `[rnw-blocked]` → RNGH); keyboard mechanics `[web-only]`.

### 4.5 Hearing
- **Do:** never convey info by sound alone; captions/transcripts for video, transcripts for audio;
  visual + (optional) haptic equivalents for audio cues/alerts.
- **WCAG:** 1.2.x (captions/alternatives), 1.4.2 (audio control).
- **API:** media component caption tracks (`react-native-video` text tracks / web `<track>`); haptics via
  a library.
- **Status / tag:** AppTodo has no audio/video, so **N/A today**; but the framework's media/notification
  components must support captions/visual alerts when added (`[lib]` for captions/haptics). Record as a
  forward requirement so we don't ship audio-only feedback later.

### 4.6 Cognitive / learning / attention
- **Do:** plain, concise language; consistent & predictable navigation and component behavior; prevent
  errors and make recovery easy (clear messages, **undo** for destructive actions, confirm or undo
  delete); minimize memory load and clutter; no unexpected context changes on focus/input; show progress/
  status; give enough time.
- **WCAG:** 3.2.x (predictable), 3.3.x (input assistance/error prevention), 2.2.x (timing), 3.1.5
  (reading level).
- **API:** mostly design/content discipline + the form-error and live-region primitives.
- **Status / tag:** `[safe]` and largely app-level. Framework can help: a standard **undo/confirm**
  pattern for destructive ops, consistent error display (the Executor error surface already centralizes
  this). Backlog §9 #15 (undo affordance), and "confirm destructive actions" as a convention.

### 4.7 Vestibular / motion sensitivity
- **Do:** honor **reduce-motion**; never convey information by motion alone; offer non-animated
  fallbacks (the mockup's reduced-motion swipe fallback); no autoplay/parallax that can't be disabled.
- **WCAG:** 2.3.3 (animation from interactions), 1.4.2.
- **API:** `AccessibilityInfo.isReduceMotionEnabled()` + change event (RN) / `prefers-reduced-motion`
  (web); `AccessibilityInfo.prefersCrossFadeTransitions` (iOS).
- **Status / tag:** **gap** — no wiring. Add `LC.With.ReducedMotion` hook (backlog §9 #7, `[safe]`); the
  future Reanimated/Moti layer (migration §9/§11) reads it to disable animation. Until then keep motion
  non-essential.

### 4.8 Seizure / photosensitivity
- **Do:** nothing flashes more than **3 times per second**; large flashes/red flashes avoided; give
  controls to stop motion.
- **WCAG:** 2.3.1 (three flashes).
- **API:** design constraint + reduce-motion.
- **Status / tag:** `[safe]` (a rule, not code). Add to design review + the gallery's "don't" notes; no
  current offenders.

### 4.9 Speech
- **Do:** never require voice input as the only path; provide typed/tap alternatives.
- **WCAG:** related to 2.1/2.5.
- **Status / tag:** N/A today (no voice features); record as a forward rule.

### 4.10 Situational / temporary
- One-handed use, bright sunlight (contrast), noisy rooms (captions), cracked screen / motor fatigue —
  all are covered transitively by §4.2–§4.5 (large targets, contrast, captions, scalable text). No
  separate work; framed here so reviewers consider context, not just permanent disability.

### 4.11 Internationalization adjacency (RTL & text expansion)
- **Do:** support RTL mirroring (use logical start/end, not left/right, where possible); allow text to
  grow ~30–40% (translations) without truncation; locale-aware number/date formatting.
- **API:** RN `I18nManager.isRTL`/`forceRTL` + writing-direction-aware flexbox (New Arch); RNW `dir`.
- **Status / tag:** **gap** — styles use `left`/`right` (e.g. label `left 10`); no RTL story. Medium,
  partly `[rnw-blocked]` (logical props land cleaner post-New-Arch). Track as §9 #14; not urgent for a
  single-locale app but document so new components prefer logical layout.

---

## 5. WCAG POUR quick cross-index

- **Perceivable:** §4.1 (text alts, 1.1.1), §4.2 (resize/reflow 1.4.4/1.4.10), §4.3 (contrast/color
  1.4.1/1.4.3/1.4.11), §4.5 (captions 1.2.x), §7 (info & relationships 1.3.1).
- **Operable:** §4.4 (keyboard 2.1, targets 2.5.5, gestures 2.5.1, cancellation 2.5.2, label-in-name
  2.5.3), §4.7/§4.8 (motion/seizure 2.3.x), §4.6 (timing 2.2.x), focus order/visible 2.4.3/2.4.7 (§7,
  §9), orientation 1.3.4 (**done** — app no longer locks orientation).
- **Understandable:** §4.6 (predictable 3.2, input assistance 3.3), language of page 3.1.1.
- **Robust:** §4.1 (name/role/value 4.1.2), status messages 4.1.3 (live regions, §7.4).

---

## 6. Platform settings the framework should honor (reactive)

Expose OS accessibility state once, reactively, so components adapt. Proposed `LibClient.Accessibility`
hooks/helpers (all `[safe]`; RN `AccessibilityInfo` + listeners, RNW media queries):

| Setting | RN API | RNW (web) | Use |
|---|---|---|---|
| Screen reader on | `isScreenReaderEnabled` + event | (assume on) | gate auto-focus / extra announcements |
| Reduce motion | `isReduceMotionEnabled` + event | `prefers-reduced-motion` | disable/replace animation |
| Bold text | `isBoldTextEnabled` (iOS) | n/a / `prefers-contrast` | heavier weights |
| Reduce transparency | `isReduceTransparencyEnabled` (iOS) | `prefers-reduced-transparency` | solid fills over blur |
| Invert / grayscale | `isInvertColorsEnabled`/`isGrayscaleEnabled` | `forced-colors` | don't fight; avoid color-only |
| Font scale | `PixelRatio.getFontScale()` | rem + zoom | layout that reflows |
| Announce | `announceForAccessibility` | live region | status messages |
| Move focus | `setAccessibilityFocus(handle)` | `.focus()` | route/dialog focus mgmt |
| Cross-fade | `prefersCrossFadeTransitions` (iOS) | — | transition style |

Deliverable: a small `With.Accessibility`/hook module returning a record of these flags (backlog §9 #7,
#8, #11). One place to add; every component benefits; survives migration (same RN APIs).

---

## 7. Screen-reader semantics recipes (`[safe]`)

(Same as before — the copy-paste core for §4.1.)

- **Name:** Pressable `label = "..."`; View/Text `accessibilityLabel = "..."`; inputs prefer a visible
  `label`. Ensure the **visible text is contained in the name** (WCAG 2.5.3).
- **Role:** `role`/`accessibilityRole = AccessibilityRole.X` (Button, Link, CheckBox, Radio, Tab,
  TabList, Header, Search, ListItem, List, Status, Switch, Image, Dialog…).
- **State:** `AccessibilityStateRecord.selected/checked'/expanded/disabled/busy`.
- **Live regions:** on the auto-updating container, `accessibilityLiveRegion = Polite` (`Assertive`
  only for interruptions) + keep its `accessibilityLabel` = current text.
- **Hide decoration:** `importantForAccessibility = No`/`NoHideDescendants` on icons that repeat text.
```fsharp
LC.Pressable(onPress = onSelect, label = sprintf "%s theme" label,
             role = AccessibilityRole.Radio, state = AccessibilityStateRecord.selected isActive)
RX.View(accessibilityRole = AccessibilityRole.Status, accessibilityLiveRegion = AccessibilityLiveRegion.Polite,
        accessibilityLabel = sprintf "%i open, %i done" openCount doneCount, children = ...)
```

## 8. Per-component conversion playbook (bulk-convertible)

> Rule of thumb: every pressable gets `label` + `role` (+ `state` if selectable/toggle); every repeated
> icon is hidden; every input gets a visible `label` or `accessibilityLabel`; every auto-updating
> count/message gets a live region; every target is ≥44px; text keeps `allowFontScaling`.

- **A. Button:** visible text = name; icon-only → `label` + `role = Button`; target ≥44px.
- **B. Toggle/segmented:** container `RadioGroup` + label; each segment `Radio` + `selected` state.
- **C. Tabs/filters:** container `TabList`; each `Tab` + `selected`.
- **D. Text input:** visible `label` (preferred) or `accessibilityLabel`; errors via live region.
- **E. Picker/select:** visible `label`; value in name; popup items `Option`/`ListItem` + `selected`.
- **F. Checkbox/toggle row:** `CheckBox` role + `checked'`; title as name.
- **G. List + rows:** list `List`; row `ListItem` + summarizing label; meta chips carry context
  ("Priority: High") so they aren't read as bare words.
- **H. Status chips/counts:** `Status` + `Polite` live region + spoken summary.
- **I. Headings:** `Header` role (level is `[web-only]` precision).
- **J. Decorative icon:** always `importantForAccessibility = No`; never the sole content of a control.
- **K. Destructive action:** confirm or provide **undo**; announce result via live region (§4.6).
- **L. Any text:** keep `allowFontScaling = true`; avoid fixed heights that clip; cap with
  `maxContentSizeMultiplier` only when necessary (§4.2).

---

## 9. Backlog (tagged; do `[safe]` now)

### Small (hours)
1. Decorative `prefixIcon` (search magnifier) rendered `importantForAccessibility = No`. `[safe]`
2. `Input.Text` `?accessibilityLabel` passthrough for placeholder-only inputs. `[safe]`
3. Search wrapper `role = Search`. `[safe]`
4. Touch-target audit (icon buttons / `TextButton` ≥44px). `[safe]`
5. AppTodo: label toggle/tabs/chips/rows/counts per §8. `[safe]`
6. Audit fixed `height`/clipping that breaks text scaling (e.g. single-line input `height 21`). `[safe]`

### Medium (≈1 day)
7. `With.Accessibility` reactive hook exposing OS flags (reduce-motion, bold, screen-reader, transparency,
   invert/grayscale, font scale) — §6. `[safe]`
8. `LibClient.Accessibility.announce : string -> unit` over `announceForAccessibility` / web live region.
   `[safe]`
9. **Bake role/state into primitives** (`Tab`/`Tabs`, `Checkbox`, `Picker`, `Button`, `Heading`) so apps
   get semantics free. Highest leverage. `[safe]`
10. `LC.Group`/`LC.RadioGroup` wrappers (container role+label in one call). `[safe]`
11. `LC.With.ReducedMotion` (subset of #7) for the animation layer to consume. `[safe]`
12. Honor bold-text / reduce-transparency in theming (heavier weights, solid fills). `[safe]`
13. **Contrast tooling** in `ColorModule`: relative-luminance + contrast-ratio helpers; a dev-time
   palette linter asserting AA on `SemanticPalette`. `[safe]`

### Large (multi-day; some gated)
14. RTL/logical-layout pass (start/end vs left/right; text expansion). partly `[rnw-blocked]`
15. Destructive-action **undo** pattern (snackbar + announce) framework-wide. `[safe]`
16. Programmatic focus management (route change, dialog open/close): basic native binding `[safe]`;
   polished version `[rnw-blocked]`.
17. Roving-tabindex keyboard nav for tablists/menus. `[web-only]`
18. Landmarks + skip links + heading levels. `[web-only]`/`[rnw-blocked]`
19. Media captions + haptic feedback when media/notifications are added. `[lib]`
20. a11y CI: axe-core (Playwright, web) + TalkBack/VoiceOver manual checklist; contrast lint in CI.

---

## 10. Deferred / do-not-hand-roll (rationale)

- **Skip links** `[web-only]`, **`:focus-visible` ring styling** `[web-only]`, **landmarks + heading
  levels** `[rnw-blocked]`, **roving-tabindex mechanics** `[web-only]` — web-DOM-shaped or dependent on
  the RNW host-element seam; implement once that seam exists. Use `role = Header/Group/Status` + labels
  as the portable subset now.
- **Haptics / media captions** `[lib]` — need libraries the migration's modern stack brings.
- **ReactXP-internal render hygiene** — never patch vendored ReactXP for a11y; fix at the F# seam (the
  "unique key" warning was fixed in `AppShell/Context/Context.fs`, see `LEARNINGS.md` 2026-06-29).

The migration *unlocks* these: a single host-element seam for ARIA/landmarks, New-Arch focus/announce,
real `prefers-*` media queries on web, and the modern gesture/animation/media libraries. Deferring is
sequencing, not lost work.

---

## 11. Gallery showcase (design only)

`AppEggShellGallery`: a theme-selector page restyling a fixed component set across palettes (incl.
AppTodo earthy light/dark) **with an a11y panel per component** showing the exposed role/name/state, the
contrast ratio of its colors, whether it scales with font size, and tags for `[web-only]`/`[rnw-blocked]`
gaps. Doubles as living docs for §7–§8. Add `docs/fsharp/accessibility.md` mirroring §4/§7/§8. Pure F#,
no render DSL (project rule 10).

## 12. Verifying accessibility

- **Screen readers:** TalkBack (Android), VoiceOver (iOS), NVDA/VoiceOver (web). Every control announces
  name + role + state; decoration silent; dynamic text announced.
- **Contrast:** automated check (the §9 #13 helper) + axe-core on web; verify the earthy palettes hit AA.
- **Text scaling:** set OS font size to max; confirm no clipping/overlap, layout reflows.
- **Reduce motion / bold / invert:** toggle each OS setting; confirm the app adapts (or at least does
  not break).
- **Keyboard (web):** tab through everything; visible focus; no traps; gesture actions reachable.
- **Motor:** target sizes ≥44px; swipe actions have a tap/keyboard alternative.
- **Layout matrix:** light/dark × portrait/landscape (orientation not locked) via the `adb`/`xcrun`
  screenshot loops in `LEARNINGS.md`.
- **Definition of done (per component):** name+role+state announced; decoration hidden; dynamic text in a
  live region; text scales without clipping; colors meet AA and never sole indicator; target ≥44px;
  any gesture has a non-gesture alternative.

---

## 13. Design principles: maximal accessibility, minimal developer effort (the "pit of success")

The target: a developer writing **idiomatic** EggShell — no extra a11y ceremony — ships WCAG-AA output.
Accessibility is a property of the primitives, not a checklist the app re-does each screen. Concretely:

1. **Accessible by default, not opt-in.** Primitives emit correct `role`/`name`/`state` with zero extra
   props. There is no "accessible variant" of a component — there is only the component, and it is
   accessible. (Backlog §9 #9 is the keystone: bake role/state into `Tab`/`Checkbox`/`Picker`/`Button`/
   `Heading`.)
2. **Infer everything inferable; ask only for what's human.** Derive the accessible **name** from the
   visible text/children; derive **role** from the component's identity; derive **state**
   (selected/checked/expanded/disabled/busy) from props the component already has. The only thing the
   developer supplies is the label they were going to write as visible text anyway. Net new effort ≈ 0.
3. **The easy path is the accessible path.** When a label *can't* be inferred (icon-only control), make
   the label **required at the type level** (e.g. an icon-button constructor that takes a mandatory
   `label`), so "forgot the name" is a compile error, not a silent a11y bug.
4. **Make missing a11y hard to ship.** A build-time check / F# analyzer (and CI) flags: pressables with
   no derivable name, color used as the *only* signal, fixed `height` that would clip scaled text, images
   without alt. The **palette contrast linter** (§9 #13) fails the build when a `SemanticPalette`
   foreground/background pair is below AA — so low-contrast themes literally cannot merge.
5. **Centralize cross-cutting behavior so apps never wire it.** One reactive `With.Accessibility` hook
   (§6/§9 #7) exposes OS settings; primitives consume it **internally** (heavier weight under bold-text,
   solid fills under reduce-transparency, no animation under reduce-motion). Zero-config **live regions**
   for the known patterns (counts, toasts, validation, "deleted X"). A standard **undo/confirm** for
   destructive actions. The app author opts into none of this; it's the default behavior of the building
   blocks.
6. **Theming guarantees contrast and color-independence by construction.** `SemanticPalette` carries
   validated foreground/background pairs; status/priority/category are expressed as
   `color + text/icon/shape` in the chip/badge primitives, so an app *cannot* accidentally convey meaning
   by color alone.
7. **Scaffolding and the gallery teach the accessible pattern.** `eggshell create-*` emits accessible
   templates; the gallery (§11) shows the one canonical, accessible usage per component (and its exposed
   role/name/state + contrast). Copy-paste leads to correct output.
8. **Escape hatches are explicit and rare.** `importantForAccessibility = No` for genuine decoration;
   `accessibilityLabel` override when inference is wrong. These are the exception, visibly so.
9. **Measured, so it can't silently regress.** axe-core (web) + contrast lint in CI; a TalkBack/VoiceOver
   checklist for native. A regression fails the pipeline, not a future audit.

The friction test for any new primitive or API: *"Does the developer have to do anything beyond writing
the visible content to get an accessible result?"* If yes, redesign until the answer is "no" (or "one
obvious required label"). That is the bar.

---

## 14. Post-migration target architecture (latest RN / RNW / React 19 / web)

The migration (`FRONTEND_MODERNIZATION_REACTXP_TO_RNW.md`) is not just parity — it **upgrades the a11y
ceiling** and lets us retire most of §10's deferrals. Build toward this; each row notes the backlog item
it unblocks.

### 14.1 React Native New Architecture (Fabric / JSI / TurboModules)
- **Synchronous layout (Fabric)** makes reliable **focus management** possible — accurate measurement and
  `AccessibilityInfo.setAccessibilityFocus(findNodeHandle …)` on route change / dialog open-close.
  → unblocks §9 #16 (focus management) toward `[safe]`.
- TurboModule `AccessibilityInfo` exposes the full flag set + change events cheaply → powers the
  `With.Accessibility` hook (§9 #7) on solid ground.

### 14.2 Modern RN a11y prop surface (adopt in the seam)
RN converged on a **cross-platform** vocabulary the wrapper should map once:
- W3C **`role`** (superset of `accessibilityRole`); **`aria-*`** props that work on iOS/Android/web:
  `aria-label`, `aria-labelledby`, `aria-live`, `aria-modal`, `aria-hidden`, `aria-expanded`,
  `aria-selected`, `aria-checked`, `aria-busy`, `aria-valuenow/min/max`.
- `accessibilityLanguage` (per-node language, WCAG 3.1.2), `accessibilityActions` + `onAccessibilityAction`
  (custom rotor actions — e.g. "delete" without the swipe), queued `announceForAccessibility`.
- **Design:** the F# `LC.Pressable`/`RX.View` a11y props stay the same (the call site doesn't change);
  the **binding** maps them to `role`+`aria-*` natively. This is the §3 contract paying off.
  → makes §7/§8 semantics richer for free; unblocks announce/actions (§9 #8, #15).

### 14.3 react-native-web → real ARIA + DOM (the `[web-only]` items become real)
RNW renders true DOM, so the deferred web items get first-class implementations:
- **`:focus-visible`** keyboard rings; **landmarks** via semantic host elements + heading levels;
  **skip links**; **roving-tabindex** handled by real focus order. → unblocks §9 #17, #18, §4.10.
- **`inert`** for the background behind a modal; native **`<dialog>`** and the **Popover API**
  (top-layer, focus-trap, Esc, light-dismiss) → accessible modals/menus **for free** instead of
  hand-built focus traps. → big win for dialogs.
- **`prefers-reduced-motion` / `prefers-contrast` / `prefers-reduced-transparency` / `color-scheme` /
  `forced-colors`** (Windows High Contrast) media queries → the `With.Accessibility` flags resolve to
  real web signals; **`:has()`** and **container queries** for component-level responsive a11y.

### 14.4 React 19
- **Form Actions / `useActionState`** → accessible submit with built-in pending/error state to announce
  (pairs with form-error live regions, §4.6/§7.4).
- **Document metadata / `<title>`** per route → page titles (WCAG 2.4.2) on web.
- **ref-as-prop** simplifies focus refs; improved Suspense → predictable, announceable loading states.

### 14.5 Modern motion / gesture / media stack
- **Reanimated 4 + Moti** run on the UI thread and read the **reduce-motion** flag declaratively →
  motion a11y (§4.7) becomes a property of the animation API, not per-screen code. **View Transitions**
  on web (reduced-motion-aware). → unblocks §9 #11 fully.
- **RNGH** gives robust swipe/drag with a clean place to attach the required **non-gesture alternative**
  (tap/keyboard/rotor action) for WCAG 2.5.1 → the swipe-to-delete in `ui2.html` becomes implementable
  *with* its alternative. → unblocks the gesture parts of §4.4.
- **Captions/haptics** (`react-native-video` text tracks, `expo-haptics`) → §4.5 `[lib]` items land on a
  supported stack.

### 14.6 Color & i18n
- Emerging CSS (`color-contrast()`, relative color syntax) on web for guaranteed-contrast theming;
  **logical properties** (`margin-inline-start`, `inset-inline`) + writing-direction-aware flexbox on
  New Arch → the RTL/logical-layout pass (§9 #14) becomes `[safe]`.

### 14.7 Net effect on the backlog
After migration, the deferred set (§10) largely resolves: focus management, landmarks, skip links,
focus-visible, roving-tabindex, accessible modals (via `<dialog>`/`inert`), reduced-motion-aware
animation, captions/haptics, and RTL all move to **implemented** — most behind the **same F# call sites**
we write today. The investment now in `[safe]` semantics (§9 #9) and the contract (§3) is exactly what
makes that transition free at the app layer.
