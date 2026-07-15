# LibClient accessibility and automation conventions

## Semantic press targets: `LC.Pressable`

Interactive controls must expose a **single labeled press target**, not visual content plus an invisible overlay.

```fsharp
LC.Pressable(
    onPress = onPress,
    label = "Save",
    ?testId = "save-button",
    role = AccessibilityRole.Button,
    overlay = true,              // TapCapture-style absolute hit area
    ?pointerState = pointerState // when using LC.Pointer.State for hover/depress visuals
)
```

`LC.TapCapture` is a thin shim over `Pressable` (overlay mode). New code should call `Pressable` directly.

### Required props

| Prop | Purpose |
|------|---------|
| `label` | Screen readers, automation text fallback |
| `testId` | Stable selector for Playwright/Appium (always-on when provided) |
| `role` | RN/RNW accessibility role (default `Button`) |
| `state` | `AccessibilityStateRecord` (disabled, selected, busy, …) |

Use `A11ySlug.testId prefix label` for slugged ids (e.g. `sidebar-item-docs`).

## Rn bindings

F# `Rn.View`, `Rn.Button`, and `Rn.ScrollView` forward the common accessibility props (`accessibilityLabel`, `accessibilityRole`, `accessibilityState`, `testId`, …) to the underlying react-native-web / React Native primitives. The primitives support these; the F# bindings were the gap.

## Dev-only UI observability

In `#if DEBUG`, apps call:

```fsharp
LibClient.UiActionLog.installGlobalHook Fable.Core.JS.globalThis "YourAppName"
```

Exposes:

- `window.__eggshell.YourAppName.uiLog()` — recent press/nav/sidebar actions
- `window.__eggshell.YourAppName.uiSnapshot()` — route, visible interactives (testId/label/role/state), recent actions

Route changes are logged via `LogRouteTransitions` → `UiActionLog.setCurrentRoute`.

## Live regions

```fsharp
LC.LiveRegion.announce "Saved" AccessibilityLiveRegion.Polite
```

## Gallery sidebar testIds

| testId | Element |
|--------|---------|
| `eggshell-sidebar-menu` | Handheld menu toggle |
| `sidebar-blade-components` | Fixed-top Components blade |
| `sidebar-component-{CaseName}` | Component nav item |
| `sidebar-scroll-middle` | Middle ScrollView |
| `aesg-sample-visuals` | Component sample wrapper |

## Migration checklist (TapCapture → Pressable)

1. **Convert the component to pure F# first** if it is still `.render` (do not patch `.render`).
2. Add `label` (and `testId` when automation needs a stable id).
3. Swap `LC.TapCapture` → `LC.Pressable` with `overlay = true`.
4. Pass `pointerState` when the parent uses `LC.Pointer.State`.
5. Remove TapCapture-only props unless still needed on Pressable.

## Not in v1

- `accessibilityHint` (not yet wired through the Rn primitives)
- Full focus-trap audit (dialogs handle Escape via `Dialog.Base`; restrictFocusWithin not yet wired everywhere)
