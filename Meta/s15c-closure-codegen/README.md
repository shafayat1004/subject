# S15c spike -- F# closure misclassification by the Orleans 10 source generator

Throwaway spike. Full write-up:
`AppEggShellGallery/public-dev/docs/modernization/spikes/s15c-closure-codegen.md`.

## Run (Phase B -- the fix)

```sh
dotnet build Meta/s15c-closure-codegen/Host/s15c-closure-codegen.fsproj -c Debug
dotnet Meta/s15c-closure-codegen/Host/bin/Debug/net10.0/s15c-closure-codegen.dll
# => SPIKE PASS: 1 / FAIL: 0   (exit 0)
```

`Grains/Grains.fs` uses the FIX: a named `PingObserver` class (valid C# `typeof`).

## Reproduce Phase A (the blocker)

Replace the named `PingObserver` + `subscribeViaNamedClass` in `Grains/Grains.fs` with the object
expression form, and point `Host/Program.fs` at it:

```fsharp
// Grains.fs
module Subscriber =
    let subscribeViaObjectExpression (grainFactory: IGrainFactory) (grain: IPingGrain) (count: int) : Task<int> =
        backgroundTask {
            let tcs = TaskCompletionSource<int>()
            let observer =
                { new IPingObserver<PingPayload> with
                    member _.Notify (payload: PingPayload) =
                        if payload.Seq >= count then tcs.TrySetResult payload.Seq |> ignore }
            let observerRef = grainFactory.CreateObjectReference<IPingObserver<PingPayload>> observer
            do! grain.PingObserver(observerRef, count)
            return! tcs.Task
        }
```

Then `dotnet build Meta/s15c-closure-codegen/Codegen/s15c-closure-codegenCodegen.csproj` fails with
`CS1646` on `config.InterfaceImplementations.Add(typeof(global::S15C_Grains.Subscriber.observer @ NN))`
in `Codegen/obj/Generated/.../s15c-closure-codegenCodegen.orleans.g.cs`.
