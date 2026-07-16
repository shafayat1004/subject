# Android Accessibility Scanner Audits

Google's **Accessibility Scanner** (`com.google.android.apps.accessibility.auditor`, Play Store)
is a Tier-1.5 on-device audit tool that sits between raw `uiautomator dump` (semantic tree only)
and the Appium/Playwright harness (structured JSON, CI). It records a screen (or a multi-screen
session), draws annotated boxes on a screenshot, and exports a **text report + annotated screenshot
ZIP** you can pull and read. It catches things `uiautomator` cannot: text contrast ratios, touch
target sizes in dp, duplicate content descriptions, unexposed text, and competing editable labels.

Use it after any UI change that touches interactive elements, colors, or layout to get a real
WCAG-AA-oriented second opinion before declaring a11y work done (rule 12). It is not a replacement
for the [Audit Toolkit](../runbooks/audit-toolkit.md) (Appium/Playwright for CI), but it is faster to
run and catches a different class of defects.

Related: [Accessibility](./index.md) | [Recipes](./recipes.md) |
[Backlog](./backlog.md) | [Android Runbook](../runbooks/android.md) |
[Screenshot Describe](../knowledge-base/engineering-log.md)

---

## Setup {#setup}

1. Install **Accessibility Scanner** from the Play Store on the device.
2. Enable it as an accessibility service: **Settings > Accessibility > Installed apps >
   Accessibility Scanner > On**. Or verify via adb:

   ```bash
   adb shell settings get secure enabled_accessibility_services
   # should list com.google.android.apps.accessibility.auditor/...ScannerService
   ```

3. A floating blue tick-mark button appears on screen. It overlays every app.

---

## Recording a session {#recording}

The Scanner offers two modes from the floating tick-mark menu:

- **Snapshot** -- scans the current single screen.
- **Record** -- records a multi-screen session as you navigate; tap the stop button when done.

**Synthetic `adb shell input tap` does NOT reach the Scanner overlay.** The floating tick mark
and its popup menu filter synthetic events (they sit in an accessibility overlay layer that adb
`input` cannot target reliably). To open the menu or start/stop a recording, **tap with a real
finger** on the device. Once the Scanner's own app window is open (History, results list), adb
taps work normally on those windows.

**Workflow (real-finger taps for the overlay, adb for everything else):**

1. Launch your app (`adb shell monkey -p com.eggshell.apptodo -c android.intent.category.LAUNCHER 1`).
2. Navigate to the screen(s) you want audited.
3. Tap the floating tick mark with a real finger.
4. Tap **Record** (or **Snapshot**) with a real finger.
5. For a recording: navigate through screens; tap the stop button when done.
6. The Scanner opens its results view. Tap **Share results** (top bar) and save the ZIP to
   `Download/Accessibility Audit/` (or anywhere pullable).
7. Pull and unzip:

   ```bash
   adb pull "/sdcard/Download/Accessibility Audit/results_Todo_<timestamp>.zip" /tmp/
   unzip /tmp/results_Todo_*.zip -d /tmp/a11y_results
   # -> report1.txt screen1.png report2.txt screen2.png ...
   ```

**Gotcha: the share-sheet "Save to Downloads" sometimes creates the folder but not the file.**
If `adb pull` finds an empty folder, re-share and save again, or copy the ZIP to the PC directly.

---

## Reading a report {#reading}

Each `reportN.txt` starts with a summary line, then one block per suggestion:

```
<Category>
<testId or [x1,y1][x2,y2] bounds>
<Description>
<Detail>
```

Categories the Scanner checks:

| Category | What it catches |
|----------|----------------|
| **Text contrast** | Foreground/background ratio < 4.5:1 (WCAG AA). Reports estimated colors. |
| **Touch target** | Clickable item smaller than 48dp in any dimension. Reports size in dp. |
| **Editable item label** | An editable `TextView` has `android:contentDescription` that competes with the editable content for screen-reader focus. |
| **Item descriptions** | Multiple items share the same speakable text (duplicate labels confuse screen-reader navigation). |
| **Item type label** | A content description contains unnecessary text (e.g. gesture hints like "swipe"). |
| **Unexposed Text** | Visible text detected on screen that is not in the element's accessibility label. |

Bounds are in **device pixels** (not dp). To map a bound to a UI element, cross-reference with
`uiautomator dump` testIds, or ask the vision model (see [Screenshot Describe
skill](../knowledge-base/engineering-log.md)) with the annotated screenshot.

---

## AppTodo audit findings (2026-07-11, post reduce-motion fix) {#apptodo-findings}

A 5-screen recording session of AppTodo (dark + light themes, add-todo form, todo list, edit form)
on the POCO F1. The ZIP exports `report1.txt` through `report5.txt` + `screen1.png` through
`screen5.png`. Below is the consolidated, deduplicated finding set. All are **app-side**
(SuiteTodo/AppTodo) unless marked `[framework]`.

### Touch target < 48dp (14 distinct elements)

| Element | testId | Size | Fix |
|---------|--------|------|-----|
| Filter tab (inactive) | `todo-filter-open` / `-done` / `-archived` | 40dp h | Raise tab height to 48dp |
| Filter tab "All" (inactive) | `todo-filter-all` | 32x40dp | Raise width + height to 48dp |
| Category chip (unselected) | `todo-new-category-none` / `-personal` / `-shopping` | 44dp h | Raise to 48dp |
| Category chip "Work" | `todo-new-category-work` | 36x44dp | Raise width to 48dp+ |
| Category chip "Health" | `todo-new-category-health` | 46x44dp | Raise to 48x48dp |
| Category chip "Other" | `todo-new-category-other` | 40x44dp | Raise width to 48dp |
| Add todo button | `todo-add-mobile` | 46dp h | Raise to 48dp |
| Title input ("What needs doing?") | -- | 42dp h | Raise to 48dp |
| Search input ("Search todos...") | -- | 42dp h | Raise to 48dp |
| Todo checkbox | `todo-item-aaaaaaaa-toggle` | 44dp w | Raise to 48dp |
| Todo edit button | `todo-item-aaaaaaaa-edit` | 42x42dp | Raise to 48x48dp |

### Text contrast < 4.5:1 (dark theme)

| Element | Foreground | Background | Ratio | Fix |
|---------|-----------|------------|-------|-----|
| Dark toggle segment label | #FFFFFF | #5BA5A6 | 2.85 | Darken toggle fill or use darker label |
| Inactive filter tab labels | #666666 | #160D08 | 3.34 | Lighten inactive tab text color |
| "No category" chip (unselected) | #458B8C | #2A1A10 | 4.25 | Lighten chip text or darken bg |
| "Work" chip (selected) | #458B8C | #1F3F4C | 2.84 | Selected chip: lighten text or darken fill |
| "Personal" chip (selected) | #458B8C | #1F3F4C | 2.84 | Same |
| "Shopping" chip | #458B8C | #1A3D24 | 3.07 | Same |
| Add todo button label | #FFFFFF | #458B8C | 3.94 | Darken button fill or use darker label |
| Search input placeholder | #000000 | #2A1A10 | 1.25 | **BLACK text on dark bg** -- use themed placeholder color |
| "High" priority badge | #1F3F4C | #0A0604 | 1.80 | Worst offender -- lighten badge text or recolor |

### Text contrast < 4.5:1 (light theme)

| Element | Foreground | Background | Ratio | Fix |
|---------|-----------|------------|-------|-----|
| Filter tab label (inactive) | #458B8C | #FDF9F6 | 3.77 | Darken inactive tab text |
| Title input placeholder | #94A3B8 | #F5F0EC | 2.27 | Darken placeholder color |
| "No category" chip | #458B8C | #E5E7EB | 3.19 | Darken chip text |
| "Work" chip (selected) | #458B8C | #C2E2E9 | 2.88 | Selected chip: darken text |
| "Personal" chip | #458B8C | #C2E2E9 | 2.88 | Same |
| "Shopping" chip | #458B8C | #C6E4D1 | 2.90 | Same |
| Add todo button | #FFFFFF | #458B8C | 3.94 | Same in both themes |
| Search input placeholder | #94A3B8 | #EFE6DF | 2.08 | Darken placeholder |

### Editable item label (2 inputs, every screen)

The title input ("What needs doing?") and search input ("Search todos...") both have an
`android:contentDescription` that competes with the editable content. A screen reader may read the
content description instead of the text the user typed. Fix: remove the competing
`contentDescription` from editable text fields; rely on the visible label + placeholder.

### Item descriptions (duplicates)

| Element | Speakable text | Duplicates | Fix |
|---------|---------------|------------|-----|
| Title input | "Todo title" | 1 other | Remove duplicate contentDescription |
| Category chips (new form) | "Personal" / "No category" / "Work" / "Shopping" / "Health" / "Other" | 1-3 others | Chips appear in both the new-todo and edit-todo forms with identical labels; disambiguate with context (e.g. "New todo: Work" vs "Edit todo: Work") |
| Priority badge "Low" | "Low" | 1 other | Non-clickable; deduplicate or add context |

### Unexposed Text

The title input placeholder "What needs doing?" is detected as visible text not included in the
element's accessibility label. The search input has the same issue when the user has typed text
(e.g. "buy" was detected as unexposed). Fix: ensure the placeholder/typed text is part of the
field's accessible name.

### Resolved (old audit, not in new)

The earlier single-screen audit (2026-07-11 20:45) flagged an **item type label** on
`todo-item-aaaaaaaa-row`: the content description "Todo: Fix swipe-to-delete..., open" contained
the word "swipe" (a gesture hint baked into the description). This did **not** appear in the
post-fix 5-screen recording, suggesting the reduce-motion / swipe work resolved it or the row
description was cleaned up. Worth confirming explicitly.

---

## What the Scanner cannot check {#limits}

- **Screen-reader announcement order / live regions** -- it does not run TalkBack; use TalkBack
  manually for announcement and focus-order verification.
- **Keyboard navigation** -- it does not test tab order or focus-visible rings; use the web audit
  (axe-core / Playwright) for that.
- **Reduce-motion behavior** -- it does not toggle or test reduce-motion paths; do that manually
  (see [Troubleshooting: reduce-motion](../runbooks/troubleshooting.md)).
- **Gesture alternatives** -- it flags "swipe" in descriptions but does not verify a non-gesture
  alternative exists.
- **Color-independence** -- it does not check whether color is the sole signal; verify manually.
- **Text scaling** -- it does not test at larger font scales; set **Settings > Display > Font size**
  to largest and re-audit.

The Scanner is a **necessary but not sufficient** a11y gate. Pair it with a TalkBack pass and the
web axe-core audit for full coverage (rule 12).
