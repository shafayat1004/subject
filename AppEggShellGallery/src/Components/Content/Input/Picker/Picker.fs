[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Picker

open System
open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input_Picker
open LibClient.Components.Input.PickerModel

type private Fruit =
| Apple
| Mango
| Banana
| Pear
with
    member this.GetName : NonemptyString =
        NonemptyString.ofLiteral (unionCaseName this)

let private fruits : OrderedSet<Fruit> =
    [ Apple; Mango; Banana; Pear ]
    |> OrderedSet.ofList

let private manyItems : OrderedSet<string> =
    "Lorem ipsum dolor sit amet consectetur adipiscing elit Sed iaculis neque nec ligula tempor aliquam eget vitae justo Sed vitae ex metus Vestibulum in turpis tempor rhoncus velit vel commodo turpis Integer aliquam vitae justo ac imperdiet Etiam eu lectus suscipit laoreet metus vitae volutpat elit Donec at mauris faucibus tristique enim non mattis turpis Donec eu pellentesque turpis ut vulputate nisi Quisque feugiat justo eu massa varius ullamcorper a in ex Ut auctor vulputate lorem quis ultricies erat porttitor ac Proin faucibus nibh at sapien efficitur non pellentesque est iaculis Duis imperdiet arcu sed elementum finibus Aliquam erat volutpat"
        .Split " "
    |> Array.toList
    |> OrderedSet.ofList

let private fruitItemView =
    PropItemViewFactory.Make (fun (fruit: Fruit) -> fruit.GetName.Value)

let private fruitToFilterString (fruit: Fruit) : string =
    fruit.GetName.Value

let private fruitToFilterStringWithAdditionalText (fruit: Fruit) : string =
    [ (Apple, "apel"); (Mango, "aam"); (Banana, "kola"); (Pear, "nashpati") ]
    |> List.find (fun (item, _) -> item = fruit)
    |> fun (fruit: Fruit, searchText: string) -> sprintf "%s %s" fruit.GetName.Value searchText

let private stringItemView =
    PropItemViewFactory.Make (fun (item: string) -> item)

let private fetchFruitsAllOnNoQuery (maybeQuery: Option<NonemptyString>) : Async<OrderedSet<Fruit>> =
    async {
        do! Async.Sleep (TimeSpan.FromMilliseconds 3000)

        let filteredFruit =
            match maybeQuery with
            | None -> fruits
            | Some query ->
                let queryLower = query.Value.ToLower()
                fruits |> OrderedSet.filter (fun fruit -> fruit.GetName.Value.ToLower().Contains queryLower)

        return filteredFruit
    }

let private fetchFruitsEmptyOnNoQuery (maybeQuery: Option<NonemptyString>) : Async<OrderedSet<Fruit>> =
    async {
        do! Async.Sleep (TimeSpan.FromMilliseconds 3000)

        let filteredFruit =
            match maybeQuery with
            | None -> OrderedSet.empty
            | Some query ->
                let queryLower = query.Value.ToLower()
                fruits |> OrderedSet.filter (fun fruit -> fruit.GetName.Value.ToLower().Contains queryLower)

        return filteredFruit
    }

type private Helpers =
    [<Component>]
    static member AtMostOneSample() : ReactElement =
        let selectedFruit = Hooks.useState None

        LC.Input.Picker(
            label    = "Fruit",
            items    = Static (fruits, fruitToFilterString),
            itemView = fruitItemView,
            value    = AtMostOne (selectedFruit.current, selectedFruit.update),
            validity = Valid
        )

    [<Component>]
    static member ExactlyOneSample() : ReactElement =
        let selectedFruit = Hooks.useState None

        LC.Input.Picker(
            label    = "Fruit",
            items    = Static (fruits, fruitToFilterString),
            itemView = fruitItemView,
            value    = ExactlyOne (selectedFruit.current, fun fruit -> selectedFruit.update (Some fruit)),
            validity = Valid
        )

    [<Component>]
    static member AtLeastOneSample(?withSearchText: bool) : ReactElement =
        let selectedFruits = Hooks.useState None

        LC.Input.Picker(
            label    = "Fruit",
            items    =
                (if withSearchText = Some true then
                     Static (fruits, fruitToFilterStringWithAdditionalText)
                 else
                     Static (fruits, fruitToFilterString)),
            itemView = fruitItemView,
            value    = AtLeastOne (selectedFruits.current, fun fruits -> selectedFruits.update (Some fruits.ToOrderedSet)),
            validity = Valid
        )

    [<Component>]
    static member AnySample(?validity: InputValidity) : ReactElement =
        let selectedFruits = Hooks.useState None
        let validity = defaultArg validity Valid

        LC.Input.Picker(
            label    = "Fruit",
            items    = Static (fruits, fruitToFilterString),
            itemView = fruitItemView,
            value    = Any (selectedFruits.current, fun fruits -> selectedFruits.update (Some fruits)),
            validity = validity
        )

    [<Component>]
    static member CustomRenderSample() : ReactElement =
        let selectedFruit = Hooks.useState None

        let renderItem (item: Fruit) =
            element {
                LC.Text (item.GetName.Value.ToUpper())
                LC.Text (item.GetName.Value.ToLower())
            }

        LC.Input.Picker(
            label    = "Fruit",
            items    = Static (fruits, fruitToFilterString),
            itemView = Custom renderItem,
            value    = AtMostOne (selectedFruit.current, selectedFruit.update),
            validity = Valid
        )

    [<Component>]
    static member AsyncAllOnNoQuerySample() : ReactElement =
        let selectedFruit = Hooks.useState None

        LC.Input.Picker(
            label    = "Fruit",
            items    = Async fetchFruitsAllOnNoQuery,
            itemView = fruitItemView,
            value    = AtMostOne (selectedFruit.current, selectedFruit.update),
            validity = Valid
        )

    [<Component>]
    static member AsyncEmptyOnNoQuerySample() : ReactElement =
        let selectedFruit = Hooks.useState None

        LC.Input.Picker(
            label    = "Fruit",
            items    = Async fetchFruitsEmptyOnNoQuery,
            itemView = fruitItemView,
            value    = AtMostOne (selectedFruit.current, selectedFruit.update),
            validity = Valid
        )

    [<Component>]
    static member ManyChoicesSample() : ReactElement =
        let selectedItems = Hooks.useState None

        LC.Input.Picker(
            label    = "Many Choices",
            items    = Static (manyItems, id),
            itemView = stringItemView,
            value    = Any (selectedItems.current, fun items -> selectedItems.update (Some items)),
            validity = Valid
        )

type Ui.Content.Input with
    [<Component>]
    static member Picker() : ReactElement =
        Ui.ComponentContent(
            displayName  = "Input.Picker",
            isResponsive = true,
            props        = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Picker",
            notes        =
                element {
                    Ui.Code(
                        heading  = "Relevant setup code",
                        language = Code.Fsharp,
                        children = [|
                            LC.Text """
type Fruit = Apple | Mango | Banana | Pear
with member this.GetName = NonemptyString.ofLiteral (unionCaseName this)

let fruits = [Apple; Mango; Banana; Pear] |> OrderedSet.ofList

let fruitItemVisuals (fruit: Fruit) = {| Label = fruit.GetName.Value |}
let fruitToFilterString (fruit: Fruit) = fruit.GetName.Value"""
                        |]
                    )
                },
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.AtMostOneSample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterString),
    itemView = Default fruitItemVisuals,
    value    = AtMostOne (maybeSelected, setSelected),
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.ExactlyOneSample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterString),
    itemView = Default fruitItemVisuals,
    value    = ExactlyOne (maybeSelected, fun fruit -> setSelected (Some fruit)),
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.AtLeastOneSample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterString),
    itemView = Default fruitItemVisuals,
    value    = AtLeastOne (maybeSelected, fun fruits -> setSelected (Some fruits.ToOrderedSet)),
    validity = Valid
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.AnySample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterString),
    itemView = Default fruitItemVisuals,
    value    = Any (maybeSelected, fun fruits -> setSelected (Some fruits)),
    validity = Valid
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Custom Rendering",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.CustomRenderSample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let renderItem (item: Fruit) =
    element {
        LC.Text (item.GetName.Value.ToUpper())
        LC.Text (item.GetName.Value.ToLower())
    }

LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterString),
    itemView = Custom renderItem,
    value    = AtMostOne (maybeSelected, setSelected),
    validity = Valid
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Async items",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    notes   = LC.Text "When the async returns ALL items when no query is entered",
                                    visuals = Helpers.AsyncAllOnNoQuerySample(),
                                    code    =
                                        ComponentSample.Children(
                                            element {
                                                Ui.Code(
                                                    language = ComponentSample.Fsharp,
                                                    children = [|
                                                        LC.Text """
let fetchFruitsAllOnNoQuery (maybeQuery: Option<NonemptyString>) : Async<OrderedSet<Fruit>> =
    async {
        do! Async.Sleep (TimeSpan.FromMilliseconds 3000)
        let filteredFruit =
            match maybeQuery with
            | None -> fruits
            | Some query ->
                let queryLower = query.Value.ToLower()
                fruits |> OrderedSet.filter (fun fruit -> fruit.GetName.Value.ToLower().Contains queryLower)
        return filteredFruit
    }"""
                                                    |]
                                                )

                                                Ui.Code(
                                                    language = ComponentSample.Fsharp,
                                                    children = [|
                                                        LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Async fetchFruitsAllOnNoQuery,
    itemView = Default fruitItemVisuals,
    value    = AtMostOne (maybeSelected, setSelected),
    validity = Valid
)"""
                                                    |]
                                                )
                                            }
                                        )
                                )

                                Ui.ComponentSample(
                                    notes   = LC.Text "When the async returns NO items when no query is entered",
                                    visuals = Helpers.AsyncEmptyOnNoQuerySample(),
                                    code    =
                                        ComponentSample.Children(
                                            element {
                                                Ui.Code(
                                                    language = ComponentSample.Fsharp,
                                                    children = [|
                                                        LC.Text """
let fetchFruitsEmptyOnNoQuery (maybeQuery: Option<NonemptyString>) : Async<OrderedSet<Fruit>> =
    async {
        do! Async.Sleep (TimeSpan.FromMilliseconds 3000)
        let filteredFruit =
            match maybeQuery with
            | None -> OrderedSet.empty
            | Some query ->
                let queryLower = query.Value.ToLower()
                fruits |> OrderedSet.filter (fun fruit -> fruit.GetName.Value.ToLower().Contains queryLower)
        return filteredFruit
    }"""
                                                    |]
                                                )

                                                Ui.Code(
                                                    language = ComponentSample.Fsharp,
                                                    children = [|
                                                        LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Async fetchFruitsEmptyOnNoQuery,
    itemView = Default fruitItemVisuals,
    value    = AtMostOne (maybeSelected, setSelected),
    validity = Valid
)"""
                                                    |]
                                                )
                                            }
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "With additional search text",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.AtLeastOneSample(withSearchText = true),
                                    code    =
                                        ComponentSample.Children(
                                            element {
                                                Ui.Code(
                                                    language = ComponentSample.Fsharp,
                                                    children = [|
                                                        LC.Text """
let fruitToFilterStringWithAdditionalText (fruit: Fruit) : string =
    [(Apple, "apel"); (Mango, "aam"); (Banana, "kola"); (Pear, "nashpati")]
    |> List.find (fun (item, _) -> item = fruit)
    |> fun (fruit, searchText) -> sprintf "%s %s" fruit.GetName.Value searchText"""
                                                    |]
                                                )

                                                Ui.Code(
                                                    language = ComponentSample.Fsharp,
                                                    children = [|
                                                        LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterStringWithAdditionalText),
    itemView = Default fruitItemVisuals,
    value    = AtLeastOne (maybeSelected, fun fruits -> setSelected (Some fruits.ToOrderedSet)),
    validity = Valid
)"""
                                                    |]
                                                )
                                            }
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Many choices",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.ManyChoicesSample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Picker(
    label    = "Many Choices",
    items    = Static (manyItems, identity),
    itemView = Default stringItemVisuals,
    value    = Any (maybeSelected, fun items -> setSelected (Some items)),
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
                                    visuals = Helpers.AnySample(validity = Missing),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterString),
    itemView = Default fruitItemVisuals,
    value    = Any (maybeSelected, fun fruits -> setSelected (Some fruits)),
    validity = Missing
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.AnySample(validity = Invalid "Not a fruit"),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Picker(
    label    = "Fruit",
    items    = Static (fruits, fruitToFilterString),
    itemView = Default fruitItemVisuals,
    value    = Any (maybeSelected, fun fruits -> setSelected (Some fruits)),
    validity = Invalid "Not a fruit"
)"""
                                        )
                                )
                            }
                    )
                }
        )
