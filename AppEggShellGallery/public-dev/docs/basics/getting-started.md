# Getting Started with EggShell

Our official editor is VSCode. We target VSCode + Ionide for F# tooling; see [Dev Experience](./dev-experience.md).

The tooling runs most smoothly on macOS and Linux. Windows users should use **Git Bash** with symlink support enabled (see below).

## Windows setup (Git Bash)

1. Install [Git for Windows](https://git-scm.com/download/win) and use Git Bash for EggShell commands.

   Enable symbolic links:
   * Turn on **Developer Mode** in Windows settings, and/or
   * `git config --global core.symlinks true`

2. Install Node.js LTS ( [nvm-windows](https://github.com/coreybutler/nvm-windows) is fine)
3. Install [.NET SDK 6.0+](https://dotnet.microsoft.com/download)
4. For native apps: see [Native getting started](./native/getting-started.md)
5. Clone this repository

Run the steps below from **Git Bash**, not PowerShell.

## Repository setup (required once)

After cloning, install:

* .NET SDK 6.0 or newer
* Node.js LTS

Add the **repository root** to your `PATH` so the `eggshell` CLI is available:

```bash
export PATH=/path/to/subject:$PATH
```

Optional tab completions:

```bash
source /path/to/subject/Meta/AppEggshellCli/bash.completions
```

At the **repo root**, run:

```bash
./initialize
```

This builds the scaffolding CLI, links npm packages across framework libs, and runs initial `eggshell build-lib` passes. It takes several minutes the first time.

## Running AppEggShellGallery (this repo's sample app)

The gallery is the best reference app and hosts these docs in dev mode.

```bash
cd AppEggShellGallery
./initialize
eggshell dev-web
```

Open **http://127.0.0.1:8082/** (gallery-specific port; other apps use 9080 by default).

For **native** gallery dev, see [Native Development](./basics/native.md).

## Creating a new app

From the repo root (after `./initialize`):

```bash
eggshell create-app
```

If you name it `Sample`, the directory is gitignored by convention. Then:

```bash
cd AppSample
./initialize
eggshell dev-web
```

Open **http://localhost:9080/** (default port for scaffolded apps).

Copy `configSourceOverrides.native.js` from the template before native dev; `./initialize` does this automatically for new apps.

## Edit-save-reload

In VSCode, open a workspace file (`.code-workspace`) for the app or lib you are editing — **do not** open the entire monorepo root in one window (Ionide and watchers misbehave).

Change a `.render` or F# file and save; the browser or Metro bundle should pick up the change.

**Caveat:** the file watcher can miss changes after git checkouts that add/remove many files. Restart `eggshell dev-web` or `eggshell dev-native` when that happens.

## Getting an existing app running

1. Repo root: `./initialize` (if tooling changed or first clone)
2. App directory: `./initialize`
3. Web: `eggshell dev-web`
4. Native: see [Native Development](./basics/native.md)

`./initialize` in the app directory is **not** run automatically by `eggshell dev-web` (to keep daily web dev fast).

## Next steps

* [eggshell CLI](./tools/cli.md) — scaffold components, routes, libraries
* [Directory structure](./unsorted/directory-structure.md)
* [How To index](./how-to/index.md)
* [Native Development](./basics/native.md)
