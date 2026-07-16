[<AutoOpen>]
module AppEggShellGallery.Components.Content_With_DataFlowControl

open System
open LibLangFsharp
open Fable.React
open LibClient
open LibClient.Components

type Ui.Content.With with
    [<Component>]
    static member DataFlowControl() : ReactElement =
        let deferred = Hooks.useState (Deferred<unit>())

        Ui.ComponentContent(
            displayName = "DataFlowControl",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.With.DataFlowControl",
            notes =
                element {
                    LC.Text "PropagateImmediately: new data reaches the child render function right away."
                    LC.Text "Block: new data is received but not propagated; the child keeps showing the last propagated value."
                    LC.Text "PropagateWhenConfirmed: a confirmation prompt appears when new data differs from what the child currently shows."
                    LC.Text "PropagateWhenResolved: propagation waits until the supplied Deferred resolves (e.g. after an async action completes)."
                },
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.With.DataFlowControl",
                    role           = "none (data propagation wrapper)",
                    namePattern    = "Child render function content provides accessible names",
                    stateNotes     = "May block or delay data propagation; confirmation prompts are dialogs",
                    scalesWithFont = true,
                    contrastNotes  = "Child content contrast unchanged by wrapper"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        samples =
                            element {
                                Ui.ComponentSample(
                                    heading = "Immediate Propagation",
                                    visuals =
                                        LC.With.Now(
                                            updateFrequency = TimeSpan.FromSeconds 5.0,
                                            ``with`` =
                                                fun now ->
                                                    LC.With.DataFlowControl(
                                                        data = now,
                                                        dataFlowPolicy =
                                                            LC.With.DataFlowControlTypes.DataFlowPolicy.PropagateImmediately,
                                                        ``with`` =
                                                            fun data ->
                                                                element {
                                                                    LC.Text "Data:"
                                                                    LC.Timestamp(UniDateTime.Of data)
                                                                }
                                                    )
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.With.DataFlowControl(
    data = now,
    dataFlowPolicy = DataFlowPolicy.PropagateImmediately,
    ``with`` = fun data ->
        element {
            LC.Text "Data:"
            LC.Timestamp (UniDateTime.Of data)
        }
)
"""
                                        )
                                )

                                Ui.ComponentSample(
                                    heading = "Blocked Propagation",
                                    visuals =
                                        LC.With.Now(
                                            updateFrequency = TimeSpan.FromSeconds 5.0,
                                            ``with`` =
                                                fun now ->
                                                    LC.With.DataFlowControl(
                                                        data           = now,
                                                        dataFlowPolicy = LC.With.DataFlowControlTypes.DataFlowPolicy.Block,
                                                        ``with`` =
                                                            fun data ->
                                                                element {
                                                                    LC.Text "Data:"
                                                                    LC.Timestamp(UniDateTime.Of data)
                                                                }
                                                    )
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.With.DataFlowControl(
    data = now,
    dataFlowPolicy = DataFlowPolicy.Block,
    ``with`` = fun data ->
        element {
            LC.Text "Data:"
            LC.Timestamp (UniDateTime.Of data)
        }
)
"""
                                        )
                                )

                                Ui.ComponentSample(
                                    heading = "Confirmed Propagation",
                                    visuals =
                                        LC.With.Now(
                                            updateFrequency = TimeSpan.FromSeconds 5.0,
                                            ``with`` =
                                                fun now ->
                                                    LC.With.DataFlowControl(
                                                        data = now,
                                                        dataFlowPolicy =
                                                            LC.With.DataFlowControlTypes.DataFlowPolicy.PropagateWhenConfirmed (
                                                                "Please confirm the propagation",
                                                                "Confirm"
                                                            ),
                                                        ``with`` =
                                                            fun data ->
                                                                element {
                                                                    LC.Text "Data:"
                                                                    LC.Timestamp(UniDateTime.Of data)
                                                                }
                                                    )
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.With.DataFlowControl(
    data = now,
    dataFlowPolicy = DataFlowPolicy.PropagateWhenConfirmed (
        "Please confirm the propagation",
        "Confirm"
    ),
    ``with`` = fun data -> ...
)
"""
                                        )
                                )

                                Ui.ComponentSample(
                                    heading = "Resolved Propagation",
                                    visuals =
                                        LC.With.Now(
                                            updateFrequency = TimeSpan.FromSeconds 5.0,
                                            ``with`` =
                                                fun now ->
                                                    element {
                                                        LC.With.DataFlowControl(
                                                            data = now,
                                                            dataFlowPolicy =
                                                                LC.With.DataFlowControlTypes.DataFlowPolicy.PropagateWhenResolved deferred.current,
                                                            ``with`` =
                                                                fun data ->
                                                                    element {
                                                                        LC.Text "Data:"
                                                                        LC.Timestamp(UniDateTime.Of data)
                                                                    }
                                                        )

                                                        if deferred.current.IsPending then
                                                            LC.Button(
                                                                "Tap to resolve",
                                                                state =
                                                                    ButtonHighLevelState.LowLevel(
                                                                        ButtonLowLevelState.Actionable(
                                                                            fun _ -> deferred.current.Resolve()
                                                                        )
                                                                    )
                                                            )
                                                    }
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let deferred = Hooks.useState (Deferred<unit>())

LC.With.DataFlowControl(
    data = now,
    dataFlowPolicy = DataFlowPolicy.PropagateWhenResolved deferred.current,
    ``with`` = fun data -> ...
)

if deferred.current.IsPending then
    LC.Button("Tap to resolve", state = ... deferred.current.Resolve())
"""
                                        )
                                )
                            }
                    )
                }
        )
