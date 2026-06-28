[<AutoOpen>]
module LibRouter.Components.LogRouteTransitions

open Fable.React
open LibClient
open LibRouter.Components
open LibRouter.Components.Constructors

let private maybeLastUrl = ref None

type LR with
    [<Component>]
    static member LogRouteTransitions (?key: string) : ReactElement =
        ignore key
        let location = Router.useLocation()

        if !maybeLastUrl <> Some location.Url then
            maybeLastUrl := Some location.Url
            Telemetry.TrackScreenView location.Url Map.empty
            LibClient.UiActionLog.setCurrentRoute location.Url

        noElement
