#!/usr/bin/env node
// Scaffold a gallery Content page (pure F#) + print the 4 registration edits.
// usage: scaffold-gallery-page.mjs <PageName>
import { mkdirSync, writeFileSync, existsSync } from 'node:fs';

const name = process.argv[2];
if (!name || !/^[A-Z][A-Za-z0-9]+$/.test(name)) {
  console.error('usage: scaffold-gallery-page.mjs <PageName>  (PascalCase)');
  process.exit(2);
}
const dir = `AppEggShellGallery/src/Components/Content/${name}`;
const file = `${dir}/${name}.fs`;
if (existsSync(file)) { console.error(`exists: ${file}`); process.exit(2); }

// TEMPLATE: minimal shape derived from Content/InfoMessage/InfoMessage.fs (simplest live page)
// and cross-checked against Content/HorizontalPanArea/HorizontalPanArea.fs (added dc3a6f4) at
// authoring time. Both use the same `type Ui.Content with [<Component>] static member <Name>()`
// extension returning `Ui.ComponentContent(displayName, samples, ...)`. This skeleton fills in
// only the required fields (displayName, samples) plus an a11y panel (mandatory per CLAUDE.md
// rule 12) and a placeholder sample; fill in real props/notes/samples for the real component.
// Update this template if page conventions change.
const TEMPLATE = `[<AutoOpen>]
module AppEggShellGallery.Components.Content___NAME__

open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member __NAME__() : ReactElement =
        Ui.ComponentContent(
            displayName = "__NAME__",
            a11y =
                Ui.A11yPanel(
                    componentName = "__NAME__",
                    role = "TODO: accessibility role",
                    namePattern = "TODO: how the accessible name is derived",
                    stateNotes = "TODO: dynamic state, if any",
                    scalesWithFont = true,
                    contrastNotes = "TODO: contrast notes"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Text "TODO: replace with a live __NAME__ sample",
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text
                                    """
// TODO: sample code for __NAME__
"""
                            )
                    )
                }
        )
`;

mkdirSync(dir, { recursive: true });
writeFileSync(file, TEMPLATE.replaceAll('__NAME__', name));
console.log(`wrote ${file}`);
console.log(`
Now wire 4 registrations (match neighboring lines exactly):
1. AppEggShellGallery/src/App.fsproj:
   <Compile Include="Components/Content/${name}/${name}.fs" />  (beside other Content pages)
2. AppEggShellGallery/src/Navigation.fs:
   | ${name}   (in the ComponentItem DU)
   | ${name}   -> "${name}"   (in ComponentItem.pageTitle)
3. AppEggShellGallery/src/Components/Route/Components/Components.fs (content router):
   | ${name} -> Ui.Content.${name}()
4. AppEggShellGallery/src/Components/Sidebar/SidebarContent.fs:
   compItemIcon "${name}" ${name} itemState   (copy a neighbor's shape / section)
Then: cd AppEggShellGallery && ../eggshell dev-web  ->  check page renders on :8082
`);
