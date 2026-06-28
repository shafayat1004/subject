[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Text

open Fable.React
open LibClient
open LibClient.Accessibility
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
        ?requestFocusOnMount: bool,
        ?testId: string,
        ?prefixIcon: LibClient.Icons.IconConstructor,
        ?accessibilityRole: AccessibilityRole,
        ?accessibilityLabel: string
    ) : ReactElement =
        let value = Hooks.useState (NonemptyString.ofString initialText)
        let resolvedTestId = testId |> Option.defaultValue (A11ySlug.testId "input" label)

        LC.Input.Text(
            label               = label,
            value               = value.current,
            validity            = validity,
            onChange            = value.update,
            testId              = resolvedTestId,
            ?multiline          = multiline,
            ?placeholder         = placeholder,
            ?prefix              = prefix,
            ?suffix              = suffix,
            ?requestFocusOnMount = requestFocusOnMount,
            ?prefixIcon          = prefixIcon,
            ?accessibilityRole   = accessibilityRole,
            ?accessibilityLabel  = accessibilityLabel
        )

type Ui.Content.Input with
    [<Component>]
    static member Text() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.Text",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Text",
            notes       =
                LC.Text "This component is a work in progress. If there's a feature that's not already supported, check if RX.TextInput supports it, and if so, plumb it through. Decorative prefixIcon is hidden from screen readers (importantForAccessibility = No).",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Input.Text",
                    role = "text field (default); search when ?accessibilityRole = AccessibilityRole.Search",
                    namePattern = "Floating label text; override with ?accessibilityLabel",
                    stateNotes = "Invalid/Missing validity surfaces error text below the field",
                    scalesWithFont = true,
                    contrastNotes = "Label, input text, and error colors meet WCAG AA; invalid state adds error icon plus text"
                ),
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
    testId              = A11ySlug.testId "input" "Name",
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
                        heading = "Search",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.Sample(
                                            label               = "Search",
                                            initialText         = "",
                                            validity            = Valid,
                                            placeholder         = "Search todos",
                                            prefixIcon          = Icon.MagnifyingGlass,
                                            accessibilityRole   = AccessibilityRole.Search,
                                            accessibilityLabel  = "Search todos"
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Text(
    label              = "Search",
    value              = value,
    validity           = Valid,
    onChange           = setValue,
    placeholder        = "Search todos",
    prefixIcon         = Icon.MagnifyingGlass,
    accessibilityRole  = AccessibilityRole.Search,
    accessibilityLabel = "Search todos"
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
