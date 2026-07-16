# Consuming Subjects from the Frontend

Subjects are read and mutated from the frontend through the shared type system: the same F# types the
backend persists are the types the client sends and receives.

## Reading (subscriptions)

The dominant pattern is **subscription-driven**. A component subscribes to a subject or a
[view](./views.md); the client's `EntityService` turns the server's SignalR push stream into a
`Subscription<'T>` that emits `AsyncData<'T>` (`Uninitialized | Fetching | Available | Error`). The
component re-renders as new state arrives. See
[Architecture → Shared Types & Codecs](../architecture/shared-types-codecs.md) for the wire path.

## Writing (actions)

To drive a transition, the client encodes a `LifeAction` payload and calls the V1 HTTP endpoint via
`HttpService`; the server decodes it, runs session and [access-control](./access-control.md)
checks, executes the action on the grain, and returns `Ok` or a typed `OpError`. The
[Executor](../how-to/executors.md) pattern wraps this with built-in in-progress and error states.

## Related

- [Actions and transitions](./actions-and-transitions.md)
- [Events and subscriptions](./events-and-subscriptions.md)
- [Views](./views.md)
- [Architecture → Frontend](../architecture/frontend.md)
