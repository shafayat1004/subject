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

### Native scaffold

First time (or fresh clone):

```bash
cd SuiteTodo/AppTodo
./scripts/scaffold-native.sh   # android/ + ios/ (idempotent)
./initialize                   # keystore + npm
../../eggshell build-native
cd ios && pod install && cd ..
```

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
| `npm run observe:setup-devices` | List AVDs/simulators; set defaults in `audit/native.local.json` |
| `npm run observe -- setup-devices --android Android_Desktop --ios "iPhone 16"` | Save device defaults |
| `npm run observe -- doctor --json` | Same checks, JSON for LLM agents |
| `npm run observe -- doctor -p android` | Single-platform doctor |
| `npm run observe -- snapshot [-p platform]` | Full capture bundle (logs + health by default) |
| `npm run observe -- add-todo "title" [-p platform]` | Add todo + capture |
| `npm run observe:layout-check` | Before/after card width diff |
| `npm run observe:verify-native` | Android/iOS smoke: health + add-todo + components |
| `npm run observe -- diff [dirA dirB]` | Compare two runs |
| `npm run observe -- open` | Web only — browser stays open |
| `npm run observe -- logs [-p platform]` | Console or device logs (also included in every snapshot) |

Platform flag: `--platform android` or `-p ios` (default: `web`).

Timeout: `--timeout-ms 120000` (app ready / end states; default 120s).

## App health detection

Every snapshot writes `*-health.json` and fails (exit 2) when the app is in a crash state:

| State | Web | Native |
|-------|-----|--------|
| `healthy` | Todo UI + testIds visible | Same via Appium |
| `metro_redbox` | — | Metro bundle error (e.g. missing module, HTTP 500) |
| `webpack_overlay` | Webpack dev-server overlay | — |
| `top_level_error` | EggShell "Oops!" / "Something went wrong" | Same text when shell crashes |
| `background` | — | Wrong package in foreground |
| `loading` | Foreground but UI not ready yet | Splash / bundle loading |

Patterns aligned with `AppEggShellGallery/audit-gallery-app-crash.mjs`. Native also streams **logcat** (`ReactNativeJS`, `AndroidRuntime`) for the whole session.

## Doctor · toolchain PATH

`npm run observe:doctor` prints a **Toolchain · PATH & SDK** section and a **Devices · emulators & simulators** section:

- Installed Android AVDs and connected adb devices (with AVD name when available)
- Available iOS simulators and which are booted
- Warning when **multiple iOS simulators are booted** (observe needs one target — set a default or quit extras)
- Configured defaults from `audit/native.local.json`

## Device defaults

Observe sessions pick targets from `audit/native.local.json` (gitignored):

```bash
cp audit/native.local.json.example audit/native.local.json
npm run observe:setup-devices                              # list all
npm run observe -- setup-devices --android Android_Desktop
npm run observe -- setup-devices --ios "iPhone 16"
npm run observe -- setup-devices --orientation portrait   # default for phone tests
```

- **Android:** matches connected emulator to `defaultAndroidAvd`; errors if multiple adb devices without a default
- **iOS:** uses `defaultIosSimulator` when set (boots it if shut down); warns when multiple simulators are booted
- **Orientation:** defaults to **portrait** for native sessions. Override per run: `--orientation landscape`, or set `deviceOrientation` in `native.local.json`, or env `APPTODO_DEVICE_ORIENTATION`

Toolchain PATH checks: `adb`, `emulator`, `eggshell`, `node`, `npx`, `react-native`, `ANDROID_SDK`, `xcrun`, `pod`, etc.

## Artifacts

### Web (`audit/out/<timestamp>-*/`)

- `manifest.json`, `current.png`, `*-layout-metrics.json`, `*-dom-summary.json`
- `*-health.json`, `*-log-summary.json`
- `*-ui-snapshot.json` (DEBUG `window.eggshell.AppTodo.uiSnapshot()`)
- `*-console.log`, `*-page-errors.log` (always written)

### Native

- `current.png`, `*-layout-metrics.json` (testID bounds + card heuristic)
- `*-health.json`, `*-log-summary.json`
- `*-ui-hierarchy.xml` + `*-ui-summary.json` (compact tree for LLM)
- `*-device.log` (logcat or simulator log)
- `session-logcat.log` (full Android session stream when using observe)

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
npm run observe -- workflow verify-native --platform android
npm run observe -- snapshot --timeout-ms 60000   # shorter wait
npm run observe -- snapshot --orientation landscape -p android
npm run observe -- snapshot --headless true      # web CI
```

## Templating

Reference for EggShell scaffolding. Health/crash detection patterns come from `AppEggShellGallery/audit-gallery-app-crash.mjs` and `audit-gallery-android-logcat.mjs`. Shared drivers will move to `Meta/LibScaffolding` as first-party observability.
