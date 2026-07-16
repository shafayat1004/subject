[<AutoOpen>]
module AppEggShellGallery.Components.Content_Accessibility_Group

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.TextButton
open AppEggShellGallery

type private Helpers =
    [<Component>]
    static member GroupSample() : ReactElement =
        LC.Group(
            label = "Shipping address",
            children =
                [|
                    LC.UiText "123 Main Street"
                    LC.UiText "Springfield"
                |]
        )

    [<Component>]
    static member RadioGroupSample() : ReactElement =
        let selected = Hooks.useState "Email"

        LC.RadioGroup(
            label = "Contact method",
            children =
                [|
                    LC.TextButton(
                        label              = "Email",
                        role               = AccessibilityRole.Radio,
                        accessibilityState = AccessibilityStateRecord.selected (selected.current = "Email"),
                        state =
                            PropStateFactory.MakeLowLevel (
                                Actionable (fun _ -> selected.update "Email")
                            )
                    )
                    LC.TextButton(
                        label              = "Phone",
                        role               = AccessibilityRole.Radio,
                        accessibilityState = AccessibilityStateRecord.selected (selected.current = "Phone"),
                        state =
                            PropStateFactory.MakeLowLevel (
                                Actionable (fun _ -> selected.update "Phone")
                            )
                    )
                |]
        )

type Ui.Content.Accessibility with
    [<Component>]
    static member Group() : ReactElement =
        Ui.ComponentContent(
            displayName = "Group / RadioGroup",
            notes =
                LC.Text "LC.Group names a related set of elements. LC.RadioGroup names a set of mutually exclusive choices; pair with controls using role = AccessibilityRole.Radio.",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Group / LC.RadioGroup",
                    role           = "group (LC.Group); radiogroup (LC.RadioGroup)",
                    namePattern    = "label or ?accessibilityLabel on LC.Group; label on LC.RadioGroup",
                    stateNotes     = "Group is static; radio children expose selected state",
                    scalesWithFont = true
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Group",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.GroupSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Group(
    label = "Shipping address",
    children = [|
        LC.UiText "123 Main Street"
        LC.UiText "Springfield"
    |]
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "RadioGroup",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.RadioGroupSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let selected = Hooks.useState "Email"

LC.RadioGroup(
    label = "Contact method",
    children = [|
        LC.TextButton(
            label = "Email",
            role = AccessibilityRole.Radio,
            accessibilityState = AccessibilityStateRecord.selected (selected.current = "Email"),
            state = PropStateFactory.MakeLowLevel (Actionable (fun _ -> selected.update "Email"))
        )
        LC.TextButton(
            label = "Phone",
            role = AccessibilityRole.Radio,
            accessibilityState = AccessibilityStateRecord.selected (selected.current = "Phone"),
            state = PropStateFactory.MakeLowLevel (Actionable (fun _ -> selected.update "Phone"))
        )
    |]
)"""
                                        )
                                )
                            }
                    )
                }
        )
