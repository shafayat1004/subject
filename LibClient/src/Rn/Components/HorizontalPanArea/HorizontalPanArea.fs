[<AutoOpen>]
module Rn.Components.HorizontalPanArea

// Cross-platform horizontal-swipe area.
//
// Native (Android/iOS) uses react-native-gesture-handler's PanGestureHandler with
// `activeOffsetX` / `failOffsetY`, so a surrounding vertical ScrollView yields
// correctly through NATIVE gesture arbitration. The custom JS-responder
// `GestureView` cannot win against a native ScrollView on Android (the native
// scroll terminates the JS responder regardless of `onResponderTerminationRequest`),
// which is why swipe-in-a-list stuttered, leaked to scroll, and mis-fired taps there.
//
// We use the imperative PanGestureHandler component API (not the newer
// GestureDetector + Gesture.Pan builder): the builder constructs Gesture objects
// whose Babel-emitted field initializers throw "Cannot define property" under
// Hermes in this app, and it needs reanimated for its animated path. The component
// API has none of that and runs its callbacks on the JS thread.
//
// Web keeps the `GestureView` responder path: `touch-action` CSS already stops the
// surrounding ScrollView from stealing the pan there, so it was never broken and we
// avoid pulling gesture-handler into the web bundle.
//
// Callbacks report `translationX`: pixels from the gesture start, negative = leftward.

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open LibClient

#if !EGGSHELL_PLATFORM_IS_WEB
// A plain JS array literal. A Fable `float[]`/`int[]` compiles to a *typed array*
// (Float64Array/Int32Array), whose indices are non-configurable. RN dev mode
// deep-freezes the arguments to native module calls, and RNGH passes
// activeOffsetX/failOffsetY straight to the native handler config, so a typed array
// makes the freeze throw "Cannot define property". A real Array freezes fine.
[<Emit("[$0, $1]")>]
let private jsPair (a: float) (b: float) : obj = jsNative

module private Rngh =
    let private PanGestureHandlerRaw: obj = import "PanGestureHandler" "react-native-gesture-handler"
    let makePanGestureHandler: obj -> array<ReactElement> -> ReactElement =
        LibClient.ThirdParty.wrapComponent<obj>(PanGestureHandlerRaw)

    let State: obj = import "State" "react-native-gesture-handler"

    let private GestureHandlerRootViewRaw: obj = import "GestureHandlerRootView" "react-native-gesture-handler"
    let makeRootView: obj -> array<ReactElement> -> ReactElement =
        LibClient.ThirdParty.wrapComponent<obj>(GestureHandlerRootViewRaw)
#endif

type Rn.Components.Constructors.Rn with

    /// Horizontal swipe area. `onUpdate`/`onEnd` receive translationX (px from the
    /// gesture start; negative = leftward). `activeOffsetX` is how far horizontally a
    /// drag must travel before the swipe activates; `failOffsetY` is how far vertically
    /// it may drift before the gesture is abandoned to the enclosing scroll.
    [<Component>]
    static member HorizontalPanArea
        (
            onStart:        unit -> unit,
            onUpdate:       float -> unit,
            onEnd:          float -> unit,
            children:       array<ReactElement>,
            ?activeOffsetX: float,
            ?failOffsetY:   float
        ) : ReactElement =

        let activeX = defaultArg activeOffsetX 15.0
        let failY   = defaultArg failOffsetY   12.0

        // Always call the latest closures (they capture current props like isOpen/onDelete).
        let onStartRef  = Hooks.useRef onStart
        let onUpdateRef = Hooks.useRef onUpdate
        let onEndRef    = Hooks.useRef onEnd
        onStartRef.current  <- onStart
        onUpdateRef.current <- onUpdate
        onEndRef.current    <- onEnd

#if !EGGSHELL_PLATFORM_IS_WEB
        // Only settle if the gesture actually activated (a vertical scroll fails the
        // recognizer via failOffsetY and never starts).
        let startedRef = Hooks.useRef false

        let stateActive    = Rngh.State?ACTIVE    |> unbox<int>
        let stateEnd       = Rngh.State?END       |> unbox<int>
        let stateFailed    = Rngh.State?FAILED    |> unbox<int>
        let stateCancelled = Rngh.State?CANCELLED |> unbox<int>

        let props =
            createObj [
                "activeOffsetX" ==> jsPair -activeX activeX
                "failOffsetY"   ==> jsPair -failY failY
                "onGestureEvent" ==>
                    (fun (e: obj) ->
                        if startedRef.current then
                            onUpdateRef.current (e?nativeEvent?translationX |> unbox<float>))
                "onHandlerStateChange" ==>
                    (fun (e: obj) ->
                        let ne = e?nativeEvent
                        let st = ne?state |> unbox<int>
                        if st = stateActive then
                            startedRef.current <- true
                            onStartRef.current ()
                        elif st = stateEnd || st = stateFailed || st = stateCancelled then
                            if startedRef.current then
                                startedRef.current <- false
                                onEndRef.current (ne?translationX |> unbox<float>))
            ]

        // PanGestureHandler attaches a ref + `collapsable` to its single child, so the
        // child must be a real native view. Our AnimatableView is a wrapComponent
        // function component that cannot receive a ref, so wrap it in a raw RN View.
        let innerView =
            Rn.RnPrimitives.createElement
                Rn.RnPrimitives.View
                (createObj [ "collapsable" ==> false ])
                children

        Rngh.makePanGestureHandler props [| innerView |]
#else
        // Web: translate GestureView's absolute pageX into translationX. RN pools
        // synthetic events, so pageX can be null on the completing event; fall back
        // to the last dataful pageX. Activation is gated by `activeX` to mirror the
        // native `activeOffsetX` (so a tap does not start a drag).
        let startedRef = Hooks.useRef false
        let lastPXRef  = Hooks.useRef 0.0

        let onPan (gs: Rn.Components.GestureView.PanGestureState) =
            let pageX =
                if isNullOrUndefined (box gs.pageX) then
                    lastPXRef.current
                else
                    lastPXRef.current <- gs.pageX
                    gs.pageX

            let tx = pageX - gs.initialPageX

            if gs.isComplete then
                if startedRef.current then
                    startedRef.current <- false
                    onEndRef.current tx
            else
                if (not startedRef.current) && abs tx >= activeX then
                    startedRef.current <- true
                    onStartRef.current ()
                if startedRef.current then
                    onUpdateRef.current tx

        Rn.GestureView(
            preferredPan      = Rn.Components.GestureView.PreferredPanGesture.Horizontal,
            panPixelThreshold = activeX,
            onPanHorizontal   = onPan,
            children          = children
        )
#endif

#if !EGGSHELL_PLATFORM_IS_WEB
    /// Native gesture-handler root. Must be an ancestor of any `HorizontalPanArea`
    /// (or other gesture-handler) or the gestures silently no-op. Native only; on web
    /// no root wrapper is needed (guard the call site with the platform define).
    ///
    /// `fillParent` (default true) makes the root `flex: 1` -- correct at the app root.
    /// Pass `false` to wrap only an inline widget (a demo, a single pannable row): the
    /// root then sizes to its content instead of stretching. Scoping the root to just
    /// the gesture subtree keeps RNGH's native touch arbitration off the rest of the
    /// app, which otherwise breaks the JS-responder gestures (`Rn.GestureView` in
    /// `Draggable`/`Scrim`) -- an app-wide root made the drawer close on vertical scroll.
    static member GestureHandlerRootView(children: array<ReactElement>, ?fillParent: bool) : ReactElement =
        let style = if defaultArg fillParent true then createObj [ "flex" ==> 1 ] else createEmpty
        Rngh.makeRootView (createObj [ "style" ==> style ]) children
#endif
