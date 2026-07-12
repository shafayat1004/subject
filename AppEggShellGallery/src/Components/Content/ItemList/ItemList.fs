[<AutoOpen>]
module AppEggShellGallery.Components.Content_ItemList

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Legacy
open AppEggShellGallery
open AppEggShellGallery.Colors
open Rn.Components
open Rn.Styles

type private Fruit = {
    Name:  string
    Color: Color
}

let private fruits = [
    { Name = "Mango";    Color = Color.Hex "#ff9000" }
    { Name = "Kiwi";     Color = Color.Hex "#1d6308" }
    { Name = "Raspbery"; Color = Color.Hex "#b90041" }
    { Name = "Apple";    Color = Color.Hex "#76b902" }
]

[<RequireQualifiedAccess>]
module private Styles =
    let emptyMessage = makeViewStyles { paddingVertical 20 }

    let emptyMessageText =
        makeTextStyles {
            TextAlign.Center
            color (colors.Caution.Main)
            fontSize 18
        }

    let customSeeAll = makeViewStyles { JustifyContent.Center }

let private fruitCards (items: seq<Fruit>) : ReactElement =
    items
    |> Seq.map (fun fruit -> LC.Legacy.Card(children = [| LC.Text fruit.Name |]))
    |> Array.ofSeq
    |> castAsElement

type Ui.Content with
    [<Component>]
    static member ItemList () : ReactElement =
        Ui.ComponentContent(
            displayName  = "ItemList",
            isResponsive = true,
            props        = ComponentContent.ForFullyQualifiedName "LibClient.Components.ItemList",
            notes =
                element {
                    LC.Text "Never forget to have a proper message for empty lists by using this component."
                    LC.Text "Base types and values used in the examples:"
                    Ui.Code(
                        language = Code.Fsharp,
                        children =
                            [| LC.Text """
type Fruit = {
    Name:  string
    Color: Color
}

let fruits = [
    { Name = "Mango";    Color = Color.Hex "#ff9000" }
    { Name = "Kiwi";     Color = Color.Hex "#1d6308" }
    { Name = "Raspbery"; Color = Color.Hex "#b90041" }
    { Name = "Apple";    Color = Color.Hex "#76b902" }
]
""" |]
                    )
                },
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.ItemList",
                    role           = "list (container); list items inherit child semantics",
                    namePattern    = "Empty-state message via emptyMessage prop; list items use child content",
                    stateNotes     = "SeeAll link is a pressable button when present",
                    scalesWithFont = true,
                    contrastNotes  = "List text and empty-state message colors meet WCAG AA"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items        = fruits,
                                            style        = ItemList.Responsive HorizontalAlignment.Center,
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = fruits,
    style = ItemList.Responsive HorizontalAlignment.Center,
    whenNonempty = fun fruits ->
        element {
            for fruit in fruits do
                LC.Legacy.Card(children = [| LC.Text fruit.Name |])
        }
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items        = fruits,
                                            style        = ItemList.Responsive HorizontalAlignment.Right,
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = fruits,
    style = ItemList.Responsive HorizontalAlignment.Right,
    whenNonempty = fruitCards
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items        = fruits,
                                            style        = ItemList.Responsive HorizontalAlignment.Left,
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = fruits,
    style = ItemList.Responsive HorizontalAlignment.Left,
    whenNonempty = fruitCards
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items        = fruits,
                                            style        = ItemList.Raw,
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = fruits,
    style = ItemList.Raw,
    whenNonempty = fruitCards
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items        = [],
                                            style        = ItemList.Raw,
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = [],
    style = ItemList.Raw,
    whenNonempty = fruitCards
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items        = [],
                                            whenEmpty    = WhenEmpty.Message "Fruitless",
                                            style        = ItemList.Raw,
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = [],
    whenEmpty = WhenEmpty.Message "Fruitless",
    style = ItemList.Raw,
    whenNonempty = fruitCards
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items = [],
                                            style = ItemList.Raw,
                                            whenEmpty =
                                                WhenEmpty.Children(
                                                    Rn.View(
                                                        styles   = [| Styles.emptyMessage |],
                                                        children = [| LC.UiText("No Fruit!", styles = [| Styles.emptyMessageText |]) |]
                                                    )
                                                ),
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = [],
    style = ItemList.Raw,
    whenEmpty = WhenEmpty.Children (
        Rn.View(
            styles = [| makeViewStyles { paddingVertical 20 } |],
            children = [| LC.UiText("No Fruit!", styles = [| makeTextStyles { TextAlign.Center; color colors.Caution.Main; fontSize 18 } |]) |]
        )
    ),
    whenNonempty = fruitCards
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "SeeAll",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items        = fruits,
                                            style        = ItemList.Horizontal,
                                            seeAll       = SeeAll.Default Actions.greet,
                                            whenNonempty = fruitCards,
                                            theme        = (fun theme -> { theme with SeeAll = { Height = 70; MarginVertical = 0 } })
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = fruits,
    style = ItemList.Horizontal,
    seeAll = SeeAll.Default Actions.greet,
    theme = fun theme -> { theme with SeeAll = { Height = 70; MarginVertical = 0 } },
    whenNonempty = fruitCards
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        LC.ItemList(
                                            items = fruits,
                                            style = ItemList.Horizontal,
                                            seeAll =
                                                SeeAll.Children(
                                                    Rn.View(
                                                        styles   = [| Styles.customSeeAll |],
                                                        children = [|
                                                            LC.TextButton(
                                                                label = "See More",
                                                                state = ButtonHighLevelState.LowLevel (LC.TextButton.Actionable Actions.greet)
                                                            )
                                                        |]
                                                    )
                                                ),
                                            whenNonempty = fruitCards
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.ItemList(
    items = fruits,
    style = ItemList.Horizontal,
    seeAll = SeeAll.Children (
        Rn.View(
            styles = [| makeViewStyles { JustifyContent.Center } |],
            children = [|
                LC.TextButton(label = "See More", state = LC.TextButton.Actionable Actions.greet)
            |]
        )
    ),
    whenNonempty = fruitCards
)"""
                                        )
                                )
                            }
                    )
                }
        )
