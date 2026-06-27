[<AutoOpen>]
module AppEggShellGallery.Components.Content_InfoMessage

open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member InfoMessage() : ReactElement =
        Ui.ComponentContent(
            displayName = "InfoMessage",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.InfoMessage",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.InfoMessage(message = "No items"),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.InfoMessage(message = "No items")
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.InfoMessage(
                                message =
                                    "We tried to place the order, but got no response from the server.\n\nPlease try again later.",
                                level = InfoMessage.Attention
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.InfoMessage(
    message = "We tried to place the order, but got no response from the server.\n\nPlease try again later.",
    level = InfoMessage.Attention
)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.InfoMessage(
                                message = "A horrible error occurred",
                                level = InfoMessage.Caution
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.InfoMessage(
    message = "A horrible error occurred",
    level = InfoMessage.Caution
)
"""
                            )
                    )
                }
        )
