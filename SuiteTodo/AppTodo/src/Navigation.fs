module AppTodo.Navigation

open LibClient
open LibRouter.RoutesSpec
open LibRouter.Components.With.Navigation

type ResultlessDialog =
| Sentinel
with interface NavigationResultlessDialog

type ResultfulDialog =
| Sentinel
with interface NavigationResultfulDialog

type Route =
| Todos
with interface NavigationRoute

type PreviousNavigationFrame = LibRouter.RoutesSpec.PreviousNavigationFrame<Route, ResultlessDialog>

let navigationState = LibRouter.RoutesSpec.NavigationState<Route, ResultlessDialog, ResultfulDialog>()

let private lazyRoutesSpec: Lazy<LibRouter.RoutesSpec.Conversions<Route, ResultlessDialog>> = lazy (
    let specs: List<LibRouter.RoutesSpec.Spec<Route>> =
        [
            ("/",
                (fun _ -> Todos),
                (function (Todos) -> Some []))
        ]
    LibRouter.RoutesSpec.makeConversions (Config.current().AppUrlBase) specs navigationState
)

let routesSpec () = lazyRoutesSpec.Force()

type Navigation(queue) =
    inherit LibRouter.Components.With.Navigation.Navigation<Route, ResultlessDialog, ResultfulDialog>(queue)

let navigationQueue: LibClient.EventBus.Queue<NavigationAction<Route, ResultlessDialog, ResultfulDialog>> =
    LibClient.EventBus.Queue "navigation"

let nav = Navigation navigationQueue
