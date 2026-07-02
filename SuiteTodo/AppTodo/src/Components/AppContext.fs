[<AutoOpen>]
module AppTodo.Components.AppContext

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components
open AppTodo.Navigation

type AppTodo.Components.Constructors.Ui with
    [<Component>]
    static member AppContext(
            ?children: ReactChildrenProp,
            ?key: string)
        : ReactElement =
        ignore key

        LR.NavigationRouter(
            spec = routesSpec(),
            navigationState = navigationState,
            queue = navigationQueue,
            child =
                LC.AppShell.Context(
                    children = defaultArg children [||],
                    // Todo actions (add/toggle/delete/edit/archive) are quick and update the list
                    // in place, so a full-screen spinner on every action is just noise. An empty
                    // key set never matches, disabling the top-level spinner.
                    showTopLevelSpinnerForKeys = LC.Executor.ShowTopLevelSpinnerForKeys.Some Set.empty
                )
        )
