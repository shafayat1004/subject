/// Cross-platform status announcements (ACCESSIBILITY_PLAN backlog #8).
module LibClient.AccessibilityAnnounce

open Fable.Core.JsInterop
open LibClient.Accessibility
open ReactXP.Helpers

let announce (message: string) (politeness: AccessibilityLiveRegion) : unit =
    UiActionLog.UiObservability.announce message politeness
    try
        ReactXPRaw?Accessibility?announceForAccessibility(message) |> ignore
    with _ ->
        Noop
