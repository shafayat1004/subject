module LibRouter.Components.With.NavigationBridge

open Fable.React
open LibClient
open LibClient.EventBus
open LibRouter.Components
open LibRouter.Components.With.Navigation

type Props<'Route, 'ResultlessDialog, 'ResultfulDialog when 'Route: equality> = {
    Spec:            LibRouter.RoutesSpec.Conversions<'Route, 'ResultlessDialog>
    NavigationState: LibRouter.RoutesSpec.NavigationState<'Route, 'ResultlessDialog, 'ResultfulDialog>
    With:            Navigation<'Route, 'ResultlessDialog, 'ResultfulDialog> -> ReactElement
    key:             string option
}

let private renderFn (props: Props<'Route, 'ResultlessDialog, 'ResultfulDialog>) : ReactElement =
    let queue =
        Hooks.useMemo(
            (fun () -> EventBus.Queue "LibRouter.With.Navigation"),
            [| |]
        )
    LR.NavigationRouter(
        props.Spec,
        props.NavigationState,
        queue,
        props.With (Navigation queue)
    )

let Make<'Route, 'ResultlessDialog, 'ResultfulDialog when 'Route: equality> =
    makeFnConstructor "LibRouter.Components.With.Navigation" renderFn

type Actions<'Route, 'ResultlessDialog, 'ResultfulDialog> = NoActions
type Estate<'Route, 'ResultlessDialog, 'ResultfulDialog> = NoEstate3<'Route, 'ResultlessDialog, 'ResultfulDialog>
type Pstate = NoPstate
