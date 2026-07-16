[<AutoOpen>]
module LibClient.Components.VirtualListView

open System

open Fable.Core
open Fable.Core.JsInterop
open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.JsInterop

open Rn.Styles

module RnPrimitives = Rn.RnPrimitives

// Public types exposed at this module path so that callers using
// `VirtualListView.WhenItemKeysMatch` or `VirtualListView.VirtualListItem`
// continue to compile without changes.

type RestoreScroll =
    | No
    | WhenItemKeysMatch of Key: string

    member this.MaybeKey: Option<string> =
        match this with
        | WhenItemKeysMatch key -> Some key
        | No                    -> None

type private ScrollPosition = { Left: int; Top: int }

type private Measurements = {
    ScrollPosition: ScrollPosition
    Keys:           list<string>
}

type VirtualListItem<'Item> = {
    Key:      string
    Item:     'Item
    Height:   Option<int>
    Template: string
}

// Internal ref surface kept for callers that need programmatic scroll.
type IVirtualListViewRef =
    abstract member scrollToTop (* animate *): bool * (* top *) int -> unit

module private Styles =
    // VirtualListView has no visual styles of its own; the styles prop is passed
    // straight through to the underlying FlatList root.
    ()

// Fable represents tupled functions as single-argument functions that destructure
// an array. Raw JS callbacks (e.g. FlatList's `getItemLayout`) pass multiple
// separate arguments, so we need an adapter that collects them into an array.
[<Emit("function () { return $0(Array.prototype.slice.call(arguments)); }")>]
let private fixTupledReturning (_f: 'T -> 'R) : obj = jsNative

// Intentionally module-level: this cache survives across component instances
// and across unmount/remount cycles (used to restore scroll on navigation return).
let mutable private scrollPositionStorage: Map<string, Measurements> = Map.empty

let private storeMeasurements (key: string) (scrollPosition: ScrollPosition) (keys: list<string>) : unit =
    scrollPositionStorage <-
        scrollPositionStorage.AddOrUpdate(
            key,
            { ScrollPosition = scrollPosition
              Keys           = keys }
        )

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member VirtualListView<'Item>
        (
            items:             seq<VirtualListItem<'Item>>,
            render:            'Item -> ReactElement,
            restoreScroll:     RestoreScroll,
            ?scrollSideEffect: (int * int -> unit),
            ?styles:           array<ViewStyles>,
            ?xLegacyStyles:    List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:              string
        ) : ReactElement =
        key |> ignore

        let itemsArray = items |> Array.ofSeq

        // Per-instance mutable refs (do not trigger re-render on change).
        let lastScrollPositionRef: IRefHook<ScrollPosition> =
            Hooks.useRef { Left = 0; Top = 0 }

        let maybeLastKeysRef: IRefHook<Option<list<string>>> = Hooks.useRef None
        let restoreWhenMatchRef: IRefHook<Option<Measurements>> = Hooks.useRef None
        let maybeFlatListRef: IRefHook<Option<obj>> = Hooks.useRef None

        // Stable helpers: capture per-instance refs in a closure that does not change.
        let tryRestore (itemsSeq: seq<VirtualListItem<'Item>>) : unit =
            let nextKeys = itemsSeq |> Seq.map (fun item -> item.Key) |> Seq.toList
            maybeLastKeysRef.current <- Some nextKeys

            restoreWhenMatchRef.current
            |> Option.sideEffect (fun originalMeasurements ->
                if nextKeys = originalMeasurements.Keys then
                    restoreWhenMatchRef.current <- None

                    maybeFlatListRef.current
                    |> Option.sideEffect (fun flatListRef ->
                        // restore after render
                        runOnNextTick (fun () ->
                            flatListRef?scrollToOffset
                                {| offset = originalMeasurements.ScrollPosition.Top
                                   animated = false |})))

        // Stable callbacks for FlatList — renderItem/keyExtractor/ref must not change
        // identity on every re-render or FlatList will re-mount cells.
        let stableRenderItem =
            Hooks.useMemo (
                (fun () ->
                    fun (info: obj) ->
                        let item = info?item |> unbox<VirtualListItem<'Item>>

                        let cellProps = createEmpty

                        RnPrimitives.assignA11yAndAutomation
                            cellProps
                            (Some(A11ySlug.testId "vlv-item" item.Key))
                            None
                            None
                            (Some AccessibilityRole.ListItem)
                            None
                            None
                            None
                            None
                            None
                            None
                            None
                            None

                        RnPrimitives.createElement RnPrimitives.View cellProps [| render item.Item |]),
                [||]
            )


        let stableKeyExtractor =
            fixTupledReturning (fun (item: VirtualListItem<'Item>, _index: int) -> item.Key)

        let stableRefCallback =
            Hooks.useMemo (
                (fun () ->
                    fun (nullableInstance: JsNullable<obj>) -> maybeFlatListRef.current <- nullableInstance.ToOption),
                [||]
            )

        // getItemLayout: only supplied when every item has a known height. When any
        // height is missing we let FlatList measure cells itself.
        let stableGetItemLayout =
            Hooks.useMemo (
                (fun () ->
                    let allHeightsKnown = itemsArray |> Array.forall (fun item -> item.Height.IsSome)

                    if allHeightsKnown then
                        let heights = itemsArray |> Array.map (fun item -> item.Height.Value)

                        let offsets = heights |> Array.scan (+) 0 |> Array.take heights.Length

                        fixTupledReturning (fun (_data: obj, index: int) ->
                            box
                                {| length = heights.[index]
                                   offset = offsets.[index]
                                   index  = index |})
                        |> Some
                    else
                        None),
                [| itemsArray :> obj |]
            )

        // onScroll: update per-instance scroll position and dispatch side-effect.
        let onScroll (top: int, left: int) : unit =
            lastScrollPositionRef.current <- { Top = top; Left = left }

            if restoreWhenMatchRef.current.IsSome then
                restoreWhenMatchRef.current <- None

            scrollSideEffect |> Option.sideEffect (fun callback -> callback (top, left))

        // componentDidMount equivalent: initialise the restore-when-keys-match
        // window, kick off the 10-second timeout, and call tryRestore.
        Hooks.useEffectDisposable (
            (fun () ->
                (match restoreScroll with
                 | WhenItemKeysMatch key ->
                     scrollPositionStorage.TryFind key
                     |> Option.sideEffect (fun measurements ->
                         restoreWhenMatchRef.current <- Some measurements

                         async {
                             // if we couldn't restore focus for 10 seconds, give up
                             do! Async.Sleep(TimeSpan.FromSeconds 10.)

                             if restoreWhenMatchRef.current.IsSome then
                                 restoreWhenMatchRef.current <- None
                         }
                         |> startSafely)
                 | No -> Noop)

                tryRestore items

                // componentWillUnmount equivalent: store scroll position for later restore.
                { new IDisposable with
                    member _.Dispose() =
                        match (restoreScroll.MaybeKey, maybeLastKeysRef.current) with
                        | (Some scrollKey, Some keys) -> storeMeasurements scrollKey lastScrollPositionRef.current keys
                        | _                           -> Noop }),
            [||]
        )

        // componentWillReceiveProps equivalent: call tryRestore whenever the items list changes.
        Hooks.useEffect ((fun () -> tryRestore items), [| items :> obj |])

        // Resolve styles: merge any ?xLegacyStyles top-level-block styles with explicit ?styles.
        let legacyStyles: array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | found ->
                    [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles>
                           "LibClient.Components.VirtualListView"
                           found |]
            | None -> [||]

        let resolvedStyles: array<ViewStyles> option =
            let combined = Array.append legacyStyles (defaultArg styles [||])
            if combined.Length = 0 then None else Some combined

        let __props = createEmpty
        __props?data <- itemsArray
        __props?renderItem <- stableRenderItem
        __props?keyExtractor <- stableKeyExtractor
        __props?ref <- stableRefCallback
        __props?scrollEventThrottle <- 10

        if restoreScroll <> No || scrollSideEffect.IsSome then
            RnPrimitives.wrapOnScroll (Some onScroll)
            |> Option.iter (fun v -> __props?onScroll <- v)

        stableGetItemLayout |> Option.iter (fun f -> __props?getItemLayout <- f)

        resolvedStyles
        |> Option.iter (fun ss -> __props?style <- ss |> Array.map (fun s -> (!!s) :> obj) |> box)

        RnPrimitives.createElement RnPrimitives.FlatList __props [||]
