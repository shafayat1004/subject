# Backend Interoperability: standalone service, client SDKs, and API contract

The backend is already a standalone service. This page describes how clients other than the built-in
F#/Fable UI can consume it, and the work that makes non-EggShell .NET clients and polyglot clients
first-class. That work is tracked as two roadmap goals: **Goal J** (standalone service + .NET client
SDK) and **Goal K** (polyglot API contract via OpenAPI + generated clients). See
[Goals & Roadmap](./modernization/goals-and-roadmap.md).

For the wire format itself see [Shared Types & Codecs](./architecture/shared-types-codecs.md); for the
overall fit discussion see [Evaluating EggShell](./evaluating-eggshell.md).

## The backend is standalone (dependency direction)

The repository is a monorepo, but the code dependency direction is a clean Y, not a tangle:

```
        LibLifeCycleTypes / LibLangFsharp / Suite*.Types   (shared domain types + codecs)
                 ▲                               ▲
                 │                               │
   backend (LibLifeCycle, Host, Orleans)     frontend (LibClient, LibUi*, App*)
```

- The backend host references **no** frontend project (`LibLifeCycleHost.fsproj` has no reference to
  `LibClient`, `LibUi*`, or any `App*`).
- The frontend depends on the shared type projects only (`LibClient` -> `LibLangFsharp` +
  `LibLifeCycleTypes`), the same projects the backend references.

The two halves are co-located, not code-coupled. The backend can be built and run with the entire
frontend removed.

## The API surface

- **Request/response:** JSON over HTTP. A client encodes an `ApiEndpoint` payload, hits the generic V1
  handler (`Web/Api/V1/GenericHttpHandler.fs`), which decodes, runs session/access checks, executes a
  view or action on the grain, and returns `200` / `422` (OpError) / `403` (denied) / `400` (bad
  request).
- **Live subscriptions:** a standard SignalR hub at `/api/v1/realTime` pushes `SubjectChange` values to
  subscribers.

Views are the read API; actions are the write/command API. The built-in Fable UI has no transport
privilege; it uses these same public endpoints.

## The contract problem

Two things a client needs are distinct:

1. **The encoding** (the JSON shape on the wire). It is produced by Fleece codecs with EggShell-specific
   conventions: a `__v1` version marker on union cases, specific encodings for `Option`,
   `NonemptyList`/`Set`/`Map`, and lossless ISO-8601 datetimes.
2. **The contract** (a machine-readable description of the endpoints and types). This does **not** exist
   as a published artifact today; a foreign client must currently infer both from the F# source.

The encoding can be standardized, with one important constraint: the same family of codecs also defines
the at-rest storage format (subjects persist as gzip JSON), protected by the `LibCodecValidation`
evolution gate. The storage encoding must not be changed to prettify the API. The clean approach is to
**decouple them**: keep the storage codec as-is and introduce a standardized-JSON projection at the HTTP
edge only (or a `/v2` edge). A useful seed already exists: `LibCodecValidation` /
`EvolutionCheckerLib.fs` already walks every type and extracts its codec into a `JsonNode` schema, which
is most of an OpenAPI / JSON-Schema emitter.

## Consumer tiers

The backend supports three consumer tiers against the same standalone service:

1. **F#/Fable UI (today).** The built-in frontend, with full shared-types benefit.
2. **Any .NET client via a NuGet SDK (Goal J).** Package the shared type projects
   (`LibLifeCycleTypes`, `LibLangFsharp`, `Suite*.Types`) plus the codecs plus a headless
   `HttpService`/`EntityService` (that is `LibClient` with the Fable/RN UI removed). Any .NET consumer
   (Blazor, MAUI, WPF, a console daemon, another ASP.NET service) then gets fully-typed, no-DTO access to
   views, actions, and subscriptions with no Fable dependency. The shared-types value survives intact
   because the types compile natively to .NET. This is the highest-ROI path and is mostly a carve-out of
   existing code: split `LibClient` into `LibClient.Core` (transport + codecs, .NET-portable) and
   `LibClient.Ui` (Fable/RN), and package the former.
3. **Any language via an OpenAPI contract plus generated clients (Goal K).** Emit an OpenAPI / JSON-Schema
   contract (seeded from the codec-shape extractor above) and generate typed clients for Dart, Kotlin,
   Swift, TypeScript, Python, and so on. These regenerate as the server changes, so they do not rot the
   way hand-written per-language SDKs do. Optionally add the standardized-JSON edge projection so
   generated models are clean.

## Contract-first, generate SDKs

The two approaches are not either/or. Standardizing the contract is the enabler; a client library is the
output. Order:

1. Emit the OpenAPI / JSON-Schema contract from the codec-shape tool that already exists. This also gives
   free API documentation.
2. Generate client libraries from that contract rather than hand-writing them.
3. Add the standardized-JSON edge projection only if the raw encoding makes generated models awkward
   (for example `__v1` tags leaking into Dart/Kotlin models). Never change the storage codec.
4. Ship the Fable/JS SDK as the cheapest first client: it can reuse the existing shared types and
   generated codecs Fable-compiled to JS, proving the standalone-consumer path with no contract work.

## Easy wins

- **Carve `LibClient.Core`** (Goal J). Split transport + codecs (`.NET`-portable) from the Fable/RN UI,
  and package it as a NuGet client SDK for .NET consumers. Mostly a project split, not a rewrite. This
  also advances the long-standing "move Subject-related components out of `LibClient`" item in
  [Goals & Roadmap](./modernization/goals-and-roadmap.md).
- **Emit an OpenAPI / JSON-Schema contract** (Goal K) by wiring the existing `LibCodecValidation` type
  walker to output a schema document instead of only comparing shapes for the evolution gate.
- **Publish the JS SDK from the existing codecs** (Goal J) (Fable-compiled shared types + generated
  codecs) as the first non-UI consumer.

## Status

Not started. Tracked as two roadmap goals: [Goal J](./modernization/goals-and-roadmap.md) (standalone
service + .NET client SDK) and [Goal K](./modernization/goals-and-roadmap.md) (polyglot API contract:
OpenAPI + generated clients).
