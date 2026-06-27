[<AutoOpen>]
module LibClient.Components.InfoMessage

open Fable.React
open LibClient
open ReactXP.Components
open ReactXP.Styles

type Level =
| Info
| Attention
| Caution

module LC =
    module InfoMessage =
        type Theme = {
            InfoColor:      Color
            AttentionColor: Color
            CautionColor:   Color
        }

open LC.InfoMessage

module private Styles =
    let view = makeViewStyles { marginVertical 16; paddingHorizontal 20 }

    let text (theme: Theme) (level: Level) =
        let levelColor =
            match level with
            | Info      -> theme.InfoColor
            | Attention -> theme.AttentionColor
            | Caution   -> theme.CautionColor
        makeTextStyles { TextAlign.Center; color levelColor }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member InfoMessage(message: string, ?level: Level, ?styles: array<TextStyles>, ?theme: Theme -> Theme, ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let theLevel = defaultArg level Level.Info
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
                LC.Text(message, styles = [| Styles.text theTheme theLevel; yield! defaultArg styles [||] |])
            |]
        )
