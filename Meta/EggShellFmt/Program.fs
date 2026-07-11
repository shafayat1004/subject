// EggShellFmt -- EggShell F# column-alignment formatter (dotnet tool: eggshell-fmt).
//
// Applies the hand-maintained alignment rules from
// AppEggShellGallery/public-dev/docs/fsharp/formatting.md that Fantomas cannot do:
//
//   2a  record TYPE fields    -> align the type after `:`   (Name: Type)
//   2b  DU cases              -> align the `of` keyword     (| Case of ...)
//   2c  short match arms      -> align `->`  (only when EVERY arm is inline)
//   2d  let / and binding grp -> align `=`   (OPT-IN: only groups already aligned)
//   7   record LITERAL fields -> align `=`   (Name = value)
//   13  CE let! / and! binds  -> align `=`   (OPT-IN, same as 2d)
//
// Plus safe normalization: leading tabs -> 4 spaces, strip trailing whitespace,
// CRLF/CR -> LF, single trailing newline.
//
// It does NOT reflow code, break long lines, or fix operator spacing inside
// expressions -- run Fantomas for those. This tool is the alignment pass Fantomas
// skips, and stands alone (it never calls Fantomas).
//
// Reliability: every structural scan runs on a MASKED copy of the line where the
// bytes of string/char literals and comments are blanked to spaces, so markers are
// never matched inside strings/comments; edits apply to the real line at the same
// indices (mask is length-preserving). Lines inside a multi-line string/block
// comment are left byte-for-byte. Idempotent.

module EggShellFmt.Program

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

let NORMAL = 0
let IN_TRIPLE = 1
let IN_BLOCK = 2

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
/// masked has the same length as line.
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
            // NORMAL
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
                        if sub i 2 = "\"\"" then
                            out.Append("  ") |> ignore; i <- i + 2
                        else
                            out.Append(' ') |> ignore; i <- i + 1; go <- false
                    else
                        out.Append(' ') |> ignore; i <- i + 1
            elif c = '"' then
                out.Append(' ') |> ignore
                i <- i + 1
                let mutable go = true
                while go && i < n do
                    if line.[i] = '\\' && i + 1 < n then
                        out.Append("  ") |> ignore; i <- i + 2
                    elif line.[i] = '"' then
                        out.Append(' ') |> ignore; i <- i + 1; go <- false
                    else
                        out.Append(' ') |> ignore; i <- i + 1
            elif c = '\'' then
                // char literal '\x' or 'x'  vs  tick in ident (foo') or generic ('T)
                if sub i 2 = "'\\" then
                    let k = line.IndexOf('\'', i + 2)
                    if k <> -1 && k - i <= 8 then
                        out.Append(String(' ', k - i + 1)) |> ignore; i <- k + 1
                    else
                        out.Append(c) |> ignore; i <- i + 1
                elif i + 2 < n && line.[i + 2] = '\'' then
                    out.Append("   ") |> ignore; i <- i + 3
                else
                    out.Append(c) |> ignore; i <- i + 1
            else
                out.Append(c) |> ignore; i <- i + 1
    out.ToString(), state

let indentOf (line: string) = line.Length - line.TrimStart(' ').Length

/// Index of the first standalone ` = ` (assignment/field), or -1.
let findAssignEq (masked: string) =
    let mutable res = -1
    let mutable k = 1
    while res = -1 && k < masked.Length - 1 do
        if masked.[k] = '=' && masked.[k - 1] = ' ' && masked.[k + 1] = ' ' then res <- k
        k <- k + 1
    res

/// Index of a type-annotation `:` (not :: := :> :?), or -1.
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

/// Classify a line: "pipe" | "let" | "field_eq" | "field_colon" | None.
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

// Alignment helpers. Each mutates real.[idx] for idx in run.

let alignMarker (real: string[]) (masked: string[]) (run: int list)
                (findIdx: string -> int) (markerWidth: int) (marker: string)
                (attachToLeft: bool) =
    // attachToLeft=true  -> ": " style: colon stays on the name, align the value.
    // attachToLeft=false -> " = "/" of "/" -> " style: align the marker itself.
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
    let w = parts |> List.map (fun (_, l, _) -> l.Length) |> List.max
    for (idx, left, right) in parts do
        if attachToLeft then
            real.[idx] <- left.PadRight(w + 1) + right
        else
            real.[idx] <- left.PadRight(w) + marker + right

let alignEq real masked run = alignMarker real masked run findAssignEq 1 " = " false
let alignColon real masked run = alignMarker real masked run findFieldColon 1 ": " true
let alignOf real masked run = alignMarker real masked run findOf 4 " of " false
let alignArrow real masked run = alignMarker real masked run findArrow 4 " -> " false

/// A `let`/CE group is only aligned when the author already aligned it, i.e. at
/// least one binding has more than one space before its `=` (opt-in, rule 2d/13).
let letGroupAlreadyAligned (real: string[]) (masked: string[]) (run: int list) =
    run |> List.exists (fun idx ->
        let k = findAssignEq masked.[idx]
        k > 0 && k - (real.[idx].Substring(0, k).TrimEnd().Length) > 1)

let handlePipe (real: string[]) (masked: string[]) (run: int list) =
    let ofLines = run |> List.filter (fun i -> findOf masked.[i] <> -1)
    let inlineArrows =
        run |> List.filter (fun i ->
            let a = findArrow masked.[i]
            a <> -1 && hasContentAfter real.[i] a 4)
    if List.isEmpty ofLines && not (List.isEmpty inlineArrows) then
        // match block: align only when EVERY arm is an inline arrow (rule 2c)
        if List.length inlineArrows = List.length run then alignArrow real masked run
    elif not (List.isEmpty ofLines) && List.isEmpty inlineArrows then
        alignOf real masked ofLines
    // else mixed/ambiguous (e.g. DU case with a function-typed payload) -> leave alone

let formatSource (src0: string) : string =
    let src = src0.Replace("\r\n", "\n").Replace("\r", "\n")
    let raw = src.Split('\n')
    let n = raw.Length

    // start state per line + protected flag (line starts inside a string/comment)
    let startState = Array.zeroCreate n
    let mutable st = NORMAL
    for i in 0 .. n - 1 do
        startState.[i] <- st
        let _, s = maskLine raw.[i] st
        st <- s
    let protectedLine = Array.init n (fun i -> startState.[i] <> NORMAL)

    // Pass 1: safe per-line normalization (unprotected lines only).
    let real = Array.copy raw
    for i in 0 .. n - 1 do
        if not protectedLine.[i] then
            let stripped = real.[i].TrimStart('\t', ' ')
            let lead = real.[i].Substring(0, real.[i].Length - stripped.Length).Replace("\t", "    ")
            real.[i] <- (lead + stripped).TrimEnd()

    // Re-mask after normalization (indent widths may have shifted).
    let masked = Array.zeroCreate n
    st <- NORMAL
    for i in 0 .. n - 1 do
        startState.[i] <- st
        let m, s = maskLine real.[i] st
        masked.[i] <- m
        st <- s

    // Pass 2: alignment runs.
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
                    if classify masked.[j] real.[j] = Some kind then
                        run.Add j
                        j <- j + 1
                    else
                        go <- false
                let runL = List.ofSeq run
                match kind with
                | "let" -> if letGroupAlreadyAligned real masked runL then alignEq real masked runL
                | "field_eq" -> alignEq real masked runL
                | "field_colon" -> alignColon real masked runL
                | "pipe" -> handlePipe real masked runL
                | _ -> ()
                i <- j

    (String.Join("\n", real)).TrimEnd('\n') + "\n"

let rec enumerateFs (path: string) : seq<string> =
    seq {
        if Directory.Exists path then
            let name = Path.GetFileName path
            if name <> "obj" && name <> "bin" && name <> "node_modules" && name <> ".git"
               && not (path.Contains "_autogenerated_") then
                for d in Directory.GetDirectories path do yield! enumerateFs d
                for f in Directory.GetFiles path do
                    if f.EndsWith ".fs" && not (f.EndsWith ".fs.js") then yield f
        elif File.Exists path then
            yield path
    }

[<EntryPoint>]
let main argv =
    let check = argv |> Array.contains "--check"
    let quiet = argv |> Array.contains "--quiet"
    let paths = argv |> Array.filter (fun a -> not (a.StartsWith "--"))
    if paths.Length = 0 then
        eprintfn "usage: eggshell-fmt [--check] [--quiet] <file.fs | dir> ..."
        1
    else
        let mutable changed = 0
        for path in paths do
            for file in enumerateFs path do
                try
                    let bytes = File.ReadAllBytes file
                    let hasBom = bytes.Length >= 3 && bytes.[0] = 0xEFuy && bytes.[1] = 0xBBuy && bytes.[2] = 0xBFuy
                    let text =
                        if hasBom then Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
                        else Encoding.UTF8.GetString bytes
                    let formatted = formatSource text
                    if formatted <> text then
                        changed <- changed + 1
                        if check then printfn "would reformat %s" file
                        else
                            let enc = Encoding.UTF8.GetBytes formatted
                            let outBytes =
                                if hasBom then Array.append [| 0xEFuy; 0xBBuy; 0xBFuy |] enc else enc
                            File.WriteAllBytes(file, outBytes)
                            printfn "reformatted %s" file
                    elif not quiet && not check then
                        printfn "unchanged %s" file
                with ex ->
                    eprintfn "skip %s: %s" file ex.Message
        if check && changed > 0 then 3 else 0
