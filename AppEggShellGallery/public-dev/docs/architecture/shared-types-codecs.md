# Shared Types, Codecs & the Wire

## Codecs

Serialization is built on **Fleece** codecs, wrapped in `LibLangFsharp/src/CodecLib.fs`
(`Codec<'Encoding,'T>`, the `codec { ... }` CE, lossless ISO-8601 datetime formats). Codecs for record
and union types are **generated** rather than hand-written:

- `LibCodecGen/src/CodecGen.Common.fs` walks types via the F# Compiler Service and emits
  `get_Codec<'Encoding>()` members for everything marked `[<CodecAutoGenerate>]`
  (`[<SkipCodecAutoGenerate>]` opts out).
- Union cases carry a `__v1` version marker for forward compatibility.

On the server's HTTP edge, encoders/decoders are produced by reflection (`generateAutoEncoder<'T>` /
`generateAutoDecoder<'T>` in `LibLifeCycleHost/src/Web/Api/V1/JsonEncoding.fs`).

## Codec evolution validation

`LibCodecValidation` + `validate-codec.sh` exist to prevent **breaking the wire/storage format** across
releases. The tool extracts each type's codec shape into a `JsonNode` schema (terminal / record / choice /
option / array / tuple / any-one-of), serializes it per git branch, and runs an **evolution checker**
(`EvolutionCheckerLib.fs`) comparing old vs new. It enforces rules like: you may make a required field
optional but not vice-versa; you may not change a terminal's type; union case counts must align. This is a
genuinely strong safety net given that subjects are persisted as JSON.

```bash
./validate-codec.sh --suite Logistics --fromcodec releases/logistics --tocodec master
```

> **Consuming the wire from non-EggShell clients.** The codec conventions here (the `__v1` union marker,
> `Option`/`NonemptyList` encodings, ISO-8601 datetimes) are the wire contract. For how other clients
> (a .NET NuGet SDK, an OpenAPI-generated client) can consume the standalone backend, and how the
> codec-shape extractor seeds an OpenAPI contract, see
> [Backend Interoperability](./modernization/backend-interop.md).

## Request path and subscriptions

- **Request/response:** client `HttpService` (`LibClient/src/Services/HttpService/HttpService.fs`) encodes
  an `ApiEndpoint`'s payload to JSON, hits the V1 generic HTTP handler
  (`Web/Api/V1/GenericHttpHandler.fs`), which decodes input, runs session/access checks, executes a
  view/action on the grain, and encodes the result (200 / 422 OpError / 403 denied / 400 bad request).
- **Subscriptions (live push):** real-time updates use **SignalR** (`Fable.SignalR`; endpoints
  `/api/v1/realTime`). The server builds an `IObservable<SubjectChange<...>>` per subject (`Web/RealTime.fs`),
  backed by Rx with `Replay(1)` + ref-counted disposal, and pushes changes to subscribed clients. On the
  client, `EntityService` turns those into `Subscription<'T>` streams that emit `AsyncData<'T>`
  (`Uninitialized | Fetching | Available | Error`), which components consume.

### Real-time / SignalR

The transport is standard Microsoft SignalR on both ends: `@microsoft/signalr` (client runtime) and
ASP.NET Core SignalR (server). What is *not* off-the-shelf is the **typed F# binding** over it.

- **Not a reimplementation of SignalR.** EggShell does not reimplement the SignalR protocol. It vendors the
  dead **`Fable.SignalR`** F# binding (the `Fable.SignalR` 0.16.0 / `Fable.SignalR.AspNetCore` 0.14.0 NuGet
  packages, derived from `Shmew/Fable.SignalR`, MIT) into a sibling repo, **`eggshell-signalr`**, checked out
  beside `subject/`. `LibUiSubject` and `LibLifeCycleHost` reference it by path.
- **Why vendor rather than drop it.** EggShell leans on the binding's deepest feature, **typed bidirectional
  streaming hubs** (`SignalR.connect<ClientApi, ClientStreamApi, _, ServerApi, ServerStreamApi>`,
  `Settings`/`Config`), plus MessagePack wire encoding and reconnect-driven stream recreation, across two
  real-time API generations (legacy + v1). A thin hand-rolled interop over `@microsoft/signalr` would have to
  reimplement most of that, so it is *more* work than re-hosting the MIT source, not less. The fork is thin:
  a Fable 5 port, `Subject` → `StreamSubject` rename, and a `CancellationToken` on server `streamFrom` handlers.
- **Only revisit** if EggShell ever drops typed bidirectional streaming for a request/response shape; then a
  thin interop becomes viable and the sibling repo could retire.
- **Upgrade tail.** The vendored binding still targets **net7.0**; it is bumped to net10 alongside the backend
  TFM in [Phase 3](modernization/phased-plan.md#phase-3-backend-tfm-to-net10-orleans-frozen).

## Lang libraries

- `LibLangFsharp` — the F# "prelude": codecs plus `NonemptyList/Set/Map`, `EmailAddress`, `PhoneNumber`,
  `Positive<_>`, `GeoLocation`, async/result/option extensions, etc.
- `LibLangTypeScript` — the runtime counterparts Fable-compiled code relies on at JS runtime (`Option`,
  `Lazy`, promise helpers, pattern-matching helpers).
