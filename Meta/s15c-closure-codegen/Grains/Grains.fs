namespace S15C_Grains

open System.Threading.Tasks
open Orleans
open S15C

// === F# grain impl (scanned by the C# Codegen project via GenerateCodeForDeclaringAssembly). ===
type PingGrain() =
    inherit Grain()
    interface IPingGrain with
        member _.PingObserver(observer: IPingObserver<PingPayload>, count: int) : Task<unit> =
            for i in 1 .. count do
                observer.Notify { Seq = i; Message = sprintf "ping-%d" i }
            Task.FromResult ()

// === THE FIX (Phase B): lift the grain-observer implementation out of the `backgroundTask { }` CE
// into a NAMED top-level F# class. F# emits a valid C# type name (`S15C_Grains.PingObserver`) so the
// Orleans 10 source generator's `config.InterfaceImplementations.Add(typeof(...))` line compiles.
//
// Phase A (REPRO) used an F# object expression bound to `let observer = { new IPingObserver<_> with
// ... }` inside the CE. F# named that closure `Subscriber.observer@33`, and the generator emitted
// the invalid C# `config.InterfaceImplementations.Add(typeof(global::S15C_Grains.Subscriber.observer
// @ 33));` (CS1026/CS1646/CS0426). A named class captures the same state (TaskCompletionSource +
// last-seen seq) via its constructor + a member instead of a closure capture, and is behaviorally
// identical.
type PingObserver(count: int) =
    let tcs = TaskCompletionSource<int>()
    let mutable last = 0

    /// Completes with the last-seen Seq once the grain has pinged `count` times.
    member _.Completion : Task<int> = tcs.Task

    interface IPingObserver<PingPayload> with
        member _.Notify (payload: PingPayload) =
            last <- payload.Seq
            if payload.Seq >= count then tcs.TrySetResult last |> ignore

// Client-side subscriber helper, mirroring GrainConnector.RunAndWait: a `backgroundTask { }` CE
// (whose own state-machine closure is NOT flagged -- it implements IAsyncStateMachine, not a
// grain-observer interface) that constructs the named observer, hands it to CreateObjectReference,
// and awaits the callback.
module Subscriber =

    let subscribeViaNamedClass (grainFactory: IGrainFactory) (grain: IPingGrain) (count: int) : Task<int> =
        backgroundTask {
            let observer = PingObserver(count)
            let observerRef = grainFactory.CreateObjectReference<IPingObserver<PingPayload>> observer
            do! grain.PingObserver(observerRef, count)
            return! observer.Completion
        }
