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
| `window.__DEV__` errors at load time | RN/RNW expects `__DEV__` to be set at load | Add `window.__DEV__ = true` in the app's `public-dev/index.html` (already set in AppEggShellGallery). |
| RNGH or async-storage Webpack 5 `fullySpecified` errors | ESM modules without explicit extensions | Add `fullySpecified: false` rule in `Meta/LibFablePlus/webpack.config.js` for those packages. |

For a complete catalog of build, styling, and layout gotchas across all platforms, see [Troubleshooting](./runbooks/troubleshooting.md).
