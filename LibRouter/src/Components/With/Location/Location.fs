[<AutoOpen>]
module LibRouter.Components.With.Location

open Fable.React
open LibClient
open LibRouter.Components
open LibRouter.Components.Constructors

type LibRouter.Components.Constructors.LR.With with
    [<Component>]
    static member Location (``with``: Router.Location -> ReactElement, ?key: string) : ReactElement =
        ignore key
        ``with`` (Router.useLocation())
