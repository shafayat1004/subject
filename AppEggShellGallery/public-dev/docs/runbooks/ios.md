# iOS Runbook

Full step-by-step runbook for booting an iOS simulator, launching and reloading the app, capturing screenshots, rotating, reading logs, and dealing with iOS-specific gotchas.

iOS shares the same `dev-native` + Metro as Android. No `adb reverse` is needed; the simulator uses `localhost:8081`.

Examples use AppEggShellGallery (`com.eggshell.appgallery`). Substitute your app's bundle ID and simulator name.

Related: [Dev loop](./dev-loop.md) | [Android](./android.md) | [Build and rebuild](./build-rebuild.md) | [Troubleshooting](./troubleshooting.md)

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
   No new dependency. See [Audit toolkit](./audit-toolkit.md).
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

For a complete catalog of build, styling, and layout gotchas, see [Troubleshooting](./troubleshooting.md).

---

## Release archive and IPA export {#release-ipa}

Produce a signed Release IPA for installation on a provisioned device (not the simulator and not
TestFlight/App Store). Three deterministic stages. The preflight script is the gate for stages 1-2.

### Signing setup (per developer, one-time) {#signing}

`DEVELOPMENT_TEAM` is **never committed** (a personal team id must not land in a shared/public repo,
and Xcode re-dirties the pbxproj on every open). Signing is layered through xcconfig:

- `ios/Config.xcconfig` -- **committed**, carries no identity. Set as the project-level
  `baseConfigurationReference` (target-level base configs stay owned by CocoaPods, so `pod install`
  never warns). It only does `#include? "Local.xcconfig"`.
- `ios/Local.xcconfig` -- **git-ignored** (`ios/**/Local.xcconfig`), per developer. Create it once
  per clone:

  ```
  DEVELOPMENT_TEAM = <your 10-char Apple team id>
  ```

  Find the id in Xcode -> Settings -> Accounts, or `security find-identity -v -p codesigning`.
  Without it the target has no team (correct fresh-clone state); set it in the Xcode UI or this file.

Deployment target is **iOS 18.0** (pbxproj `IPHONEOS_DEPLOYMENT_TARGET` and Podfile `platform :ios`).

Verify the wiring resolves: `xcodebuild -project ios/<App>.xcodeproj -target <App> -configuration
Release -showBuildSettings | grep DEVELOPMENT_TEAM` should print your team.

**Adding iOS to a new app.** The scaffold (`eggshell create-app`) does not generate an `ios/` tree;
add it by hand, then apply the signing hygiene in one shot (idempotent):

```bash
Meta/LibScaffolding/scripts/wire-ios-signing.pl <app>/ios/<App>.xcodeproj/project.pbxproj
```

It strips any hardcoded `DEVELOPMENT_TEAM`, normalizes `IPHONEOS_DEPLOYMENT_TARGET` to 18.0, and
creates + wires `ios/Config.xcconfig`. The scaffold `.gitignore.template` already ignores
`ios/**/Local.xcconfig` and `*.mobileprovision`, so a new app is protected by default.

### Stage 1: preflight (run before archiving)

```bash
.claude/skills/release-build/scripts/ios-preflight.sh <appdir>
```

Each check prints `PASS`/`FAIL`/`WARN` with the exact value it tested; any `FAIL` exits non-zero
with an actionable fix. Do not archive until it is green. Checks: workspace + Pods, scheme (prefers
the scheme matching the app stem), `PRODUCT_BUNDLE_IDENTIFIER` (pbxproj) + `Config.xcconfig` present
+ `DEVELOPMENT_TEAM` (from `ios/Local.xcconfig`, see [Signing setup](#signing)), keychain
`Apple Development` identity, the team is logged into Xcode Accounts, a cached provisioning profile
for `<TEAM>.<BUNDLE_ID>` with the target device listed, Podfile disables pod-target signing, the
Fable native bundle is fresh, and two advisories (`__DEV__` config gating, the home-screen display
name).

### Stage 2: archive in Xcode UI

The first archive (and any archive after a signing change) MUST be done in Xcode, not `xcodebuild`
CLI. Two reasons specific to a free personal team:

1. `-allowProvisioningUpdates` generates a provisioning profile from the CLI but NOT a signing
   certificate; the certificate is generated on first archive inside the Xcode UI session.
2. The CLI `codesign` of embedded frameworks (`[CP] Embed Pods Frameworks`) hits
   `errSecInternalComponent` because it cannot unlock the login keychain non-interactively; the
   Xcode UI session holds that unlock.

Steps: open `<appdir>/ios/<App>.xcworkspace`, set the preflight's scheme, set destination to
**Any iOS Device (arm64)**, **Product -> Archive**. The `.xcarchive` lands under
`~/Library/Developer/Xcode/Archives/<date>/`.

### Stage 3: wrap the signed app into an IPA (do not exportArchive)

`xcodebuild -exportArchive` re-signs embedded frameworks and hits the same `errSecInternalComponent`
as stage 2. A Development IPA is just a signed `.app` inside a `Payload/` zip, so wrap the
already-signed app from the archive directly (no re-signing, no App Store / "release testing" gate):

```bash
.claude/skills/release-build/scripts/release-build.sh ios <appdir>
```

The script picks the newest `.xcarchive` for the app (matched by bundle id, ordered by file mtime so
several same-day archives resolve to the true latest -- the archive filename's 12h clock time sorts
wrong lexically), verifies it is signed + has `main.jsbundle` + `embedded.mobileprovision`, and writes
`<bundleid>.ipa` to `<appdir>/dist/ios/`.

Install from `<appdir>/dist/ios/` -- **never** from a scratch/`/tmp` copy: tmp is reaped on reboot or
by the next build, and a sideloader (SideStore, LiveContainer) that imported it by path then reports
"IPA does not exist." Drag the `dist/ios/*.ipa` into Xcode -> Window -> Devices and Simulators -> the
iPhone, or `xcrun devicectl device install app --device <UDID> <ipa>`, then on the phone trust the team
under Settings -> General -> VPN & Device Management.

### Why not the simulator build?

`xcodebuild ... -sdk iphonesimulator build` only builds for the simulator and is a fast smoke
(`SKILL.md` used to point here). It does not produce a device-installable artifact and does not
exercise signing. Use it only for a quick "does it compile for iOS" check, never for a release IPA.
