# Fable 5 `[<Pojo>]` conversion playbook

Audience: an LLM (or human) converting hand-built `createObj` / `Option.map` / `box` JS prop
bags to typed Fable 5 `[<Pojo>]` classes. Read this whole file before editing. Do **not**
attempt an automated/regex codemod: the ~165 `createObj` sites in the framework are
heterogeneous (computed keys, nested bags, `==>`, dynamic `?` access, conditional `yield!`)
and a blind rewrite will produce wrong output. This is a judgment-based, file-by-file change.

## What `[<Pojo>]` does (verified, Fable.Core 5.0.0)

`Fable.Core.JS.PojoAttribute` on a **class** erases the class declaration and emits the
constructor call as a plain JS object. Optional ctor args that are `None` are **omitted**
(not emitted as `undefined`). Member names set the JS key casing.

Source proof (`Test.fs` compiled with `dotnet fable`):

```fsharp
[<Fable.Core.JS.Pojo>]
type AccessibilityStateJs
    ( ?disabled: bool, ?selected: bool, ?``checked``: bool, ?expanded: bool, ?busy: bool ) =
    member val disabled    = disabled
    member val selected    = selected
    member val ``checked`` = ``checked``
    member val expanded    = expanded
    member val busy        = busy

let emptyState () : obj = AccessibilityStateJs() |> box
let someState  () : obj = AccessibilityStateJs(?disabled = Some true, ?``checked`` = Some false) |> box
```

emits:

```js
export function emptyState() { return {}; }
export function someState()  { return { disabled: true, checked: false }; }
```

This is **semantically identical** to the old pattern:

```fsharp
createObj [
    yield! (state.Disabled |> Option.map (fun v -> ("disabled", box v)) |> Option.toList)
    ...
]
```

Reference conversion already in the tree: `LibClient/src/Accessibility.fs`
(`AccessibilityStateJs` + `AccessibilityStateRecord.toJs`). Use it as the template.

## Recipe

1. Add `open Fable.Core` (already present in most interop files) or fully qualify
   `[<Fable.Core.JS.Pojo>]`.
2. Define a class, **one optional ctor arg per JS key**, each typed to the JS value type
   (`bool`, `string`, `int`, `float`, or `obj` for nested/opaque values).
3. Add `member val <name> = <name>` for each arg. **Member name = exact JS key** the target
   library expects — usually camelCase (`disabled`, not `Disabled`).
4. Escape F# keywords with backticks: `` ?``checked`` ``, `` ?``type`` ``, `` ?``end`` ``,
   `` ?``done`` ``.
5. At the call site, splat options through with the `?arg =` syntax:
   `Foo(?disabled = state.Disabled, ?``checked`` = state.Checked)`. For non-optional values
   pass `Foo(disabled = true)`. Return `obj` via `|> box` if the existing signature is `obj`.
6. Keep the public API of the surrounding module identical (drop-in). If a record like
   `AccessibilityStateRecord` is used elsewhere via `{ empty with ... }`, **leave it**; only
   replace the `createObj` body inside its `toJs`/serializer with the Pojo call.

## When to convert vs. leave alone

**Convert** when the prop bag is a fixed, statically-known set of named keys with simple
values — i.e. an options/props object for a JS/RN/ReactXP component or API. These gain type
safety and read better as a Pojo.

**Leave alone** (Pojo adds nothing or breaks):
- Computed/dynamic keys (`key` is a variable, or keys come from a loop/map).
- `createObj []` empty placeholders.
- Nested `createObj` feeding `==>` chains where the shape is deeply dynamic — convert only the
  leaf objects that are fixed, if at all.
- Bags built by merging/`?`-assigning onto an existing JS object (`__props?style <- ...`).
- One-off bags with 1-2 keys where a Pojo class is more code than it saves. Use judgment;
  verbosity reduction is the goal, not Pojo-for-its-own-sake.

## Validate every change (mandatory)

1. **Type-check fast:** rely on the IDE/Ionide diagnostics for the edited file, or call the
   IDE `getDiagnostics` tool. Must be empty.
2. **Confirm emit when in doubt:** compile the file's lib and read the generated JS. Use the
   project's flags, **NOT** `--configuration "Web Debug"** (see gotcha below):

   ```
   export DOTNET_ROOT="$HOME/.dotnet"
   dotnet fable LibStandard/src -o /tmp/<probe> --define DEBUG \
     --define EGGSHELL_PLATFORM_IS_WEB --exclude FablePlugins --noCache
   ```
   Then inspect `/tmp/<probe>/.../<File>.js` — keys must match the target library's expected
   casing, None fields must be absent.
3. **Diff against old output** if the site is behavior-sensitive: the Pojo object and the old
   `createObj` object must be key-for-key equal for the same inputs.

## Build gotcha (Fable 5)

`dotnet fable <proj>.fsproj --configuration "Web Debug"` **fails** in Fable 5: the new MSBuild
project cracker invokes `dotnet msbuild ... /p:Configuration=Web Debug` with the space
unquoted, so `--getProperty:TargetFramework` throws. The framework never uses that config with
Fable — `eggshell` compiles the **src directory** with `--define DEBUG` /
`--define EGGSHELL_PLATFORM_IS_WEB` and lets the fsproj crack with its default config. Do the
same for any manual Fable invocation.
