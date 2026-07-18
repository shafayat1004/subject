---
name: spike-driven
description: Run a time-boxed spike to de-risk a third-party-ecosystem or unfamiliar-territory question (Orleans, ASP.NET, Fable, Npgsql, React Native, Postgres, etc.) before committing to production code. Use when the master plan has a "spike Sx" entry, when a library/stack version jump is being evaluated, when a single change touches many consumers, or when you need to prove an unknown before doing the real work. Produces a catalog doc + engineering log entry + codemem entry + green build evidence (or empirical FAIL evidence). NOT for routine feature work.
user-invocable: true
argument-hint: "<spike-name> [query=<symptom keywords>] [upstream=<OWNER/REPO>]"
---

# spike-driven

Spikes de-risk the unknowns before committing to production code. A spike is **throwaway** code +
**durable** documentation: catalog doc + engineering log entry + codemem entry + green build
evidence (or empirical FAIL evidence). The production codebase never carries half-stabilized state.

The four rules every spike enforces (lessons from S0, S10, S15, mssql-debug):

1. **Upstream research BEFORE code.** Read the upstream repo's issues + official sample README + API
   docs BEFORE writing the `.fsproj`. Most ecosystem gotchas are already documented.
   Lesson from S15 (codemem 1893): 6 of 7 findings were upstream-documented (dotnet/orleans #8520,
   #8717, SO Q77159202, official API docs); 4-5h iteration could have been ~1h.
2. **Throwaway project under `Meta/`** — never touch production code (rule 8). If a 3-project
   layout is needed (Types / Codegen / Host), put each in its own subdirectory so `obj/` doesn't
   collide across projects.
3. **Catalog doc under
   `AppEggShellGallery/public-dev/docs/modernization/spikes/<spike-name>.md`** — durable artifact
   with verbatim errors + per-shape PASS/FAIL + decision + next-spike worklist. Follow the
   format of an existing catalog (`spikes/s10-orleans-bump-catalog.md`,
   `spikes/s15-serializer-roundtrip.md`).
4. **Mirror to codemem + engineering log** in the same commit. codemem captures the durable lesson;
   the engineering-log captures the narrative; the catalog doc captures the full evidence. All
   three live in the spike's closing commit.

## When to use

- A version jump (major upgrade of Orleans / Npgsql / Fable / RN / etc.).
- A "spike Sx" entry in `modernization/sql-server-to-postgres.md` or any other spike plan doc.
- An unfamiliar third-party API surface that gates a refactor.
- A single change touches many consumers and you need to size the break surface before doing the
  real work (TIER 0 catalog spike).
- A new pattern (e.g. codegen-host, surrogate serializer) you have not used before.
- **Extending a prior spike's pattern to production code.** This is a spike-pattern reuse, and
  it needs a SHORT re-run of step 1 (upstream issue search) using the production code's
  specific F# construct shapes as search keywords (see "Reusing a spike pattern in production"
  below). Lesson from S15b-production-port (codemem 1898): the S15b spike was green 7/7 but the
  production port hit a HARD BLOCKER late because the spike's trivial impls didn't mirror
  production's `backgroundTask { }` CEs + F# object expressions.

## When NOT to use

- Routine feature work in known territory (use `verify-feature`, `a11y-check`, etc.).
- Bug fixes in code you own (use the regular edit + build + test flow).
- Doc edits (use `docs-sync`).
- Perf work that doesn't introduce unknowns (use `verify-feature` for evidence).

## Flow

### Step 0 — Inventory prior work (cheap, fast)

- `codemem_memory_search query="<spike topic>"` — surface prior codemem entries.
- `grep -r "<spike topic>" AppEggShellGallery/public-dev/docs/` — find existing catalog docs.
- `git log --oneline --grep="<spike topic>"` — find prior commits.
- If a prior catalog exists for the SAME question, **cite it and proceed only if the prior result
  is stale** (different versions, different shapes). Otherwise close the spike as already-done.

### Step 1 — Upstream research (MANDATORY before code)

For a third-party-ecosystem question (Orleans, ASP.NET, Fable, Npgsql, React Native, Postgres, etc.):

1. **List upstream issues matching the symptom** via `gh issue` (NOT webfetch — auth wall, no
   comments):

   ```sh
   scripts/upstream-research.sh "<symptom keywords>" dotnet/orleans dotnet/aspnetcore
   ```

   The script runs `gh issue list --repo OWNER/REPO --search "<symptom>" --state all --limit 30
   --json number,title,state,url` for each repo and prints a one-row-per-match table.

2. **Read each match with comments:**

   ```sh
   gh issue view 8717 --repo dotnet/orleans --comments
   gh issue view https://github.com/dotnet/orleans/issues/8520 --comments
   ```

3. **Search SO + answeroverflow.com** for the product + symptom. Webfetch is fine for SO (no auth
   wall). For answeroverflow.com use webfetch directly.

4. **Read the official sample's README + source.** Microsoft samples live under
   `learn.microsoft.com/en-us/samples/dotnet/samples/<sample-name>/` and on GitHub under
   `dotnet/samples` — the README often contains non-obvious setup notes (e.g. the Orleans F#
   sample README says verbatim "Microsoft.Orleans.Sdk does not support emitting F# code, however,
   it supports analyzing F# assemblies and emitting C# code").

5. **Read the API docs for any option mentioned** (e.g. `TypeManifestOptions.AllowAllTypes` —
   `learn.microsoft.com/en-us/dotnet/api/<namespace>.<type>`).

6. **Check the package XML docs locally** for a referenced package — they often list every public
   type the package exposes, which tells you what the package actually does:

   ```sh
   find ~/.nuget/packages/<package-id>/<version>/lib/net10.0/ -name '*.xml' \
     -exec rg -n 'member name="T:' {} \;
   ```

   Lesson from S15 finding #2: `Microsoft.Orleans.Serialization.FSharp`'s XML doc list shows it
   only ships `FSharpUnit`, `FSharpOption`, `FSharpValueOption`, `FSharpChoice` — NO codecs for
   user-defined F# records/DUs.

7. **Record the upstream citations in the catalog doc.** Per finding: which upstream issue/sample/
   API-doc/ package-XML documents it. If no upstream source is found for a finding, mark it as
   "genuinely novel" — that's the spike's real contribution.

### Step 2 — Decide: spike still needed?

After step 1, you may find the question is fully answered upstream. In that case:

- Record the answer in a SHORT catalog doc (cite the upstream sources verbatim, no throwaway
  code).
- Append a one-line engineering-log entry: "Sx spike skipped — upstream-documented (see catalog
  for citations)."
- Run codemem-update with the upstream sources as the durable lesson.
- Commit. Done. No code required.

If the upstream answer is incomplete for the repo's specific shapes (e.g. the docs cover F#
`Option` but the repo uses `Result<'T, 'E>` on grain interfaces), the spike narrows to the
uncovered shapes. Document the narrowing.

### Step 3 — Scaffold the throwaway project

```sh
scripts/new-spike.sh <spike-name>          # e.g. s16-result-codec
```

**Mirror production's F# construct shapes (MANDATORY — lesson from S15b-production-port,
codemem 1898).** The spike's grain impls + host code MUST use the SAME F# constructs production
uses, not trivial stubs. Specifically, if production code uses any of:

- `backgroundTask { ... }` / `task { ... }` computation expressions (F# compiler emits
  closure classes like `RunAndWait@117-1<...>`),
- F# object expressions `{ new ISubjectGrainObserver<'T> with member ... }` (F# compiler
  emits closure classes implementing the interface),
- constraint-bearing generic interfaces `interface IFoo<'T when 'T :> Bar> with ...`,
- F# lambda captures inside grain methods,

the spike MUST include at least one grain impl + one grain-observer registration that
exercises each of those constructs. S15b's spike PASSED 7/7 because its grain impls were
trivial (no CEs, no object expressions); the production port then hit a HARD BLOCKER late
because Orleans 10's C# source generator (`CodeGenerator.cs:248-259` in v10.2.1)
misclassifies F# compiler-emitted closure classes as `InterfaceImplementations` and emits
broken C# referencing F# mangled names (`@` / `$` chars). The spike gave false confidence
by hiding the construct-classification bug. A 5-minute spike extension to mirror production
shapes would have surfaced it before touching 30+ production files.



Creates `Meta/<spike-name>/` with this layout (3-project pattern; drop subdirs you don't need):

```
Meta/<spike-name>/
  Types/<spike-name>Types.fsproj           # F# types with [<GenerateSerializer>] etc.
  Types/Shapes.fs
  Codegen/<spike-name>Codegen.csproj       # C# helper that triggers source generators
  Codegen/CodegenAssemblyInfo.cs            # [assembly: GenerateCodeForDeclaringAssembly(typeof(...))]
  Host/<spike-name>.fsproj                 # F# console that boots a TestCluster + asserts
  Host/Program.fs
```

**4-project variant** (needed when grain impls are F# AND the host is F#, e.g. the
S15b production-shape pattern). Add a `Grains/` subdir between Types and Codegen:

```
Meta/<spike-name>/
  Types/...                                 # F# interfaces + types
  Grains/<spike-name>Grains.fsproj          # F# grain impls (references Types)
  Grains/Grains.fs
  Codegen/...                                # C# -- scans BOTH Types AND Grains (TWO attributes)
  Host/...                                   # F# host (references Types + Grains + Codegen)
```

Without the 4-project layout, the F# Host cannot host F# grain impls because the C#
source generator (C#-only) cannot scan the F# Host project. The 4-project layout has no
project-reference cycle: Codegen (C#) references Types + Grains (F#); Host (F#) references
Types + Grains + Codegen; the F# Grains project does NOT reference the C# Codegen project
(the runtime wiring is via `[<assembly: Orleans.ApplicationPartAttribute("Codegen")>]`
on the F# Host, which is a string-name, not a project ref). Lesson from S15b (codemem 1895).

**Multiple `GenerateCodeForDeclaringAssembly` attributes**: when grain interfaces and grain
classes live in separate assemblies (e.g. interfaces in Types, impls in Grains), the C# Codegen
project must carry TWO `[assembly: GenerateCodeForDeclaringAssembly(...)]` attributes -- one
pointing at a type from the F# Types assembly (for interface invoker codegen), one at a type
from the F# Grains assembly (for grain-class metadata + activator codegen). Each attribute
scans only the DECLARING assembly of the given type; there is no transitive scan of referenced
assemblies. S15b confirmed empirically: single attribute emitted 23 lines (empty
TypeManifestProviderBase); two attributes emitted 666 lines (full invokers + activators).
Lesson from S15b (codemem 1895).

Every subdirectory has its own `.fsproj` / `.csproj` — do NOT share `obj/` across projects (MSBuild
MSB3540 error: `MSBuildProjectExtensionsPath` modified after use). Lesson from S15.

`.fsproj` / `.csproj` template (copy from an existing spike like
`Meta/S15SerializerRoundtrip/`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>8.0</LangVersion>            <!-- 13.0 for C# projects -->
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>   <!-- spike: surface real errors past new warnings -->
    <Configurations>Debug;Release</Configurations>
    <EggShellFmtSeverity>none</EggShellFmtSeverity>        <!-- skip repo format check on throwaway code -->
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Update="FSharp.Core" VersionOverride="10.0.103" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

**FSharp.Core version pinning (mandatory when an Orleans package is referenced).**
`Directory.Build.props` pins `FSharp.Core` 9.0.201 via `PackageReference Update`. Orleans 10's
`Microsoft.Orleans.Serialization.FSharp` requires >= 10.0.103. Use `VersionOverride` (NOT
`Version`) so the repo's `Directory.Build.targets` translates it to a `Version` that beats the
pinned 9.0.201. Without the override, NU1605 downgrade warning fires and `Microsoft.Orleans.
Serialization.FSharp` may not bind correctly. Lesson from S10 (codemem 1889).

**F# vs C# project syntax** (both use `VersionOverride` but different reference kinds):
- F# projects: `<PackageReference Update="FSharp.Core" VersionOverride="10.0.103" />` (uses
  `Update` because `Directory.Build.props`'s `Update` matches the F# project's implicit
  FSharp.Core reference).
- C# projects that need to interop with F# types: `<PackageReference Include="FSharp.Core" VersionOverride="10.0.103" />` (uses `Include` because C# projects have no implicit FSharp.Core
  reference to `Update`). The `VersionOverride` still gets translated by `Directory.Build.targets`
  into a winning `Version`. Lesson from S15b (codemem 1895).

### Step 4 — Build + run + capture per-shape PASS/FAIL

- `dotnet build Meta/<spike-name>/Host/<spike-name>.fsproj -c Debug` — confirm green.
- `dotnet Meta/<spike-name>/Host/bin/Debug/net10.0/<spike-name>.dll` — capture full output.
- If a shape fails, capture the verbatim error message. Orleans serializer diagnostics usually
  say "No codec found for type ..." or "Could not find an implementation for interface ...".
- **Before declaring the spike PASS, run the spike's grain impls through an actual grain
  round-trip (invoke a grain method via the cluster client, not just construct the grain).**
  S15b's spike PASSED its 7/7 serializer round-trip but never invoked a grain method, so it
  never exercised the source-generator invoker path that broke in production. The spike's
  PASS bar must match production's actual call path. Lesson from S15b-production-port.
- Iterate: try the documented workaround first (cite the upstream issue/sample for each fix).
  Only invent a new workaround if the documented ones fail; mark that as a "genuinely novel"
  finding.
- Do NOT propagate fixes back to production code. The spike is throwaway. Production fixes happen
  in a follow-up spike (e.g. S15 -> S15b).

### Step 5 — Write the catalog doc

Template: `scripts/spike-catalog-template.md`. Render at
`AppEggShellGallery/public-dev/docs/modernization/spikes/<spike-name>.md`. Sections:

1. **Branch + spike goal** (one paragraph; cite the master plan section).
2. **Setup** — project layout, package refs, any non-obvious config (`GenerateCodeForDeclaringAssembly`,
   `AllowAllTypes`, `VersionOverride` etc).
3. **Critical findings** — each finding with: verbatim error/symptom, root cause, fix, upstream
   citation (issue number / sample URL / API doc URL / package XML).
4. **Per-shape result table** — shape × annotation × result × note.
5. **Decision** — answer to the spike's gating question (e.g. "delete custom serializer" vs
   "rewrite as `IFieldCodec`"). Cite which findings drove the decision.
6. **Open questions / surprises** — including any process miss (e.g. "websearch not done first")
   with a citation to the codemem entry that captures the rule.
7. **Next spikes (worklist)** — gated follow-ups, with the gating reason explicit (e.g. "S1 is
   gated by S15b because LibLifeCycleCore does not compile under Orleans 10 yet").

### Step 6 — Mirror to engineering log + codemem

- Append a session entry to `AppEggShellGallery/public-dev/docs/knowledge-base/engineering-log.md`
  (newest at top). Format:

  ```
  ## YYYY-MM-DD (session N -- <spike-name> <one-line summary>)

  <spike goal, one paragraph>
  <results, one paragraph>
  <key findings, bulleted with upstream citations>
  Decision: <one paragraph>
  Catalog: modernization/spikes/<spike-name>.md.
  Next: <next-spike name + gating reason>.
  ```

- Run the **codemem-update** skill (`codemem_memory_remember`) with: kind=`discovery` (or
  `decision`/`change`/`bugfix` as fits), title symptom-first (≤120 chars), body = symptom → root
  cause → fix → verification → non-obvious technique + upstream citations. Confidence 0.95 if
  verified by build, 0.7 if inferred.

### Step 7 — Commit (single coherent changeset)

Stage ONLY the spike's files:

- `Meta/<spike-name>/` (entire new directory: Types/, Codegen/, Host/, bin/obj ignored).
- `AppEggShellGallery/public-dev/docs/modernization/spikes/<spike-name>.md`
- `AppEggShellGallery/public-dev/docs/knowledge-base/engineering-log.md` (the new session entry)

Commit message subject ≤50 chars, Conventional Commits style. Body explains the why + points at
the catalog doc + cites upstream issue numbers. Example:

```
S15 spike: F# Orleans 10 serializer via C# codegen host

Throwaway 3-project spike (Meta/S15SerializerRoundtrip/) proves the F# +
Orleans 10 interop question the master plan gated on. Result: S15 FAILS
the no-regression bar -- 1 of 10 shapes round-trips. The 7 critical
findings are cataloged in spikes/s15-serializer-roundtrip.md. 6 of 7
were upstream-documented (dotnet/orleans #8520, #8717, SO Q77159202,
official API docs); finding #7 is genuinely novel.
```

Push the branch (do not squash-merge — squash-merge breaks rebase of pre-merge branch tips;
codemem 1888). The branch stays open for follow-up work.

## Reusing a spike pattern in production (semi-spike)

When the master plan calls for "apply the spike pattern to production code" (e.g. the
S15b-production-port work item: apply the S15b spike's serializer + codegen-host pattern to
the real `LibLifeCycleCore` + `LibLifeCycleHost`), this is NOT a green-field spike — it is a
semi-spike that reuses the spike's proven pattern. Before starting the production port:

1. **Re-run step 1 (upstream issue search) with the production code's specific F# construct
   shapes as search keywords.** The original spike searched for the serializer/codec symptom;
   the production port introduces NEW symptoms (F# closure classes, object-expression closure
   classes, constraint-bearing generic interfaces) that the original spike never searched for.
   S15b-production-port's HARD BLOCKER (F# closure misclassification by the Orleans C# source
   generator) had NO upstream report but would have been narrowed faster by searching
   `gh issue list --repo dotnet/orleans --search "F# closure source generator"` and
   `--search "F# object expression codegen"` BEFORE the 30-file production rewrite.

2. **Inventory the production code's F# constructs BEFORE the spike.** `grep` production for:
   - `backgroundTask {`, `task {` — CEs that emit closure classes,
   - `{ new I.*GrainObserver`, `{ new I.*Observer` — object expressions on grain-observer
     interfaces,
   - `when '.* :>` — constraint-bearing generic interfaces (F# 10 FS0909 cascade risk),
   - `IGrainActivationContext`, `IGrainReferenceConverter`, `IExternalSerializer` — APIs the
     version jump may have dropped.

   Cross-check each construct against the spike's coverage. If the spike did not exercise a
   construct the production code uses, the spike is INCOMPLETE — extend it before the port.

3. **Treat the spike's PASS bar as the production's MINIMUM bar, not the final bar.** The
   production port may hit constructs the spike never tested. Budget time for a follow-up
   spike (e.g. S15c) when the port surfaces a novel construct-related blocker. Do NOT assume
   the spike's PASS means the port will be smooth.

4. **F# 10 FS0909 cascade check.** F# 10 (`FSharp.Core` 10.0.103) tightened the
   `This constructor is provided for FSharp.Core only` constraint. Generic interfaces with
   `when 'T :> Foo` constraints (e.g. `IConnectorGrain<'Request, 'Env when 'Request :> Request>`)
   may trigger FS0909 when implemented via `interface ... with` blocks. If production uses
   constraint-bearing generic grain interfaces, search for FS0909 in F# compiler release notes
   BEFORE the port. Lesson from S15b-production-port: `ConnectorGrain.fs`'s
   `interface IConnectorGrain<...> with` block had to be commented out, unwiring the connector
   grain at runtime.

## Validation gates (before claiming done)

1. `dotnet build Meta/<spike-name>/Host/<spike-name>.fsproj -c Debug` — green.
2. `dotnet run --project Meta/<spike-name>/Host/<spike-name>.fsproj -c Debug` — prints per-shape
   PASS/FAIL, exit code 0 (all pass) or 1 (any fail). Capture full output verbatim.
3. **The spike's grain impls + host code use the SAME F# constructs production uses**
   (`backgroundTask { }` CEs, F# object expressions on grain-observer interfaces,
   constraint-bearing generic interfaces). If production uses a construct the spike does not
   exercise, the spike is INCOMPLETE — extend it before claiming PASS. Lesson from
   S15b-production-port.
4. **At least one grain method is invoked via the cluster client** (not just grain
   construction). The spike's PASS bar must cover the source-gen invoker path, not only the
   serializer round-trip. Lesson from S15b-production-port.
5. Catalog doc exists at the right path, follows the template, has upstream citations per
   finding.
6. Engineering log has the new session entry at the top.
7. codemem has the new entry (run `codemem_memory_search query="<spike name>"` to verify).
8. Git commit message body points at the catalog doc.
9. If the spike discovered a process miss (e.g. upstream research not done first), the rule is
   added to CLAUDE.md (see rule 14 for the S15 lesson) OR codemem-only if the rule is
   spike-specific.
10. **If the work item is "apply the spike pattern to production" (semi-spike)**: the
    "Reusing a spike pattern in production" checklist (re-run upstream issue search with
    production-construct keywords + inventory production's F# constructs + FS0909 cascade
    check) was completed BEFORE the port began. If a novel construct-related blocker
    surfaced during the port, a follow-up spike (e.g. S15c) is opened with the blocker as
    its gating question.

## Doc refs

- `AppEggShellGallery/public-dev/docs/modernization/sql-server-to-postgres.md` — master spike
  plan, the canonical spike order (S0 → S10 → S15 → S1 → ...).
- `AppEggShellGallery/public-dev/docs/modernization/spikes/s10-orleans-bump-catalog.md` — format
  precedent for a TIER 0 catalog spike (break surface only, no fixes).
- `AppEggShellGallery/public-dev/docs/modernization/spikes/s15-serializer-roundtrip.md` — format
  precedent for a TIER 1 spike (per-shape PASS/FAIL + decision + upstream citations).
- `AppEggShellGallery/public-dev/docs/knowledge-base/engineering-log.md` — append-only log,
  newest at top.
- `CLAUDE.md` rule 14 — the upstream-research rule, distilled from S15.

## How to improve this skill (mandatory post-spike review)

At the end of every spike that used this skill, evaluate whether the skill should be updated
(CLAUDE.md rule 1's post-session skill review, applied to spikes):

- Did the spike hit a gotcha the skill doesn't mention? Add it as a finding-pattern entry.
- Did the spike discover a better upstream-research path (new repo, new sample, new search
  technique)? Add it to step 1.
- Did the spike discover a new project-layout pattern (e.g. a 4th subproject for surrogate
  codecs)? Update step 3.
- Did the spike reveal that one of the validation gates is insufficient? Strengthen it.
- If a spike completes without any new learning beyond what the skill already says, say so
  explicitly ("nothing generalizable; skill is current") and skip — do not pad the skill with
  one-off details.

Concrete update triggers from prior spikes:

| Trigger | Update |
|---|---|
| S10's FSharp.Core NU1605 downgrade | step 3 documents `VersionOverride` |
| S15's websearch-first miss | step 1 mandates `gh issue` before code; codemem 1893 |
| S15's `obj/` collision across sibling projects | step 3 mandates one-subdir-per-project |
| S15b's 4-project layout for F# host + F# grain impls | step 3 documents 4-project variant |
| S15b's two-attribute GenerateCodeForDeclaringAssembly | step 3 documents multi-attribute case |
| S15b's C# project uses `Include`, F# uses `Update` for FSharp.Core | step 3 documents the split |
| Codemem 1888 (squash-merge breaks rebase) | step 7 warns against squash-merge of spike branches |
| Codemem 1851 (dev-web watch misses lib edits) | not a spike-specific gotcha — lives in runbooks only |
| S15b-production-port (codemem 1898): spike PASSED 7/7 with trivial impls but production port hit HARD BLOCKER late because spike never exercised `backgroundTask { }` CEs + F# object expressions (Orleans 10 C# source generator misclassifies F# closure classes as InterfaceImplementations) | step 3 mandates mirroring production's F# construct shapes; step 4 mandates a real grain round-trip; new "Reusing a spike pattern in production" section mandates re-running upstream issue search with production-specific construct keywords + inventorying production's F# constructs before the port |
| S15b-production-port (codemem 1898): F# 10 FS0909 on `interface IConnectorGrain<'Request, 'Env when 'Request :> Request> with` — constraint-bearing generic grain interface blocks implementation | "Reusing a spike pattern in production" section step 4 documents the FS0909 cascade risk |
| S15b-production-port (codemem 1898): spike's PASS bar was serializer round-trip only; production port needed invoker (method-dispatch) round-trip too — the source-gen invoker path is what broke | step 4 mandates an actual grain method invocation via the cluster client, not just grain construction |

When in doubt, add the learning to codemem (which is search-targetable) rather than padding this
skill. This skill stays a concise procedure; codemem carries the long tail.

## Doc-refs (anti-pattern: do not duplicate)

This skill does NOT duplicate:
- The master spike plan (`sql-server-to-postgres.md`) — it points at it.
- The catalog doc format precedent (existing `spikes/s*.md`) — it points at it.
- The codemem-update skill — it invokes it (step 6).
- The docs-sync skill — it complements it (catalog doc lives under the docs site).
- The runbooks — for dev-loop / device gotchas, use the runbook skill, not this one.

If this skill ever grows to >200 lines, split a helper script out.
