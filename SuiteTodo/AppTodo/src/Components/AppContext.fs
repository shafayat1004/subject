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
                    children = defaultArg children [||]
                )
        )
