# Accessibility Recipes

Copy-paste recipes for screen-reader semantics and per-component accessibility patterns. These are the
`[safe]` building blocks -- all use RN-native props that survive the ReactXP to RNW migration unchanged.

For the theory behind these patterns, see [Spectrum](./accessibility/spectrum.md). For the platform settings that
feed into them, see [Platform Settings](./accessibility/platform-settings.md). For the component model these build on,
see [Frontend Architecture](./architecture/frontend.md) and [Component Authoring](./fsharp/component.md).

---

## 1. Screen-reader semantics (`[safe]`)

These are the four pillars of every accessible interactive element.

### 1.1 Name

Assign an accessible name by:

- `LC.Pressable(label = "...")` for pressable controls.
- `RX.View(accessibilityLabel = "...")` / `RX.Text(accessibilityLabel = "...")` when the node needs a
  label distinct from its children.
- Prefer a **visible** label for inputs (a separate `LC.UiText` above the field) -- this also satisfies
  sighted users.
- Ensure the **visible text is contained in the name** (WCAG 2.5.3 "label in name"). Icon-only controls
  must have a `label` even though there is no visible text.

### 1.2 Role

Assign the role that matches the component's function:

```fsharp
role = AccessibilityRole.Button    // activates an action
role = AccessibilityRole.Link      // navigates
role = AccessibilityRole.CheckBox  // binary toggle with persistent state
role = AccessibilityRole.Radio     // one-of-many selection
role = AccessibilityRole.Tab       // tab in a tablist
role = AccessibilityRole.TabList   // container of tabs
role = AccessibilityRole.Header    // heading (level is [web-only] precision)
role = AccessibilityRole.Search    // search container
role = AccessibilityRole.ListItem  // row in a list
role = AccessibilityRole.List      // list container
role = AccessibilityRole.Status    // live status container
role = AccessibilityRole.Switch    // on/off toggle
role = AccessibilityRole.Image     // image with meaningful content
role = AccessibilityRole.Dialog    // modal dialog
```

### 1.3 State

Use `AccessibilityStateRecord` to express the current interactive state:

```fsharp
state = AccessibilityStateRecord.selected isActive
state = AccessibilityStateRecord.checked' isChecked
state = AccessibilityStateRecord.expanded isOpen
state = AccessibilityStateRecord.disabled isDisabled
state = AccessibilityStateRecord.busy isLoading
```

### 1.4 Live regions

**Persistent container** (counts, validation, progress): set a live region on the view so the screen
reader announces changes automatically:

```fsharp
RX.View(
    accessibilityRole = AccessibilityRole.Status,
    accessibilityLiveRegion = AccessibilityLiveRegion.Polite,
    accessibilityLabel = sprintf "%i open" count,
    children = ...
)
```

`Polite` waits for the user to finish; `Assertive` interrupts immediately (use sparingly, errors only).

**Imperative announcement** (transient result with no on-screen representation):

```fsharp
LC.LiveRegion.announcePolite "Deleted Buy milk"          // Polite (most common)
LC.LiveRegion.announce "Error saving" Assertive          // Assertive

// Always include the item name -- "Deleted Buy milk", not "Deleted item"
LC.LiveRegion.announcePolite (sprintf "Deleted %s" todo.Title.Value)
```

On native this calls `AccessibilityInfo.announceForAccessibility`. On web it updates a hidden
`aria-live` DOM node (VoiceOver, NVDA, and JAWS all react). Do **not** call
`announceForAccessibility` directly -- it is native-only and silently no-ops on web.

Also keep the container's `accessibilityLabel` equal to the current text, so the full reading is correct
on re-focus.

### 1.5 Hiding decoration

Every icon that is purely decorative (repeats adjacent text, provides no information on its own) must be
hidden from screen readers:

```fsharp
importantForAccessibility = ImportantForAccessibility.No              // hide this node
importantForAccessibility = ImportantForAccessibility.NoHideDescendants // hide this and all children
```

---

## 2. Complete examples

### Pressable as a radio option

```fsharp
LC.Pressable(
    onPress = onSelect,
    label = sprintf "%s theme" label,
    role = AccessibilityRole.Radio,
    state = AccessibilityStateRecord.selected isActive
)
```

### Live-region count container

```fsharp
RX.View(
    accessibilityRole = AccessibilityRole.Status,
    accessibilityLiveRegion = AccessibilityLiveRegion.Polite,
    accessibilityLabel = sprintf "%i open, %i done" openCount doneCount,
    children = ...
)
```

### Decorative prefix icon

```fsharp
RX.Image(
    source = Icons.magnifier,
    importantForAccessibility = ImportantForAccessibility.No
)
```

---

## 3. Per-component playbook

> Rule of thumb: every pressable gets `label` plus `role` (plus `state` if selectable or toggle); every
> repeated icon is hidden; every input gets a visible `label` or `accessibilityLabel`; every
> auto-updating count or message gets a live region; every target is at least 44px; text keeps
> `allowFontScaling`.

The gallery shows a live version of each pattern in the **Group/RadioGroup**, **LiveRegion**, and
**With.Accessibility** components (Components section).

### A. Button

- Visible text becomes the accessible name automatically when it is the sole child.
- Icon-only button: `label` is **required** at the call site (make it mandatory in the constructor).
- `role = AccessibilityRole.Button`.
- Touch target at least 44px (use padding or `minHeight`/`minWidth` in styles, not fixed size).

### B. Toggle / segmented control

- Wrap the group in a container with `role = AccessibilityRole.RadioGroup` and a group label
  (`LC.RadioGroup` or `LC.Group` when available -- backlog #10).
- Each segment: `role = AccessibilityRole.Radio` plus `state = AccessibilityStateRecord.selected isActive`.
- The container label names the whole control (for example, "View mode").

### C. Tabs / filters

- Container: `role = AccessibilityRole.TabList`.
- Each tab: `role = AccessibilityRole.Tab` plus `state = AccessibilityStateRecord.selected isActive`.
- Tab label should match the visible tab text.

### D. Text input

- Prefer a **visible** label above the field (a `LC.UiText` that is always visible, not a floating
  placeholder that disappears).
- When a visible label is not present, add `accessibilityLabel` to the input.
- Error messages: render in a live region (`Polite`) adjacent to the field so they are announced without
  re-focus.

### E. Picker / select

- Visible label names the field (see D).
- The picker trigger: `role = AccessibilityRole.Button` (or the picker primitive's built-in role) plus
  the current value in its `label` (for example, "Priority: High").
- Popup items: `role = AccessibilityRole.Option` (or `ListItem`) plus `state = AccessibilityStateRecord.selected`.

### F. Checkbox / toggle row

- `role = AccessibilityRole.CheckBox`.
- `state = AccessibilityStateRecord.checked' isChecked`.
- Use the row's title as the accessible name so the full row label is announced.

### G. List and rows

- List container: `role = AccessibilityRole.List`.
- Each row: `role = AccessibilityRole.ListItem`.
- Compose a **summarizing label** for each row so screen-reader users hear "Buy milk, due today,
  Priority: High" rather than four separate reads of unlabelled children.
- Meta chips (priority, category) carry context in their label ("Priority: High") so they are not read
  as bare words.

### H. Status chips and counts

- `role = AccessibilityRole.Status`.
- `accessibilityLiveRegion = AccessibilityLiveRegion.Polite`.
- `accessibilityLabel` = the spoken summary (for example, "3 open tasks, 5 done").

### I. Headings

- `role = AccessibilityRole.Header`.
- Heading level (h1/h2/h3) is `[web-only]` precision -- the role alone is sufficient for native.

### J. Decorative icon

- Always `importantForAccessibility = ImportantForAccessibility.No`.
- Never the sole content of a control (backlog #9 / principle 3 in [index](./accessibility/index.md)).

### K. Destructive action

- Confirm with a dialog or provide an **undo** snackbar.
- Announce the result imperatively -- include the item name:

```fsharp
let! result = deleteTodo todo.Id
if Result.isOk result then
    LC.LiveRegion.announcePolite (sprintf "Deleted %s" todo.Title.Value)
```

- The delete button label must also include the item name so VoiceOver identifies it before activation:
  `"Delete Buy milk"`, not `"Delete"` or `"Delete item"`.
- See backlog #15 for the framework-wide undo pattern.

### M. OS accessibility settings

Use `LC.With.Accessibility` to adapt rendering to OS flags:

```fsharp
LC.With.ReducedMotion (fun reduced ->
    if reduced then staticView else animatedView)

LC.With.BoldText (fun bold ->
    LC.UiText(
        styles = [| if bold then Styles.heavyLabel else Styles.label |],
        children = castAsElement label))

LC.With.Accessibility (fun settings ->
    RX.View(
        styles = [| Styles.panel settings.ReduceTransparency |],
        children = content))

### L. Any text

- Keep `allowFontScaling = true` (the default -- do not set it to false).
- Avoid fixed `height` values that clip when text grows; use `minHeight` and allow the container to
  expand.
- Use `maxContentSizeMultiplier` only when an extreme multiplier would genuinely break layout -- prefer
  reflow.

---

## 4. Verification checklist (per component)

Run through this before marking a component accessibility-complete:

| Check | Tool |
|-------|------|
| Name + role + state announced correctly | TalkBack (Android), VoiceOver (iOS), NVDA/VoiceOver (web) |
| Decoration is silent | Screen reader -- decorative icons produce no announcement |
| Dynamic text is announced | Screen reader -- trigger the update and listen |
| Text scales without clipping | Set OS font size to maximum; confirm layout reflows |
| Colors meet AA contrast and are never the sole signal | Contrast helper (backlog #13); visual check |
| Touch target at least 44px | Inspect styles; `minHeight`/`minWidth` |
| Gesture has a non-gesture alternative | Tap or keyboard path exists for every swipe action |

For a full description of verification tools and commands, see
[Troubleshooting](./runbooks/troubleshooting.md).
