---
name: gallery-page-add
description: Add a new App Gallery content page in pure F# (never render DSL) with all four registrations wired. Use whenever a LibClient/LibRouter component is added or changed and needs a gallery page (CLAUDE.md rule 10), or when asked to add a gallery demo/docs page.
user-invocable: true
argument-hint: "<PageName>"
---

# gallery-page-add

Five files per page. `scripts/scaffold-gallery-page.mjs <PageName>` writes the page skeleton and
prints the four registration edits (registrations stay manual: those files are load-bearing and
order-sensitive).

1. `AppEggShellGallery/src/Components/Content/<PageName>/<PageName>.fs` (generated skeleton;
   fill in the demo content; model on Content/HorizontalPanArea/HorizontalPanArea.fs or the
   simpler Content/InfoMessage/InfoMessage.fs)
2. `AppEggShellGallery/src/App.fsproj`: add
   `<Compile Include="Components/Content/<PageName>/<PageName>.fs" />` NEXT TO the other Content
   pages (compile order matters; keep alphabetical/grouped placement consistent with neighbors)
3. `AppEggShellGallery/src/Navigation.fs`: add `| <PageName>` to the `ComponentItem` DU AND a
   `| <PageName> -> "<PageName>"` case to `ComponentItem.pageTitle`
4. `AppEggShellGallery/src/Components/Route/Components/Components.fs` (content router): add
   `| <PageName> -> Ui.Content.<PageName>()` match case
5. `AppEggShellGallery/src/Components/Sidebar/SidebarContent.fs`: add
   `compItemIcon "<PageName>" <PageName> itemState` following the neighboring items' exact shape

## Rules

- Pure F# `[<Component>]` style only; no render DSL, no `.typext.fs`.
- Page must meet the a11y bar (a11y-check skill) and style rules (style-leak-audit skill).
- Static sibling element arrays go through `castAsElementAckingKeysWarning` (React keys).

## Validate

- `cd AppEggShellGallery && ../eggshell dev-web`, open http://localhost:8082, click the new
  sidebar entry, page renders. Then fable-rebuild-verify proof gate if anything looks cached.

## Doc refs

- maintaining-docs.md (docs routing when the page documents a component)
- modernization/render-dsl-retirement.md (component style rules)

(All under AppEggShellGallery/public-dev/docs/.)
