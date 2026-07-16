# Knowledge Base

This section collects durable engineering knowledge that does not fit neatly into either [Architecture](../architecture/index.md) (reference) or [Runbooks](../runbooks/index.md) (procedures).

## Pages in this section

| Page | What it contains |
|------|-----------------|
| [Engineering Log](./engineering-log.md) | The append-only chronological record of the modernization effort. Every significant discovery, build quirk, wrong assumption, and toolchain gotcha from the initiative is dated and logged here. This is the running memory of the work. |
| [App Structure](./app-structure.md) | The layered source-organization convention for `App*` projects: layers, dependency direction, `.fsproj` build order, styles-in-render prohibition, pure-F# component convention, and the small-app escape hatch. |
| [Dependencies](./dependencies.md) | A point-in-time npm audit snapshot across all workspaces, showing package counts and vulnerability counts by severity. |

## How this section relates to the others

**Architecture** (`../architecture/`) is the stable reference for the framework design: grains, lifecycles, shared types, the frontend model. It describes the system as it is.

**Runbooks** (`../runbooks/`) are procedural: step-by-step recipes for recurring operations (building, deploying, debugging). They answer "how do I do X right now."

**Knowledge Base** is the historical and contextual layer: why things are the way they are, what was tried and failed, and the conventions that have crystallized from working in the codebase. The Engineering Log in particular is the append-only record of the modernization initiative (Goals A-E: retiring the render DSL, fixing scaffolding, reducing verbosity, standardizing directory structure, speeding up builds).

### Distilled vs. historical

The Engineering Log is a **chronological narrative** of discoveries as they happened. When a log entry surfaces a durable "symptom to cause to fix" insight, that insight is also curated into [`../runbooks/troubleshooting.md`](../runbooks/troubleshooting.md). The log is the source of record; the troubleshooting page is the distilled reference. Do not duplicate entries across both: the log tells the story, the runbook gives the remedy.
