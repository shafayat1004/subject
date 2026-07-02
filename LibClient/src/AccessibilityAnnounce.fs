/// Cross-platform status announcements (accessibility/backlog.md item #8).
module LibClient.AccessibilityAnnounce

open Fable.Core.JsInterop
open LibClient.Accessibility

#if EGGSHELL_PLATFORM_IS_WEB
open Fable.Core.JS

// Lazily create a hidden aria-live DOM node and update its text.
// Clear then re-set after 50ms so screen readers see a fresh DOM mutation even
// when the same string is announced twice in a row.
let private getOrCreateLiveRegion (politeness: string) : Browser.Types.Element =
    let id = sprintf "eggshell-a11y-live-%s" politeness
    match Browser.Dom.document.getElementById id with
    | el when not (isNull el) -> el
    | _ ->
        let el = Browser.Dom.document.createElement "div"
        el?id <- id
        el?setAttribute ("aria-live", politeness) |> ignore
        el?setAttribute ("aria-atomic", "true") |> ignore
        el?setAttribute ("aria-relevant", "additions text") |> ignore
        el?setAttribute (
            "style",
            "position:absolute;left:-9999px;width:1px;height:1px;overflow:hidden;"
        )
        |> ignore
        Browser.Dom.document.body?appendChild el |> ignore
        el

let private announceWeb (message: string) (politeness: AccessibilityLiveRegion) : unit =
    let p =
        match politeness with
        | AccessibilityLiveRegion.Assertive -> "assertive"
        | _ -> "polite"

    let region = getOrCreateLiveRegion p
    region?textContent <- ""
    setTimeout (fun () -> region?textContent <- message) 50 |> ignore
#endif

let announce (message: string) (politeness: AccessibilityLiveRegion) : unit =
    UiActionLog.UiObservability.announce message politeness
#if EGGSHELL_PLATFORM_IS_WEB
    announceWeb message politeness
#else
    try
        ReactXP.RNSeam.AccessibilityInfoModule?announceForAccessibility(message)
        |> ignore
    with _ ->
        Noop
#endif

/// Announce with Polite politeness (most common case).
let announcePolite (message: string) : unit =
    announce message AccessibilityLiveRegion.Polite
