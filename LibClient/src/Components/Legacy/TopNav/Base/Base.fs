namespace LibClient.Components.Legacy.TopNav

open LibClient

module Base =
    type Center =
    | Children
    | Heading of string

    type Theme = {
        BackgroundColor: Color
        TextColor:       Color
        Height:          int
    }

namespace LibClient.Components

open Fable.React

open LibClient

open ReactXP.Components
open ReactXP.Styles

open LibClient.Components.Legacy.TopNav.Base

// Cluster producer (§9.3): FullScreen.styles.fs used to cascade into this component's internal
// "view"/"heading" blocks via BaseStyles.Theme.One/Height. Callers still on render DSL pass those
// overrides through ?xLegacyStyles (class="top-nav"); modern callers use ?theme / per-section style params.

[<AutoOpen>]
module Legacy_TopNav_Base =

    [<RequireQualifiedAccess>]
    module private Styles =
        let view (theme: Theme) =
            makeViewStyles {
                FlexDirection.Row
                JustifyContent.SpaceBetween
                AlignContent.Center
                AlignItems.Center
                Overflow.Visible
                height            theme.Height
                shadow            (Color.BlackAlpha 0.2) 3 (0, 2)
                borderBottom      1 (Color.Grey "dc")
                backgroundColor   theme.BackgroundColor
            }

        let left =
            makeViewStyles {
                flex 0
                Overflow.Visible
                padding 8
            }

        let center =
            makeViewStyles {
                flex 1
                FlexDirection.Row
                JustifyContent.Center
                Overflow.Visible
            }

        let right =
            makeViewStyles {
                flex 0
                Overflow.Visible
                padding 8
            }

        let heading (theme: Theme) =
            makeTextStyles {
                fontSize 20
                color theme.TextColor
            }

    module private LegacyBridge =
        let viewStyles (xLegacyStyles: Option<List<ReactXP.LegacyStyles.RuntimeStyles>>) (className: string) : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let textStyles (xLegacyStyles: Option<List<ReactXP.LegacyStyles.RuntimeStyles>>) (className: string) : array<TextStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<TextStyles> "ReactXP.Components.Text" styles |]
            | None -> [||]

    type Constructors.LC.Legacy.TopNav with
        [<Component>]
        static member Base(
                center:         Center,
                ?left:          ReactElement,
                ?right:         ReactElement,
                ?children:      ReactChildrenProp,
                ?theme:         Theme -> Theme,
                ?styles:        array<ViewStyles>,
                ?leftStyles:    array<ViewStyles>,
                ?centerStyles:  array<ViewStyles>,
                ?rightStyles:   array<ViewStyles>,
                ?headingStyles: array<TextStyles>,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?key:           string
            ) : ReactElement =
            key |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme
            let leftEl   = defaultArg left noElement
            let rightEl  = defaultArg right noElement

            RX.View(
                styles =
                    [|
                        Styles.view theTheme
                        yield! LegacyBridge.viewStyles xLegacyStyles "view"
                        yield! (defaultArg styles [||])
                    |],
                children =
                    [|
                        RX.View(
                            styles =
                                [|
                                    Styles.left
                                    yield! LegacyBridge.viewStyles xLegacyStyles "left"
                                    yield! (defaultArg leftStyles [||])
                                |],
                            children =
                                [|
                                    if leftEl <> noElement then leftEl
                                    else LC.Legacy.TopNav.Filler()
                                |]
                        )

                        RX.View(
                            styles =
                                [|
                                    Styles.center
                                    yield! LegacyBridge.viewStyles xLegacyStyles "center"
                                    yield! (defaultArg centerStyles [||])
                                |],
                            children =
                                [|
                                    match center with
                                    | Center.Children ->
                                        children
                                        |> Option.map (fun ch -> castAsElement ch)
                                        |> Option.defaultValue noElement
                                    | Center.Heading text ->
                                        LC.UiText(
                                            value = text,
                                            styles =
                                                [|
                                                    Styles.heading theTheme
                                                    yield! LegacyBridge.textStyles xLegacyStyles "heading"
                                                    yield! (defaultArg headingStyles [||])
                                                |]
                                        )
                                |]
                        )

                        RX.View(
                            styles =
                                [|
                                    Styles.right
                                    yield! LegacyBridge.viewStyles xLegacyStyles "right"
                                    yield! (defaultArg rightStyles [||])
                                |],
                            children =
                                [|
                                    if rightEl <> noElement then rightEl
                                    else LC.Legacy.TopNav.Filler()
                                |]
                        )
                    |]
            )
