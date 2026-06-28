[<AutoOpen>]
module LibRouter.Components.With_CurrentRoute

open Fable.React
open LibClient
open LibRouter.Components
open LibRouter.RoutesSpec
open LibRouter.Components.Constructors
open LibRouter.Components.With.Route

type LibRouter.Components.Constructors.LR.With with
    static member CurrentRoute (spec: Conversions<'Route, 'ResultlessDialog>, fn: Option<'Route> -> ReactElement) : ReactElement =
        LR.With.Route (
            spec = spec,
            ``with`` = (Option.map NavigationFrame.route >> fn)
        )
