# Web Runbook

Starting the EggShell web dev server, observing the running app in a browser, and dealing with web-specific gotchas.

Related: [Dev loop](./runbooks/dev-loop.md) | [Build and rebuild](./runbooks/build-rebuild.md) | [Audit toolkit](./runbooks/audit-toolkit.md) | [Troubleshooting](./runbooks/troubleshooting.md)

---

## Start dev-web and open in a browser {#start}

From the app directory:

```bash
cd SuiteTodo/AppTodo
../../eggshell dev-web        # default port 9080; binds 0.0.0.0 (every interface)
open http://127.0.0.1:9080
```

For the EggShell Gallery:

```bash
cd AppEggShellGallery
../eggshell dev-web            # serves on port 8082
open http://127.0.0.1:8082
```

On startup, webpack prints every reachable URL (127.0.0.1, LAN IPs, etc.).

**Important:** after any LibClient change, **restart `eggshell dev-web`** rather than relying on hot reload. The stale `LibStandard/.build/web/fable` precompiled lib is what webpack serves; a restart picks up the fresh output.

---

## No backend needed — fake service {#fake-service}

`dev-web` alone gives a fully working AppTodo with seeded todos; you do **not** need the Orleans/SQL backend. AppTodo uses **fake in-memory todos** whenever `BackendUrl` is commented out in `SuiteTodo/AppTodo/configSourceOverrides.dev.js` (the default), wiring `FakeTodoService`:

```js
// SuiteTodo/AppTodo/configSourceOverrides.dev.js
// eggshell.AppTodo.configSourceOverrides.BackendUrl = "http://localhost:5001";  // ← leave COMMENTED for fake todos
```

This matters because the backend does not run everywhere — on **Apple Silicon** the SQL Server full-text-search container will not start, so fake service is the only way to exercise the web app there. If `BackendUrl` is **uncommented** and nothing answers on it, the app hangs on **"Loading…"** and `npm run audit:web` fails with *"Todo UI not ready (no heading or inputs)"*. Fix: comment `BackendUrl` back out and restart `dev-web`. (Gallery has no backend dependency at all.)

---

## Observe {#observe}

**Scripted / headless (Tier 2):**

```bash
npm run audit:web                              # Playwright smoke: adds a todo, fails on console/page errors
npm run observe -- snapshot -p web             # structured snapshot bundle: screenshot + DOM summary + health + logs
npm run observe -- snapshot -p web --orientation landscape
npm run observe -- open                        # keep a browser open for manual inspection
```

**Manual:** open browser DevTools. The console is the fastest way to read errors on web.

**Verifying a change reached the bundle:**

```bash
curl http://127.0.0.1:9080/index.js | grep -c "<your-marker>"   # 0 = stale; restart dev-web
```

---

## Web-specific gotchas {#gotchas}

| Symptom | Cause | Fix |
|---|---|---|
| Blank white page; console `ReferenceError: map is not defined` | Fable watch partial write left a stray `map` line at the end of a generated `.js` | Delete the affected `.js` file and force a full `dotnet fable ... --noCache`, or restart `dev-web` after a clean. |
| `Directory is locked, waiting...` / wrong Fable version | A prior `dotnet fable` run left `.build/web/fable/fable.lock` behind | Delete `LibStandard/.build/web/fable/fable.lock`, kill stray `dotnet fable` processes, confirm `dotnet fable --version` is `5.4.0`, wipe `.build/web/fable` after a tool bump. |
| Stale-cache false green: `Skipped compilation ... up-to-date` | Fable exited 0 without re-checking your edits | `touch` the changed `.fs` file, or `rm -rf LibStandard/.build/web/fable`; confirm `Started Fable compilation...` in the output. |
| Framework change not visible after `dev-web` restart | Stale precompiled lib served by webpack | Run `dotnet fable precompile src -o .build/web/fable ...` from `LibStandard/`, then `touch` the app's `App.fs` to trigger webpack. |
| `Module not found: Can't resolve 'react'` | JS deps live in library `node_modules`, not the app | Run the **root** `./initialize` (installs every lib's `node_modules`), not just the app's. |
| `--run webpack-dev-server` never starts | The first Fable compile failed; webpack only starts after a successful compile | Fix the F# error, then webpack starts on the next successful compile. |
| App stuck on **"Loading…"**; `audit:web` says *"Todo UI not ready"* | Either a boot-time page error (open DevTools console / dump `pageerror`), or AppTodo is trying to reach a backend that is not running | Check the console first. If it is a backend timeout, comment out `BackendUrl` in `configSourceOverrides.dev.js` to use the fake service (see [#fake-service](#fake-service)). |
| Boot `ReferenceError: process is not defined` (from `react-native-worklets/.../platformChecker.js`) | Webpack 5 does not polyfill `process`; worklets reads `process.env.JEST_WORKER_ID` at import | **Fixed centrally**: `Meta/LibFablePlus/webpack.config.js` defines it via `DefinePlugin`. If it recurs, that plugin was dropped. |
| Boot `ReferenceError: __DEV__ is not defined` (from `react-native-reanimated`) | `__DEV__` is a Metro/RN global webpack does not inject | **Fixed centrally**: same `DefinePlugin` defines `__DEV__` for every app's web bundle. (Superseded the old per-app `window.__DEV__ = true` in `public-dev/index.html`, which AppTodo lacked.) |
| RNGH or async-storage Webpack 5 `fullySpecified` errors | ESM modules without explicit extensions | Add `fullySpecified: false` rule in `Meta/LibFablePlus/webpack.config.js` for those packages. |

For a complete catalog of build, styling, and layout gotchas across all platforms, see [Troubleshooting](./runbooks/troubleshooting.md).
