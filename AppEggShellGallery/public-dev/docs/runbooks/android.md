# Android Runbook

Full step-by-step runbook for booting an Android emulator, starting the EggShell toolchain, launching and reloading the app, capturing screenshots, driving interactions, and reading runtime errors.

Examples use AppTodo (`com.eggshell.apptodo`, AVD `Medium_Phone_API_35`). Substitute your app's package, AVD name, and paths as needed. Paths shown are macOS defaults.

Related: [Dev loop](./runbooks/dev-loop.md) | [Build and rebuild](./runbooks/build-rebuild.md) | [Troubleshooting](./runbooks/troubleshooting.md) | [Audit toolkit](./runbooks/audit-toolkit.md)

---

## Boot the emulator (and fix sideways/landscape permanently) {#boot}

```bash
~/Library/Android/sdk/emulator/emulator -list-avds
~/Library/Android/sdk/emulator/emulator -avd Medium_Phone_API_35 -no-snapshot-load &   # cold boot
~/Library/Android/sdk/platform-tools/adb wait-for-device
```

**Gotcha: emulator launches in landscape / sideways.** The AVD config has a reversed skin and landscape default. Permanent fix in `~/.android/avd/<AVD>.avd/config.ini`:

```
hw.initialOrientation = portrait
skin.name = 1080x2400          # WIDTHxHEIGHT -- a reversed "2400x1080" forces a landscape window
```

Then delete the cached state so quickboot does not restore the old landscape:

```bash
rm -f  ~/.android/avd/<AVD>.avd/hardware-qemu.ini ~/.android/avd/<AVD>.avd/hardware-qemu.ini.lock
rm -rf ~/.android/avd/<AVD>.avd/snapshots/default_boot
```

Relaunch with `-no-snapshot-load`. Verify:

```bash
adb shell dumpsys window | grep -E "mCurrentRotation|mDisplayRotation"   # ROTATION_0 = portrait
```

---

## Start the toolchain {#start-toolchain}

Run from the app directory (`SuiteTodo/AppTodo` for AppTodo, `AppEggShellGallery` for the gallery):

```bash
export DOTNET_ROOT="$HOME/.dotnet"          # always, before dotnet/fable/eggshell
cd SuiteTodo/AppTodo
../../eggshell dev-native &                  # Fable watch (terminal 1)
npx react-native start --port 8081 &         # Metro (terminal 2)
adb reverse tcp:8081 tcp:8081                # once per emulator boot
```

A full native (re)install is only needed when native code or deps changed:

```bash
adb reverse tcp:8081 tcp:8081
npx react-native run-android --no-packager
```

---

## Launch and reload the app {#launch-reload}

If the app is already installed, force-stop and relaunch without a full native rebuild:

```bash
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB shell am force-stop com.eggshell.apptodo
$ADB shell monkey -p com.eggshell.apptodo -c android.intent.category.LAUNCHER 1
# the first JS bundle build after a Metro (re)start is slow -- wait ~25-30s before screenshotting
```

---

## Screenshot {#screenshot}

```bash
$ADB shell screencap -p /sdcard/s.png && $ADB pull /sdcard/s.png /tmp/s.png
```

**Zoom into a pixel detail** (useful for catching a square-cornered toggle invisible at full scale):

```bash
# sips -c <cropH> <cropW> --cropOffset <y> <x> in.png --out out.png   (1080x2400 device)
sips -c 144 453 --cropOffset 108 626 /tmp/s.png --out /tmp/toggle.png
```

---

## Tap, type, and rotate {#interact}

```bash
$ADB shell input tap <x> <y>            # PIXEL coords -- recompute after layout changes (brittle)
$ADB shell input text "hello"
# rotate (disable auto-rotate first):
$ADB shell settings put system accelerometer_rotation 0
$ADB shell settings put system user_rotation 1   # 1=landscape, 0=portrait
# restore:
$ADB shell settings put system user_rotation 0 ; $ADB shell settings put system accelerometer_rotation 1
```

When a tap target matters more than speed, prefer Tier 2 (`observe`/Appium) to tap by `testId` instead of guessing pixels. See [Audit toolkit](./runbooks/audit-toolkit.md).

---

## Read runtime errors and warnings {#read-errors}

This is the debugging workhorse. Clear the log before reproducing the issue, then dump and filter:

```bash
$ADB logcat -c                                   # clear, then reproduce
$ADB logcat -d | grep -iE "unique .key.|Uncaught|Error|Warning"      # one-shot dump + filter
$ADB logcat -d | grep -A30 "unique .key."        # full component stack for a specific warning
```

The component stack names the **owning component**. That is how you locate the source (for example, the "unique key" warning pointed at `Executor.DisplayErrorsManually`, whose root cause was in Fable.React `contextProvider`).

---

## Install a standalone build on a physical device (wireless adb) {#physical-device}

For a self-contained build that runs without Metro (JS bundled into the APK), on a real phone over Wi-Fi.

**1. Pair + connect over wireless adb.** On the phone: Developer options -> Wireless debugging. There are **two different ports**: the **pairing** port (in the "Pair device with pairing code" dialog, shown with a 6-digit code) and the **connect** port (on the main Wireless debugging screen). Both, plus the code, rotate each time the dialog opens and time out fast, so pair where you can read both at once.

```bash
ping -c 2 <phone-ip>                                   # confirm reachable first
adb pair <phone-ip>:<PAIRING_PORT> <6-digit-code>      # from the pairing dialog
adb connect <phone-ip>:<CONNECT_PORT>                  # from the main screen (different port)
adb devices -l                                         # confirm the device is listed
```

A bare `adb connect` that "failed to connect" against a host that pings usually means not-yet-paired or you used the pairing port instead of the connect port.

**2. Build a signed release APK.** The `release` signingConfig in `android/app/build.gradle` is empty unless `MYAPP_RELEASE_*` gradle properties are set, so a plain `assembleRelease` produces an **unsigned, uninstallable** APK. For a throwaway build, sign with the debug keystore:

```bash
cd SuiteTodo/AppTodo/android
./gradlew assembleRelease \
  -PMYAPP_RELEASE_STORE_FILE=debug.keystore -PMYAPP_RELEASE_STORE_PASSWORD=android \
  -PMYAPP_RELEASE_KEY_ALIAS=androiddebugkey -PMYAPP_RELEASE_KEY_PASSWORD=android
```

The release bundle embeds whatever JS is already in `.build/native/commonjs` (the Fable watch output, compiled with `--define DEBUG`), so `FakeTodoService` (`#if DEBUG`) stays in and the standalone app has in-memory todos with no backend. `configSourceOverrides.native.js` `AppUrlBase = 10.0.2.2:8081` is emulator-only and unreachable on a phone, but the in-memory todo UI does not need it.

**3. Install + launch on the phone (target it explicitly with `-s`):**

```bash
adb -s <phone-ip>:<CONNECT_PORT> install -r app/build/outputs/apk/release/app-release.apk
adb -s <phone-ip>:<CONNECT_PORT> shell am start -n com.eggshell.apptodo/.MainActivity
```

`versionCode` is still `1`; keep signing with the same key so installs update in place.

**Caveat: raw `adb` cannot reliably drive RN gestures on this build.** Synthetic `adb shell input tap` on a control inside a swipe `GestureView` is swallowed (the GestureView claims the responder on touch-start; only real touches negotiate to the child), and `adb shell input text` sets the native field value without firing RN `onChangeText`. Use real touch or the Appium `observe` harness ([Audit toolkit](./runbooks/audit-toolkit.md)) for gesture/text interactions.

---

## Release build to judge performance {#release-perf-build}

**Never judge performance on the dev/debug build.** Dev is slow for reasons that vanish in release: Metro **lazy bundling** (first interactions stream modules -- e.g. the theme toggle lags then smooths out), per-frame `deepFreezeAndThrowOnMutationInDev` on every native call (so JS-driven animations like `translateX.SetValue` pay a freeze each frame), unminified JS, and no Hermes bytecode precompile. A release build removes all of these.

Fastest way to build + install a release variant on the connected device, signed with the debug keystore (throwaway) and only the device's ABI:

```bash
cd SuiteTodo/AppTodo
export DOTNET_ROOT="$HOME/.dotnet"
export ANDROID_HOME="$HOME/Library/Android/sdk"; export ANDROID_SDK_ROOT="$ANDROID_HOME"
export PATH="$ANDROID_HOME/platform-tools:$PATH"; export ANDROID_SERIAL=<device-serial>
# sign the throwaway release with the debug keystore via ORG_GRADLE_PROJECT_* env vars
export ORG_GRADLE_PROJECT_MYAPP_RELEASE_STORE_FILE=debug.keystore
export ORG_GRADLE_PROJECT_MYAPP_RELEASE_STORE_PASSWORD=android
export ORG_GRADLE_PROJECT_MYAPP_RELEASE_KEY_ALIAS=androiddebugkey
export ORG_GRADLE_PROJECT_MYAPP_RELEASE_KEY_PASSWORD=android
npx react-native run-android --mode release --active-arch-only
```

This produces a **standalone** APK (JS bundled + minified, no Metro, no live reload -- rebuild to see changes). It still embeds the `--define DEBUG` Fable output, so `FakeTodoService` and in-memory todos stay in (no backend needed). `--active-arch-only` keeps it to one ABI (smaller, avoids the `/data` full-storage install failure).

---

## Android-specific gotchas {#gotchas}

| Symptom | Cause | Fix |
|---|---|---|
| White/blank inputs in dark mode | Framework inputs used to hardcode `Color.White`; not applied per appearance | Apply the input theme per appearance (themeable bg + label). |
| `backgroundColor` + `borderRadius` renders square corners | Missing `Overflow.Hidden` on the filled view | Add `Overflow.Hidden` to filled rounded views. |
| Runtime crash `Color is expected to match #[0-oa-f]{6}` | `Color.Hex` requires lowercase hex; uppercase throws at runtime even though the build is green | Lowercase all hex strings passed to `Color.Hex`. |
| `adb` / `run-android` fails | DOTNET_ROOT not set | `export DOTNET_ROOT="$HOME/.dotnet"` before any dotnet/fable/eggshell command. |
| `libhermes_executor.so not found` at startup (RN 0.76) | `SoLoader.init(this, false)` in `MainApplication.kt` | Use `SoLoader.init(this, OpenSourceMergedSoMapping)`. **On RN 0.86** `MainApplication.kt` no longer calls `SoLoader.init` at all -- `loadReactNative(this)` handles it; see [RN 0.86 upgrade](./runbooks/troubleshooting.md#rn86-upgrade). |

For a complete catalog of build, styling, and layout gotchas, see [Troubleshooting](./runbooks/troubleshooting.md).
