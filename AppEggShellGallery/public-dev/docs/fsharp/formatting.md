# F# Code Formatting Conventions

> **Status:** canonical style reference for EggShell framework code (Lib\*, LibUi\*, LibRouter,
> LibAutoUi, LibLifeCycleUi, ThirdParty, Meta/\*, Suite\*, App\*). Derived from the established
> codebase baseline. Use this as the source-of-truth when reviewing PRs or reformatting code.
>
> **Formatter note:** `eggshell-fmt` (`Meta/EggShellFmt/`, tool command `eggshell-fmt`) is the
> **sole** formatter -- Fantomas has been retired. It normalizes whitespace and applies the column
> alignment rules below; the shapes it cannot enforce (line reflow, bracket placement, operator
> spacing inside expressions) are maintained by hand and PR review. See section 16.

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

1. `./Meta/EggShellFmt/install.sh` from the solution root -- packs `eggshell-fmt` into the
   git-ignored local feed and registers it in `.config/dotnet-tools.json`. Required once per machine
   (re-run after a version bump); the build-time check calls `dotnet tool run eggshell-fmt` which
   resolves from there.
2. Editor: there is no IDE formatter plugin. Run the tool from the terminal (or wire it as an
   on-save external tool / pre-commit hook) after editing.
3. Done. Every build now warns on formatting violations. To fix:
   `dotnet tool run eggshell-fmt -- <file.fs>`.

To silence a project during active work: `<EggShellFmtSeverity>none</EggShellFmtSeverity>` in its
`.fsproj`. Remove when done.

The alignment rules (sections 2a-2d, 7, 13) are applied by `eggshell-fmt` (section 16b); the shapes
it does not touch (line reflow, bracket placement, operator spacing) stay a hand + PR-review concern.

---

## 1. Indentation

**Rule:** 4 spaces per level. No tabs anywhere.

**Formatter:** `eggshell-fmt` normalizes leading tabs to 4 spaces; `.editorconfig` sets `indent_size = 4`.

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

Column alignment is used in **type definitions**, **`let` binding groups**, **match case arms**, and
**consecutive single-pipe statements** when it significantly aids readability.

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

### 2e. Consecutive pipe statements — align `|>`

When two or more consecutive statements at the same indent are single-pipe expressions
(`expr |> f`), pad the shorter left-hand sides so the `|>` line up:

```fsharp
// CORRECT — |> aligned
key           |> ignore
children      |> ignore
xLegacyStyles |> ignore

// WRONG — ragged
key |> ignore
children |> ignore
xLegacyStyles |> ignore
```

A group ends at a blank line or a non-pipe line. This applies only to single-pipe *statements*;
it does NOT apply to a multi-line pipeline whose steps start with a leading `|>` (see section 10).

---

## 3. Line length and line breaks

**Soft limit:** ~120 characters. Lines inside computation expressions (`codec`, `async`, `task`) may
exceed this when the alternative is an unreadable multi-line break.

**Hard limit:** none enforced by the formatter here, but prefer line breaks over very long lines in
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

Consecutive `abstract [member] Name: Type` declarations in an interface align their type annotations,
same rule as record fields (section 2a):

```fsharp
type MultiTouchGestureState =
    abstract initialCenterClientX: float with get, set
    abstract initialWidth:         float with get, set
    abstract angle:                float with get, set
```

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
`.eggshellfmtignore` at the solution root -- no per-project config needed for those.

**Autogenerated files** (`_autogenerated_/`) and build output (`obj/`, `bin/`) are skipped by the
tool automatically, so they are never checked or reformatted.

**`.config/dotnet-tools.json`** at the solution root pins `eggshell-fmt`. Run
`./Meta/EggShellFmt/install.sh` once before building; the MSBuild target calls
`dotnet tool run eggshell-fmt` which resolves from there.

---

Coverage breakdown: what the tooling covers and what stays a hand + PR-review concern.

| Convention | EditorConfig | eggshell-fmt | By hand |
|---|---|---|---|
| 4-space indent, no tabs | hint | tabs to spaces | -- |
| Trailing whitespace / final newline / LF | hint | enforced | -- |
| Operator spacing | -- | -- | **yes** |
| Blank lines between defs | -- | -- | **yes** |
| Line length limit | hint | -- | **yes** |
| Bracket / brace placement | -- | record-brace reflow only | **yes** (general) |
| Signature `+8` param indent | -- | -- | **yes** |
| Record field `:` alignment (2a) | -- | enforced | -- |
| Union case `of` alignment (2b) | -- | enforced | -- |
| Match arm `->` alignment (2c) | -- | enforced | -- |
| `let` group `=` alignment (2d) | -- | enforced (opt-in) | -- |
| Record literal `=` alignment (7) | -- | enforced | -- |
| CE binding `=` alignment (13) | -- | enforced (opt-in) | -- |

`eggshell-fmt` is the sole formatter. It does not run as a CI gate or pre-commit hook by default
(the build emits a warning); run it explicitly, or wire it as a pre-commit / on-save hook yourself.

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

### 16b. EggShellFmt -- the sole formatter (`Meta/EggShellFmt`, tool command `eggshell-fmt`)

A dotnet local tool (F#, no JS, no Node.js). It is the only formatter for this repo (Fantomas has
been retired). Pinned in `.config/dotnet-tools.json`; the package is built into a git-ignored local
feed (`Meta/EggShellFmt/nupkg`, wired in `nuget.config`).

```bash
./Meta/EggShellFmt/install.sh                            # one-time per machine (packs + installs)
dotnet tool run eggshell-fmt -- LibClient/src            # format in place
dotnet tool run eggshell-fmt -- --check LibClient/src    # CI: exit 3 if anything would change
```

**Safe whitespace normalization** (always, every level): leading tabs to 4 spaces, strip trailing
whitespace, CRLF/CR to LF, single trailing newline.

**Column alignment:** 2a (record `:`), 2b (DU `of`), 2c (match `->`, only when all arms inline), 2e
(consecutive single-pipe statements align `|>`), and 7 (record literal `=`). The `=` binding-group
rules **2d (let/and) and 13 (CE let!/and!) are opt-in** by default: a group is aligned only when the
author already aligned it (>1 space before an `=`). Multi-line named arguments and parameters
(`name = value,` / `name: Type,`) are aligned like record fields. It masks strings/comments before
scanning (never edits inside them) and is idempotent.

**What it does NOT do** (keep canonical by hand -- there is no second formatter): reflow long lines,
break signatures, general bracket placement, operator spacing inside expressions. See the
`fsharp-format` skill.

**Aggressiveness levels** (`--level`, default `standard`): `whitespace|0` (normalization only),
`conservative|1` (tolerance 12), `standard|2` (tolerance 24), `aggressive|3` (forces let/CE and aligns
outliers). The tool **never degrades existing alignment** -- a block the author already aligned is kept
aligned exactly, at any level. When *newly* aligning a ragged block, levels 1-2 apply **aesthetic
relaxation** consistent with section 2's "when it significantly aids readability": a member whose left
part is far longer than the rest (one very long field name, or `| _ ->` beside a long pattern) is
treated as an outlier -- kept at a single space so it neither aligns nor drags the block out, while the
rest still align. **Match arms (2c) are exempt from relaxation**: their `->` always align in full, so a
long guarded pattern (`… when … ->`) still lines its arrow up with the short arms. Multi-line
string/comment literals (including regular and verbatim strings that span lines) are never edited.

**Record brace normalization** (structural levels, `--no-brace` to disable): the leading-brace style
(`{ Field` sharing the first field's line) is reflowed to the canonical 2a/7 form -- `{` onto the
`type X =` / `let x =` line, fields at +4, `}` on its own line -- then fields are aligned. Only records
introduced by a line ending in `=`; skips record-updates (`{ x with`), object expressions, anonymous
records, inline records, and blocks touching a multi-line string. Nested records handled. General
bracket placement and line reflow remain out of scope.

**Ignore files:** a gitignore-style `.eggshellfmtignore` in the working directory excludes
files/globs; `--no-ignore` bypasses it. The tool also always skips `obj/`, `bin/`, `_autogenerated_/`,
`node_modules/`, `.git/`, and `*.fs.js`.

Implementation is line-based (masking + indent-scoped block detection). Every structural scan runs on
a masked copy of the line where string/char literals and comments are blanked to spaces
(length-preserving), so markers are never matched inside them and edits apply to the real line at the
same indices.

**Not a CI gate by default.** The MSBuild check emits a warning (per `EggShellFmtSeverity`); the tool
otherwise runs only when explicitly invoked. Wire it as a pre-commit / on-save hook if you want it
enforced automatically.

---

### 16c. Editor integration

There is no bundled IDE formatter plugin. Run `eggshell-fmt` from the terminal after editing, or wire
it yourself as an editor "on save" external tool or a git pre-commit hook. `.editorconfig` (16a) still
drives as-you-type behavior (indent, final newline, trailing whitespace) in VS Code, Rider, and most
editors, so most whitespace stays correct before the tool ever runs.
