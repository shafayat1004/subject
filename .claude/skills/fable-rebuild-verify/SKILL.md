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

## Doc refs

- runbooks/build-rebuild.md (targeted recompile, confirm-rebuild, restart rules)
- runbooks/troubleshooting.md (build-cache section)
- runbooks/dev-loop.md (moving parts: Fable watch vs Metro vs native shell)

(All under AppEggShellGallery/public-dev/docs/.)
