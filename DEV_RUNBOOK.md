# EggShell Frontend Dev Runbook (Android / iOS / Web)

A verbose, follow-it-literally runbook for the **inner development loop**: edit → targeted rebuild →
reload → **observe** (screenshot / read errors / tap / rotate) → fix, plus how to get out of the gotchas
that waste the most time. Written so a human **or an LLM agent** can drive a device/simulator/browser
and debug without prior context.

- Canonical **setup** (install, native project gen, day-to-day commands) lives in
  `SuiteTodo/AppTodo/README.md`. This runbook does **not** repeat setup; it covers the debug/observe
  loop and the failure modes.
- Deep gotchas and their fixes: `LEARNINGS.md` (referenced inline as "LEARNINGS <date>").
- Examples use AppTodo (`com.eggshell.apptodo`, AVD `Medium_Phone_API_35`); substitute your app's
  package / AVD / simulator. Paths shown are macOS defaults
  (`~/Library/Android/sdk/...`, `~/.android/avd/...`).

---

## 0. Two tiers of observation — and the Appium/Playwright verdict

There are two ways to drive and inspect a running app. **Use the lightest one that answers your
question.**

### Tier 1 — Raw CLI (adb / simctl / browser). The tight inner loop. *No setup.*
This is what was used for nearly all of this session's debug-fix-screenshot work. Zero servers, instant,
perfect for "did my change render? is there a runtime error?". You drive the device with `adb`/`xcrun`,
grab a PNG, read logs with `logcat`/`simctl log`, tap with pixel coords, rotate with a settings write.

- **Pros:** zero startup, immediate, sees runtime red-box errors, trivial to script ad hoc.
- **Cons:** taps are **pixel coordinates** (brittle — they shift when layout changes), no element lookup
  by `testId`, no structured layout data.

### Tier 2 — The `audit/` toolkit (Appium for native, Playwright for web). Structured + repeatable.
Already built in `SuiteTodo/AppTodo/audit/` (`npm run observe -- …`). Drives by **`testId` /
accessibility id** (robust), emits **structured JSON** (layout metrics, DOM/hierarchy summaries, health,
classified logs) into `audit/out/<timestamp>/`, supports before/after **layout diffs**, orientation, and
a `doctor` preflight. Designed to be **LLM- and CI-friendly**.

- **Pros:** deterministic element interaction (not pixels), machine-readable state for agents, layout
  regression diffs, cross-platform one command, CI gate.
- **Cons:** needs the Appium server + drivers (`npm run appium:setup`), slower to start.

### Verdict (answering "were Appium/Playwright needed?")
**For the interactive debug/fix loop in this session: no — raw adb was sufficient and faster.** You do
**not** need Appium/Playwright to: reload an app, screenshot it, read a runtime error, or eyeball a fix.

**But keep them, and reach for them when:** (1) you need to tap a specific control reliably while the
layout is changing (tap by `testId`, not pixels); (2) you want **structured layout metrics** or a
**before/after diff** (e.g., "did the card width regress?"); (3) **CI / regression** gates; (4) you are
an **LLM agent** that benefits from JSON state instead of staring at a PNG. Rule of thumb: **debug with
Tier 1, verify/gate with Tier 2.**

---

## 1. Mental model: the moving parts

| Part | Command | Role | Default port |
|---|---|---|---|
| Fable watch (`dev-native`) | `../../eggshell dev-native` | F# → `.build/native/commonjs` on save (native) | — |
| Metro | `npx react-native start --port 8081` | serves the JS bundle to device/sim | 8081 |
| Web dev-server (`dev-web`) | `../../eggshell dev-web` | Fable + webpack for the browser | 9080 |
| Appium (Tier 2 native) | `npm run appium` | WebDriver server for native automation | 4723 |
| Device | emulator / simulator / browser | runs the app | — |

Key facts that cause confusion:
- **One** `dev-native` + **one** Metro serve **both** Android and iOS at once.
- Android emulator reaches Metro via `10.0.2.2:8081` and needs `adb reverse tcp:8081 tcp:8081` once per
  boot; iOS simulator uses `localhost:8081` (no reverse needed).
- A **green Fable/Metro build does not mean your change is on screen** — the app must be reloaded, and
  Metro may serve a cached bundle. Always confirm on-device (Tier 1) or via `doctor`/`snapshot` (Tier 2).
- **Metro bundles `node_modules` from each package's build entry.** For `@chaldal/reactxp` that is
  `dist/native-common/*.js`, **not** `src/*.tsx` — patching the wrong one is invisible (LEARNINGS
  2026-06-29). Verify a patch actually reached the bundle (see §7.4).

---

## 2. The universal inner loop

1. **Edit** F# (app or framework) / styles / a vendored JS patch.
2. **Targeted rebuild** — let the running watch recompile just the changed file(s) (§7).
3. **Confirm the rebuild is real** — watch printed `Started Fable compilation…` then compiled with
   no `error FS`; the emitted `.js` is newer than your `.fs` (§7.2). Beware "Skipped compilation …
   up-to-date" false-greens (LEARNINGS).
4. **Reload the app** on the target (relaunch or Metro reload).
5. **Observe** — screenshot + read logs (Tier 1), or `npm run observe -- snapshot` (Tier 2).
6. **If a runtime error** — read it from the red box / `logcat` / browser console; the component stack
   tells you the owning component (this is how the "unique key" warning was traced, §9).

Fast-changing styles often only need an app reload, not a full rebuild — but F# changes always need the
Fable recompile first.

---

## 3. Android runbook (verbose)

### 3.1 Boot the emulator (and fix sideways/landscape permanently)
```bash
~/Library/Android/sdk/emulator/emulator -list-avds
~/Library/Android/sdk/emulator/emulator -avd Medium_Phone_API_35 -no-snapshot-load &   # cold boot
~/Library/Android/sdk/platform-tools/adb wait-for-device
```
**Gotcha — emulator launches in landscape / sideways** (LEARNINGS 2026-06-28): the AVD config had a
reversed skin and landscape default. Permanent fix in `~/.android/avd/<AVD>.avd/config.ini`:
```
hw.initialOrientation = portrait
skin.name = 1080x2400          # WIDTHxHEIGHT — a reversed "2400x1080" forces a landscape window
```
Then delete the cached state so quickboot doesn't restore the old landscape:
```bash
rm -f  ~/.android/avd/<AVD>.avd/hardware-qemu.ini ~/.android/avd/<AVD>.avd/hardware-qemu.ini.lock
rm -rf ~/.android/avd/<AVD>.avd/snapshots/default_boot
```
Relaunch with `-no-snapshot-load`. Verify:
```bash
adb shell dumpsys window | grep -E "mCurrentRotation|mDisplayRotation"   # ROTATION_0 = portrait
```

### 3.2 Start the toolchain (from the app dir)
```bash
export DOTNET_ROOT="$HOME/.dotnet"          # always, before dotnet/fable/eggshell (LEARNINGS)
cd SuiteTodo/AppTodo
../../eggshell dev-native &                  # Fable watch (terminal 1)
npx react-native start --port 8081 &         # Metro (terminal 2)
adb reverse tcp:8081 tcp:8081                # once per emulator boot
```

### 3.3 Launch / reload the app (no run-android rebuild needed if installed)
```bash
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB shell am force-stop com.eggshell.apptodo
$ADB shell monkey -p com.eggshell.apptodo -c android.intent.category.LAUNCHER 1
# first JS bundle build after a Metro (re)start is slow — wait ~25-30s before screenshotting
```
A full native (re)install is only needed when native code/deps changed:
`adb reverse … ; npx react-native run-android --no-packager`.

### 3.4 Screenshot
```bash
$ADB shell screencap -p /sdcard/s.png && $ADB pull /sdcard/s.png /tmp/s.png
```
**Zoom into a pixel detail** (caught a square-cornered toggle invisible at full scale, LEARNINGS):
```bash
# sips -c <cropH> <cropW> --cropOffset <y> <x> in.png --out out.png   (1080x2400 device)
sips -c 144 453 --cropOffset 108 626 /tmp/s.png --out /tmp/toggle.png
```

### 3.5 Tap / type / rotate
```bash
$ADB shell input tap <x> <y>            # PIXEL coords — recompute after layout changes (brittle!)
$ADB shell input text "hello"
# rotate (disable auto-rotate first):
$ADB shell settings put system accelerometer_rotation 0
$ADB shell settings put system user_rotation 1   # 1=landscape, 0=portrait
# restore:
$ADB shell settings put system user_rotation 0 ; $ADB shell settings put system accelerometer_rotation 1
```
> When a tap target matters more than speed, prefer Tier 2 (`observe`/Appium) to tap by `testId`
> instead of guessing pixels.

### 3.6 Read runtime errors & warnings (the debugging workhorse)
```bash
$ADB logcat -c                                   # clear, then reproduce
$ADB logcat -d | grep -iE "unique .key.|Uncaught|Error|Warning"      # one-shot dump + filter
$ADB logcat -d | grep -A30 "unique .key."        # full component stack for a specific warning
```
The component stack names the **owning component** — that is how you locate the source (e.g. the key
warning pointed at `Executor.DisplayErrorsManually` → root cause in Fable.React `contextProvider`,
§9).

### 3.7 Android-specific gotchas
- **White/blank inputs in dark mode**: framework inputs used to hardcode `Color.White`; now themeable
  (LEARNINGS 2026-06-28). If you see it, the input theme isn't being applied for the appearance.
- **`backgroundColor` + `borderRadius` renders square** unless the view also has `Overflow.Hidden`
  (LEARNINGS 2026-06-29). Add it to filled rounded views.
- **Runtime crash `Color is expected to match #[0-oa-f]{6}`**: `Color.Hex` requires **lowercase** hex;
  uppercase from CSS throws at runtime even though the build is green.

---

## 4. iOS runbook

iOS shares the **same** `dev-native` + Metro as Android (no `adb reverse`; sim uses `localhost:8081`).

### 4.1 Boot a simulator
```bash
xcrun simctl list devices available
xcrun simctl list devices booted          # is one already up?
xcrun simctl boot "iPhone 16"             # skip if already booted
open -a Simulator                         # show the window
```

### 4.2 Launch / reload
```bash
# build+install on a SPECIFIC simulator (Metro already on :8081):
npx react-native run-ios --simulator "iPhone 16" --no-packager
# subsequent reloads without rebuild:
xcrun simctl launch booted com.eggshell.apptodo
xcrun simctl terminate booted com.eggshell.apptodo   # force-stop
```

### 4.3 Screenshot / rotate / logs
```bash
xcrun simctl io booted screenshot /tmp/ios.png
# rotate: Simulator UI (Cmd+←/→) or Device > Rotate; programmatic orientation is best via Tier 2.
xcrun simctl spawn booted log show --last 2m --style compact | tail -300   # runtime logs
```
### 4.4 iOS gotchas
- After `metro.config.js` / LibClient vendor-patch changes, restart Metro with `--reset-cache`.
- Name collisions ("two iPhone 15"): use the exact runtime `"iPhone 15 (17.0)"` or `--udid`.
- Metro key **`i`** launches a default sim; for a specific device boot it first and use
  `run-ios --simulator "…"`.

---

## 5. Web runbook

### 5.1 Start dev-web and open
```bash
cd SuiteTodo/AppTodo
../../eggshell dev-web        # default port 9080; binds 0.0.0.0 (every interface)
open http://127.0.0.1:9080
# On startup, webpack prints every reachable URL (127.0.0.1, LAN IPs, etc.)
```
### 5.2 Observe
- **Screenshot / console (scripted, headless):** the Playwright smoke `npm run audit:web` (adds a todo,
  fails on console/page errors), or `npm run observe -- snapshot -p web` for the structured bundle, or
  `npm run observe -- open` to keep a browser open.
- **Manual:** browser DevTools console is the fastest error read on web.
### 5.3 Web gotchas (from LEARNINGS)
- **Blank white page, console `ReferenceError: map is not defined`**: a Fable watch partial write left a
  stray `map` line at the end of a generated `.js`. Delete that file and force a full
  `dotnet fable … --noCache`, or restart `dev-web` after a clean.
- **`Directory is locked, waiting…` / wrong Fable version**: delete
  `LibStandard/.build/web/fable/fable.lock`, kill stray `dotnet fable`, confirm `dotnet fable --version`
  is the expected one, wipe `.build/web/fable` after a tool bump.
- **Stale-cache false green**: Fable may print `Skipped compilation … up-to-date` and exit 0 without
  type-checking your edit — force recompile and confirm `Started Fable compilation…`.

---

## 6. The `audit/` toolkit (Tier 2) — commands

Preflight any time something feels off:
```bash
cd SuiteTodo/AppTodo
npm run appium:setup            # one-time: install uiautomator2 + xcuitest drivers
npm run observe -- doctor       # checks PATH, devices, dev-web :9080, Metro :8081, Appium :4723
npm run observe -- setup-devices --list   # pick default AVD + simulator -> native.local.json
```
Observe / interact / diff (`-p web|android|ios`, `--orientation portrait|landscape`):
```bash
npm run observe -- snapshot -p android            # screenshot + layout + logs + health -> audit/out/<ts>/
npm run observe -- add-todo "Buy milk" -p web     # fills the form by testId and captures
npm run observe -- workflow layout-check -p android   # before/after add-todo + card-width diff
npm run observe -- workflow verify-native -p ios   # native smoke: health + add-todo + components
npm run observe -- diff                            # compare the two most recent runs
npm run observe -- logs -p web                     # console (web) / device log (native) snapshot
```
Artifacts land in `audit/out/<timestamp>-*/` (`current.png`, `*-layout-metrics.json`,
`*-ui-hierarchy.xml` / `*-dom-summary.json`, `*-health.json`, `*-log-summary.json`). This is the
machine-readable state an LLM should consume instead of re-deriving from a PNG.

---

## 7. Build / rebuild reference

### 7.1 Targeted recompile (let the watch do it)
The running `dev-native` recompiles only what changed. Force it to notice an edit and to **not**
false-green from cache:
```bash
touch <changed>.fs                         # nudge the watcher
# or, nuke the platform fable cache for a guaranteed full recompile:
rm -rf SuiteTodo/AppTodo/.build/native/fable
```
Editing **framework** files (LibClient/LibRouter/…) recompiles through the app's graph because the watch
prints `Watching ../../..`. No separate framework build needed for native validation.

### 7.2 Confirm the rebuild produced output
```bash
# the emitted JS should be newer than the source you changed:
find SuiteTodo/AppTodo/.build/native/commonjs -name "<File>.js" -newer SuiteTodo/AppTodo/src/.../<File>.fs
grep -nE "error FS" /tmp/dev-native.log | tail        # zero = type-check clean
tail -3 /tmp/dev-native.log                            # "Successfully compiled N files" / "watcher is ready"
```

### 7.3 When a restart is mandatory (not just a touch)
- **`.fsproj` changed** (added/moved/reordered files): Fable watch does **not** pick it up — restart
  `dev-native` (LEARNINGS 2026-06-28). Compile order is the `<Compile Include>` order; a file must come
  after everything it references (e.g. `ComponentsTheme.fs` before the route that uses it).
- **`metro.config.js` / LibClient vendor patch / "looks stale"**: restart Metro with `--reset-cache`.

### 7.4 Verify a `node_modules` / vendor patch actually reached the bundle
Metro bundles ReactXP from `dist`, not `src` (§1). After patching + Metro `--reset-cache`:
```bash
curl -s "http://localhost:8081/index.bundle?platform=android&dev=true" | grep -c "<your-marker>"
```
`0` means your edit isn't in the bundle (wrong file, or Metro cached). Durable native ReactXP patches go
through `LibClient/vendor/reactxp-native-common/` + the `postinstall` copy — but **prefer an F# fix**;
vendored ReactXP edits are maintenance surface the RNW migration deletes (LEARNINGS 2026-06-29).

---

## 8. Killing stale / duplicate processes

Duplicate `dev-native`/Metro instances race each other's output and serve half-old bundles (LEARNINGS).
Detect and clean:
```bash
ps aux | grep -iE "eggshell dev-native|react-native start|dotnet fable|fable.dll|qemu-system" | grep -v grep
# full clean (native dev + emulator):
pkill -f "eggshell dev-native"; pkill -f "dotnet fable"; pkill -f "fable.dll"
pkill -f "plugin-transform-modules-commonjs"; pkill -f "react-native start"
pkill -f "qemu-system-aarch64"; pkill -f "emulator -avd"
```
Notes:
- These watches typically run in **the user's Cursor/IDE terminals**; killing them is fine, then relaunch
  (theirs is authoritative if running). Re-verify with the `ps` line above (expect 0).
- `eggshell dev-native` is a wrapper that returns while the real `dotnet fable …` child keeps watching —
  don't assume the watch died just because the wrapper command "completed".

---

## 9. Gotchas catalog (curated; full detail in LEARNINGS)

| Symptom | Cause | Fix |
|---|---|---|
| Emulator window sideways | AVD `skin.name` reversed + landscape default | §3.1 config.ini + delete snapshot |
| Build green but change not visible | app not reloaded, or Metro cache | reload app; Metro `--reset-cache`; §7.4 |
| `Skipped compilation … up-to-date`, edit ignored | Fable cache false-green | `touch` file / wipe `.build/<plat>/fable` |
| Patch to node_modules has no effect | Metro bundles `dist`, you edited `src` | patch `dist`; verify via §7.4 |
| Runtime `Color is expected to match #[0-oa-f]{6}` | uppercase hex in `Color.Hex` | lowercase all hex |
| Responsive row won't stack on handheld | `LC.Row` appends `FlexDirection.Row` last, overriding styles | use `RX.View` + a direction-correct style |
| Filled rounded view shows square corners (Android) | missing `Overflow.Hidden` | add it to the filled view |
| Dark mode inputs render white | input theme hardcoded white / not applied per appearance | themeable bg + apply per appearance |
| Picker/floating label "wrong background" | label straddles field fill at `top:0` | float label above onto the panel (`top:-8`) |
| Dev-only "unique key" warning | Fable.React `contextProvider` doesn't key children | wrap provider children with `tellReactArrayKeysAreOkay` (F#), not a ReactXP patch |
| `.fsproj` edit ignored by watch | Fable doesn't watch fsproj | restart `dev-native` |
| Web blank page, `map is not defined` | Fable partial-write stray `map` line | delete the `.js`, `dotnet fable … --noCache` |

---

## 10. "I'm stuck" decision tree

1. **Does it build?** `grep "error FS" /tmp/dev-native.log`. If errors → fix F#; if "Skipped … up-to-date"
   → force recompile (§7.1).
2. **Did the change reach the device?** Reload the app. Still old? Metro `--reset-cache`; for a
   node_modules/vendor patch, verify with §7.4.
3. **Blank screen / red box?** Read it: `adb logcat -d | grep -A30 -iE "error|unique"` (Android),
   `simctl … log show` (iOS), DevTools console (web). The component stack names the owner — start there.
4. **Renders but wrong layout/spacing/rounding?** Screenshot + zoom (§3.4). Check the gotchas table (§9)
   — most visual bugs there are framework-style interactions, not your code.
5. **Need to confirm an element/state precisely or gate a fix?** Switch to Tier 2:
   `npm run observe -- snapshot` / `workflow layout-check` for JSON + diffs.
6. **Everything's weird / duplicated output?** You probably have stale duplicate watches — §8, then
   restart one `dev-native` + one Metro.
```
