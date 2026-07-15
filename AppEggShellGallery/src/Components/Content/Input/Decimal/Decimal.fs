[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Decimal

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input.Decimal

type private Helpers =
    [<Component>]
    static member Sample(initialText: string, validity: InputValidity) : ReactElement =
        let value = Hooks.useState (parse (NonemptyString.ofString initialText))
        LC.Input.Decimal(
            label    = "Price",
            value    = value.current,
            validity = validity,
            onChange = value.update
        )

type Ui.Content.Input with
    [<Component>]
    static member Decimal () : ReactElement =
        Ui.ComponentContent (
            displayName = "Input.Decimal",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Decimal",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Input.Decimal",
                    role           = "text field (numeric)",
                    namePattern    = "Floating label text",
                    stateNotes     = "Internal validation errors take precedence; Invalid/Missing validity surfaces error text",
                    scalesWithFont = true,
                    contrastNotes  = "Label, input text, and error colors meet WCAG AA"
                ),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "3.14", validity = Valid),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.Decimal(
    label    = "Price",
    value    = value,
    validity = Valid,
    onChange = setValue
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Internal Validation",
                        notes   = LC.Text "This component performs internal validation of the input, which has higher precedence for display than externally supplied validation. e.g. if the input is non-numeric and the caller is also passing InputValidity.Missing, then it's the non-numeric error message that'll be displayed, and only once that's fixed will the externally provided Validity be shown. Note that an empty field is not considered internally invalid — the component returns Ok None so the caller can supply Missing on form submission.",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Non-numeric input",
                                    visuals = Helpers.Sample(initialText = "thirteen", validity = Valid),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """LC.Input.Decimal(label = "Price", value = value, validity = Valid, onChange = setValue)
// "thirteen" -> internal error: Only numbers and period allowed""")
                                )

                                Ui.ComponentSample(
                                    heading = "Negative number",
                                    visuals = Helpers.Sample(initialText = "-13", validity = Valid),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """LC.Input.Decimal(label = "Price", value = value, validity = Valid, onChange = setValue)
// "-13" -> parses as Ok (Some -13M) (negatives are allowed for Decimal)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Empty field (no error)",
                                    visuals = Helpers.Sample(initialText = "", validity = Valid),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """LC.Input.Decimal(label = "Price", value = value, validity = Valid, onChange = setValue)
// empty -> Ok None (no internal error)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "External Validation",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Missing",
                                    visuals = Helpers.Sample(initialText = "12", validity = Missing),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.Decimal(
    label    = "Price",
    value    = value,
    validity = Missing,
    onChange = setValue
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Invalid with message",
                                    visuals = Helpers.Sample(initialText = "11", validity = Invalid "This input is just bad"),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.Decimal(
    label    = "Price",
    value    = value,
    validity = Invalid "This input is just bad",
    onChange = setValue
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
