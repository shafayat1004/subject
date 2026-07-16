[<AutoOpen>]
module AppEggShellGallery.Components.Content_ToggleButtons

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.Icons

type private Fruit =
| Mango
| Peach
| Banana

type private Helpers =
    [<Component>]
    static member AtMostOneSample() : ReactElement =
        let selected = Hooks.useState None

        LC.ToggleButtons(
            label = "Fruit",
            value = LC.ToggleButtons.AtMostOne (selected.current, selected.update),
            buttons =
                fun group ->
                    element {
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Mango", value = Fruit.Mango)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Peach", value = Fruit.Peach)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Banana", value = Fruit.Banana)
                    }
        )

    [<Component>]
    static member ExactlyOneSample() : ReactElement =
        let selected = Hooks.useState None

        LC.ToggleButtons(
            label = "Fruit",
            value = LC.ToggleButtons.ExactlyOne (selected.current, fun fruit -> selected.update (Some fruit)),
            buttons =
                fun group ->
                    element {
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Icon Icon.Home, value = Fruit.Mango)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Icon Icon.Calendar, value = Fruit.Peach)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Icon Icon.Menu, value = Fruit.Banana)
                    }
        )

    [<Component>]
    static member AtLeastOneSample() : ReactElement =
        let selected = Hooks.useState None

        LC.ToggleButtons(
            label = "Fruit",
            value =
                LC.ToggleButtons.AtLeastOne (
                    selected.current,
                    fun fruits -> selected.update (Some fruits.ToOrderedSet)
                ),
            buttons =
                fun group ->
                    element {
                        LC.ToggleButton(group = group, style = LC.ToggleButton.LabelAndIcon ("Mango", Icon.Home), value = Fruit.Mango)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.LabelAndIcon ("Peach", Icon.Calendar), value = Fruit.Peach)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.LabelAndIcon ("Banana", Icon.Menu), value = Fruit.Banana)
                    }
        )

    [<Component>]
    static member AnySample() : ReactElement =
        let selected = Hooks.useState None

        LC.ToggleButtons(
            label = "Fruit",
            value = LC.ToggleButtons.Any (selected.current, fun fruits -> selected.update (Some fruits)),
            buttons =
                fun group ->
                    element {
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Mango", value = Fruit.Mango)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Peach", value = Fruit.Peach)
                        LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Banana", value = Fruit.Banana)
                    }
        )

type Ui.Content with
    [<Component>]
    static member ToggleButtons() : ReactElement =
        Ui.ComponentContent(
            displayName = "ToggleButtons",
            props =
                ComponentContent.Manual(
                    element {
                        Ui.ScrapedComponentProps(
                            heading            = "ToggleButtons",
                            fullyQualifiedName = "LibClient.Components.ToggleButtons"
                        )

                        Ui.ScrapedComponentProps(
                            heading            = "ToggleButton",
                            fullyQualifiedName = "LibClient.Components.ToggleButton"
                        )
                    }
                ),
            notes =
                LC.Text "Each toggle button auto-slugs testId from its label (toggle-button-mango) or from the value for icon-only styles (toggle-button-mango). Pass label on LC.ToggleButtons to name the radio group.",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.ToggleButtons / LC.ToggleButton",
                    role           = "radiogroup (LC.ToggleButtons); radio (LC.ToggleButton)",
                    namePattern    = "Button label or icon+value; group named via label prop on LC.ToggleButtons",
                    stateNotes     = "selected toggle exposes selected state via accessibilityState",
                    scalesWithFont = true,
                    contrastNotes  = "Selected/unselected states use theme colors with non-color selected indicator"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.AtMostOneSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
type Fruit = Mango | Peach | Banana

let selected = Hooks.useState None

LC.ToggleButtons(
    label = "Fruit",
    value = LC.ToggleButtons.AtMostOne (selected.current, selected.update),
    buttons = fun group ->
        element {
            LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Mango",  value = Fruit.Mango)
            LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Peach",  value = Fruit.Peach)
            LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Banana", value = Fruit.Banana)
        }
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.ExactlyOneSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let selected = Hooks.useState None

LC.ToggleButtons(
    label = "Fruit",
    value = LC.ToggleButtons.ExactlyOne (selected.current, fun fruit -> selected.update (Some fruit)),
    buttons = fun group ->
        element {
            LC.ToggleButton(group = group, style = LC.ToggleButton.Icon Icon.Home,     value = Fruit.Mango)
            LC.ToggleButton(group = group, style = LC.ToggleButton.Icon Icon.Calendar, value = Fruit.Peach)
            LC.ToggleButton(group = group, style = LC.ToggleButton.Icon Icon.Menu,     value = Fruit.Banana)
        }
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.AtLeastOneSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let selected = Hooks.useState None

LC.ToggleButtons(
    label = "Fruit",
    value = LC.ToggleButtons.AtLeastOne (selected.current, fun fruits -> selected.update (Some fruits.ToOrderedSet)),
    buttons = fun group ->
        element {
            LC.ToggleButton(group = group, style = LC.ToggleButton.LabelAndIcon ("Mango",  Icon.Home),     value = Fruit.Mango)
            LC.ToggleButton(group = group, style = LC.ToggleButton.LabelAndIcon ("Peach",  Icon.Calendar), value = Fruit.Peach)
            LC.ToggleButton(group = group, style = LC.ToggleButton.LabelAndIcon ("Banana", Icon.Menu),     value = Fruit.Banana)
        }
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.AnySample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let selected = Hooks.useState None

LC.ToggleButtons(
    label = "Fruit",
    value = LC.ToggleButtons.Any (selected.current, fun fruits -> selected.update (Some fruits)),
    buttons = fun group ->
        element {
            LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Mango",  value = Fruit.Mango)
            LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Peach",  value = Fruit.Peach)
            LC.ToggleButton(group = group, style = LC.ToggleButton.Label "Banana", value = Fruit.Banana)
        }
)"""
                                        )
                                )
                            }
                    )
                }
        )
