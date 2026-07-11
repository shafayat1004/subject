---
name: a11y-check
description: Check new or edited F# components and app pages against the mandatory accessibility bar (label, role, state, testId, live regions, target size, gesture alternatives, text scaling). Use after writing or editing any UI component or page, and before declaring UI work done. Accessibility is mandatory in this repo (CLAUDE.md rule 12).
user-invocable: true
argument-hint: "<file.fs|dir>"
---

# a11y-check

## The bar (per archetype)

| Archetype           | label    | role  | state              | testId   | live region        |
|---------------------|----------|-------|--------------------|----------|--------------------|
| Interactive control | required | required | disabled/selected | required | -                  |
| Input/form leaf     | required (associated) | field role | disabled/checked | required | error -> assertive |
| Leaf display        | if status-bearing | - | -                 | optional | if it announces    |

Cross-cutting, all UI:
- One labeled press target per control: `LC.Pressable(label = "...")`.
- Decorative icons hidden (`importantForAccessibility = No` / no announcement).
- Touch targets >= 44px; text scales without clipping; color never the sole signal (WCAG AA).
- Every gesture has a non-gesture alternative; dynamic changes announce via a live region.
- Bake semantics into the primitive over per-call patching. If genuinely rnw-blocked, say so and
  use the portable subset; never silently skip.

## Procedure

1. `node .claude/skills/a11y-check/scripts/scan-a11y.mjs <file-or-dir>` flags Pressables/inputs
   missing `label`/`testId` (heuristic; judge each).
2. Walk the archetype table for every interactive element you touched.
3. Runtime verify: web page loaded, run the gallery a11y audit
   (`AppEggShellGallery/audit-gallery-a11y.mjs` via `npm run observe`/audit toolkit), or check
   elements expose `data-testid` + accessible name in the DOM.
4. Real screen reader spot-check for new interaction patterns: TalkBack (Android) / VoiceOver
   (iOS): name + role + state announced.

## Doc refs

- accessibility/recipes.md (per-component patterns + verification checklist)
- accessibility/index.md (principles, full spectrum)
- modernization/render-dsl-retirement.md (a11y bar table per archetype)

(All under AppEggShellGallery/public-dev/docs/.)
