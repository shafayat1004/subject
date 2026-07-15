[<AutoOpen>]
module AppEggShellGallery.Components.Content_Card

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Legacy

type Ui.Content with
    [<Component>]
    static member Card () : ReactElement =
        Ui.ComponentContent (
            displayName = "Card",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Legacy.Card",
            notes       = LC.Text "Legacy.Card wraps content in a bordered container. Use style for shadow vs flat appearance; pass onPress to make the whole card tappable.",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Legacy.Card",
                    role           = "none (static) or button when onPress is provided",
                    namePattern    = "Child text content; card is labeled by its children",
                    stateNotes     = "Pressable cards expose button role and disabled state when applicable",
                    scalesWithFont = true,
                    contrastNotes  = "Card content text meets WCAG AA on card backgrounds"
                ),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Shadowed",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = LC.Legacy.Card(
                                        children = [| LC.Text "This is a shadowed card with some text" |],
                                        style    = Card.Style.Shadowed
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Legacy.Card(
    children = [| LC.Text "This is a shadowed card with some text" |],
    style = Card.Style.Shadowed
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Flat",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = LC.Legacy.Card(
                                        children = [| LC.Text "This is a flat card with some text" |],
                                        style    = Card.Style.Flat
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Legacy.Card(
    children = [| LC.Text "This is a flat card with some text" |],
    style = Card.Style.Flat
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Pressable",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = LC.Legacy.Card(
                                        children = [| LC.Text "This is a card that you can press" |],
                                        onPress  = (fun _ -> Action.alert "hello"),
                                        testId   = A11ySlug.testId "legacy-card" "Open"
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Legacy.Card(
    children = [| LC.Text "This is a card that you can press" |],
    onPress = (fun _ -> Action.alert "hello"),
    testId = A11ySlug.testId "legacy-card" "Open"
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
