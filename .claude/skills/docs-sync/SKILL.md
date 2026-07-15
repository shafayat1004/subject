---
name: docs-sync
description: Keep the gallery docs site in sync after any code or doc change. Use before every commit that changes behavior, components, build, tooling, or docs. Routes the change type to the docs that must be updated, appends the engineering log, and validates links and status-page agreement.
user-invocable: true
argument-hint: "[change summary]"
---

# docs-sync

Docs root: `AppEggShellGallery/public-dev/docs/`. Golden rules (maintaining-docs.md): update docs
in the SAME commit as code; engineering log is append-only, newest at top; status pages must agree.

## Routing (change type -> docs to update)

- Backend lifecycle/grain/view -> architecture/backend-lifecycles.md, subject/ guide
- Frontend primitive / LibClient/src/Rn seam -> architecture/frontend.md,
  modernization/reactxp-to-rnw.md, status dashboards
- LibClient/LibRouter component -> its gallery Content page (pure F#, rule 10),
  fsharp/component.md if the pattern changed, accessibility/recipes.md if a11y changed
- Styling/theming -> fsharp/styling.md, fsharp/themeing.md
- eggshell CLI / scaffolding -> tools/cli.md, modernization/scaffolding.md
- Build/speed -> runbooks/build-rebuild.md, modernization/build-performance.md
- Dev-loop gotcha -> runbooks/<platform>.md + runbooks/troubleshooting.md +
  knowledge-base/engineering-log.md
- Goal/phase status -> modernization/index.md, goals-and-roadmap.md, phased-plan.md
- Doc page added/renamed/moved/deleted -> llms.txt

## Engineering log entry template (prepend under the title)

    ## YYYY-MM-DD (<short title>)
    **Context:** <what was being done>
    **Learning/gotcha:** <what was wrong or missing, and the fix>
    **Distilled:** <added to runbooks/troubleshooting.md? which section? or "not durable">

## Validation gates

1. `node .claude/skills/docs-sync/scripts/check-doc-links.mjs` (exit 1 = broken links, fix them)
2. `.claude/skills/docs-sync/scripts/scan-stale-framings.sh` (judge hits: history pages may
   legitimately mention ReactXP/Fable 4)
3. Status agreement: modernization/index.md vs goals-and-roadmap.md vs phased-plan.md vs reality
   (global.json SDK, package.json versions)
4. Component changes: also run style-leak-audit + a11y-check skills.
5. Docs build: gallery compiles and the page renders (debug-web skill).

## Doc refs

- maintaining-docs.md (golden rules + full update matrix)
- knowledge-base/engineering-log.md (format precedent)

(All under AppEggShellGallery/public-dev/docs/.)
