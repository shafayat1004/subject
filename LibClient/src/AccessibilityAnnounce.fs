/// Cross-platform status announcements (accessibility/backlog.md item #8).
module LibClient.AccessibilityAnnounce

open Fable.Core.JsInterop
open LibClient.Accessibility

let announce (message: string) (politeness: AccessibilityLiveRegion) : unit =
    UiActionLog.UiObservability.announce message politeness

    try
        ReactXP.RNSeam.AccessibilityInfoModule?announceForAccessibility(message)
        |> ignore
    with _ ->
        Noop
