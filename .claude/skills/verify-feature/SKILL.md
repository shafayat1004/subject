---
name: verify-feature
description: Prove a feature or page actually works end to end on android, ios, and/or web, with fresh-bundle proof, driver-based navigation, screenshots, and bucketed log capture. Use whenever asked to verify, test, or demo a feature/page, and before claiming any UI change is done. Verification means evidence, not "it compiled".
user-invocable: true
argument-hint: "{android|ios|web} [route or deeplink]"
---

# verify-feature

Verify matrix: portrait + landscape, on every platform the change touches. "Compiles" is not
verified; a screenshot of the working feature is.

## Flow

1. **Fresh bundle proof:** invoke the fable-rebuild-verify skill for the touched project(s).
2. **Preflight the platform:** debug-android / debug-ios / debug-web skill preflight scripts.
3. **Route to the target:**
   - Web: open `http://localhost:<port>/<route>` directly (pathname routing).
   - Android: `adb shell am start -n <package>/.MainActivity` then navigate, or deep-link URL if
     the app registers a scheme.
   - iOS: `ios-observe.sh launch <bundleid>` / `ios-observe.sh openurl <url>`.
4. **Interact deterministically (Tier 2 first):** web via Playwright toolkit
   (`npm run audit:web`, `npm run observe -- snapshot -p web`); Android via Appium driver
   (`AppEggShellGallery/audit-gallery-android-driver.mjs`, Appium :4723,
   `npm run observe -- doctor` to check the chain). Raw coordinate taps
   (`android-observe.sh tap`) only as last resort.
5. **Capture:** `scripts/capture-session.sh {android|ios} [--url <deeplink>] [--duration N]`
   records logs for the whole window + before/after screenshots. Web: observe snapshot captures
   screenshot + console + health.
6. **Analyse:** the capture summary buckets FATAL EXCEPTION / Uncaught / ReactNativeJS / error. Zero
   fatals and the expected UI in screenshots = PASS. Anything else: report evidence, do not claim done.
7. Repeat for landscape (`android-observe.sh rotate landscape`, or observe --orientation).

## Reporting

State PASS/FAIL per platform x orientation with screenshot paths and the log summary. Failures go
to the engineering log via the docs-sync skill.

## Doc refs

- runbooks/audit-toolkit.md (two-tier observation model)
- runbooks/android.md, runbooks/ios.md, runbooks/web.md
- runbooks/dev-loop.md

(All under AppEggShellGallery/public-dev/docs/.)
