# AppTodo dev observability

First-class tooling for **humans and LLM agents** to capture screenshots, DOM data, layout metrics, and logs while developing. **Headed (visible) browser is the default** so you and the agent see the same UI.

Platforms: **web (Playwright)** today; **Android (Appium)** and **iOS** hooks are stubbed for templating later (see gallery `audit-gallery-android-driver.mjs`).

## Prerequisites

```bash
cd SuiteTodo/AppTodo
./initialize
../../eggshell dev-web   # http://127.0.0.1:9080, fake backend by default
```

## Quick commands (for agents)

Copy-paste these from the repo root or `SuiteTodo/AppTodo`:

| Goal | Command |
|------|---------|
| Current state bundle | `npm run observe -- snapshot` |
| LLM JSON manifest | `npm run observe -- state` |
| Add todo + capture | `npm run observe -- add-todo "Buy milk"` |
| **Layout regression** (card width before/after add) | `npm run observe -- workflow layout-check` |
| Compare two runs | `npm run observe -- diff` |
| Pair programming (browser stays open) | `npm run observe -- open` |
| Console/page errors only | `npm run observe -- logs` |
| Headless CI | `npm run observe -- snapshot --headless true` |

## Output layout

Each run writes to `audit/out/<timestamp>-<label>/`:

- `manifest.json` — index for agents (paths, card width, error counts)
- `current.png` / `before.png` / `after.png` — full-page screenshots
- `*-layout-metrics.json` — bounding boxes (`todo-card`, inputs, viewport)
- `*-dom-summary.json` — compact DOM tree
- `*-ui-snapshot.json` — `window.__eggshell.AppTodo.uiSnapshot()` (DEBUG builds)
- `*-console.log`, `*-page-errors.log`, `*-network-errors.log`
- `layout-diff.json` — from `workflow layout-check`

## Workflows

### `workflow layout-check`

1. Captures empty-state screenshot + layout metrics  
2. Adds a todo  
3. Captures again  
4. Diffs `todo-card` width/height (flags if change &gt; 2px)

Exit code `2` = likely layout regression (e.g. card shrinking after first todo).

### `diff`

Compare layout metrics between two run folders. With no args, uses the two most recent dirs under `audit/out/`.

## Test IDs (AppTodo)

| testId | Element |
|--------|---------|
| `todo-page` | Page wrapper |
| `todo-card` | White card shell |
| `todo-new-title` | New todo input |
| `todo-add` | Add button |
| `todo-search` | Search input |

Selectors fall back to `input` / `[data-text-as-pseudo-element]` when ReactXP does not expose `data-testid`.

## Smoke audit (legacy)

`npm run audit:web` — minimal add-todo smoke test (headless).

## Templating note

This folder is the reference implementation for EggShell app scaffolding. Later: move shared `audit/lib/*` into `Meta/LibScaffolding` and wire Android/iOS drivers from the gallery patterns.
