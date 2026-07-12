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
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.WithSortAndFilter",
                    role           = "none (layout wrapper for sort/filter controls)",
                    namePattern    = "Child sort and filter inputs provide their own labels",
                    stateNotes     = "Wraps grid query UI; state managed by parent",
                    scalesWithFont = true,
                    contrastNotes  = "Child control colors meet WCAG AA"
                ),
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
