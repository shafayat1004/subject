[<AutoOpen>]
module LibClient.Components.With_Accessibility

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components

module LC =
    module With =
        module Accessibility =
#if EGGSHELL_PLATFORM_IS_WEB
            let private mediaMatches (query: string) : bool =
                try
                    !!(Browser.Dom.window?matchMedia (query)?matches)
                with _ ->
                    false

            let private subscribeMedia (query: string) (onChange: bool -> unit) : (unit -> unit) =
                try
                    let mql = Browser.Dom.window?matchMedia (query)

                    let handler (_: Browser.Types.Event) = onChange !!mql?matches

                    mql?addEventListener ("change", handler) |> ignore
                    fun () -> mql?removeEventListener ("change", handler) |> ignore
                with _ ->
                    fun () -> ()

            let private querySettings () : AccessibilitySettings =
                { AccessibilitySettings.defaults with
                    ReduceMotion = mediaMatches "(prefers-reduced-motion: reduce)"
                    BoldText = mediaMatches "(prefers-contrast: more)"
                    ReduceTransparency = mediaMatches "(prefers-reduced-transparency: reduce)"
                    InvertColors = mediaMatches "(forced-colors: active)" }

            let private subscribe (onChange: AccessibilitySettings -> unit) : (unit -> unit) =
                let mutable current = querySettings ()

                let bump () =
                    let next = querySettings ()

                    if next <> current then
                        current <- next
                        onChange next

                let unsubscribers =
                    [ subscribeMedia "(prefers-reduced-motion: reduce)" (fun _ -> bump ())
                      subscribeMedia "(prefers-contrast: more)" (fun _ -> bump ())
                      subscribeMedia "(prefers-reduced-transparency: reduce)" (fun _ -> bump ())
                      subscribeMedia "(forced-colors: active)" (fun _ -> bump ()) ]

                fun () -> unsubscribers |> List.iter (fun u -> u ())
#else
            let private tryAsyncBool (query: string) (onResult: bool -> unit) : unit =
                async {
                    try
                        let! result =
                            ReactXP.RNSeam.AccessibilityInfoModule?(query)()
                            |> Async.AwaitPromise
                            |> Async.TryCatch

                        match result with
                        | Ok v -> onResult v
                        | Error _ -> onResult false
                    with _ ->
                        onResult false
                }
                |> Async.StartImmediate

            let private tryFontScale (onResult: float -> unit) : unit =
                async {
                    try
                        let! scale =
                            ReactXP.RNSeam.AccessibilityInfoModule?getFontScale()
                            |> Async.AwaitPromise
                            |> Async.TryCatch

                        match scale with
                        | Ok v -> onResult v
                        | Error _ -> onResult 1.0
                    with _ ->
                        onResult 1.0
                }
                |> Async.StartImmediate

            let private querySettings () : AccessibilitySettings = AccessibilitySettings.defaults

            let private trySubscribeBoolEvent
                (eventName: string)
                (update: bool -> AccessibilitySettings -> AccessibilitySettings)
                (bump: (AccessibilitySettings -> AccessibilitySettings) -> unit)
                (unsubscribers: ResizeArray<unit -> unit>)
                : unit =
                try
                    let handler (isEnabled: bool) = bump (fun s -> update isEnabled s)

                    let subscription =
                        ReactXP.RNSeam.AccessibilityInfoModule?addEventListener(eventName, handler)

                    unsubscribers.Add(fun () ->
                        try
                            subscription?remove () |> ignore
                        with _ ->
                            try
                                ReactXP.RNSeam.AccessibilityInfoModule?removeEventListener(eventName, handler)
                                |> ignore
                            with _ ->
                                ())
                with _ ->
                    ()

            let private subscribe (onChange: AccessibilitySettings -> unit) : (unit -> unit) =
                let mutable current = querySettings ()

                let bump (updater: AccessibilitySettings -> AccessibilitySettings) =
                    let next = updater current

                    if next <> current then
                        current <- next
                        onChange next

                let unsubscribers = ResizeArray<unit -> unit>()

                trySubscribeBoolEvent
                    "screenReaderChanged"
                    (fun v s -> { s with ScreenReaderEnabled = v })
                    bump
                    unsubscribers

                trySubscribeBoolEvent "reduceMotionChanged" (fun v s -> { s with ReduceMotion = v }) bump unsubscribers
                trySubscribeBoolEvent "boldTextChanged" (fun v s -> { s with BoldText = v }) bump unsubscribers

                trySubscribeBoolEvent
                    "reduceTransparencyChanged"
                    (fun v s -> { s with ReduceTransparency = v })
                    bump
                    unsubscribers

                trySubscribeBoolEvent "invertColorsChanged" (fun v s -> { s with InvertColors = v }) bump unsubscribers
                trySubscribeBoolEvent "grayscaleChanged" (fun v s -> { s with Grayscale = v }) bump unsubscribers

                tryAsyncBool "isScreenReaderEnabled" (fun v -> bump (fun s -> { s with ScreenReaderEnabled = v }))

                tryAsyncBool "isReduceMotionEnabled" (fun v -> bump (fun s -> { s with ReduceMotion = v }))

                tryAsyncBool "isBoldTextEnabled" (fun v -> bump (fun s -> { s with BoldText = v }))

                tryAsyncBool "isReduceTransparencyEnabled" (fun v -> bump (fun s -> { s with ReduceTransparency = v }))

                tryAsyncBool "isInvertColorsEnabled" (fun v -> bump (fun s -> { s with InvertColors = v }))

                tryAsyncBool "isGrayscaleEnabled" (fun v -> bump (fun s -> { s with Grayscale = v }))

                tryFontScale (fun scale -> bump (fun s -> { s with FontScale = scale }))

                fun () -> unsubscribers |> Seq.iter (fun u -> u ())
#endif

            let useSettings () : AccessibilitySettings =
                let settingsHook = Hooks.useState AccessibilitySettings.defaults

                Hooks.useEffect (
                    (fun () ->
                        settingsHook.update (querySettings ())
                        ignore (subscribe settingsHook.update)),
                    [||]
                )

                settingsHook.current

open LC.With.Accessibility

type LC.With with
    [<Component>]
    static member Accessibility(``with``: AccessibilitySettings -> ReactElement) : ReactElement =
        ``with`` (useSettings ())
