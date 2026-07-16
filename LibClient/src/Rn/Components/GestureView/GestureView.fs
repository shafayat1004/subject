[<AutoOpen>]
module Rn.Components.GestureView

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Browser.Types
open LibClient

// NOTE since these types I expect to only be used for
// consumption, I'm leaving them as auto-converted ts2fable interfaces

type [<AllowNullLiteral>] GestureState =
    abstract isTouch:   bool with get, set
    abstract timeStamp: float with get, set

type [<AllowNullLiteral>] MultiTouchGestureState =
    inherit GestureState
    abstract initialCenterClientX: float with get, set
    abstract initialCenterClientY: float with get, set
    abstract initialCenterPageX:   float with get, set
    abstract initialCenterPageY:   float with get, set
    abstract initialWidth:         float with get, set
    abstract initialHeight:        float with get, set
    abstract initialDistance:      float with get, set
    abstract initialAngle:         float with get, set
    abstract centerClientX:        float with get, set
    abstract centerClientY:        float with get, set
    abstract centerPageX:          float with get, set
    abstract centerPageY:          float with get, set
    abstract velocityX:            float with get, set
    abstract velocityY:            float with get, set
    abstract width:                float with get, set
    abstract height:               float with get, set
    abstract distance:             float with get, set
    abstract angle:                float with get, set
    abstract isComplete:           bool with get, set

type [<AllowNullLiteral>] ScrollWheelGestureState =
    inherit GestureState
    abstract clientX:      float with get, set
    abstract clientY:      float with get, set
    abstract pageX:        float with get, set
    abstract pageY:        float with get, set
    abstract scrollAmount: float with get, set

type [<AllowNullLiteral>] PanGestureState =
    inherit GestureState
    abstract initialClientX: float with get, set
    abstract initialClientY: float with get, set
    abstract initialPageX:   float with get, set
    abstract initialPageY:   float with get, set
    abstract clientX:        float with get, set
    abstract clientY:        float with get, set
    abstract pageX:          float with get, set
    abstract pageY:          float with get, set
    abstract velocityX:      float with get, set
    abstract velocityY:      float with get, set
    abstract isComplete:     bool with get, set

type [<AllowNullLiteral>] TapGestureState =
    inherit GestureState
    abstract clientX: float with get, set
    abstract clientY: float with get, set
    abstract pageX:   float with get, set
    abstract pageY:   float with get, set

type GestureMouseCursor =
| Default    =  0
| Pointer    =  1
| Grab       =  2
| Move       =  3
| EWResize   =  4
| NSResize   =  5
| NESWResize =  6
| NWSEResize =  7
| NotAllowed =  8
| ZoomIn     =  9
| ZoomOut    = 10

type PreferredPanGesture =
| Horizontal = 0
| Vertical   = 1

do
    // Tell the browser to leave pans on a GestureView to the element instead of
    // letting a surrounding horizontal ScrollView scroll the page. Firefox Mobile
    // in particular needs this even when preventDefault() is called.
    Rn.LegacyStyles.Css.addCss """
[data-eggshell-gesture="horizontal"] {
    touch-action: pan-y;
}
[data-eggshell-gesture="vertical"] {
    touch-action: pan-x;
}
[data-eggshell-gesture="both"] {
    touch-action: none;
}
"""

module private GestureViewImpl =
    type private Props = {
        Children:          ReactChildrenProp option
        OnPan:             (PanGestureState -> unit) option
        OnPanVertical:     (PanGestureState -> unit) option
        OnPanHorizontal:   (PanGestureState -> unit) option
        OnTap:             (TapGestureState -> unit) option
        OnFocus:           (FocusEvent -> unit) option
        OnBlur:            (FocusEvent -> unit) option
        OnKeyPress:        (KeyboardEvent -> unit) option
        PreferredPan:      PreferredPanGesture option
        PanPixelThreshold: float option
        Styles:            array<Rn.Styles.FSharpDialect.ViewStyles> option
        XLegacyStyles:     List<Rn.LegacyStyles.RuntimeStyles> option
    }

    let private makePanState
            (initialClient: IRefValue<float * float>)
            (initialPage: IRefValue<float * float>)
            (e: obj)
            (isComplete: bool)
            : PanGestureState =
        let (initClientX, initClientY) = initialClient.current
        let (initPageX, initPageY) = initialPage.current

        createObj [
            "isTouch"        ==> true
            "timeStamp"      ==> JS.Constructors.Date.now()
            "initialClientX" ==> initClientX
            "initialClientY" ==> initClientY
            "initialPageX"   ==> initPageX
            "initialPageY"   ==> initPageY
            "clientX"        ==> e?x
            "clientY"        ==> e?y
            "pageX"          ==> e?absoluteX
            "pageY"          ==> e?absoluteY
            "velocityX"      ==> e?velocityX
            "velocityY"      ==> e?velocityY
            "isComplete"     ==> isComplete
        ]
        |> unbox

    let private makeTapState (e: obj) : TapGestureState =
        createObj [
            "isTouch"   ==> true
            "timeStamp" ==> JS.Constructors.Date.now()
            "clientX"   ==> e?x
            "clientY"   ==> e?y
            "pageX"     ==> e?absoluteX
            "pageY"     ==> e?absoluteY
        ]
        |> unbox

    let private dispatchPan (props: Props) (state: PanGestureState) : unit =
        match props.OnPan with
        | Some f -> f state
        | None   -> ()

        match props.PreferredPan with
        | Some PreferredPanGesture.Vertical -> ()
        | _ ->
            match props.OnPanHorizontal with
            | Some f -> f state
            | None   -> ()

        match props.PreferredPan with
        | Some PreferredPanGesture.Horizontal -> ()
        | _ ->
            match props.OnPanVertical with
            | Some f -> f state
            | None   -> ()

    [<Component>]
    let private Render (props: Props) : ReactElement =
        let initialClient = Hooks.useRef (0.0, 0.0)
        let initialPage = Hooks.useRef (0.0, 0.0)
        let isActive = Hooks.useRef false

        let threshold = props.PanPixelThreshold |> Option.defaultValue 10.0

        // GestureView's own page origin, measured on layout. Used to convert an absolute
        // touch pageX/pageY into a coordinate relative to THIS view, which is what a tap's
        // `clientX`/`clientY` are meant to be. nativeEvent.locationX is relative to the
        // deepest touched child (e.g. one SegmentedControl cell), not this view, so it can't
        // be used to locate a tap across the whole view.
        let selfOriginRef = Hooks.useRef (0.0, 0.0)
        let nodeRef = Hooks.useRef (null: obj)

        // Only tap consumers (SegmentedControl) need a view-relative coordinate. Measuring
        // every GestureView on every layout floods the RN bridge with measureInWindow
        // callbacks (hundreds pending), which starves touch delivery. So measurement is
        // gated to OnTap views, and re-issued only when the cached origin is stale.
        let needsMeasure = props.OnTap.IsSome

        let measureSelf () =
            let node = nodeRef.current
            if needsMeasure && not (isNullOrUndefined node) then
                try
                    node?measureInWindow(fun (x: float) (y: float) (_w: float) (_h: float) ->
                        selfOriginRef.current <- (x, y))
                    |> ignore
                with _ -> ()

        let inline preventDefault (e: obj) =
            try e?preventDefault()             |> ignore with _ -> ()
            try e?nativeEvent?preventDefault() |> ignore with _ -> ()

        // React Native pools synthetic events: after a responder handler returns, RN nulls
        // out `nativeEvent`. onResponderRelease in particular hands us an event whose
        // `nativeEvent` is already null, so reading `nativeEvent.pageX` there throws. We must
        // read coordinate primitives synchronously inside grant/move and keep them, never
        // retain the event object. Coords are (locationX, locationY, pageX, pageY).
        let lastCoordsRef = Hooks.useRef (0.0, 0.0, 0.0, 0.0)

        // Read coords from a live event; returns None if nativeEvent is unavailable (pooled).
        let readCoords (e: obj) : (float * float * float * float) option =
            let ne = e?nativeEvent
            if isNullOrUndefined ne then
                None
            else
                Some (ne?locationX, ne?locationY, ne?pageX, ne?pageY)

        // Update lastCoordsRef from a live event when possible, and return the coords to use
        // (falling back to the previously captured ones for a pooled release event).
        let coordsFrom (e: obj) : float * float * float * float =
            match readCoords e with
            | Some coords ->
                lastCoordsRef.current <- coords
                coords
            | None -> lastCoordsRef.current

        let makeTapState (locX, locY, pgX, pgY) : TapGestureState =
            createObj [
                "isTouch"   ==> true
                "timeStamp" ==> JS.Constructors.Date.now()
                "clientX"   ==> locX
                "clientY"   ==> locY
                "pageX"     ==> pgX
                "pageY"     ==> pgY
            ]
            |> unbox

        let makePanState (locX, locY, pgX, pgY) (isComplete: bool) : PanGestureState =
            let (initClientX, initClientY) = initialClient.current
            let (initPageX, initPageY) = initialPage.current
            createObj [
                "isTouch"        ==> true
                "timeStamp"      ==> JS.Constructors.Date.now()
                "initialClientX" ==> initClientX
                "initialClientY" ==> initClientY
                "initialPageX"   ==> initPageX
                "initialPageY"   ==> initPageY
                "clientX"        ==> locX
                "clientY"        ==> locY
                "pageX"          ==> pgX
                "pageY"          ==> pgY
                "velocityX"      ==> 0.0
                "velocityY"      ==> 0.0
                "isComplete"     ==> isComplete
            ]
            |> unbox

        let viewProps = createEmpty
        viewProps?onFocus  <- props.OnFocus
        viewProps?onBlur   <- props.OnBlur
        viewProps?onKeyPress <- props.OnKeyPress
        if needsMeasure then
            viewProps?ref <- fun (n: obj) ->
                nodeRef.current <- n
                measureSelf ()
            viewProps?onLayout <- fun (_e: obj) -> measureSelf ()

        if props.OnPan.IsSome || props.OnPanHorizontal.IsSome || props.OnPanVertical.IsSome || props.OnTap.IsSome then
            let inline dxOf (_, _, pgX, _) = pgX - fst initialPage.current
            let inline dyOf (_, _, _, pgY) = pgY - snd initialPage.current

            viewProps?dataSet <-
                let gestureDirection =
                    match props.PreferredPan with
                    | Some PreferredPanGesture.Vertical -> "vertical"
                    | Some PreferredPanGesture.Horizontal -> "horizontal"
                    | _ ->
                        if props.OnPanVertical.IsSome && not props.OnPanHorizontal.IsSome then
                            "vertical"
                        elif props.OnPanHorizontal.IsSome && not props.OnPanVertical.IsSome then
                            "horizontal"
                        else
                            "both"
                createObj [ "eggshellGesture" ==> gestureDirection ]

            viewProps?onStartShouldSetResponder <- fun (e: obj) ->
                preventDefault e
                true
            viewProps?onMoveShouldSetResponder  <- fun (e: obj) ->
                preventDefault e
                true
            viewProps?onResponderGrant <- fun (e: obj) ->
                preventDefault e
                measureSelf ()
                let ((locX, locY, pgX, pgY) as coords) = coordsFrom e
                initialClient.current <- (locX, locY)
                initialPage.current   <- (pgX, pgY)
                isActive.current      <- false
                makePanState coords false |> dispatchPan props
            // For a preferred-direction GestureView, only movement in that direction marks
            // the gesture as "active". Perpendicular jitter (e.g. vertical wobble on a
            // horizontal SegmentedControl) would otherwise suppress onTap on Android.
            let isSignificant coords =
                match props.PreferredPan with
                | Some PreferredPanGesture.Horizontal -> abs (dxOf coords) > threshold
                | Some PreferredPanGesture.Vertical   -> abs (dyOf coords) > threshold
                | _                                   -> abs (dxOf coords) > threshold || abs (dyOf coords) > threshold

            viewProps?onResponderMove <- fun (e: obj) ->
                preventDefault e
                let coords = coordsFrom e
                if not isActive.current && isSignificant coords then
                    isActive.current <- true
                makePanState coords false |> dispatchPan props
            viewProps?onResponderRelease <- fun (e: obj) ->
                // Release event's nativeEvent is pooled/null; coordsFrom falls back to the last
                // captured coords. isActive was set during the move phase, so it gates tap
                // without needing valid release coordinates.
                let coords = coordsFrom e
                makePanState coords true |> dispatchPan props
                if not isActive.current then
                    match props.OnTap with
                    | Some onTap ->
                        // Report the tap relative to THIS view (pageX minus the view's page
                        // origin), not the child-relative locationX.
                        let (_, _, pgX, pgY) = coords
                        let (ox, oy) = selfOriginRef.current
                        makeTapState (pgX - ox, pgY - oy, pgX, pgY) |> onTap
                    | None -> ()
                isActive.current <- false
            viewProps?onResponderTerminate <- fun (e: obj) ->
                makePanState (coordsFrom e) true |> dispatchPan props
                isActive.current <- false
            viewProps?onResponderTerminationRequest <- fun (_e: obj) -> true

            // Stop a surrounding horizontal ScrollView from stealing the pan on web.
            viewProps?onTouchStart <- fun (e: obj) -> preventDefault e
            viewProps?onTouchMove  <- fun (e: obj) -> preventDefault e

        let combinedStyles: array<obj> =
            let legacyStyles: array<obj> =
                match props.XLegacyStyles with
                | None | Some [] -> [||]
                | Some styles ->
                    [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<Rn.Styles.FSharpDialect.ViewStyles> "Rn.Components.GestureView" styles |]
                    |> Array.map (fun s -> (!!s) :> obj)

            let explicitStyles: array<obj> =
                props.Styles
                |> Option.defaultValue [||]
                |> Array.map (fun s -> (!!s) :> obj)

            Array.concat [ legacyStyles; explicitStyles ]

        viewProps?style <- combinedStyles

        let children =
            props.Children
            |> Option.map tellReactArrayKeysAreOkay
            |> Option.getOrElse [||]
            |> ThirdParty.fixPotentiallySingleChild

        Rn.RnPrimitives.createElement Rn.RnPrimitives.View viewProps children

    let build
            (children:          ReactChildrenProp option)
            (onPan:             (PanGestureState -> unit) option)
            (onPanVertical:     (PanGestureState -> unit) option)
            (onPanHorizontal:   (PanGestureState -> unit) option)
            (onTap:             (TapGestureState -> unit) option)
            (onFocus:           (FocusEvent -> unit) option)
            (onBlur:            (FocusEvent -> unit) option)
            (onKeyPress:        (KeyboardEvent -> unit) option)
            (preferredPan:      PreferredPanGesture option)
            (panPixelThreshold: float option)
            (styles:            array<Rn.Styles.FSharpDialect.ViewStyles> option)
            (xLegacyStyles:     List<Rn.LegacyStyles.RuntimeStyles> option)
            : ReactElement =
        Render {
            Children          = children
            OnPan             = onPan
            OnPanVertical     = onPanVertical
            OnPanHorizontal   = onPanHorizontal
            OnTap             = onTap
            OnFocus           = onFocus
            OnBlur            = onBlur
            OnKeyPress        = onKeyPress
            PreferredPan      = preferredPan
            PanPixelThreshold = panPixelThreshold
            Styles            = styles
            XLegacyStyles     = xLegacyStyles
        }

type Rn.Components.Constructors.Rn with
    static member GestureView(
        ?children:          ReactChildrenProp,
        ?onPinchZoom:       MultiTouchGestureState -> unit,
        ?onRotate:          MultiTouchGestureState -> unit,
        ?onScrollWheel:     ScrollWheelGestureState -> unit,
        ?mouseOverCursor:   GestureMouseCursor,
        ?onPan:             PanGestureState -> unit,
        ?onPanVertical:     PanGestureState -> unit,
        ?onPanHorizontal:   PanGestureState -> unit,
        ?onTap:             TapGestureState -> unit,
        ?onDoubleTap:       TapGestureState -> unit,
        ?onLongPress:       TapGestureState -> unit,
        ?onContextMenu:     TapGestureState -> unit,
        ?onFocus:           FocusEvent -> unit,
        ?onBlur:            FocusEvent -> unit,
        ?onKeyPress:        KeyboardEvent -> unit,
        ?preferredPan:      PreferredPanGesture,
        ?panPixelThreshold: float,
        ?releaseOnRequest:  bool,
        ?styles:            array<Rn.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:     List<Rn.LegacyStyles.RuntimeStyles>
    ) =
        // Pinch, rotate, scroll wheel, double-tap, long-press, context-menu,
        // mouse cursor and release-on-request are not wired to the responder system.
        // They are accepted to preserve the public API but currently have no effect.
        onPinchZoom      |> ignore
        onRotate         |> ignore
        onScrollWheel    |> ignore
        onTap            |> ignore
        onDoubleTap      |> ignore
        onLongPress      |> ignore
        onContextMenu    |> ignore
        mouseOverCursor  |> ignore
        releaseOnRequest |> ignore

        GestureViewImpl.build
            children
            onPan
            onPanVertical
            onPanHorizontal
            onTap
            onFocus
            onBlur
            onKeyPress
            preferredPan
            panPixelThreshold
            styles
            xLegacyStyles
