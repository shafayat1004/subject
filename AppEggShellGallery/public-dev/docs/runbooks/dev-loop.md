# Dev Loop

The universal inner loop for all EggShell frontend work: edit, targeted rebuild, confirm, reload, observe, fix.

Related: [Android](./android.md) | [iOS](./ios.md) | [Web](./web.md) | [Build and rebuild](./build-rebuild.md) | [Architecture: Frontend](../architecture/frontend.md)

---

## Mental model: the moving parts

| Part | Command | Role | Default port |
|---|---|---|---|
| Fable watch (`dev-native`) | `../../eggshell dev-native` | F# to `.build/native/commonjs` on save (native) | -- |
| Metro | `npx react-native start --port 8081` | serves the JS bundle to device/sim | 8081 |
| Web dev-server (`dev-web`) | `../../eggshell dev-web` | Fable + webpack for the browser | 9080 |
| Gallery dev-web | `../eggshell dev-web` (from `AppEggShellGallery/`) | Fable + webpack for the gallery site | 8082 |
| Appium (Tier 2 native) | `npm run appium` | WebDriver server for native automation | 4723 |
| Device | emulator / simulator / browser | runs the app | -- |

Key facts that cause the most confusion:

- **One** `dev-native` and **one** Metro serve **both** Android and iOS simultaneously.
- The Android emulator reaches Metro via `10.0.2.2:8081` and needs `adb reverse tcp:8081 tcp:8081` once per emulator boot. iOS simulator uses `localhost:8081` with no reverse needed.
- **A green Fable/Metro build does not mean your change is on screen.** The app must be reloaded and Metro may serve a cached bundle. Always confirm on-device (Tier 1) or via `doctor`/`snapshot` (Tier 2).
- **Metro bundles `node_modules` from each package's build entry** (e.g. `react-native-web` from its `dist/` directory, not `src/`). Patching the wrong file is invisible. Verify a patch actually reached the bundle (see [Build and rebuild](./build-rebuild.md#verify-patch-reached-bundle)).
- **Framework edits (LibClient, LibRouter, ...) are picked up through the app's dependency graph** because the watch prints `Watching ../../..`. Confirm the watch log shows `Compiled .../LibClient/...` after a `touch`; otherwise wipe `.build/<platform>/fable` and restart.

---

## The universal inner loop

1. **Edit** F# (app or framework) / styles / a vendored JS patch.
2. **Targeted rebuild** -- let the running watch recompile only the changed file(s). See [Build and rebuild](./build-rebuild.md#targeted-recompile).
3. **Confirm the rebuild is real** -- the watch printed `Started Fable compilation...` then compiled with no `error FS`; the emitted `.js` is newer than your `.fs`. Beware `Skipped compilation ... up-to-date` false-greens.
4. **Reload the app** on the target. This is always agent-driven (see below). Do not ask the user to press `r` in Metro or webpack.
5. **Observe** -- screenshot and read logs (Tier 1), or `npm run observe -- snapshot` (Tier 2). See [Android](./android.md), [iOS](./ios.md), [Web](./web.md), [Audit toolkit](./audit-toolkit.md).
6. **If a runtime error** -- read it from the red box / `logcat` / browser console. The component stack names the owning component; that is your starting point.

Fast-changing styles often only need an app reload, not a full rebuild. F# changes always require the Fable recompile first.

---

## Agent owns terminal input -- never ask the user to press `r`

Watch terminals (Metro, `dev-web`, `eggshell dev-native`) that the agent started are **read-only for the user**. Do not tell them to press `r`, `a`, or `i` in those sessions.

| Target | Agent reload method |
|---|---|
| Fable watch | `touch` the changed `.fs`; read terminal for `Compiled N/M:` |
| Web (`dev-web`) | `touch` sources; restart `eggshell dev-web` in agent shell if stale; verify with `curl` |
| Metro / native | `adb shell am force-stop ...` + relaunch, or `xcrun simctl terminate/launch`; not user keystrokes in agent Metro |
| Browser | The user may hard-refresh the page themselves -- that is not a terminal operation |

If the user already has `dev-native` and Metro running in their own terminals, the agent must not start parallel copies. Read the user's terminal files first; `touch` changed files to nudge the existing watch; wait for `Compiled .../LibClient/...` before claiming the bundle is fresh.

---

## When a restart is mandatory (not just a touch)

- **`.fsproj` changed** (added, moved, or reordered files): Fable watch does not pick up `.fsproj` changes. Restart `dev-native` or `dev-web`.
- **`metro.config.js` changed / LibClient vendor patch applied / watch output looks stale**: restart Metro with `--reset-cache`.

See also [Build and rebuild: mandatory restart conditions](./build-rebuild.md#mandatory-restart).
