[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Checkbox

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input

type private Helpers =
    [<Component>]
    static member BasicLabelSample() : ReactElement =
        let value = Hooks.useState (Some false)

        LC.Input.Checkbox(
            label = String "Accept terms",
            value = value.current,
            onChange = (fun isChecked -> value.update (Some isChecked)),
            validity = Valid,
            testId = "input-checkbox-accept-terms"
        )

    [<Component>]
    static member ChildrenLabelSample() : ReactElement =
        let value = Hooks.useState (Some true)

        LC.Input.Checkbox(
            value = value.current,
            onChange = (fun isChecked -> value.update (Some isChecked)),
            validity = Valid,
            accessibilityLabel = "Children-based Label",
            testId = "input-checkbox-children-based-label",
            children = [| LC.Text "Children-based Label" |]
        )

    [<Component>]
    static member ValidationSample() : ReactElement =
        let value = Hooks.useState None

        LC.Input.Checkbox(
            label = String "I want fries with that",
            value = value.current,
            onChange = (fun isChecked -> value.update (Some isChecked)),
            validity = Invalid "Required",
            testId = "input-checkbox-i-want-fries-with-that"
        )

type Ui.Content.Input with
    [<Component>]
    static member Checkbox() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.Checkbox",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Checkbox",
            notes = LC.Text "Label defaulting to Children is for backwards compatibility. When using Children for the visual label, pass accessibilityLabel and testId for a11y and automation.",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Input.Checkbox",
                    role = "checkbox",
                    namePattern = "label prop or ?accessibilityLabel when using children for the visible label",
                    stateNotes = "checked/unchecked via value; invalid when validity is Invalid",
                    scalesWithFont = true,
                    contrastNotes = "Checked/unchecked icon colors and error text meet WCAG AA"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.BasicLabelSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Checkbox(
    label = String "Accept terms",
    value = value,
    onChange = setValue,
    validity = Valid,
    testId = "input-checkbox-accept-terms"
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.ChildrenLabelSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Checkbox(
    value = value,
    onChange = setValue,
    validity = Valid,
    accessibilityLabel = "Children-based Label",
    testId = "input-checkbox-children-based-label",
    children = [| LC.Text "Children-based Label" |]
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Validation",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.ValidationSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Checkbox(
    label = String "I want fries with that",
    value = value,
    onChange = setValue,
    validity = Invalid "Required",
    testId = "input-checkbox-i-want-fries-with-that"
)"""
                                        )
                                )
                            }
                    )
                }
        )
