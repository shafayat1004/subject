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

## Observe (Tier 2, preferred)

From the app dir:
- `npm run audit:web` (Playwright smoke: interactions + console/page errors)
- `npm run observe -- snapshot -p web` (screenshot + DOM summary + health + logs)
- `npm run observe -- snapshot -p web --orientation landscape`
- `npm run observe -- doctor` (checks PATH, devices, Appium :4723, Metro :8081, dev-web :9080)

Gallery audit toolkit: `AppEggShellGallery/audit-gallery-*.mjs` (components, interactions, a11y,
style-leaks). Selectors use `data-testid` (react-native-web DOM).

## Console errors

Playwright observe captures page errors. For manual checks, load the page and read the browser
console; React key warnings and StyleLeakDetector output land there.

## Routing

Web routes by pathname; navigate directly to `http://localhost:<port>/<route>` to deep-link a page.

## Doc refs

- runbooks/web.md
- runbooks/audit-toolkit.md
- runbooks/build-rebuild.md (web build output layout)

(All under AppEggShellGallery/public-dev/docs/.)
