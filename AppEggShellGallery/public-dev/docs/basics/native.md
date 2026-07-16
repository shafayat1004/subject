# Native Development

Native targets **React Native 0.76** with Fable output in `.build/native/commonjs`. Metro serves the bundle; the native shell loads it from the dev server in debug builds.

## Prerequisites

* Completed [Getting Started](./basics/getting-started.md) (`./initialize` at repo root)
* App `./initialize` (npm install, Android keystore, optional `pod install`)
* **`configSourceOverrides.native.js`** in the app directory (gitignored; created from `configSourceOverrides.native.js.template` by `./initialize`)

The template configures:

| Setting | Purpose |
|---------|---------|
| `AppUrlBase` | Metro host as seen from the device (`10.0.2.2:8081` Android emulator, `localhost:8081` iOS simulator) |
| `MaybeInBundleResourceUrlHashedDirectoryPrefix` | `/public-dev` — markdown and static docs under `public-dev/` (gallery Docs/Components/Tools pages) |
| `BackendUrl` | Optional backend for Subject demos (`10.0.2.2:5000` / `localhost:5000`) |

## Day-to-day workflow

Use **three terminals** from the app directory (e.g. `AppEggShellGallery`):

**Terminal 1 — Fable watch**

```bash
eggshell dev-native
```

Compiles F# and `.render` files to `.build/native/commonjs`.

**Terminal 2 — Metro**

```bash
npx react-native start --port 8081
```

After changing `metro.config.js` or native npm deps, restart with `--reset-cache`.

**Terminal 3 — Run on device**

Android (emulator must be running):

```bash
adb reverse tcp:8081 tcp:8081
npx react-native run-android
```

iOS (macOS only):

```bash
npx react-native run-ios --no-packager
```

First-time / Podfile changes:

```bash
cd ios && pod install && cd ..
```

Open **`ios/*.xcworkspace`** in Xcode, not `.xcodeproj`.

### Verify it worked

* Logcat / Metro shows `Running "RXApp"`
* Gallery: tap **Components** in the top nav (or sidebar on handheld) — should load markdown, not crash
* If markdown pages are blank, check `configSourceOverrides.native.js` has `/public-dev` prefix and Metro serves `http://127.0.0.1:8081/public-dev/docs/...`

## First-time platform setup

See [Native getting started](./native/getting-started.md) for Android Studio / Xcode install links.

**Android:** create or start an AVD (API 33+ recommended). `./gradlew assembleDebug` in `android/` should succeed.

**iOS:** install CocoaPods. Podfile uses static Firebase frameworks; run `pod install` in `ios/`. Simulator needs an iOS runtime matching your Xcode SDK.

## One-shot commands (legacy)

Some docs still mention:

```bash
dotnet fsi ./build.fs build -t EggShell --command="dev-android"
```

Prefer the three-terminal workflow above with `eggshell dev-native` + Metro + `run-android` / `run-ios`. It matches RN 0.76 and current gallery validation.

## Metro reload quirks

Metro does not always pick up changes in precompiled `.js` under `.build/native`. If the app looks stale after a native compile, reload the app (R R in emulator) or restart Metro.

## Android hardware back button

`<LR.NativeBackButton>` must appear once in the tree (typically in `AppContext`, under the router). Two `LR.Router` instances break back navigation.

Some AVD images show hardware buttons that do not work; try a Pixel-class API 30+ image if back appears dead.

## When the app misbehaves

* Android: App info → Storage → **Clear storage** (uninstall may not clear everything)
* Delete `.build/native`, run `eggshell build-native`, restart Metro
* `adb reverse tcp:8081 tcp:8081` after emulator restart

## Releasing

### Android APK

```bash
cd android && ./gradlew assembleRelease
```

### iOS archive

Ensure Xcode build scripts find Node (e.g. `ln -s $(which node) /usr/local/bin/node` if needed).

## Emulator and Subject backend

When testing against a local Subject backend in the emulator, you may need to disable secure cookies in `LibLifeCycleHost/src/Web/Session.fs` (`cookieOptions.Secure <- true`) — **do not commit** that change.

## Debugging

RN 0.76 does not ship Flipper integration. Use:

* React Native DevTools (press `j` in the Metro terminal when offered)
* `npx react-native start --experimental-debugger`

More notes: [Native dev experience](./native/dev-experience.md).
