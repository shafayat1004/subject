# SuiteTodo notes

## Simulation tests

Tests follow **`LibLifeCycleTest` + `[<Simulation>]`** patterns from in-repo **`SuiteJobs`**
(construct/act, timers via `thenMoveTimeForwardAndRunReminders`, basic gens).

**Feature coverage to grow over time** (when useful): view reads in sim,
`constructWait` / life-event asserts, delete timer, richer gens. **`dotnet test` runner wiring**
still in progress on net10 SDK.

## SQL Server full-text search

The framework provisions a **full-text catalog** and per-lifecycle **`_SearchIndex`** tables
when a subject defines a non-`NoSearchIndex` search index (`LibLifeCycleHost` SQL setup).

SuiteTodo indexes **`TodoSearchIndex.Title`** so the reference app exercises FTS on SQL Server
(via `./dev-stack.sh` / Docker). Simulation tests query with `IndexPredicate.Matches`.
