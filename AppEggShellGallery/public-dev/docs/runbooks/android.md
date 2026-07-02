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

## Android-specific gotchas {#gotchas}

| Symptom | Cause | Fix |
|---|---|---|
| White/blank inputs in dark mode | Framework inputs used to hardcode `Color.White`; not applied per appearance | Apply the input theme per appearance (themeable bg + label). |
| `backgroundColor` + `borderRadius` renders square corners | Missing `Overflow.Hidden` on the filled view | Add `Overflow.Hidden` to filled rounded views. |
| Runtime crash `Color is expected to match #[0-oa-f]{6}` | `Color.Hex` requires lowercase hex; uppercase throws at runtime even though the build is green | Lowercase all hex strings passed to `Color.Hex`. |
| `adb` / `run-android` fails | DOTNET_ROOT not set | `export DOTNET_ROOT="$HOME/.dotnet"` before any dotnet/fable/eggshell command. |
| `libhermes_executor.so not found` at startup | `SoLoader.init(this, false)` in `MainApplication.kt` (RN 0.76+) | Use `SoLoader.init(this, OpenSourceMergedSoMapping)`. |

For a complete catalog of build, styling, and layout gotchas, see [Troubleshooting](./runbooks/troubleshooting.md).
