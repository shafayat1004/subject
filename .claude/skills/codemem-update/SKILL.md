---
name: codemem-update
description: Persist a session learning (bugfix, decision, gotcha, milestone) to the codemem MCP store. Use at the end of any session that fixed a bug, made a decision, or learned a gotcha, and whenever you add an engineering-log entry. claude-mem auto-capture is retired, so nothing lands in codemem unless you run this. Also use to search codemem for prior context before nontrivial work.
user-invocable: true
argument-hint: "[search <query>] | context=<raw notes/diff/facts> prompt=<how to summarize>"
---

# codemem-update

claude-mem's background auto-capture is **retired**. codemem does **not** auto-tail sessions, so a
learning is lost unless it is written explicitly. This skill is the single procedure for that.

## When to run

- **End of any session** that fixed a bug, made a decision, or learned a gotcha.
- **Whenever you add an entry** to `knowledge-base/engineering-log.md` (mirror the same learning here).
- **Before nontrivial work** run the search step for prior context.

## Cost model (mandatory)

The strong caller model does **not** write the entry and does **not** even summarize it. The caller
only gathers two parameters and hands them to a **cheaper background worker**, which does the
summarization **and** the tool call. This keeps expensive-model tokens near zero for memory upkeep.

**Use the cheapest model your harness offers — do not hardcode a model name.** Resolve the worker in
whatever way your platform provides:

- **Claude Code:** spawn a background subagent via the Agent tool, setting `model` to the cheapest
  available tier (e.g. `haiku`), `run_in_background: true`.
- **OpenCode (or any other harness):** dispatch to whatever cheap/fast model or background
  subagent/task mechanism it is configured with (its "small"/"cheap" model slot). If the harness has
  no delegation primitive, fall back to switching this session to its cheapest model for the write.

**Parameters the caller provides:**

- `context` — the raw material to remember: paste the actual facts (symptom, root cause, files +
  exact change, verification, non-obvious technique). Raw and unpolished is fine; do not pre-summarize.
- `prompt` — a short instruction on how to summarize / what to emphasize (e.g. "symptom-first bugfix
  entry, one fact per memory, split if two distinct learnings", or "record the decision + rationale").

## Write procedure (caller)

Hand the two parameters to the cheap background worker (see Cost model for how to resolve it on your
harness), passing the WRITER PROMPT below with `{context}` and `{prompt}` filled in. Fire it in the
background and move on; do not wait on strong-model time.

### WRITER PROMPT (given to the cheap worker)

> You are writing a durable memory to the codemem MCP store. Summarize the CONTEXT per the
> INSTRUCTION, then call the codemem tool `memory_remember` (search for codemem tools first if they
> are not already loaded).
>
> Fields: `project` = "subject"; `kind` = one of
> bugfix|decision|discovery|change|feature|refactor|exploration (infer from the context);
> `title` = one line, symptom-first for bugs; `body` = symptom -> root cause -> fix (files + exact
> change) -> verification, plus any non-obvious technique; `confidence` = 0.9 for a verified fix, 0.5
> for a hypothesis. One fact per memory — if the context holds two distinct learnings, call
> `memory_remember` once per learning. First run `memory_search` on the key symptom/component; if a
> near-identical entry exists, prefer enriching it over adding a duplicate. Report the new id(s).
>
> INSTRUCTION: {prompt}
>
> CONTEXT:
> {context}

If you are already running on the harness's cheapest model, skip the delegation and follow the WRITER
PROMPT directly.

## Search procedure

Before nontrivial work: `memory_search` with symptom keywords / component names, or `memory_recent`
for a newest-first sweep. Reading is cheap — do it inline on whatever model you're on; no worker.

## Don't

- Don't rely on any background observer (claude-mem is gone).
- Don't burn strong-model tokens summarizing — that is the cheap worker's job; hand it raw context.
- Don't hardcode a model name; use whatever cheapest tier the current harness exposes.
- Don't record what the repo already captures (code structure, git history) — record the
  non-obvious symptom->fix, decision rationale, or gotcha.

## Doc refs

- CLAUDE.md working rule 1 (engineering log + mandatory codemem write).
- knowledge-base/engineering-log.md (the human-readable running log this mirrors).
