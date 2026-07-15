// Converted from render DSL (ScrollView.typext.fs + ScrollView.styles.fs + ScrollView.render)
// to a pure-F# [<Component>] function. The component is genuinely stateful: it tracks scroll
// position and content size via Hooks.useRef, stores measurements across unmount/remount cycles
// in a module-level mutable Map (matching the old `static let mutable measurementStorage`), and
// implements IScrollViewComponentRef for callers that pass a `scrollViewRef` callback.
[<AutoOpen>]
module LibClient.Components.ScrollView

open System

open Fable.React

open LibClient
open LibClient.JsInterop

open Rn.Components
open Rn.Styles

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
        | No                                                -> None

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

    // A horizontal scroller's cross axis is its HEIGHT: it must size to its content. RNW's
    // ScrollView base style is flexShrink 1, so in a column-flex parent (the common case) the
    // scroller gets shrunk below its content height, collapsing it — the row clips on first
    // paint and a stray scrollbar appears. Pinning flexShrink 0 makes the height track content.
    // Vertical / Both scrollers keep the default (fill available space and scroll on the main axis).
    let horizontalOuter =
        makeScrollViewStyles {
            flexShrink 0
        }

    // Reserve room for a horizontal scrollbar. The scroller is overflow-y:hidden, so a
    // space-reserving (classic / "always show") scrollbar eats a strip off the bottom of the
    // content box and clips the row. Padding the content bottom by a scrollbar's height puts the
    // bar in that strip instead of over the content. On overlay-scrollbar systems (the macOS
    // default) it is just a few px of empty space. Appended last so it wins over caller padding.
    let horizontalScrollbarReserve =
        makeViewStyles {
            paddingBottom 16
        }

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member ScrollView (
            scroll:         Scroll,
            restoreScroll:  RestoreScroll,
            children:       array<ReactElement>,
            ?onScroll:      int * int -> unit,
            ?onLayout:      Rn.Types.ViewOnLayoutEvent -> unit,
            ?showsHorizontalScrollIndicatorOnNative: bool,
            ?showsVerticalScrollIndicatorOnNative: bool,
            ?styles:        array<ViewStyles>,
            ?scrollViewRef: JsNullable<IScrollViewComponentRef> -> unit,
            ?key:           string
        ) : ReactElement =
        ignore key

        let showsHoriz = defaultArg showsHorizontalScrollIndicatorOnNative true
        let showsVert  = defaultArg showsVerticalScrollIndicatorOnNative   true

        // Per-instance mutable refs — do not trigger re-renders on change.
        let lastScrollPositionRef:              IRefHook<ScrollPosition>       = Hooks.useRef { Left = 0; Top = 0 }
        let maybeLastContentSizeRef:            IRefHook<Option<Layout>>       = Hooks.useRef None
        let restoreWhenMatchRef:                IRefHook<Option<Measurements>> = Hooks.useRef None
        let maybeScrollViewRef:                 IRefHook<Option<Rn.Components.ScrollView.IScrollViewRef>> = Hooks.useRef None

        // IScrollViewComponentRef implementation — delegates to the inner scroll view ref.
        // Created once via useMemo so its identity is stable across renders.
        let selfRef =
            Hooks.useMemo (
                (fun () ->
                    { new IScrollViewComponentRef with
                        member _.SetScrollLeft (scrollLeft: int, animate: bool) : unit =
                            maybeScrollViewRef.current
                            |> Option.sideEffect (fun sv ->
                                sv.scrollTo (box {| x = scrollLeft; y = lastScrollPositionRef.current.Top; animated = animate |}))

                        member _.SetScrollTop (scrollTop: int, animate: bool) : unit =
                            maybeScrollViewRef.current
                            |> Option.sideEffect (fun sv ->
                                sv.scrollTo (box {| x = lastScrollPositionRef.current.Left; y = scrollTop; animated = animate |}))
                    }
                ),
                [||]
            )

        // Notify caller's callback whenever the inner scroll view ref is (re)bound.
        // We call it once on mount with the stable selfRef object.
        Hooks.useEffect (
            (fun () ->
                scrollViewRef |> Option.sideEffect (fun refCallback ->
                    refCallback (selfRef :> obj :?> JsNullable<IScrollViewComponentRef>)
                )
            ),
            [| scrollViewRef :> obj |]
        )

        // Stable inner-ref callback — captures maybeScrollViewRef.
        let stableInnerRefCallback =
            Hooks.useMemo (
                (fun () ->
                    fun (nullableInstance: JsNullable<Rn.Components.ScrollView.IScrollViewRef>) ->
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

        // Restore scroll from a saved Measurements record, clearing the pending-restore flag.
        // Used both from the onLayout path and the mount/key-change effect (closes the race where
        // onLayout lands before the effect sets the pending-restore flag on the reused native view).
        // Also adopts the restored position into lastScrollPositionRef immediately: a follow-up
        // onLayout can fire before the scrollTo's onScroll lands, and without this the re-arm path
        // in handleContentLayout (which only acts while Top = 0) would re-scroll to the same place.
        let restoreNow (measurements: Measurements) : unit =
            restoreWhenMatchRef.current <- None
            lastScrollPositionRef.current <- measurements.ScrollPosition
            maybeScrollViewRef.current
            |> Option.sideEffect (fun sv ->
                sv.scrollTo (box {| x = measurements.ScrollPosition.Left
                                    y        = measurements.ScrollPosition.Top
                                    animated = false |}))

        // Returns true if `currentHeight` approximately matches the saved content height (~10%).
        let heightApproximatelyMatches (currentHeight: int) (saved: Measurements) : bool =
            abs (currentHeight - saved.ContentSize.Height) < int (float currentHeight * 0.1)

        // onContentLayout handler: updates content size and tries to restore scroll.
        let handleContentLayout (layout: Rn.Types.ViewOnLayoutEvent) : unit =
            maybeLastContentSizeRef.current <- Some { Width = layout.width; Height = layout.height }
            // Restore from a pending measurement, or re-arm from storage if the pending flag was
            // cleared by a native clamp onScroll. Back-nav race: when the outgoing page's offset
            // exceeds the new (shorter) page's height, RN clamps the reused ScrollView to 0 and
            // emits an onScroll; handleScroll clears restoreWhenMatchRef, so by the time this
            // layout event fires the restore would be skipped and back-nav lands at the top. Re-arm
            // only while we're still at the clamped top (Top = 0) so a genuine user scroll (non-zero
            // offset) still cancels the pending restore, matching the original onScroll-cancel intent.
            let tryRestore (measurements: Measurements) : unit =
                if heightApproximatelyMatches layout.height measurements then
                    restoreNow measurements
            match restoreWhenMatchRef.current with
            | Some measurements -> tryRestore measurements
            | None ->
                match restoreScroll.MaybeKey with
                | Some key when lastScrollPositionRef.current.Top = 0 ->
                    match measurementStorage.TryFind key with
                    | Some measurements ->
                        restoreWhenMatchRef.current <- Some measurements
                        tryRestore measurements
                    | None -> ()
                | _ -> ()
            onLayout |> Option.sideEffect (fun cb -> cb layout)

        // Runs on mount AND whenever the restore key (the route url) changes. A parent that
        // navigates between pages of the same shape (e.g. LR.Route across docs) REUSES this
        // ScrollView instance rather than remounting it, so keying on the url is what lets a new
        // page start fresh: with deps [||] the effect never re-ran, the native scroll view kept
        // the previous page's offset, and a new doc opened already scrolled down.
        Hooks.useEffectDisposable (
            (fun () ->
                (match restoreScroll with
                 | WhenContentApproximatelyMatchesOriginalHeight key ->
                     match measurementStorage.TryFind key with
                     | Some measurements ->
                         restoreWhenMatchRef.current <- Some measurements
                         // Close a race on the reused native ScrollView: onLayout is a native
                         // bridge event and can land BEFORE this passive useEffect sets
                         // restoreWhenMatchRef (so handleContentLayout saw None and skipped the
                         // restore), and onLayout does not re-fire when the revisited content
                         // settles at the same height. Re-check the last measured size here: if
                         // it already approximately matches the saved height, restore immediately
                         // instead of waiting for an onLayout that will never come.
                         maybeLastContentSizeRef.current
                         |> Option.sideEffect (fun currentSize ->
                             if heightApproximatelyMatches currentSize.Height measurements then
                                 restoreNow measurements
                         )
                         async {
                             // If we couldn't restore scroll for 10 seconds, give up.
                             do! Async.Sleep (TimeSpan.FromSeconds 10.)
                             if restoreWhenMatchRef.current.IsSome then
                                 restoreWhenMatchRef.current <- None
                         } |> startSafely
                     | None ->
                         // Fresh page with no saved position: start at the top. Without this a
                         // reused scroll view retains the previous page's offset.
                         restoreWhenMatchRef.current <- None
                         lastScrollPositionRef.current <- { Left = 0; Top = 0 }
                         maybeScrollViewRef.current
                         |> Option.sideEffect (fun sv -> sv.scrollTo (box {| x = 0; y = 0; animated = false |}))
                 | No -> Noop)

                // On unmount OR before the next key's effect runs: persist this key's measurements
                // so navigating back can restore the position.
                { new IDisposable with
                    member _.Dispose() =
                        match (restoreScroll.MaybeKey, maybeLastContentSizeRef.current) with
                        | (Some key, Some lastContentSize) ->
                            storeMeasurements key lastScrollPositionRef.current lastContentSize
                        | _ -> Noop
                }
            ),
            [| box restoreScroll.MaybeKey |]
        )

        // The effective onScroll:
        //   - No restore in play: pass through the caller's onScroll.
        //   - Restore in play: always use handleScroll (which also calls caller's onScroll).
        let effectiveOnScroll =
            match restoreScroll with
            | No -> onScroll
            | _  -> Some handleScroll

        let outerStyles =
            match scroll with
            | Horizontal -> Some [| Styles.horizontalOuter |]
            | _          -> None

        Rn.ScrollView (
            horizontal = (scroll = Both || scroll = Horizontal),
            vertical   = (scroll = Both || scroll = Vertical),
            ?styles    = outerStyles,
            showsHorizontalScrollIndicator = showsHoriz,
            showsVerticalScrollIndicator = showsVert,
            ?onScroll  = effectiveOnScroll,
            ref        = stableInnerRefCallback,
            children =
                [|
                    // Note: onContentSizeChange on the ScrollView doesn't work as expected
                    // (called once, not re-called when async content changes height). We use
                    // a wrapping View with onLayout instead, which fires reliably.
                    Rn.View (
                        onLayout = handleContentLayout,
                        styles   = [|
                            Styles.innerDiv
                            yield! (defaultArg styles [||])
                            if scroll = Horizontal || scroll = Both then
                                Styles.horizontalScrollbarReserve
                        |],
                        children = children
                    )
                |]
        )
