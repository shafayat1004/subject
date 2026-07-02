# App Source Structure

A lightweight convention for organizing an EggShell application (`App*`) project under `src/`. It is deliberately loose: follow the spirit (clear layers, predictable locations, dependencies flow one direction) rather than enforcing every folder for tiny apps. `AppTodo` inside `SuiteTodo/` is the reference implementation.

For where files live in the broader repository tree, see [Directory Structure](./unsorted/directory-structure.md). For how to write components inside an app, see [Component Guide](./fsharp/component.md).

---

## Layers (dependency direction: top cannot import bottom's successors)

Dependencies may only flow **downward**. A lower layer must never import from a layer above it.

```
Foundation
    Domain/
    Theme/
    Platform registration
    Components/
```

### 1. Foundation

App wiring that everything else builds on.

| File | Purpose |
|------|---------|
| `Config.fs` | Config source + validated `Config` record. |
| `Services.fs` | Service construction (HTTP, event bus, subject services). |
| `Navigation.fs` | Routes, dialogs, navigation spec. |
| `RenderHelpers.fs` | Tiny shared render utilities. |
| `Bootstrap.fs` | Registers libraries, loads config, mounts the app. **Always last in the fsproj.** |

### 2. Domain/

App logic with no view code.

| File | Purpose |
|------|---------|
| `ErrorMessages.fs` | Error to display-string mapping. |
| `Actions.fs` | User intents returning action results. |
| `*Queries.fs` | Query builders and client-side filtering/sorting. |
| `*Display.fs` | Formatting and label helpers, enumerations of domain values. |

### 3. Theme/

Everything visual that is not a component.

| File | Purpose |
|------|---------|
| `Colors.fs` | `ColorScheme` and a `SemanticPalette` (light/dark) record. |
| `*Theme.fs` | Memoized `makeViewStyles`/`makeTextStyles` styles for screens. |
| `ComponentsTheme.fs` | Applies palette/scheme to framework component themes (e.g. `Themes.Set` for inputs/pickers per appearance mode). |

### 4. Platform registration

Generated or registration glue: `Icons.fs`, `LocalImagesRegistration.fs`, `ComponentsHierarchy.fs`.

### 5. Components/

All UI.

- `App.fs` (root component) and `AppContext.fs` (providers) at the top of the subfolder.
- `Route/` — one file per route screen.
- Feature subfolders (`Dialog/`, `Input/`, `Form/`, `<Feature>/`) as the app grows.
- `FakeData/` — debug-only fake services (kept out of Domain).

---

## Conventions

### Module name vs. path

F# module names (`module AppTodo.Colors`) are independent of the folder they live in. Files can be grouped into `Domain/`, `Theme/`, etc. without renaming their module. This is intentional: it keeps code organized by concern without forcing dotted module paths to mirror directory paths.

### `.fsproj` order is the build order

Keep `<Compile Include>` entries grouped by layer with a comment per group. A file must appear after everything it references. The typical order is:

```
Config
  -> Domain types
  -> Services / Navigation
  -> Theme
  -> Registration
  -> Domain logic
  -> ComponentsTheme
  -> Components
  -> Bootstrap
```

Foundation services may depend on Domain and FakeData, so services appear after Domain types but before Theme.

### Styles never run in render

Static styles are top-level `let` bindings. Parametrized styles use `ViewStyles.Memoize` / `TextStyles.Memoize` keyed on **primitives** (Color, int, bool, small DU), never on whole `Theme` or `Colors` records (fresh record references defeat the cache). See the framework [Styling guide](./fsharp/styling.md) for the full recipe, including the CE naming trap.

### Pure F# components

Use `type Ui.Route with [<Component>] static member RouteName(...)` (and a private `Helpers` type for sub-components). No render DSL in apps. See [Component Guide](./fsharp/component.md).

### Appearance (light/dark mode)

A `SemanticPalette` per appearance mode is defined in `Colors.fs`. Screens read `SemanticPalette.forMode`. `ComponentsTheme.applyInputThemes palette` re-themes framework inputs and pickers before they render, so dark mode does not flash light fills on first render.

---

## Small-app escape hatch

For a single-screen app it is fine to keep `Domain/` and `Theme/` as the only subfolders, and to leave foundation and registration files at `src/` root (as `AppTodo` does). Add deeper `Components/` subfolders only when the screen count justifies them.
