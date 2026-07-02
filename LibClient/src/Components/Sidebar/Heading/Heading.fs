[<AutoOpen>]
module LibClient.Components.Sidebar_Heading

open Fable.React
open LibClient
open ReactXP.Components
open ReactXP.Styles

type Level =
| Primary
| Secondary

module LC =
    module Sidebar =
        module Heading =
            type Theme = {
                PrimaryTextColor:   Color
                PrimaryFontSize:    int
                SecondaryTextColor: Color
                SecondaryFontSize:  int
            }

open LC.Sidebar.Heading

module private Styles =
    // Headings sit close to the items they label (more space above, less below), so a
    // heading visually groups with the section beneath it rather than floating between rows.
    let view = makeViewStyles { marginHorizontal 18; marginTop 22; marginBottom 6 }

    let text =
        TextStyles.Memoize(
            fun (textColor: Color) (labelFontSize: int) ->
                // Uppercase + semibold + letter-spacing gives a section label a distinct
                // "heading" texture that reads apart from the normal-case, heavier item rows.
                makeTextStyles { color textColor; fontSize labelFontSize; FontWeight.W600; letterSpacing 1 }
        )

    let textFor (theme: Theme) (level: Level) =
        match level with
        | Primary   -> text theme.PrimaryTextColor theme.PrimaryFontSize
        | Secondary -> text theme.SecondaryTextColor theme.SecondaryFontSize

type LibClient.Components.Constructors.LC.Sidebar with
    [<Component>]
    static member Heading(text: string, ?level: Level, ?styles: array<TextStyles>, ?theme: Theme -> Theme, ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let theLevel = defaultArg level Level.Primary
        let theTheme = Themes.GetMaybeUpdatedWith theme
        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | s  -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" s |]
            | None -> [||]
        RX.View(
            styles = [| Styles.view; yield! legacyViewStyles |],
            children = [|
                LC.UiText(text.ToUpperInvariant(), styles = [| Styles.textFor theTheme theLevel; yield! defaultArg styles [||] |])
            |]
        )
