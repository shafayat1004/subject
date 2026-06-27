# AppEggShellGallery

Documentation and component gallery for EggShell. Hosted at https://eggshell.dev/.

## Requirements

* [.NET SDK](https://dotnet.microsoft.com/download) (6.0+; repo tooling targets modern SDKs)
* [Node.js](https://nodejs.org/) with npm (LTS recommended)
* An F# editor: VS Code + Ionide, JetBrains Rider, or Visual Studio
* **Web only:** the above is enough
* **Native Android:** Android SDK, emulator or device, `adb`
* **Native iOS (macOS only):** Xcode, CocoaPods (`pod`), iOS simulator runtime matching the Xcode SDK

## First-time repo setup

From the **repository root** (not this directory):

```bash
./initialize
```

This builds and npm-links the `eggshell` CLI, initializes framework libs, and compiles core libraries.

Add the repo root to your `PATH` if you have not already (so `eggshell` is available everywhere):

```bash
export PATH="/path/to/subject:$PATH"
```

Optional: enable tab completions:

```bash
source /path/to/subject/Meta/AppEggshellCli/bash.completions
```

Then initialize the gallery app:

```bash
cd AppEggShellGallery
./initialize
```

`./initialize` installs npm deps, creates the Android debug keystore if missing, runs `pod install` when `ios/` exists, symlinks `public-dev/images`, and creates `configSourceOverrides.native.js` from the template when that file is missing (the file is gitignored and holds local dev overrides).

## Web development

```bash
cd AppEggShellGallery
eggshell dev-web
```

Open **http://127.0.0.1:8082/** (this app uses port 8082; newly scaffolded apps default to 9080).

F# and `.render` changes reload automatically. If the watcher misses new or deleted files after a git checkout, stop and restart `eggshell dev-web`.

**Mac stack overflow:** if Fable dies with `Stack overflow` during `dev-web`, try `export COMPlus_DefaultStackSize=180000` first (see docs in `public-dev/docs/basics/dev-experience.md`).

## Native development

Native apps need **three** pieces running together:

| Terminal | Command | Purpose |
|----------|---------|---------|
| 1 | `../eggshell dev-native` | Fable watch → `.build/native/commonjs` |
| 2 | `npx react-native start --port 8081` | Metro bundler |
| 3 | Platform launch (below) | Install/run on emulator |

### `configSourceOverrides.native.js`

Required for native dev. Created automatically by `./initialize` from `configSourceOverrides.native.js.template`. Edit only if you need custom API keys or URLs.

The template sets:

* **AppUrlBase** — Metro URL seen from the emulator/simulator (`10.0.2.2:8081` on Android emulator, `localhost:8081` on iOS simulator)
* **MaybeInBundleResourceUrlHashedDirectoryPrefix** — `/public-dev` so Docs/Components/Tools markdown loads from Metro (same files webpack serves in dev-web)

### Android

```bash
# once per machine session (or after emulator restart)
adb reverse tcp:8081 tcp:8081

cd AppEggShellGallery
npx react-native run-android
```

Or build only: `cd android && ./gradlew assembleDebug`

Success signal in logcat: `Running "RXApp"`.

### iOS (macOS)

```bash
cd AppEggShellGallery/ios
pod install   # first time and after Podfile / native dep changes

cd ..
npx react-native run-ios --no-packager
```

Open **`ios/AppEggshellGallery.xcworkspace`** in Xcode, not the `.xcodeproj`.

### When native misbehaves

* Restart Metro with `--reset-cache` after large dependency or `metro.config.js` changes
* Android: clear app storage (uninstall alone may not clear cache)
* Delete `.build/native` and run `eggshell build-native`, then restart Metro
* Flipper is **not** bundled with RN 0.76; use React Native DevTools or `npx react-native start --experimental-debugger` instead

More detail: `public-dev/docs/basics/native.md` (also shown in the in-app Docs section after dev-web).

## Scraping component props

Component property tables on gallery pages come from `src/ScrapedData.fs`, generated manually:

```bash
cd AppEggShellGallery/Scraping
dotnet run
```

Commit updated `ScrapedData.fs` when component APIs change.

## Releasing

Pushes to `releases/gallery` trigger deployment to https://eggshell.dev/:

```bash
git checkout main && git pull
git checkout releases/gallery && git merge main && git push
```

## Troubleshooting compile failures

1. Re-run `./initialize` at repo root, then `AppEggShellGallery/./initialize`
2. Re-run `eggshell dev-web` or `eggshell build-native` as appropriate
3. See `LEARNINGS.md` at repo root for recent toolchain gotchas
