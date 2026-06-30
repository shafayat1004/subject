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

    let private dispatchPan (props: Props) (state: PanGestureState) : unit =
        props.OnPan |> Option.iter (fun f -> f state)

        match props.PreferredPan with
        | Some PreferredPanGesture.Vertical -> ()
        | _ -> props.OnPanHorizontal |> Option.iter (fun f -> f state)

        match props.PreferredPan with
        | Some PreferredPanGesture.Horizontal -> ()
        | _ -> props.OnPanVertical |> Option.iter (fun f -> f state)

    [<Component>]
    let private Render (props: Props) : ReactElement =
        let initialClient = Hooks.useRef (0.0, 0.0)
        let initialPage = Hooks.useRef (0.0, 0.0)

        let threshold = props.PanPixelThreshold |> Option.defaultValue 10.0

        let pan = ReactXP.RNSeam.Gesture?Pan()

        match props.PreferredPan with
        | Some PreferredPanGesture.Vertical ->
            pan?activeOffsetY(-threshold, threshold) |> ignore
            pan?activeOffsetX(-1000.0, 1000.0) |> ignore
        | Some _ ->
            pan?activeOffsetX(-threshold, threshold) |> ignore
            pan?activeOffsetY(-1000.0, 1000.0) |> ignore
        | None ->
            pan?activeOffsetX(-threshold, threshold) |> ignore
            pan?activeOffsetY(-threshold, threshold) |> ignore

        pan?onBegin(fun (e: obj) ->
            initialClient.current <- (e?x - e?translationX, e?y - e?translationY)
            initialPage.current   <- (e?absoluteX - e?translationX, e?absoluteY - e?translationY)
        ) |> ignore

        pan?onUpdate(fun (e: obj) ->
            makePanState initialClient initialPage e false
            |> dispatchPan props
        ) |> ignore

        pan?onEnd(fun (e: obj) ->
            makePanState initialClient initialPage e true
            |> dispatchPan props
        ) |> ignore

        let viewProps = createEmpty
        viewProps?onFocus  <- props.OnFocus
        viewProps?onBlur   <- props.OnBlur
        viewProps?onKeyPress <- props.OnKeyPress

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

        let viewElement = ReactXP.RNSeam.createElement ReactXP.RNSeam.View viewProps children

        ReactXP.RNSeam.createElement
            ReactXP.RNSeam.GestureDetector
            (createObj [ "gesture" ==> pan ])
            [| viewElement |]

    let build
            (children:          ReactChildrenProp option)
            (onPan:             (PanGestureState -> unit) option)
            (onPanVertical:     (PanGestureState -> unit) option)
            (onPanHorizontal:   (PanGestureState -> unit) option)
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
        // Pinch, rotate, scroll wheel, tap, double-tap, long-press, context-menu,
        // mouse cursor and release-on-request are not yet wired to react-native-gesture-handler.
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
            onFocus
            onBlur
            onKeyPress
            preferredPan
            panPixelThreshold
            styles
            xLegacyStyles
