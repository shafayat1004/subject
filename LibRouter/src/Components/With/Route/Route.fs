[<AutoOpen>]
module LibRouter.Components.With.Route

open Fable.React
open LibClient
open LibRouter.RoutesSpec
open LibRouter.Components
open LibRouter.Components.Constructors

let decodeLocation<'Route, 'ResultlessDialog> (spec: Conversions<'Route, 'ResultlessDialog>) (location: Router.Location) : Option<NavigationFrame<'Route, 'ResultlessDialog>> =
    spec.FromLocation { Path = location.pathname; Query = location.search }

type LibRouter.Components.Constructors.LR.With with
    [<Component>]
    static member Route (spec: Conversions<'Route, 'ResultlessDialog>, ``with``: Option<NavigationFrame<'Route, 'ResultlessDialog>> -> ReactElement, ?key: string) : ReactElement =
        ignore key
        ``with`` (decodeLocation spec (Router.useLocation()))
