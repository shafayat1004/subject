[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Quantity

open Fable.React
open LibClient
open LibClient.Components
open LC.Input.QuantityTypes

type private Helpers =
    [<Component>]
    static member CanRemoveSample(initialValue: Option<PositiveInteger>, validity: InputValidity, ?max: PositiveInteger) : ReactElement =
        let value = Hooks.useState initialValue

        LC.Input.Quantity(
            value    = value.current,
            onChange = CanRemove (fun v _e -> value.update v),
            validity = validity,
            ?max     = max
        )

    [<Component>]
    static member CannotRemoveSample(initialValue: Option<PositiveInteger>, validity: InputValidity, ?max: PositiveInteger) : ReactElement =
        let value = Hooks.useState initialValue

        LC.Input.Quantity(
            value    = value.current,
            onChange = CannotRemove (fun v _e -> value.update (Some v)),
            validity = validity,
            ?max     = max
        )

type Ui.Content.Input with
    [<Component>]
    static member Quantity() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.Quantity",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Quantity",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Input.Quantity",
                    role = "text field (numeric with unit)",
                    namePattern = "Floating label text; unit suffix visible in field",
                    stateNotes = "Internal validation errors take precedence; Invalid/Missing validity surfaces error text",
                    scalesWithFont = true,
                    contrastNotes = "Label, input text, and error colors meet WCAG AA"
                ),
            samples     =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.CanRemoveSample(initialValue = Some (PositiveInteger.ofLiteral 1), validity = Valid),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Quantity(
    value    = value,
    onChange = CanRemove setValue,
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.CannotRemoveSample(initialValue = Some (PositiveInteger.ofLiteral 1), validity = Valid),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Quantity(
    value    = value,
    onChange = CannotRemove (fun v _ -> setValue (Some v)),
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    notes   =
                                        LC.Text "The Max prop can only enforce that the plus button is not visible and that OnChange is never called with a value greater than Max, but it cannot prevent Value from containing a value greater than Max, since that is managed externally. So external validation is necessary.",
                                    visuals =
                                        Helpers.CannotRemoveSample(
                                            initialValue = Some (PositiveInteger.ofLiteral 5),
                                            validity     = Valid,
                                            max          = PositiveInteger.ofLiteral 5
                                        ),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Quantity(
    value    = value,
    max      = PositiveInteger.ofLiteral 5,
    onChange = CannotRemove (fun v _ -> setValue (Some v)),
    validity = Valid
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Validation (hardcoded, not dynamic)",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.CanRemoveSample(initialValue = None, validity = Missing),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Quantity(
    value    = value,
    validity = Missing,
    onChange = CanRemove setValue
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        Helpers.CanRemoveSample(
                                            initialValue = Some (PositiveInteger.ofLiteral 6),
                                            validity     = Invalid "Not allowed so many",
                                            max          = PositiveInteger.ofLiteral 5
                                        ),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Quantity(
    value    = value,
    validity = Invalid "Not allowed so many",
    max      = PositiveInteger.ofLiteral 5,
    onChange = CanRemove setValue
)"""
                                        )
                                )
                            }
                    )
                }
        )
