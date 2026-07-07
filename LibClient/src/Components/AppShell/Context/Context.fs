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
open Rn.Components
open Rn.Styles

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
            ?showTopLevelSpinnerForKeys: LC.Executor.ShowTopLevelSpinnerForKeys,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key           |> ignore
        xLegacyStyles |> ignore

        // Default preserves the historical behavior (a top-level spinner for every in-progress
        // executor key). Apps whose actions update in place can pass `Some Set.empty` to suppress
        // the global spinner (e.g. a todo list where a full-screen spinner per toggle is noise).
        let showTopLevelSpinnerForKeys =
            defaultArg showTopLevelSpinnerForKeys LC.Executor.ShowTopLevelSpinnerForKeys.All

        let dataHook = Hooks.useState (Map.empty : Map<string, LibClient.Components.With.GlobalDataFlowControl.Context.DataFlowPolicyValue>)

        let updateGlobalDataFlowControlData (updater: Map<string, LibClient.Components.With.GlobalDataFlowControl.Context.DataFlowPolicyValue> -> Map<string, LibClient.Components.With.GlobalDataFlowControl.Context.DataFlowPolicyValue>) =
            dataHook.update (updater dataHook.current)

        let globalDataFlowControl: LibClient.Components.With.GlobalDataFlowControl.Context.Control = {
            Update = updateGlobalDataFlowControlData
            Data   = dataHook.current
        }

        // Fable.React `contextProvider` renders its children collection without injecting
        // keys, so passing a bare array triggers a dev-only "unique key" warning. Route the
        // children through tellReactArrayKeysAreOkay (React.Children.toArray assigns keys).
        LibClient.Components.With.GlobalDataFlowControl.Context.globalDataFlowControlContextProvider
            globalDataFlowControl
            (tellReactArrayKeysAreOkay [|
                Rn.View(
                    styles   = [| Styles.view |],
                    children = [|
                        LC.Executor.AlertErrors(
                            ``with`` =
                                (fun makeExecutor ->
                                    setGlobalExecutor makeExecutor
                                    globalExecutorContextProvider
                                        makeExecutor
                                        (tellReactArrayKeysAreOkay [| castAsElement children |])
                                ),
                            showTopLevelSpinnerForKeys = showTopLevelSpinnerForKeys
                        )
                    |]
                )
            |])
