[<AutoOpen>]
module LibClient.Components.LiveRegion

open Fable.React
open Fable.Core.JsInterop
open LibClient
open LibClient.Accessibility
open LibClient.UiActionLog
open ReactXP.Components
open ReactXP.Components.View
open ReactXP.Helpers

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member LiveRegion(
            ?children: ReactChildrenProp,
            ?liveRegion: AccessibilityLiveRegion,
            ?testId: string,
            ?key: string
        ) : ReactElement =
        key |> ignore
        RX.View(
            ?testId = testId,
            ?accessibilityLiveRegion = (liveRegion |> Option.map (fun r -> unbox<AccessibilityLiveRegion> (int r))),
            children = (defaultArg children [||])
        )

module LC =
    module LiveRegion =
        let announce (message: string) (politeness: LibClient.Accessibility.AccessibilityLiveRegion) =
            UiActionLog.UiObservability.announce message politeness
            try
                ReactXP.Helpers.ReactXPRaw?Accessibility?announceForAccessibility(message) |> ignore
            with _ ->
                Noop
