module LibAutoUi.Components.DialogInputForm

open Fable.React

open LibClient
open LibClient.Components
open LibClient.Components.Dialog.Shell.WhiteRounded
open LibClient.Dialogs
open LibAutoUi.Components.InputForm
open LibAutoUi.Types

open ReactXP.Components
open ReactXP.Styles

type Parameters<'T> = {
    FormWrapper: FormWrapper<'T>
    Settings:    Settings
}

[<RequireQualifiedAccess>]
module private Styles =
    let dialogContents =
        makeViewStyles {
            minWidth 200
            minHeight 200
            Overflow.Visible
            JustifyContent.SpaceBetween
        }

    let buttons =
        makeViewStyles {
            FlexDirection.Row
            JustifyContent.Center
            marginTop 12
        }

type private DialogInputFormContent =
    [<Component>]
    static member Render<'T>(
            dialogProps: DialogProps<Parameters<'T>, 'T>,
            parameters:  Parameters<'T>
        ) : ReactElement =
        let maybeValueHook = Hooks.useState None

        let canCancel () = async { return true }

        let tryCancel (e: ReactEvent.Action) =
            Dialogs.tryCancel dialogProps canCancel DialogCloseMethod.HistoryBack e

        let onChange (result: InputValidationResult<'T>) =
            maybeValueHook.update (result |> Result.toOption)

        let submitResult (result: 'T) (_e: ReactEvent.Action) =
            dialogProps.ResponseChannel.OnResult result

        let stopPropagation (e: Browser.Types.PointerEvent) =
            e.stopPropagation()

        LC.Dialog.Shell.WhiteRounded.Base(
            canClose = Base.CanClose.When ([Base.OnBackground], tryCancel),
            children =
                [|
                    RX.View(
                        onPress = stopPropagation,
                        styles = [| Styles.dialogContents |],
                        children =
                            elements {
                                UIAuto.InputForm(
                                    formWrapper = parameters.FormWrapper,
                                    settings = parameters.Settings,
                                    onChange = onChange
                                )

                                RX.View(
                                    styles = [| Styles.buttons |],
                                    children =
                                        elements {
                                            LC.Button(
                                                label = "Cancel",
                                                level = Button.Level.Secondary,
                                                state =
                                                    ButtonHighLevelState.LowLevel (
                                                        ButtonLowLevelState.Actionable tryCancel
                                                    )
                                            )

                                            LC.Button(
                                                label = "Execute",
                                                level = Button.Level.Primary,
                                                state =
                                                    ButtonHighLevelState.LowLevel (
                                                        match maybeValueHook.current with
                                                        | None        -> ButtonLowLevelState.Disabled
                                                        | Some value  -> ButtonLowLevelState.Actionable (submitResult value)
                                                    )
                                            )
                                        }
                                )
                            }
                    )
                |]
        )

// NOTE we can't take Parameters here because of circular dependency with DialogsInterface/DialogsImplementation
let Open<'T>
        (formWrapper: FormWrapper<'T>)
        (settings:    Settings)
        (onResult:    'T -> unit)
        (close:       DialogCloseMethod -> ReactEvent.Action -> unit)
    : ReactElement =
    doOpen
        "DialogInputForm"
        {
            FormWrapper = formWrapper
            Settings    = settings
        }
        (fun dialogProps _ ->
            DialogInputFormContent.Render(
                dialogProps,
                dialogProps.Parameters
            )
        )
        {
            OnResult      = onResult
            MaybeOnCancel = None
        }
        close
