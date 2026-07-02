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
xcrun simctl boot "iPhone 16"             # skip if already booted
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
npx react-native run-ios --simulator "iPhone 16" --no-packager
```

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

---

## iOS-specific gotchas {#gotchas}

| Symptom | Cause | Fix |
|---|---|---|
| Old bundle after `metro.config.js` or LibClient vendor patch change | Metro cached the old bundle | Restart Metro with `--reset-cache`. |
| `run-ios` launches the wrong simulator | Multiple simulators match by name | Use exact form `"iPhone 15 (17.0)"` or `--udid`. |
| `run-ios` finds zero eligible destinations | Xcode simulator runtime version mismatch | Download the correct runtime via Xcode Components or `xcodebuild -downloadPlatform iOS`. |
| Build fails with `.xcodeproj` not found | Used the wrong project file | Open `.xcworkspace`, not `.xcodeproj`. |
| `pod install` fails with version conflicts | ThirdParty React Native libs with conflicting peer ranges | Use `--legacy-peer-deps` when needed and verify Podfile platform target (`platform :ios, '15.5'` for CodePush 9.x). |

For a complete catalog of build, styling, and layout gotchas, see [Troubleshooting](./runbooks/troubleshooting.md).
