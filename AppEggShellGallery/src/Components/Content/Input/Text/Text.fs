[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Text

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Icons

type private Helpers =
    [<Component>]
    static member Sample(
        label: string,
        initialText: string,
        validity: InputValidity,
        ?multiline: bool,
        ?placeholder: string,
        ?prefix: string,
        ?suffix: InputSuffix,
        ?requestFocusOnMount: bool
    ) : ReactElement =
        let value = Hooks.useState (NonemptyString.ofString initialText)

        LC.Input.Text(
            label               = label,
            value               = value.current,
            validity            = validity,
            onChange            = value.update,
            ?multiline          = multiline,
            ?placeholder         = placeholder,
            ?prefix              = prefix,
            ?suffix              = suffix,
            ?requestFocusOnMount = requestFocusOnMount
        )

type Ui.Content.Input with
    [<Component>]
    static member Text() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.Text",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Text",
            notes       =
                LC.Text "This component is a work in progress. If there's a feature that's not already supported, check if RX.TextInput supports it, and if so, plumb it through.",
            samples     =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label               = "Name",
                                            initialText         = "Momo",
                                            validity            = Valid,
                                            requestFocusOnMount = true
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label               = "Name",
    value               = value,
    validity            = Valid,
    requestFocusOnMount = true,
    onChange            = setValue
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Fruit",
                                            initialText = "Mango\nBanana\nApple",
                                            validity    = Valid,
                                            multiline   = true
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Fruit",
    value    = value,
    validity = Valid,
    onChange = setValue,
    multiline = true
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Title",
                                            initialText = "",
                                            validity    = Valid,
                                            placeholder = "e.g. Kafka on the Shore"
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label       = "Title",
    value       = value,
    validity    = Valid,
    onChange    = setValue,
    placeholder = "e.g. Kafka on the Shore"
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Prefix",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Price",
                                            initialText = "",
                                            validity    = Valid,
                                            prefix      = "$"
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Price",
    prefix   = "$",
    value    = value,
    validity = Valid,
    onChange = setValue
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Price",
                                            initialText = "13.42",
                                            validity    = Valid,
                                            prefix      = "$"
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Price",
    prefix   = "$",
    value    = value,
    validity = Valid,
    onChange = setValue
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Suffix",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Portion Size",
                                            initialText = "",
                                            validity    = Valid,
                                            suffix      = InputSuffix.Text "Persons"
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Portion Size",
    suffix   = InputSuffix.Text "Persons",
    value    = value,
    validity = Valid,
    onChange = setValue
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Portion Size",
                                            initialText = "6",
                                            validity    = Valid,
                                            suffix      = InputSuffix.Text "Persons"
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Portion Size",
    suffix   = InputSuffix.Text "Persons",
    value    = value,
    validity = Valid,
    onChange = setValue
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Portion Size",
                                            initialText = "6",
                                            validity    = Valid,
                                            suffix      = InputSuffix.Icon Icon.MagnifyingGlass
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Portion Size",
    suffix   = InputSuffix.Icon Icon.MagnifyingGlass,
    value    = value,
    validity = Valid,
    onChange = setValue
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
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Name",
                                            initialText = "Table",
                                            validity    = Missing
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Name",
    value    = value,
    validity = Missing,
    onChange = setValue
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label       = "Name",
                                            initialText = "Chair",
                                            validity    = Invalid "Something is wrong with this value"
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label    = "Name",
    value    = value,
    validity = Invalid "Something is wrong with this value",
    onChange = setValue
)"""
                                        )
                                )
                            }
                    )
                }
        )
