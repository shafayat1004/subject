# Native app development experience

This page supplements [Native Development](./basics/native.md) with day-to-day notes.

## Standard workflow (RN 0.76)

From your app directory:

1. **`eggshell dev-native`** — Fable watch → `.build/native/commonjs`
2. **`npx react-native start --port 8081`** — Metro
3. **Launch the app** — `npx react-native run-android` or `run-ios --no-packager`

Android emulator: run **`adb reverse tcp:8081 tcp:8081`** each time the emulator starts.

Ensure **`configSourceOverrides.native.js`** exists (from `./initialize`). Without it, native bootstrap fails on missing `AppUrlBase`.

## Markdown and static assets in dev

Web dev-webpack serves `public-dev/` at the site root. Native fetches the same files from Metro at **`/public-dev/...`**. The native config template sets `MaybeInBundleResourceUrlHashedDirectoryPrefix = "/public-dev"`.

Image assets are copied to `.build/native/assets/public-dev/images/` during native builds (`copyStaticFiles`); markdown lives under the app's `public-dev/docs/` and is served directly by Metro.

## Android hardware back button

`<LR.NativeBackButton>` must be in the component tree (usually in `AppContext`). Duplicate `LR.Router` instances break back.

Some emulator images have non-functional hardware buttons; try a different AVD if back never fires.

## When the app will not cooperate

* Clear Android app **storage** (not just uninstall)
* Remove `.build/native`, run `eggshell build-native`, restart Metro with `--reset-cache`
* Re-run `./initialize` in the app if npm links break

## Subject stack in the emulator

You may need to comment out `cookieOptions.Secure <- true` in `LibLifeCycleHost/src/Web/Session.fs` for sessions to stick — local dev only, never commit.

## Debugging

Flipper is not enabled out of the box on RN 0.76. Use React Native DevTools or:

```shell
npx react-native start --experimental-debugger
```
