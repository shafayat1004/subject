# Orleans Test Triage Skill

When SuiteJobs/LibLifeCycle tests fail with stasis/timeout/CodecNotFound, follow this ordered triage procedure.

---

## Inputs
- Test output (e.g. `Stasis not reached, 1 side effects not processed within 00:00:15`).
- Silo logs (`TestCluster.log` or `DevelopmentHost.log`).
- Test name and suite (e.g. `SuiteJobs/Ecosystem/Tests`, `LibLifeCycleTest`).

---

## Procedure

### 1. Confirm the failure is stasis/timeout/CodecNotFound

```bash
# SuiteJobs
cd SuiteJobs/Ecosystem/Tests
dotnet test | grep -E "Stasis not reached|CodecNotFoundException"

# LibLifeCycleTest
cd LibLifeCycleTest
dotnet test | grep -E "Stasis not reached|CodecNotFoundException"
```

If the output does not match, escalate to the parent.

---

### 2. Check serializer registration

**Symptom:** `CodecNotFoundException: <user F# type>` at `ClusterClient..ctor`.

**Action:**

- Audit EVERY `IClientBuilder` path for codec registration:
  - Production: `GrainConnectorProvider.fs`, `BiosphereGrainFactory.fs`, `Startup.fs`.
  - Test: `TestCluster.fs` (`TestSiloClientConfigurator`).
  - Remote-host: `SiloBuilder.fs` (`HostToRemoteHost` branch).
- Ensure `configureSiloClientSerializers` is called in the client path.
- Register bare F# leaf types (e.g. `BlobData`), not wrappers (e.g. `Option<BlobData>`).

**Verify:**

```bash
# Check for missing codec registrations in the silo logs
grep -i "CodecNotFoundException" TestCluster.log
```

---

### 3. Check for closure args in grain calls

**Symptom:** `CodecNotFoundException: LibLifeCycle.Services+buildRequest@282-2<...>`.

**Action:**

- Never pass F# closures/function values across grain boundaries.
- Replace with data + command objects (e.g. `IConnectorRequestBuilder<'Request,'Reply>`).

**Verify:**

```bash
# Check for F# closure classes in grain method signatures
grep -r "FSharpFunc\|fun " LibLifeCycleCore/src/GrainClientInterface.fs
```

---

### 4. Check test SDK version alignment

**Symptom:** `0 tests run` + `The Settings file '.runsettings' could not be found`.

**Action:**

- Bump `Microsoft.NET.Test.Sdk` to 17.12.0.
- Add `xunit.runner.visualstudio` (needed because `SimulationTestFramework` extends `XunitTestFramework`).
- Remove dangling `<RunSettingsFilePath>`.

**Verify:**

```bash
# Check test discovery
cd SuiteJobs/Ecosystem/Tests
dotnet test --list-tests
```

---

### 5. Check in-process TestCluster vs real MSSQL silo divergence

**Symptom:** Stasis failures in SuiteJobs but not LibLifeCycleTest.

**Action:**

- The in-process `TestCluster` does NOT inherit the silo's DI container.
- Compare `TestCluster.fs` and `SiloBuilder.fs` for missing registrations (e.g. `AddReminders()`).

**Verify:**

```bash
# Compare silo and test-cluster logs for missing registrations
grep -i "AddReminders\|IReminderRegistry" TestCluster.log DevelopmentHost.log
```

---

### 6. Read the silo logs

**Action:**

- `LibLifeCycleTest/bin/Debug/net10.0/TestCluster.log` (in-process TestCluster).
- `SuiteTodo/Launchers/Dev/DevelopmentHost/src/bin/Debug/net10.0/DevelopmentHost.log` (real silo).

**Look for:**

```bash
grep -iE "CodecNotFoundException|IReminderRegistry|ArgumentNullException|Stasis" TestCluster.log
```

---

### 7. Inspect the DB (if applicable)

**Action:** Use the [mssql-debug skill](../mssql-debug/SKILL.md):

```bash
# List stalled subjects
dotnet fsi .claude/skills/mssql-debug/scripts/stall-list.fsx Todo_Dev Todo

# Decode a subject blob
dotnet fsi .claude/skills/mssql-debug/scripts/decode-subject.fsx Todo_Dev Todo "<subject_id>"
```

---

### 8. Spike if needed

**Action:** Use the [spike-driven skill](../spike-driven/SKILL.md) for third-party-ecosystem questions (e.g. Orleans, ASP.NET, Npgsql).

Example:

```bash
skill spike-driven "Orleans 10 F# closure misclassification" upstream=dotnet/orleans query="F# closure CS1646"
```

---

## Outputs
- Root cause (e.g. missing codec registration, closure args, test SDK mismatch).
- Fix (e.g. register bare leaf types, replace closures with command objects, bump test SDK).
- Verification steps (e.g. re-run tests, check silo logs).

---

## Escalation
- If the procedure does not resolve the issue, escalate to the parent with:
  - Test output.
  - Silo logs.
  - Steps attempted.
  - Hypotheses.