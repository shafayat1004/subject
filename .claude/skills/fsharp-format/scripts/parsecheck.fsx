// Validate that a whitespace-only change (e.g. an eggshell-fmt sweep) introduced NO new parse
// errors, by comparing each file's current parse against its HEAD version. Alignment is
// whitespace-only, so the complete safety net is a PARSE check (not a full typecheck): the only
// way the formatter can break code is at parse level (brace reflow / offside), and that is exactly
// what this catches. This session the brace-normalization bug produced FS0010 that a plain build of
// a Fable-only lib would have masked; a parse-check-vs-HEAD flags it immediately across the sweep.
//
// Usage:
//   dotnet fsi parsecheck.fsx <file-or-dir> [<file-or-dir> ...]
//   # or feed a newline-separated file list on stdin:
//   git diff --name-only | grep '\.fs$' | dotnet fsi parsecheck.fsx -
//
// Exit 0 = no introduced parse errors; exit 1 = at least one file parses now but did NOT before
// (or vice versa is ignored — pre-existing errors, e.g. conditional-compilation false positives,
// are not counted). Compares against `git show HEAD:<path>`.

#r "nuget: FSharp.Compiler.Service, 43.9.300"
open System.IO
open System.Diagnostics
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharp.Compiler.Diagnostics

let checker = FSharpChecker.Create()

let parseErrs (path: string) (text: string) =
    let src = SourceText.ofString text
    let opts, _ = checker.GetParsingOptionsFromCommandLineArgs [ path ]
    let res = checker.ParseFile(path, src, opts) |> Async.RunSynchronously
    res.Diagnostics |> Array.filter (fun d -> d.Severity = FSharpDiagnosticSeverity.Error)

let headText (rel: string) =
    let psi = ProcessStartInfo("git", sprintf "show HEAD:%s" rel)
    psi.RedirectStandardOutput <- true
    psi.UseShellExecute <- false
    use p = Process.Start psi
    let o = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    if p.ExitCode = 0 then Some o else None

let args = System.Environment.GetCommandLineArgs() |> Array.skip 2 |> Array.toList

let files =
    let fromArgs =
        args
        |> List.filter (fun a -> a <> "-")
        |> List.collect (fun a ->
            if Directory.Exists a then
                Directory.GetFiles(a, "*.fs", SearchOption.AllDirectories)
                |> Array.filter (fun f -> not (f.EndsWith ".fs.js"))
                |> Array.toList
            elif File.Exists a then [ a ]
            else [])
    let fromStdin =
        if args |> List.contains "-" then
            let mutable acc = []
            let mutable line = System.Console.In.ReadLine()
            while line <> null do
                if line.Trim() <> "" then acc <- line.Trim() :: acc
                line <- System.Console.In.ReadLine()
            List.rev acc
        else []
    (fromArgs @ fromStdin) |> List.distinct

let mutable introduced = 0
let mutable preexisting = 0
for path in files do
    let now = parseErrs path (File.ReadAllText path)
    if now.Length > 0 then
        let headBad =
            match headText path with
            | Some t -> (parseErrs path t).Length > 0
            | None -> false // new file: treat its errors as introduced
        if headBad then
            preexisting <- preexisting + 1
        else
            introduced <- introduced + 1
            printfn "INTRODUCED PARSE ERROR: %s" path
            now |> Array.truncate 3 |> Array.iter (fun d -> printfn "   L%d C%d: %s" d.StartLine d.StartColumn d.Message)

printfn "RESULT files=%d introduced=%d preexisting(ignored)=%d" files.Length introduced preexisting
exit (if introduced > 0 then 1 else 0)
