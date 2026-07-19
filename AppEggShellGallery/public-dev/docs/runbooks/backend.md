# Backend Runbook

Procedures for bringing up the backend stack, diagnosing silo boot failures, and triaging Orleans test stalls.

---

## Bringing up the stack

### Modes

`SuiteTodo/dev-stack.sh` drives both fake and real modes:

- `./dev-stack.sh up` (default): **fake mode** (in-memory todos, no SQL, no backend host). Fastest.
- `./dev-stack.sh up --real`: **real backend** on local Docker SQL (MSSQL).
- `./dev-stack.sh up --real --sql=external --sql-server=host,port --sa-password='...'`: **real backend** against an external SQL Server.

The script writes the **served** `AppTodo/public-dev/configSourceOverrides.dev.js` (with `BackendUrl` uncommented for real mode, commented for fake) and exports `ASPNETCORE_URLS=:5001`.

### External SQL Server

- **Host/port:** Pass `--sql-server=192.168.2.231,1433` (comma-separated, no space).
- **Authentication:** `--sa-password='...'` or `--sql-conn="Server=...;User Id=...;Password=...;TrustServerCertificate=true"`.
- **Provisioning:** The silo auto-provisions the `Todo` schema and Orleans membership tables on boot.
- **Verification:** `./dev-stack.sh status` shows the active `BackendUrl`; probe the backend with `curl http://localhost:5001/api/v1/ecosystem/Todo/subject/Todo/debug/all`.

### dev-stack.sh commands

| Command | Action |
|---|---|
| `up` | Start the stack (fake mode by default). |
| `up --real` | Start the real backend (local Docker SQL). |
| `up --real --sql=external --sql-server=host,port --sa-password='...'` | Start the real backend against an external SQL Server. |
| `status` | Show the active `BackendUrl` and backend health. |
| `down` | Stop the host (+ Docker SQL if used). |

### Backend-mode check

`.claude/skills/debug-web/scripts/backend-mode-check.sh` reads the *served* `configSourceOverrides.dev.js` (not the on-disk file) and reports:

```
FAKE mode (BackendUrl commented out)
REAL mode (BackendUrl = http://localhost:5001)
REAL mode but backend down (BackendUrl = http://localhost:5001, negotiate failed)
```

---

## Silo boot failures

### Catalog

| Symptom | Cause | Fix |
|---|---|---|
| `ArgumentNullException: Value cannot be null. (Parameter 'instance')` at `Autofac.RegisterInstance(null)` | Autofac.Extensions.DependencyInjection 6.0.0 predates .NET 8 keyed-services support. Orleans 10 registers keyed services. | Bump Autofac.Extensions.DependencyInjection to 10.0.0 + Autofac to 8.3.0 in the test projects. |
| `InvalidOperationException: No service for type 'Orleans.Timers.IReminderRegistry' has been registered` | Orleans 7+ factored reminders into `Microsoft.Orleans.Reminders`. The service is no longer auto-registered. | Add `.AddReminders()` to the silo builder. |
| `CodecNotFoundException: <user F# type>` at `ClusterClient..ctor` | Orleans 10 ships native F# wrapper codecs that decompose `Option`/`ValueOption`/`Choice`/`Result`/tuples and delegate to the inner leaf's codec. Whole-wrapper registrations leave the bare leaf uncovered. | Register the bare LEAF types, never the wrappers. See `LibLifeCycleCore/src/OrleansEx/Serializer.fs` (TypeIds 84-103). |
| `TLS Certificate embedded resource was not found` -> `crit: Orleans.Networking Exception in AcceptAsync` -> `Unable to connect to endpoint S127.0.0.1:20043` | The embedded `STAR_dev_subject_app.pfx` is not in the repo. | `Certificates.fs` generates an equivalent self-signed dev cert at runtime when the resource is absent. The dev client TLS sets `AllowAnyRemoteCertificate()`. |
| `Http:Urls = http://localhost:5001` not honored (Kestrel binds 5000) | The `Http:Urls` appsettings key feeds only `HttpCookieConfiguration`, not Kestrel. | Launch the host with `ASPNETCORE_URLS=http://localhost:5001`. |

---

## Orleans test triage: "stasis not reached" / 15s stasis timeouts

### What stasis means

Stasis is the 15-second window (`defaultStasisWaitFor` in `Cluster.fs`) where the test harness waits for all side effects to complete. A test fails with `Stasis not reached, N side effects not processed within 00:00:15` if any `GrainSideEffect` (transient or persistent) stays unprocessed in the `TestSideEffectTrackerHook` queue.

### First 5 things to check

1. **Serializer registration.**
   - Orleans 10 `AnalyzeSerializerAvailability` runs eagerly at `ClusterClient..ctor` and decomposes every declared grain param/return type, asking the DI for `IFieldCodec<T>` per bare leaf.
   - Audit EVERY `IClientBuilder` path (production, test-cluster, remote-host) for codec registration. Missing client-side codec registrations were silently masked under Orleans 3.7.
   - Register bare F# leaf types, not wrappers (e.g. `BlobData`, not `Option<BlobData>`).

2. **Closure args in grain calls.**
   - Orleans 10 `CopyContext.DeepCopy<T>` throws `CodecNotFoundException` for F# compiler-generated closure classes.
   - Never pass F# closures/function values across grain boundaries. Use data + command objects (e.g. `IConnectorRequestBuilder<'Request,'Reply>`).

3. **Test SDK version alignment.**
   - Under .NET 10, `Microsoft.NET.Test.Sdk` 16.8.3 cannot discover `SimulationTestFramework`-attributed tests.
   - Bump to 17.12.0, add `xunit.runner.visualstudio` (needed because `SimulationTestFramework` extends `XunitTestFramework`).

4. **In-process TestCluster vs real MSSQL silo divergence.**
   - The in-process `TestCluster` does NOT inherit the silo's DI container. A missing client-side codec registration that works in the silo may fail in the test cluster.
   - Example: `AddReminders()` was missing in the test cluster but present in the silo (session 47).

5. **Where the silo logs live.**
   - `SuiteTodo/Launchers/Dev/DevelopmentHost/src/bin/Debug/net10.0/DevelopmentHost.log` (real silo).
   - `LibLifeCycleTest/bin/Debug/net10.0/TestCluster.log` (in-process TestCluster).

### Next steps

- **Read the silo logs.** Look for `CodecNotFoundException`, `IReminderRegistry` errors, or grain dispatch failures.
- **Use the mssql-debug skill** to inspect the DB state (e.g. `stalled-list`, `failed-sideffects`).
- **Spike first** (per spike-driven skill) if the issue is a third-party-ecosystem question (e.g. Orleans, ASP.NET, Npgsql).

---

## Pointers

- **DB inspection:** Use the [mssql-debug skill](../.claude/skills/mssql-debug/SKILL.md) for read-only queries against the dev DB.
- **Upgrade spikes:** Use the [spike-driven skill](../.claude/skills/spike-driven/SKILL.md) for third-party-ecosystem questions.
- **Engineering log:** See [knowledge-base/engineering-log.md](../knowledge-base/engineering-log.md) for backend-specific sessions (S43-S48, RW7).