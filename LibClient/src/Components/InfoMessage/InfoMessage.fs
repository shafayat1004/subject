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

    // Key on CSS string (primitive), not Color — fast-memoize uses reference equality on objects.
    let textForColorCss =
        TextStyles.Memoize (fun (colorCss: string) ->
            makeTextStyles { TextAlign.Center; color (Color.InternalString colorCss) }
        )

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member InfoMessage(message: string, ?level: Level, ?styles: array<TextStyles>, ?theme: Theme -> Theme, ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        let theLevel = defaultArg level Level.Info
        let theTheme = Themes.GetMaybeUpdatedWith theme
        let levelColor =
            match theLevel with
            | Info      -> theTheme.InfoColor
            | Attention -> theTheme.AttentionColor
            | Caution   -> theTheme.CautionColor
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
                LC.Text(message, styles = [| Styles.textForColorCss levelColor.ToCssString; yield! defaultArg styles [||] |])
            |]
        )
