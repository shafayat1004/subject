# The Test Framework (a core reliability feature)

`LibLifeCycleTest` is what makes EggShell apps trustworthy. Tests are written as `simulation { ... }`
computation expressions (`SimulationBuilder.fs`) and discovered via a custom xUnit `[<Simulation>]`
attribute. For the developer-facing guide, see [Subject → Testing](./subject/testing.md).

## What it gives you

- **A real in-memory Orleans cluster** (`TestCluster.fs`, Orleans `TestingHost`, volatile storage by
  default) — so tests exercise actual grain activation/concurrency, not mocks.
- **A virtual clock** (`ClockSimulation.fs`). The `Clock` default service is replaced by a per-partition
  simulated clock that only advances when you tell it to: `Ecosystem.moveTimeForwardAndRunReminders`,
  `setNewTimeAndRunReminders`, `overrideInitialSimulatedTime`. Each read ticks 100 ticks forward for
  monotonicity. Reminders/timers fire deterministically as the clock crosses their due time.
- **Connector interception** (`Ecosystem.interceptConnector`) — external calls are answered by the test,
  so integrations are simulated precisely.
- **Stasis detection** — after each action/time-move, the harness waits until the side-effect queue is
  fully drained and no timers are overdue before asserting (`waitForSystemStasis`,
  `SideEffectTracking.fs`). This eliminates the usual async-test flakiness.
- **Rich assertions** — `thenAssertOk`, `thenAssertEvent(Triggered)`, `thenAssertEventually(Within)`,
  `thenAssertSome`, `thenAssertNoBadLogs` (fails the test if the system logged warnings/errors).

Operations like `construct/act/get/readView` come in immediate and `...Wait` (await a specific `LifeEvent`)
flavors, so you can assert "this action eventually causes that event" across simulated time.

## Scope and rough edges

**Scope:** backend only. There is no UI/component test harness — frontend validation goes through the
gallery audit toolkit instead (see [Runbooks → Audit Toolkit](./runbooks/audit-toolkit.md)).

Known rough edges: a forward-only clock, an explicit `hackDelay` workaround for async subscription timing,
and default test parallelism capped at 2 (serialization makes tests CPU-heavy).
