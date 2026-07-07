[<AutoOpen>]
module LibClient.Components.LabelledFormField

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Responsive

open Rn.Components
open Rn.Styles

module LC =
    module LabelledFormField =
        type Theme = {
            LabelWidth: int
            LabelColor: Color
        }

open LC.LabelledFormField

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        ViewStyles.Memoize (fun (screenSize: ScreenSize) ->
            makeViewStyles {
                Overflow.Visible

                match screenSize with
                | ScreenSize.Desktop ->
                    FlexDirection.Row
                    AlignItems.Center
                    JustifyContent.FlexEnd
                    padding 10
                | ScreenSize.Handheld ->
                    FlexDirection.Column
                    marginBottom 12
            })

    let label =
        TextStyles.Memoize (fun (screenSize: ScreenSize) (labelColor: Color) (labelWidth: int) ->
            makeTextStyles {
                color labelColor

                match screenSize with
                | ScreenSize.Desktop ->
                    width labelWidth
                | ScreenSize.Handheld ->
                    marginBottom 6
                    fontSize 14
            })

    let field =
        ViewStyles.Memoize (fun (screenSize: ScreenSize) ->
            makeViewStyles {
                Overflow.Visible

                match screenSize with
                | ScreenSize.Desktop -> flex 1
                | ScreenSize.Handheld -> ()
            })

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member LabelledFormField(
            label:            string,
            ?children:        ReactChildrenProp,
            ?labelStyles:     array<TextStyles>,
            ?fieldStyles:     array<ViewStyles>,
            ?theme:           Theme -> Theme,
            ?testId:          string,
            ?xLegacyStyles:   List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:             string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        let legacyLabelStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findApplicableStyles legacyStyles "label" with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        let legacyFieldStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findApplicableStyles legacyStyles "field" with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    Rn.View(
                        ?testId = testId,
                        styles =
                            [|
                                Styles.view screenSize
                                yield! legacyViewStyles
                            |],
                        children =
                            elements {
                                Rn.View(
                                    accessibilityLabel = label,
                                    styles = [| yield! legacyLabelStyles |],
                                    children =
                                        elements {
                                            LC.UiText(
                                                value = label,
                                                styles =
                                                    [|
                                                        Styles.label screenSize theTheme.LabelColor theTheme.LabelWidth
                                                        yield! (defaultArg labelStyles [||])
                                                    |]
                                            )
                                        }
                                )

                                Rn.View(
                                    styles =
                                        [|
                                            Styles.field screenSize
                                            yield! legacyFieldStyles
                                            yield! (defaultArg fieldStyles [||])
                                        |],
                                    children = (defaultArg children [||])
                                )
                            }
                    )
        )
