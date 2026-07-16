[<AutoOpen>]
module LibRouter.Components.NativeBackButton

open Fable.Core.JsInterop
open Fable.React
open LibClient
open Rn
open LibRouter.Components.Constructors

// React Native's BackHandler. On Android it fires 'hardwareBackPress' for the OS Back
// gesture/button; on iOS it never fires (iOS has no hardware back -- see LR.EdgeSwipeBack for
// the iOS edge-swipe counterpart). On web `react-native`'s BackHandler is a no-op stub.
let private BackHandler: obj = import "BackHandler" "react-native"

type LR with
    [<Component>]
    static member NativeBackButton (goBack: unit -> unit, ?canGoBack: unit -> bool, ?key: string) : ReactElement =
        ignore key

        // When canGoBack is omitted, default to "always can go back" to preserve the historical
        // behavior of existing callers (AppTodo, AppPerformancePlayground) that pass only goBack.
        // The gallery passes an explicit canGoBack (pathname <> "/") so Back at the root route
        // returns false and lets the OS close the app instead of trapping the user forever.
        let canGoBack = canGoBack |> Option.defaultValue (fun () -> true)

        // Hold the BackHandler subscription in a ref so cleanup can .remove() it. The previous
        // implementation discarded the subscription and returned an empty cleanup, leaking a
        // listener per mount/key change (each navigation re-ran the effect with stale goBack).
        let maybeSubscriptionRef: IRefHook<Option<obj>> = Hooks.useRef None

        Hooks.useEffectDisposableFn(
            (fun () ->
                // react-native's BackHandler is a no-op stub on web that logs
                // "BackHandler is not supported on web and should not be used" on every
                // addEventListener call, so skip the subscription entirely there. This
                // mirrors LR.EdgeSwipeBack's `Rn.Runtime.isWeb ()` guard. The hook itself
                // still runs unconditionally (Rules of Hooks); only the body is skipped.
                if not (Rn.Runtime.isWeb ()) then
                    // Remove any subscription left from a previous effect run.
                    maybeSubscriptionRef.current
                    |> Option.sideEffect (fun sub -> sub?remove())

                    let subscription: obj =
                        BackHandler?addEventListener(
                            "hardwareBackPress",
                            (fun () ->
                                if canGoBack () then
                                    goBack ()
                                    true    // we handled it: do not let the OS close the app
                                else
                                    false   // nothing to pop: let the OS perform the default (close app)
                            )
                        )
                    maybeSubscriptionRef.current <- Some subscription
            ),
            (fun () ->
                if not (Rn.Runtime.isWeb ()) then
                    maybeSubscriptionRef.current
                    |> Option.sideEffect (fun sub -> sub?remove())
                    maybeSubscriptionRef.current <- None
            ),
            [| box goBack; box canGoBack |]
        )

        noElement
