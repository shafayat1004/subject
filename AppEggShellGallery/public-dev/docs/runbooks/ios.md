# iOS Runbook

Full step-by-step runbook for booting an iOS simulator, launching and reloading the app, capturing screenshots, rotating, reading logs, and dealing with iOS-specific gotchas.

iOS shares the same `dev-native` + Metro as Android. No `adb reverse` is needed; the simulator uses `localhost:8081`.

Examples use AppEggShellGallery (`com.eggshell.appgallery`). Substitute your app's bundle ID and simulator name.

Related: [Dev loop](./runbooks/dev-loop.md) | [Android](./runbooks/android.md) | [Build and rebuild](./runbooks/build-rebuild.md) | [Troubleshooting](./runbooks/troubleshooting.md)

---

## Boot a simulator {#boot}

```bash
xcrun simctl list devices available
xcrun simctl list devices booted          # is one already up?
xcrun simctl boot "iPhone 17 Pro Max"     # skip if already booted
open -a Simulator                         # show the window
```

**Name collision gotcha.** If you have multiple simulators with similar names (e.g. "two iPhone 15"), use the exact runtime qualifier `"iPhone 15 (17.0)"` or `--udid`.

---

## Start the toolchain {#start-toolchain}

Same as Android: one `dev-native` + one Metro serve both platforms at once.

```bash
export DOTNET_ROOT="$HOME/.dotnet"
cd SuiteTodo/AppTodo
../../eggshell dev-native &
npx react-native start --port 8081 &
# no adb reverse needed for iOS simulator
```

---

## Launch and reload {#launch-reload}

First-time install or after native dep changes:

```bash
npx react-native run-ios --simulator "iPhone 17 Pro Max" --no-packager
```

**`run-ios` fails here with "Something went wrong while installing CocoaPods"** even after a manual
`pod install` succeeds — none of our apps has a root `Gemfile`, and the RN 0.86 CLI hard-requires one
before it will drive CocoaPods. Workaround: `pod install` manually, then build + install directly:

```bash
xcrun simctl list devices available | grep "iPhone 17 Pro Max"   # get the UDID
xcodebuild -workspace ios/<Workspace>.xcworkspace -scheme <Scheme> -configuration Debug \
  -destination "platform=iOS Simulator,id=<SIMUDID>" -derivedDataPath ios/build_derived
xcrun simctl install <SIMUDID> ios/build_derived/Build/Products/Debug-iphonesimulator/<App>.app
xcrun simctl launch <SIMUDID> <bundleid>
```

Metro (and `eggshell dev-native`) must already be running for the JS bundle to load.

Subsequent reloads without a rebuild:

```bash
xcrun simctl terminate booted com.eggshell.apptodo   # force-stop
xcrun simctl launch booted com.eggshell.apptodo
```

**Metro key `i`** launches a default simulator. For a specific device, boot it first and use `run-ios --simulator "..."` instead.

---

## Screenshot, rotate, and logs {#observe}

```bash
xcrun simctl io booted screenshot /tmp/ios.png
```

Rotate: use Simulator UI (Cmd+Left/Right arrow) or Device menu > Rotate. Programmatic orientation is more reliable via the Tier 2 audit toolkit.

Read runtime logs:

```bash
xcrun simctl spawn booted log show --last 2m --style compact | tail -300
```

JS-channel red-box / uncaught errors are also on the `com.facebook.react.log:javascript` subsystem:

```bash
xcrun simctl spawn booted log show --last 3m \
  --predicate 'eventMessage CONTAINS[c] "javascript"' | grep -v SecTrust
```

---

## Interacting with the simulator (tap / swipe / type / hierarchy) {#interact}

**`xcrun simctl` cannot tap, swipe, type text, or dump the view hierarchy.** This is the one
big asymmetry with Android: there is no iOS equivalent of `adb shell input tap` or
`uiautomator dump` in the built-in CLI. `simctl` only does screenshot, log, launch/terminate,
`openurl` (deep links), `push`, and appearance/location/status-bar overrides. So a raw-CLI iOS
loop can *observe* (screenshot + logs) but cannot *drive* the UI — e.g. it cannot dismiss a LogBox
red box or navigate its "Log 1 of 2 / 2 of 2" pager, or tap a button by id.

To actually drive iOS, pick one:

1. **Tier 2 audit toolkit (preferred — already installed).** Appium + `xcuitest` driver, tap by
   `testId`/accessibility id, structured JSON output. WebDriverAgent is already built for this repo.
   No new dependency. See [Audit toolkit](./runbooks/audit-toolkit.md).
   ```bash
   cd SuiteTodo/AppTodo
   npm run observe -- doctor                 # checks Appium :4723 + simulator
   npm run observe -- snapshot -p ios        # screenshot + hierarchy + logs (reads LogBox text too)
   npm run observe -- add-todo "Buy milk" -p ios
   ```

2. **`idb` (raw-CLI, adb-analog).** Meta's iOS Development Bridge — the closest thing to `adb`:
   `idb ui tap <x> <y>`, `idb ui swipe`, `idb ui text`, `idb ui key`, `idb ui describe-all`
   (JSON accessibility tree). Not installed by default; needs a full Xcode install:
   ```bash
   brew install idb-companion
   pip3 install fb-idb
   idb ui describe-all      # dump on-screen elements + bounds
   idb ui tap 200 120       # tap a point
   ```
   Actively maintained but has a history of breaking on new Xcode releases — verify after Xcode
   upgrades.

3. **`axe` (raw-CLI, newer, simulator-only).** Lightweight Swift CLI over Apple's HID +
   accessibility APIs: `axe tap`, `axe type`, `axe describe-ui`, `axe screenshot`. Simulators only
   (no physical devices).
   ```bash
   brew tap cameroncooke/axe && brew install axe
   axe describe-ui          # accessibility hierarchy
   axe tap -x 200 -y 120
   ```

**Rule of thumb:** raw `simctl` (Tier 1) to observe a red box or eyeball a render; Appium
(`npm run observe`, Tier 2) to tap, read the full LogBox text, or gate a fix. Reach for `idb`/`axe`
only when you want adb-style raw tapping without standing up an Appium server.

---

## iOS-specific gotchas {#gotchas}

| Symptom | Cause | Fix |
|---|---|---|
| Old bundle after `metro.config.js` or LibClient vendor patch change | Metro cached the old bundle | Restart Metro with `--reset-cache`. |
| `run-ios` launches the wrong simulator | Multiple simulators match by name | Use exact form `"iPhone 15 (17.0)"` or `--udid`. |
| `run-ios` finds zero eligible destinations | Xcode simulator runtime version mismatch | Download the correct runtime via Xcode Components or `xcodebuild -downloadPlatform iOS`. |
| Build fails with `.xcodeproj` not found | Used the wrong project file | Open `.xcworkspace`, not `.xcodeproj`. |
| `pod install` fails with version conflicts | ThirdParty React Native libs with conflicting peer ranges | Use `--legacy-peer-deps` when needed and verify Podfile platform target (`platform :ios, '15.5'` for CodePush 9.x). |
| `run-ios` errors "Something went wrong while installing CocoaPods" despite pods already installed | RN 0.86 CLI requires a root `Gemfile`; none of our apps has one | `pod install` manually, then `xcodebuild` + `simctl install`/`launch` directly (see [Launch and reload](#launch-reload)). |
| Can't dismiss a LogBox red box / can't tap a button from the CLI | `simctl` has no tap/swipe/type | Use Tier 2 Appium (`npm run observe -- snapshot -p ios` reads the LogBox text; tap by id), or install `idb`/`axe`. See [Interacting with the simulator](#interact). |

For a complete catalog of build, styling, and layout gotchas, see [Troubleshooting](./runbooks/troubleshooting.md).
