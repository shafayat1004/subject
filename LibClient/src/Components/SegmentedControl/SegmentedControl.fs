[<AutoOpen>]
module LibClient.Components.SegmentedControl

open System
open Fable.Core.JsInterop
open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Components

open Rn.Components
open Rn.Styles

module LC =
    module SegmentedControl =
        type Theme = {
            TrackBackground:      Color
            ThumbBackground:      Color
            SelectedLabelColor:   Color
            UnselectedLabelColor: Color
            TrackWidth:           int
            TrackPadding:         int
        }

        type Segment<'T when 'T : equality> = {
            Label:         string
            Value:         'T
            TestIdSuffix:  string option
        }

open LC.SegmentedControl

[<RequireQualifiedAccess>]
module private GestureHelpers =
    let activationThreshold = 8

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

    let panDelta (gs: Rn.Components.GestureView.PanGestureState) =
        int gs.pageX - int gs.initialPageX

[<RequireQualifiedAccess>]
module private Styles =
    let track (theme: Theme) =
        makeViewStyles {
            width theme.TrackWidth
            padding theme.TrackPadding
            borderRadius 999
            backgroundColor theme.TrackBackground
            AlignSelf.FlexStart
            Position.Relative
            Overflow.Hidden
        }

    let segmentsRow (innerWidth: int) =
        makeViewStyles {
            FlexDirection.Row
            width innerWidth
        }

    let segmentCell (cellWidth: int) =
        makeViewStyles {
            width cellWidth
            minHeight 44
            JustifyContent.Center
            AlignItems.Center
        }

    let segmentPressable =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

    let segmentLabel (labelColor: Color) =
        makeTextStyles {
            fontSize 12
            FontWeight.W600
            color labelColor
            TextAlign.Center
        }

    let thumbStatic (theme: Theme) (thumbWidth: int) (offsetX: int) =
        makeViewStyles {
            Position.Absolute
            top theme.TrackPadding
            bottom theme.TrackPadding
            left (theme.TrackPadding + offsetX)
            width (max 0 thumbWidth)
            borderRadius 999
            backgroundColor theme.ThumbBackground
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member SegmentedControl<'T when 'T : equality>(
            segments:                  array<Segment<'T>>,
            selected:                  'T,
            onSelect:                  'T -> unit,
            ?accessibilityGroupLabel:  string,
            ?testId:                   string,
            ?theme:                     Theme -> Theme,
            ?draggable:                bool,
            ?key:                      string)
        : ReactElement =
        key |> ignore

        let draggable = defaultArg draggable true
        let theTheme = Themes.GetMaybeUpdatedWith theme
        let innerWidth = max 0 (theTheme.TrackWidth - theTheme.TrackPadding * 2)
        let segmentCount = max 1 segments.Length
        let cellWidth = innerWidth / segmentCount

        let selectedIndex =
            segments
            |> Array.tryFindIndex (fun segment -> segment.Value = selected)
            |> Option.defaultValue 0

        // Reanimated shared value drives the thumb's translateX (UI thread). The animated style hook
        // is called unconditionally at the top level (hook rule); it is unused in the reduced-motion
        // branch, which renders a static thumb instead.
        let translateX = Reanimated.useSharedValue 0.0
        let animatedThumbStyle = Reanimated.useAnimatedTranslateX translateX
        let animationTokenRef = Hooks.useRef 0
        let isAnimatingRef = Hooks.useRef false
        let restOffsetRef = Hooks.useRef (selectedIndex * cellWidth)
        let isDraggingHook = Hooks.useState false
        let panStartBaseRef = Hooks.useRef 0
        let gestureActiveRef = Hooks.useRef false
        let lastDragOffsetRef = Hooks.useRef 0
        let lastGestureStateRef = Hooks.useRef None
        let thumbInitializedRef = Hooks.useRef false

        let targetOffset index = index * cellWidth

        // Assigning translateX (directly or via withTiming) cancels any running animation; the token
        // invalidates a superseded settle's JS-thread completion so it can't stomp restOffset.
        let animateTo (target: int) =
            animationTokenRef.current <- animationTokenRef.current + 1
            let token = animationTokenRef.current
            isAnimatingRef.current <- true
            translateX.AnimateTiming(
                target,
                durationMs = 220.0,
                onComplete =
                    (fun () ->
                        if animationTokenRef.current = token then
                            isAnimatingRef.current <- false
                            restOffsetRef.current <- target))

        let syncThumb () =
            let target = targetOffset selectedIndex
            translateX.SetValue target
            restOffsetRef.current <- target

        Hooks.useEffect(
            (fun () ->
                if not thumbInitializedRef.current && cellWidth > 0 then
                    syncThumb ()
                    thumbInitializedRef.current <- true
                ()),
            [| box cellWidth |]
        )

        Hooks.useEffect(
            (fun () ->
                if not isDraggingHook.current && thumbInitializedRef.current && not isAnimatingRef.current then
                    let target = targetOffset selectedIndex
                    if restOffsetRef.current <> target then
                        animateTo target
                ()),
            [| box selected; box selectedIndex |]
        )

        let selectIndex (index: int) =
            if index >= 0 && index < segments.Length then
                let value = segments.[index].Value
                if value <> selected then
                    animateTo (targetOffset index)
                    onSelect value

        let settleFromOffset (offset: int) =
            let maxOffset = max 0 ((segmentCount - 1) * cellWidth)
            let clamped = max 0 (min maxOffset offset)
            let index =
                if cellWidth <= 0 then
                    0
                else
                    (clamped + cellWidth / 2) / cellWidth |> min (segmentCount - 1)
            animateTo (targetOffset index)
            if segments.[index].Value <> selected then
                onSelect segments.[index].Value

        let onPanHorizontal (rawGs: Rn.Components.GestureView.PanGestureState) =
            if not draggable || cellWidth <= 0 then
                ()
            else
                let gs = GestureHelpers.currentOrLastDatafulGestureState lastGestureStateRef rawGs
                let delta = GestureHelpers.panDelta gs
                let maxOffset = max 0 ((segmentCount - 1) * cellWidth)

                if gs.isComplete then
                    let wasActive = gestureActiveRef.current
                    gestureActiveRef.current <- false
                    isDraggingHook.update false

                    if not wasActive && abs delta < GestureHelpers.activationThreshold then
                        animateTo restOffsetRef.current
                    else
                        let offset =
                            if wasActive then
                                lastDragOffsetRef.current
                            else
                                max 0 (min maxOffset (panStartBaseRef.current + delta))

                        settleFromOffset offset

                    lastGestureStateRef.current <- None
                elif abs delta < GestureHelpers.activationThreshold then
                    ()
                else
                    if not gestureActiveRef.current then
                        gestureActiveRef.current <- true
                        panStartBaseRef.current <- restOffsetRef.current
                        // Invalidate any in-flight settle so its completion can't stomp restOffset.
                        animationTokenRef.current <- animationTokenRef.current + 1
                        isAnimatingRef.current <- false

                    isDraggingHook.update true
                    let offset = max 0 (min maxOffset (panStartBaseRef.current + delta))
                    lastDragOffsetRef.current <- offset
                    translateX.SetValue offset

        let onTap (e: Rn.Components.GestureView.TapGestureState) =
            if not draggable || cellWidth <= 0 then
                ()
            else
                let index =
                    int (e.clientX / float cellWidth)
                    |> max 0
                    |> min (segmentCount - 1)
                selectIndex index

        let segmentLabelColor (isActive: bool) =
            if isActive then theTheme.SelectedLabelColor
            else theTheme.UnselectedLabelColor

        let segmentTestIdFor (segment: Segment<'T>) =
            segment.TestIdSuffix
            |> Option.map (fun suffix ->
                match testId with
                | Some root -> sprintf "%s-%s" root suffix
                | None -> A11ySlug.testId "segmented-control" suffix)
            |> Option.orElse testId

        let renderSegment (index: int) (segment: Segment<'T>) =
            let isActive = index = selectedIndex
            let labelColor = segmentLabelColor isActive
            let segmentTestId = segmentTestIdFor segment

            Rn.View(
                styles = [| Styles.segmentCell cellWidth |],
                accessibilityRole = AccessibilityRole.Radio,
                accessibilityState = AccessibilityStateRecord.selected isActive,
                accessibilityLabel = segment.Label,
                importantForAccessibility = LibClient.Accessibility.ImportantForAccessibility.Yes,
                ?testId = segmentTestId,
                children = [|
                    LC.UiText(
                        value = segment.Label,
                        styles = [| Styles.segmentLabel labelColor |]
                    )
                |]
            )

        // Reduce-motion / non-draggable fallback: the thumb slide is replaced by a static thumb, but
        // each segment must stay tappable (rule 12 -- reduce-motion only skips decorative motion, not
        // interaction). GestureView uses the JS responder system which is fine here because there is
        // no competing native ScrollView inside the track.
        let renderPressableSegment (index: int) (segment: Segment<'T>) =
            let isActive = index = selectedIndex
            let labelColor = segmentLabelColor isActive
            let segmentTestId = segmentTestIdFor segment

            LC.Pressable(
                onPress = (fun _ -> selectIndex index),
                label = segment.Label,
                role = AccessibilityRole.Radio,
                state = AccessibilityStateRecord.selected isActive,
                ?testId = segmentTestId,
                styles = [| Styles.segmentCell cellWidth |],
                children = [|
                    LC.UiText(
                        value = segment.Label,
                        styles = [| Styles.segmentLabel labelColor |]
                    )
                |]
            )

        let control =
            LC.With.ReducedMotion (fun reduceMotion ->
                let thumbOffset = targetOffset selectedIndex

                Rn.View(
                    styles = [| Styles.track theTheme |],
                    children = [|
                        if cellWidth > 0 then
                            if reduceMotion || not draggable then
                                Rn.View(
                                    importantForAccessibility = LibClient.Accessibility.ImportantForAccessibility.No,
                                    styles = [| Styles.thumbStatic theTheme cellWidth thumbOffset |],
                                    children = [||]
                                )
                            else
                                Rn.ReanimatedView(
                                    styles = [| Styles.thumbStatic theTheme cellWidth 0 |],
                                    animatedStyle = animatedThumbStyle,
                                    children = [||]
                                )

                        if draggable && not reduceMotion then
                            Rn.GestureView(
                                children =
                                    [|
                                        yield!
                                            segments
                                            |> Array.mapi (fun index segment -> renderSegment index segment)
                                    |],
                                preferredPan = Rn.Components.GestureView.PreferredPanGesture.Horizontal,
                                panPixelThreshold = float GestureHelpers.activationThreshold,
                                onPanHorizontal = onPanHorizontal,
                                onTap = onTap,
                                styles = [| Styles.segmentsRow innerWidth |]
                            )
                        else
                            Rn.View(
                                styles = [| Styles.segmentsRow innerWidth |],
                                children =
                                    [|
                                        yield!
                                            segments
                                            |> Array.mapi (fun index segment -> renderPressableSegment index segment)
                                    |]
                            )
                    |]
                )
            )

        match accessibilityGroupLabel with
        | Some groupLabel ->
            LC.RadioGroup(
                label = groupLabel,
                ?testId = testId,
                children = [| control |]
            )
        | None ->
            Rn.View(?testId = testId, children = [| control |])
