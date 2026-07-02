# Backend: the Lifecycle / State-Machine Core

The backend programming model is a strongly-typed state machine called a **lifecycle**. See
[Subject](./subject/index.md) for the developer-facing guide; this page is the architectural view.

## What a lifecycle is

A **lifecycle** is a strongly-typed state machine over a **Subject**. It is a record of functions,
defined in `LibLifeCycle/src/LifeCycle.fs`:

```fsharp
type LifeCycle<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent,
               'SubjectIndex, 'SubjectId, 'AccessPredicateInput, 'Session, 'Role, 'Env> = {
    IdGeneration:    'Env -> 'Constructor -> IdGenerationResult<'SubjectId, 'OpError>
    Construction:    'Env -> 'SubjectId -> 'Constructor -> ConstructionResult<...>
    Transition:      'Env -> 'Subject -> 'LifeAction -> TransitionResult<...>

    Subscriptions:   'Subject -> Map<SubscriptionName, Subscription<'LifeAction>>
    Timers:          'Subject -> list<Timer<'LifeAction>>
    Indices:         'Subject -> seq<'SubjectIndex>

    Storage:         LifeCycleStorage
    MaybeApiAccess:  Option<LifeCycleApiAccess<...>>
    ResponseHandler: SideEffectResponse -> seq<SideEffectResponseDecision<'LifeAction>>
    // ...
}
```

Core type vocabulary (`LibLifeCycleTypes/src/SubjectTypes.fs`):

| Concept        | Role |
|----------------|------|
| `Subject`      | The persisted state (a record). Must carry a `SubjectId` and a creation timestamp. |
| `LifeAction`   | A discriminated union of commands that drive transitions. |
| `LifeEvent`    | Emitted when a transition completes; other subjects can subscribe to these. |
| `Constructor`  | The "create" payload — input to `IdGeneration` + `Construction`. |
| `OpError`      | Domain error type for a lifecycle. |
| `SubjectIndex` | Promoted, queryable projections of subject state (string / numeric / geo / full-text). |

Transitions are written in computation expressions (`transition { ... }`, `construction { ... }`,
`operation { ... }`, `idGeneration { ... }`). A transition returns the new subject plus **side effects**:

```fsharp
type TransitionSideEffects<'Constructor, 'LifeEvent, 'LifeAction> = {
    Constructors:    List<'Constructor>                       // create other subjects
    LifeEvents:      List<'LifeEvent>                         // publish events
    LifeActions:     List<'LifeAction>                        // enqueue actions on self
    ExternalActions: List<ExternalOperation<'LifeAction>>     // act on other lifecycles / connectors
}
```

Timers are declarative and time-relative:

```fsharp
type Schedule = Now | On of DateTimeOffset | AfterLastTransition of TimeSpan
type TimerAction<'LifeAction> = RunAction of 'LifeAction | DeleteSelf
```

Subscriptions wire one subject's events to another's actions:

```fsharp
type Subscription<'LifeAction> =
| ForSubject    of SubjectSubscription * ActionToRaise: 'LifeAction
| ForSubjectMap of SubjectSubscription * (LifeEvent -> Option<'LifeAction>)
```

> **The "QQQ" convention.** Builder CEs (`TransitionBuilder.fs`, etc.) define a `TODO` sentinel that
> raises `NotImplementedException "QQQ Not implemented yet"`. Paired with custom warning codes
> (`10666` = not-implemented, `10667` = long-term-development not-implemented in `Directory.Build.props`),
> this lets devs leave typed holes that warn in dev and **error in Release** — a clean way to ship-block
> on stubs.

## Other first-class building blocks

All in `LibLifeCycle/src`:

- **Views** (`View.fs`) — read-only projections over subjects, with their own access control. This is the
  primary read path the frontend hits. See [Views](./subject/views.md).
- **TimeSeries** (`TimeSeries.fs`) — units-of-measure-typed, time-indexed data streams ingested via side
  effects; support bucketing/aggregation.
- **Connectors** (`Services.fs`) — the integration boundary to the outside world (HTTP APIs, gateways,
  messaging). Single-response (`ResponseChannel`) or streaming (`MultiResponseChannel`). Crucially,
  connectors are the seam the **test harness intercepts** to mock external systems.
- **Ecosystem** (`Ecosystem.fs`) — a named bundle of lifecycles + views + time series + connectors. An
  ecosystem maps onto an Orleans cluster identity (ClusterId/ServiceId = ecosystem name).
- **Default services** (`DefaultServices.fs`) — ambient capabilities injected into `Env`: `ISubjectRepo`,
  `IBlobRepo`, `ITimeSeriesRepo`, `ICryptographer`, `ISequence`, `Clock`, etc. The `Clock` service is
  what makes time mockable (see [Testing Framework](./architecture/testing-framework.md)).

## Mapping onto Orleans

Each lifecycle is hosted as an Orleans **stateful grain**:

- `ISubjectClientGrain` (`LibLifeCycleCore/src/GrainClientInterface.fs`) — the callable surface:
  `Construct`, `Act`, `ActMaybeConstruct`, `Get`, etc., all `Task<Result<...>>`.
- `SubjectGrain` (`LibLifeCycleHost/src/SubjectGrain.fs`) — the activation that holds state and runs the
  lifecycle's `Transition` / `Construction` functions.
- `SubjectGrainModel.fs` — the serializable grain state, including a **persistent side-effect queue**
  (`Persisted` vs `Transient` effects: RPCs, timer triggers, subscription responses, self-actions,
  transactional steps, time-series ingestion).
- Supporting grains: `ISubjectIdGenerationGrain`, `ISubjectRepoGrain` (queries/indices),
  `DynamicSubscriptionDispatcherGrain` (pub-sub fan-out), and reminder integration.

The side-effect queue is the heart of reliability: a transition's effects are persisted alongside the
state change, then drained asynchronously with retries. This is what gives the framework
exactly-once-ish semantics across grain calls, and what the test harness's "stasis" detection waits on.
