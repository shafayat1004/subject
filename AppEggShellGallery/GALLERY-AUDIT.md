# EggShell Gallery — Web Audit Guide

Automated Playwright audits for **AppEggShellGallery** on web. Exercises component demo pages, captures console errors, runs post-interaction checks, and archives screenshots for human/AI aesthetic review.

**Scope:** web gallery at `http://127.0.0.1:8082` (local dev-web). Native (Metro/iOS/Android) is not driven by these scripts yet; use logcat/Metro console separately.

---

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| **Node.js** | ≥ 18.19 (see `AppEggShellGallery/package.json` `engines`) |
| **eggshell CLI** | Repo root: `./eggshell` must work |
| **Gallery web build** | Fable + webpack dev server; router autogen must exist |
| **Playwright** | Node package + Chromium browser (see Setup) |
| **macOS / Linux / WSL** | Headed mode opens a real browser window |

Optional for comparing against production:

- Network access to `https://eggshell.dev`

---

## One-time setup

### 1. Install Node dependencies (gallery)

From the repo root:

```bash
cd AppEggShellGallery
npm install
```

### 2. Install Playwright

The audit scripts import `playwright` but it is not yet listed in `package.json`. Install it locally in the gallery app:

```bash
cd AppEggShellGallery
npm install -D playwright
npx playwright install chromium
```

Verify:

```bash
node -e "import('playwright').then(() => console.log('playwright ok'))"
```

### 3. Build / start the gallery (web)

In a **dedicated terminal**, from the repo root:

```bash
cd AppEggShellGallery
../eggshell dev-web
```

Wait until webpack reports:

```text
Loopback: http://127.0.0.1:8082/
```

Smoke-check in a browser: open `http://127.0.0.1:8082/` and navigate to **Components → Tabs**.

The interactive audit **requires** dev-web to be running for the full crawl. If the router file is missing, run a build first:

```bash
../eggshell build-lib   # from AppEggShellGallery, or eggshell test-build from repo root
```

Component routes are discovered from:

`src/Components/Route/Components/Components.fs`

---

## Audit scripts (overview)

All scripts live in `AppEggShellGallery/` and are run with `node`:

| Script | Purpose |
|--------|---------|
| **`audit-gallery-interactive.mjs`** | **Primary.** Headed (default) full crawl: interactions, assertions, visual archive, 2 passes |
| **`audit-gallery-full.mjs`** | Fast headless console-only crawl; actionable vs dev noise |
| `audit-gallery-interactions.mjs` | Per-component interaction recipes (imported by interactive) |
| `audit-gallery-assertions.mjs` | Post-interaction UI checks + assertion screenshots |
| **`audit-gallery-selectors.mjs`** | **TestId-first helpers:** `clickByTestId`, `findByTestId`, `clickLabelOrTestId`, `readUiSnapshot` |
| `audit-gallery-visual-archive.mjs` | Archival PNGs + manifests for human/AI review |
| `audit-gallery-components.mjs` | Route discovery from router; skip lists; coverage report |

---

## Running the full interactive audit (recommended)

### Start

1. Ensure **dev-web** is running on `:8082`.
2. In another terminal:

```bash
cd AppEggShellGallery
node audit-gallery-interactive.mjs http://127.0.0.1:8082
```

A Chromium window opens (unless `--headless`). Each component page is visited, interacted with, asserted, and archived.

### CLI options

```bash
node audit-gallery-interactive.mjs [baseUrl] [options]
```

| Flag | Default | Description |
|------|---------|-------------|
| `--headless` | off | Run without visible browser |
| `--slow-mo=N` | `120` | Delay between Playwright actions (ms) |
| `--passes=N` | `2` | Full gallery crawl repetitions |
| `--pause-ms=N` | `800` | Wait after each page load (ms) |
| `--screenshots=all\|failures\|none` | `all` | Screenshot per **assertion** |
| `--visual-archive=on\|off` | `on` | Archival PNGs for aesthetic review |

Examples:

```bash
# Headless CI-style run, assertion screenshots on failure only
node audit-gallery-interactive.mjs http://127.0.0.1:8082 --headless --screenshots=failures

# Slower headed run for watching interactions
node audit-gallery-interactive.mjs http://127.0.0.1:8082 --slow-mo=250

# Against production (read-only smoke; do not rely on local-only fixtures)
node audit-gallery-interactive.mjs https://eggshell.dev --headless

# Tee console to a stable log path
node audit-gallery-interactive.mjs http://127.0.0.1:8082 2>&1 | tee audit-browser/interactive-latest-run.log
```

### Output layout

Each run creates a timestamped directory:

```text
AppEggShellGallery/audit-browser/interactive/<ISO-timestamp>/
  coverage-report.json       # route discovery vs interaction/assertion recipes
  final-report.json
  final-report.md
  pass-1/
    meta.json
    interactions.log         # every click/fill/assert line
    assertions.log
    console-full.log
    native-dialogs.log       # window.alert/confirm if any
    assertions-summary.json
    summary.json
    summary.md
    pages/                   # per-component console log
    screenshots/             # assertion pass/fail captures
      {Component}/
      failures/
    visual-archive/          # human/AI aesthetic review (no pixel diff)
      README.md
      index.jsonl
      {Component}/
        manifest.json
        sample-00-after-interaction.png
        viewport-after-interaction.png   # overlap-prone pages
  pass-2/
    ... (same structure)
```

### Reading results

**Console line per page:**

```text
[pass 1] QueryGrid                    ok (16 actions, 3 asserts)
[pass 1] Forms                        ACTIONABLE:1
[pass 1] Layout_Row                   REVIEW:1
[pass 1] SomeComponent                ASSERT:2
```

| Flag | Meaning |
|------|---------|
| `ok` | No failed assertions, no actionable console issues |
| `ACTIONABLE:N` | N console/page errors worth fixing (see `summary.md`) |
| `ASSERT:N` | N post-interaction UI assertions failed |
| `REVIEW:N` | Page has clickable demos but no specific recipe yet |
| `LOAD FAIL` | Page did not load (HTTP/timeout) |

**Where to look first:**

1. `final-report.md` — pass comparison
2. `pass-1/summary.md` — actionable console issues by component
3. `pass-1/assertions-summary.json` — failed UI assertions
4. `pass-1/visual-archive/index.jsonl` — filter `"overlapReviewPriority": true` for layout/overlap review
5. `pass-1/screenshots/failures/` — assertion failure images

---

## Fast console-only audit

Headless, no interactions beyond page load:

```bash
cd AppEggShellGallery
node audit-gallery-full.mjs http://127.0.0.1:8082
```

Output:

```text
audit-browser/local/full-audit.json
audit-browser/local/full-audit.md
```

Use `https://eggshell.dev` as the argument for production. Useful for a quick “any red console?” sweep before or after the interactive run.

---

## What the interactive audit does

### 1. Route discovery

Components are **not** hardcoded. The crawl reads `Components.fs` (~72 routes). `coverage-report.json` lists:

- routes with only generic interaction/assertion handlers
- stale routes removed from the router

When you add a gallery page, it is picked up automatically; add recipes in `audit-gallery-interactions.mjs` / `audit-gallery-assertions.mjs` for deeper coverage.

### 2. Interactions

- Scoped to demo **visuals** cells: `.aesg-ContentComponent-table td.vertical-align-middle/top` (web) or `~aesg-sample-visuals` (Android)
- **TestId-first:** use `audit-gallery-selectors.mjs` (`clickLabelOrTestId`, `clickByTestId`) before pseudo-element heuristics
- ReactXP labels use `[data-text-as-pseudo-element="..."]` (not DOM text nodes) as fallback on web
- Horizontal sample scrolling before interacting
- **File inputs:** never clicks `Select File` (OS picker); uses `setInputFiles` on hidden `<input type="file">`
- **Native dialogs:** `window.alert/confirm` auto-accepted/dismissed → `native-dialogs.log`
- **EggShell Dialogs:** in-app overlays (OK/Yes/No pseudo buttons)

### 3. Post-interaction assertions

Behavioral checks only (not pixel diff): tab content after click, form values retained, grid headers after submit, AsyncData failure caught, etc.

Assertion screenshots: `pass-N/screenshots/{Component}/`

### 4. Visual archive (human / AI review)

After interactions, **archival PNGs** for qualitative review (overlap, spacing, aesthetics):

- One PNG per sample column (`sample-NN-after-interaction.png`)
- Extra viewport capture on layout-heavy / multi-sample pages
- `manifest.json` per component with `reviewFocus` tags and `reviewPrompt` for AI batch review

**No pixel baseline comparison** — images are stored for you or an AI to judge later.

---

## Conducting a review session

### A. Console / correctness pass

1. Run interactive audit (or use latest `audit-browser/interactive/<timestamp>/`).
2. Open `pass-1/summary.md` → **Actionable issues**.
3. Fix framework/gallery code; rebuild dev-web.
4. Re-run audit; confirm `final-report.md` shows fewer actionable pages.
5. Compare pass 1 vs pass 2 in `final-report.md` (flaky vs stable).

**Common noise (usually not actionable):** HMR, webpack-dev-server, telemetry ScreenView, React Router future flags, legacy ReactXP context warnings on input focus. The scripts filter many of these; see `textIsDevNoise` in `audit-gallery-interactive.mjs`.

### B. UI assertion pass

1. Open `pass-1/assertions-summary.json`.
2. For each failure, open the linked screenshot in `pass-1/screenshots/failures/`.
3. Read `pass-1/assertions.log` for the exact assert message.
4. Fix demo or recipe; re-run.

### C. Aesthetic / layout pass (human or AI)

1. Open `pass-1/visual-archive/README.md`.
2. Parse `index.jsonl`; prioritize `"overlapReviewPriority": true`.
3. For each component, open PNGs + `manifest.json`.
4. Review using `reviewFocus` tags (`overlap`, `layout`, `controls`, `media`, …).
5. For AI: attach PNGs + paste `reviewPrompt` from manifest.

### D. Coverage pass (new components)

1. Open `coverage-report.json`.
2. Check `genericInteractionOnly` / `genericAssertionOnly`.
3. Add handlers to `audit-gallery-interactions.mjs` and `audit-gallery-assertions.mjs` (mirror gallery `.fs` / `.render` demos).
4. Re-run; `REVIEW` flags should drop for that component.

---

## Adding recipes for a new component

1. Add gallery page + route (normal eggshell/Fable workflow).
2. Add the route case in `Components.fs` (`renderContent` match).
3. In `audit-gallery-interactions.mjs`, add `COMPONENT_HANDLERS.YourComponent`:
   - scope clicks/fills to `cell` (visuals column)
   - prefer `clickLabelOrTestId` / `clickByTestId` from `audit-gallery-selectors.mjs`; fall back to `clickPseudo`, `fillLabel`
4. In `audit-gallery-assertions.mjs`, add `ASSERTION_HANDLERS.YourComponent`:
   - verify post-interaction state in the sample cell
5. Re-run audit; check `coverage-report.json` and `REVIEW` lines.

Pure-F# gallery migrations (goal A): if only the **code column** changes, visuals assertions should stay valid. Update recipes if **demo behavior** changes.

---

## What is NOT covered

| Area | Status |
|------|--------|
| Pixel / visual regression baselines | Not used |
| Animation frame sequences | Only post-animate resting screenshot |
| Code column vs rendered layout parity | Not checked |
| Choice-list selected styling | Clicks only; no selected-state CSS assert |
| Native Metro / iOS / Android | **Android:** automated Appium audit (see below). iOS: manual/logcat workflow |
| OS file picker UI | Avoided via `setInputFiles` |

See also `LEARNINGS.md` (§ Gallery Playwright audit).

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `Cannot find module 'playwright'` | `npm install -D playwright && npx playwright install chromium` |
| `Gallery router not found` | Run `../eggshell dev-web` or `eggshell test-build` first |
| All pages `LOAD FAIL` | dev-web not running or wrong port; check `:8082` |
| Audit hangs on file picker | Update scripts (should skip `Select File`); use latest `audit-gallery-interactions.mjs` |
| `found 0 visual sample cell(s)` | Wrong page layout or page not finished loading; increase `--pause-ms` |
| Headed run steals focus | Use `--headless` or `--slow-mo=0` |
| Too many `ACTIONABLE` on Input_* | Often ReactXP `TextInput` legacy context warnings when filling fields; triage vs real errors in `summary.md` |

---

---

## Android interactive audit (Appium)

Mirrors the web Playwright audit: same interaction recipes (`audit-gallery-interactions.mjs`) and assertions (`audit-gallery-assertions.mjs`), driven through Appium instead of URLs.

### Prerequisites

1. **Native bundle** (includes `testId` on sample wrappers — rebuild after pulling LibClient change):
   ```bash
   cd AppEggShellGallery
   ../eggshell dev-native
   ```
2. **Metro** on `:8081`:
   ```bash
   npx react-native start --port 8081
   adb reverse tcp:8081 tcp:8081
   ```
3. **App** on emulator:
   ```bash
   npx react-native run-android
   ```
4. **Appium 2** (UiAutomator2) — installed locally in this project:
   ```bash
   cd AppEggShellGallery
   npm install
   npm run appium:setup          # once: installs uiautomator2@2.45.1 (Appium 2 compatible)
   npm run appium                # start server on :4723
   ```
   Or without npm scripts: `npx appium` / `npx appium driver install uiautomator2`
5. **adb device** connected (`adb devices` shows `device`).

### Run

```bash
cd AppEggShellGallery
npm install                    # installs webdriverio
npm run audit:interactive:android
# or 1 pass, slower:
node audit-gallery-interactive-android.mjs --passes=1 --slow-mo=200
```

Fast logcat-only sweep (no interactions beyond navigation):

```bash
npm run audit:full:android
```

### Options

| Flag | Default | Description |
|------|---------|-------------|
| `--passes=N` | `2` | Full gallery crawl repetitions |
| `--pause-ms=N` | `800` | Wait after sidebar navigation |
| `--slow-mo=N` | `120` | Extra delay between actions |
| `--screenshots=all\|failures\|none` | `all` | Per-assertion device screenshots |
| `--visual-archive=on\|off` | `on` | Archival PNGs per sample cell |
| `--appium-host=HOST` | `127.0.0.1` | Appium server |
| `--appium-port=N` | `4723` | Appium port |
| `--launch-timeout-ms=N` | `120000` | Wait for Metro bundle + first RN render before audit |

### Output layout

```text
AppEggShellGallery/audit-android/interactive/<ISO-timestamp>/
  coverage-report.json
  final-report.json
  final-report.md
  pass-1/
    meta.json
    interactions.log
    assertions.log
    logcat-full.log
    pages/{Component}.log
    screenshots/{Component}/
    visual-archive/{Component}/
    summary.json
    summary.md
```

Navigation uses **stable testIds** (see [`LibClient/ACCESSIBILITY.md`](../LibClient/ACCESSIBILITY.md)):

| testId | Purpose |
|--------|---------|
| `eggshell-sidebar-menu` | Open handheld drawer |
| `sidebar-blade-components` | Enter Components blade |
| `sidebar-component-{CaseName}` | Tap component (e.g. `sidebar-component-IconButton`) |
| `sidebar-scroll-middle` | Middle scroll container |
| `aesg-sample-visuals` | Loaded component sample wrapper |

Flow: open menu → tap Components blade → swipe-scroll middle list → tap `~sidebar-component-*` → wait for drawer close. Text-label matching is fallback only.

On connect, the runner waits until the gallery UI is visible (sidebar menu `testId`), not just until the Activity is foreground. It also detects Metro load failures ("Unable to load script", etc.). Use `--launch-timeout-ms=180000` on cold starts if needed.

**Dev observability (DEBUG builds):** `window.__eggshell.AppEggShellGallery.uiLog()` and `.uiSnapshot()` expose recent actions and visible interactives for automation debugging.

**Handheld sidebar behavior (important for Appium nav):** The gallery uses `AppShell.Content` + `LC.Draggable` — a drawer that slides in from the left. `LC.Sidebar.WithClose` wraps all items: every tap runs `nav.Go(...); close e`, so the drawer **always retracts** after selecting a blade. Fixed-top row (Docs / Tools / Components / …) switches route sections; the long component list scrolls inside `ScrollableMiddle` only. The audit opens the drawer before each component, enters the Components blade, swipe-scrolls the middle list (scoped to the 300px drawer — not the main content scroll view), taps the item, then waits for the drawer to close.

### Android vs web differences

| Area | Web | Android |
|------|-----|---------|
| Navigation | URL `/Desktop/Components/...` | testId sidebar (`~sidebar-component-*`) |
| Sample scope | `.aesg-ContentComponent-table td...` | `~aesg-sample-visuals` (driver maps `~` → Android `resource-id`) |
| Labels/buttons | `[data-text-as-pseudo-element]` | Native `TextView` text |
| Console | Playwright `page.on('console')` | `adb logcat` (ReactNativeJS) |
| File inputs | `setInputFiles` on hidden input | Skipped (OS picker) |
| REVIEW heuristic | Unhandled pseudo clickables | Disabled (no pseudo DOM) |

---

## Native gallery (manual / iOS)

Web audit does not replace native testing. For Android/iOS:

1. `../eggshell dev-native` (Fable → native bundle)
2. `npx react-native start --port 8081`
3. `configSourceOverrides.native.js` (from template)
4. Run app on emulator/simulator; watch Metro/logcat for red errors

See `LEARNINGS.md` for emulator/simulator setup details.

---

## Quick reference

```bash
# Terminal 1 — gallery
cd AppEggShellGallery && ../eggshell dev-web

# Terminal 2 — full audit
cd AppEggShellGallery
npm install -D playwright          # once
npx playwright install chromium    # once
node audit-gallery-interactive.mjs http://127.0.0.1:8082

# Fast console sweep
node audit-gallery-full.mjs http://127.0.0.1:8082

# Android interactive (Appium + Metro + emulator)
npm run audit:interactive:android
```

Latest run output: `audit-browser/interactive/` (timestamped) or `audit-browser/interactive-latest-run.log` if you tee stdout.
