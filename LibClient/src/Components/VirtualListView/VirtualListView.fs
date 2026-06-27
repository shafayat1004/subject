[<AutoOpen>]
module LibClient.Components.VirtualListView

open System

open Fable.React

open LibClient
open LibClient.JsInterop

open ReactXP.Styles
open ReactXP.Components

// Public types exposed at this module path so that callers using
// `VirtualListView.WhenItemKeysMatch` or `VirtualListView.VirtualListItem`
// continue to compile without changes.

type RestoreScroll =
| No
| WhenItemKeysMatch of Key: string
    member this.MaybeKey : Option<string> =
        match this with
        | WhenItemKeysMatch key -> Some key
        | No -> None

type private ScrollPosition = {
    Left: int
    Top:  int
}

type private Measurements = {
    ScrollPosition: ScrollPosition
    Keys:           list<string>
}

type VirtualListItem<'Item> = {
    Key:      string
    Item:     'Item
    Height:   Option<int>
    Template: string
} with
    member this.ToRX : ReactXP.Components.VirtualListView.VirtualListViewItemInfo = {
        key                          = this.Key
        height                       = this.Height |> Option.getOrElse 1
        payload                      = this.Item :> obj
        measureHeight                = Some this.Height.IsNone
        template                     = this.Template
        isNavigable                  = Some false
        disableTouchOpacityAnimation = true
    }

module VirtualListItem =
    let toRX (item: VirtualListItem<'Item>) = item.ToRX

// Intentionally module-level: this cache survives across component instances
// and across unmount/remount cycles (used to restore scroll on navigation return).
let mutable private scrollPositionStorage: Map<string, Measurements> = Map.empty

let private storeMeasurements (key: string) (scrollPosition: ScrollPosition) (keys: list<string>) : unit =
    scrollPositionStorage <- scrollPositionStorage.AddOrUpdate (key, { ScrollPosition = scrollPosition; Keys = keys })

module private Styles =
    // VirtualListView has no visual styles of its own; the styles prop is passed
    // straight through to the underlying RX.VirtualListView root.
    ()

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member VirtualListView<'Item> (
            items:             seq<VirtualListItem<'Item>>,
            render:            'Item -> ReactElement,
            restoreScroll:     RestoreScroll,
            ?scrollSideEffect: (int * int -> unit),
            ?styles:           array<ViewStyles>,
            ?xLegacyStyles:    List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:              string
        ) : ReactElement =
        key |> ignore

        // Per-instance mutable refs (do not trigger re-render on change).
        let lastScrollPositionRef:      IRefHook<ScrollPosition>                                               = Hooks.useRef { Left = 0; Top = 0 }
        let maybeLastKeysRef:           IRefHook<Option<list<string>>>                                         = Hooks.useRef None
        let restoreWhenMatchRef:        IRefHook<Option<Measurements>>                                         = Hooks.useRef None
        let maybeVirtualListViewRef:    IRefHook<Option<ReactXP.Components.VirtualListView.IVirtualListViewRef>> = Hooks.useRef None

        // Stable helpers: capture per-instance refs in a closure that does not change.
        let tryRestore (itemsSeq: seq<VirtualListItem<'Item>>) : unit =
            let nextKeys = itemsSeq |> Seq.map (fun item -> item.Key) |> Seq.toList
            maybeLastKeysRef.current <- Some nextKeys
            restoreWhenMatchRef.current
            |> Option.sideEffect (fun originalMeasurements ->
                if nextKeys = originalMeasurements.Keys then
                    restoreWhenMatchRef.current <- None
                    maybeVirtualListViewRef.current |> Option.sideEffect (fun virtualListViewRef ->
                        // restore after render
                        runOnNextTick (fun () ->
                            virtualListViewRef.scrollToTop ((* animate *) false, originalMeasurements.ScrollPosition.Top)
                        )
                    )
            )

        // Stable callbacks for renderItem and ref — VirtualListView asserts they
        // don't change identity on every re-render, so we pin them with useMemo.
        let stableRenderItem =
            Hooks.useMemo (
                (fun () ->
                    fun (details: ReactXP.Components.VirtualListView.VirtualListCellRenderDetails) ->
                        details.GetItem<'Item>() |> render
                ),
                [||]
            )

        let stableRefCallback =
            Hooks.useMemo (
                (fun () ->
                    fun (nullableInstance: JsNullable<ReactXP.Components.VirtualListView.IVirtualListViewRef>) ->
                        maybeVirtualListViewRef.current <- nullableInstance.ToOption
                ),
                [||]
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
                             do! Async.Sleep (TimeSpan.FromSeconds 10.)
                             if restoreWhenMatchRef.current.IsSome then
                                 restoreWhenMatchRef.current <- None
                         } |> startSafely
                     )
                 | No -> Noop)

                tryRestore items

                // componentWillUnmount equivalent: store scroll position for later restore.
                { new IDisposable with
                    member _.Dispose() =
                        match (restoreScroll.MaybeKey, maybeLastKeysRef.current) with
                        | (Some scrollKey, Some keys) ->
                            storeMeasurements scrollKey lastScrollPositionRef.current keys
                        | _ -> Noop
                }
            ),
            [||]
        )

        // componentWillReceiveProps equivalent: call tryRestore whenever the items list changes.
        Hooks.useEffect (
            (fun () -> tryRestore items),
            [| items :> obj |]
        )

        // Resolve styles: merge any ?xLegacyStyles top-level-block styles with explicit ?styles.
        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | []     -> [||]
                | found  -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.VirtualListView" found |]
            | None -> [||]

        let resolvedStyles : array<ViewStyles> option =
            let combined =
                Array.append legacyStyles (defaultArg styles [||])
            if combined.Length = 0 then None
            else Some combined

        element {
            RX.VirtualListView (
                renderItem          = stableRenderItem,
                scrollEventThrottle = 10,
                itemList            = (items |> Seq.map VirtualListItem.toRX |> Array.ofSeq),
                ref                 = stableRefCallback,
                ?onScroll           = (match restoreScroll with No -> None | _ -> Some onScroll),
                ?styles             = resolvedStyles
            )
        }
