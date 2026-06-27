[<AutoOpen>]
module LibClient.Components.Tabs

open Fable.React

open LibClient

open ReactXP.Components
open ReactXP.Styles

// NOTE: do NOT `open ReactXP.LegacyStyles` here. Its rule functions (flex, Overflow, FlexDirection,
// backgroundColor, ...) shadow the new-dialect ones and break the make*Styles computation expressions.
// Reference legacy types/helpers fully-qualified instead (see the xLegacyStyles bridge below).

let Selected   = LC.Tab.Selected
let Unselected = LC.Tab.Unselected

type Theme = {
    BackgroundColor: Color
    BorderColor:     Color
    BorderWidth:     int
}

[<RequireQualifiedAccess>]
module private Styles =
    let scrollView (theme: Theme) =
        makeScrollViewStyles {
            Overflow.Visible
            flex 0
            backgroundColor theme.BackgroundColor
            borderBottom    theme.BorderWidth theme.BorderColor
        }

    let view =
        makeViewStyles {
            FlexDirection.Row
            AlignContent.Stretch
            Overflow.Visible
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Tabs(
            children: array<ReactElement>,
            ?styles: array<ScrollViewStyles>,
            ?theme: Theme -> Theme,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme

        // Bridge legacy class-based styles (passed by not-yet-converted render-DSL callers via the
        // parent's `class=` attribute) into the modern styles array. Safe to delete once every caller
        // passes `styles` directly. See LEARNINGS.md (render-DSL -> F# conversion recipe).
        let legacyScrollViewStyles : array<ScrollViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ScrollViewStyles> "ReactXP.Components.ScrollView" styles |]
            | None -> [||]

        RX.ScrollView(
            horizontal = true,
            styles =
                [|
                    Styles.scrollView theTheme
                    yield! legacyScrollViewStyles
                    yield! (styles |> Option.defaultValue [||])
                |],
            children =
                elements {
                    RX.View(
                        styles   = [| Styles.view |],
                        children = children
                    )
                }
        )
