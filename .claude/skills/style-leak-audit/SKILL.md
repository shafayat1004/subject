---
name: style-leak-audit
description: Audit F# components and app pages for style leaks, which are makeViewStyles/makeTextStyles calls that run during render, memoization keyed on records, or CE-colliding parameter names. Use after writing or editing any component or page that touches styles, and before declaring UI work done.
user-invocable: true
argument-hint: "<file.fs|dir>"
---

# style-leak-audit

Hard rule (CLAUDE.md + fsharp/styling.md): a `makeViewStyles`/`makeTextStyles` must NEVER execute
inside render. Every render allocates fresh style objects, defeating reconciliation and the style
cache; the runtime StyleLeakDetector fires on the second render.

## Legal placements

1. Top-level `let foo = makeViewStyles {...}` (static styles).
2. `ViewStyles.Memoize(fun (primitiveArgs) -> makeViewStyles {...})` / `TextStyles.Memoize(...)`,
   keyed on primitives only: Color, int, float, bool, string, small DU. NEVER a whole
   `Theme`/`Colors` record (fresh record refs on every render mean the cache never hits).
3. Memoized lambda parameter names must not shadow CE operations (`height`, `color`, `fontSize`,
   `top`, `left`, `bottom`). Shadowing makes `height height` parse as an application and fails
   with "This value is not a function and cannot be applied". Rename: `itemHeight`, `labelColor`.

## Procedure

1. `node .claude/skills/style-leak-audit/scripts/scan-style-leaks.mjs <file-or-dir>`
2. Judge each finding against the rules above (the scanner is a heuristic, not a verdict).
3. Fix: static -> hoist to top-level `let`; parametrized -> `Memoize` keyed on primitives;
   collision -> rename the lambda parameter.
4. Runtime confirmation when in doubt: run the page (debug-web skill) and watch the browser
   console for StyleLeakDetector output on re-render.

## Doc refs

- fsharp/styling.md (section "Avoiding style leaks": full rationale + recipes)
- runbooks/troubleshooting.md (style-leak symptoms)

(All under AppEggShellGallery/public-dev/docs/.)
