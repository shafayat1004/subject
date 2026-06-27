[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_PhoneNumber

open Fable.React
open LibClient
open LibClient.Components
open LC.Input.PhoneNumberTypes

type private Helpers =
    [<Component>]
    static member Sample(initialText: string, validity: InputValidity) : ReactElement =
        let value = Hooks.useState (parse (NonemptyString.ofString initialText))

        LC.Input.PhoneNumber(
            label    = "Home Number",
            value    = value.current,
            onChange = value.update,
            validity = validity
        )

type Ui.Content.Input with
    [<Component>]
    static member PhoneNumber() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.PhoneNumber",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.PhoneNumber",
            samples     =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "+880-155-5575-868", validity = Valid),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.PhoneNumber(
    label    = "Home Number",
    value    = value,
    onChange = setValue,
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "+1-555-9389-489", validity = Valid),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.PhoneNumber(
    label    = "Home Number",
    value    = value,
    onChange = setValue,
    validity = Valid
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Internal Validation",
                        notes   =
                            LC.Text "This component performs internal validation of the input, which has higher precedence for display than externally supplied validation, e.g. if the input is not a valid number, and the caller is also (for some reason) passing InputValidity.Missing, then it's the invalid number message that'll be displayed, and only once that's fixed will the externally provided Validity be shown.",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "-13", validity = Valid),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.PhoneNumber(
    label    = "Home Number",
    value    = value,
    onChange = setValue,
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "abc", validity = Valid),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.PhoneNumber(
    label    = "Home Number",
    value    = value,
    onChange = setValue,
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    notes   =
                                        LC.Text "Note that an empty field is not considered internally invalid. Doing so would result in a tonne of red fields when the form is empty, which would a horrible user experience. So instead, the component returns an Ok None in this case, and externally providing InputValidity.Missing when a form submission was attempted is the right way to deal with missing values.",
                                    visuals = Helpers.Sample(initialText = "", validity = Valid),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.PhoneNumber(
    label    = "Home Number",
    value    = value,
    onChange = setValue,
    validity = Valid
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "External Validation",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "+880-155-5575-868", validity = Missing),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.PhoneNumber(
    label    = "Home Number",
    value    = value,
    onChange = setValue,
    validity = Missing
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "+1-555-9389-489", validity = Invalid "This input is just bad"),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.PhoneNumber(
    label    = "Home Number",
    value    = value,
    onChange = setValue,
    validity = Invalid "This input is just bad"
)"""
                                        )
                                )
                            }
                    )
                }
        )
