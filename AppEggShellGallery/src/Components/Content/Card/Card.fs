[<AutoOpen>]
module AppEggShellGallery.Components.Content_Card

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Legacy

type Ui.Content with
    [<Component>]
    static member Card () : ReactElement =
        Ui.ComponentContent (
            displayName = "Card",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Legacy.Card",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = LC.Legacy.Card(
                                        children = [| LC.Text "This is a shadowed card with some text" |],
                                        style = Card.Style.Shadowed
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Legacy.Card(
    children = [| LC.Text "This is a shadowed card with some text" |],
    style = Card.Style.Shadowed
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.Legacy.Card(
                                        children = [| LC.Text "This is a flat card with some text" |],
                                        style = Card.Style.Flat
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Legacy.Card(
    children = [| LC.Text "This is a flat card with some text" |],
    style = Card.Style.Flat
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.Legacy.Card(
                                        children = [| LC.Text "This is a card that you can press" |],
                                        onPress = (fun _ -> Action.alert "hello")
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Legacy.Card(
    children = [| LC.Text "This is a card that you can press" |],
    onPress = (fun _ -> Action.alert "hello")
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
