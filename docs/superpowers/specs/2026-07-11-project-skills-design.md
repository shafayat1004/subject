# Project Skills + Formatting Burn-in — Design

Date: 2026-07-11. Branch: modernization/rnw. Approved approach from brainstorming session.

## Goal

Package the recurring workflows discovered during the RNW modernization (engineering log sessions
6-14, main..modernization/rnw history) into project-scoped Claude skills under `.claude/skills/`,
each with helper scripts where automation pays off. Additionally, make the F# formatting
conventions impossible to miss for any model working in this repo.

## Principles

- **Skills are thin procedure wrappers.** The gallery docs (`AppEggShellGallery/public-dev/docs/`)
  stay the single source of truth. A skill carries: trigger description (frontmatter), command
  skeleton, decision tree, validation gates, and pointers into the docs. No wholesale runbook
  duplication.
- **Scripts live with their skill** at `.claude/skills/<name>/scripts/`, executable, zsh/node,
  no new dependencies beyond what the repo already uses (node, adb, xcrun, rg).
- **Heuristic scanners are advisory.** Regex-based .fs scanners (style leaks, a11y) report
  candidates for the model to judge; they do not gate builds.
- **Skills supplement the runbooks with expert knowledge.** Where a runbook is silent (release
  builds, e2e capture flows, platform gotchas), the skill encodes best-practice commands directly
  so weaker models don't improvise; docs-sync then backfills the runbook so docs stay canonical.

## Skills

### 1. fable-rebuild-verify
Kill stale-cache false greens. 3-tier escalation: (1) `touch` changed `.fs`; (2) wipe
`.build/<platform>/fable`; (3) kill watch, restart, Metro `--reset-cache`. Proof gate before
"done": log shows `Started Fable compilation...`, `rg "error FS"` clean, emitted `.js` newer than
edited `.fs`.
- **Wrong-invocation guard:** never run `dotnet fable` / `dotnet fable watch` directly against a
  project — it emits `.fs.js` beside the source files. Always build via `./eggshell build-lib` /
  `eggshell dev-*`, which target `.build/<platform>/fable`. The skill states this up front.
- Script `scripts/clean-stray-fable-output.sh [--check]`: finds `.fs.js` (and `.fs.js.map`)
  sitting beside `.fs` sources outside `.build/`; `--check` reports, default deletes. Run when a
  prior session polluted the tree (symptoms: stale code paths, duplicate-module confusion).
- Script `scripts/rebuild-verify.sh <libdir> <platform> [changed.fs...]`: runs the tier sequence,
  prints PASS/FAIL with evidence (timestamps, log lines); also runs the stray-output check.
- Refs: `runbooks/build-rebuild.md`, `runbooks/troubleshooting.md#build-cache`.

### 2. debug-android
Preflight + observe loop for Android device/emulator work.
- Script `scripts/android-preflight.sh`: `adb devices` check, `adb reverse tcp:8081 tcp:8081`,
  Metro reachability, prints device state.
- Script `scripts/android-observe.sh {screenshot|logcat|tap X Y|rotate}`: screenshot to scratchpad,
  logcat parsed into fatal/ReactNativeJS/warn buckets.
- Decision tree: Tier 1 raw adb (fast, brittle) vs Tier 2 Appium audit scripts
  (`AppEggShellGallery/audit-gallery-*.mjs`) — escalate to Tier 2 when taps must be deterministic.
- Refs: `runbooks/android.md`, `runbooks/dev-loop.md`, `runbooks/audit-toolkit.md`.

### 3. debug-ios
Same shape for iOS: simulator boot (`xcrun simctl`), install/launch, screenshot, log stream
predicate for ReactNativeJS + crash, physical-device notes.
- Script `scripts/ios-preflight.sh`, `scripts/ios-observe.sh {screenshot|log|launch}`.
- Refs: `runbooks/ios.md`, `runbooks/dev-loop.md`.

### 4. debug-web
dev-web launch + browser observation: `./eggshell dev-web`, Playwright-based audit scripts,
console error read, "did my patch reach the bundle" check.
- Script `scripts/web-preflight.sh`: dev server up? port? bundle fresh vs source timestamps.
- Refs: `runbooks/web.md`, `runbooks/audit-toolkit.md`.

### 5. style-leak-audit
For NEW or edited components and app pages (conversions are done). Checks:
`makeViewStyles`/`makeTextStyles` call context (top-level `let` OK; render body / `Pointer.State` /
`With.ScreenSize` callback = leak), `ViewStyles.Memoize`/`TextStyles.Memoize` keyed on records
(cache-defeating), memoized lambda params colliding with CE ops (`height`, `color`, `fontSize`,
`top`, `left`, `bottom`).
- Script `scripts/scan-style-leaks.mjs <file|dir>`: reports file:line + category; advisory.
- Refs: `fsharp/styling.md` (Avoiding style leaks), `runbooks/troubleshooting.md`.

### 6. a11y-check
For NEW or edited components and app pages. Archetype checklist (interactive / input / leaf):
label, role, state, stable `testId`, live regions, decorative icons hidden, ≥44px targets,
non-gesture alternative, text scaling. Runtime verify via `window.__eggshell.<App>.uiSnapshot()`.
- Script `scripts/scan-a11y.mjs <file|dir>`: flags `Pressable`/inputs missing label/testId,
  icon-only pressables; advisory.
- Refs: `accessibility/recipes.md`, `accessibility/index.md`,
  `modernization/render-dsl-retirement.md` (a11y bar table).

### 7. docs-sync
Run after any code change, before commit. Update-matrix routing (change type → doc list from
`maintaining-docs.md`), engineering-log entry template (dated, prepended), status-page agreement
check (modernization/index.md vs goals-and-roadmap.md vs phased-plan.md vs reality), stale-framing
scan, broken-link check, `llms.txt` sync on page add/rename.
- Script `scripts/check-doc-links.mjs`: verifies all relative `.md` links in docs root resolve
  (productionizes the Python snippet in maintaining-docs.md).
- Script `scripts/scan-stale-framings.sh`: rg for present-tense `ReactXP`/`RX.`/Fable 4/old
  versions in docs.
- Refs: `maintaining-docs.md`, `knowledge-base/engineering-log.md`.

### 8. gallery-page-add
Scaffold the 5-file gallery page boilerplate: Content route `.fs`, `Components.fs` registration,
`SidebarContent.fs` nav entry, `Navigation.fs` route case, `.fsproj` compile entry. Pure F# only
(no render DSL). Ends with compile check.
- Script `scripts/scaffold-gallery-page.mjs <PageName>`: generates the `.fs` from a template
  modeled on an existing pure-F# Content page and prints the 4 registration edits (edits stay
  manual: registration files are load-bearing and order-sensitive).
- Refs: existing `AppEggShellGallery/src/Components/Content/` pure-F# pages, CLAUDE.md rule 10.

### 9. release-build
Produce and sanity-check release artifacts per platform. One skill, platform-routed:
- **Android:** `./gradlew :app:assembleRelease` (or `bundleRelease` for AAB) from the app's
  `android/`; verify signing config present; install release APK on device
  (`adb install -r`) and boot it — release surfaces bugs debug masks (bundler resolution,
  missing `extraNodeModules` for LibClient-only deps, missing crypto polyfill, workletization).
  Checklist of known release-only gaps from engineering log session 10.
- **iOS:** `xcodebuild -workspace ... -scheme ... -configuration Release archive` (or
  Release-configuration build to simulator for a fast pass); pods installed; bundle embedded
  (not Metro-served) — verify app runs with Metro OFF.
- **Web:** production bundle via the eggshell/webpack release path (`Web Release` config);
  verify output size sane, no dev-server references, page boots from static serve.
- Script `scripts/release-build.sh {android|ios|web} <appdir>`: runs the platform sequence,
  prints artifact path + smoke-check results.
- Refs: `runbooks/build-rebuild.md`, `runbooks/troubleshooting.md#rn86-upgrade`,
  `knowledge-base/engineering-log.md` (session 10 release-gap findings).

### 10. verify-feature
End-to-end "prove the feature/page works" loop combining CLI + automation drivers. Flow:
1. Ensure fresh bundle (invoke fable-rebuild-verify).
2. Route to the target page: deep link (`adb shell am start` with URL / `xcrun simctl openurl`)
   or driver navigation (Appium for native via `audit-gallery-android-driver.mjs` patterns,
   Playwright for web).
3. Interact via stable `testId`s (Tier 2), fall back to coordinates only when unavoidable.
4. Capture: screenshots at each step (adb screencap / simctl io screenshot / Playwright
   screenshot), plus log capture running for the whole session (`adb logcat` filtered to
   ReactNativeJS+fatal; `xcrun simctl spawn ... log stream` predicate; browser console via
   Playwright).
5. Analyse: bucket logs (fatal / error / warn / probe output), diff screenshots against
   expectation, report PASS/FAIL with evidence.
- Script `scripts/capture-session.sh {android|ios} [--url <deeplink>]`: starts log capture to
  scratchpad, optionally deep-links, takes before/after screenshots, stops capture, prints
  bucketed summary.
- Web path leans on existing `audit-gallery-*.mjs` Playwright toolkit.
- Portrait+landscape × platform matrix per the verify-matrix convention.
- Refs: `runbooks/audit-toolkit.md`, `runbooks/android.md`, `runbooks/ios.md`,
  `runbooks/web.md`, existing `AppEggShellGallery/audit-*.mjs`.

## Cross-wiring

- debug-android/ios/web invoke fable-rebuild-verify whenever "did my patch reach the bundle"
  arises.
- docs-sync checklist references style-leak-audit + a11y-check for component-touching changes.
- All skills open with "read the runbook/doc refs before improvising" per CLAUDE.md rule 11.

## Formatting burn-in

`AppEggShellGallery/public-dev/docs/fsharp/formatting.md` (766 lines, Fantomas 6.x + hand-held
alignment rules) is canonical. Burn-in mechanism, in priority order:

1. **CLAUDE.md block** (always in context for any model): new "F# formatting (mandatory)"
   section, ~12 lines distilling the hard rules — 4-space indent no tabs, column alignment
   conventions (2a-2d), line length, match/DU/record layout pointers — plus: read
   `fsharp/formatting.md` before non-trivial F# edits; after editing run
   `dotnet tool run fantomas <file>`; alignment rules are NOT Fantomas-enforced so match
   surrounding code by eye. Also includes the build-invocation guard: never `dotnet fable`
   directly (emits `.fs.js` beside sources); use `./eggshell build-lib` / `eggshell dev-*`.
2. **Distillation accuracy**: the block is distilled from the doc, listing rule numbers so a model
   can jump to the full rule.

No hook needed: CLAUDE.md is injected every session for every model already.

## Out of scope

- convert-component skill (conversions complete per user).
- rn-upgrade-preflight (upgrade tail is small).
- Fantomas/EggShellFmt tooling changes (formatting.md section 16d — separate effort).

## Validation

- Each script runs against the live repo with a known-good and known-bad input where feasible
  (e.g. style-leak scanner must flag a deliberate inline `makeViewStyles` and pass a clean file).
- Skills reviewed against superpowers:writing-skills guidance (frontmatter triggers, terse body).
- docs-sync applied to this change itself: engineering-log entry + maintaining-docs routing.
