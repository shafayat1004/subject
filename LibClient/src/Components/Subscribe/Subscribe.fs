[<AutoOpen>]
module LibClient.Components.Subscribe

open Fable.React
open LibClient
open LibClient.Services.Subscription
open System

// ─── Hook-based subscription store ───────────────────────────────────────────
// (kept from the legacy typext.fs — callers may use this directly)

type SubscriptionDataStoreEntry = {
    CurrentValue: obj
    Unsubscribe:  unit -> unit
}

type SubscriptionsDataStore =
| SubscriptionsDataStore of IStateHook<Map<string, SubscriptionDataStoreEntry>>
with
    member this.Subscribe (key: string) (subscription: (LibClient.AsyncDataModule.AsyncData<'T> -> unit) -> SubscribeResult) : AsyncData<'T> =
        let (SubscriptionsDataStore state) = this
        let entry =
            match state.current.TryFind key with
            | None ->
                // we need this because some subscription implementations call the subscriber
                // with a value immediately, in which case we wouldn't have added it to the map
                // yet, causing a "receiving an update for something we haven't subscribed to" error.
                let mutable maybeUnsubscribe = None

                let entry = {
                    CurrentValue = (AsyncData<'T>.Uninitialized :> obj)
                    Unsubscribe  = fun () -> maybeUnsubscribe |> Option.sideEffect (fun off -> off ())
                }
                state.update (state.current.Add (key, entry))

                maybeUnsubscribe <- (subscription (fun value ->
                    (this.Update key) value
                )).Off |> Some
                entry

            | Some entry -> entry

        entry.CurrentValue :?> AsyncData<'T>

    member private this.Update (key: string) (value: AsyncData<'T>) : unit =
        let (SubscriptionsDataStore state) = this
        state.update (fun map ->
            match map.TryFind key with
            | None       -> failwith $"Trying to update entry for a key {key} which we never subscribed on"
            | Some entry -> map.AddOrUpdate (key, { entry with CurrentValue = value :> obj })
        )

let useSubscriptions () : SubscriptionsDataStore =
    let store = Hooks.useStateLazy (fun () -> Map.empty) |> SubscriptionsDataStore

    Hooks.useEffectDisposable (
        (fun () ->
            { new IDisposable with
                member _.Dispose() =
                    let (SubscriptionsDataStore state) = store
                    state.current |> Map.iter (fun _ value ->
                        value.Unsubscribe ()
                    )
            }
        ),
        [||] // run only once
    )

    store

// ─── Public types (preserved for callers) ────────────────────────────────────

type OnSubscriptionKeyChange =
| ShowCurrentDataAsFetching
| ShowCurrentDataAsAvailable

[<RequireQualifiedAccess>]
type With<'T> =
| Raw           of (AsyncData<'T> -> ReactElement)
| WhenAvailable of ('T -> ReactElement)

let Raw           = With.Raw
let WhenAvailable = With.WhenAvailable

// ─── Component ───────────────────────────────────────────────────────────────

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Subscribe<'T when 'T: equality>(
        subscription:             (LibClient.AsyncDataModule.AsyncData<'T> -> unit) -> SubscribeResult,
        ``with``:                 With<'T>,
        ?subscriptionKey:         string,
        ?onSubscriptionKeyChange: OnSubscriptionKeyChange,
        ?key:                     string
    ) : ReactElement =
        ignore key

        let onKeyChange = defaultArg onSubscriptionKeyChange ShowCurrentDataAsFetching

        // Async data state — initialised to WillStartFetchingSoonHack as the original did.
        let valueHook = Hooks.useState AsyncData<'T>.WillStartFetchingSoonHack

        // A ref that always holds the latest value so the effect closure can read it without
        // capturing a stale snapshot (needed for the OnSubscriptionKeyChange logic on re-subscribe).
        let latestValueRef = Hooks.useRef AsyncData<'T>.WillStartFetchingSoonHack

        // Keep the ref in sync every render (before the effect runs).
        latestValueRef.current <- valueHook.current

        // Effect depends on subscriptionKey so it re-runs (unsubscribes + re-subscribes) whenever
        // the key changes, mirroring the original ComponentWillReceiveProps behaviour.
        Hooks.useEffectDisposable (
            (fun () ->
                let subscribeResult =
                    subscription (fun updatedAD ->
                        let currentValue = latestValueRef.current
                        let resolvedValue =
                            match (currentValue, updatedAD) with
                            | (Available oldAvailableValue, WillStartFetchingSoonHack) ->
                                match onKeyChange with
                                | ShowCurrentDataAsAvailable -> Available oldAvailableValue
                                | ShowCurrentDataAsFetching  -> Fetching (Some oldAvailableValue)
                            | _ -> updatedAD

                        if currentValue <> resolvedValue then
                            latestValueRef.current <- resolvedValue
                            valueHook.update resolvedValue
                    )

                { new IDisposable with
                    member _.Dispose() =
                        subscribeResult.Off ()
                }
            ),
            [| subscriptionKey :> obj |]
        )

        match ``with`` with
        | With.WhenAvailable render ->
            LC.AsyncData(
                data          = valueHook.current,
                whenAvailable = render
            )
        | With.Raw render ->
            render valueHook.current
