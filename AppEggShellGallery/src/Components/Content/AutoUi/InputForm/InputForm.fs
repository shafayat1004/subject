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
    [<Component>]
    let Render () : ReactElement =
        let validationResultHook = Hooks.useState (None: Option<InputValidationResult<SampleRecord>>)

        let formWrapper = FormWrapper.Make<SampleRecord> "SampleRecord"

        element {
            UIAuto.InputForm(
                formWrapper = formWrapper,
                settings = List.empty,
                onChange = (fun result -> validationResultHook.update (Some result))
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
            props = ComponentContent.ForFullyQualifiedName "LibAutoUi.Components.InputForm",
            notes =
                element {
                    LC.UiText(
                        value =
                            "LibAutoUi.InputForm builds an editable form from an F# type via reflection. "
                            + "Primitive fields map to registered input components (string, date/time, etc.)."
                    )
                },
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
