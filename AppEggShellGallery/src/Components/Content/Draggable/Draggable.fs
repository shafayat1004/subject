[<AutoOpen>]
module AppEggShellGallery.Components.Content_Draggable

open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member Draggable() : ReactElement =
        Ui.ComponentContent(
            displayName = "Draggable",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.Draggable",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.With.Ref(
                                ``with`` =
                                    fun (bindRef, maybeRef: Option<LibClient.Components.Draggable.IDraggableRef>) ->
                                        element {
                                            LC.Draggable(
                                                ref = bindRef,
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
                                                        LC.ImageCard(source = localImage "/images/wlop4.jpg")
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
                                ComponentSample.Render,
                                LC.Text """
                    <LC.With.Ref rt-fs='true' rt-with='bindRef, maybeRef: Option<Draggable.IDraggableRef>'>
                        <LC.Draggable
                         Left='{| ForwardThreshold = 50; Offset = 100; BackwardThreshold = 20 |}'
                         Right='{| ForwardThreshold = 100; Offset = 200; BackwardThreshold = 20 |}'
                         Up='{| ForwardThreshold = 20; Offset = 100; BackwardThreshold = 20 |}'
                         Down='{| ForwardThreshold = 20; Offset = 200; BackwardThreshold = 20 |}'
                         BaseOffset='(50, 20)'
                         ref='bindRef'>
                            <LC.ImageCard Source='localImage "/images/wlop4.jpg"'/>
                        </LC.Draggable>
                        ...
                    </LC.With.Ref>
            """
                            )
                    )
                }
        )
