[<AutoOpen>]
module LibClient.Components.With_ReducedMotion

open Fable.React
open LibClient.Components

type LC.With with
    [<Component>]
    static member ReducedMotion (``with``: bool -> ReactElement) : ReactElement =
        LC.With.Accessibility (fun settings -> ``with`` settings.ReduceMotion)

    [<Component>]
    static member BoldText (``with``: bool -> ReactElement) : ReactElement =
        LC.With.Accessibility (fun settings -> ``with`` settings.BoldText)

    [<Component>]
    static member ReduceTransparency (``with``: bool -> ReactElement) : ReactElement =
        LC.With.Accessibility (fun settings -> ``with`` settings.ReduceTransparency)
