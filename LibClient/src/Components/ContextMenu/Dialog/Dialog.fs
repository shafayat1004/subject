module LibClient.Components.ContextMenu.Dialog

open Fable.React

open LibLang
open LibClient
open LibClient.Accessibility
open LibClient.Dialogs
open LibClient.ContextMenus.Types
open LibClient.Components
open LibClient.Components.Dialog.Base
open LC.Button

open Rn.Components
open Rn.Styles

module ButtonThemes =
    let private appearance textColor borderColor backgroundColor : Appearance =
        {
            TextColor       = textColor
            BorderColor     = borderColor
            BackgroundColor = backgroundColor
            FontWeight      = Rn.Styles.RulesRestricted.FontWeight.Normal
        }

    let private stateAppearance textColor borderColor backgroundColor : StateAppearance =
        {
            Actionable = appearance textColor borderColor backgroundColor
            Disabled   = appearance textColor borderColor backgroundColor
            InProgress = appearance textColor borderColor backgroundColor
        }

    let normal (theme: LC.Button.Theme) : LC.Button.Theme =
        { theme with Primary = stateAppearance (Color.Hex "#004eff") Color.White Color.White }

    let normalSelected (theme: LC.Button.Theme) : LC.Button.Theme =
        { theme with Primary = stateAppearance (Color.Grey "59") Color.White Color.White }

    let cautionary (theme: LC.Button.Theme) : LC.Button.Theme =
        { theme with Cautionary = stateAppearance Color.DevRed Color.White Color.White }

type Parameters = {
    Items: List<ContextMenuItem>
}

[<RequireQualifiedAccess>]
module private Styles =
    let dialogContents =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            FlexDirection.Column
            JustifyContent.FlexEnd
            padding 12
        }

    let scrollView =
        makeScrollViewStyles {
            flex -1
        }

    let divider =
        makeViewStyles {
            height 20
        }

    let heading =
        makeTextStyles {
            color Color.White
            TextAlign.Center
        }

type private DialogContent =
    [<Component>]
    static member Render(
            dialogProps: DialogProps<Parameters, unit>,
            parameters:  Parameters
        ) : ReactElement =
        let tryCancel (e: ReactEvent.Action) : unit =
            Dialogs.tryCancel dialogProps (fun () -> Async.Of true) DialogCloseMethod.HistoryBack e

        LC.Dialog.Base(
            contentPosition = Free,
            canClose        = When ([ OnEscape; OnBackground ], tryCancel),
            children =
                [|
                    Rn.View(
                        onPress = (fun e -> e.stopPropagation(); tryCancel (ReactEvent.Action.OfBrowserEvent e)),
                        styles  = [| Styles.dialogContents |],
                        children =
                            [|
                                Rn.ScrollView(
                                    vertical = true,
                                    styles   = [| Styles.scrollView |],
                                    children =
                                        [|
                                            castAsElement (
                                                parameters.Items
                                                |> List.map (fun item ->
                                                    match item with
                                                    | Divider ->
                                                        Rn.View(styles = [| Styles.divider |])
                                                    | Heading text ->
                                                        Rn.View(
                                                            children =
                                                                [|
                                                                    LC.UiText(
                                                                        value  = text,
                                                                        styles = [| Styles.heading |]
                                                                    )
                                                                |]
                                                        )
                                                    | InternalButton (label, isSelected, onPress) ->
                                                        LC.Button(
                                                            label   = label,
                                                            level   = Primary,
                                                            ?testId = Some (A11ySlug.testId "context-menu-item" label),
                                                            theme =
                                                                (if isSelected then
                                                                     ButtonThemes.normalSelected
                                                                 else
                                                                     ButtonThemes.normal),
                                                            state =
                                                                Button.PropStateFactory.MakeLowLevel (
                                                                    Button.Actionable (fun e -> (tryCancel e; onPress e))
                                                                )
                                                        )
                                                    | ButtonCautionary (label, onPress) ->
                                                        LC.Button(
                                                            label   = label,
                                                            level   = Cautionary,
                                                            ?testId = Some (A11ySlug.testId "context-menu-item" label),
                                                            theme   = ButtonThemes.cautionary,
                                                            state =
                                                                Button.PropStateFactory.MakeLowLevel (
                                                                    Button.Actionable (fun e -> (tryCancel e; onPress e))
                                                                )
                                                        )
                                                )
                                                |> Array.ofList
                                            )
                                        |]
                                )
                            |]
                    )
                |]
        )

let Open (items: List<ContextMenuItem>) (onResult: unit -> unit) (onCancel: unit -> unit) (close: DialogCloseMethod -> ReactEvent.Action -> unit) : ReactElement =
    doOpen
        "ContextMenu.Dialog"
        {
            Items = items
        }
        (fun dialogProps _ ->
            DialogContent.Render(dialogProps, dialogProps.Parameters)
        )
        {
            OnResult      = onResult
            MaybeOnCancel = Some onCancel
        }
        close
