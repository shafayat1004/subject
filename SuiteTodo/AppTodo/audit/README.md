# AppTodo audit / observability

Implementation details for the `audit/` tooling. **Start with the [AppTodo README](../README.md)** for dev setup (web, Android, iOS, Metro, Fable watch) and observability quick reference.

## CLI

```bash
npm run observe -- <command> [options]
```

Entry: `audit/todo-observe.mjs`.

## Commands

| Command | Description |
|---------|-------------|
| `doctor` | Toolchain PATH, devices, dev-web `:9080`, Metro `:8081`, Appium `:4723` |
| `snapshot` | Screenshot + layout + logs + health (web Playwright or native Appium) |
| `add-todo "title"` | Fill add form + capture |
| `workflow layout-check` | Before/after add-todo + card width diff |
| `workflow verify-native` | Native smoke: health + add-todo + components |
| `diff [dirA dirB]` | Compare two run directories |
| `open` | Web — browser stays open |
| `logs` | Console or device logs |
| `setup-devices` | List/set default AVD and simulator in `native.local.json` |

Flags: `-p web|android|ios`, `--timeout-ms`, `--orientation portrait|landscape`, `--headless` (web), `--json` (doctor).

## Health states

See [App health detection](../README.md#app-health-detection) in the main README. Native crash patterns align with `AppEggShellGallery/audit-gallery-app-crash.mjs`.

## Artifacts layout

### Web (`audit/out/<timestamp>-*/`)

- `manifest.json`, `current.png`, `*-layout-metrics.json`, `*-dom-summary.json`
- `*-health.json`, `*-log-summary.json`
- `*-ui-snapshot.json` (when DEBUG hook available)
- `*-console.log`, `*-page-errors.log`, `*-network-errors.log`

### Native

- `current.png`, `*-layout-metrics.json` (testID bounds + card heuristic)
- `*-health.json`, `*-log-summary.json`
- `*-ui-hierarchy.xml`, `*-ui-summary.json`
- `*-device.log`, `session-logcat.log` (Android full session)

## Native app IDs

Defaults: `com.eggshell.apptodo` / `MainActivity` / iOS bundle `com.eggshell.apptodo`.

Override via `audit/native.local.json`, env (`APPTODO_ANDROID_PACKAGE`, etc.), or auto-read from `android/app/build.gradle`.

## Source layout

| Path | Role |
|------|------|
| `todo-observe.mjs` | CLI (yargs) |
| `lib/` | Drivers (web, android, ios), health, capture, selectors |
| `workflows/` | `layout-check`, `verify-native`, `snapshot` |
| `out/` | Run artifacts (gitignored) |
| `native.local.json.example` | Device default template |
