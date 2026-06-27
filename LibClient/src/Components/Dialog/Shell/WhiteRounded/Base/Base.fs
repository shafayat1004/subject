[<AutoOpen>]
module LibClient.Components.Dialog_Shell_WhiteRounded_Base

open Fable.React

open LibClient
open LibClient.Components.Dialog.Shell.WhiteRounded.Base

open ReactXP.Components
open ReactXP.Styles

module LC =
    module Dialog =
        module Shell =
            module WhiteRounded =
                module Base =
                    type Theme = {
                        Width: Option<int>
                    }

open LC.Dialog.Shell.WhiteRounded.Base

type LibClient.Components.Constructors.LC.Dialog.Shell.WhiteRounded with
    [<Component>]
    static member Base(
            canClose: CanClose,
            ?children: ReactChildrenProp,
            ?inProgress: bool,
            ?accessibilityLabel: string,
            ?theme: Theme -> Theme,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let inProgress = defaultArg inProgress false
        let theTheme = Themes.GetMaybeUpdatedWith theme

        let rawTheme (rawDefault: LC.Dialog.Shell.WhiteRounded.Raw.Theme) : LC.Dialog.Shell.WhiteRounded.Raw.Theme =
            match theTheme.Width with
            | Some width -> { rawDefault with Width = Some width }
            | None       -> rawDefault

        LC.Dialog.Shell.WhiteRounded.Raw(
            canClose = canClose,
            inProgress = inProgress,
            ?accessibilityLabel = accessibilityLabel,
            theme = rawTheme,
            ?xLegacyStyles = xLegacyStyles,
            children =
                [|
                    RX.ScrollView(
                        vertical = true,
                        children =
                            (children |> Option.defaultValue [||])
                    )
                |]
        )
