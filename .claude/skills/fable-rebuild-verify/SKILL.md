---
name: fable-rebuild-verify
description: Prove a Fable rebuild actually happened before claiming a change works. Use when a code edit seems to have no effect, when the build says "Skipped compilation because all generated files are up-to-date!", when asking "did my patch reach the bundle", or before declaring any frontend change done. Also cleans stray .fs.js files left by a wrongly-invoked dotnet fable.
user-invocable: true
argument-hint: "<projdir> <native|web> [changed.fs ...]"
---

# fable-rebuild-verify

Fable caches aggressively and will exit 0 WITHOUT type-checking your edits ("Skipped compilation
because all generated files are up-to-date!"). Never trust a green build you did not prove.

## Never do this

- NEVER run `dotnet fable` / `dotnet fable watch` directly: it emits `.fs.js` beside sources.
  Correct entry points: `./eggshell build-lib` (from lib dir), `eggshell dev-web`,
  `eggshell dev-native`. Output belongs in `.build/<platform>/fable` + `.build/native/commonjs`.
- NEVER pass `-c "Web Debug"` to fable; that is a `dotnet build` flag.
- If the user already has a watch/dev terminal running, their watch beats a separate cached build.

## Procedure (3-tier escalation)

Tier 1: `scripts/rebuild-verify.sh <projdir> <platform> <changed.fs ...>`
  touches the changed files, polls up to 90s for the emitted `.js` under the build dir to become
  newer than the source, reports PASS/FAIL with timestamps. Also runs the stray-output check.

Tier 2 (Tier 1 FAIL): `scripts/rebuild-verify.sh <projdir> <platform> --wipe`
  wipes `.build/<platform>/fable`, then you restart the build/watch
  (`eggshell dev-native` / `eggshell dev-web` / `./eggshell build-lib`) and re-run Tier 1.

Tier 3 (still FAIL): kill the wedged watch (`pkill -f "eggshell dev"`), restart it, restart Metro
with cache reset: `npx react-native start --port 8081 --reset-cache`. Re-run Tier 1.

## Proof gate (all three before "done")

1. Watch/build log shows `Started Fable compilation...` (not "Skipped").
2. `rg "error FS"` over the build output is clean.
3. `rebuild-verify.sh` prints PASS (emitted JS newer than every edited .fs).

## Silent-wedge gotchas (learned the hard way)

- **A frozen emit timestamp is a false green.** "grep finds my value in the bundle" is NOT proof the
  edit compiled — the value may be from an old compile. The only proof is
  `stat -f %m <emitted.js>` >= `stat -f %m <source.fs>`. If you keep editing and the emit mtime never
  advances, the watch is **silently wedged** (fs-events stopped) — go straight to Tier 3, don't keep
  editing and re-checking `grep`.
- **`touch` does not force a recompile.** Fable's up-to-date check is content-hash based, so bumping
  mtime alone is a no-op. To force one file: make a real content change (even a comment), or Tier 3.
  (This also means Tier 1's `touch`-and-poll can FAIL-timeout on a wedged watch even though the source
  is fine — the failure is the watch, not your edit.)
- **An external formatter/editor save can desync the watch.** Batch-reformatting files out of band can
  leave some recompiled and some not; verify each edited file's emit mtime individually.
- **Script path caveat:** `rebuild-verify.sh` assumes `<projdir>/.build/<platform>/commonjs`; in apps
  where the build dir sits at the app root (e.g. `SuiteTodo/AppTodo/.build/native/`), pass the app dir
  or verify the emit mtime by hand instead of trusting a "build output dir missing" FAIL.
- **Gallery `dev-web` watch does NOT recompile LibClient/LibRouter.** The gallery's Fable watch only
  recompiles the gallery's OWN `.fs` files; `LibClient`/`LibRouter`/`LibLangFsharp` are precompiled
  into the shared `LibStandard/.build/web/fable/` tree (the gallery's JS imports them from
  `../../../../../../LibStandard/.build/web/fable/...`), and that tree is only rebuilt on a FULL
  `dev-web` start, not on incremental lib edits. Symptom: you edit `LibClient/src/EggShellReact.fs`,
  the gallery reloads, but the change has no runtime effect; `stat` shows the
  `LibStandard/.build/web/fable/.../EggShellReact.js` mtime is OLDER than your `.fs` edit. Fix: kill
  the stale `:8082` webpack + children (`pkill -9 -f "eggshell dev-web"; pkill -9 -f fable;
  pkill -9 -f webpack`) and restart `cd AppEggShellGallery && ../eggshell dev-web`. A surviving old
  webpack makes the new start hit `EADDRINUSE :8082` and silently serve the STALE bundle — use the
  broad `pkill -9 -f webpack`. When verifying, check `LibStandard/.build/web/fable/...` freshness,
  NOT just `AppEggShellGallery/.build/web/fable/...` (the latter only holds the gallery's own files).

## Doc refs

- runbooks/build-rebuild.md (targeted recompile, confirm-rebuild, restart rules)
- runbooks/troubleshooting.md (build-cache section)
- runbooks/dev-loop.md (moving parts: Fable watch vs Metro vs native shell)

(All under AppEggShellGallery/public-dev/docs/.)
