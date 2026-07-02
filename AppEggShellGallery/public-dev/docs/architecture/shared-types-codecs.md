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

## Lang libraries

- `LibLangFsharp` — the F# "prelude": codecs plus `NonemptyList/Set/Map`, `EmailAddress`, `PhoneNumber`,
  `Positive<_>`, `GeoLocation`, async/result/option extensions, etc.
- `LibLangTypeScript` — the runtime counterparts Fable-compiled code relies on at JS runtime (`Option`,
  `Lazy`, promise helpers, pattern-matching helpers).
