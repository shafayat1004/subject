[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_LocalTime

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input.LocalTime

type private Helpers =
    [<Component>]
    static member InteractiveSample() : ReactElement =
        let value = Hooks.useState empty
        LC.Input.LocalTime(
            label    = "Start Time",
            value    = value.current,
            validity = Valid,
            onChange = value.update
        )

type Ui.Content.Input with
    [<Component>]
    static member LocalTime() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.LocalTime",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.LocalTime",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Input.LocalTime",
                    role           = "text field with time picker",
                    namePattern    = "Floating label text",
                    stateNotes     = "Invalid/Missing validity surfaces error text below the field",
                    scalesWithFont = true,
                    contrastNotes  = "Label, input text, and error colors meet WCAG AA"
                ),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        notes   = LC.Text "LocalTime input uses 12-hour format: separate hour (1–12) and minute (0–59) fields with an AM/PM picker. A complete time is available in Value.Result only when both fields are filled and valid.",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.InteractiveSample(),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.LocalTime(
    label    = "Start Time",
    value    = value,
    validity = Valid,
    onChange = setValue
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Validation (hardcoded, not dynamic)",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Missing",
                                    visuals = LC.Input.LocalTime(
                                        label    = "Start Time",
                                        value    = empty,
                                        validity = Missing,
                                        onChange = ignore
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.LocalTime(
    label    = "Start Time",
    value    = LocalTime.empty,
    validity = Missing,
    onChange = ignore
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Invalid with message",
                                    visuals = LC.Input.LocalTime(
                                        label    = "Start Time",
                                        value    = empty,
                                        validity = Invalid "That is just a bad time",
                                        onChange = ignore
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.LocalTime(
    label    = "Start Time",
    value    = LocalTime.empty,
    validity = Invalid "That is just a bad time",
    onChange = ignore
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
