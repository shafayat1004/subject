module LibClient.Components.Dialog.Confirm

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Dialog
open LibClient.Dialogs

module ShellStandard = LibClient.Components.Dialog.Shell.WhiteRounded.Standard

open ReactXP.Components
open ReactXP.Styles

type Button =
| Cancel       of Label: string * Components.Button.Level * (unit -> unit)
| Confirm      of Label: string * Components.Button.Level * (unit -> unit)
| AsyncConfirm of Label: string * Components.Button.Level * (unit -> Async<Result<unit, string>>)

type private Parameters = {
    MaybeHeading: Option<string>
    Details:      string
    Buttons:      List<Button>
}

[<RequireQualifiedAccess>]
type private ConfirmMode =
| Initial
| InProgress
| Error of Message: string

[<RequireQualifiedAccess>]
module private Styles =
    let details =
        makeViewStyles {
            minWidth 300
        }

    let detailsText =
        makeTextStyles {
            color (Color.Grey "66")
        }

module private Helpers =
    let dialogLabel (maybeHeading: Option<string>) (details: string) =
        maybeHeading |> Option.defaultValue details

    let buttonTestId (label: string) =
        Some (A11ySlug.testId "dialog-confirm" label)

    let renderButton
            (mode: ConfirmMode)
            (tryCancel: ReactEvent.Action -> unit)
            (hide: ReactEvent.Action -> unit)
            (asyncConfirm: (unit -> Async<Result<unit, string>>) -> ReactEvent.Action -> unit)
            (button: Button)
        : ReactElement =
        match button with
        | Cancel (label, level, callback) ->
            LC.Button(
                label = label,
                level = level,
                state =
                    ButtonHighLevelState.LowLevel (
                        match mode with
                        | ConfirmMode.InProgress -> ButtonLowLevelState.Disabled
                        | _ -> ButtonLowLevelState.Actionable (fun e -> callback(); tryCancel e)
                    ),
                ?testId = buttonTestId label
            )
        | Confirm (label, level, callback) ->
            LC.Button(
                label = label,
                level = level,
                state =
                    ButtonHighLevelState.LowLevel (
                        match mode with
                        | ConfirmMode.InProgress -> ButtonLowLevelState.Disabled
                        | _ -> ButtonLowLevelState.Actionable (fun e -> callback(); hide e)
                    ),
                ?testId = buttonTestId label
            )
        | AsyncConfirm (label, level, work) ->
            LC.Button(
                label = label,
                level = level,
                state =
                    ButtonHighLevelState.LowLevel (
                        match mode with
                        | ConfirmMode.InProgress -> ButtonLowLevelState.InProgress
                        | _ -> ButtonLowLevelState.Actionable (asyncConfirm work)
                    ),
                ?testId = buttonTestId label
            )

type private ConfirmContent =
    [<Component>]
    static member Render(
            dialogProps: DialogProps<Parameters, unit>,
            parameters: Parameters
        ) : ReactElement =
        let modeHook = Hooks.useState ConfirmMode.Initial

        Hooks.useEffect(
            (fun () ->
                LC.LiveRegion.announce
                    (Helpers.dialogLabel parameters.MaybeHeading parameters.Details)
                    LibClient.Accessibility.AccessibilityLiveRegion.Polite
            ),
            [| box parameters.MaybeHeading; box parameters.Details |]
        )

        let canCancel () = async { return modeHook.current <> ConfirmMode.InProgress }

        let tryCancel (e: ReactEvent.Action) =
            Dialogs.tryCancel dialogProps canCancel DialogCloseMethod.HistoryBack e

        let hide (e: ReactEvent.Action) =
            Dialogs.hide dialogProps DialogCloseMethod.HistoryBack e

        let asyncConfirm (work: unit -> Async<Result<unit, string>>) (e: ReactEvent.Action) =
            modeHook.update ConfirmMode.InProgress

            async {
                match! work() with
                | Error message -> modeHook.update (ConfirmMode.Error message)
                | Ok _ -> Dialogs.hide dialogProps DialogCloseMethod.HistoryBack e
            }
            |> startSafely

        let shellMode : LibClient.Components.Dialog.Shell.WhiteRounded.Standard.Mode =
            match modeHook.current with
            | ConfirmMode.InProgress -> ShellStandard.Mode.InProgress
            | ConfirmMode.Error message -> ShellStandard.Mode.Error message
            | _ -> ShellStandard.Mode.Default

        let buttons =
            parameters.Buttons
            |> List.map (Helpers.renderButton modeHook.current tryCancel hide asyncConfirm)
            |> List.toArray

        LC.Dialog.Shell.WhiteRounded.Standard(
            canClose = ShellStandard.Never,
            mode = shellMode,
            ?heading = parameters.MaybeHeading,
            accessibilityLabel = Helpers.dialogLabel parameters.MaybeHeading parameters.Details,
            body =
                element {
                    RX.View(
                        styles = [| Styles.details |],
                        children =
                            elements {
                                LC.UiText(
                                    value = parameters.Details,
                                    styles = [| Styles.detailsText |]
                                )
                            }
                    )
                },
            buttons = castAsElementAckingKeysWarning buttons
        )

// NOTE we can't take Parameters here because of circular dependency with DialogsInterface/DialogsImplementation
let Open (maybeHeading: Option<string>) (details: string) (buttons: List<Button>) (close: DialogCloseMethod -> ReactEvent.Action -> unit) : ReactElement =
    doOpen
        "DialogConfirm"
        {
            MaybeHeading = maybeHeading
            Details = details
            Buttons = buttons
        }
        (fun dialogProps _ ->
            ConfirmContent.Render(
                dialogProps,
                dialogProps.Parameters
            )
        )
        {
            OnResult = NoopFn
            MaybeOnCancel = None
        }
        close

let OpenSimple (maybeHeading: Option<string>) (details: string) (cancelLabel: string) (okLabel: string) (onResult: bool -> unit) (close: DialogCloseMethod -> ReactEvent.Action -> unit) : ReactElement =
    Open
        maybeHeading
        details
        [
            Button.Cancel (cancelLabel, Components.Button.Level.Secondary, fun () -> onResult false)
            Button.Confirm (okLabel, Components.Button.Level.Primary, fun () -> onResult true)
        ]
        close

let OpenSimpleAsync (maybeHeading: Option<string>) (details: string) (cancelLabel: string) (okLabel: string) (onConfirm: unit -> Async<Result<unit, string>>) (close: DialogCloseMethod -> ReactEvent.Action -> unit) : ReactElement =
    Open
        maybeHeading
        details
        [
            Button.Cancel (cancelLabel, Components.Button.Level.Secondary, ignore)
            Button.AsyncConfirm (okLabel, Components.Button.Level.Primary, onConfirm)
        ]
        close

let OpenAsAlert (maybeHeading: Option<string>) (details: string) (close: DialogCloseMethod -> ReactEvent.Action -> unit) : ReactElement =
    Open
        maybeHeading
        details
        [Button.Cancel ("OK", Components.Button.Level.Primary, ignore)]
        close
