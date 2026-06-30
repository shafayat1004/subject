[<AutoOpen>]
module ReactXP.Components.GestureView

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Browser.Types
open LibClient

// NOTE since these types I expect to only be used for
// consumption, I'm leaving them as auto-converted ts2fable interfaces

type [<AllowNullLiteral>] GestureState =
    abstract isTouch: bool with get, set
    abstract timeStamp: float with get, set

type [<AllowNullLiteral>] MultiTouchGestureState =
    inherit GestureState
    abstract initialCenterClientX: float with get, set
    abstract initialCenterClientY: float with get, set
    abstract initialCenterPageX: float with get, set
    abstract initialCenterPageY: float with get, set
    abstract initialWidth: float with get, set
    abstract initialHeight: float with get, set
    abstract initialDistance: float with get, set
    abstract initialAngle: float with get, set
    abstract centerClientX: float with get, set
    abstract centerClientY: float with get, set
    abstract centerPageX: float with get, set
    abstract centerPageY: float with get, set
    abstract velocityX: float with get, set
    abstract velocityY: float with get, set
    abstract width: float with get, set
    abstract height: float with get, set
    abstract distance: float with get, set
    abstract angle: float with get, set
    abstract isComplete: bool with get, set

type [<AllowNullLiteral>] ScrollWheelGestureState =
    inherit GestureState
    abstract clientX: float with get, set
    abstract clientY: float with get, set
    abstract pageX: float with get, set
    abstract pageY: float with get, set
    abstract scrollAmount: float with get, set

type [<AllowNullLiteral>] PanGestureState =
    inherit GestureState
    abstract initialClientX: float with get, set
    abstract initialClientY: float with get, set
    abstract initialPageX: float with get, set
    abstract initialPageY: float with get, set
    abstract clientX: float with get, set
    abstract clientY: float with get, set
    abstract pageX: float with get, set
    abstract pageY: float with get, set
    abstract velocityX: float with get, set
    abstract velocityY: float with get, set
    abstract isComplete: bool with get, set

type [<AllowNullLiteral>] TapGestureState =
    inherit GestureState
    abstract clientX: float with get, set
    abstract clientY: float with get, set
    abstract pageX: float with get, set
    abstract pageY: float with get, set

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
        Styles:            array<ReactXP.Styles.FSharpDialect.ViewStyles> option
        XLegacyStyles:     List<ReactXP.LegacyStyles.RuntimeStyles> option
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
        | None -> ()

        match props.PreferredPan with
        | Some PreferredPanGesture.Vertical -> ()
        | _ ->
            match props.OnPanHorizontal with
            | Some f -> f state
            | None -> ()

        match props.PreferredPan with
        | Some PreferredPanGesture.Horizontal -> ()
        | _ ->
            match props.OnPanVertical with
            | Some f -> f state
            | None -> ()

    [<Component>]
    let private Render (props: Props) : ReactElement =
        let initialClient = Hooks.useRef (0.0, 0.0)
        let initialPage = Hooks.useRef (0.0, 0.0)
        let isActive = Hooks.useRef false

        let threshold = props.PanPixelThreshold |> Option.defaultValue 10.0

        let inline locationX (e: obj) = e?nativeEvent?locationX
        let inline locationY (e: obj) = e?nativeEvent?locationY
        let inline pageXOf (e: obj) = e?nativeEvent?pageX
        let inline pageYOf (e: obj) = e?nativeEvent?pageY

        let inline preventDefault (e: obj) =
            try e?preventDefault() |> ignore with _ -> ()
            try e?nativeEvent?preventDefault() |> ignore with _ -> ()

        let makeTapStateFromEvent (e: obj) : TapGestureState =
            createObj [
                "isTouch"   ==> true
                "timeStamp" ==> JS.Constructors.Date.now()
                "clientX"   ==> locationX e
                "clientY"   ==> locationY e
                "pageX"     ==> pageXOf e
                "pageY"     ==> pageYOf e
            ]
            |> unbox

        let makePanStateFromEvent (e: obj) (isComplete: bool) : PanGestureState =
            let (initClientX, initClientY) = initialClient.current
            let (initPageX, initPageY) = initialPage.current
            let px = pageXOf e
            let py = pageYOf e
            createObj [
                "isTouch"        ==> true
                "timeStamp"      ==> JS.Constructors.Date.now()
                "initialClientX" ==> initClientX
                "initialClientY" ==> initClientY
                "initialPageX"   ==> initPageX
                "initialPageY"   ==> initPageY
                "clientX"        ==> locationX e
                "clientY"        ==> locationY e
                "pageX"          ==> px
                "pageY"          ==> py
                "velocityX"      ==> 0.0
                "velocityY"      ==> 0.0
                "isComplete"     ==> isComplete
            ]
            |> unbox

        let viewProps = createEmpty
        viewProps?onFocus  <- props.OnFocus
        viewProps?onBlur   <- props.OnBlur
        viewProps?onKeyPress <- props.OnKeyPress

        if props.OnPan.IsSome || props.OnPanHorizontal.IsSome || props.OnPanVertical.IsSome || props.OnTap.IsSome then
            let inline dx e = pageXOf e - fst initialPage.current
            let inline dy e = pageYOf e - snd initialPage.current

            viewProps?onStartShouldSetResponder <- fun (e: obj) ->
                preventDefault e
                true
            viewProps?onMoveShouldSetResponder  <- fun (e: obj) ->
                preventDefault e
                true
            viewProps?onResponderGrant <- fun (e: obj) ->
                preventDefault e
                initialClient.current <- (locationX e, locationY e)
                initialPage.current   <- (pageXOf e, pageYOf e)
                isActive.current      <- false
                makePanStateFromEvent e false |> dispatchPan props
            viewProps?onResponderMove <- fun (e: obj) ->
                preventDefault e
                if not isActive.current && (abs (dx e) > threshold || abs (dy e) > threshold) then
                    isActive.current <- true
                makePanStateFromEvent e false |> dispatchPan props
            viewProps?onResponderRelease <- fun (e: obj) ->
                makePanStateFromEvent e true |> dispatchPan props
                if not isActive.current && abs (dx e) < threshold && abs (dy e) < threshold then
                    match props.OnTap with
                    | Some onTap -> makeTapStateFromEvent e |> onTap
                    | None -> ()
                isActive.current <- false
            viewProps?onResponderTerminate <- fun (e: obj) ->
                makePanStateFromEvent e true |> dispatchPan props
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
                    [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ReactXP.Styles.FSharpDialect.ViewStyles> "ReactXP.Components.GestureView" styles |]
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

        ReactXP.RNSeam.createElement ReactXP.RNSeam.View viewProps children

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
            (styles:            array<ReactXP.Styles.FSharpDialect.ViewStyles> option)
            (xLegacyStyles:     List<ReactXP.LegacyStyles.RuntimeStyles> option)
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

type ReactXP.Components.Constructors.RX with
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
        ?styles:            array<ReactXP.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:     List<ReactXP.LegacyStyles.RuntimeStyles>
    ) =
        // Pinch, rotate, scroll wheel, double-tap, long-press, context-menu,
        // mouse cursor and release-on-request are not wired to the responder system.
        // They are accepted to preserve the public API but currently have no effect.
        onPinchZoom    |> ignore
        onRotate       |> ignore
        onScrollWheel  |> ignore
        onTap          |> ignore
        onDoubleTap    |> ignore
        onLongPress    |> ignore
        onContextMenu  |> ignore
        mouseOverCursor |> ignore
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
