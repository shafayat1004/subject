[<AutoOpen>]
module LibClient.Components.Draggable

open System

open Fable.React
open Fable.Core.JsInterop

open LibClient

open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
type Position =
| Left
| Right
| Up
| Down
| Base

type IDraggableRef =
    abstract member SetPosition:     Position -> bool
    abstract member OnPanHorizontal: Rn.Components.GestureView.PanGestureState -> unit
    abstract member OnPanVertical:   Rn.Components.GestureView.PanGestureState -> unit

type DragTarget = {|
    ForwardThreshold:  int
    Offset:            int
    BackwardThreshold: int
|}

type PositionChangeReason =
| AnimationFinished
| ManualDrag

type Change =
| DragInducedAnimationStarted  of Target: Position
| PositionChanged of Target: Position * PositionChangeReason

type Direction =
| Horizontal
| Vertical

type Rn.Components.GestureView.PanGestureState with
    member this.DeltaX : int =
        this.pageX - this.initialPageX |> int

    member this.DeltaY : int =
        this.pageY - this.initialPageY |> int

[<RequireQualifiedAccess>]
module private Helpers =
    let range (maybeNegative: Option<DragTarget>) (maybePositive: Option<DragTarget>) : int * int =
        match (maybeNegative, maybePositive) with
        | (None,                None)                -> (0, 0)
        | (Some negativeTarget, None)                -> (-negativeTarget.Offset, 0)
        | (None,                Some positiveTarget) -> (0, positiveTarget.Offset)
        | (Some negativeTarget, Some positiveTarget) -> (-negativeTarget.Offset, positiveTarget.Offset)

    let limitToRange (range: int * int) (value: int) : int =
        value
        |> min (snd range)
        |> max (fst range)

    let currentOrLastDatafulGestureState
            (maybeLastGestureStateRef: IRefValue<Option<Rn.Components.GestureView.PanGestureState>>)
            (current: Rn.Components.GestureView.PanGestureState)
            : Rn.Components.GestureView.PanGestureState =
        if current.isComplete && current.isTouch && isNullOrUndefined current.pageX then
            match maybeLastGestureStateRef.current with
            | Some last ->
                if last.initialPageX = current.initialPageX && last.initialPageY = current.initialPageY then
                    LibClient.JsInterop.extendRecord
                        [
                            "pageX" ==> last.pageX
                            "pageY" ==> last.pageY
                        ]
                        current
                else
                    current
            | None -> current
        else
            maybeLastGestureStateRef.current <- Some current
            current

    // Assigning the shared value (directly or via withTiming) cancels any running animation on it.
    // The token invalidates a superseded settle's JS-thread completion so it can't stomp the rest
    // state after a newer drag/animation has taken over.
    let animate
            (animationTokenRef: IRefValue<int>)
            (setRestP: int -> unit)
            (value: Reanimated.SharedValue)
            (baseOffset: int)
            (maybeFromP: Option<int>)
            (toP: int)
            (maybeOnComplete: Option<unit -> unit>)
            : unit =
        animationTokenRef.current <- animationTokenRef.current + 1
        let token = animationTokenRef.current

        maybeFromP |> Option.sideEffect (fun fromP -> value.SetValue (fromP + baseOffset))
        value.AnimateTiming(
            toP + baseOffset,
            durationMs = 100.0,
            onComplete =
                (fun () ->
                    if animationTokenRef.current = token then
                        maybeOnComplete |> Option.sideEffect (fun fn -> fn ())
                        setRestP toP))

    let onPan
            (maybeOnChange: Option<Change -> unit>)
            (baseOffset: int)
            (lastRestP: int)
            (lastRestPosition: Position)
            (range: int * int)
            (aniValue: Reanimated.SharedValue)
            (maybeNegativeDragTarget: Option<DragTarget> * Position)
            (maybePositiveDragTarget: Option<DragTarget> * Position)
            (setRestP: int -> unit)
            (setLastRestPosition: Position -> unit)
            (animationTokenRef: IRefValue<int>)
            (rawDeltaP: int)
            (isGestureComplete: bool)
            : unit =

        let nextP = lastRestP + rawDeltaP |> limitToRange range
        let deltaP = nextP - lastRestP

        match isGestureComplete with
        | false -> aniValue.SetValue (nextP + baseOffset)
        | true  ->
            match nextP with
            | 0 ->
                setRestP 0
                setLastRestPosition Position.Base
                maybeOnChange |> Option.sideEffect (fun fn ->
                    fn (PositionChanged (Position.Base, ManualDrag))
                )

            | _ ->
                let (targetRestP, targetRestPosition) =
                    match (lastRestP, deltaP > 0, fst maybeNegativeDragTarget, fst maybePositiveDragTarget) with
                    | (0, false, Some negativeTarget, _) when -nextP > negativeTarget.ForwardThreshold -> (-negativeTarget.Offset, snd maybeNegativeDragTarget)
                    | (0, false, Some negativeTarget, _) when -nextP <= negativeTarget.ForwardThreshold -> (0, Position.Base)
                    | (0, true, _, Some positiveTarget) when nextP > positiveTarget.ForwardThreshold -> (positiveTarget.Offset, snd maybePositiveDragTarget)
                    | (0, true, _, Some positiveTarget) when nextP <= positiveTarget.ForwardThreshold -> (0, Position.Base)
                    | (lastRestP, true, Some negativeTarget, _) when (lastRestP = -negativeTarget.Offset) && (deltaP > negativeTarget.BackwardThreshold) -> (0, Position.Base)
                    | (lastRestP, true, Some negativeTarget, _) when (lastRestP = -negativeTarget.Offset) && (deltaP <= negativeTarget.BackwardThreshold) -> (-negativeTarget.Offset, snd maybeNegativeDragTarget)
                    | (lastRestP, false, _, Some positiveTarget) when (lastRestP = positiveTarget.Offset) && (-deltaP > positiveTarget.BackwardThreshold) -> (0, Position.Base)
                    | (lastRestP, false, _, Some positiveTarget) when (lastRestP = positiveTarget.Offset) && (-deltaP <= positiveTarget.BackwardThreshold) -> (positiveTarget.Offset, snd maybePositiveDragTarget)
                    | (_, _, None, None) -> (0, Position.Base)
                    | _ -> (lastRestP, lastRestPosition)

                maybeOnChange |> Option.sideEffect (fun fn -> fn (DragInducedAnimationStarted targetRestPosition))
                animate animationTokenRef setRestP aniValue baseOffset (Some nextP) targetRestP (Some (fun () ->
                    maybeOnChange |> Option.sideEffect (fun fn ->
                        setLastRestPosition targetRestPosition
                        fn (PositionChanged (targetRestPosition, AnimationFinished))
                    )
                ))

[<RequireQualifiedAccess>]
module private Styles =
    let gestureView =
        makeViewStyles {
            Overflow.Visible
        }

    // Used when the wrapper is explicitly sized (styles prop provided); makes the GestureView
    // fill the AnimatableView so gestures are captured over the full interactive surface.
    let gestureViewFill =
        makeViewStyles {
            flex 1
            Overflow.Visible
        }

    let wrapper =
        makeViewStyles {
            Overflow.Visible
        }

    // Used when the wrapper is explicitly sized (styles prop provided); makes the animated view
    // fill the wrapper's full height.
    let animatableViewFill =
        makeViewStyles {
            flex 1
        }

    // Base style for the animated contents view; the translateX/translateY transform is supplied
    // separately as a Reanimated animated style (see the component body).
    let contents =
        makeViewStyles {
            Overflow.Visible
        }

type private DraggableRuntime = {
    Left:            Option<DragTarget>
    Right:           Option<DragTarget>
    Up:              Option<DragTarget>
    Down:            Option<DragTarget>
    BaseOffsetX:     int
    BaseOffsetY:     int
    OnChange:        Option<Change -> unit>
    LastRestX:       IRefValue<int>
    LastRestY:       IRefValue<int>
    LastRestPosition: IRefValue<Position>
    RangeX:          int * int
    RangeY:          int * int
    AniValueX:       Reanimated.SharedValue
    AniValueY:       Reanimated.SharedValue
    AnimationToken:  IRefValue<int>
    MaybeLastGestureState: IRefValue<Option<Rn.Components.GestureView.PanGestureState>>
}

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Draggable(
            ?children:        ReactChildrenProp,
            ?baseOffset:      int * int,
            ?left:            DragTarget,
            ?right:           DragTarget,
            ?up:              DragTarget,
            ?down:            DragTarget,
            ?onChange:        Change -> unit,
            ?draggableRef:    LibClient.JsInterop.JsNullable<IDraggableRef> -> unit,
            ?testId:          string,
            ?styles:          array<ViewStyles>,
            ?xLegacyStyles:   List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:             string
        ) : ReactElement =
        key |> ignore

        let baseOffset = defaultArg baseOffset (0, 0)
        let baseOffsetX = fst baseOffset
        let baseOffsetY = snd baseOffset
        let rangeX = Helpers.range left right
        let rangeY = Helpers.range up down

        let lastRestXRef = Hooks.useRef 0
        let lastRestYRef = Hooks.useRef 0
        let lastRestPositionRef = Hooks.useRef Position.Base
        let maybeLastGestureStateRef = Hooks.useRef None
        let animationTokenRef = Hooks.useRef 0

        // Reanimated shared values drive the two axes; the animated transform style is hooked once at
        // the top level (hook rule) and applied to the contents view in the render below.
        let aniValueX = Reanimated.useSharedValue (double baseOffsetX)
        let aniValueY = Reanimated.useSharedValue (double baseOffsetY)
        let contentsAnimatedStyle = Reanimated.useAnimatedTranslateXY aniValueX aniValueY

        let setLastRestX (x: int) : unit =
            lastRestXRef.current <- x

        let setLastRestY (y: int) : unit =
            lastRestYRef.current <- y

        let setLastRestPosition (value: Position) : unit =
            lastRestPositionRef.current <- value

        let runtimeRef = Hooks.useRef Unchecked.defaultof<DraggableRuntime>
        runtimeRef.current <- {
            Left = left
            Right = right
            Up = up
            Down = down
            BaseOffsetX = baseOffsetX
            BaseOffsetY = baseOffsetY
            OnChange = onChange
            LastRestX = lastRestXRef
            LastRestY = lastRestYRef
            LastRestPosition = lastRestPositionRef
            RangeX = rangeX
            RangeY = rangeY
            AniValueX = aniValueX
            AniValueY = aniValueY
            AnimationToken = animationTokenRef
            MaybeLastGestureState = maybeLastGestureStateRef
        }

        Hooks.useEffect(
            (fun () ->
                if lastRestXRef.current = 0 && lastRestYRef.current = 0 then
                    aniValueX.SetValue (double baseOffsetX)
                    aniValueY.SetValue (double baseOffsetY)
            ),
            [| baseOffsetX; baseOffsetY |]
        )

        let onPanX (rawDeltaX: int) (isGestureComplete: bool) : unit =
            let rt = runtimeRef.current
            Helpers.onPan
                rt.OnChange
                rt.BaseOffsetX
                rt.LastRestX.current
                rt.LastRestPosition.current
                rt.RangeX
                rt.AniValueX
                (rt.Left, Position.Left)
                (rt.Right, Position.Right)
                setLastRestX
                setLastRestPosition
                rt.AnimationToken
                rawDeltaX
                isGestureComplete

        let onPanY (rawDeltaY: int) (isGestureComplete: bool) : unit =
            let rt = runtimeRef.current
            Helpers.onPan
                rt.OnChange
                rt.BaseOffsetY
                rt.LastRestY.current
                rt.LastRestPosition.current
                rt.RangeY
                rt.AniValueY
                (rt.Up, Position.Up)
                (rt.Down, Position.Down)
                setLastRestY
                setLastRestPosition
                rt.AnimationToken
                rawDeltaY
                isGestureComplete

        let onPanHorizontal (rawGestureState: Rn.Components.GestureView.PanGestureState) : unit =
            let gestureState = Helpers.currentOrLastDatafulGestureState maybeLastGestureStateRef rawGestureState
            onPanX gestureState.DeltaX gestureState.isComplete

        let onPanVertical (rawGestureState: Rn.Components.GestureView.PanGestureState) : unit =
            let gestureState = Helpers.currentOrLastDatafulGestureState maybeLastGestureStateRef rawGestureState
            onPanY gestureState.DeltaY gestureState.isComplete

        let performSetPosition (newPosition: Position) : bool =
            let rt = runtimeRef.current
            let maybeTarget =
                match (newPosition, rt.Left, rt.Right, rt.Up, rt.Down) with
                | (Position.Left, Some target,           _,           _,           _) -> (setLastRestX, rt.AniValueX, rt.BaseOffsetX, -target.Offset, fun () -> rt.AniValueY.SetValue rt.BaseOffsetY) |> Some
                | (Position.Right,          _, Some target,           _,           _) -> (setLastRestX, rt.AniValueX, rt.BaseOffsetX,  target.Offset, fun () -> rt.AniValueY.SetValue rt.BaseOffsetY) |> Some
                | (Position.Up,             _,           _, Some target,           _) -> (setLastRestY, rt.AniValueY, rt.BaseOffsetY, -target.Offset, fun () -> rt.AniValueX.SetValue rt.BaseOffsetX) |> Some
                | (Position.Down,           _,           _,           _, Some target) -> (setLastRestY, rt.AniValueY, rt.BaseOffsetY,  target.Offset, fun () -> rt.AniValueX.SetValue rt.BaseOffsetX) |> Some
                | (Position.Base,           _,           _,           _,           _) ->
                    match rt.LastRestPosition.current with
                    | Position.Right | Position.Left -> (setLastRestX, rt.AniValueX, rt.BaseOffsetX, 0, fun () -> rt.AniValueY.SetValue rt.BaseOffsetY) |> Some
                    | Position.Up    | Position.Down -> (setLastRestY, rt.AniValueY, rt.BaseOffsetY, 0, fun () -> rt.AniValueX.SetValue rt.BaseOffsetX) |> Some
                    | Position.Base                  -> (setLastRestX, rt.AniValueX, rt.BaseOffsetX, 0, fun () -> rt.AniValueY.SetValue rt.BaseOffsetY) |> Some
                | _ -> None

            maybeTarget |> Option.sideEffect (fun (setRestP, value, baseOffset, targetP, resetNonAnimatingAxis) ->
                resetNonAnimatingAxis ()
                Helpers.animate animationTokenRef setRestP value baseOffset None targetP (Some (fun () -> setLastRestPosition newPosition))
            )

            maybeTarget.IsSome

        let performSetPositionRef = Hooks.useRef performSetPosition
        performSetPositionRef.current <- performSetPosition

        let onPanHorizontalRef = Hooks.useRef onPanHorizontal
        onPanHorizontalRef.current <- onPanHorizontal

        let onPanVerticalRef = Hooks.useRef onPanVertical
        onPanVerticalRef.current <- onPanVertical

        let selfRef =
            Hooks.useMemo(
                (fun () ->
                    { new IDraggableRef with
                        member _.SetPosition newPosition =
                            performSetPositionRef.current newPosition

                        member _.OnPanHorizontal rawGestureState =
                            onPanHorizontalRef.current rawGestureState

                        member _.OnPanVertical rawGestureState =
                            onPanVerticalRef.current rawGestureState
                    }
                ),
                [||]
            )

        Hooks.useEffectDisposableFn(
            (fun () ->
                draggableRef |> Option.sideEffect (fun refCallback ->
                    refCallback (selfRef :> obj :?> LibClient.JsInterop.JsNullable<IDraggableRef>)
                )
            ),
            (fun () ->
                draggableRef |> Option.sideEffect (fun refCallback ->
                    refCallback (null :> obj :?> LibClient.JsInterop.JsNullable<IDraggableRef>)
                )
            ),
            [| draggableRef :> obj |]
        )

        let legacyGestureViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findApplicableStyles legacyStyles "gesture-view" with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.GestureView" styles |]
            | None -> [||]

        let legacyContentsStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<array<ViewStyles>> "Rn.Components.View" legacyStyles
            | None -> [||]

        // When the wrapper is explicitly sized via the styles prop, the animated view and
        // GestureView must fill it; otherwise they default to flexGrow:0 and collapse to
        // content height, making the interactive surface too small.
        let fillStyles = styles.IsSome

        let gestureView =
            Rn.GestureView(
                ?onPanHorizontal = (if left.IsSome || right.IsSome then Some onPanHorizontal else None),
                ?onPanVertical = (if up.IsSome || down.IsSome then Some onPanVertical else None),
                styles =
                    [|
                        if fillStyles then Styles.gestureViewFill else Styles.gestureView
                        yield! legacyGestureViewStyles
                    |],
                children = defaultArg children [||]
            )

        Rn.View(
            ?testId = testId,
            // box-none: the wrapper is a positioning container, never itself a pointer target,
            // but its interactive children (the GestureView) still are. Without this, a sized
            // wrapper (e.g. the AppShell sidebar drawer, absolute/full-height/width) overlays the
            // content beneath and swallows pointer + wheel events across its whole box even while
            // the drawer itself is translated off-screen.
            blockPointerEvents = true,
            styles = [| Styles.wrapper; yield! defaultArg styles [||] |],
            children =
                [|
                    Rn.ReanimatedView(
                        styles =
                            [|
                                yield! if fillStyles then [| Styles.animatableViewFill |] else [||]
                                yield! legacyContentsStyles
                                Styles.contents
                            |],
                        animatedStyle = contentsAnimatedStyle,
                        children = [| gestureView |]
                    )
                |]
        )
