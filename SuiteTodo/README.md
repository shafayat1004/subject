# SuiteTodo (Phase 5 reference app)

Full-stack TODO reference for goal B (see `MIGRATION_RUNBOOK.md` Phase 5).

## Layout

```
SuiteTodo/
  Ecosystem/
    Todo.Types/       Subject + TodoListView projection types
    LifeCycles/       TodoLifeCycle, TodoListView, AllLifeCycles
    Tests/            LibLifeCycleTest simulation tests
  AppTodo/            Pure-F# frontend (Phase 5C)
  Launchers/Dev/
    DevelopmentHost/  V1 HTTP + SignalR dev silo (SQL required; see template.appsettings.Development.json)
    TypesCodecGen/    Codec generation (requires LibCodecGen build fix on net10 SDK)
```

## Dev stack (one command)

From `SuiteTodo/`:

```bash
./dev-stack.sh up      # Docker SQL + DevelopmentHost + AppTodo dev-web
./dev-stack.sh down    # stop host (+ SQL container)
./dev-stack.sh status
```

Requires Docker for SQL (see `docker-compose.yml`) or your own SQL Server with `appsettings.Development.json`.
First run copies `template.appsettings.Development.json` → `appsettings.Development.json` (gitignored).

See **`NOTES.md`** for simulation-test scope and SQL full-text search notes.

## Dev host (Phase 5B)

1. Or use `./dev-stack.sh up` (creates `appsettings.Development.json` from template if missing).
2. Manual: copy `Launchers/Dev/DevelopmentHost/src/template.appsettings.Development.json` to `appsettings.Development.json`.
3. `export DOTNET_ROOT="$HOME/.dotnet"`
4. `dotnet run --project Launchers/Dev/DevelopmentHost/src/DevelopmentHost.fsproj`
5. Backend URL default: `http://localhost:5001` (V1 API + `/api/v1/realTime`).

Orleans silo/gateway ports: **20042 / 20043** (chosen to avoid collisions with other suites).

## Build / test

```bash
export DOTNET_ROOT="$HOME/.dotnet"
dotnet build Ecosystem/LifeCycles/LifeCycles.fsproj
dotnet build Launchers/Dev/DevelopmentHost/src/DevelopmentHost.fsproj
dotnet build Ecosystem/Tests/Tests.fsproj
dotnet test Ecosystem/Tests/Tests.fsproj   # simulation runner wiring in progress
```

## Feature coverage (backend, Phase 5A)

| Feature | Status |
|---|---|
| Subject CRUD lifecycle | Done |
| LifeEvents (Created, TitleChanged, DoneToggled, Archived) | Done |
| OpError EmptyTitle | Done |
| View projection TodoList (active todos) | Done |
| SQL full-text search (`TodoSearchIndex.Title`) | Done |
| Timer auto-archive after 5 min when Done | Done |
| Simulation tests | Written (4 tests) |
| Dev host | Builds; `./dev-stack.sh up` (Docker SQL + host + dev-web) |
| Frontend AppTodo | Phase 5C (Web Debug build green; Fable via eggshell pending npm init) |
| Playwright audit | Phase 5D (stub at `AppTodo/audit/audit-todo-web.mjs`) |
| Dev stack script | `dev-stack.sh` (Phase 5E precursor) |
| Scaffold templatize | Phase 5E (Meta/LibScaffolding) |

## Frontend (Phase 5C)

Minimal auth-free app wired to the Todo ecosystem:

- `SubjectService` for `Todo` (construct, toggle, delete)
- `ViewService` for `TodoList` projection
- `RealTimeService` (SignalR keep-alive on web)
- `UiActionLog` hook in debug builds
- `A11ySlug.testId` on add/toggle/delete controls

```bash
cd AppTodo
./initialize          # npm install + symlinks
dotnet build src/App.fsproj -c "Web Debug"
# With SQL dev host running on :5001:
# dotnet fsi build.fsx -- -t Eggshell --command=dev-web
```

Default backend URL: `http://localhost:5001` (see `configSourceOverrides.dev.js`).

## CI vision (Phase 5F+)

Post-commit pipelines should build and smoke-test both **AppTodo** (template benchmark) and **AppEggShellGallery** (component regressions), mirroring `audit-todo-web.mjs` and `audit-gallery-full.mjs`.
