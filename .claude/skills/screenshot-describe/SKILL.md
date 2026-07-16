---
name: screenshot-describe
description: Delegate screenshot/image understanding to a vision-capable model, since the author's opencode orchestrator model glm 5.2 cannot read images. Use whenever you need to know what a screenshot shows (broken layout, visual regression, before/after UI comparison, a11y state visible on screen, rendered component correctness) and cannot inspect it yourself. Captures via adb screencap then pipes to the claude CLI.
user-invocable: true
argument-hint: "[capture|<image.png> [\"your prompt\"]]"
---

# screenshot-describe

The orchestrator model (and all local subagents) cannot read images. When a task needs to know what
a screenshot actually shows, delegate to a vision-capable model via the `claude` CLI (installed at
`/opt/homebrew/bin/claude`). This is the ONLY working invocation -- do not improvise others.

## Why this exists

Image-understanding is needed for: visual regression checks, "does the UI look broken", confirming a
rendered component matches intent, verifying a11y state visible on screen (selection, focus), and
before/after comparisons. `uiautomator dump` gives the semantic tree but NOT the visual reality
(colors, spacing, overlap, clipping, icon rendering). For those, ask the vision model.

## Capture (Android)

```
adb exec-out screencap -p > /tmp/shot.png
```
(iOS simulator: `xcrun simctl io booted screenshot /tmp/shot.png`. Web: browser/devtools screenshot
or Playwright `page.screenshot`.)

## Describe -- the ONLY working invocation

```
echo "<your prompt>" | claude -p --input-format text /path/to/shot.png
```

- The prompt goes via STDIN with `--input-format text`; the image path is a trailing positional arg.
- `claude -p "<prompt>" <image.png>` FAILS ("No screenshot attached").
- `claude -p "<prompt>" --file <image.png>` FAILS (needs a session token).
- Piping raw PNG bytes via STDIN FAILS ("corrupt PNG bytes pasted as text").
- Output goes to stdout. The model speaks in caveman style by default if the repo CLAUDE.md is
  loaded; for a detailed description, say so in the prompt (e.g. "describe in detail, full
  sentences").

## Prompt guidance

Be specific about what you need and give context. Bad: "describe this". Good:

> This is AppTodo on Android with reduce-motion ON. Previously the first todo row was a massive
> full-width red delete block with content squeezed to a sliver. Describe what you see now: the
> todo row layout, whether the delete button is compact (~72px) or still huge, whether content
> (title/checkbox) is visible, and the theme toggle state.

For before/after: capture both, describe each, compare. Keep screenshots in `/tmp/` (git-ignored).

## When NOT to use this

- You only need the semantic tree (labels, roles, testIds, selected state, bounds): use
  `adb shell uiautomator dump` + parse the XML directly. Faster and exact.
- You need pixel diffing between two screenshots: use the `argent-screenshot-diff` skill.
- The model CAN read images in this session: just use the `read` tool on the image path.

## Model availability

`claude` CLI (Claude Code) is the default; it supports vision. `ollama` is also installed but no
vision model is pulled by default -- prefer `claude`. If `claude` is unavailable, ask the user to
describe the screenshot.

## Doc refs

- runbooks/android.md (screenshot capture)
- knowledge-base/engineering-log.md (session 16 -- why this skill exists, working invocation)
