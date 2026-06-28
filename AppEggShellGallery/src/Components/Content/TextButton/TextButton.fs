[<AutoOpen>]
module AppEggShellGallery.Components.Content_TextButton

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.TextButton
open AppEggShellGallery

module private SampleThemes =
    let special (theme: LC.TextButton.Theme) : LC.TextButton.Theme =
        { theme with
            Primary =
                { theme.Primary with
                    Actionable =
                        { theme.Primary.Actionable with
                            TextColor = Color.DevOrange
                        }
                }
        }

type private Helpers =
    [<Component>]
    static member RadioRoleSample() : ReactElement =
        let selected = Hooks.useState "Standard"

        LC.RadioGroup(
            label = "Shipping speed",
            children =
                [|
                    LC.TextButton(
                        label = "Standard",
                        role = AccessibilityRole.Radio,
                        accessibilityState = AccessibilityStateRecord.selected (selected.current = "Standard"),
                        state =
                            PropStateFactory.MakeLowLevel (
                                Actionable (fun _ -> selected.update "Standard")
                            )
                    )
                    LC.TextButton(
                        label = "Express",
                        role = AccessibilityRole.Radio,
                        accessibilityState = AccessibilityStateRecord.selected (selected.current = "Express"),
                        state =
                            PropStateFactory.MakeLowLevel (
                                Actionable (fun _ -> selected.update "Express")
                            )
                    )
                |]
        )

type Ui.Content with
    [<Component>]
    static member TextButton() : ReactElement =
        Ui.ComponentContent(
            displayName = "TextButton",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.TextButton",
            notes =
                LC.Text "Actionable text buttons auto-slug testId from the label (text-button-add-to-cart). Pass ?testId to override. Minimum tap target is 44px (padding applied by the component).",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.TextButton",
                    role = "button (default); radio when ?role = AccessibilityRole.Radio inside LC.RadioGroup",
                    namePattern = "Visible label text (label prop)",
                    stateNotes = "disabled when MakeDisabled; busy when InProgress; selected when ?accessibilityState.Selected = Some true (Radio role)",
                    scalesWithFont = true,
                    contrastNotes = "Primary theme text color meets WCAG AA on typical backgrounds"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Add to Cart",
                                state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TextButton(
    label = "Add to Cart",
    state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction)
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Add to Cart",
                                state = PropStateFactory.MakeLowLevel InProgress
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TextButton(
    label = "Add to Cart",
    state = PropStateFactory.MakeLowLevel InProgress
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Add to Cart",
                                state = PropStateFactory.MakeDisabled
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.TextButton(
    label = "Add to Cart",
    state = PropStateFactory.MakeDisabled
)"""
                            )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Radio role",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.RadioRoleSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let selected = Hooks.useState "Standard"

LC.RadioGroup(
    label = "Shipping speed",
    children = [|
        LC.TextButton(
            label = "Standard",
            role = AccessibilityRole.Radio,
            accessibilityState = AccessibilityStateRecord.selected (selected.current = "Standard"),
            state = PropStateFactory.MakeLowLevel (Actionable (fun _ -> selected.update "Standard"))
        )
        LC.TextButton(
            label = "Express",
            role = AccessibilityRole.Radio,
            accessibilityState = AccessibilityStateRecord.selected (selected.current = "Express"),
            state = PropStateFactory.MakeLowLevel (Actionable (fun _ -> selected.update "Express"))
        )
    |]
)"""
                                        )
                                )
                            }
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.TextButton(
                                label = "Special Add to Cart",
                                state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction),
                                theme = SampleThemes.special
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.TextButton(
    label = "Special Add to Cart",
    state = PropStateFactory.MakeLowLevel (Actionable Actions.demoPointerEventAction),
    theme = SampleThemes.special
)""" |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Theme",
                                        children =
                                            [| LC.Text """
let special (theme: LC.TextButton.Theme) =
    { theme with
        Primary =
            { theme.Primary with
                Actionable =
                    { theme.Primary.Actionable with TextColor = Color.DevOrange }
            }
    }
""" |]
                                    )
                                }
                            )
                    )
                }
        )
