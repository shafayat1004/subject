# Keeping Code and Docs in Sync

**This is the single source of truth for documentation maintenance.** It tells both people and LLM
agents: when you change something in the codebase, which docs to read and update, and how to keep the
docs themselves consistent. If you touch the repo, read this first, and update the relevant docs **in the
same change**.

## Golden rules

1. **Docs live in the gallery.** The canonical documentation tree is
   `AppEggShellGallery/public-dev/docs/`. It is rendered inside the running gallery (served at `/docs/`)
   and packaged into builds via `AppEggShellGallery/eggshell.json` (`copyStaticFiles`). There is no other
   docs tree (`Meta/Docs/` was removed). Edit docs here. On **web** the gallery fetches these `.md` over
   HTTP; on **native** there is no server, so they are inlined into `src/DocsContent.generated.js` by
   `scripts/gen-docs-bundle.js`, wired into `metro.config.js` to regenerate on every native bundle — you
   do not run it by hand for a dev reload. (Manual, if ever needed: `node scripts/gen-docs-bundle.js`.)
2. **Update docs in the same commit as the code.** A behavior, API, convention, gotcha, status, or
   dependency change is not "done" until its docs are updated. Never leave docs describing the old world.
3. **The Engineering Log is append-only.** Every discovery, wrong assumption you corrected, build quirk,
   or toolchain gotcha gets a dated entry at the top of
   [knowledge-base/engineering-log.md](./knowledge-base/engineering-log.md). When it distills into a
   durable symptom-to-fix, also add it to [runbooks/troubleshooting.md](./runbooks/troubleshooting.md).
4. **Keep the status pages honest.** [modernization/index.md](./modernization/index.md) is the
   authoritative status dashboard; it must agree with
   [goals-and-roadmap.md](./modernization/goals-and-roadmap.md) and
   [phased-plan.md](./modernization/phased-plan.md), and with reality (verify `global.json`, the `fable`
   tool version, and `package.json` before asserting toolchain state).

## When you change X, update these docs

| You changed... | Read / update |
|---|---|
| A backend lifecycle, grain, view, timeseries, connector, or the Orleans/hosting layer | [architecture/backend-lifecycles.md](./architecture/backend-lifecycles.md), [architecture/backend-hosting-persistence.md](./architecture/backend-hosting-persistence.md); developer guide under [subject/](./subject/index.md) |
| Storage (SQL Server / Postgres seams), clustering, migrations | [architecture/backend-hosting-persistence.md](./architecture/backend-hosting-persistence.md); if it advances the Orleans/Postgres workstream, [modernization/goals-and-roadmap.md](./modernization/goals-and-roadmap.md) (Goal G) + [modernization/index.md](./modernization/index.md) |
| Codecs, the wire format, codec validation | [architecture/shared-types-codecs.md](./architecture/shared-types-codecs.md) |
| The test harness (`simulation`, clock, connectors) | [architecture/testing-framework.md](./architecture/testing-framework.md), [subject/testing.md](./subject/testing.md) |
| A frontend primitive / the `LibClient/src/Rn` (RNW) seam / react-native-web wiring | [architecture/frontend.md](./architecture/frontend.md), [modernization/reactxp-to-rnw.md](./modernization/reactxp-to-rnw.md); update the migration status in [modernization/index.md](./modernization/index.md) + [phased-plan.md](./modernization/phased-plan.md) |
| F# interop invariants | [fsharp/interop-invariants.md](./fsharp/interop-invariants.md) |
| A **LibClient/LibRouter component** (new, converted, or behavior/a11y change) | Its **gallery Content page** under `AppEggShellGallery/src/Components/Content/` (pure F#, per CLAUDE.md rule 10); [fsharp/component.md](./fsharp/component.md) if the authoring pattern changed; [accessibility/recipes.md](./accessibility/recipes.md) if a11y semantics changed |
| Styling DSL, theming, style-leak rules | [fsharp/styling.md](./fsharp/styling.md), [fsharp/themeing.md](./fsharp/themeing.md) |
| Accessibility behavior, OS-setting handling, contrast, backlog items | [accessibility/](./accessibility/index.md) (index principles, spectrum, recipes, platform-settings, backlog) |
| The `eggshell` CLI, `eggshell.json` schema, scaffolding templates | [tools/cli.md](./tools/cli.md); scaffolding status in [modernization/scaffolding.md](./modernization/scaffolding.md); render/toolchain in [architecture/render-dsl-and-toolchain.md](./architecture/render-dsl-and-toolchain.md) |
| Build pipeline / speed (Fable, webpack, precompile) | [runbooks/build-rebuild.md](./runbooks/build-rebuild.md), [modernization/build-performance.md](./modernization/build-performance.md) |
| The dev loop, device debugging, or you hit a new gotcha | [runbooks/](./runbooks/index.md) (the relevant platform page + [troubleshooting.md](./runbooks/troubleshooting.md)); log it in [knowledge-base/engineering-log.md](./knowledge-base/engineering-log.md) |
| App source layout / `.fsproj` ordering / layering | [knowledge-base/app-structure.md](./knowledge-base/app-structure.md) |
| npm / NuGet dependencies | [knowledge-base/dependencies.md](./knowledge-base/dependencies.md) (regenerate via `./initialize`) |
| Toolchain versions (Fable, .NET SDK, React, Orleans) | [architecture/index.md](./architecture/index.md) tech baseline **and** [modernization/index.md](./modernization/index.md) toolchain table (keep both in sync) |
| Security-relevant surfaces (auth, SQL identifiers, crypto, CORS, cookies) | [modernization/security-review.md](./modernization/security-review.md) |
| A goal's or phase's status changed | [modernization/index.md](./modernization/index.md), [goals-and-roadmap.md](./modernization/goals-and-roadmap.md), [phased-plan.md](./modernization/phased-plan.md) |
| You added, renamed, moved, or deleted a **doc page** | See "Adding or changing a doc page" below, and **update [llms.txt](./llms.txt)** |

Agent-facing rules that point here: `CLAUDE.md` (rules 1, 10, 11), `.cursor/rules/runbooks-first.mdc`,
`.cursor/rules/accessibility-default.mdc`.

## Voice and content

Gallery docs are **as-is technical documentation**: they describe the system, a design, or a plan as it
stands, in the present tense, for a reader who was not in the room. They are not a work journal, a
changelog, or a record of how a page came to exist.

- **No meta-status about the document.** Never write about the doc itself ("this page is planning only",
  "no code has been changed as part of writing this", "this is a draft", "written so it can be picked up
  later"). A doc simply exists; that it exists is not content. Factual status about the *subject* is fine
  and encouraged (a workstream row that reads "Status: not started", a component marked "web-only").
- **No process narration.** Do not describe how or why the doc was produced, that you were asked to write
  it, what prompt or session produced it, what you searched, or what inputs you "grounded" it in. State
  the facts and recommendations directly; cite sources with links, not with an authorship story.
- **No transient or private references.** Do not point at scratch investigations, local-only repos or
  branches, chat context, or another team's internal paths. A doc must stand on its own for any reader.
  Durable cross-links to other docs and public upstream URLs are the way to reference things.
- **Present-tense, declarative.** "The backend persists through Orleans." not "I found that the backend
  persists through Orleans." Recommendations are stated as the approach ("the explicit version column is
  preferred"), not as a personal note.

The append-only [engineering log](./knowledge-base/engineering-log.md) is the one place process and
"how we learned X" belongs; keep it out of the technical pages.

## Keeping the docs internally consistent

When you edit docs (not just code), check these invariants:

- **No stale framing.** The frontend runs on react-native-web, the toolchain is Fable 5 / .NET 10 SDK,
  and the render DSL is retired. Do not reintroduce present-tense "ReactXP is the current primitive",
  "Fable 4", ".NET 7", or "render DSL is how you author" claims (historical/legacy references are fine).
- **Status pages agree** with each other and with the repo (see Golden rule 4).
- **`llms.txt` lists every section and page.** If you add/rename/remove a page, update
  [llms.txt](./llms.txt) so the machine index stays complete.
- **Links resolve** (see verification below).

## Adding or changing a doc page

Docs are plain Markdown; the first `# H1` is the page title. **No YAML frontmatter.**

- **Links are true relative paths**, resolved against the linking file's own folder, exactly like
  GitHub (and any standard markdown renderer) resolves them. From `modernization/index.md`, link a
  sibling as `./other.md`, a page under a different top-level section as `../architecture/frontend.md`,
  and a page in the same section as a bare `sibling.md` or `./sibling.md` (equivalent). Never write a
  root-relative `./section/page.md` from a non-root file; it renders fine in the gallery but 404s on
  github.com. The in-app link handler (`Components/App/App.fs` `globalMarkdownLinkHandler`, backed by
  `AppEggShellGallery/src/RenderHelpers.fs` `resolveRelativeDocPath`) resolves the href against the
  current page's folder and then routes the result into `architecture/`, `modernization/`,
  `runbooks/`, `accessibility/`, `knowledge-base/`, `tools/`, `how-to/`, `subject/` sections by
  prefix; everything else renders through the generic Docs route. A leading `/` (e.g. `/llms.txt`) is
  docs-root-relative, an escape hatch for links that must not shift when a file moves.
- **Tables** use GFM pipe syntax; they render because Showdown has `tables` enabled
  (`AppEggShellGallery/src/RenderHelpers.fs`) and the markdown container carries the `markdown-body`
  class styled in `AppEggShellGallery/public-dev/app.css`.
- **Prose style:** no em-dashes (use commas, colons, or parentheses); no banned packages referenced as
  recommended.

**To add a page in an existing section:** create the `.md`, then register it in
`AppEggShellGallery/src/Components/Sidebar/SidebarContent.fs` in that section's `*Items()` function (flat
array literal, keep the depth-1 rule noted in the file header), and add it to `llms.txt`.

**To add a whole new top-level section:** wire all of these (mirrors the existing Subject/Tools pattern):
1. `src/Navigation.fs` — add an `ActualRoute` DU case (`of MarkdownUrl: string`) and a URL spec entry.
2. `src/Components/App/App.fs` — a `routeContent` arm (reuse `Ui.Route.Docs`) and a link-handler prefix.
3. `src/Components/Sidebar/Sidebar.fs` — a `fixedTopBlades` blade and a `routeSidebar` arm.
4. `src/Components/Sidebar/SidebarContent.fs` — a new `*Items()` function.
5. `src/Components/TopNav/TopNav.fs` — a `desktopHeading` case and a nav `LC.Nav.Top.Item`.
6. Add the section and its pages to `llms.txt`.

## Verification

- **Build green:** run the gallery (`cd AppEggShellGallery && ../eggshell dev-web`, port 8082); confirm
  `Started Fable compilation`, `webpack ... compiled successfully`, and zero `error FS`. Framework
  (`Lib*`, `ThirdParty`) edits live in the precompiled lib, so clear `LibStandard/.build/web/fable` and
  restart dev-web to see them (see [runbooks/build-rebuild.md](./runbooks/build-rebuild.md)).
- **No dead links:** every `./…\.md` (and `.txt`) target must exist. Quick check from the docs root:

  ```bash
  cd AppEggShellGallery/public-dev/docs
  python3 - <<'PY'
  import os, re, glob
  existing = {os.path.normpath(p) for p in glob.glob("**/*", recursive=True) if os.path.isfile(p)}
  rx = re.compile(r'\]\((\.?/?[^)\s]+?\.(?:md|txt))(#[^)]*)?\)')
  bad = []
  for f in glob.glob("**/*.md", recursive=True) + ["llms.txt"]:
      for i, line in enumerate(open(f, encoding="utf-8"), 1):
          for m in rx.finditer(line):
              raw = m.group(1)
              if raw.startswith("http"): continue
              t = raw[2:] if raw.startswith("./") else raw.lstrip("/")
              if os.path.normpath(t) not in existing: bad.append((f, i, raw))
  print("NO BROKEN LINKS" if not bad else "\n".join(f"{f}:{i} {r}" for f, i, r in bad))
  PY
  ```

- **Nav works:** the sidebar lists the page and clicking it loads the markdown (screenshot or `curl`
  `http://127.0.0.1:8082/docs/<path>`).
