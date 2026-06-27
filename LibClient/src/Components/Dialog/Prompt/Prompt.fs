module LibClient.Components.Dialog.Prompt

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Dialog
open LibClient.Components.Form_Base
open LibClient.Components.Form_Base.Types
open LibClient.Dialogs

open ReactXP.Components
open ReactXP.Styles

type private Parameters = {
    MaybeHeading: Option<string>
    Details:      string
    OnResult:     Option<NonemptyString> -> unit
}

[<RequireQualifiedAccess>]
type private Field = Value

type private Acc = {
    Value: Option<NonemptyString>
} with
    static member Initial : Acc = {
        Value = None
    }

    interface AbstractAcc<Field, NonemptyString> with
        member this.Validate () : Result<NonemptyString, ValidationErrors<Field>> = validateForm {
            let! value = Forms.GetFieldValue2 this.Value Field.Value
            return value
        }

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

type private PromptContent =
    [<Component>]
    static member Render(
            dialogProps: DialogProps<Parameters, unit>,
            parameters: Parameters
        ) : ReactElement =
        Hooks.useEffect(
            (fun () ->
                LC.LiveRegion.announce
                    (Helpers.dialogLabel parameters.MaybeHeading parameters.Details)
                    LibClient.Accessibility.AccessibilityLiveRegion.Polite
            ),
            [| box parameters.MaybeHeading; box parameters.Details |]
        )

        let tryCancel (e: ReactEvent.Action) =
            Dialogs.tryCancel dialogProps (fun () -> Async.Of true) DialogCloseMethod.HistoryBack e
            parameters.OnResult None

        let submit (value: NonemptyString) (e: ReactEvent.Action) () : UDActionResult =
            async {
                parameters.OnResult (Some value)
                Dialogs.tryCancel dialogProps (fun () -> Async.Of true) DialogCloseMethod.HistoryBack e
                return Ok ()
            }

        Constructors.LC.Form.Base(
            accumulator = Accumulator.ManageInternallyInitializingWith Acc.Initial,
            submit = submit,
            content =
                fun form ->
                    LC.Dialog.Shell.WhiteRounded.Standard(
                        canClose = Shell.WhiteRounded.Standard.Never,
                        ?heading = parameters.MaybeHeading,
                        accessibilityLabel = Helpers.dialogLabel parameters.MaybeHeading parameters.Details,
                        body =
                            RX.View(
                                children =
                                    [|
                                        RX.View(
                                            styles = [| Styles.details |],
                                            children =
                                                [|
                                                    LC.UiText(
                                                        value = parameters.Details,
                                                        styles = [| Styles.detailsText |]
                                                    )
                                                |]
                                        )
                                        LC.Input.Text(
                                            label = "Value",
                                            validity = form.FieldValidity Field.Value,
                                            value = form.Acc.Value,
                                            onChange =
                                                fun value ->
                                                    form.UpdateAcc (fun acc -> { acc with Value = value })
                                        )
                                    |]
                            ),
                        buttons =
                            RX.View(
                                children =
                                    [|
                                        LC.Button(
                                            label = "Cancel",
                                            level = Components.Button.Level.Secondary,
                                            state =
                                                ButtonHighLevelState.LowLevel (
                                                    ButtonLowLevelState.Actionable tryCancel
                                                ),
                                            ?testId = Some (A11ySlug.testId "dialog-prompt" "Cancel")
                                        )
                                        LC.Button(
                                            label = "Submit",
                                            state =
                                                ButtonHighLevelState.LowLevel (
                                                    if form.IsSubmitInProgress then
                                                        ButtonLowLevelState.InProgress
                                                    else
                                                        ButtonLowLevelState.Actionable form.TrySubmitLowLevel
                                                ),
                                            ?testId = Some (A11ySlug.testId "dialog-prompt" "Submit")
                                        )
                                    |]
                            )
                    )
        )

// NOTE we can't take Parameters here because of circular dependency with DialogsInterface/DialogsImplementation
let Open (maybeHeading: Option<string>) (details: string) (onResult: Option<NonemptyString> -> unit) (close: DialogCloseMethod -> ReactEvent.Action -> unit) : ReactElement =
    doOpen
        "Prompt"
        {
            MaybeHeading = maybeHeading
            Details = details
            OnResult = onResult
        }
        (fun dialogProps _ ->
            PromptContent.Render(
                dialogProps,
                dialogProps.Parameters
            )
        )
        {
            OnResult = NoopFn
            MaybeOnCancel = None
        }
        close
