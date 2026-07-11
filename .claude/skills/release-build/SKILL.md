---
name: release-build
description: Produce and smoke-check a RELEASE build for android, ios, or web. Use when asked for a release/production build, before shipping, or when a bug reproduces only outside the dev loop. Release surfaces failures debug masks (bundler resolution, missing deps, polyfills), so verify features against release when touching native deps.
user-invocable: true
argument-hint: "{android|ios|web} <appdir>"
---

# release-build

`scripts/release-build.sh {android|ios|web} <appdir> [--install]`

## Android

`cd <appdir>/android && ./gradlew assembleRelease` with the repo's debug-keystore params (see
script). Artifact: `<appdir>/android/app/build/outputs/apk/release/app-release.apk`. The release
APK embeds the JS bundle from `.build/native/commonjs` and runs WITHOUT Metro; `--install` does
`adb install -r` + launches. Ensure the Fable native build is fresh first (fable-rebuild-verify).

## Release-only failure modes (engineering log, session 10). Check these when release breaks and debug works:

1. LibClient-only deps not resolvable in release bundle -> map them in the app's
   `metro.config.js` `extraNodeModules` (e.g. @react-native-picker/picker, async-storage).
2. Reanimated/worklets live only in LibClient/node_modules -> add as direct app deps
   (+ @babel/core + babel.config.js).
3. Local images crash RCTImageView ("Value for uri cannot be cast from Double to String") ->
   numeric require() assets must pass as bare RN source, not {uri}.
4. `ReferenceError: Property 'crypto' doesn't exist` -> add react-native-get-random-values,
   import it FIRST in index.js.

## iOS

`xcodebuild -workspace <App>.xcworkspace -scheme <scheme> -configuration Release
-sdk iphonesimulator build` (fast pass; the script auto-detects the scheme via
`xcodebuild -list`). Pods must be installed. Smoke check: install + launch on the booted
simulator with Metro STOPPED; the app must boot from the embedded bundle.

## Web

`eggshell package-web` from the app dir is the eggshell web release command: it runs `build-lib`
for the web target, runs Fable in `package` mode (plain `webpack`, NOT `webpack-dev-server`; this
is the production bundle path, mirrored from `Meta/LibFablePlus/src/index.ts`), then packages the
`dist-template` into a standalone dist (server.js, package.json, run.sh) via
`Meta/LibRtCompilerFileSystemBindings/src/packageApp.ts`. Run it as
`(cd <appdir> && node <repo-root>/eggshell package-web)` (no dedicated CLI flag takes an appdir;
the CLI resolves the project from cwd). Smoke check: serve the output statically, page boots, no
dev-server references, no console errors.

## Doc refs

- runbooks/android.md (release APK section)
- runbooks/ios.md
- knowledge-base/engineering-log.md (session 10, release-gap findings)

(All under AppEggShellGallery/public-dev/docs/.)
