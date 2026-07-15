---
name: debug-web
description: Set up and drive the web dev loop for this repo, including dev servers, Playwright-based observation, browser console reading, and bundle-freshness checks. Use for any web debugging, observing gallery or AppTodo in a browser, or running web audits. Read runbooks/web.md before improvising.
user-invocable: true
argument-hint: "[preflight [port]]"
---

# debug-web

## Servers and ports

- Gallery: `cd AppEggShellGallery && ../eggshell dev-web` on :8082
- AppTodo: `cd SuiteTodo/AppTodo && ../../eggshell dev-web` on :9080

`scripts/web-preflight.sh [port]` checks the server responds and warns if source `.fs` files are
newer than `.build/web/fable` output (stale bundle: invoke fable-rebuild-verify).

**Lib edits need a full `dev-web` restart (not just a watch tick).** The gallery's `dev-web` Fable
watch only recompiles the gallery's OWN `.fs` files; `LibClient`/`LibRouter`/`LibLangFsharp` are
precompiled into the shared `LibStandard/.build/web/fable/` tree (the gallery's JS imports them
from there), and that tree is only rebuilt on a FULL `dev-web` start. If you edit a lib `.fs`
while `dev-web` is running and the change has no effect, kill the stale `:8082` webpack
(`pkill -9 -f "eggshell dev-web"; pkill -9 -f fable; pkill -9 -f webpack`) and restart
`cd AppEggShellGallery && ../eggshell dev-web`. A surviving old webpack makes the new start hit
`EADDRINUSE :8082` and silently serve the STALE bundle — use the broad `pkill -9 -f webpack`.
See fable-rebuild-verify skill + runbooks/build-rebuild.md #mandatory-restart.

## No backend needed — fake service (READ THIS before "app won't load")

AppTodo runs on **fake in-memory todos** by default: `BackendUrl` is commented out in
`SuiteTodo/AppTodo/configSourceOverrides.dev.js`. So `dev-web` alone gives a fully working app with
seeded todos — you do **not** need the Orleans/SQL backend running (and on some hosts, e.g. Apple
Silicon, SQL Server full-text search will not start at all). If `BackendUrl` is uncommented and no
backend answers, the app hangs on **"Loading…"** and `audit:web` reports *"Todo UI not ready"* — comment
`BackendUrl` back out. See `runbooks/web.md` #fake-service.

## Observe (Tier 2, preferred)

From the app dir:
- `npm run audit:web` (Playwright smoke: interactions + console/page errors)
- `npm run observe -- snapshot -p web` (screenshot + DOM summary + health + logs)
- `npm run observe -- snapshot -p web --orientation landscape`
- `npm run observe -- doctor` (checks PATH, devices, Appium :4723, Metro :8081, dev-web :9080)

Gallery audit toolkit: `AppEggShellGallery/audit-gallery-*.mjs` (components, interactions, a11y,
style-leaks). Selectors use `data-testid` (react-native-web DOM).

## Diagnosing layout bugs (measure the DOM, don't eyeball)

For a layout bug (clipping, collapse, stray scrollbar, wrong size), a throwaway Playwright script that
**measures** beats screenshots. `page.evaluate` inside the app's `node_modules` (run the `.mjs` from
the app dir so `import { chromium } from 'playwright'` resolves):
- `el.getBoundingClientRect()` for box geometry; `getComputedStyle(el)` for `flexShrink`, `overflowX`,
  `scrollbarWidth`, padding, etc. Diff a child's rect against its container's to quantify a clip.
- `el.scrollWidth > el.clientWidth` to detect real overflow; `rect.height - el.clientHeight` = the
  scrollbar strip a space-reserving bar is eating.
- Poll the same measure at `t = 150 / 500 / 1500 / 3500 ms` after `domcontentloaded` to catch a
  first-paint transient that self-corrects.
- `select:data-testid` selectors (RNW emits `data-testid`); the outermost element of an `LC.TextButton`
  is an absolutely-positioned tap-capture overlay inset **-12px** — measure the bordered child, not the
  testid element, for the visible box.

## Browser-engine caveats (learned the hard way)

- **Playwright WebKit ≠ Safari.app.** Playwright (Chromium AND WebKit, headless or headed) always uses
  **overlay** scrollbars (reserve no space), so it CANNOT reproduce a Safari.app bug caused by classic
  space-reserving scrollbars, nor Safari's first-mount RNW/Fabric layout quirks. If a bug reproduces
  only in the user's real Safari, your Playwright probes will look healthy — say so; don't claim fixed.
- **Headed mode exists** (`chromium.launch({ headless:false })`, same for `webkit`) — use it when you
  suspect headless-specific rendering, but it does not change the overlay-scrollbar limitation above.
- Known open example: horizontal `LC.ScrollView` clips on first mount **in Safari only**
  (`modernization/rn86-upgrade-status.md` RW9) — not reproducible in tooling.

## Console errors

Playwright observe captures page errors. For manual checks, load the page and read the browser
console; React key warnings and StyleLeakDetector output land there.

### Quick console capture (preferred over eyeballing)

`scripts/console-check.mjs` loads one or more URLs headless, captures every console error +
warning (deduplicated), prints them, and exits non-zero if any ERRORS remain. Run it from the app
dir so `playwright` resolves:

```bash
cd AppEggShellGallery
node ../../.claude/skills/debug-web/scripts/console-check.mjs                      # defaults: / + a docs deep-link
node ../../.claude/skills/debug-web/scripts/console-check.mjs --port 9080           # AppTodo default port
node ../../.claude/skills/debug-web/scripts/console-check.mjs --filter "key prop|boxShadow"  # narrow
node ../../.claude/skills/debug-web/scripts/console-check.mjs --full                # include React component stacks
node ../../.claude/skills/debug-web/scripts/console-check.mjs http://localhost:8082/components  # custom URLs
```

Use it to **prove a fix cleared an error** (run before + after the fix on a fresh bundle) or to
**triage which errors remain** without leaving the terminal.

### Console-error triage procedure (React key / DOM-nesting / deprecated-API warnings)

1. Read the mangled Fable component name in the warning (e.g.
   `LibClient_Components_Constructors_LCModule_Executor__Executor_DisplayErrorsManually_Static_162D5454`).
   Strip the `Lib..._` prefix + `_Static_<HASH>` suffix → `LCModule.Executor.DisplayErrorsManually`
   → `LC.Executor.DisplayErrorsManually` → grep the F# source for `DisplayErrorsManually`.
2. Determine whether the warning is **one call site's bug** or a **primitive's bug**. If multiple
   unrelated components warn the same way (e.g. key warnings from `Executor.DisplayErrorsManually`,
   `LR.With.Route`, `Ui.Route.Docs`), the shared primitive is wrong — fix the primitive
   (`castAsElementAckingKeysWarning`, `element { }` CE, `asFragment`) so all callers benefit
   (rule 3: reuse, don't duplicate). If only one call site warns, fix it locally.
3. Rebuild + verify with `console-check.mjs` on a **fresh bundle** (see fable-rebuild-verify;
   gallery lib edits need a full `dev-web` restart, not just a watch tick).

### Verify a CSS / style fix rendered (don't eyeball — measure)

A `getComputedStyle` probe via Playwright proves a style change reached the DOM (e.g. after
switching `shadow*` → `boxShadow`, confirm `getComputedStyle(el).boxShadow` is non-empty):

```js
await page.evaluate(() => {
  const el = document.querySelector('[data-testid="eggshell-nav-top"]');
  return el ? { tag: el.tagName, boxShadow: getComputedStyle(el).boxShadow } : { found: false };
});
```

Run such probes from the app dir (`import { chromium } from 'playwright'` resolves there). See
"Diagnosing layout bugs" above for the `getBoundingClientRect` / `scrollWidth` measurement pattern.

## Routing

Web routes by pathname; navigate directly to `http://localhost:<port>/<route>` to deep-link a page.

## Doc refs

- runbooks/web.md
- runbooks/audit-toolkit.md
- runbooks/build-rebuild.md (web build output layout)

(All under AppEggShellGallery/public-dev/docs/.)
