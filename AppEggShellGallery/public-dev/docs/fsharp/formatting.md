# F# Code Formatting Conventions

> **Status:** canonical style reference for EggShell framework code (Lib\*, LibUi\*, LibRouter,
> LibAutoUi, LibLifeCycleUi, ThirdParty, Meta/\*, Suite\*, App\*). Derived from the established
> codebase baseline. Use this as the source-of-truth when configuring Fantomas or reviewing PRs.
>
> **Fantomas note:** EggShell uses Fantomas 6.x (see `.config/dotnet-tools.json`). Each rule below
> notes the corresponding Fantomas setting where one exists. Settings not listed here are left at
> the Fantomas default.

---

## Table of Contents

1. [Indentation](#1-indentation)
2. [Column alignment](#2-column-alignment)
3. [Line length and line breaks](#3-line-length-and-line-breaks)
4. [Operators and spacing](#4-operators-and-spacing)
5. [Match expressions](#5-match-expressions)
6. [Type annotations](#6-type-annotations)
7. [Record construction](#7-record-construction)
8. [Discriminated union definitions](#8-discriminated-union-definitions)
9. [Function signatures](#9-function-signatures)
10. [Pipeline style](#10-pipeline-style)
11. [Module and namespace declarations](#11-module-and-namespace-declarations)
12. [Comments](#12-comments)
13. [Computation expressions (codec / async / ce blocks)](#13-computation-expressions)
14. [Class and interface members](#14-class-and-interface-members)
15. [Attributes](#15-attributes)
16. [Tooling](#16-tooling)

---

## Developer setup (day 1)

1. `dotnet tool restore` from the solution root -- installs Fantomas as pinned in
   `.config/dotnet-tools.json`. Required once per machine; the build-time check calls
   `dotnet tool run fantomas` which resolves from there.
2. In Rider: Settings > Editor > Code Style > F# -- confirm formatter shows "Fantomas", then
   Settings > Tools > Fantomas > "Use custom Fantomas tool" so Rider uses the same pinned version.
3. Done. Every build now warns on formatting violations. To fix: `dotnet tool run fantomas <file.fs>`
   or reformat in Rider with `Cmd+Alt+L` / `Ctrl+Alt+L`.

To silence a project during active work: `<EggShellFmtSeverity>none</EggShellFmtSeverity>` in its
`.fsproj`. Remove when done.

The alignment rules (sections 2a-2d, 7, 13) are not enforced by Fantomas -- maintained by hand
and PR review until `EggShellFmt` is built (section 16d).

---

## 1. Indentation

**Rule:** 4 spaces per level. No tabs anywhere.

**Fantomas:** `indent_size = 4`

Continuation indents (arguments that wrap to the next line, bodies of lambdas passed as arguments)
also use 4 spaces relative to the expression they belong to, NOT a "visual" alignment to the opening
parenthesis.

```fsharp
// CORRECT — 4-space continuation, not visual indent to '('
|> Option.bind
    (fun elapsed ->
        match elapsed with
        | ...)

// WRONG — visual indent locked to '('
|> Option.bind (fun elapsed ->
                    match elapsed with
                    | ...)
```

Multi-parameter function bodies start 4 spaces in from the `let`/`member` keyword, even when the
parameter list itself spans multiple lines at +8.

```fsharp
let private computeFoo
        (a: int)
        (b: string)
        : ResultType =
    doWork a b          // body at +4, not +8
```

---

## 2. Column alignment

Column alignment is used in **type definitions**, **`let` binding groups**, and **match case arms**
when it significantly aids readability.

### 2a. Record type fields — align the `:` separator

When a record has multiple fields whose names differ in length, pad the shorter names with spaces so
all `:` signs line up at the same column.

```fsharp
// CORRECT — : signs aligned
type Address = {
    Street:      string
    City:        string
    PostalCode:  string
    CountryCode: string
}

// WRONG — no alignment
type Address = {
    Street: string
    City: string
    PostalCode: string
    CountryCode: string
}
```

### 2b. Discriminated union cases — align the `of` keyword or payload

When union cases differ in name length, pad with spaces so the `of` keyword (or the payload) starts
at the same column:

```fsharp
// CORRECT
| Short       of ShortStatus * ShortConfig
| Medium      of MediumStatus * MediumConfig
| LongerCase  of LongerStatus

// WRONG — ragged
| Short of ShortStatus * ShortConfig
| Medium of MediumStatus * MediumConfig
| LongerCase of LongerStatus
```

### 2c. Short match arms — align `->` for sequential cases

When all cases in a `match` have short bodies (names, string literals, simple expressions), align
the `->` arrows:

```fsharp
// CORRECT — arms are short, alignment helps scanning
match this with
| ShortCase _    -> "short"
| Medium _       -> "medium"
| LongerCaseName -> "longer"

// CORRECT — mixed: one arm needs a multi-line body, others stay inline
// Do NOT force alignment when any arm breaks to the next line
match this with
| ShortCase _ -> "short"
| Medium _    -> "medium"
| LongerCaseName ->
    let result = computeSomething ()
    result.ToString()
```

### 2d. Let bindings — align `=` signs within a group

When two or more consecutive `let` bindings in the same scope are related (same logical step,
same data shape), pad the shorter names so all `=` signs line up at the same column.

```fsharp
// CORRECT — = signs aligned
let maybeCurrentStatus  = getStatus entityId
let maybeCurrentDefault = getDefault ()

// WRONG — ragged = signs
let maybeCurrentStatus = getStatus entityId
let maybeCurrentDefault = getDefault ()
```

A "group" ends at a blank line or a non-`let` expression. Bindings in separate groups do not need
to align with each other.

---

## 3. Line length and line breaks

**Soft limit:** ~120 characters. Lines inside computation expressions (`codec`, `async`, `task`) may
exceed this when the alternative is an unreadable multi-line break.

**Hard limit:** none enforced by Fantomas here, but prefer line breaks over very long lines in
function bodies and signatures.

**When to break:**

| Construct | Break when |
|---|---|
| Function call with named args | any named arg makes the call exceed ~80 chars |
| Function signature params | more than 2 params, OR total length > ~80 chars |
| Pipeline | more than 1 `\|>` operator |
| Record literal | more than 2 fields, OR total length > ~60 chars |
| `match … with` case body | body is more than ~40 chars |

---

## 4. Operators and spacing

| Operator | Rule | Example |
|---|---|---|
| `\|>` pipe | space before, space after | `x \|> f` |
| `>>` compose | space before, space after | `f >> g` |
| `=` assignment / comparison | space before, space after | `let x = 5`, `a = b` |
| `:` type annotation | **no** space before, one space after | `x: int` |
| `->` arrow | space before, space after | `fun x -> x + 1` |
| `*` in type expressions | space before, space after | `string * int` |
| `<` `>` generic brackets | no spaces inside | `Option<string>` |
| `;` in lists/arrays | no space before, one space after | `[1; 2; 3]` |
| `_` wildcard | no surrounding spaces beyond what the construct requires | `| _ -> ()` |

**Function application:** use a space, not parens, for single arguments. Use parens when passing a
compound expression, tuple, or when disambiguation is needed.

```fsharp
foo bar             // single arg — no parens
foo (bar, baz)      // tuple arg — parens required
foo (getBar ())     // compound — parens for clarity
```

---

## 5. Match expressions

### Placement

`match` and the scrutinee stay on the same line. `with` stays on the same line as `match`.

```fsharp
match this.GetStatus itemId with
| Some x -> ...
| None   -> ...
```

### Case arm indentation

Each `|` aligns with the `m` of `match` (same column).

```fsharp
let result =
    match value with          // match is at +4
    | Some x -> process x     // | is at +4
    | None   -> defaultValue
```

When `match` is the top-level expression in a function body (not nested), `|` is at the function
body indent level (+4 from `let`).

### Case body: inline vs. newline

| Body length | Style |
|---|---|
| Short (< ~40 chars) | inline after `->` on same line |
| Long or multi-expression | newline, body at +4 from the `\|` line |

```fsharp
// Inline — short body
| Some x -> x.ToString()

// Newline — longer or multi-expression body
| Some x ->
    let processed = transform x
    processed.ToString()
```

### Mixed arms in the same match

It is fine — and common — for some arms to be inline and others to break to the next line within the
same `match`. Do not force all arms to one style.

```fsharp
match status with
| Pending  -> "pending"
| Active   -> "active"
| Archived ->
    // needs extra computation
    let label = buildArchivedLabel metadata
    label.Display
```

When any arm goes multi-line, stop trying to column-align the `->` arrows (see section 2c).

---

## 6. Type annotations

Always inline with the binding or parameter. Never on a separate line.

```fsharp
let foo (x: string) (y: int) : bool =   // annotation on param, return type after last param
    x.Length > y
```

- No space before `:`, one space after.
- Return type annotation (`: ReturnType`) goes at the end of the last parameter line, before `=`.
- Generic type arguments use `<>` with no internal spaces: `Option<string>`, `Map<Key, Value>`.

---

## 7. Record construction

### Inline

Use inline form only when the entire expression fits comfortably on one line (< ~60 chars):

```fsharp
{ Id = id; Name = name }
```

### Multi-line

Opening `{` on the same line as the let/return. Each field on its own line at +4. Closing `}` on
its own line, at the same indent as the opening context (NOT the same indent as the fields).

```fsharp
return {
    FirstName   = firstName
    LastName    = lastName
    Email       = email
    PhoneNumber = phoneNumber
}
```

**Field value alignment:** `=` signs in a multi-line record literal are aligned, same rule as
section 2d (let binding groups). Pad field names so the `=` signs line up.

### Record update

```fsharp
{ existing with
    FieldA = newA
    FieldB = newB }
```

---

## 8. Discriminated union definitions

Short cases (no payload or very short payload) go one per line. Use the column-alignment rule from
section 2b when names vary in length.

Labeled fields in a union case use the `FieldName: Type` form:

```fsharp
| Completed of Amount: PositiveDecimal * RefundInfo: Option<RefundDetails>
```

Multi-line union case (when the type expression is very long):

```fsharp
| ComplexCase
    of FieldA: SomeLongTypeName
    *  FieldB: AnotherLongTypeName
    *  FieldC: YetAnotherType
```

---

## 9. Function signatures

### Single-line

When the full signature fits in ~80 chars, keep it on one line:

```fsharp
let add (a: int) (b: int) : int = a + b
```

### Multi-line

When a function has more than 2 parameters, or when the signature exceeds ~80 chars, break at
each parameter. Parameters indent **+8** from the `let` (double indent). Return type annotation
goes on the LAST parameter line, before `=`. Function body then starts on the next line at +4.

```fsharp
let private processItems
        (source:   ItemSource)
        (filter:   ItemFilter)
        (pageSize: int)
        : List<Item> * PageInfo =
    // body at +4 from let
    doWork source filter pageSize
```

The `+8` for params vs `+4` for the body creates a clear visual distinction between "parameters"
and "body."

### Recursive and access modifiers

`rec` and `private`/`internal` go between `let` and the function name, on the same line:

```fsharp
let rec private buildTree (nodes: List<Node>) : Tree = ...
```

---

## 10. Pipeline style

### Single pipe — keep inline

```fsharp
let result = xs |> List.filter pred
```

### Two or more pipes — one per line

```fsharp
let result =
    items
    |> List.filter isActive
    |> List.sortBy (fun x -> x.Name)
    |> List.truncate pageSize
```

The first expression (the source) is on its own line. Each `|>` goes at the start of the next line,
at the same indent level as the source. Continuation lambdas passed to a pipe step indent +4 from
the `|>`:

```fsharp
items
|> List.filterMap
    (fun item ->
        match item.Status with
        | Active amount -> Some (item.Id, amount)
        | _             -> None)
|> Map.ofList
```

---

## 11. Module and namespace declarations

One blank line after the `module`/`namespace` line, before the first `open`. `open` statements
group together with no blank lines between them. One blank line after the last `open`, before the
first type or let.

```fsharp
[<AutoOpen; CodecLib.CodecAutoGenerate>]
module SuiteFoo.Types.Common

open System
open System.Collections.Generic
open LibLifeCycleCore

type Foo = { ... }
```

One blank line between top-level type definitions. Two blank lines before a major section (e.g.,
before the generated codec block or before a large group of members).

---

## 12. Comments

- `//` line comments: one space after `//`. `// This is correct`, NOT `//This is wrong`.
- `///` doc-comments: one space after `///`. Used for XML documentation on public types and members.
- Inline comments: rare; place one space before the `//` when appending to a line of code.
- Multi-line comments: use consecutive `//` lines, not `(* *)` block comments.
- No commented-out code in committed files.

```fsharp
// Good comment: explains WHY, not what
let retryLimit = 3  // per Orleans 3-strike policy
```

---

## 13. Computation expressions

Applies to `codec { }`, `async { }`, `task { }`, and any custom CE builder.

Opening keyword and `{` on the same line. Body at +4. Closing `}` on its own line at the same
indent as the opening keyword.

```fsharp
async {
    let! result  = fetchData id
    let! profile = fetchProfile result.ProfileId
    return {
        Data    = result
        Profile = profile
    }
}
```

`let!` and `and!` bindings align their `=` signs, same rule as section 2d (let binding groups):

```fsharp
codec {
    let! version = reqWith Codecs.int "__v1" (fun _ -> Some 0)
    and! name    = reqWith Codecs.string "Name" (fun x -> Some x.Name)
    and! email   = reqWith Codecs.string "Email" (fun x -> Some x.Email)
    return {
        Name  = name
        Email = email
    }
}
```

**Long lines in CE blocks are acceptable** when breaking would produce worse readability. The
~120-char soft limit is relaxed inside CE bodies; do not introduce artificial line breaks just to
fit the limit if the natural reading is clearer as one line.

---

## 14. Class and interface members

One blank line between members inside a `type … with` block. Method bodies indent +4 from `member`.

```fsharp
type Item with
    member this.GetLabel() =
        match this with
        | Active _   -> "active"
        | Archived _ -> "archived"

    member this.IsVisible() =
        match this with
        | Active _ -> true
        | _        -> false
```

Interface implementation:

```fsharp
interface IStringIndex<ErrorCode> with
    member this.Primitive =
        this.ToString()
```

---

## 15. Attributes

Attributes go on the line immediately above the declaration they annotate. No blank line between
the attribute and the declaration.

```fsharp
[<AutoOpen>]
module Foo.Bar

[<RequireQualifiedAccess>]
type Status =
    | Active
    | Inactive

[<Component>]
static member Foo(...) : ReactElement = ...
```

Multiple attributes on one line when short, or stacked one per line when long or semantically
distinct:

```fsharp
[<AutoOpen; CodecLib.CodecAutoGenerate>]   // short — one line OK

[<JsonConverter(typeof<FooConverter>)>]
[<Sealed>]                                 // stacked — long or distinct concerns
type Bar = ...
```

---

## 16. Tooling

### Severity model (per project, build-time)

Formatting is checked at build time via an MSBuild target in `Directory.Build.targets`. Severity
is controlled by a single property, overridable per project:

| Value | Behaviour |
|---|---|
| `warning` **(default)** | build succeeds; a warning is emitted if files don't match |
| `error` | build fails if files don't match (use for CI-critical projects) |
| `none` | check is skipped entirely (use for vendored / in-progress / unformatted projects) |

Set it in a project's `.fsproj` to override the default:

```xml
<PropertyGroup>
  <EggShellFmtSeverity>none</EggShellFmtSeverity>    <!-- silence this project -->
  <!-- or -->
  <EggShellFmtSeverity>error</EggShellFmtSeverity>   <!-- block build on violations -->
</PropertyGroup>
```

Or pass on the command line for a one-off stricter check:

```bash
dotnet build -p:EggShellFmtSeverity=error
```

**Vendored source** (`Meta/LibSignalRClient`, `Meta/LibSignalRServer`) is excluded via
`.fantomasignore` at the solution root -- no per-project config needed for those.

**Autogenerated files** (`_autogenerated_/`) and build output (`obj/`, `bin/`) are also excluded
via `.fantomasignore` so they are never checked or reformatted.

**`.config/dotnet-tools.json`** at the solution root pins Fantomas. Run `dotnet tool restore` once
before building; the MSBuild target calls `dotnet tool run fantomas` which resolves from there.

---

Coverage breakdown: what each tool covers and what it can't touch.

| Convention | EditorConfig | Fantomas | Rider | eggshell-fmt |
|---|---|---|---|---|
| 4-space indent, no tabs | hint | enforced | enforced | -- |
| Operator spacing | -- | enforced | enforced | -- |
| Blank lines between defs | -- | enforced | enforced | -- |
| Line length limit | hint | enforced | enforced | -- |
| Bracket / brace placement | -- | enforced | enforced | -- |
| Signature `+8` param indent | -- | partial | partial | -- |
| Record field `:` alignment (2a) | -- | **no** | **no** | enforced |
| Union case `of` alignment (2b) | -- | **no** | **no** | enforced |
| Match arm `->` alignment (2c) | -- | **no** | **no** | enforced |
| `let` group `=` alignment (2d) | -- | **no** | **no** | enforced |
| Record literal `=` alignment (7) | -- | **no** | **no** | enforced |
| CE binding `=` alignment (13) | -- | **no** | **no** | enforced |

None of these tools run as a CI gate or pre-commit hook. Run them when you want.

---

### 16a. EditorConfig (always-on hints)

`.editorconfig` at the solution root. Picked up automatically by VS Code, Rider, and most editors.
Does not auto-format -- it tells the editor how to behave as you type. Apply once at solution root;
no per-project copy needed since `.editorconfig` inherits via directory walk.

```ini
root = true

[*.fs]
indent_style             = space
indent_size              = 4
end_of_line              = lf
charset                  = utf-8-bom
trim_trailing_whitespace = true
insert_final_newline     = true
max_line_length          = 120

[*.fsproj]
indent_style = space
indent_size  = 2
```

---

### 16b. Fantomas (auto-formatter, covers ~60% of rules)

Fantomas handles spacing, indentation, blank lines, bracket placement, and line-length-driven
line breaks. It **cannot** do column alignment (sections 2a-2d, 7, 13). With `StrictMode = false`
it preserves alignment you have already written; it will not create it.

**IMPORTANT:** do not run Fantomas in strict mode (`--check` with `StrictMode = true`) on files
that contain manual column alignment -- it will flag them as incorrectly formatted even though
they follow this spec.

Place `.fantomasrc.json` in each opted-in project directory (not at solution root):

```json
{
  "IndentSize": 4,
  "IndentOnTryWith": true,
  "MaxLineLength": 120,
  "SpaceBeforeColon": false,
  "SpaceAfterComma": true,
  "SpaceAfterSemicolon": true,
  "IndentationStyle": "FSharp",
  "MultilineBracketStyle": "Aligned",
  "AlignFunctionSignatureToIndentation": true,
  "AlternativeLongMemberDefinitions": true,
  "MultilineBlockBracketsOnSameColumn": false,
  "KeepIndentInBranch": true,
  "SpaceAroundDelimiter": true,
  "MaxRecordWidth": 60,
  "MaxArrayOrListWidth": 60,
  "MaxValueBindingWidth": 80,
  "MaxFunctionBindingWidth": 80,
  "MaxElmishWidth": 40,
  "SingleArgumentWebMode": false,
  "AlignLongDistanceOperators": true,
  "StrictMode": false
}
```

Run on a single file: `dotnet fantomas <file.fs>`
Run on a project directory: `dotnet fantomas <ProjectDir/src/>`
Check without writing: `dotnet fantomas --check <file.fs>`

---

### 16c. Rider IDE

Rider 2023+ uses Fantomas as its F# formatter engine. It discovers the nearest `.fantomasrc.json`
walking up from the file -- so the per-project files from 16b are picked up automatically.

**Settings to verify** (Settings > Editor > Code Style > F#):

- Formatter: confirm "Fantomas" is selected (not "Built-in"). Install the Fantomas plugin from
  the JetBrains Marketplace if missing.
- To use the repo's pinned Fantomas version rather than Rider's bundled one: Settings > Tools >
  Fantomas > "Use custom Fantomas tool" > point at `dotnet fantomas` (resolved via
  `.config/dotnet-tools.json`). Keeps the version consistent across CLI and IDE.

**Reformat shortcut:**

| OS | Shortcut |
|---|---|
| macOS | `Cmd + Alt + L` |
| Windows / Linux | `Ctrl + Alt + L` |

**Reformat on save (opt-in per developer):** Settings > Tools > Actions on Save > "Reformat code".
Scope it to F# files only. Off by default; each developer enables it for themselves.

**Alignment live templates:** Settings > Editor > Live Templates. Use these to insert pre-aligned
boilerplate stubs (e.g. a record type with `:` signs already padded). Per-developer, not committed.

---

### 16d. EggShellFmt (implemented -- `Meta/EggShellFmt`, tool command `eggshell-fmt`)

A dotnet local tool (F#, no JS, no Node.js) that covers the alignment rules Fantomas cannot. Pinned
in `.config/dotnet-tools.json`; the package is built into a git-ignored local feed
(`Meta/EggShellFmt/nupkg`, wired in `nuget.config`).

```bash
./Meta/EggShellFmt/install.sh                            # one-time per machine (packs + installs)
dotnet tool run eggshell-fmt -- LibClient/src            # format in place
dotnet tool run eggshell-fmt -- --check LibClient/src    # CI: exit 3 if anything would change
```

Enforces 2a (record `:`), 2b (DU `of`), 2c (match `->`, only when all arms inline), and 7 (record
literal `=`). The `=` binding-group rules **2d (let/and) and 13 (CE let!/and!) are opt-in** by default:
a group is aligned only when the author already aligned it (>1 space before an `=`). It masks
strings/comments before scanning (never edits inside them), leaves function params / named-args
(trailing comma) alone, and is idempotent. It does not reflow lines or fix operator spacing -- run
Fantomas for that. See the `fsharp-format` skill.

**Aggressiveness levels** (`--level`, default `standard`): `whitespace|0` (normalization only),
`conservative|1`, `standard|2`, `aggressive|3` (forces let/CE and aligns outliers). Levels 1-2 apply
**aesthetic relaxation** consistent with section 2's "when it significantly aids readability": within a
group, a member whose left part is far longer than the rest (one very long field name, or `| _ ->`
beside a long pattern) is treated as an outlier -- kept at a single space so it neither aligns nor
drags the block out, while the rest still align.

**Ignore files:** a gitignore-style `.eggshellfmtignore` (and `.fantomasignore`) in the working
directory excludes files/globs; `--no-ignore` bypasses them.

Implementation is line-based (masking + indent-scoped block detection), not FCS-based as originally
sketched below; the FCS notes are kept for reference.

**Opt-in check:** EggShellFmt looks for a `.fantomasrc.json` in the same directory as the target
file (walking up at most to the nearest `.fsproj`). If none is found, it exits without changes.
This means the same opt-in signal (`.fantomasrc.json`) controls both tools -- no separate marker
file needed.

**Workflow (pure dotnet, no JS):**

```bash
dotnet fantomas <file.fs>       # normalize spacing / indentation / brackets
dotnet eggshell-fmt <file.fs>   # apply column alignment on top
```

Or as a single pass that runs Fantomas internally first:

```bash
dotnet eggshell-fmt --full <file.fs>
```

**Scope of what it enforces:**

- Record type field `:` alignment (section 2a): scan each `type … = { … }` block; find the longest
  field name; pad all shorter names to that column.
- Union case `of` alignment (section 2b): same pass over `| CaseName of …`; align `of` to longest
  case name.
- Short match arm `->` alignment (section 2c): within a `match` block where ALL bodies are inline
  (no multi-line arm), align `->` to the longest pattern.
- `let` binding group `=` alignment (section 2d): consecutive `let` bindings with no blank line
  between them form a group; align `=` within each group.
- Record literal `=` alignment (section 7): multi-line `{ Field = value }` construction; align `=`
  within each literal.
- CE binding `=` alignment (section 13): `let!` / `and!` chains; align `=` within each CE block.

**Implementation path:**

- F# console app packaged as a dotnet tool in `Meta/EggShellFmt/`
- `<PackAsTool>true</PackAsTool>` -- installs via `dotnet tool install`, zero Node.js
- Parses input with `FSharp.Compiler.Services` (FCS) to get the concrete syntax tree
- Walks the CST to locate the target constructs above
- Emits the modified source with padding applied (text-level rewrite on top of the CST; Fantomas
  already handled the pretty-printing pass)

**Not a CI gate.** Runs only when explicitly invoked.
