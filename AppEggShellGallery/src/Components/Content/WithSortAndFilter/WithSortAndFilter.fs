[<AutoOpen>]
module AppEggShellGallery.Components.Content_WithSortAndFilter

open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member WithSortAndFilter() : ReactElement =
        Ui.ComponentContent(
            displayName = "WithSortAndFilter",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.WithSortAndFilter",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Text "TODO",
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text "TODO"
                            )
                    )
                }
        )
