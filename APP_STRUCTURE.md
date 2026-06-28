# App Source Structure — Loose Standard

A lightweight convention for organizing an EggShell **application** (`App*`) under
`src/`. It is deliberately loose: follow the spirit (clear layers, predictable
locations, dependencies flow one direction) rather than enforcing every folder for
tiny apps. `AppTodo` is the reference implementation.

## Layers (dependency direction: top → bottom may not reverse)

1. **Foundation** — app wiring that everything else builds on.
   - `Config.fs` — config source + validated `Config` record.
   - `Services.fs` — service construction (HTTP, event bus, subject services).
   - `Navigation.fs` — routes, dialogs, navigation spec.
   - `RenderHelpers.fs` — tiny shared render utilities.
   - `Bootstrap.fs` — registers libraries, loads config, mounts the app. **Always last.**

2. **Domain/** — app logic, no view code.
   - `ErrorMessages.fs` — error → display string mapping.
   - `Actions.fs` — user intents returning action results.
   - `*Queries.fs` — query builders + client-side filtering/sorting.
   - `*Display.fs` — formatting/label helpers, enumerations of domain values.

3. **Theme/** — everything visual that is not a component.
   - `Colors.fs` — `ColorScheme` + a `SemanticPalette` (light/dark) record.
   - `*Theme.fs` — memoized `makeViewStyles`/`makeTextStyles` styles for screens.
   - `ComponentsTheme.fs` — applies palette/scheme to framework component themes
     (e.g. `Themes.Set` for inputs/pickers per appearance).

4. **Platform registration** — generated/registration glue.
   - `Icons.fs`, `LocalImagesRegistration.fs`, `ComponentsHierarchy.fs`.

5. **Components/** — all UI.
   - `App.fs` (root), `AppContext.fs` (providers) at the top.
   - `Route/` — one file per route screen.
   - Feature subfolders (`Dialog/`, `Input/`, `Form/`, `<Feature>/`) as the app grows.
   - `FakeData/` — debug-only fake services (kept out of Domain).

## Conventions

- **Module name ≠ path.** F# module names (`module AppTodo.Colors`) are independent of
  folder, so files can be grouped into `Domain/`, `Theme/`, etc. without renaming modules.
- **`.fsproj` order is the build order.** Keep `<Compile Include>` entries grouped by
  layer with a comment per group; a file must appear after everything it references.
  Foundation services may depend on Domain/FakeData, so the order is:
  Config → Domain types → services/navigation → Theme → registration → Domain logic →
  ComponentsTheme → Components → Bootstrap.
- **Styles never run in render.** Static styles are top-level `let`s; parametrized styles
  use `ViewStyles.Memoize`/`TextStyles.Memoize` keyed on primitives. See the framework
  styling guidance.
- **Pure F# components.** Use `type Ui.Route with [<Component>] static member` (and a
  private `Helpers` type for sub-components). No render DSL in apps.
- **Appearance (light/dark).** A `SemanticPalette` per mode in `Colors.fs`; screens read
  `SemanticPalette.forMode`; `ComponentsTheme.applyInputThemes palette` re-themes
  framework inputs/pickers before they render so dark mode does not flash light fills.

## Small-app escape hatch

For a single-screen app it is fine to keep `Domain/` and `Theme/` as the only folders and
leave foundation + registration files at `src/` root (as `AppTodo` does). Add deeper
`Components/` subfolders only when the screen count justifies them.
