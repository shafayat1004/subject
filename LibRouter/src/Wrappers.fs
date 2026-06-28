[<AutoOpen>]
module LibRouter_Wrappers

open Fable.React
open LibClient
open LibRouter.Components
open LibRouter.Components.Constructors

type LR with
    static member UseRoute (spec: LibRouter.RoutesSpec.Conversions<'Route, 'ResultlessDialog>) =
        let location = LibRouter.Components.Router.useLocation()
        spec.FromLocation { Path = location.pathname; Query = location.search }
