[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_ChoiceList

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input.ChoiceList
open LibClient.Components.Input.ChoiceListItem

type private Fruit =
| Mango
| Peach
| Banana

type private Helpers =
    [<Component>]
    static member FruitItems (group: Group<Fruit>) : ReactElement =
        element {
            LC.Input.ChoiceListItem(group = group, label = Label.String "Mango",  value = Fruit.Mango)
            LC.Input.ChoiceListItem(group = group, label = Label.String "Peach",  value = Fruit.Peach)
            LC.Input.ChoiceListItem(group = group, label = Label.String "Banana", value = Fruit.Banana)
        }

    [<Component>]
    static member FruitItemsWithChildren (group: Group<Fruit>) : ReactElement =
        element {
            LC.Input.ChoiceListItem(group = group, value = Fruit.Mango,  accessibilityLabel = "Mango", testId = "choice-list-item-mango",  children = [| LC.UiText "Mango" |])
            LC.Input.ChoiceListItem(group = group, value = Fruit.Peach,  accessibilityLabel = "Peach", testId = "choice-list-item-peach",  children = [| LC.UiText "Peach" |])
            LC.Input.ChoiceListItem(group = group, value = Fruit.Banana, accessibilityLabel = "Banana", testId = "choice-list-item-banana", children = [| LC.UiText "Banana" |])
        }

    [<Component>]
    static member AtLeastOneSample() : ReactElement =
        let selectedFruits = Hooks.useState (Some (OrderedSet.ofList [Fruit.Mango]))
        LC.Input.ChoiceList(
            items = Helpers.FruitItems,
            value = AtLeastOne (selectedFruits.current, fun fruits -> selectedFruits.update (Some fruits.ToOrderedSet)),
            validity = Valid
        )

    [<Component>]
    static member AnySample() : ReactElement =
        let selectedFruits = Hooks.useState None
        LC.Input.ChoiceList(
            items = Helpers.FruitItems,
            value = Any (selectedFruits.current, fun fruits -> selectedFruits.update (Some fruits)),
            validity = Valid
        )

    [<Component>]
    static member AtMostOneSample() : ReactElement =
        let selectedFruit = Hooks.useState None
        LC.Input.ChoiceList(
            items = Helpers.FruitItems,
            value = AtMostOne (selectedFruit.current, selectedFruit.update),
            validity = Valid
        )

    [<Component>]
    static member ExactlyOneSample() : ReactElement =
        let selectedFruit = Hooks.useState (Some Fruit.Mango)
        LC.Input.ChoiceList(
            items = Helpers.FruitItemsWithChildren,
            value = ExactlyOne (selectedFruit.current, fun fruit -> selectedFruit.update (Some fruit)),
            validity = Valid
        )

    [<Component>]
    static member InvalidSample() : ReactElement =
        let selectedFruit = Hooks.useState None
        LC.Input.ChoiceList(
            items = Helpers.FruitItems,
            value = AtMostOne (selectedFruit.current, selectedFruit.update),
            validity = Invalid "Not a fruit"
        )

type Ui.Content.Input with
    [<Component>]
    static member ChoiceList() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.ChoiceList",
            props = ComponentContent.Manual (element {
                Ui.ScrapedComponentProps(heading = "ChoiceList",     fullyQualifiedName = "LibClient.Components.Input.ChoiceList")
                Ui.ScrapedComponentProps(heading = "ChoiceListItem", fullyQualifiedName = "LibClient.Components.Input.ChoiceListItem")
            }),
            notes = LC.Text "For 'AtMostOne' and 'ExactlyOne', Radio Button is rendered, and for 'AtLeastOne' and 'Any' we render checkboxes.",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.AtLeastOneSample(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.ChoiceList(
    items = fun group -> element {
        LC.Input.ChoiceListItem(group = group, label = Label.String "Mango",  value = Fruit.Mango)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Peach",  value = Fruit.Peach)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Banana", value = Fruit.Banana)
    },
    value = AtLeastOne (selectedFruits.current, fun fruits -> selectedFruits.update (Some fruits.ToOrderedSet)),
    validity = Valid
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.AnySample(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.ChoiceList(
    items = fun group -> element {
        LC.Input.ChoiceListItem(group = group, label = Label.String "Mango",  value = Fruit.Mango)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Peach",  value = Fruit.Peach)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Banana", value = Fruit.Banana)
    },
    value = Any (selectedFruits.current, fun fruits -> selectedFruits.update (Some fruits)),
    validity = Valid
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.AtMostOneSample(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.ChoiceList(
    items = fun group -> element {
        LC.Input.ChoiceListItem(group = group, label = Label.String "Mango",  value = Fruit.Mango)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Peach",  value = Fruit.Peach)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Banana", value = Fruit.Banana)
    },
    value = AtMostOne (selectedFruit.current, selectedFruit.update),
    validity = Valid
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Children Based Label",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.ExactlyOneSample(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.ChoiceList(
    items = fun group -> element {
        LC.Input.ChoiceListItem(group = group, value = Fruit.Mango,  children = [| LC.UiText "Mango" |])
        LC.Input.ChoiceListItem(group = group, value = Fruit.Peach,  children = [| LC.UiText "Peach" |])
        LC.Input.ChoiceListItem(group = group, value = Fruit.Banana, children = [| LC.UiText "Banana" |])
    },
    value = ExactlyOne (selectedFruit.current, fun fruit -> selectedFruit.update (Some fruit)),
    validity = Valid
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Validation (Hardcoded, not dynamic)",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.InvalidSample(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.ChoiceList(
    items = fun group -> element {
        LC.Input.ChoiceListItem(group = group, label = Label.String "Mango",  value = Fruit.Mango)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Peach",  value = Fruit.Peach)
        LC.Input.ChoiceListItem(group = group, label = Label.String "Banana", value = Fruit.Banana)
    },
    value = AtMostOne (selectedFruit.current, selectedFruit.update),
    validity = Invalid "Not a fruit"
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
