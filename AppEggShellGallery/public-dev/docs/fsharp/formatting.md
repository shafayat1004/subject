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
16. [Fantomas .fantomasrc reference](#16-fantomas-fantomasrc-reference)

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

Coverage breakdown: what each tool covers and what it can't touch.

| Convention | EditorConfig | Fantomas | Rider | EggShellFmt (future) |
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

**Enforcement is opt-in.** None of these tools run as a CI gate or pre-commit hook by default.
Run them when you want; the alignment conventions are maintained by discipline and PR review.

---

### 16a. EditorConfig (always-on hints)

`.editorconfig` at the repository root. Picked up automatically by VS Code, Rider, and most
editors. Does not auto-format -- it tells the editor how to behave as you type.

```ini
[*.fs]
indent_style  = space
indent_size   = 4
end_of_line   = lf
charset       = utf-8-bom
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

Create `.fantomasrc.json` at the repository root:

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
Run on a directory: `dotnet fantomas <dir/>`
Check without writing: `dotnet fantomas --check <file.fs>`

---

### 16c. Rider IDE

Rider 2023+ uses Fantomas as its F# formatter engine, so the `.fantomasrc.json` above is the
primary configuration. Rider picks it up automatically from the repository root.

**Settings to verify in Rider** (Settings > Editor > Code Style > F#):

- Formatter: confirm it shows "Fantomas" (not "Built-in"). If not, install the Fantomas plugin
  from the JetBrains Marketplace.
- The Fantomas version Rider bundles may differ from the repo's `dotnet-tools.json` version.
  To use the repo's pinned version: Settings > Tools > Fantomas > "Use custom Fantomas tool"
  and point it at `dotnet fantomas` (resolved via `dotnet-tools.json`).

**Reformat shortcut:**

| OS | Shortcut |
|---|---|
| macOS | `Cmd + Alt + L` |
| Windows / Linux | `Ctrl + Alt + L` |

**Reformat on save (opt-in):** Settings > Tools > Actions on Save > "Reformat code" -- enable only
for F# files if desired. Off by default.

**Alignment live templates:** Rider's "Live Templates" (Settings > Editor > Live Templates) can be
used to insert pre-aligned boilerplate (e.g., a 4-field record type stub with `:` signs already
column-aligned). These are per-developer and not committed to the repo.

---

### 16d. EggShellFmt (future -- `Meta/EggShellFmt`)

A planned opt-in CLI post-processor that covers the alignment rules Fantomas cannot. The intended
workflow:

```
dotnet fantomas <file.fs>        # normalize spacing/indentation/brackets
eggshell fmt <file.fs>           # apply column alignment on top
```

Or as a single command: `eggshell fmt --full <file.fs>` (runs Fantomas internally first).

**Scope of what it would enforce:**

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

- F# console app in `Meta/EggShellFmt/`
- Parses input with `FSharp.Compiler.Services` (FCS) to get the concrete syntax tree
- Walks the CST to locate the target constructs above
- Emits the modified source with padding applied (text-level rewrite on top of the CST, not a
  full pretty-printer -- Fantomas already handled the pretty-printing pass)
- Registered as an `eggshell` subcommand via the existing CLI plugin mechanism

**Not a CI gate.** Runs only when explicitly invoked. The goal is "run once on a file when you're
done, same as running Fantomas."
