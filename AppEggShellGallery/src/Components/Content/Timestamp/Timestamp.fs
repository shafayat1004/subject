[<AutoOpen>]
module AppEggShellGallery.Components.Content_Timestamp

open System
open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member Timestamp() : ReactElement =
        Ui.ComponentContent(
            displayName = "Timestamp",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Timestamp",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Timestamp(
                                value = LC.Timestamp.PropValueFactory.Make(DateTimeOffset.Now)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Timestamp(
    value = LC.Timestamp.PropValueFactory.Make(System.DateTimeOffset.Now)
)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Timestamp(
                                value = LC.Timestamp.PropValueFactory.Make(1603261700701L)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Timestamp(
    value = LC.Timestamp.PropValueFactory.Make(1603261700701L)
)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Timestamp(
                                value = LC.Timestamp.PropValueFactory.Make("2011-03-15"),
                                format = "dddd, MMMM dd, yyyy"
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Timestamp(
    value = LC.Timestamp.PropValueFactory.Make("2011-03-15"),
    format = "dddd, MMMM dd, yyyy"
)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Timestamp(
                                value = LC.Timestamp.PropValueFactory.Make(DateTimeOffset.Now),
                                format = "dddd, MMMM dd, yyyy"
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Timestamp(
    value = LC.Timestamp.PropValueFactory.Make(System.DateTimeOffset.Now),
    format = "dddd, MMMM dd, yyyy"
)
"""
                            )
                    )
                }
        )
