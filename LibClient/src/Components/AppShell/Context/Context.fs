// AppShell.Context — converted from render DSL to pure F#.
// Owns the global data-flow-control context and the executor context.
// All children are wrapped in LC.Executor.AlertErrors so executor errors surface
// as top-level alerts, and inside globalExecutorContextProvider so any descendant
// can read the current MakeExecutor.
[<AutoOpen>]
module LibClient.Components.AppShell_Context

open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

type LibClient.Components.Constructors.LC.AppShell with
    [<Component>]
    static member Context(
            children:      array<ReactElement>,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key           |> ignore
        xLegacyStyles |> ignore

        let dataHook = Hooks.useState (Map.empty : Map<string, LibClient.Components.With.GlobalDataFlowControl.Context.DataFlowPolicyValue>)

        let updateGlobalDataFlowControlData (updater: Map<string, LibClient.Components.With.GlobalDataFlowControl.Context.DataFlowPolicyValue> -> Map<string, LibClient.Components.With.GlobalDataFlowControl.Context.DataFlowPolicyValue>) =
            dataHook.update (updater dataHook.current)

        let globalDataFlowControl: LibClient.Components.With.GlobalDataFlowControl.Context.Control = {
            Update = updateGlobalDataFlowControlData
            Data   = dataHook.current
        }

        LibClient.Components.With.GlobalDataFlowControl.Context.globalDataFlowControlContextProvider
            globalDataFlowControl
            [|
                RX.View(
                    styles   = [| Styles.view |],
                    children = [|
                        LC.Executor.AlertErrors(
                            ``with`` =
                                (fun makeExecutor ->
                                    setGlobalExecutor makeExecutor
                                    globalExecutorContextProvider
                                        makeExecutor
                                        [| castAsElement children |]
                                ),
                            showTopLevelSpinnerForKeys = LC.Executor.ShowTopLevelSpinnerForKeys.All
                        )
                    |]
                )
            |]
