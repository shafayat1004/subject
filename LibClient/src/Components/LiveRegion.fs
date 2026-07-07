[<AutoOpen>]
module LibClient.Components.LiveRegion

open Fable.React
open Fable.Core.JsInterop
open LibClient
open LibClient.Accessibility
open LibClient.UiActionLog
open Rn.Components
open Rn.Components.View
open Rn.Helpers

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member LiveRegion(
            ?children: ReactChildrenProp,
            ?liveRegion: AccessibilityLiveRegion,
            ?testId: string,
            ?key: string
        ) : ReactElement =
        key |> ignore
        Rn.View(
            ?testId = testId,
            ?accessibilityLiveRegion = (liveRegion |> Option.map (fun r -> unbox<AccessibilityLiveRegion> (int r))),
            children = (defaultArg children [||])
        )

module LC =
    module LiveRegion =
        let announce (message: string) (politeness: LibClient.Accessibility.AccessibilityLiveRegion) =
            LibClient.AccessibilityAnnounce.announce message politeness

        /// Announce with Polite politeness (most common case).
        let announcePolite (message: string) =
            LibClient.AccessibilityAnnounce.announcePolite message
