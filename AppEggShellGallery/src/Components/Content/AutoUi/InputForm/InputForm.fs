[<AutoOpen>]
module AppEggShellGallery.Components.Content_AutoUi_InputForm

open System
open Fable.React
open LibClient
open LibClient.Components
open LibAutoUi.Components.InputForm
open LibAutoUi.Components.Constructors
open LibAutoUi.Types

type SampleRecord = {
    Name:      string
    CreatedAt: DateTimeOffset
}

module private Sample =
    let formWrapper = FormWrapper.Make<SampleRecord> "SampleRecord"

    [<Component>]
    let Render () : ReactElement =
        let validationResultHook = Hooks.useState (None: Option<InputValidationResult<SampleRecord>>)

        let onChange =
            Hooks.useMemo(
                (fun () -> fun (result: InputValidationResult<SampleRecord>) -> validationResultHook.update (Some result)),
                [||]
            )

        element {
            UIAuto.InputForm(
                formWrapper = formWrapper,
                settings = List.empty,
                onChange = onChange
            )

            match validationResultHook.current with
            | None ->
                LC.UiText(value = "Fill in the fields above.")
            | Some (Ok value) ->
                LC.UiText(value = sprintf "Valid: %s at %O" value.Name value.CreatedAt)
            | Some (Error failure) ->
                LC.UiText(value = sprintf "Validation error: %A" failure)
        }

type Ui.Content with
    [<Component>]
    static member AutoUi_InputForm () : ReactElement =
        Ui.ComponentContent(
            displayName = "AutoUi InputForm",
            props =
                ComponentContent.Manual(
                    element {
                        Ui.Code(
                            language = ComponentSample.Fsharp,
                            children =
                                [| LC.Text """
UIAuto.InputForm(
    formWrapper: FormWrapper<'T>,
    settings: Settings,
    onChange: InputValidationResult<'T> -> unit,
    ?key: string
)
""" |]
                        )
                    }
                ),
            notes =
                element {
                    LC.UiText(
                        value =
                            "LibAutoUi.InputForm builds an editable form from an F# type via reflection. "
                            + "Primitive fields map to registered input components (string, date/time, etc.)."
                    )
                },
            a11y =
                Ui.A11yPanel(
                    componentName = "UIAuto.InputForm",
                    role = "none (auto-generated form)",
                    namePattern = "Field names from F# record properties via reflection",
                    stateNotes = "Validation surfaced via onChange InputValidationResult",
                    scalesWithFont = true
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        verticalAlignment = ComponentSample.VerticalAlignment.Top,
                        visuals = Sample.Render(),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
type SampleRecord = { Name: string; CreatedAt: DateTimeOffset }

let formWrapper = FormWrapper.Make<SampleRecord> "SampleRecord"

UIAuto.InputForm(
    formWrapper = formWrapper,
    settings = List.empty,
    onChange = fun result -> ...
)
""" |]
                                    )
                                }
                            )
                    )
                }
        )
