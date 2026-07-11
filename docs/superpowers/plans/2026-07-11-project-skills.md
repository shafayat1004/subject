# Project Skills + Formatting Burn-in Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create 10 project-scoped Claude skills under `.claude/skills/` with helper scripts, plus a mandatory F# formatting block in CLAUDE.md, per spec `docs/superpowers/specs/2026-07-11-project-skills-design.md`.

**Architecture:** Each skill is a directory `.claude/skills/<name>/` with `SKILL.md` (frontmatter + terse procedure) and optional `scripts/` (standalone zsh/node, executable). Skills are thin wrappers: they carry command skeletons, decision trees, and validation gates, and point into the canonical docs at `AppEggShellGallery/public-dev/docs/`. Scanners are advisory (report, never block).

**Tech Stack:** zsh scripts, node >= 18 ESM scripts (no npm deps), rg, adb, xcrun, existing eggshell CLI and audit-*.mjs toolkit.

## Global Constraints

- Repo root: `/Volumes/HomeX/shafayat/Code/subject`. All paths below relative to it unless absolute.
- Docs root: `AppEggShellGallery/public-dev/docs/` (skill doc refs are relative to this root).
- Skills live at `.claude/skills/<name>/SKILL.md`; scripts at `.claude/skills/<name>/scripts/`, `chmod +x`.
- SKILL.md frontmatter fields (match local `impeccable` convention): `name`, `description`, `user-invocable: true`, `argument-hint` where args exist.
- `description` must state WHEN to use the skill (trigger phrases), since that is what skill selection sees.
- No em-dash anywhere in new prose (org rule). Use commas or parentheses.
- No new package dependencies. Node scripts use only `node:fs`/`node:path`.
- Scripts exit 0 on findings (advisory), exit 2 on usage error. Exception: `check-doc-links.mjs` exits 1 on broken links (used as a gate).
- Every SKILL.md ends with a `## Doc refs` section listing its canonical doc files; those paths must exist (verify with `ls`).
- Commit after each task. Message format: `feat(skills): ...` / `docs: ...`, ending with:
  `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`
- Key facts (verified 2026-07-11): gallery web port 8082, AppTodo web port 9080, Metro port 8081, Appium port 4723; gallery Android package `com.eggshell.appgallery`, AppTodo `com.eggshell.apptodo`, activity `.MainActivity`; AVD `Medium_Phone_API_35`; Fable output `.build/native/commonjs` (native) and `.build/web/fable` (web); fantomas via `dotnet tool run fantomas <file.fs>`.

---

### Task 1: CLAUDE.md formatting burn-in block + skills pointer

**Files:**
- Modify: `CLAUDE.md` (repo root)

**Interfaces:**
- Produces: the invariant "never run `dotnet fable` directly" and the script path `.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh` that Task 2 must create.

- [ ] **Step 1: Insert new section into CLAUDE.md** after the `## Component conventions (frontend)` section and before `## Build & validate`:

```markdown
## F# formatting (mandatory, every model, every edit)

Canonical reference: `AppEggShellGallery/public-dev/docs/fsharp/formatting.md`. Read it before any
non-trivial F# edit. Hard rules (rule number in parens):
- 4-space indent, never tabs (1). Soft line limit ~120 chars (3).
- Column alignment is hand-maintained, NOT enforced by Fantomas. Match surrounding code exactly:
  record-field `:` (2a), DU `of` (2b), short match-arm `->` (2c), let-binding `=` groups (2d),
  record-construction `=` (7).
- `match x with` on one line (5). DU cases one per line, labeled fields `Name: Type` (8).
- After editing F#, run `dotnet tool run fantomas <file.fs>` (pinned; `dotnet tool restore` once
  per machine). Fantomas will NOT restore alignment, so re-check 2a-2d by eye afterwards.
- **NEVER run `dotnet fable` directly.** It emits `.fs.js` beside the source files. Build only via
  `./eggshell build-lib` / `eggshell dev-web` / `eggshell dev-native`; output belongs under
  `.build/<platform>/`. If stray `.fs.js` files appear beside `.fs` sources, run
  `.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh`.

## Project skills

Procedure skills live in `.claude/skills/` and wrap the runbooks (rule 11): `fable-rebuild-verify`,
`debug-android`, `debug-ios`, `debug-web`, `style-leak-audit`, `a11y-check`, `docs-sync`,
`gallery-page-add`, `release-build`, `verify-feature`. Prefer invoking the matching skill over
improvising commands.
```

- [ ] **Step 2: Verify referenced doc exists**

Run: `ls AppEggShellGallery/public-dev/docs/fsharp/formatting.md`
Expected: file listed.

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: burn F# formatting rules + dotnet-fable guard + skills index into CLAUDE.md

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: fable-rebuild-verify skill

**Files:**
- Create: `.claude/skills/fable-rebuild-verify/SKILL.md`
- Create: `.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh`
- Create: `.claude/skills/fable-rebuild-verify/scripts/rebuild-verify.sh`

**Interfaces:**
- Produces: `clean-stray-fable-output.sh [--check] [root]` (referenced by CLAUDE.md from Task 1) and `rebuild-verify.sh <projdir> <native|web> [--wipe] [changed.fs ...]` (invoked by debug-* and verify-feature skills).

- [ ] **Step 1: Write SKILL.md**

```markdown
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
```

- [ ] **Step 2: Write clean-stray-fable-output.sh**

```zsh
#!/bin/zsh
# Finds (and by default deletes) .fs.js/.fs.js.map emitted beside .fs sources by a raw
# `dotnet fable` invocation. Correct Fable output lives under .build/<platform>/.
# usage: clean-stray-fable-output.sh [--check] [root]
set -u
MODE="delete"
[[ "${1:-}" == "--check" ]] && { MODE="check"; shift; }
ROOT="${1:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}"
STRAYS=$(find "$ROOT" \( -name "*.fs.js" -o -name "*.fs.js.map" \) \
  -not -path "*/.build/*" -not -path "*/node_modules/*" -not -path "*/fable_modules/*" 2>/dev/null)
if [[ -z "$STRAYS" ]]; then
  echo "OK: no stray .fs.js beside sources"
  exit 0
fi
COUNT=$(echo "$STRAYS" | wc -l | tr -d ' ')
echo "FOUND $COUNT stray Fable output file(s) beside sources (symptom of raw 'dotnet fable' run):"
echo "$STRAYS"
if [[ "$MODE" == "delete" ]]; then
  echo "$STRAYS" | while read -r f; do rm "$f"; done
  echo "DELETED $COUNT file(s). Rebuild via eggshell (never 'dotnet fable' directly)."
fi
```

- [ ] **Step 3: Write rebuild-verify.sh**

```zsh
#!/bin/zsh
# Prove a Fable rebuild reached the build output. See runbooks/build-rebuild.md.
# usage: rebuild-verify.sh <projdir> <native|web> [--wipe] [changed.fs ...]
set -u
usage() { echo "usage: rebuild-verify.sh <projdir> <native|web> [--wipe] [changed.fs ...]"; exit 2; }
PROJ="${1:-}"; PLAT="${2:-}"
[[ -d "$PROJ" && ( "$PLAT" == "native" || "$PLAT" == "web" ) ]] || usage
shift 2
WIPE=0; typeset -a FILES; FILES=()
for a in "$@"; do [[ "$a" == "--wipe" ]] && WIPE=1 || FILES+=("$a"); done

"$(dirname "$0")/clean-stray-fable-output.sh" --check "$PROJ"

BUILD="$PROJ/.build/$PLAT"
if [[ $WIPE -eq 1 ]]; then
  echo "wiping $BUILD/fable"
  rm -rf "$BUILD/fable"
  echo "now restart the build (eggshell dev-$PLAT / build-lib), then re-run without --wipe"
  exit 0
fi
[[ ${#FILES[@]} -gt 0 ]] || { echo "no changed .fs files given; nothing to verify"; exit 2; }

OUT="$BUILD/commonjs"; [[ "$PLAT" == "web" ]] && OUT="$BUILD/fable"
[[ -d "$OUT" ]] || { echo "FAIL: build output dir $OUT missing (build never ran?)"; exit 0; }

for f in "${FILES[@]}"; do touch "$f"; done
echo "touched ${#FILES[@]} file(s); polling $OUT for fresher emitted JS (90s max)..."
FAIL=0
for f in "${FILES[@]}"; do
  base="$(basename "$f" .fs)"
  js="$(find "$OUT" -name "$base.js" 2>/dev/null | head -1)"
  if [[ -z "$js" ]]; then echo "FAIL: no emitted $base.js under $OUT"; FAIL=1; continue; fi
  ok=0
  for i in {1..45}; do
    [[ "$js" -nt "$f" ]] && { ok=1; break; }
    sleep 2
  done
  if [[ $ok -eq 1 ]]; then echo "PASS: $js newer than $f"
  else echo "FAIL: $js still older than $f after 90s (stale cache or watch not running)"; FAIL=1; fi
done
echo "reminder: also confirm the watch log printed 'Started Fable compilation...' and has no 'error FS'"
[[ $FAIL -eq 0 ]] && echo "RESULT: PASS" || echo "RESULT: FAIL (escalate: --wipe, then restart watch + metro --reset-cache)"
```

- [ ] **Step 4: Make executable and test**

Run:
```bash
chmod +x .claude/skills/fable-rebuild-verify/scripts/*.sh
.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh --check
touch /tmp/stray-test/src/Foo.fs 2>/dev/null; mkdir -p /tmp/stray-test/src && touch /tmp/stray-test/src/Foo.fs /tmp/stray-test/src/Foo.fs.js
.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh /tmp/stray-test
ls /tmp/stray-test/src/
.claude/skills/fable-rebuild-verify/scripts/rebuild-verify.sh
```
Expected: repo check prints `OK` (or lists real strays, judge them); tmp run prints `FOUND 1` + `DELETED 1` and `Foo.fs.js` gone; last call prints usage and exits 2.

- [ ] **Step 5: Verify doc refs exist**

Run: `ls AppEggShellGallery/public-dev/docs/runbooks/build-rebuild.md AppEggShellGallery/public-dev/docs/runbooks/troubleshooting.md AppEggShellGallery/public-dev/docs/runbooks/dev-loop.md`
Expected: all three listed.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/fable-rebuild-verify
git commit -m "feat(skills): fable-rebuild-verify with stray .fs.js cleaner and rebuild proof gate

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: debug-android skill

**Files:**
- Create: `.claude/skills/debug-android/SKILL.md`
- Create: `.claude/skills/debug-android/scripts/android-preflight.sh`
- Create: `.claude/skills/debug-android/scripts/android-observe.sh`

**Interfaces:**
- Consumes: `fable-rebuild-verify` skill (Task 2).
- Produces: `android-preflight.sh` and `android-observe.sh {screenshot|logcat|clear|tap|rotate}` (invoked by verify-feature, Task 11).

- [ ] **Step 1: Write SKILL.md**

```markdown
---
name: debug-android
description: Set up and drive the Android device/emulator loop for this repo, including launching apps, screenshots, logcat reading, taps, and rotation. Use for any Android on-device task, debugging a native defect, checking whether the app boots, or observing UI state. Read runbooks/android.md before improvising.
user-invocable: true
argument-hint: "[preflight|screenshot|logcat|tap X Y|rotate]"
---

# debug-android

## Preflight (always first)

`scripts/android-preflight.sh` checks: device attached, `adb reverse tcp:8081 tcp:8081` set
(REQUIRED once per device boot, else blank screen / "could not connect to development server"),
Metro reachable on :8081.

No device? Emulator: `emulator -avd Medium_Phone_API_35 -no-snapshot-load &` then
`adb wait-for-device`. Sideways emulator: set `hw.initialOrientation = portrait` and
`skin.name = 1080x2400` in `~/.android/avd/<AVD>.avd/config.ini`.

## Launch

- Gallery: `adb shell am start -n com.eggshell.appgallery/.MainActivity`
- AppTodo:  `adb shell am start -n com.eggshell.apptodo/.MainActivity`
- Metro (from the app dir): `npx react-native start --port 8081`
- Fable watch (from the app dir): `../../eggshell dev-native` (AppTodo) / `../eggshell dev-native`

## Observe

`scripts/android-observe.sh screenshot [out.png]`, `... logcat [pattern]`, `... clear`,
`... tap X Y`, `... rotate {portrait|landscape}`.

Log facts: F# `Log.Info` does NOT reach logcat; only JS `console.log` surfaces, under tag
`ReactNativeJS`. Crashes: grep `FATAL EXCEPTION|Uncaught`. React key warnings: grep `unique .key.`.

## Tier 1 vs Tier 2

Tier 1 (raw adb, above): instant, but coordinate taps are brittle. Tier 2 (Appium via
`AppEggShellGallery/audit-gallery-android-driver.mjs`, Appium on :4723): deterministic testId
selectors. `npm run observe -- doctor` checks the whole chain. Escalate to Tier 2 when a tap must
hit an exact element or when verifying interactions repeatedly.

## Patch not showing?

Invoke the fable-rebuild-verify skill before debugging "broken" code. Then reload the app
(`adb shell input keyevent 82` for dev menu, or relaunch the activity).

## Doc refs

- runbooks/android.md (boot, logcat, physical device, release APK)
- runbooks/dev-loop.md (who owns which terminal)
- runbooks/audit-toolkit.md (Tier 2 observe/doctor)

(All under AppEggShellGallery/public-dev/docs/.)
```

- [ ] **Step 2: Write android-preflight.sh**

```zsh
#!/bin/zsh
# Android dev-loop preflight: device, adb reverse, Metro. See runbooks/android.md.
set -u
DEV=$(adb devices | tail -n +2 | awk '$2=="device"{print $1}' | head -1)
if [[ -z "$DEV" ]]; then
  echo "FAIL: no device/emulator attached."
  echo "  emulator -avd Medium_Phone_API_35 -no-snapshot-load &  && adb wait-for-device"
  exit 0
fi
echo "device: $DEV"
adb -s "$DEV" reverse tcp:8081 tcp:8081 && echo "adb reverse tcp:8081 OK"
if curl -sf --max-time 2 http://localhost:8081/status | grep -q running; then
  echo "Metro OK on :8081"
else
  echo "WARN: Metro not running. From the app dir: npx react-native start --port 8081"
fi
```

- [ ] **Step 3: Write android-observe.sh**

```zsh
#!/bin/zsh
# Raw (Tier 1) Android observation helpers. See runbooks/android.md.
# usage: android-observe.sh {screenshot [out.png]|logcat [pattern]|clear|tap X Y|rotate {portrait|landscape}}
set -u
CMD="${1:-}"; shift 2>/dev/null || true
case "$CMD" in
  screenshot)
    OUT="${1:-${TMPDIR:-/tmp}/android-$(date +%H%M%S).png}"
    adb exec-out screencap -p > "$OUT" && echo "$OUT" ;;
  logcat)
    PAT="${1:-FATAL EXCEPTION|Uncaught|ReactNativeJS|error}"
    adb logcat -d | grep -iE "$PAT" | tail -200 ;;
  clear) adb logcat -c && echo "logcat cleared" ;;
  tap) adb shell input tap "$1" "$2" && echo "tapped $1,$2 (brittle; prefer Tier 2 testId)" ;;
  rotate)
    adb shell settings put system accelerometer_rotation 0
    [[ "${1:-portrait}" == "landscape" ]] && R=1 || R=0
    adb shell settings put system user_rotation $R && echo "rotation: ${1:-portrait}" ;;
  *) echo "usage: android-observe.sh {screenshot [out]|logcat [pattern]|clear|tap X Y|rotate {portrait|landscape}}"; exit 2 ;;
esac
```

- [ ] **Step 4: Make executable and test**

Run:
```bash
chmod +x .claude/skills/debug-android/scripts/*.sh
.claude/skills/debug-android/scripts/android-observe.sh
.claude/skills/debug-android/scripts/android-preflight.sh
```
Expected: first prints usage, exit 2. Second prints either `device: ...` lines or `FAIL: no device` (both acceptable; no crash).

- [ ] **Step 5: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/runbooks/android.md AppEggShellGallery/public-dev/docs/runbooks/audit-toolkit.md`
Expected: both listed.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/debug-android
git commit -m "feat(skills): debug-android preflight + observe wrappers over runbook

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: debug-ios skill

**Files:**
- Create: `.claude/skills/debug-ios/SKILL.md`
- Create: `.claude/skills/debug-ios/scripts/ios-preflight.sh`
- Create: `.claude/skills/debug-ios/scripts/ios-observe.sh`

**Interfaces:**
- Consumes: `fable-rebuild-verify` skill (Task 2).
- Produces: `ios-preflight.sh` and `ios-observe.sh {screenshot|log|launch|terminate|openurl}` (invoked by verify-feature, Task 11).

- [ ] **Step 1: Write SKILL.md**

```markdown
---
name: debug-ios
description: Set up and drive the iOS simulator loop for this repo, including boot, install, launch, screenshots, log reading, and deep links. Use for any iOS on-device/simulator task, debugging a native defect, or observing UI state. Read runbooks/ios.md before improvising.
user-invocable: true
argument-hint: "[preflight|screenshot|log|launch <bundleid>]"
---

# debug-ios

## Preflight (always first)

`scripts/ios-preflight.sh` checks: a simulator is Booted (boots "iPhone 16" if not), Simulator app
open, Metro reachable on :8081.

## Install + launch

- First install (from the app dir): `npx react-native run-ios --simulator "iPhone 16" --no-packager`
- Subsequent runs: `scripts/ios-observe.sh terminate <bundleid> && scripts/ios-observe.sh launch <bundleid>`
- Bundle ids: AppTodo `com.eggshell.apptodo`; gallery: read it from
  `AppEggShellGallery/ios/AppEggshellGallery/Info.plist` (CFBundleIdentifier).
- Metro (from the app dir): `npx react-native start --port 8081`
- Fable watch (from the app dir): `eggshell dev-native` (path-relative eggshell).

## Observe

`scripts/ios-observe.sh screenshot [out.png]`, `... log [minutes]`,
`... launch <bundleid>`, `... terminate <bundleid>`, `... openurl <url>` (deep links).

## Gotchas (from the runbook and engineering log)

- Always open the `.xcworkspace`, never the `.xcodeproj` (pods).
- Pod trouble: verify `platform :ios, '15.5'` in the Podfile; npm peer conflicts may need
  `--legacy-peer-deps`.
- JS `console.log` shows in the Metro terminal and in `log show` output; F# `Log.Info` does not.

## Patch not showing?

Invoke the fable-rebuild-verify skill first, then relaunch the app.

## Doc refs

- runbooks/ios.md (boot, install, screenshot, logs, gotchas)
- runbooks/dev-loop.md
- runbooks/audit-toolkit.md

(All under AppEggShellGallery/public-dev/docs/.)
```

- [ ] **Step 2: Write ios-preflight.sh**

```zsh
#!/bin/zsh
# iOS dev-loop preflight: booted simulator + Metro. See runbooks/ios.md.
set -u
SIM="${1:-iPhone 16}"
if xcrun simctl list devices | grep -q "Booted"; then
  echo "simulator booted:"; xcrun simctl list devices | grep Booted
else
  echo "booting '$SIM'..."
  xcrun simctl boot "$SIM" || { echo "FAIL: cannot boot '$SIM'; available:"; xcrun simctl list devices available | tail -20; exit 0; }
fi
open -a Simulator
if curl -sf --max-time 2 http://localhost:8081/status | grep -q running; then
  echo "Metro OK on :8081"
else
  echo "WARN: Metro not running. From the app dir: npx react-native start --port 8081"
fi
```

- [ ] **Step 3: Write ios-observe.sh**

```zsh
#!/bin/zsh
# iOS simulator observation helpers. See runbooks/ios.md.
# usage: ios-observe.sh {screenshot [out.png]|log [minutes]|launch <bundleid>|terminate <bundleid>|openurl <url>}
set -u
CMD="${1:-}"; shift 2>/dev/null || true
case "$CMD" in
  screenshot)
    OUT="${1:-${TMPDIR:-/tmp}/ios-$(date +%H%M%S).png}"
    xcrun simctl io booted screenshot "$OUT" && echo "$OUT" ;;
  log)
    MIN="${1:-2}"
    xcrun simctl spawn booted log show --last "${MIN}m" --style compact \
      | grep -iE "ReactNativeJS|error|crash|exception" | tail -200 ;;
  launch) xcrun simctl launch booted "$1" ;;
  terminate) xcrun simctl terminate booted "$1" 2>/dev/null; echo "terminated $1" ;;
  openurl) xcrun simctl openurl booted "$1" ;;
  *) echo "usage: ios-observe.sh {screenshot [out]|log [minutes]|launch <id>|terminate <id>|openurl <url>}"; exit 2 ;;
esac
```

- [ ] **Step 4: Make executable and test**

Run:
```bash
chmod +x .claude/skills/debug-ios/scripts/*.sh
.claude/skills/debug-ios/scripts/ios-observe.sh
grep -A1 CFBundleIdentifier AppEggShellGallery/ios/AppEggshellGallery/Info.plist
```
Expected: usage + exit 2; the grep prints the gallery bundle id. If the bundle id is a literal string (not a `$(...)` build variable), replace the "read it from Info.plist" sentence in SKILL.md with the actual id.

- [ ] **Step 5: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/runbooks/ios.md`
Expected: listed.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/debug-ios
git commit -m "feat(skills): debug-ios preflight + observe wrappers over runbook

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: debug-web skill

**Files:**
- Create: `.claude/skills/debug-web/SKILL.md`
- Create: `.claude/skills/debug-web/scripts/web-preflight.sh`

**Interfaces:**
- Consumes: `fable-rebuild-verify` skill (Task 2).
- Produces: `web-preflight.sh [port]` (invoked by verify-feature, Task 11).

- [ ] **Step 1: Write SKILL.md**

```markdown
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
```

- [ ] **Step 2: Write web-preflight.sh**

```zsh
#!/bin/zsh
# Web dev-loop preflight: server up + bundle freshness. See runbooks/web.md.
# usage: web-preflight.sh [port] [appdir]
set -u
PORT="${1:-8082}"
APP="${2:-AppEggShellGallery}"
if curl -sf --max-time 3 "http://localhost:$PORT/" > /dev/null; then
  echo "dev-web OK on :$PORT"
else
  echo "FAIL: nothing on :$PORT. Gallery: cd AppEggShellGallery && ../eggshell dev-web (8082); AppTodo: cd SuiteTodo/AppTodo && ../../eggshell dev-web (9080)"
  exit 0
fi
if [[ -d "$APP/.build/web/fable" ]]; then
  NEWSRC=$(find "$APP/src" -name "*.fs" -newer "$APP/.build/web/fable" 2>/dev/null | head -5)
  if [[ -n "$NEWSRC" ]]; then
    echo "WARN: sources newer than web build output (stale bundle?); run fable-rebuild-verify:"
    echo "$NEWSRC"
  else
    echo "bundle freshness OK"
  fi
fi
```

- [ ] **Step 3: Make executable and test**

Run:
```bash
chmod +x .claude/skills/debug-web/scripts/web-preflight.sh
.claude/skills/debug-web/scripts/web-preflight.sh 8082
```
Expected: either `dev-web OK` + freshness lines, or `FAIL: nothing on :8082` with the two start commands. No crash.

- [ ] **Step 4: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/runbooks/web.md`
Expected: listed.

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/debug-web
git commit -m "feat(skills): debug-web preflight + observe routing over runbook

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: style-leak-audit skill

**Files:**
- Create: `.claude/skills/style-leak-audit/SKILL.md`
- Create: `.claude/skills/style-leak-audit/scripts/scan-style-leaks.mjs`

**Interfaces:**
- Produces: `scan-style-leaks.mjs <file.fs|dir>` printing `file:line CATEGORY message`; referenced by docs-sync (Task 8).

- [ ] **Step 1: Write SKILL.md**

```markdown
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
```

- [ ] **Step 2: Write scan-style-leaks.mjs**

```js
#!/usr/bin/env node
// Advisory scanner for style-leak patterns in .fs files. See docs fsharp/styling.md.
// usage: scan-style-leaks.mjs <file.fs|dir>
import { readFileSync, statSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

const CE_OPS = ['height', 'color', 'fontSize', 'top', 'left', 'bottom'];
const SKIP_DIRS = new Set(['node_modules', '.build', 'fable_modules', '_autogenerated_', '.git']);
const target = process.argv[2];
if (!target) { console.error('usage: scan-style-leaks.mjs <file.fs|dir>'); process.exit(2); }

function* fsFiles(p) {
  const st = statSync(p);
  if (st.isFile()) { if (p.endsWith('.fs')) yield p; return; }
  for (const e of readdirSync(p, { withFileTypes: true })) {
    if (SKIP_DIRS.has(e.name)) continue;
    yield* fsFiles(join(p, e.name));
  }
}

let count = 0;
const report = (file, line, cat, msg) => { count++; console.log(`${file}:${line} [${cat}] ${msg}`); };

for (const file of fsFiles(target)) {
  const lines = readFileSync(file, 'utf8').split('\n');
  lines.forEach((line, i) => {
    const n = i + 1;
    if (/make(View|Text)Styles/.test(line) && !/^\s*\/\//.test(line)) {
      const topLevel = /^let\s/.test(line) || /^\s{0,4}let\s+\w+\s*=\s*make(View|Text)Styles/.test(line) && !/^\s{5,}/.test(line);
      const nearMemoize = lines.slice(Math.max(0, i - 3), i + 1).some(l => /Memoize/.test(l));
      if (!topLevel && !nearMemoize)
        report(file, n, 'IN-RENDER?', 'make*Styles not at top level and not memoized; if this runs during render it is a leak');
    }
    const memo = line.match(/(View|Text)Styles\.Memoize\s*\(\s*fun\s*\(?([^)-]*)/);
    if (memo) {
      const params = memo[2];
      if (/:\s*(Theme|Colors)\b/.test(params) || /theme|colors/i.test(params) && !/Color\b/.test(params))
        report(file, n, 'RECORD-KEY?', 'Memoize possibly keyed on a Theme/Colors record; key on primitives (Color/int/bool) instead');
      for (const op of CE_OPS) {
        if (new RegExp(`\\b${op}\\b\\s*(:|,|\\))`).test(params))
          report(file, n, 'CE-COLLISION', `memoized lambda param '${op}' shadows a CE operation; rename (e.g. item${op[0].toUpperCase()}${op.slice(1)})`);
      }
    }
  });
}
console.log(count === 0 ? 'OK: no style-leak candidates' : `${count} candidate(s); judge each against fsharp/styling.md`);
```

- [ ] **Step 3: Test against known-bad and known-good input**

Write `/tmp/leak-test/Bad.fs`:
```fsharp
module Bad

let goodStatic = makeViewStyles { backgroundColor Color.White }

type LC with
    [<Component>]
    static member Foo () =
        let leaked = makeViewStyles { padding 4 }
        let memoBad = ViewStyles.Memoize(fun (height: int) -> makeViewStyles { height height })
        Rn.View(styles = [| leaked |])
```
Run: `node .claude/skills/style-leak-audit/scripts/scan-style-leaks.mjs /tmp/leak-test`
Expected: flags line with `let leaked` as IN-RENDER?, flags `memoBad` line as CE-COLLISION (`height`), does NOT flag `goodStatic`. Then run against a known-clean converted component, e.g. `LibClient/src/Components/Tabs` (or the file `Tabs.fs` wherever `rg -l "module private Styles" LibClient/src/Components | head -1` points): expect zero or only judgeable candidates. Tune the regexes if the good file false-positives on every line (acceptance: goodStatic-style top-level lets are not flagged).

- [ ] **Step 4: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/fsharp/styling.md`
Expected: listed.

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/style-leak-audit
git commit -m "feat(skills): style-leak-audit with advisory .fs scanner

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: a11y-check skill

**Files:**
- Create: `.claude/skills/a11y-check/SKILL.md`
- Create: `.claude/skills/a11y-check/scripts/scan-a11y.mjs`

**Interfaces:**
- Produces: `scan-a11y.mjs <file.fs|dir>`; referenced by docs-sync (Task 8).

- [ ] **Step 1: Write SKILL.md**

```markdown
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
```

- [ ] **Step 2: Write scan-a11y.mjs**

```js
#!/usr/bin/env node
// Advisory a11y scanner for .fs UI code. See docs accessibility/recipes.md.
// usage: scan-a11y.mjs <file.fs|dir>
import { readFileSync, statSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

const SKIP_DIRS = new Set(['node_modules', '.build', 'fable_modules', '_autogenerated_', '.git']);
const target = process.argv[2];
if (!target) { console.error('usage: scan-a11y.mjs <file.fs|dir>'); process.exit(2); }

function* fsFiles(p) {
  const st = statSync(p);
  if (st.isFile()) { if (p.endsWith('.fs')) yield p; return; }
  for (const e of readdirSync(p, { withFileTypes: true })) {
    if (SKIP_DIRS.has(e.name)) continue;
    yield* fsFiles(join(p, e.name));
  }
}

// Capture the argument window of a call: from the opening paren, until parens balance
// or 40 lines pass (heuristic for F# named-arg call layout).
function argWindow(lines, startIdx) {
  let depth = 0, out = [], started = false;
  for (let i = startIdx; i < Math.min(lines.length, startIdx + 40); i++) {
    for (const ch of lines[i]) {
      if (ch === '(') { depth++; started = true; }
      if (ch === ')') depth--;
    }
    out.push(lines[i]);
    if (started && depth <= 0) break;
  }
  return out.join('\n');
}

let count = 0;
const report = (file, line, msg) => { count++; console.log(`${file}:${line} ${msg}`); };

for (const file of fsFiles(target)) {
  const lines = readFileSync(file, 'utf8').split('\n');
  lines.forEach((line, i) => {
    const n = i + 1;
    if (/\bPressable\s*\(/.test(line) && !/^\s*\/\//.test(line)) {
      const w = argWindow(lines, i);
      if (!/\blabel\s*=/.test(w)) report(file, n, 'Pressable without label= (screen reader gets nothing)');
      if (!/\btestId\s*=/.test(w)) report(file, n, 'Pressable without testId= (automation cannot target it)');
    }
    if (/\b(TextInput|Input\.Text)\s*\(/.test(line) && !/^\s*\/\//.test(line)) {
      const w = argWindow(lines, i);
      if (!/\b(label|accessibilityLabel)\s*=/.test(w)) report(file, n, 'text input without an associated label');
    }
    if (/\bIcon\s*\(/.test(line) && /onPress/.test(argWindow(lines, i)) && !/\blabel\s*=/.test(argWindow(lines, i)))
      report(file, n, 'pressable icon without label (icon-only controls need names)');
  });
}
console.log(count === 0 ? 'OK: no a11y candidates' : `${count} candidate(s); judge against accessibility/recipes.md`);
```

- [ ] **Step 3: Test against known-bad and known-good input**

Write `/tmp/a11y-test/Bad.fs`:
```fsharp
module Bad

let good = LC.Pressable(label = "Save", testId = "save-button", onPress = ignore)

let bad =
    LC.Pressable(
        onPress = ignore,
        children = [| Rn.Text "tap" |]
    )
```
Run: `node .claude/skills/a11y-check/scripts/scan-a11y.mjs /tmp/a11y-test`
Expected: `bad` flagged twice (no label, no testId); `good` not flagged. Then run against one real converted component dir (`rg -l "LC.Pressable" LibClient/src/Components | head -1`); findings must be plausible (judge, tune regex on gross false positives).

- [ ] **Step 4: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/accessibility/recipes.md AppEggShellGallery/public-dev/docs/accessibility/index.md AppEggShellGallery/public-dev/docs/modernization/render-dsl-retirement.md`
Expected: all listed.

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/a11y-check
git commit -m "feat(skills): a11y-check with archetype bar + advisory scanner

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: docs-sync skill

**Files:**
- Create: `.claude/skills/docs-sync/SKILL.md`
- Create: `.claude/skills/docs-sync/scripts/check-doc-links.mjs`
- Create: `.claude/skills/docs-sync/scripts/scan-stale-framings.sh`

**Interfaces:**
- Produces: `check-doc-links.mjs [docsroot]` (exit 1 on broken links) and `scan-stale-framings.sh [docsroot]`; used by Task 12 and by future doc changes.

- [ ] **Step 1: Write SKILL.md**

```markdown
---
name: docs-sync
description: Keep the gallery docs site in sync after any code or doc change. Use before every commit that changes behavior, components, build, tooling, or docs. Routes the change type to the docs that must be updated, appends the engineering log, and validates links and status-page agreement.
user-invocable: true
argument-hint: "[change summary]"
---

# docs-sync

Docs root: `AppEggShellGallery/public-dev/docs/`. Golden rules (maintaining-docs.md): update docs
in the SAME commit as code; engineering log is append-only, newest at top; status pages must agree.

## Routing (change type -> docs to update)

- Backend lifecycle/grain/view -> architecture/backend-lifecycles.md, subject/ guide
- Frontend primitive / LibClient/src/Rn seam -> architecture/frontend.md,
  modernization/reactxp-to-rnw.md, status dashboards
- LibClient/LibRouter component -> its gallery Content page (pure F#, rule 10),
  fsharp/component.md if the pattern changed, accessibility/recipes.md if a11y changed
- Styling/theming -> fsharp/styling.md, fsharp/themeing.md
- eggshell CLI / scaffolding -> tools/cli.md, modernization/scaffolding.md
- Build/speed -> runbooks/build-rebuild.md, modernization/build-performance.md
- Dev-loop gotcha -> runbooks/<platform>.md + runbooks/troubleshooting.md +
  knowledge-base/engineering-log.md
- Goal/phase status -> modernization/index.md, goals-and-roadmap.md, phased-plan.md
- Doc page added/renamed/moved/deleted -> llms.txt

## Engineering log entry template (prepend under the title)

    ## YYYY-MM-DD — <short title>
    **Context:** <what was being done>
    **Learning/gotcha:** <what was wrong or missing, and the fix>
    **Distilled:** <added to runbooks/troubleshooting.md? which section? or "not durable">

## Validation gates

1. `node .claude/skills/docs-sync/scripts/check-doc-links.mjs` (exit 1 = broken links, fix them)
2. `.claude/skills/docs-sync/scripts/scan-stale-framings.sh` (judge hits: history pages may
   legitimately mention ReactXP/Fable 4)
3. Status agreement: modernization/index.md vs goals-and-roadmap.md vs phased-plan.md vs reality
   (global.json SDK, package.json versions)
4. Component changes: also run style-leak-audit + a11y-check skills.
5. Docs build: gallery compiles and the page renders (debug-web skill).

## Doc refs

- maintaining-docs.md (golden rules + full update matrix)
- knowledge-base/engineering-log.md (format precedent)

(All under AppEggShellGallery/public-dev/docs/.)
```

- [ ] **Step 2: Write check-doc-links.mjs**

```js
#!/usr/bin/env node
// Verify all relative .md links in the docs tree resolve, and every page is in llms.txt.
// usage: check-doc-links.mjs [docsroot]   (exit 1 on broken links)
import { readFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, dirname, resolve, relative } from 'node:path';

const ROOT = process.argv[2] ?? 'AppEggShellGallery/public-dev/docs';
if (!existsSync(ROOT)) { console.error(`no such docs root: ${ROOT}`); process.exit(2); }

function* mdFiles(p) {
  for (const e of readdirSync(p, { withFileTypes: true })) {
    const full = join(p, e.name);
    if (e.isDirectory()) yield* mdFiles(full);
    else if (e.name.endsWith('.md')) yield full;
  }
}

const broken = [];
const pages = [];
for (const file of mdFiles(ROOT)) {
  pages.push(relative(ROOT, file));
  const text = readFileSync(file, 'utf8');
  for (const m of text.matchAll(/\]\(([^)\s]+?\.md)(#[^)]*)?\)/g)) {
    const href = m[1];
    if (/^https?:/.test(href)) continue;
    const targetPath = resolve(dirname(file), href);
    if (!existsSync(targetPath)) broken.push(`${file}: ${href}`);
  }
}
if (broken.length) {
  console.log(`BROKEN LINKS (${broken.length}):`);
  broken.forEach(b => console.log('  ' + b));
}
const llmsPath = join(ROOT, 'llms.txt');
if (existsSync(llmsPath)) {
  const llms = readFileSync(llmsPath, 'utf8');
  const missing = pages.filter(p => !llms.includes(p) && p !== 'llms.txt');
  if (missing.length) {
    console.log(`WARN: ${missing.length} page(s) not listed in llms.txt:`);
    missing.forEach(p => console.log('  ' + p));
  }
}
console.log(broken.length ? 'RESULT: FAIL' : 'RESULT: PASS');
process.exit(broken.length ? 1 : 0);
```

- [ ] **Step 3: Write scan-stale-framings.sh**

```zsh
#!/bin/zsh
# Flag present-tense mentions of retired tech in the docs. Advisory: history/log pages may
# legitimately reference them; judge each hit.
# usage: scan-stale-framings.sh [docsroot]
set -u
DOCS="${1:-AppEggShellGallery/public-dev/docs}"
rg -n \
  --glob '!knowledge-base/engineering-log.md' \
  --glob '!modernization/reactxp-to-rnw.md' \
  -e 'ReactXP' -e '\bRX\.' -e 'Fable 4' -e '\.NET 7' -e 'react-native 0\.7' -e 'RNGH 2\b' \
  "$DOCS" && echo "(judge each: retired-tech mention may be historical)" || echo "OK: no stale framings"
```

- [ ] **Step 4: Make executable and test**

Run:
```bash
chmod +x .claude/skills/docs-sync/scripts/scan-stale-framings.sh
node .claude/skills/docs-sync/scripts/check-doc-links.mjs
.claude/skills/docs-sync/scripts/scan-stale-framings.sh
```
Expected: link checker walks the real docs tree and prints PASS, or lists genuinely broken links (report them, do not silently fix unrelated docs in this task); stale-framings prints hits or `OK`. Both exit without crashing.

- [ ] **Step 5: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/maintaining-docs.md`
Expected: listed.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/docs-sync
git commit -m "feat(skills): docs-sync with link checker + stale-framing scanner

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 9: gallery-page-add skill

**Files:**
- Create: `.claude/skills/gallery-page-add/SKILL.md`
- Create: `.claude/skills/gallery-page-add/scripts/scaffold-gallery-page.mjs`

**Interfaces:**
- Consumes: registration file shapes: `AppEggShellGallery/src/App.fsproj` (Compile Include line), `AppEggShellGallery/src/Navigation.fs` (DU case), `Components.fs` match case, `SidebarContent.fs` nav entry.
- Produces: `scaffold-gallery-page.mjs <PageName>` which writes `AppEggShellGallery/src/Components/Content/<PageName>/<PageName>.fs` and prints the four registration edits.

- [ ] **Step 1: Read the live example to lock the template.** Read `AppEggShellGallery/src/Components/Content/HorizontalPanArea/HorizontalPanArea.fs` fully, plus the exact registration lines:

Run:
```bash
rg -n "HorizontalPanArea" AppEggShellGallery/src/App.fsproj AppEggShellGallery/src/Navigation.fs AppEggShellGallery/src/SidebarContent.fs $(rg -l "HorizontalPanArea.*->.*Ui.Content" AppEggShellGallery/src)
```
Record: namespace/module header shape, the `[<Component>]` member signature, how the page wraps content (scroll container, headings), and the four registration line shapes with exact surrounding context.

- [ ] **Step 2: Write SKILL.md**

```markdown
---
name: gallery-page-add
description: Add a new App Gallery content page in pure F# (never render DSL) with all four registrations wired. Use whenever a LibClient/LibRouter component is added or changed and needs a gallery page (CLAUDE.md rule 10), or when asked to add a gallery demo/docs page.
user-invocable: true
argument-hint: "<PageName>"
---

# gallery-page-add

Five files per page. `scripts/scaffold-gallery-page.mjs <PageName>` writes the page skeleton and
prints the four registration edits (registrations stay manual: those files are load-bearing and
order-sensitive).

1. `AppEggShellGallery/src/Components/Content/<PageName>/<PageName>.fs` (generated skeleton;
   fill in the demo content; model on Content/HorizontalPanArea/HorizontalPanArea.fs)
2. `AppEggShellGallery/src/App.fsproj`: add
   `<Compile Include="Components/Content/<PageName>/<PageName>.fs" />` NEXT TO the other Content
   pages (compile order matters; keep alphabetical/grouped placement consistent with neighbors)
3. `AppEggShellGallery/src/Navigation.fs`: add `| <PageName>` to the component-item DU
4. `Components.fs` (content router): add `| <PageName> -> Ui.Content.<PageName>()` match case
5. `SidebarContent.fs`: add the sidebar nav entry following the neighboring items' exact shape

## Rules

- Pure F# `[<Component>]` style only; no render DSL, no `.typext.fs`.
- Page must meet the a11y bar (a11y-check skill) and style rules (style-leak-audit skill).
- Static sibling element arrays go through `castAsElementAckingKeysWarning` (React keys).

## Validate

- `cd AppEggShellGallery && ../eggshell dev-web`, open http://localhost:8082, click the new
  sidebar entry, page renders. Then fable-rebuild-verify proof gate if anything looks cached.

## Doc refs

- maintaining-docs.md (docs routing when the page documents a component)
- modernization/render-dsl-retirement.md (component style rules)

(All under AppEggShellGallery/public-dev/docs/.)
```

- [ ] **Step 3: Write scaffold-gallery-page.mjs.** Embed the skeleton derived in Step 1 as a template literal with `__NAME__` placeholders. Structure (fill `TEMPLATE` with the real shape observed in Step 1; the constant below shows the required mechanics):

```js
#!/usr/bin/env node
// Scaffold a gallery Content page (pure F#) + print the 4 registration edits.
// usage: scaffold-gallery-page.mjs <PageName>
import { mkdirSync, writeFileSync, existsSync } from 'node:fs';

const name = process.argv[2];
if (!name || !/^[A-Z][A-Za-z0-9]+$/.test(name)) {
  console.error('usage: scaffold-gallery-page.mjs <PageName>  (PascalCase)');
  process.exit(2);
}
const dir = `AppEggShellGallery/src/Components/Content/${name}`;
const file = `${dir}/${name}.fs`;
if (existsSync(file)) { console.error(`exists: ${file}`); process.exit(2); }

// TEMPLATE: copied from Components/Content/HorizontalPanArea/HorizontalPanArea.fs at authoring
// time, demo body reduced to a heading + placeholder text. Update if page conventions change.
const TEMPLATE = `<REPLACE AT IMPLEMENTATION: real namespace/module/[<Component>] skeleton from Step 1, with __NAME__ placeholders>`;

mkdirSync(dir, { recursive: true });
writeFileSync(file, TEMPLATE.replaceAll('__NAME__', name));
console.log(`wrote ${file}`);
console.log(`
Now wire 4 registrations (match neighboring lines exactly):
1. AppEggShellGallery/src/App.fsproj:
   <Compile Include="Components/Content/${name}/${name}.fs" />  (beside other Content pages)
2. AppEggShellGallery/src/Navigation.fs:  | ${name}   (component-item DU)
3. Components.fs content router:          | ${name} -> Ui.Content.${name}()
4. SidebarContent.fs:                     sidebar entry, copy a neighbor's shape
Then: cd AppEggShellGallery && ../eggshell dev-web  ->  check page renders on :8082
`);
```

- [ ] **Step 4: Test end-to-end, then revert**

Run:
```bash
chmod +x .claude/skills/gallery-page-add/scripts/scaffold-gallery-page.mjs
node .claude/skills/gallery-page-add/scripts/scaffold-gallery-page.mjs ScaffoldSmokeTest
```
Do the four registration edits it prints, then from `AppEggShellGallery`: `../eggshell dev-web` (or `../eggshell build-lib` for a faster type-check pass) and confirm zero `error FS` and the page compiles. Then revert the smoke test completely:
```bash
git checkout -- AppEggShellGallery/src/App.fsproj AppEggShellGallery/src/Navigation.fs
git checkout -- $(git diff --name-only | grep -E "Components.fs|SidebarContent.fs")
rm -rf AppEggShellGallery/src/Components/Content/ScaffoldSmokeTest
```
Expected: template compiles as generated (this is the acceptance test for the TEMPLATE constant; iterate the template until it does).

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/gallery-page-add
git commit -m "feat(skills): gallery-page-add scaffolder + registration checklist

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 10: release-build skill

**Files:**
- Create: `.claude/skills/release-build/SKILL.md`
- Create: `.claude/skills/release-build/scripts/release-build.sh`

**Interfaces:**
- Produces: `release-build.sh {android|ios|web} <appdir> [--install]`.

- [ ] **Step 1: Determine the web release path.** Run `./eggshell 2>&1 | head -40` (or read the `eggshell` script source at repo root) and note the web production build command (candidates: `build-web`, `release-web`, a webpack `--mode production` path). If none exists, the web case in the script prints `NOT SUPPORTED: no eggshell web release command; use dev-web + dotnet build -c "Web Release" for type-check` and exits 0. Record the finding for SKILL.md.

- [ ] **Step 2: Write SKILL.md**

```markdown
---
name: release-build
description: Produce and smoke-check a RELEASE build for android, ios, or web. Use when asked for a release/production build, before shipping, or when a bug reproduces only outside the dev loop. Release surfaces failures debug masks (bundler resolution, missing deps, polyfills), so verify features against release when touching native deps.
user-invocable: true
argument-hint: "{android|ios|web} <appdir>"
---

# release-build

`scripts/release-build.sh {android|ios|web} <appdir> [--install]`

## Android

`cd <appdir>/android && ./gradlew assembleRelease` with the repo's debug-keystore params (see
script). Artifact: `<appdir>/android/app/build/outputs/apk/release/app-release.apk`. The release
APK embeds the JS bundle from `.build/native/commonjs` and runs WITHOUT Metro; `--install` does
`adb install -r` + launches. Ensure the Fable native build is fresh first (fable-rebuild-verify).

## Release-only failure modes (engineering log, session 10). Check these when release breaks and debug works:

1. LibClient-only deps not resolvable in release bundle -> map them in the app's
   `metro.config.js` `extraNodeModules` (e.g. @react-native-picker/picker, async-storage).
2. Reanimated/worklets live only in LibClient/node_modules -> add as direct app deps
   (+ @babel/core + babel.config.js).
3. Local images crash RCTImageView ("Value for uri cannot be cast from Double to String") ->
   numeric require() assets must pass as bare RN source, not {uri}.
4. `ReferenceError: Property 'crypto' doesn't exist` -> add react-native-get-random-values,
   import it FIRST in index.js.

## iOS

`xcodebuild -workspace <App>.xcworkspace -scheme <scheme> -configuration Release
-sdk iphonesimulator build` (fast pass; the script auto-detects the scheme via
`xcodebuild -list`). Pods must be installed. Smoke check: install + launch on the booted
simulator with Metro STOPPED; the app must boot from the embedded bundle.

## Web

<FILL AT IMPLEMENTATION from Step 1: the found eggshell web release command, or the NOT SUPPORTED
fallback.> Smoke check: serve the output statically, page boots, no dev-server references, no
console errors.

## Doc refs

- runbooks/android.md (release APK section)
- runbooks/ios.md
- knowledge-base/engineering-log.md (session 10, release-gap findings)

(All under AppEggShellGallery/public-dev/docs/.)
```

- [ ] **Step 3: Write release-build.sh**

```zsh
#!/bin/zsh
# Release builds per platform. See .claude/skills/release-build/SKILL.md.
# usage: release-build.sh {android|ios|web} <appdir> [--install]
set -u
PLAT="${1:-}"; APP="${2:-}"; INSTALL="${3:-}"
[[ -n "$PLAT" && -d "$APP" ]] || { echo "usage: release-build.sh {android|ios|web} <appdir> [--install]"; exit 2; }
case "$PLAT" in
  android)
    ( cd "$APP/android" && ./gradlew assembleRelease \
        -PMYAPP_RELEASE_STORE_FILE=debug.keystore \
        -PMYAPP_RELEASE_STORE_PASSWORD=android \
        -PMYAPP_RELEASE_KEY_ALIAS=androiddebugkey \
        -PMYAPP_RELEASE_KEY_PASSWORD=android ) || { echo "RESULT: FAIL (gradle)"; exit 0; }
    APK="$APP/android/app/build/outputs/apk/release/app-release.apk"
    [[ -f "$APK" ]] && echo "artifact: $APK ($(du -h "$APK" | cut -f1))" || { echo "RESULT: FAIL (no apk)"; exit 0; }
    if [[ "$INSTALL" == "--install" ]]; then
      adb install -r "$APK" && echo "installed; launch the app WITHOUT Metro to smoke-test"
    fi
    echo "RESULT: PASS" ;;
  ios)
    WS=$(ls "$APP"/ios/*.xcworkspace 2>/dev/null | head -1)
    [[ -n "$WS" ]] || { echo "RESULT: FAIL (no .xcworkspace; pods installed?)"; exit 0; }
    SCHEME=$(xcodebuild -workspace "$WS" -list 2>/dev/null | awk '/Schemes:/{f=1;next} f&&NF{print $1;exit}')
    echo "workspace: $WS scheme: $SCHEME"
    xcodebuild -workspace "$WS" -scheme "$SCHEME" -configuration Release -sdk iphonesimulator build \
      && echo "RESULT: PASS (smoke: install+launch on booted sim with Metro stopped)" \
      || echo "RESULT: FAIL (xcodebuild)" ;;
  web)
    echo "<REPLACE AT IMPLEMENTATION: found eggshell web release command, or NOT SUPPORTED message>" ;;
  *) echo "usage: release-build.sh {android|ios|web} <appdir> [--install]"; exit 2 ;;
esac
```

- [ ] **Step 4: Make executable and test cheaply**

Run:
```bash
chmod +x .claude/skills/release-build/scripts/release-build.sh
.claude/skills/release-build/scripts/release-build.sh
.claude/skills/release-build/scripts/release-build.sh web AppEggShellGallery
```
Expected: usage + exit 2; then the web branch output. Do NOT run a full Android/iOS release build in this task (minutes-long); the command lines are verbatim from runbooks/android.md:139-142 and standard xcodebuild. If the user asks for a full validation later, run `release-build.sh android SuiteTodo/AppTodo`.

- [ ] **Step 5: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/knowledge-base/engineering-log.md`
Expected: listed.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/release-build
git commit -m "feat(skills): release-build for android/ios/web with release-gap checklist

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 11: verify-feature skill

**Files:**
- Create: `.claude/skills/verify-feature/SKILL.md`
- Create: `.claude/skills/verify-feature/scripts/capture-session.sh`

**Interfaces:**
- Consumes: `rebuild-verify.sh` (Task 2), `android-preflight.sh`/`android-observe.sh` (Task 3), `ios-preflight.sh`/`ios-observe.sh` (Task 4), `web-preflight.sh` (Task 5).
- Produces: `capture-session.sh {android|ios} [--url <deeplink>] [--app <id>] [--duration N] [--out dir]`.

- [ ] **Step 1: Write SKILL.md**

```markdown
---
name: verify-feature
description: Prove a feature or page actually works end to end on android, ios, and/or web, with fresh-bundle proof, driver-based navigation, screenshots, and bucketed log capture. Use whenever asked to verify, test, or demo a feature/page, and before claiming any UI change is done. Verification means evidence, not "it compiled".
user-invocable: true
argument-hint: "{android|ios|web} [route or deeplink]"
---

# verify-feature

Verify matrix: portrait + landscape, on every platform the change touches. "Compiles" is not
verified; a screenshot of the working feature is.

## Flow

1. **Fresh bundle proof:** invoke the fable-rebuild-verify skill for the touched project(s).
2. **Preflight the platform:** debug-android / debug-ios / debug-web skill preflight scripts.
3. **Route to the target:**
   - Web: open `http://localhost:<port>/<route>` directly (pathname routing).
   - Android: `adb shell am start -n <package>/.MainActivity` then navigate, or deep-link URL if
     the app registers a scheme.
   - iOS: `ios-observe.sh launch <bundleid>` / `ios-observe.sh openurl <url>`.
4. **Interact deterministically (Tier 2 first):** web via Playwright toolkit
   (`npm run audit:web`, `npm run observe -- snapshot -p web`); Android via Appium driver
   (`AppEggShellGallery/audit-gallery-android-driver.mjs`, Appium :4723,
   `npm run observe -- doctor` to check the chain). Raw coordinate taps
   (`android-observe.sh tap`) only as last resort.
5. **Capture:** `scripts/capture-session.sh {android|ios} [--url <deeplink>] [--duration N]`
   records logs for the whole window + before/after screenshots. Web: observe snapshot captures
   screenshot + console + health.
6. **Analyse:** the capture summary buckets FATAL / error / warn / ReactNativeJS. Zero fatals and
   the expected UI in screenshots = PASS. Anything else: report evidence, do not claim done.
7. Repeat for landscape (`android-observe.sh rotate landscape`, or observe --orientation).

## Reporting

State PASS/FAIL per platform x orientation with screenshot paths and the log summary. Failures go
to the engineering log via the docs-sync skill.

## Doc refs

- runbooks/audit-toolkit.md (two-tier observation model)
- runbooks/android.md, runbooks/ios.md, runbooks/web.md
- runbooks/dev-loop.md

(All under AppEggShellGallery/public-dev/docs/.)
```

- [ ] **Step 2: Write capture-session.sh**

```zsh
#!/bin/zsh
# Capture a timed device session: logs for the whole window + before/after screenshots.
# usage: capture-session.sh {android|ios} [--url <deeplink>] [--app <package-or-bundleid>] [--duration N] [--out <dir>]
set -u
PLAT="${1:-}"; shift 2>/dev/null || true
[[ "$PLAT" == "android" || "$PLAT" == "ios" ]] || { echo "usage: capture-session.sh {android|ios} [--url u] [--app id] [--duration N] [--out dir]"; exit 2; }
URL=""; APPID=""; DUR=20; OUT="${TMPDIR:-/tmp}/capture-$(date +%H%M%S)"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --url) URL="$2"; shift 2 ;;
    --app) APPID="$2"; shift 2 ;;
    --duration) DUR="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    *) shift ;;
  esac
done
mkdir -p "$OUT"
SKILLS="$(cd "$(dirname "$0")/../.." && pwd)"

if [[ "$PLAT" == "android" ]]; then
  adb logcat -c
  adb logcat > "$OUT/logcat.txt" & LOGPID=$!
  "$SKILLS/debug-android/scripts/android-observe.sh" screenshot "$OUT/before.png" > /dev/null
  [[ -n "$URL" ]] && adb shell am start -a android.intent.action.VIEW -d "$URL"
  [[ -n "$APPID" && -z "$URL" ]] && adb shell am start -n "$APPID/.MainActivity"
  echo "capturing ${DUR}s (interact with the device now)..."
  sleep "$DUR"
  "$SKILLS/debug-android/scripts/android-observe.sh" screenshot "$OUT/after.png" > /dev/null
  kill $LOGPID 2>/dev/null
  LOG="$OUT/logcat.txt"
else
  "$SKILLS/debug-ios/scripts/ios-observe.sh" screenshot "$OUT/before.png" > /dev/null
  [[ -n "$URL" ]] && xcrun simctl openurl booted "$URL"
  [[ -n "$APPID" && -z "$URL" ]] && xcrun simctl launch booted "$APPID"
  echo "capturing ${DUR}s (interact with the simulator now)..."
  sleep "$DUR"
  "$SKILLS/debug-ios/scripts/ios-observe.sh" screenshot "$OUT/after.png" > /dev/null
  xcrun simctl spawn booted log show --last "$((DUR / 60 + 1))m" --style compact > "$OUT/log.txt" 2>/dev/null
  LOG="$OUT/log.txt"
fi

echo "--- summary ($OUT) ---"
echo "screenshots: $OUT/before.png $OUT/after.png"
for bucket in "FATAL EXCEPTION" "Uncaught" "ReactNativeJS" "error"; do
  C=$(grep -ic "$bucket" "$LOG" 2>/dev/null || echo 0)
  echo "$bucket: $C"
done
echo "full log: $LOG (grep the buckets above for detail)"
```

- [ ] **Step 3: Make executable and test**

Run:
```bash
chmod +x .claude/skills/verify-feature/scripts/capture-session.sh
.claude/skills/verify-feature/scripts/capture-session.sh
.claude/skills/verify-feature/scripts/capture-session.sh android --duration 3
```
Expected: usage + exit 2; then either a real 3s capture (device attached: two PNGs + bucket counts) or clean adb failures with the summary still printing (no device: acceptable, note it).

- [ ] **Step 4: Verify doc refs**

Run: `ls AppEggShellGallery/public-dev/docs/runbooks/audit-toolkit.md`
Expected: listed.

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/verify-feature
git commit -m "feat(skills): verify-feature e2e capture + evidence-based pass/fail loop

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 12: docs backfill (docs-sync applied to this change)

**Files:**
- Modify: `AppEggShellGallery/public-dev/docs/knowledge-base/engineering-log.md` (prepend entry)
- Modify: `AppEggShellGallery/public-dev/docs/runbooks/index.md` (cross-ref skills)
- Modify: `AppEggShellGallery/public-dev/docs/runbooks/troubleshooting.md` (stray .fs.js symptom)

**Interfaces:**
- Consumes: everything from Tasks 1-11.

- [ ] **Step 1: Prepend engineering log entry** (below the doc title, above the previous newest entry), following the file's existing entry format (check the top entry's heading shape first and match it):

```markdown
## 2026-07-11 — Project skills created under .claude/skills/

**Context:** Recurring workflows from the RNW modernization (rebuild verification, device debug
loops, style/a11y audits, docs upkeep, gallery pages, release builds, e2e verification) were
packaged as Claude skills with helper scripts.
**Learning/gotcha:** Models sometimes ran `dotnet fable` directly, emitting `.fs.js` beside
sources instead of into `.build/<platform>/`. Guard added to CLAUDE.md;
`clean-stray-fable-output.sh` (fable-rebuild-verify skill) detects/removes strays.
**Distilled:** troubleshooting.md gained the stray-.fs.js symptom; runbooks/index.md now points
to the skills as the executable front door to the runbooks.
```

- [ ] **Step 2: Add to runbooks/index.md**, near the top where the runbook list lives (match surrounding formatting):

```markdown
> **Skills front door:** executable wrappers for these runbooks live in `.claude/skills/`
> (fable-rebuild-verify, debug-android, debug-ios, debug-web, verify-feature, release-build).
> Invoke the skill; it follows the runbook and reports evidence.
```

- [ ] **Step 3: Add symptom to troubleshooting.md** (in or near the build-cache section, matching its symptom -> fix format):

```markdown
- **Symptom:** `.fs.js` files appear beside `.fs` sources; edits behave unpredictably.
  **Cause:** raw `dotnet fable` run (bypasses eggshell; output belongs in `.build/<platform>/`).
  **Fix:** `.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh`, then rebuild
  via `./eggshell build-lib` / `eggshell dev-*`. Never invoke `dotnet fable` directly.
```

- [ ] **Step 4: Validate with the new tooling**

Run:
```bash
node .claude/skills/docs-sync/scripts/check-doc-links.mjs
.claude/skills/docs-sync/scripts/scan-stale-framings.sh
```
Expected: link checker RESULT: PASS (fix any link this task introduced); stale-framings unchanged from Task 8 baseline.

- [ ] **Step 5: Commit**

```bash
git add AppEggShellGallery/public-dev/docs
git commit -m "docs: engineering log + runbook cross-refs for new project skills

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 13: final validation sweep

**Files:** none created; verification only.

- [ ] **Step 1: All scripts executable and usage-clean**

Run:
```bash
ls -l .claude/skills/*/scripts/* | grep -v rwxr && echo "PERMISSION GAP" || echo "all executable"
for s in .claude/skills/*/scripts/*.sh; do zsh -n "$s" && echo "OK $s"; done
for s in .claude/skills/*/scripts/*.mjs; do node --check "$s" && echo "OK $s"; done
```
Expected: `all executable`; every script syntax-checks OK.

- [ ] **Step 2: Frontmatter sanity**

Run: `head -8 .claude/skills/*/SKILL.md | grep -E "^(name|description|user-invocable):" | sort | uniq -c | head -40`
Expected: every SKILL.md has `name`, `description`, `user-invocable: true`; names match their directory names.

- [ ] **Step 3: No em-dash in new prose**

Run: `rg -l "—" .claude/skills docs/superpowers CLAUDE.md`
Expected: no matches in the new skill files or the new CLAUDE.md section (pre-existing CLAUDE.md text is out of scope; only fix lines this plan added).

- [ ] **Step 4: Doc refs all resolve**

Run:
```bash
rg -o "runbooks/[a-z-]+\.md|fsharp/[a-z-]+\.md|accessibility/[a-z.]+\.md|modernization/[a-z-]+\.md|maintaining-docs\.md|knowledge-base/[a-z-]+\.md" .claude/skills/*/SKILL.md | cut -d: -f2 | sort -u | while read f; do ls "AppEggShellGallery/public-dev/docs/$f" > /dev/null || echo "MISSING $f"; done
```
Expected: no `MISSING` lines.

- [ ] **Step 5: Commit any fixes**

```bash
git add -A .claude/skills CLAUDE.md
git commit -m "chore(skills): final validation sweep fixes" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>" --allow-empty
```

---

## Self-Review Notes

- Spec coverage: 10 skills (Tasks 2-11) + CLAUDE.md burn-in (Task 1) + docs backfill (Task 12) + validation (Task 13). Spec's "cross-wiring" is realized inside SKILL.md bodies (debug-* and verify-feature reference fable-rebuild-verify; docs-sync references style-leak-audit + a11y-check).
- Two deliberate implementation-time lookups exist (gallery page TEMPLATE in Task 9 Step 1/3; web release command in Task 10 Step 1). Both have concrete discovery steps and acceptance tests; they are data reads, not design gaps.
- TDD adaptation: scripts are validated with known-bad/known-good fixtures (Tasks 6, 7) or usage/smoke tests (Tasks 2-5, 10, 11); SKILL.md files are validated by doc-ref existence checks and the Task 13 sweep.
