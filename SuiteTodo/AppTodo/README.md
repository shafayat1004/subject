# AppTodo

Reference EggShell todo app: F# + Fable + ReactXP on web and React Native 0.76 on Android/iOS. Uses **fake in-memory todos** in DEBUG by default (no backend silo required).

All commands below assume you are in **`SuiteTodo/AppTodo`**. The `eggshell` CLI is invoked as **`../../eggshell`** from here.

---

## Prerequisites

- Monorepo bootstrapped: `./initialize` at repo root
- Node ≥ 18.19 (`./initialize` in this app runs `npm install`)
- **Web:** nothing else
- **Android:** Android Studio, SDK, an AVD (API 33+ recommended), `ANDROID_SDK` on PATH
- **iOS (macOS):** Xcode, CocoaPods, iOS simulator runtime
- **Observe (native automation):** Appium — see [Observability](#observability)

---

## One-time setup

### Every clone

```bash
cd SuiteTodo/AppTodo
./initialize
```

Creates npm links, debug keystore (when `android/` exists), and symlinks `public-dev/configSourceOverrides.dev.js`.

### Native (first time or no `android/` / `ios/`)

```bash
./scripts/scaffold-native.sh   # idempotent — creates android/, ios/, configSourceOverrides.native.js
./initialize
../../eggshell build-native
cd ios && pod install && cd ..
```

If Android still shows a stale splash or launcher icon, re-apply AppTodo branding:

```bash
./scripts/install-apptodo-branding.sh
# then rebuild/reinstall the native app
```

### Observability (optional, for `npm run observe`)

```bash
npm run appium:setup    # once — installs UiAutomator2 + XCUITest drivers
cp audit/native.local.json.example audit/native.local.json   # optional device defaults
```

---

## Web development

**Terminal 1:**

```bash
../../eggshell dev-web    # http://127.0.0.1:9080
```

Config: `configSourceOverrides.dev.js` — fake todos by default. Uncomment `BackendUrl` there only if you run a real Todo backend on `:5001`.

**Verify:**

```bash
npm run observe:doctor
npm run observe:snapshot
```

---

## Native development

Native debug loads JS from **Metro on port 8081**. Fable output lives in `.build/native/commonjs`. You need **both** Fable watch and Metro; the native shell only displays what those produce.

### Architecture (three moving parts)

| Piece | Command | Role |
|-------|---------|------|
| Fable watch | `../../eggshell dev-native` | F# → `.build/native/commonjs` on save |
| Metro | `npx react-native start --port 8081` | Serves bundle to device/simulator |
| Native app | Launch via Metro or `run-*` | Android APK / iOS `.app` shell |

Config: `configSourceOverrides.native.js` — Metro host is `10.0.2.2:8081` (Android emulator) or `localhost:8081` (iOS simulator). Fake todos by default.

### Recommended day-to-day workflow

**Terminal 1 — Fable watch (leave running):**

```bash
../../eggshell dev-native
```

**Terminal 2 — Metro (leave running):**

```bash
npx react-native start --port 8081
```

After changing `metro.config.js`, LibClient vendor patches, or when the app looks stale:

```bash
npx react-native start --port 8081 --reset-cache
```

**Launch platforms** (pick one style below).

#### Style A — Metro interactive keys (simplest)

With Metro running, in the **same Metro terminal**:

- Press **`a`** — build/install/launch **Android** (start your chosen AVD first; see [Android-only checklist](#android-only-checklist))
- Press **`i`** — build/install/launch **iOS** on the default/booted simulator (for a **specific** simulator, use Style B below)

#### Style B — Separate CLI (`run-* --no-packager`)

Use when you prefer a second terminal instead of Metro keys.

**Prerequisites before `run-* --no-packager`:**

1. `../../eggshell dev-native` running (or at least one successful `../../eggshell build-native`)
2. `npx react-native start --port 8081` already running
3. **Android:** chosen AVD running + `adb reverse tcp:8081 tcp:8081` (once per emulator boot)
4. **iOS:** target simulator booted, or pass `--simulator` / `--udid` to `run-ios` (see [iOS-only checklist](#ios-only-checklist))

```bash
# Android
adb reverse tcp:8081 tcp:8081
npx react-native run-android --no-packager

# iOS
npx react-native run-ios --simulator "iPhone 16" --no-packager
```

`npm run android` / `npm run ios` without `--no-packager` will try to start Metro themselves — avoid that if Metro is already on `:8081`.

### Android-only checklist

**1. List available emulators (AVDs):**

```bash
emulator -list-avds
# or
npm run observe:setup-devices    # lists AVDs + simulators
```

**2. Start the AVD you want** (Android Studio Device Manager, or CLI):

```bash
emulator -avd Medium_Phone_API_35
```

**3. Once per emulator boot** — Metro port forwarding:

```bash
adb reverse tcp:8081 tcp:8081
```

**4. dev-native + Metro** (see above), then launch:

```bash
npx react-native run-android --no-packager
# Metro key `a` uses whichever emulator adb reports as connected
```

Multiple emulators: pick one with `adb devices`, then `npx react-native run-android --device <id> --no-packager` (or `--list-devices` for an interactive picker).

Set a default AVD for observe automation: `npm run observe -- setup-devices --android Medium_Phone_API_35`.

### iOS-only checklist

**1. List available simulators:**

```bash
xcrun simctl list devices available
# or
npm run observe:setup-devices    # lists simulators + AVDs
```

Use the **device name** column (e.g. `iPhone 16`, `iPhone SE (3rd generation)`). If several runtimes share a name, disambiguate with the iOS version in parentheses (see step 4).

**2. Boot the simulator you want** (Xcode → Open Developer Tool → Simulator, or CLI):

```bash
xcrun simctl boot "iPhone 16"
open -a Simulator
```

Skip `boot` if that simulator is already running (check with `xcrun simctl list devices booted`).

**3. dev-native + Metro** (see above), then launch on **that** simulator:

```bash
# By name (recommended)
npx react-native run-ios --simulator "iPhone 16" --no-packager

# Exact runtime when names collide, e.g. two "iPhone 15" entries
npx react-native run-ios --simulator "iPhone 15 (17.0)" --no-packager

# Interactive picker
npx react-native run-ios --list-devices --no-packager

# By UDID (from simctl list)
npx react-native run-ios --udid <SIMULATOR-UDID> --no-packager
```

Metro key **`i`** builds and launches on a default simulator (often the booted one or Xcode’s default). For a **specific** device, prefer `run-ios --simulator "…"` after booting it.

**4. Keep one simulator booted** when using observe or Metro — multiple booted simulators cause ambiguous targets. Set a default for observe:

```bash
npm run observe -- setup-devices --ios "iPhone 16"
```

Open **`ios/*.xcworkspace`** in Xcode (not `.xcodeproj`) when debugging native code. Re-run `pod install` after Podfile changes.

### Android + iOS together

**One** `dev-native` and **one** Metro serve **both** platforms simultaneously.

```bash
# T1
../../eggshell dev-native

# T2
npx react-native start --port 8081

# Android: emulator + adb reverse, then press `a` or run-android --no-packager
# iOS: boot simulator, then press `i` or run-ios --simulator "iPhone 16" --no-packager
```

Reload each app independently (Android: R R; iOS: Cmd+R; or press **`r`** in Metro).

### One-shot eggshell commands (alternative)

```bash
../../eggshell dev-android    # compile + run on Android
../../eggshell dev-ios        # compile + run on iOS (macOS)
```

For iterative UI work, the **dev-native + Metro + launch** flow above is more reliable than repeated one-shots.

---

## When changes do not apply (full restart)

Metro often misses updates under `.build/native`. Do not trust Fable output that says `Skipped compilation because all generated files are up-to-date!` as proof your edits were type-checked.

```bash
# 1. Stop Metro and dev-native

# 2. Force native recompile
rm -rf .build/native
../../eggshell build-native

# 3. Restart Metro with clean cache
npx react-native start --port 8081 --reset-cache

# 4. Restart dev-native in another terminal
../../eggshell dev-native

# 5. Relaunch app (Metro `a`/`i`, or run-* --no-packager)
# Android: adb reverse tcp:8081 tcp:8081 again if emulator was restarted
```

**Validate builds:** confirm Fable log shows `Started Fable compilation...` and your changed files in `Compiled N/M: ...`. Grep for `error FS` before calling a change done.

---

## Observability

Headed, agent-friendly tooling under `audit/` for **web (Playwright)**, **Android**, and **iOS (Appium)**. Screenshots, layout metrics, DOM/UI hierarchy, console/device logs, health detection, and named workflows.

Full audit internals: [`audit/README.md`](audit/README.md).

### npm scripts

| Script | Description |
|--------|-------------|
| `npm run observe:doctor` | Health check — web + Android + iOS toolchain and dev servers |
| `npm run observe:snapshot` | Full capture bundle (default platform: web) |
| `npm run observe:layout-check` | Before/after add-todo + layout diff (web) |
| `npm run observe:verify-native` | Native smoke: health + add-todo |
| `npm run observe:android` | Snapshot on Android |
| `npm run observe:ios` | Snapshot on iOS |
| `npm run observe:open` | Web — keep browser open |
| `npm run observe:setup-devices` | List AVDs/simulators; configure defaults |

CLI entrypoint: `npm run observe -- <command> [options]`.

### Common commands

```bash
# Health (run dev stack first for the platform you care about)
npm run observe:doctor
npm run observe:doctor -p android
npm run observe:doctor -p ios

# Snapshots (headed browser / visible device by default)
npm run observe:snapshot
npm run observe -- snapshot -p android
npm run observe -- snapshot -p ios

# Workflows
npm run observe:layout-check
npm run observe:verify-native

# Agent-friendly JSON
npm run observe -- doctor --json
npm run observe -- snapshot --timeout-ms 60000
npm run observe -- snapshot --orientation landscape -p android
```

Platform flag: `-p web` | `-p android` | `-p ios` (default: `web`).

**Native observe** needs Appium running: `npm run appium` (terminal, port `:4723`).

### App health detection

Every snapshot writes `*-health.json`. Exits with code **2** when the app is crashed or not ready.

| State | Meaning |
|-------|---------|
| `healthy` | Todo UI visible (testIds found) |
| `metro_redbox` | Native Metro bundle error |
| `render_error` | LogBox render crash |
| `webpack_overlay` | Webpack dev-server error overlay |
| `top_level_error` | EggShell top-level error screen |
| `background` | Wrong app in foreground (native) |
| `loading` | Foreground but UI not ready |

Native sessions poll logcat/simulator logs and Appium for LogBox / render errors and fail fast instead of waiting the full timeout.

### Doctor output

- **Toolchain:** `node`, `npx`, `eggshell`, `adb`, `emulator`, `ANDROID_SDK`, `xcrun`, `pod`, etc.
- **Devices:** AVD list, booted simulators, Metro `:8081`, dev-web `:9080`, Appium `:4723`
- Warns when **multiple iOS simulators** are booted (pick one default)

### Device defaults

Copy `audit/native.local.json.example` → `audit/native.local.json` (gitignored), then:

```bash
npm run observe:setup-devices
npm run observe -- setup-devices --android Medium_Phone_API_35 --ios "iPhone 16"
npm run observe -- setup-devices --orientation portrait
```

### Snapshot artifacts

Written under `audit/out/<timestamp>-<label>/`:

**Web:** `current.png`, layout metrics, DOM summary, `*-console.log`, `*-page-errors.log`, `*-health.json`, optional `*-ui-snapshot.json`.

**Native:** `current.png`, layout metrics (testID bounds), UI hierarchy XML + summary, device log, health + log summary.

### Test IDs (automation)

| testId | Element |
|--------|---------|
| `todo-page` | Page wrapper |
| `todo-card` | Card shell |
| `todo-new-title` | New todo input |
| `todo-add` / `todo-add-mobile` | Add button |
| `todo-search` | Search input |
| `todo-new-priority` | Priority picker |
| `todo-stats-open` / `todo-stats-done` | Stats chips |
| `todo-filter-*` | Filter tabs |
| `todo-item-{id8}-*` | Per-row actions (toggle, edit, delete, …) |

On native, `testID` maps to Android `resource-id` / iOS accessibility id (`~todo-new-title` in Appium).

### Compare runs

```bash
npm run observe -- diff audit/out/run-a audit/out/run-b
```

---

## Config reference

| File | Platform | Notes |
|------|----------|-------|
| `configSourceOverrides.dev.js` | Web | Symlinked into `public-dev/` by `./initialize` |
| `configSourceOverrides.native.js` | Android/iOS | Created by `scaffold-native.sh`; gitignored template pattern |
| `audit/native.local.json` | Observe | Device defaults; gitignored |

Uncomment `BackendUrl` in the relevant config to use a real Todo backend instead of `FakeTodoService`.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Blank / stale UI | Full restart (see above); Metro `--reset-cache` |
| Android cannot reach Metro | `adb reverse tcp:8081 tcp:8081` |
| `configSourceOverrides.native.js missing` | `./scripts/scaffold-native.sh` |
| iOS build fails after pod change | `cd ios && pod install` |
| Observe native fails | `npm run observe:doctor`; ensure Appium + Metro + one simulator/emulator |
| Wrong iOS simulator launches | Boot target first: `xcrun simctl boot "iPhone 16"`; use `run-ios --simulator "iPhone 16"` |
| Multiple iOS simulators booted | Quit extras (Simulator → Window); set default: `npm run observe -- setup-devices --ios "…"` |
| Web "Network failure" loading todos | Remove or comment `BackendUrl` in dev config; use fake service |
| Fable "Skipped compilation" | Touch changed `.fs` or delete `.build/native/fable`; re-run build |

---

## Related docs

- [`audit/README.md`](audit/README.md) — observability implementation details
- [`AppEggShellGallery/public-dev/docs/basics/native.md`](../../AppEggShellGallery/public-dev/docs/basics/native.md) — framework native dev guide
- [`AppEggShellGallery/public-dev/docs/native/dev-experience.md`](../../AppEggShellGallery/public-dev/docs/native/dev-experience.md) — Metro reload quirks
