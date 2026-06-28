# AppTodo dev observability

First-class tooling for **humans and LLM agents** on **web, Android, and iOS**: screenshots, layout metrics, DOM/UI hierarchy, device logs, and named workflows.

**Web:** headed browser by default (Playwright). **Native:** Appium via WebdriverIO (device/emulator stays visible).

## Quick start by platform

### Web

```bash
cd SuiteTodo/AppTodo
./initialize
../../eggshell dev-web                    # http://127.0.0.1:9080
npm run observe -- doctor                 # verify dev-web
npm run observe -- snapshot
npm run observe:layout-check
```

### Android

```bash
# One-time
npm run appium:setup
npm run appium                            # terminal 1 — Appium on :4723

# Native project + Metro (when android/ exists)
../../eggshell dev-android                # or dev-native-server + install APK
adb reverse tcp:8081 tcp:8081

npm run observe -- doctor --platform android
npm run observe -- snapshot --platform android
npm run observe -- workflow layout-check --platform android
```

### iOS (macOS + Xcode)

```bash
npm run appium:setup
npm run appium                            # terminal 1

# Boot simulator, install app via ../../eggshell dev-native
npm run observe -- doctor --platform ios
npm run observe -- snapshot --platform ios
```

AppTodo does not ship `android/` / `ios/` until native is scaffolded (`eggshell dev-android` / `dev-native`). **Doctor** tells you exactly what is missing.

## Commands (all platforms)

| Command | Description |
|---------|-------------|
| `npm run observe:doctor` | **All platforms** — web + Android + iOS health (beige UI) |
| `npm run observe -- doctor --json` | Same checks, JSON for LLM agents |
| `npm run observe -- doctor -p android` | Single-platform doctor |
| `npm run observe -- snapshot [-p platform]` | Full capture bundle |
| `npm run observe -- add-todo "title" [-p platform]` | Add todo + capture |
| `npm run observe:layout-check` | Before/after card width diff |
| `npm run observe -- diff [dirA dirB]` | Compare two runs |
| `npm run observe:open` | Web only — browser stays open |
| `npm run observe -- logs [-p platform]` | Console or device logs |

Platform flag: `--platform android` or `-p ios` (default: `web`).

## Artifacts

### Web (`audit/out/<timestamp>-*/`)

- `manifest.json`, `current.png`, `*-layout-metrics.json`, `*-dom-summary.json`
- `*-ui-snapshot.json` (DEBUG `window.__eggshell.AppTodo.uiSnapshot()`)
- `*-console.log`, `*-page-errors.log`

### Native

- `current.png`, `*-layout-metrics.json` (testID bounds + card heuristic)
- `*-ui-hierarchy.xml` + `*-ui-summary.json` (compact tree for LLM)
- `*-device.log` (logcat or simulator log)

## Native app IDs

Defaults: `com.eggshell.apptodo` / `MainActivity` / iOS bundle `com.eggshell.apptodo`.

Override:

1. Copy `audit/native.local.json.example` → `audit/native.local.json`
2. Or env: `APPTODO_ANDROID_PACKAGE`, `APPTODO_ANDROID_ACTIVITY`, `APPTODO_IOS_BUNDLE_ID`
3. Or auto-read from `android/app/build.gradle` when present

## Test IDs

| testId | Element |
|--------|---------|
| `todo-page` | Page wrapper |
| `todo-card` | Card shell |
| `todo-new-title` | New todo input |
| `todo-add` | Add button |
| `todo-search` | Search input |

On native, RN `testID` → Android `resource-id` / iOS accessibility id (`~todo-new-title` in Appium).

## LLM copy-paste

```bash
npm run observe -- doctor --platform android
npm run observe -- snapshot --platform android
npm run observe -- workflow layout-check --platform android
npm run observe -- snapshot --headless true          # web CI
```

## Templating

Reference for EggShell scaffolding. Shared drivers will move to `Meta/LibScaffolding`; gallery `audit-gallery-android-driver.mjs` was the prior art.
