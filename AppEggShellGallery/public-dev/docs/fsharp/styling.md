# Styling

The EggShell styling system is a least-common-denominator system between CSS and React Native's
styles. The primitive layer runs on react-native-web (web) and React Native (native), surfaced
through the `RX.*` wrappers (`RX.View`, `RX.Image`, etc.). We thinly wrap this system to:
* make it type-safe
* allow for writing helper functions that return multiple rules
* provide some performance optimizations that complement the internal caching

If you're coming from the web world, the earlier you abandon "but I can do this in CSS like this!"
line of thinking, the better.

Styles are typically applied in three ways:
* directly to RN/RNW primitives via the `RX.*` wrappers (like `RX.View`, `RX.Image`, `LC.Text`)
* to custom components that decided to accept styles and pass it along to their top level `RX.*` component
* via themes

## Applying styles directly to RX.* components

In the `.fs` file where your component lives, declare styles **before** the component. Two patterns:

### Top-level `let` bindings (preferred for simple styles)

Use this for static, non-parametrized styles — gallery sample pages, one-off layout, demos:

```fsharp
let card = makeViewStyles { margin 16 }

RX.View(
    styles = [| card |],
    children = ...
)
```

Each binding is constructed once regardless of how often you reference it.

### Named private module (when grouping several styles)

When a file has many related styles, group them under a **descriptively named** module. Do **not** use a
generic `module private Styles =` in new code:

```fsharp
[<RequireQualifiedAccess>]
module private CardStyles =
    let view = makeViewStyles { margin 16 }
    let title = makeTextStyles { fontSize 18; FontWeight.Bold }

RX.View(
    styles = [| CardStyles.view |],
    children = [| LC.Text("Title", styles = [| CardStyles.title |]) |]
)
```

`[<RequireQualifiedAccess>]` keeps call sites explicit (`CardStyles.view`, not bare `view`).

> **Note — this is forward guidance.** Most converted LibClient components (~98 files, including the
> canonical `Tabs.fs`) still use a generic `module private Styles =` and are not being mass-migrated.
> Prefer the named/top-level forms in new code; matching a file's existing `module private Styles =`
> when editing it is acceptable.

### Parametrized styles

Sometimes we want to vary styles based on input parameters. E.g.

```fsharp
type Level =
| Info
| Warning

[<Component>]
member static Card (level: Level) =
    RX.View (
        styles = [| CardStyles.card level |],
        children = ...
    )
```

For this, a memoized function should be used (inside a named styles module or at top level):

```fsharp
[<RequireQualifiedAccess>]
module private CardStyles =
    let card = ViewStyles.Memoize (fun (level: Level) -> makeViewStyles {
        backgroundColor (
            match level with
            | Level.Info    -> Colors.White
            | Level.Warning -> Colors.Orange
        )
    })
```

This works for multiple parameters as well.

If you forget to memoize, and just write a let-bound function that produces styles, worry not — the moment
you use it for the second time with the same parameters, the style-leak detector will report the leak on the JS
console, which is your cue to add memoization.

Note, that there's another way of accomplishing the same thing:

```fsharp
[<RequireQualifiedAccess>]
module private CardStyles =
    let cardInfo = makeViewStyles { backgroundColor Colors.White }
    let cardWarning = makeViewStyles { backgroundColor Colors.Orange }

[<Component>]
member static Card (level: Level) =
    RX.View (
        styles = [|
            match level with
            | Level.Info    -> CardStyles.cardInfo
            | Level.Warning -> CardStyles.cardWarning
        |],
        children = ...
    )
```

Performance here would be around the same, which means that we should prioritize readability.

As the `styles` parameter takes an array, you can pass multiple styles.

`makeViewStyles` makes styles to be used with RX.View, and `makeTextStyles` is used for LC.Text.

## Avoiding style leaks (the standard)

A "style leak" is the style system detecting that the *same* call site produced the *same* style content more
than once as a *new* object — i.e. a `makeViewStyles`/`makeTextStyles` ran inside render instead of once.
It is a correctness/perf smell, not just noise: each leak is a style object that cannot be cached.

Follow these rules and you will not produce leaks:

1. **Never build a style inside render.** Every `makeViewStyles {...}` / `makeTextStyles {...}` must be
   reached through a top-level `let`, a named styles module, or a `*.Memoize` wrapper — never inline in a
   `styles = [| ... |]` array or inside a component body / `Pointer.State` / `With.ScreenSize` callback.

   ```fsharp
   // ✗ leaks — a fresh object every render
   RX.View(styles = [| makeViewStyles { Position.Relative } |], ...)

   // ✓ built once
   let relative = makeViewStyles { Position.Relative }
   RX.View(styles = [| relative |], ...)
   ```

2. **Parametrized styles must be memoized** (see the previous section), and **key on primitives, not
   records.** Memoization keys are serialized; a whole `Theme`/`Colors` record is large and a fresh
   instance each render can defeat the cache. Pass the specific fields you use:

   ```fsharp
   // ✗ keyed on whole records
   let item = fun (theme: Theme) (colors: Colors) -> makeViewStyles { height theme.ItemHeight; ... }

   // ✓ memoized, keyed on the primitives actually used
   let item =
       ViewStyles.Memoize (fun (itemHeight: int) (border: Color) (background: Color) ->
           makeViewStyles { height itemHeight; borderColor border; backgroundColor background }
       )
   // call site: Styles.item theme.ItemHeight colors.Border colors.Background
   ```

   `Color`, `int`, `bool`, and small DUs (e.g. `FontWeight`, `ScreenSize`) are good keys.

3. **A memoized lambda parameter may not share a name with a builder operation.** Inside
   `makeViewStyles`/`makeTextStyles`, `height`, `color`, `fontSize`, `top`, `left`, `bottom`, ... are
   custom operations. A parameter of the same name shadows the operation, so `height height` is read as
   *applying* the int and fails to compile (`This value is not a function and cannot be applied`). Give
   parameters distinct names: `itemHeight`, `labelColor`, `iconFontSize`, `badgeTop`, ...

4. **Reading the warning.** The first stack frame after `createViewStyle`/`createTextStyle` names the
   leaking style (e.g. `Sidebar_Item_Styles_labelText`). Find that `let`, apply rule 1 or 2. The same
   warning re-fires on every render of a leaky site, so under a re-render loop a single offender looks
   like "tons" of leaks — fix the loop *and* the style.

## Applying styles to custom components

In general, the visuals of the component is its own business, and "injecting" styles into it should
feel quite wrong. There are some legitimate use cases for this sort of behaviour, though. For example:
* setting the width of a text input element
* setting the margin around a button
* forcing a component to expand to full width

To accommodate these types of use cases, components can choose to take `?styles: array<ViewStyles>` as
a parameter, and apply it internally as they see fit, typically onto the top-level `RX.View` within their
internal implementation. There are plenty of examples of this in the standard component library.

## Themeing components

The less privacy-invasive way of telling a component how to look is to use the "theme" that it provides
to style it. When a component declares a theme, they are explicitly saying "my visuals can be modified, here
are the modifications I accept". To learn more about themeing, read [here](./themeing.md).

## What about dynamic style values

We talked about parametrized style values. Note that we are comfortable memoizing them because in the
entire application, the number of unique parametrizations that's likely to be used is likely limited
to a fairly small number, even if we take parameters like `width: int`.

This is because "parametrized" is not the same as "dynamic". If we had to implement some style function
that takes `x: int` and produces a `left x` sort of style rule, and used it to implement, say, a draggable
component, this use case would work poorly with memoization. We _could_ allow dynamic styles, but from experience
these dynamic use cases are far better served by the animation layer (Reanimated / `RX.Animated`), which is built for performance,
which is usually a requirement for dynamic style scenarios anyway.

## Older way of declaring styles (deprecated)

We used to keep styles at the bottom of the file via a mutually recursive type:

```
and private Styles() =
    ....
```

That forced an unorthodox `static member val` syntax; missing `val` caused runtime performance
degradation. We then switched to `module private Styles =`, which is simpler but the generic name
collides easily. **Current guidance:** top-level `let` bindings, or `[<RequireQualifiedAccess>] module private FooStyles`.
See [Components](./component.md).
