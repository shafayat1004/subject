# EggShell — Project Rules (Claude)

**Check available skills FIRST, before doing anything else** — before reading files, running
commands, or answering. Scan the skill list (project `.claude/skills/*` + any superpowers skills)
against the request; if one matches, invoke it before taking any other action. Do this on every
prompt, not just ones that "feel like" they need a skill.

ALWAYS use caveman style unless I explicitly say "normal mode".
After compaction, context reset, resume, or uncertainty, re-read:
~/.claude/skills/caveman/SKILL.md
Then continue in caveman mode.
Keep replies terse: no filler, no pleasantries, no long explanations unless requested.

**Project documentation now lives in the App Gallery docs site** at
`AppEggShellGallery/public-dev/docs/` (rendered in the running gallery). It is the single source of
truth for architecture, status, goals, runbooks, accessibility, and the engineering log. Paths below are
relative to that docs root unless stated otherwise.

Full architecture writeup: `architecture/` (Orleans backend, react-native-web frontend, state-machine
lifecycles, shared types, render DSL).

**Docs maintenance — read first.** When you change anything in the codebase (or the docs), follow
`maintaining-docs.md` (in the docs root): it is the single map of which docs to read/update for a given
change, plus how to add/wire a doc page and keep the docs internally consistent. It consolidates rules 1,
10, and 11 below.

## Current initiative

Executing goals A–E together (see `modernization/goals-and-roadmap.md`; status dashboard in
`modernization/index.md`):
- **A** Retire the render DSL; convert framework `.render` components to pure F#.
- **B** Fix scaffolding (`eggshell create-app` must produce a working, modern app).
- **C** Reduce frontend component verbosity.
- **D** Standardize frontend directory structure.
- **E** Speed up the frontend build.

**Toolchain (migrated):** the frontend is on **Fable 5** (`fable` tool 5.4.0, `Fable.Core` 5.0.0,
`Fable.React` 10.0.0-alpha.1), built with the **.NET 10 SDK** (`global.json` 10.0.301). Write Fable-5
code; do not target Fable 4. The UI stack is **React 19.2 + react-native 0.86 + react-native-web
0.21.2 with the New Architecture (Fabric) enabled**, RNGH 3. **ReactXP is fully retired** — the
primitive seam is `LibClient/src/Rn/` (namespace **`Rn`**, constructor prefix **`Rn.`** e.g.
`Rn.View`/`Rn.Text`, imports in `RnPrimitives.fs`). Do **not** write `ReactXP`/`RX.` in new code.
`.fs.js` are generated Fable output and are **git-ignored** (never commit them).

(Still LATER — do not start now: full **.NET 10 TFM migration** is NOT complete repo-wide and **no
.NET-10-specific features** have been adopted yet (the net10 SDK is the build host; many projects still
target older TFMs). The **Orleans/Postgres** workstream is also later. **Remaining upgrade tail:** the
gallery + PerformancePlayground still need the RN 0.86 native config that AppTodo now has, and
Reanimated 4 + Moti are not yet added. See `modernization/phased-plan.md` +
`modernization/reactxp-to-rnw.md` + `knowledge-base/engineering-log.md` (session 8) for status.)

## Working rules

1. **Maintain the Engineering Log** (`knowledge-base/engineering-log.md`). Whenever I discover something
   I initially got wrong / missed and then corrected (a build quirk, a convention, a wrong assumption, a
   gotcha in the toolchain), append a dated entry at the top. This is the running memory of the effort.
   When a learning distills to a durable symptom→fix, also add it to `runbooks/troubleshooting.md`.
   **MANDATORY: also write it to codemem** by invoking the **`codemem-update` skill** (claude-mem's
   background auto-capture is **retired**, so nothing lands in codemem unless the skill runs it). Run
   the skill at the end of any session that fixed a bug, made a decision, or learned a gotcha, and
   whenever you add an engineering-log entry; run its search step before nontrivial work. Pass raw
   `context` + a `prompt`; the skill hands them to the harness's cheapest background model (whatever
   the current platform exposes — not a hardcoded model) that summarizes *and* writes, so no
   expensive-model tokens are spent on the entry.
   **Post-session skill review (mandatory).** At the end of any debugging, feature-implementation, or
   code-understanding session, evaluate whether a project skill (`.claude/skills/*/SKILL.md` or its
   `scripts/`) should be updated — not just the docs (docs-sync handles docs). A repeatable procedure, a
   reusable script, or a gotcha that will recur belongs in the matching skill so the next session starts
   with it in context instead of rediscovering it. Concrete triggers: you wrote a throwaway script that
   would be useful next time → promote it to `scripts/`; you followed a multi-step triage/verification
   procedure that isn't written anywhere → add it to the SKILL.md; you hit a dev-loop/build gotcha that
   the skill doesn't mention → add it to the skill's gotchas section (and the runbook). If nothing is
   generalizable, say so and skip — do not pad skills with one-off details.
2. **Thorough + efficient code.** Match surrounding conventions (naming, file layout, comment density).
   Prefer the idiomatic existing pattern over inventing a new one.
3. **Reuse, don't duplicate.** Factor shared logic into one place. If I'm about to copy-paste, stop and
   extract. Build reusable tooling for repetitive conversions rather than hand-editing N files.
4. **Validate the build.** Before calling any change done, build the affected framework project(s) and
   confirm green. See "Build & validate" below for exact commands. Report failures with output; never
   claim done on an unverified change.
5. **Consult Fable docs regularly** (we are on **Fable 5**; `Fable.React` 10.x). Don't guess
   Fable/Fable.React API — look it up. Local clone (if present): see `knowledge-base/engineering-log.md`
   for path; otherwise fetch from https://fable.io/docs.
6. **Render-DSL conversion must be drop-in.** Converted F# must be semantically identical to what the
   `.render` compiled to. When in doubt, diff the generated `.Render.fs` against the new F#.
7. **Do not edit `.render` for feature work — convert to pure F# instead.** Goal A is retiring the
   render DSL. When changing a component (a11y, Pressable migration, testIds, behavior), **never**
   patch `.render` or hand-edit `_autogenerated_/.../*.Render.fs`. **Never copy autogenerated
   `.Render.fs` into a hand-written file** — that preserves legacy `findApplicableStyles` soup and
   is not a conversion. Follow the `[<Component>]` recipe in `modernization/render-dsl-retirement.md`
   (see Tabs, Tab, TextButton, HandheldListItem): port `.styles.fs` to
   `makeViewStyles`/`makeTextStyles`, write `type LC with [<Component>] static member Foo(...)`, bridge
   `?xLegacyStyles` if needed, migrate `DefaultComponentsTheme` to `Themes.Set`, delete
   `.render`/`.typext.fs`/autogenerated Render.
8. **Framework only.** Touch `Lib*`, `LibUi*`, `LibRouter`, `LibAutoUi`, `LibLifeCycleUi`, `ThirdParty`,
   `Meta/*`. Do **not** modify `App*` or `Suite*` (those are applications) unless explicitly asked.
   **Exception:** app-specific theming, copy, and composition — but **behavior and reusable layout belong in
   LibClient** (see rule 13).
9. **Org rules apply.** No banned NuGet packages (Moq, AutoMapper). No em-dash in prose. Use the
   efficient model unless a task needs heavy reasoning.
10. **Gallery mirrors converted components.** Whenever a LibClient component is converted from render DSL
   to pure F#, its corresponding `AppEggShellGallery/src/Components/Content/` page must be updated to
   use the new F# API and be written in pure F# (not render DSL). If no gallery page exists for the
   component, add one. No new render DSL in the gallery; convert existing pages when touching them.
11. **Check the runbooks before repeated work.** Before any dev-loop / device-debugging / build /
   observe task — running, launching, screenshotting, tapping/rotating a device, reading runtime
   errors, killing stale watches, targeted rebuilds, "did my patch reach the bundle" — read
   `runbooks/` (start at `runbooks/index.md`) and follow its commands + decision tree instead of
   improvising. Also consult `knowledge-base/engineering-log.md` (newest first), `accessibility/`,
   `knowledge-base/app-structure.md`, `modernization/`, and `SuiteTodo/AppTodo/README.md` per
   `.cursor/rules/runbooks-first.mdc`. When you hit a new gotcha or a runbook step is wrong, fix the doc
   in the same change.
12. **Accessible design is the default and is mandatory — do not be lazy.** Every UI change ships
   accessible by default across the full spectrum (screen readers, low vision / text scaling, color &
   contrast, motor / target size & gesture alternatives, hearing, cognitive, motion / reduce-motion,
   seizure), per `accessibility/` (read `accessibility/index.md` "pit of success" principles and
   `accessibility/recipes.md`). Accessibility is not optional, not a follow-up, and not "good enough
   without it." Concretely: every interactive element exposes name + role + state; decorative icons are
   hidden; text scales without clipping; colors meet WCAG AA and are never the sole signal; targets are
   ≥44px; any gesture has a non-gesture alternative; dynamic changes announce via a live region. Prefer
   baking semantics into the primitive over per-call patching. If a piece is genuinely
   `[rnw-blocked]`/`[web-only]`, say so and use the portable subset — never silently skip a11y.
13. **Framework-first UI.** Before any UI component task, decide framework vs app (see
   `.cursor/rules/framework-ui-first.mdc`). Extend `LibClient` / `LibRouter` for reusable behavior
   (segmented controls, picker/dialog lifecycle, gestures, a11y primitives); keep apps to palette,
   theme overrides, and domain composition. Add a gallery page for new/changed `LibClient` components.
14. **Read upstream issues before building third-party-ecosystem spikes.** Before writing any spike
   code that tests a third-party ecosystem question (Orleans, ASP.NET, Fable, Npgsql, React Native,
   Postgres, etc.), read the upstream repo's issues + the official sample README + the API docs.
   Most ecosystem gotchas have already been hit and documented by the maintainers or by outside
   teams migrating. Use `gh issue` (NOT webfetch — it hits an auth wall on GitHub search and lacks
   comments):
   ```
   # List recent issues matching a symptom
   gh issue list --repo OWNER/REPO --search "<symptom keywords>" --state all \
     --limit 30 --json number,title,url

   # Read one issue with comments (accepts the URL directly too)
   gh issue view 8717 --repo dotnet/orleans --comments
   gh issue view https://github.com/dotnet/orleans/issues/8520 --comments

   # Structured JSON for catalog docs
   gh issue view 8520 -R dotnet/orleans --json title,body,comments --jq '
     "# " + .title + "\n\n" + .body + "\n\n## Comments\n" +
     ([.comments[].body] | join("\n\n---\n\n"))'
   ```
   `-R` works from inside any repo (no `cd` needed). For private repos verify `gh auth status`
   first. Then read the official sample's README + source and the API docs for any option mentioned.
   Only after those return no answer, build the spike. Cite the upstream issue numbers in the
   spike catalog doc. Lesson from S15 (codemem 1893): 6 of 7 findings were upstream-documented
   (dotnet/orleans #8520, #8717, SO Q77159202, official API docs); 4-5h of iteration could have
   been ~1h if the issues had been read first.

## Component conventions (frontend)

- `.render` component = `Foo/Foo.typext.fs` (Props + component class + `Make`) + `Foo/Foo.styles.fs`
  + generated `_autogenerated_/.../Foo.Render.fs` + registration in `ComponentRegistration.fs`.
- Modern pure-F# component (target) = single `Foo.fs` using `type LC with [<Component>] static member`
  (see `Tabs.fs`, `Heading.fs`, `HandheldListItem.fs`). **No** `.typext.fs`, **no** copying
  `_autogenerated_/.../Foo.Render.fs`. Public types live in the same file (often under
  `module LC = module Foo`, or `namespace ... module Foo` for nested paths like `Nav.Top.Item`).
- **When touching a `.render` component:** convert to pure F# per the recipe in
  `modernization/render-dsl-retirement.md`; delete `.render`, `.typext.fs`, and autogenerated
  Render/TypeExtensions together.
- **Styles (forward guidance):** in *new* files prefer a top-level `let foo = makeViewStyles {...}` or a
  named `[<RequireQualifiedAccess>] module private FooStyles =`. ~98 converted files (incl. `Tabs.fs`)
  use a generic `module private Styles =`; those are not being mass-migrated, and matching the existing
  `module private Styles =` when editing one is fine. See `fsharp/styling.md`.
- **No style leaks (hard rule).** Never let a `makeViewStyles`/`makeTextStyles` run inside render: no
  inline `styles = [| makeViewStyles {...} |]`, none inside a component body / `Pointer.State` /
  `With.ScreenSize` callback. Static styles → top-level `let`. Parametrized styles → `ViewStyles.Memoize`
  / `TextStyles.Memoize`, **keyed on primitives** (Color/int/bool/small DU), *never* on whole
  `Theme`/`Colors` records (fresh record refs defeat the cache). A memoized lambda param must not share a
  name with a CE operation (`height`, `color`, `fontSize`, `top`, `left`, `bottom`) or it won't compile —
  rename it (`itemHeight`, `labelColor`, ...). Full rationale + recipe in `fsharp/styling.md`
  ("Avoiding style leaks") and `runbooks/troubleshooting.md`.
- **React key warnings.** When passing a *static array* of sibling elements as a component's children,
  route it through `castAsElementAckingKeysWarning` / `tellReactArrayKeysAreOkay` (both run
  `React.Children.toArray`, which injects stable keys) instead of a bare `[| ... |]` / `castAsElement`.
  See `modernization/render-dsl-retirement.md` (frontend render hygiene) and
  `runbooks/troubleshooting.md`.
- `.fsproj` compile order matters: a component's source files precede its `_autogenerated_` files.
- The `RenderConvert` compiler mode emits readable F# from a `.render` (cleaner than the build-time
  `.Render.fs`). `eggshell convert-component` runs it but currently only prints to stdout.

## F# formatting (mandatory, every model, every edit)

Canonical reference: `AppEggShellGallery/public-dev/docs/fsharp/formatting.md`. Read it before any
non-trivial F# edit. Hard rules (rule number in parens):
- 4-space indent, never tabs (1). Soft line limit ~120 chars (3).
- Column alignment is applied by `eggshell-fmt` (the sole formatter; Fantomas is retired). Match
  surrounding code exactly: record-field `:` (2a), DU `of` (2b), short match-arm `->` (2c),
  let-binding `=` groups (2d), record-construction `=` (7).
- `match x with` on one line (5). DU cases one per line, labeled fields `Name: Type` (8).
- After editing F#, run `dotnet tool run eggshell-fmt -- <file.fs>` (pinned; install once per machine
  via `Meta/EggShellFmt/install.sh`). It normalizes whitespace and column alignment but does not
  reflow long lines or fix operator spacing inside expressions, so keep those canonical by hand.
- **NEVER run `dotnet fable` directly.** It emits `.fs.js` beside the source files. Build only via
  `./eggshell build-lib` / `eggshell dev-web` / `eggshell dev-native`; output belongs under
  `.build/<platform>/`. If stray `.fs.js` files appear beside `.fs` sources, run
  `.claude/skills/fable-rebuild-verify/scripts/clean-stray-fable-output.sh`.

## Project skills

Procedure skills live in `.claude/skills/` and wrap the runbooks (rule 11): `fable-rebuild-verify`,
`debug-android`, `debug-ios`, `debug-web`, `fsharp-format`, `style-leak-audit`, `a11y-check`, `docs-sync`,
`gallery-page-add`, `release-build`, `verify-feature`, `screenshot-describe`, `codemem-update`. Prefer
invoking the matching skill over improvising commands.

## Build & validate

(Commands confirmed during baseline — update here + in `knowledge-base/engineering-log.md` as learned.)
- Frontend lib type-check / build: `./eggshell build-lib` from the lib dir (compiles `.render` +
  Fable). Backend libs: `dotnet build`.
- Framework libs use custom configs (`Web Debug`, `Web Release`, `Native Debug`, `Native Release`),
  not plain `Debug` — pass `-c "Web Debug"` to `dotnet build` where relevant.
- **No stale-cache false greens.** Fable may print `Skipped compilation because all generated files
  are up-to-date!` and exit 0 without type-checking your edits. Before claiming done: force
  recompile (`touch` changed `.fs`, or clear `.build/<platform>/fable`), confirm
  `Started Fable compilation...` and `rg "error FS"` is clean. If the user has watch/dev running,
  their watch terminal beats a separate cached build.
- **Check build output promptly** (30–45s waits; read terminal files early). Do not block 120s+
  and miss errors already printed.
