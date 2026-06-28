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
    let view = makeViewStyles { marginHorizontal 18; marginVertical 18 }

    let text =
        TextStyles.Memoize(
            fun (textColor: Color) (labelFontSize: int) ->
                makeTextStyles { color textColor; fontSize labelFontSize }
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
                LC.UiText(text, styles = [| Styles.textFor theTheme theLevel; yield! defaultArg styles [||] |])
            |]
        )
