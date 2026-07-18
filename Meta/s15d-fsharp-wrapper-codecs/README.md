# S15d spike -- F# Option/Result wrapper decomposition by Orleans 10

Throwaway spike. Full write-up:
`AppEggShellGallery/public-dev/docs/modernization/spikes/s15d-fsharp-wrapper-codecs.md`.

## Run (Phase B -- the fix)

```sh
dotnet build Meta/s15d-fsharp-wrapper-codecs/Host/s15d-fsharp-wrapper-codecs.fsproj -c Debug
dotnet Meta/s15d-fsharp-wrapper-codecs/Host/bin/Debug/net10.0/s15d-fsharp-wrapper-codecs.dll
# => SPIKE PASS: 3 / FAIL: 0   (exit 0)
```

`Host/Program.fs` `Registry.supported` registers the bare LEAF types (`PingPayload`, `PingError`).

## Reproduce Phase A (the blocker)

In `Host/Program.fs`, change `Registry.supported` to the WRAPPERS:

```fsharp
let supported : Type list = [ typeof<Option<PingPayload>>; typeof<Result<PingPayload, PingError>> ]
```

Rebuild + run:

```
Unhandled exception. Orleans.Serialization.CodecNotFoundException:
    Could not find a codec for type S15D.PingPayload.
  at Orleans.Serialization.Serializers.CodecProvider.ThrowCodecNotFound(Type fieldType)
```

Orleans 10 resolves `Option<PingPayload>` via the built-in `FSharpOptionCodec` and
`Result<PingPayload, PingError>` via `FSharpResultCodec`; each decomposes the wrapper and asks for the
inner leaf's codec, which the wrapper-only registration never provided.
