# Build and Rebuild

Targeted recompile recipes, how to confirm a rebuild actually happened, when you must restart (not just touch), verifying that a vendor patch reached the bundle, and killing stale/duplicate processes.

Related: [Dev loop](./runbooks/dev-loop.md) | [Troubleshooting](./runbooks/troubleshooting.md) | [Architecture: Frontend](./architecture/frontend.md)

---

## Always set DOTNET_ROOT first {#dotnet-root}

Every dotnet/fable/eggshell command requires `DOTNET_ROOT` to be set. Add this to your shell profile, or prepend it to each session:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
```

Setting only `PATH="$HOME/.dotnet:$PATH"` is not enough for the apphost binary; `DOTNET_ROOT` must be set explicitly.

---

## Targeted recompile (let the watch do it) {#targeted-recompile}

The running `dev-native` or `dev-web` recompiles only what changed. Force it to notice an edit and avoid a false-green from cache:

```bash
touch <changed>.fs                         # nudge the watcher
```

For a guaranteed full recompile, nuke the platform Fable cache:

```bash
rm -rf SuiteTodo/AppTodo/.build/native/fable   # for native
rm -rf LibStandard/.build/web/fable            # for web (precompiled lib that webpack serves)
```

Editing **framework files** (LibClient, LibRouter, ...) recompiles through the app's graph because the watch prints `Watching ../../..`. Confirm the watch log includes `Compiled .../LibClient/...` or `LibRouter/...` after `touch`ing the framework file, not only app paths. If the running app still shows old behavior:

```bash
rm -rf SuiteTodo/AppTodo/.build/<platform>/fable
# restart dev-native or dev-web
# native: Metro may need --reset-cache as well
```

---

## Confirm the rebuild produced output {#confirm-rebuild}

The emitted JS should be newer than the source you changed:

```bash
find SuiteTodo/AppTodo/.build/native/commonjs -name "<File>.js" -newer SuiteTodo/AppTodo/src/.../<File>.fs
grep -nE "error FS" /tmp/dev-native.log | tail        # zero = type-check clean
tail -3 /tmp/dev-native.log                            # "Successfully compiled N files" / "watcher is ready"
```

**Stale-cache false green.** Fable may print `Skipped compilation because all generated files are up-to-date!` and exit 0 without type-checking your edits. This is the most common source of "it compiled but my change wasn't picked up." The fix is always to force recompile: `touch` the changed `.fs`, or wipe `.build/<platform>/fable`, and then confirm `Started Fable compilation...` appears in the output.

---

## When a restart is mandatory {#mandatory-restart}

These situations require a full restart, not just a `touch`:

| Situation | What to do |
|---|---|
| `.fsproj` changed (added, moved, or reordered files) | Fable watch does not pick up `.fsproj` changes. Restart `dev-native` or `dev-web`. Compile order is the `<Compile Include>` order; a file must come after everything it references. |
| `metro.config.js` changed | Restart Metro with `--reset-cache`. |
| LibClient vendor patch applied or updated | Restart Metro with `--reset-cache`. |
| Watch output "looks stale" and a cache wipe did not fix it | Restart `dev-native` + Metro (`--reset-cache`). |
| Edited a LibClient/LibRouter/LibLangFsharp `.fs` while the gallery's `dev-web` is running, and the change has NO effect on the bundle | The gallery's `dev-web` watch only recompiles the gallery's OWN `.fs` files. LibClient/LibRouter/LibLangFsharp are precompiled into the shared `LibStandard/.build/web/fable/` tree (the gallery's JS imports `LibClient`/`LibRouter` from there), and that tree is only rebuilt on a FULL `dev-web` start, not on incremental lib edits. | Kill the stale `:8082` webpack (and any `fable`/`webpack` children) and restart `cd AppEggShellGallery && ../eggshell dev-web`. Confirm the output file under `LibStandard/.build/web/fable/...` is newer than your edited `.fs` before trusting the bundle. |

---

## Framework lib type-check and build commands {#build-commands}

```bash
# Type-check a framework lib (no JS emitted):
dotnet build LibClient/src/LibClient.fsproj -c "Web Debug"
dotnet build LibClient/src/LibClient.fsproj -c "Native Debug"

# Fable emit to a scratch directory (defeats false-green; plugin errors at the end are benign):
dotnet fable LibClient/src --exclude FablePlugins --define DEBUG --define EGGSHELL_PLATFORM_IS_WEB --noCache -o /tmp/libclient-check

# Build-lib (render DSL codegen + eggshell-specific build; does NOT run Fable):
cd LibClient && ../eggshell build-lib

# Gallery full dev-web:
cd AppEggShellGallery && ../eggshell dev-web    # port 8082

# AppTodo dev-web:
cd SuiteTodo/AppTodo && ../../eggshell dev-web   # port 9080
```

**Framework libs use custom configs, not plain `Debug`.** Always pass `-c "Web Debug"` or `-c "Native Debug"` to `dotnet build`; plain `-c Debug` picks the wrong configuration.

**Do NOT pass `--configuration "Web Debug"` to `dotnet fable`.** The Fable 5 MSBuild cracker runs `dotnet msbuild ... /p:Configuration=Web Debug` with the space unquoted, which throws. Compile the `src` directory directly with `--define` flags instead.

---

## Verify a vendor/node_modules patch actually reached the bundle {#verify-patch-reached-bundle}

Metro bundles node_modules from each package's build entry (for example, `react-native-web` from its `dist/` directory). After patching a file and restarting Metro with `--reset-cache`:

```bash
curl -s "http://localhost:8081/index.bundle?platform=android&dev=true" | grep -c "<your-marker>"
```

`0` means your edit is not in the bundle: either you edited the wrong file (e.g. `src/` instead of `dist/`), or Metro cached an old version. Restart Metro with `--reset-cache` and check again.

**Prefer an F# seam fix over a node_modules patch.** Vendored edits are maintenance surface. The "unique key" warning, for example, was fixed in F# (`tellReactArrayKeysAreOkay` in `Context.fs`), not by patching node_modules.

---

## Killing stale and duplicate processes {#killing-stale-processes}

Duplicate `dev-native` or Metro instances race each other's output and serve half-old bundles.

Detect stale processes:

```bash
ps aux | grep -iE "eggshell dev-native|react-native start|dotnet fable|fable.dll|qemu-system" | grep -v grep
```

Full clean (native dev + emulator):

```bash
pkill -f "eggshell dev-native"; pkill -f "dotnet fable"; pkill -f "fable.dll"
pkill -f "plugin-transform-modules-commonjs"; pkill -f "react-native start"
pkill -f "qemu-system-aarch64"; pkill -f "emulator -avd"
```

Notes:
- These watches typically run in the user's IDE terminals. Killing them is safe; then relaunch. Re-verify with the `ps` line above (expect 0 matches).
- `eggshell dev-native` is a wrapper that returns while the real `dotnet fable ...` child keeps watching. Do not assume the watch died just because the wrapper command "completed".
