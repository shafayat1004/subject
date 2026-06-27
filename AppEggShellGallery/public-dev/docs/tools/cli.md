# The `eggshell` Command Line Tool

We use the `eggshell` command to scaffold new apps, components, routes, dialogs,
to rename existing components and thus keep the codebase clean and organized,
to run the watch-for-changes-and-recompile development toolchain, and to package
the app for deployment.

Currently the `eggshell` CLI commands do not take command line arguments, instead
collecting their data in an interactive fashion. We're planning to support both
in the future.

## `eggshell create-app`

Scaffolds a new application at the root of the repo. Some manual steps may be required,
especially if you need to move it in a level deeper into the hierarchy, like into a suite.
Outputs the following message when done:

```plaintext
Created app ${appName}

To get started you can:

cd ${appRootPath} #if you haven't already
eggshell dev-web

And then once it's all running, go to http://localhost:9080 in your browser.

New apps listen on **9080** by default (`Meta/LibFablePlus/webpack.config.js`). **AppEggShellGallery** uses **8082**.

If you need to move the app into a non-root directory, here are the files you'll
need to update with additional dot-dot-slashes

    .npmrc
    eggshell.json
    src/App.fsproj
    webpack.config.js
    webpack.config.build.js
    ${appName}.sln
    App.code-workspace
```

## `eggshell build-lib`

Compiles `.render` / `.typext.fs` files in the current project and listed dependencies,
regenerates `ComponentRegistration.fs`, and runs the Fable precompile pass for framework libs.
Run from a lib or app directory after adding/removing components or deleting converted render files.

Typical use after converting a component cluster: ensures autogen on disk matches the fsproj and
registration no longer lists deleted `.render` pages.

## `eggshell test-build`

One-shot production webpack bundle (same pipeline as CI packaging, without deploy). Use to validate
the app compiles end-to-end after large refactors. From `AppEggShellGallery`:

```bash
../eggshell test-build
```

## `eggshell convert-component`

Runs the RenderDSL compiler in **RenderConvert** mode and prints readable F# to stdout. It does
**not** write files — use the output as a starting point for hand conversion to `[<Component>]`
(see `MODERNIZATION_PLAN.md` in the repo). Prefer the LEARNINGS.md conversion recipe for production
migrations.

## `eggshell dev-web`

Runs the current app in development mode via webpack-dev-server. Default port **9080**; **AppEggShellGallery** uses **8082** (see `Meta/LibFablePlus/webpack.config.js`).

Watches `.render` and `.typext.fs` files and compiles them with the RenderDSL compiler in this project and in dependencies listed under `eggshell.json` → `render.dependenciesToRtCompile`.

## `eggshell dev-native`

Transpiles the app to `.build/native/commonjs` in **watch** mode. Use with Metro on port 8081:

```bash
npx react-native start --port 8081
adb reverse tcp:8081 tcp:8081   # Android emulator, each session
```

Requires `configSourceOverrides.native.js` (from `./initialize`). See [Native Development](../basics/native.md).

## `eggshell build-native`

One-shot native Fable compile (no watch). Run after pulling native toolchain changes, then restart Metro.

## `eggshell dev-native-server`

Starts Metro from the app directory (`npx react-native start`, optional `--reset-cache`).

## `eggshell create-component`

Scaffolds a new component. Prefer the **pure F#** path: one `Components/.../Foo.fs` with
`[<Component>]`, module name `AppOrLib.Components.Foo` (or `Foo_Bar_Baz` for nested paths),
entry in `App.fsproj`, and extend the type in `ComponentsHierarchy.fs` if adding a new namespace.

The CLI may still offer legacy render-DSL shapes (typext + `.render` + `.styles.fs`). Do not use
those for new framework or gallery work — they are being retired (Goal A).

Component names can be namespaced (`Cook.Dish.Schedule`). Use `rename-component` if the name
needs to change later.

## `eggshell rename-component`

Just like easy renaming of values, functions, modules, and types is necessary to maintain
a clean codebase in a general language, easily renaming components is fundamental to maintaining
a clean component library or front end app.

This command allows to rename components, including across namespaces, within the same eggshell
project. If you need to move out a component into a different library, that has to be done manually.
(it's not rocket science or anything, just need to move some files, update the references, rename
modules and namespaces).

This command does not currently update the use sites, as that's the relatively easy task that can be
accomplished by a simple global replace in your IDE.

## `eggshell create-route`

Scaffolds a new route component in the current app (interactive prompts).

## `eggshell create-dialog`

Scaffolds a dialog component (interactive prompts).

## `eggshell create-third-party-wrapper`

Scaffolds a third-party wrapper lib (interactive prompts).

## `eggshell package-web`

Production webpack bundle for web deployment.

## `eggshell package-android`

Package Android native app (see CLI `--help` for variants).

## `eggshell package-app`

Legacy alias / see `package-web` and `package-android` above.

## The `eggshell.json` config file

Each eggshell project, including the repo root "metaproject", are configured using
the `eggshell.json` configuration file. It has the following structure:

```typescript
// defined in /Meta/LibEggshell/src/index.ts
// Seq here is an alias for immutable array.

export interface RootConfig {
    type: 'repoRoot';
}

export interface EggShellProjectConfigCommon {
    name: string;
    render: {
        // Which other projects to compile .render and .typext.fs files in, and watch
        // for changes during `eggshell dev-web`. Values should be relative paths to the
        // projects' directories.
        dependenciesToRtCompile:      Seq<string> | undefined;

        // These modules will be `open`'ed in the .Render.fs file
        // that is generated for each .render file. Use this for very
        // common modules only; one-off modules required only in some
        // particular components should be opened using `rt-open` on the
        // root XML node of the .render file.
        additionalModulesToOpen:      Seq<string>;

        // Aliases for component libraries. That's where, for example,
        // "LC" is configured to refer to "LibClient.Components".
        componentLibraryAliases:      Seq<[string | 'default', string]>;

        // Namespace to relative path mapping for libraries.
        componentLibraryPaths:        Seq<[string, string]>;

        // Component aliases. This is where, for example,
        // "div" is mapped to "RX.View".
        componentAliases:             Seq<[string, string]>;

        // In rare cases where a project has multiple Components.proj-like files,
        // you can help the rename-component tool by adding the additional files here.
        additionalComponentProjFiles: Seq<string> | undefined;
    }
}

export interface AppConfig extends EggShellProjectConfigCommon {
    type: 'app';
    build: {
        // Source file or glob to destination file or directory mapping.
        // These files will be copied to the destination bundle.
        copyStaticFiles: Seq<[string, string]>;
    }
}

export interface LibraryConfig extends EggShellProjectConfigCommon {
    type: 'library';
}

export type ProjectConfig = AppConfig | LibraryConfig | RootConfig
```
