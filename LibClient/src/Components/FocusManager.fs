[<AutoOpen>]
module LibClient.Components.FocusManager

open Fable.Core.JsInterop
open LibClient

module LC =
    module FocusManager =
        /// Move focus to an element ref. Web: calls .focus(). Native: setAccessibilityFocus.
        let setFocusTo (element: obj) : unit =
#if EGGSHELL_PLATFORM_IS_WEB
            try element?focus() with _ -> ()
#else
            try
                ReactXP.RNSeam.AccessibilityInfoModule?setAccessibilityFocus(element) |> ignore
            with _ ->
                ()
#endif

        /// Move focus to a DOM element by id. Web-only; no-op on native.
        let setFocusById (id: string) : unit =
#if EGGSHELL_PLATFORM_IS_WEB
            try
                let el = Browser.Dom.document.getElementById id
                if not (isNull el) then el?focus()
            with _ ->
                ()
#else
            id |> ignore
#endif
