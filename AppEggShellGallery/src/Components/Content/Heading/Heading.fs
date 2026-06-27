[<AutoOpen>]
module AppEggShellGallery.Components.Content_Heading

open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let margin = makeTextStyles { margin 10 }

type Ui.Content with
    [<Component>]
    static member Heading () : ReactElement =
        Ui.ComponentContent (
            displayName = "Heading",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Heading",
            notes = LC.Text "Heading is a very common component and you should not arbitrarily apply style to it. Use themed styles.",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = LC.Heading(
                                        children = [| LC.Text "I am default heading" |],
                                        styles = [| Styles.margin |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Heading(
    children = [| LC.Text "I am default heading" |],
    styles = [| makeTextStyles { margin 10 } |]
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.Heading(
                                        children = [| LC.Text "I am Primary heading" |],
                                        level = Heading.Primary,
                                        styles = [| Styles.margin |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Heading(
    children = [| LC.Text "I am Primary heading" |],
    level = Heading.Primary,
    styles = [| makeTextStyles { margin 10 } |]
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.Heading(
                                        children = [| LC.Text "I am Secondary heading" |],
                                        level = Heading.Secondary,
                                        styles = [| Styles.margin |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Heading(
    children = [| LC.Text "I am Secondary heading" |],
    level = Heading.Secondary,
    styles = [| makeTextStyles { margin 10 } |]
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.Heading(
                                        children = [| LC.Text "I am Tertiary heading" |],
                                        level = Heading.Tertiary,
                                        styles = [| Styles.margin |]
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Heading(
    children = [| LC.Text "I am Tertiary heading" |],
    level = Heading.Tertiary,
    styles = [| makeTextStyles { margin 10 } |]
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
