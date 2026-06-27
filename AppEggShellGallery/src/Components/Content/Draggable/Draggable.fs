[<AutoOpen>]
module AppEggShellGallery.Components.Content_Draggable

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.LocalImages
open AppEggShellGallery

type Ui.Content with
    [<Component>]
    static member Draggable() : ReactElement =
        Ui.ComponentContent(
            displayName = "Draggable",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.Draggable",
            notes = LC.Text "Draggable wraps content that can be swiped or programmatically moved via a ref. Thresholds and offsets control how far each swipe moves the element.",
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Swipe and programmatic control",
                        samples =
                            element {
                                Ui.ComponentSample(
                        visuals =
                            LC.With.Ref(
                                ``with`` =
                                    fun (bindRef, maybeRef: Option<LibClient.Components.Draggable.IDraggableRef>) ->
                                        element {
                                            LC.Draggable(
                                                draggableRef = bindRef,
                                                testId = "gallery-draggable",
                                                left =
                                                    {|
                                                        ForwardThreshold = 50
                                                        Offset = 100
                                                        BackwardThreshold = 20
                                                    |},
                                                right =
                                                    {|
                                                        ForwardThreshold = 100
                                                        Offset = 200
                                                        BackwardThreshold = 20
                                                    |},
                                                up =
                                                    {|
                                                        ForwardThreshold = 20
                                                        Offset = 100
                                                        BackwardThreshold = 20
                                                    |},
                                                down =
                                                    {|
                                                        ForwardThreshold = 20
                                                        Offset = 200
                                                        BackwardThreshold = 20
                                                    |},
                                                baseOffset = (50, 20),
                                                children =
                                                    elements {
                                                        LC.ImageCard(
                                                            source = localImage "/images/wlop4.jpg",
                                                            styles = [| sampleImageCardStyles |]
                                                        )
                                                    }
                                            )

                                            LC.Buttons(
                                                children =
                                                    elements {
                                                        LC.Button(
                                                            label = "Move Left",
                                                            state =
                                                                ButtonHighLevelState.LowLevel (
                                                                    maybeRef
                                                                    |> Option.map (fun draggableRef ->
                                                                        ButtonLowLevelState.Actionable (fun _ ->
                                                                            draggableRef.SetPosition LibClient.Components.Draggable.Position.Left
                                                                            |> ignore))
                                                                    |> Option.getOrElse ButtonLowLevelState.Disabled
                                                                )
                                                        )

                                                        LC.Button(
                                                            label = "Reset",
                                                            state =
                                                                ButtonHighLevelState.LowLevel (
                                                                    maybeRef
                                                                    |> Option.map (fun draggableRef ->
                                                                        ButtonLowLevelState.Actionable (fun _ ->
                                                                            draggableRef.SetPosition LibClient.Components.Draggable.Position.Base
                                                                            |> ignore))
                                                                    |> Option.getOrElse ButtonLowLevelState.Disabled
                                                                )
                                                        )

                                                        LC.Button(
                                                            label = "Move Right",
                                                            state =
                                                                ButtonHighLevelState.LowLevel (
                                                                    maybeRef
                                                                    |> Option.map (fun draggableRef ->
                                                                        ButtonLowLevelState.Actionable (fun _ ->
                                                                            draggableRef.SetPosition LibClient.Components.Draggable.Position.Right
                                                                            |> ignore))
                                                                    |> Option.getOrElse ButtonLowLevelState.Disabled
                                                                )
                                                        )
                                                    }
                                            )
                                        }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.With.Ref(
    ``with`` = fun (bindRef, maybeRef) ->
        element {
            LC.Draggable(
                draggableRef = bindRef,
                testId = "gallery-draggable",
                left = {| ForwardThreshold = 50; Offset = 100; BackwardThreshold = 20 |},
                right = {| ForwardThreshold = 100; Offset = 200; BackwardThreshold = 20 |},
                up = {| ForwardThreshold = 20; Offset = 100; BackwardThreshold = 20 |},
                down = {| ForwardThreshold = 20; Offset = 200; BackwardThreshold = 20 |},
                baseOffset = (50, 20),
                children = elements {
                    LC.ImageCard(
                        source = localImage "/images/wlop4.jpg",
                        styles = [| sampleImageCardStyles |]
                    )
                }
            )
            // Move Left / Reset / Move Right buttons use maybeRef.SetPosition ...
        }
)
"""
                            )
                                )
                            }
                    )
                }
        )
