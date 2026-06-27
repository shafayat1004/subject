[<AutoOpen>]
module AppEggShellGallery.Components.Content_Section_Padded

open Fable.React
open LibClient
open LibClient.Components

type Ui.Content.Section with
    [<Component>]
    static member Padded () : ReactElement =
        Ui.ComponentContent (
            displayName = "Section.Padded",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Section.Padded",
            notes = LC.Text "Section.Padded adds horizontal and vertical padding around its children. Content outside the section keeps the default layout spacing for comparison.",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = (
                                        element {
                                            LC.Section.Padded(
                                                children = [| LC.UiText "Sample uitext block wrapped by Section.Padded to get extra padding." |]
                                            )
                                            LC.UiText "Sample uitext outside Section.Padded"
                                        }
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Section.Padded(
    children = [| LC.UiText "Sample uitext block wrapped by Section.Padded to get extra padding." |]
)
LC.UiText "Sample uitext outside Section.Padded""")
                                )
                            }
                        )
                    )
                }
            )
        )
