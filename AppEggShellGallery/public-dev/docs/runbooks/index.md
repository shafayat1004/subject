# Runbooks

Follow-it-literally procedures for the dev loop, per-platform debugging, and the modernization migration.
Written so a human or an LLM agent can execute them without prior context.

Related docs: [Architecture: Frontend](../architecture/frontend.md) | [Modernization overview](../modernization/index.md) | [ReactXP to RNW](../modernization/reactxp-to-rnw.md) | [Accessibility](../accessibility/index.md) | [CLI tools](../tools/cli.md)

> **Skills front door:** executable wrappers for these runbooks live in `.claude/skills/`
> (fable-rebuild-verify, debug-android, debug-ios, debug-web, verify-feature, release-build, plus
> style-leak-audit, a11y-check, docs-sync, gallery-page-add). Invoke the skill; it follows the
> runbook and reports evidence.

---

## Pages in this section

| Page | What it covers |
|---|---|
| [Dev loop](./dev-loop.md) | The universal edit-rebuild-confirm-reload-observe cycle; the agent/terminal ownership model; the mental-model table of moving parts. |
| [Android](./android.md) | Boot emulator, fix landscape/sideways AVD, start the toolchain, launch/reload, screenshot/zoom, tap/type/rotate, read logcat errors. |
| [iOS](./ios.md) | Boot simulator, launch/reload, screenshot/rotate/logs, iOS-specific gotchas. |
| [Web](./web.md) | Start dev-web, observe, web-specific gotchas. |
| [Audit toolkit](./audit-toolkit.md) | The Tier 2 `audit/` toolkit: doctor, snapshot, add-todo, workflow, diff, logs; when to use it vs raw CLI. |
| [Build and rebuild](./build-rebuild.md) | Targeted recompile, confirming rebuild output, when a restart is mandatory, verifying a vendor patch reached the bundle, killing stale processes. |
| [Troubleshooting](./troubleshooting.md) | Symptom-to-fix catalog organized by theme (build/cache, styling, RNW/Rn bindings, layout, color, dark-mode inputs, picker, key warnings, gestures). |
| [Backend](./backend.md) | Bringing up the backend stack, silo boot failures, Orleans test triage (stasis/timeout/CodecNotFound). |
| [Migration execution](./migration-execution.md) | The executor manual for the .NET 10 + Fable 5 + RNW modernization: per-phase step recipes, exact version pins, per-primitive mapping table, pitfall checklist, escalation gates. |

---

## Two tiers of observation

Before reaching for any observation command, pick the lightest tier that answers your question.

### Tier 1: raw CLI (adb / simctl / browser DevTools)

No server setup. Instant. Answers "did my change render?" and "is there a runtime error?"

- Drive the device with `adb` (Android) or `xcrun simctl` (iOS).
- Grab a screenshot as a PNG, read logs from `logcat`/`simctl log`, and (Android only) tap with pixel coordinates.
- **Asymmetry — iOS has no raw tap.** `adb` injects input (`adb shell input tap/swipe/text/keyevent`) and dumps the hierarchy (`uiautomator dump`). `xcrun simctl` has **no** tap/swipe/type or hierarchy dump; it can only screenshot, log, launch/terminate, `openurl`, `push`, and set appearance/location/status-bar. To tap or read the view tree on iOS you must go to Tier 2 (Appium/xcuitest) or install a raw-CLI helper (`idb` or `axe`). See [iOS: interacting with the simulator](./ios.md#interact).
- **Pros:** zero startup, immediate, sees runtime red-box errors, trivial to script.
- **Cons:** Android taps are pixel coordinates (brittle when layout changes); iOS has no raw tap at all; no element lookup by `testId`, no structured layout data.

### Tier 2: `audit/` toolkit (Appium for native, Playwright for web)

Already built in `SuiteTodo/AppTodo/audit/` (`npm run observe -- ...`). Drives by `testId`/accessibility id. Emits structured JSON (layout metrics, DOM/hierarchy summaries, health, classified logs) into `audit/out/<timestamp>/`.

- **Pros:** deterministic element interaction, machine-readable state for agents, layout regression diffs, cross-platform one command, CI gate.
- **Cons:** needs Appium server + drivers (`npm run appium:setup`), slower to start.

**Rule of thumb: debug with Tier 1, verify/gate with Tier 2.** You do not need Appium or Playwright to reload an app, screenshot it, read a runtime error, or eyeball a fix.

---

## "I'm stuck" decision tree

Work through these in order before escalating.

1. **Does it build?** `grep "error FS" /tmp/dev-native.log`. If errors, fix the F#. If the log says `Skipped compilation ... up-to-date`, that is a cache false-green: force recompile (see [Build and rebuild](./build-rebuild.md#targeted-recompile)).
2. **Did the change reach the device?** Reload the app. Still showing old behavior? Try Metro `--reset-cache`. For a node_modules/vendor patch, verify with the bundle-check command in [Build and rebuild](./build-rebuild.md#verify-patch-reached-bundle).
3. **Blank screen or red box?** Read it: `adb logcat -d | grep -A30 -iE "error|unique"` (Android), `xcrun simctl spawn booted log show --last 2m` (iOS), browser DevTools console (web). The component stack names the owning component; start there.
4. **Renders but wrong layout/spacing/rounding?** Screenshot and zoom ([Android](./android.md#screenshot)). Check the [Troubleshooting](./troubleshooting.md) catalog first; most visual bugs there are known framework-style interactions.
5. **Need to confirm an element/state precisely or gate a fix?** Switch to Tier 2: `npm run observe -- snapshot` or `workflow layout-check` for JSON and diffs.
6. **Everything is weird / duplicated output?** You probably have stale duplicate watches. See [Build and rebuild: killing stale processes](./build-rebuild.md#killing-stale-processes), then restart one `dev-native` and one Metro.
7. **A control does nothing on tap/click?** Don't assume the handler is wrong. First **bisect input-delivery vs handler-logic**: fire the handler's code path programmatically (e.g. a temporary mount `useEffect`) to skip the gesture. Works now → the bug is touch/click delivery (hit-testing, `opacity < 0.01` on Fabric iOS, an overlapping view, a gesture responder stealing the touch); still broken → the handler/downstream is. An element being present in the accessibility tree does **not** mean it receives touch. On native prefer coordinate taps over element `.click()`, and remember JS `console.log` is unreliable (RN 0.86 + Hermes) — use screenshot / page-source diffs. See [iOS](./ios.md#interact) and the `debug-ios`/`debug-android` skills.
