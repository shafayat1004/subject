# Audit Toolkit (Tier 2)

The `audit/` toolkit lives in `SuiteTodo/AppTodo/audit/`. It drives native platforms via **Appium** and the web via **Playwright**, using `testId`/accessibility ids instead of pixel coordinates. Output is structured JSON in `audit/out/<timestamp>/` -- machine-readable for agents and CI gates.

Use this for reliable element interaction, before/after layout diffs, and CI regression checks. For the fast interactive debug loop (did my change render? is there a red-box error?) use [Tier 1 raw CLI](./index.md#two-tiers-of-observation) first.

Related: [Dev loop](./dev-loop.md) | [Android](./android.md) | [iOS](./ios.md) | [Web](./web.md)

---

## One-time setup {#setup}

```bash
cd SuiteTodo/AppTodo
npm run appium:setup            # install uiautomator2 (Android) + xcuitest (iOS) drivers
npm run observe -- setup-devices --list   # pick default AVD + simulator -> writes native.local.json
```

---

## Preflight check {#doctor}

Run `doctor` any time something feels off before starting a debug session:

```bash
npm run observe -- doctor
# checks: PATH, connected devices, dev-web :9080, Metro :8081, Appium :4723
```

---

## Observe commands {#commands}

All commands accept `-p web|android|ios` and `--orientation portrait|landscape`.

**Snapshot:** screenshot + layout metrics + logs + health summary:

```bash
npm run observe -- snapshot -p android
npm run observe -- snapshot -p web --orientation landscape
```

**Add a todo by testId:**

```bash
npm run observe -- add-todo "Buy milk" -p web
```

**Workflow: before/after add-todo + card-width diff:**

```bash
npm run observe -- workflow layout-check -p android
```

**Native smoke: health + add-todo + component checks:**

```bash
npm run observe -- workflow verify-native -p ios
```

**Compare two most recent runs:**

```bash
npm run observe -- diff
```

**Console/device log snapshot:**

```bash
npm run observe -- logs -p web
npm run observe -- logs -p android
```

**Keep a browser open for manual inspection:**

```bash
npm run observe -- open
```

---

## Artifact layout {#artifacts}

Artifacts land in `audit/out/<timestamp>-*/`:

| File | Contents |
|---|---|
| `current.png` | Screenshot |
| `*-layout-metrics.json` | Measured widths, heights, positions of key elements |
| `*-ui-hierarchy.xml` | Native UI hierarchy (Android/iOS) |
| `*-dom-summary.json` | DOM summary (web) |
| `*-health.json` | App health status |
| `*-log-summary.json` | Classified log output |

This is the machine-readable state an LLM agent should consume instead of re-deriving from a PNG.

---

## When to use Tier 2 vs Tier 1 {#when-to-use}

| Situation | Tier |
|---|---|
| "Did my change render?" | 1 (screenshot + adb/simctl) |
| "Is there a runtime error?" | 1 (logcat / simctl log / DevTools) |
| Tap a specific control reliably while layout is changing | 2 (tap by testId, not pixels) |
| Measure element widths/heights; detect a layout regression | 2 (layout-metrics JSON + diff) |
| CI / regression gate | 2 (workflow + diff) |
| LLM agent needs structured JSON state | 2 (snapshot) |

---

## testId attribute name on web {#testid-attribute}

The primitive layer now runs on react-native-web, which emits `data-testid`. Use `[data-testid="..."]` in Playwright selectors. (Historically, the former `@chaldal/reactxp` layer emitted `data-test-id`; any components not yet migrated through the RNW seam may still emit that attribute.) The gallery audit selectors in `audit-gallery-selectors.mjs` already handle both attributes.

---

## Playwright gallery audit scripts {#gallery-scripts}

The gallery has its own Playwright audit scripts under `AppEggShellGallery/`:

```bash
cd AppEggShellGallery
node audit/audit-gallery-components.mjs          # full component sweep
node audit/audit-gallery-components.mjs --only=a11y   # accessibility baseline only
node audit/audit-gallery-components.mjs --only=style-leak-full   # style leak detection
node audit/audit-todo-web.mjs http://127.0.0.1:9080              # AppTodo web smoke
```
