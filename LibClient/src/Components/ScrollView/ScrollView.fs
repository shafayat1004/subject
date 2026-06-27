// Converted from render DSL (ScrollView.typext.fs + ScrollView.styles.fs + ScrollView.render)
// to a pure-F# [<Component>] function. The component is genuinely stateful: it tracks scroll
// position and content size via Hooks.useRef, stores measurements across unmount/remount cycles
// in a module-level mutable Map (matching the old `static let mutable measurementStorage`), and
// implements IScrollViewComponentRef for callers that pass a `ref` callback.
[<AutoOpen>]
module LibClient.Components.ScrollView

open System

open Fable.React

open LibClient
open LibClient.JsInterop

open ReactXP.Components
open ReactXP.Styles

// ---------------------------------------------------------------------------
// Public types — kept at LibClient.Components.ScrollView.* so existing callers
// that qualify them (e.g. ScrollView.Scroll.Horizontal, ScrollView.RestoreScroll.No)
// continue to compile without changes.
// ---------------------------------------------------------------------------

type IScrollViewComponentRef =
    abstract member SetScrollLeft: (* scrollLeft *) int * (* animate *) bool -> unit
    abstract member SetScrollTop:  (* scrollTop *)  int * (* animate *) bool -> unit

type Scroll = NoScroll | Horizontal | Vertical | Both

type RestoreScroll =
| No
| WhenContentApproximatelyMatchesOriginalHeight of Key: string
with
    member this.MaybeKey : Option<string> =
        match this with
        | WhenContentApproximatelyMatchesOriginalHeight key -> Some key
        | No -> None

// ---------------------------------------------------------------------------
// Module-level storage — intentionally survives component unmount/remount.
// ---------------------------------------------------------------------------

type private ScrollPosition = {
    Left: int
    Top:  int
}

type private Measurements = {
    ScrollPosition: ScrollPosition
    ContentSize:    Layout
}

let mutable private measurementStorage: Map<string, Measurements> = Map.empty

let private storeMeasurements (key: string) (scrollPosition: ScrollPosition) (contentSize: Layout) : unit =
    measurementStorage <- measurementStorage.AddOrUpdate (key, { ScrollPosition = scrollPosition; ContentSize = contentSize })

// ---------------------------------------------------------------------------
// Styles
// ---------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module private Styles =
    let innerDiv =
        makeViewStyles {
            Overflow.Visible
        }

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member ScrollView (
            scroll:                                 Scroll,
            restoreScroll:                          RestoreScroll,
            children:                               array<ReactElement>,
            ?onScroll:                              int * int -> unit,
            ?onLayout:                              ReactXP.Types.ViewOnLayoutEvent -> unit,
            ?showsHorizontalScrollIndicatorOnNative: bool,
            ?showsVerticalScrollIndicatorOnNative:   bool,
            ?styles:                                array<ViewStyles>,
            ?ref:                                   JsNullable<IScrollViewComponentRef> -> unit,
            ?key:                                   string
        ) : ReactElement =
        ignore key

        let showsHoriz = defaultArg showsHorizontalScrollIndicatorOnNative true
        let showsVert  = defaultArg showsVerticalScrollIndicatorOnNative   true

        // Per-instance mutable refs — do not trigger re-renders on change.
        let lastScrollPositionRef:              IRefHook<ScrollPosition>                               = Hooks.useRef { Left = 0; Top = 0 }
        let maybeLastContentSizeRef:            IRefHook<Option<Layout>>                               = Hooks.useRef None
        let restoreWhenMatchRef:                IRefHook<Option<Measurements>>                         = Hooks.useRef None
        let maybeScrollViewRef:                 IRefHook<Option<ReactXP.Components.ScrollView.IScrollViewRef>> = Hooks.useRef None

        // IScrollViewComponentRef implementation — delegates to the inner scroll view ref.
        // Created once via useMemo so its identity is stable across renders.
        let selfRef =
            Hooks.useMemo (
                (fun () ->
                    { new IScrollViewComponentRef with
                        member _.SetScrollLeft (scrollLeft: int, animate: bool) : unit =
                            maybeScrollViewRef.current
                            |> Option.sideEffect (fun sv -> sv.setScrollLeft (scrollLeft, animate))

                        member _.SetScrollTop (scrollTop: int, animate: bool) : unit =
                            maybeScrollViewRef.current
                            |> Option.sideEffect (fun sv -> sv.setScrollTop (scrollTop, animate))
                    }
                ),
                [||]
            )

        // Notify caller's ref callback whenever the inner scroll view ref is (re)bound.
        // We call it once on mount with the stable selfRef object.
        Hooks.useEffect (
            (fun () ->
                ref |> Option.sideEffect (fun refCallback ->
                    refCallback (selfRef :> obj :?> JsNullable<IScrollViewComponentRef>)
                )
            ),
            [| ref :> obj |]
        )

        // Stable inner-ref callback — captures maybeScrollViewRef.
        let stableInnerRefCallback =
            Hooks.useMemo (
                (fun () ->
                    fun (nullableInstance: JsNullable<ReactXP.Components.ScrollView.IScrollViewRef>) ->
                        maybeScrollViewRef.current <- nullableInstance.ToOption
                ),
                [||]
            )

        // onScroll handler: updates position and cancels pending restore.
        let handleScroll (top: int, left: int) : unit =
            lastScrollPositionRef.current <- { Top = top; Left = left }
            if restoreWhenMatchRef.current.IsSome then
                restoreWhenMatchRef.current <- None
            onScroll |> Option.sideEffect (fun cb -> cb (top, left))

        // onContentLayout handler: updates content size and tries to restore scroll.
        let handleContentLayout (layout: ReactXP.Types.ViewOnLayoutEvent) : unit =
            maybeLastContentSizeRef.current <- Some { Width = layout.width; Height = layout.height }
            restoreWhenMatchRef.current
            |> Option.sideEffect (fun originalMeasurements ->
                if abs (layout.height - originalMeasurements.ContentSize.Height) < (int ((float layout.height) * 0.1)) then
                    restoreWhenMatchRef.current <- None
                    maybeScrollViewRef.current |> Option.sideEffect (fun sv ->
                        sv.setScrollLeft (originalMeasurements.ScrollPosition.Left, (* animate *) false)
                        sv.setScrollTop  (originalMeasurements.ScrollPosition.Top,  (* animate *) false)
                    )
            )
            onLayout |> Option.sideEffect (fun cb -> cb layout)

        // Mount/unmount effect — mirrors ComponentDidMount + ComponentWillUnmount.
        Hooks.useEffectDisposable (
            (fun () ->
                (match restoreScroll with
                 | WhenContentApproximatelyMatchesOriginalHeight key ->
                     measurementStorage.TryFind key
                     |> Option.sideEffect (fun measurements ->
                         restoreWhenMatchRef.current <- Some measurements
                         async {
                             // If we couldn't restore scroll for 10 seconds, give up.
                             do! Async.Sleep (TimeSpan.FromSeconds 10.)
                             if restoreWhenMatchRef.current.IsSome then
                                 restoreWhenMatchRef.current <- None
                         } |> startSafely
                     )
                 | No -> Noop)

                // componentWillUnmount: persist measurements for the next mount.
                { new IDisposable with
                    member _.Dispose() =
                        match (restoreScroll.MaybeKey, maybeLastContentSizeRef.current) with
                        | (Some key, Some lastContentSize) ->
                            storeMeasurements key lastScrollPositionRef.current lastContentSize
                        | _ -> Noop
                }
            ),
            [||]
        )

        // The effective onScroll:
        //   - No restore in play: pass through the caller's onScroll.
        //   - Restore in play: always use handleScroll (which also calls caller's onScroll).
        let effectiveOnScroll =
            match restoreScroll with
            | No     -> onScroll
            | _      -> Some handleScroll

        RX.ScrollView (
            horizontal                   = (scroll = Both || scroll = Horizontal),
            vertical                     = (scroll = Both || scroll = Vertical),
            showsHorizontalScrollIndicator = showsHoriz,
            showsVerticalScrollIndicator   = showsVert,
            ?onScroll                    = effectiveOnScroll,
            ref                          = stableInnerRefCallback,
            children =
                [|
                    // Note: onContentSizeChange on the ScrollView doesn't work as expected
                    // (called once, not re-called when async content changes height). We use
                    // a wrapping View with onLayout instead, which fires reliably.
                    RX.View (
                        onLayout = handleContentLayout,
                        styles   = [|
                            Styles.innerDiv
                            yield! (defaultArg styles [||])
                        |],
                        children = children
                    )
                |]
        )
