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
