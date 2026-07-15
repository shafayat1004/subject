# Evaluating EggShell: strengths, tradeoffs, and fit

EggShell is an opinionated, full-stack, single-language application framework: F# on both ends, the
domain modeled as state machines ("lifecycles") hosted as Microsoft Orleans grains, the same F# types
shared client-to-server over JSON, and a deterministic virtual-time test harness built into the core.
It is best compared to an assembled conventional stack (an ASP.NET Core service, an ORM, a background-job
runner, a realtime channel, a DTO/mapping layer, and a separate SPA), not to any single library.

This page is an honest positioning: where the framework has a real edge, where a conventional .NET stack
is the better choice, and what its performance profile actually looks like. For consuming the backend
from clients other than the built-in F#/Fable UI, see
[Backend Interoperability](./modernization/backend-interop.md).

## Strengths

- **One type system, no DTO seam.** The record a grain persists is the record a component renders. The
  wire is just a JSON encoding of shared F# types (see [Shared Types & Codecs](./architecture/shared-types-codecs.md)).
  This removes an entire category of work: hand-maintained DTOs, object mappers, and client/server
  drift bugs. This is the most defensible single advantage.
- **Deterministic time-travel testing as a first-class primitive.** Tests run a real in-memory Orleans
  cluster with a virtual clock the test advances by hand, connector interception for external calls, and
  stasis detection that drains the side-effect queue before asserting (see
  [Testing Framework](./architecture/testing-framework.md)). This removes async-test flakiness
  structurally rather than by convention, which is rare even among mature frameworks.
- **Distribution, persistence, and concurrency are the framework's job.** A developer writes a
  transition and returns side effects (create subjects, publish events, schedule timers, call
  connectors). Activation, single-threaded-per-grain concurrency, persistence, optimistic-concurrency
  ETags, timers/reminders, and pub-sub are handled by the framework.
- **State machines force domain rigor.** Actions, events, constructors, and errors are discriminated
  unions with exhaustive matching, so illegal states and unhandled transitions are compile errors. The
  business logic reads as a specification.
- **Reactive end-to-end.** A client subscribes to a subject and receives `AsyncData<'T>` pushed over
  SignalR as state changes, with no hand-written cache-invalidation or polling plumbing.

## Tradeoffs and costs

- **Bespoke-framework bus factor.** Orleans underneath is battle-tested at large scale, but the EggShell
  layer on top is a bespoke framework with a small maintainer base, its own documentation, and no
  external hiring pool. A conventional ASP.NET Core stack has vendor backing, a large community, and a
  deep body of public knowledge.
- **You own the stack's upgrade path.** When the foundation ages, there is no external migration to ride;
  the current modernization effort (render-DSL retirement, React/RN upgrade, the pending Orleans and
  Postgres work) is itself the evidence of this cost.
- **Learning curve and staffing.** F#-everywhere plus the actor model plus the lifecycle programming
  model is a steep ramp. You cannot hire people who already know the framework.
- **The shared type layer is dual-compiled.** Domain types compile natively for the backend and through
  Fable to JavaScript for the frontend, so those types must stay Fable-clean (no arbitrary .NET-only
  APIs inside the shared `*.Types` projects). This is a real constraint on where code can live.
- **Testing is backend-only.** There is no component/UI test harness; frontend correctness goes through
  the gallery audit toolkit.

## Performance characteristics

There is no benchmark of this codebase yet, and the framework is currently running its slow-path
defaults (Orleans 3.7.2 wire serializer, JSON storage serializer). The points below describe the shape
of the actor model, not measured numbers. See [SQL Server to Postgres](./modernization/sql-server-to-postgres.md)
for the perf-relevant parts of the pending Orleans and storage upgrade.

Where the actor model wins:

- **Hot state lives in memory, not the database.** An activated grain serves reads and writes from RAM,
  effectively a strongly-consistent write-back cache built into the programming model. For frequently
  accessed entities this is a large latency and database-load reduction that a stateless service would
  have to build and invalidate by hand.
- **No lock contention within an entity.** Single-threaded-per-grain execution gives per-entity
  serialization without locks or retry storms.
- **Horizontal scale by partitioning.** Grains shard across silos by id; throughput scales by adding
  silos. Orleans is proven at very large scale, so the model is not the ceiling.
- **Push, not poll.** Live subscriptions push deltas over SignalR.

Where a conventional stack wins:

- **Per-request overhead.** A plain minimal-API endpoint doing one indexed query beats a grain call on
  raw single-request latency and trivial-endpoint throughput; grain calls marshal state, may hop to the
  owning silo, and go through a directory lookup.
- **Cold activation latency.** First touch of an idle grain deserializes state and activates, so cold
  tail latency is worse than an always-warm stateless service.
- **The "hot grain" bottleneck.** Single-threaded-per-grain is a hard ceiling for a single hot entity;
  a stateless design can spread that same entity's load across every instance.
- **Read-mostly and analytical traffic** is often faster on a conventional stack with read replicas and
  a CDN or cache than routed through grains.

Net: the model trades a little per-request overhead for keeping hot state in memory with strong
consistency. On stateful domains that is a net performance and database-cost win; on stateless CRUD it is
slower than a conventional stack.

## When EggShell is a good fit

- Domain-heavy, stateful, workflow-oriented backends that are naturally state machines with timers and
  reactions (orders, claims, approvals, device state, running aggregates).
- Small full-stack teams that can absorb the learning curve and want one language with no client/server
  seam and a large amount of infrastructure handled for them.
- Correctness-critical logic where the deterministic test harness pays for itself in avoided incidents.

## When a conventional .NET stack is the better choice

- Content or CRUD-shaped applications, public APIs, and read-mostly workloads.
- Teams that must staff up quickly or that need a large third-party ecosystem.
- Organizations that value maturity and a deep public knowledge base over per-domain leverage.

## In one line

EggShell competes on leverage for stateful domain logic, not on maturity or hiring. Shared types remove
the DTO/mapping tax and the deterministic actor test harness gives reliability that conventional stacks
approximate by hand. If the problem is a stateful workflow domain and the team can own the stack, it is a
real edge. If the problem is CRUD or the team must hire fast, a conventional stack is the safer call.
