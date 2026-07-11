// EggShellFmt -- EggShell F# column-alignment formatter (dotnet tool: eggshell-fmt).
//
// Applies the hand-maintained alignment rules from
// AppEggShellGallery/public-dev/docs/fsharp/formatting.md that Fantomas cannot do:
//
//   2a  record TYPE fields    -> align the type after `:`   (Name: Type)
//   2b  DU cases              -> align the `of` keyword     (| Case of ...)
//   2c  short match arms      -> align `->`  (only when EVERY arm is inline)
//   2d  let / and binding grp -> align `=`
//   7   record LITERAL fields -> align `=`   (Name = value)
//   13  CE let! / and! binds  -> align `=`
//
// Plus safe normalization: leading tabs -> 4 spaces, strip trailing whitespace,
// CRLF/CR -> LF, single trailing newline.
//
// AGGRESSIVENESS LEVELS (--level):
//   whitespace | 0  whitespace normalization only, no alignment
//   conservative | 1  structural alignment, strong aesthetic relaxation; let/CE opt-in
//   standard | 2  (default) structural alignment, moderate relaxation; let/CE opt-in
//   aggressive | 3  align everything incl. outliers; let/CE forced
//
// AESTHETIC RELAXATION: within an alignment group, a member whose left part is far
// longer than the rest (e.g. one very long record field name) is treated as an
// outlier and NOT allowed to drag the whole block out -- it keeps a single space
// while the remaining members align. This follows the spec's "align ... when it
// significantly aids readability". The tolerance grows with the level.
//
// IGNORE FILE: a gitignore-style `.eggshellfmtignore` (and `.fantomasignore`, if
// present) in the working directory excludes files/globs from formatting.
//
// Fantomas complements this tool (spacing/indent/brackets/line-breaks); this tool
// never calls Fantomas. Reliability: every structural scan runs on a MASKED copy
// of the line where string/char literals and comments are blanked to spaces, so
// markers are never matched inside them; edits apply at the same indices (mask is
// length-preserving). Lines inside a multi-line string/block comment are left
// byte-for-byte. Idempotent.

module EggShellFmt.Program

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

let NORMAL = 0
let IN_TRIPLE = 1
let IN_BLOCK = 2

type LetMode =
    | Off
    | OptIn
    | Force

type Config = {
    AlignStructural: bool
    Tolerance:       int
    LetMode:         LetMode
}

let configForLevel (level: string) : Config option =
    match level.ToLowerInvariant() with
    | "whitespace" | "0" -> Some { AlignStructural = false; Tolerance = 0;      LetMode = Off }
    | "conservative" | "1" -> Some { AlignStructural = true; Tolerance = 6;      LetMode = OptIn }
    | "standard" | "2" -> Some { AlignStructural = true; Tolerance = 12;     LetMode = OptIn }
    | "aggressive" | "3" -> Some { AlignStructural = true; Tolerance = 100000; LetMode = Force }
    | _ -> None

let excl =
    set [
        "if"; "elif"; "else"; "then"; "while"; "for"; "do"; "done"; "match"; "with"
        "fun"; "function"; "type"; "module"; "namespace"; "open"; "member"; "override"
        "abstract"; "default"; "inherit"; "new"; "when"; "in"; "yield"; "return"; "use"
        "try"; "finally"; "begin"; "end"; "mutable"; "rec"; "internal"; "private"
        "public"; "val"; "interface"; "class"; "struct"; "enum"; "exception"; "of"
        "downto"; "to"; "lazy"; "assert"; "upcast"; "downcast"; "not"; "true"; "false"
        "null"; "base"; "global"; "static"
    ]

/// Mask string/char/comment bytes to spaces. Returns (maskedLine, stateAfter).
let maskLine (line: string) (state0: int) : string * int =
    let n = line.Length
    let out = StringBuilder(n)
    let mutable i = 0
    let mutable state = state0
    let sub (a: int) (len: int) = if a + len <= n then line.Substring(a, len) else line.Substring(a)
    while i < n do
        let c = line.[i]
        if state = IN_BLOCK then
            if c = '*' && sub i 2 = "*)" then
                out.Append("  ") |> ignore; i <- i + 2; state <- NORMAL
            else
                out.Append(' ') |> ignore; i <- i + 1
        elif state = IN_TRIPLE then
            if c = '"' && sub i 3 = "\"\"\"" then
                out.Append("   ") |> ignore; i <- i + 3; state <- NORMAL
            else
                out.Append(' ') |> ignore; i <- i + 1
        else
            if c = '"' && sub i 3 = "\"\"\"" then
                out.Append("   ") |> ignore; i <- i + 3; state <- IN_TRIPLE
            elif c = '/' && sub i 2 = "//" then
                out.Append(String(' ', n - i)) |> ignore; i <- n
            elif c = '(' && sub i 2 = "(*" then
                out.Append("  ") |> ignore; i <- i + 2; state <- IN_BLOCK
            elif c = '@' && sub i 2 = "@\"" then
                out.Append("  ") |> ignore
                i <- i + 2
                let mutable go = true
                while go && i < n do
                    if line.[i] = '"' then
                        if sub i 2 = "\"\"" then out.Append("  ") |> ignore; i <- i + 2
                        else out.Append(' ') |> ignore; i <- i + 1; go <- false
                    else out.Append(' ') |> ignore; i <- i + 1
            elif c = '"' then
                out.Append(' ') |> ignore
                i <- i + 1
                let mutable go = true
                while go && i < n do
                    if line.[i] = '\\' && i + 1 < n then out.Append("  ") |> ignore; i <- i + 2
                    elif line.[i] = '"' then out.Append(' ') |> ignore; i <- i + 1; go <- false
                    else out.Append(' ') |> ignore; i <- i + 1
            elif c = '\'' then
                if sub i 2 = "'\\" then
                    let k = line.IndexOf('\'', i + 2)
                    if k <> -1 && k - i <= 8 then out.Append(String(' ', k - i + 1)) |> ignore; i <- k + 1
                    else out.Append(c) |> ignore; i <- i + 1
                elif i + 2 < n && line.[i + 2] = '\'' then out.Append("   ") |> ignore; i <- i + 3
                else out.Append(c) |> ignore; i <- i + 1
            else
                out.Append(c) |> ignore; i <- i + 1
    out.ToString(), state

let indentOf (line: string) = line.Length - line.TrimStart(' ').Length

let findAssignEq (masked: string) =
    let mutable res = -1
    let mutable k = 1
    while res = -1 && k < masked.Length - 1 do
        if masked.[k] = '=' && masked.[k - 1] = ' ' && masked.[k + 1] = ' ' then res <- k
        k <- k + 1
    res

let findFieldColon (masked: string) =
    let mutable res = -1
    let mutable k = 1
    while res = -1 && k < masked.Length do
        if masked.[k] = ':' && masked.[k - 1] <> ':' then
            let nxt = if k + 1 < masked.Length then masked.[k + 1] else ' '
            if nxt <> ':' && nxt <> '>' && nxt <> '?' && nxt <> '=' then res <- k
        k <- k + 1
    res

let findOf (masked: string) = masked.IndexOf(" of ")
let findArrow (masked: string) = masked.IndexOf(" -> ")

let hasContentAfter (real: string) (idx: int) (width: int) =
    idx + width <= real.Length && real.Substring(idx + width).Trim() <> ""

let firstWord (mb: string) =
    let m = Regex.Match(mb, @"^[A-Za-z_][\w']*")
    if m.Success then m.Value else ""

let classify (masked: string) (real: string) : string option =
    let mb = masked.Trim()
    if mb = "" then None
    elif mb.[0] = '|' && not (mb.StartsWith "|>") && (mb.Length = 1 || mb.[1] = ' ') then
        Some "pipe"
    elif Regex.IsMatch(mb, @"^(let|and)!?(\s|$)") then
        let eqk = findAssignEq masked
        if eqk <> -1 && hasContentAfter real eqk 1 then Some "let" else None
    else
        let fw = firstWord mb
        if fw <> "" && not (excl.Contains fw) then
            let trailingComma = masked.TrimEnd().EndsWith ","
            let eqk = findAssignEq masked
            if eqk <> -1 && hasContentAfter real eqk 1 && not trailingComma then
                Some "field_eq"
            else
                let ck = findFieldColon masked
                if ck <> -1 && (eqk = -1 || ck < eqk) && hasContentAfter real ck 1 && not trailingComma then
                    Some "field_colon"
                else None
        else None

/// Align a marker across a run, with aesthetic relaxation: the alignment column is
/// the widest left part that is within `tolerance` of the shortest -- members longer
/// than that are outliers and keep a single space (they are not dragged into line,
/// and they do not drag the block out).
let alignMarker (real: string[]) (masked: string[]) (run: int list)
                (findIdx: string -> int) (markerWidth: int) (marker: string)
                (attachToLeft: bool) (tolerance: int) =
    let parts =
        run |> List.map (fun idx ->
            let k = findIdx masked.[idx]
            if attachToLeft then
                let left = real.[idx].Substring(0, k).TrimEnd() + marker.TrimEnd()
                let right = real.[idx].Substring(k + markerWidth).TrimStart()
                idx, left, right
            else
                let left = real.[idx].Substring(0, k).TrimEnd()
                let right = real.[idx].Substring(k + markerWidth).TrimStart()
                idx, left, right)
    let widths = parts |> List.map (fun (_, l, _) -> l.Length)
    let minW = List.min widths
    let threshold = minW + tolerance
    let cluster = widths |> List.filter (fun w -> w <= threshold)
    let alignCol = if List.isEmpty cluster then List.max widths else List.max cluster
    for (idx, left, right) in parts do
        if attachToLeft then
            if left.Length <= alignCol then real.[idx] <- left.PadRight(alignCol + 1) + right
            else real.[idx] <- left + " " + right
        else
            if left.Length <= alignCol then real.[idx] <- left.PadRight(alignCol) + marker + right
            else real.[idx] <- left + marker + right

let alignEq real masked run tol = alignMarker real masked run findAssignEq 1 " = " false tol
let alignColon real masked run tol = alignMarker real masked run findFieldColon 1 ": " true tol
let alignOf real masked run tol = alignMarker real masked run findOf 4 " of " false tol
let alignArrow real masked run tol = alignMarker real masked run findArrow 4 " -> " false tol

/// A `let`/CE group counts as "already aligned" (opt-in signal) when at least one
/// binding has more than one space before its `=`.
let letGroupAlreadyAligned (real: string[]) (masked: string[]) (run: int list) =
    run |> List.exists (fun idx ->
        let k = findAssignEq masked.[idx]
        k > 0 && k - (real.[idx].Substring(0, k).TrimEnd().Length) > 1)

let handlePipe (real: string[]) (masked: string[]) (run: int list) (tol: int) =
    let ofLines = run |> List.filter (fun i -> findOf masked.[i] <> -1)
    let inlineArrows =
        run |> List.filter (fun i ->
            let a = findArrow masked.[i]
            a <> -1 && hasContentAfter real.[i] a 4)
    if List.isEmpty ofLines && not (List.isEmpty inlineArrows) then
        if List.length inlineArrows = List.length run then alignArrow real masked run tol
    elif not (List.isEmpty ofLines) && List.isEmpty inlineArrows then
        alignOf real masked ofLines tol
    // else mixed/ambiguous (e.g. DU case with a function-typed payload) -> leave alone

let formatSource (cfg: Config) (src0: string) : string =
    let src = src0.Replace("\r\n", "\n").Replace("\r", "\n")
    let raw = src.Split('\n')
    let n = raw.Length

    let startState = Array.zeroCreate n
    let mutable st = NORMAL
    for i in 0 .. n - 1 do
        startState.[i] <- st
        let _, s = maskLine raw.[i] st
        st <- s
    let protectedLine = Array.init n (fun i -> startState.[i] <> NORMAL)

    // Pass 1: safe per-line normalization.
    let real = Array.copy raw
    for i in 0 .. n - 1 do
        if not protectedLine.[i] then
            let stripped = real.[i].TrimStart('\t', ' ')
            let lead = real.[i].Substring(0, real.[i].Length - stripped.Length).Replace("\t", "    ")
            real.[i] <- (lead + stripped).TrimEnd()

    let masked = Array.zeroCreate n
    st <- NORMAL
    for i in 0 .. n - 1 do
        startState.[i] <- st
        let m, s = maskLine real.[i] st
        masked.[i] <- m
        st <- s

    // Pass 2: alignment runs (skipped entirely at the whitespace level).
    if cfg.AlignStructural || cfg.LetMode <> Off then
        let mutable i = 0
        while i < n do
            if protectedLine.[i] || real.[i].Trim() = "" then
                i <- i + 1
            else
                match classify masked.[i] real.[i] with
                | None -> i <- i + 1
                | Some kind ->
                    let ind = indentOf real.[i]
                    let run = ResizeArray<int>()
                    run.Add i
                    let mutable j = i + 1
                    let mutable go = true
                    while go && j < n && not protectedLine.[j] && real.[j].Trim() <> "" && indentOf real.[j] = ind do
                        if classify masked.[j] real.[j] = Some kind then run.Add j; j <- j + 1
                        else go <- false
                    let runL = List.ofSeq run
                    match kind with
                    | "let" ->
                        let doIt =
                            match cfg.LetMode with
                            | Off -> false
                            | Force -> true
                            | OptIn -> letGroupAlreadyAligned real masked runL
                        if doIt then alignEq real masked runL cfg.Tolerance
                    | "field_eq" -> if cfg.AlignStructural then alignEq real masked runL cfg.Tolerance
                    | "field_colon" -> if cfg.AlignStructural then alignColon real masked runL cfg.Tolerance
                    | "pipe" -> if cfg.AlignStructural then handlePipe real masked runL cfg.Tolerance
                    | _ -> ()
                    i <- j

    (String.Join("\n", real)).TrimEnd('\n') + "\n"

// -------- ignore file (.eggshellfmtignore / .fantomasignore) --------

/// Translate one gitignore-style pattern to (isNegation, anchoredRegex).
let compilePattern (pat0: string) : (bool * Regex) option =
    let mutable pat = pat0
    let neg = pat.StartsWith "!"
    if neg then pat <- pat.Substring 1
    let dirOnly = pat.EndsWith "/"
    if dirOnly then pat <- pat.TrimEnd '/'
    if pat = "" then None
    else
        let anchored = pat.StartsWith "/" || pat.Contains "/"
        let core = if pat.StartsWith "/" then pat.Substring 1 else pat
        // Build regex, treating ** / * / ? specially and escaping the rest.
        let sb = StringBuilder()
        let mutable k = 0
        while k < core.Length do
            let c = core.[k]
            if c = '*' && k + 1 < core.Length && core.[k + 1] = '*' then
                sb.Append(".*") |> ignore; k <- k + 2
                if k < core.Length && core.[k] = '/' then k <- k + 1  // `**/` -> `.*`
            elif c = '*' then sb.Append("[^/]*") |> ignore; k <- k + 1
            elif c = '?' then sb.Append("[^/]") |> ignore; k <- k + 1
            else sb.Append(Regex.Escape(string c)) |> ignore; k <- k + 1
        let body = sb.ToString()
        // A match on a directory also matches everything under it: allow (/.*)?
        let full =
            if anchored then "^" + body + "(/.*)?$"
            else "(^|.*/)" + body + "(/.*)?$"
        Some(neg, Regex(full, RegexOptions.Compiled))

let loadIgnore (dir: string) : (bool * Regex) list =
    [ ".eggshellfmtignore"; ".fantomasignore" ]
    |> List.collect (fun name ->
        let p = Path.Combine(dir, name)
        if File.Exists p then
            File.ReadAllLines p
            |> Array.toList
            |> List.map (fun l -> l.Trim())
            |> List.filter (fun l -> l <> "" && not (l.StartsWith "#"))
            |> List.choose compilePattern
        else [])

let isIgnored (patterns: (bool * Regex) list) (relPath: string) : bool =
    let rel = relPath.Replace('\\', '/')
    let mutable ignored = false
    for (neg, rx) in patterns do
        if rx.IsMatch rel then ignored <- not neg
    ignored

let rec enumerateFs (patterns: (bool * Regex) list) (root: string) (path: string) : seq<string> =
    seq {
        if Directory.Exists path then
            let name = Path.GetFileName path
            let rel = Path.GetRelativePath(root, path)
            if name <> "obj" && name <> "bin" && name <> "node_modules" && name <> ".git"
               && not (path.Contains "_autogenerated_")
               && not (rel <> "." && isIgnored patterns rel) then
                for d in Directory.GetDirectories path do yield! enumerateFs patterns root d
                for f in Directory.GetFiles path do
                    if f.EndsWith ".fs" && not (f.EndsWith ".fs.js")
                       && not (isIgnored patterns (Path.GetRelativePath(root, f))) then
                        yield f
        elif File.Exists path then
            if not (isIgnored patterns (Path.GetRelativePath(root, path))) then yield path
    }

let usage () =
    eprintfn "usage: eggshell-fmt [--level <whitespace|conservative|standard|aggressive|0-3>]"
    eprintfn "                    [--check] [--quiet] [--no-ignore] <file.fs | dir> ..."

[<EntryPoint>]
let main argv =
    let check = argv |> Array.contains "--check"
    let quiet = argv |> Array.contains "--quiet"
    let noIgnore = argv |> Array.contains "--no-ignore"

    // --level <x>  (or -l <x>); default "standard"
    let levelArg =
        let idx = argv |> Array.tryFindIndex (fun a -> a = "--level" || a = "-l")
        match idx with
        | Some i when i + 1 < argv.Length -> argv.[i + 1]
        | _ -> "standard"

    match configForLevel levelArg with
    | None ->
        eprintfn "error: unknown --level '%s'" levelArg
        usage ()
        2
    | Some cfg ->
        // positional paths: not a flag, and not the value consumed by --level
        let levelValueIdx =
            match argv |> Array.tryFindIndex (fun a -> a = "--level" || a = "-l") with
            | Some i -> i + 1
            | None -> -1
        let paths =
            argv
            |> Array.mapi (fun i a -> i, a)
            |> Array.filter (fun (i, a) -> not (a.StartsWith "-") && i <> levelValueIdx)
            |> Array.map snd
        if paths.Length = 0 then
            usage ()
            1
        else
            let root = Directory.GetCurrentDirectory()
            let patterns = if noIgnore then [] else loadIgnore root
            let mutable changed = 0
            for path in paths do
                for file in enumerateFs patterns root path do
                    try
                        let bytes = File.ReadAllBytes file
                        let hasBom = bytes.Length >= 3 && bytes.[0] = 0xEFuy && bytes.[1] = 0xBBuy && bytes.[2] = 0xBFuy
                        let text =
                            if hasBom then Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
                            else Encoding.UTF8.GetString bytes
                        let formatted = formatSource cfg text
                        if formatted <> text then
                            changed <- changed + 1
                            if check then printfn "would reformat %s" file
                            else
                                let enc = Encoding.UTF8.GetBytes formatted
                                let outBytes = if hasBom then Array.append [| 0xEFuy; 0xBBuy; 0xBFuy |] enc else enc
                                File.WriteAllBytes(file, outBytes)
                                printfn "reformatted %s" file
                        elif not quiet && not check then
                            printfn "unchanged %s" file
                    with ex ->
                        eprintfn "skip %s: %s" file ex.Message
            if check && changed > 0 then 3 else 0
