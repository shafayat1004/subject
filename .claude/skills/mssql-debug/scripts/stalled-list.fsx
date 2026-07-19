#r "nuget: Microsoft.Data.SqlClient, 5.2.3"

open System
open Microsoft.Data.SqlClient
open System.Text.Json

let ensureConnectTimeout (conn: string) =
    if conn.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) then conn
    else conn + ";Connect Timeout=5"

let tryLocal (dto: DateTimeOffset option) =
    match dto with
    | Some d ->
        let local = d.ToLocalTime()
        sprintf "%s UTC / %s %s" (d.ToString("o")) (local.ToString("yyyy-MM-dd HH:mm:ss")) (local.ToString("zzz"))
    | None -> "NULL"

let prettyJson (raw: string) =
    try
        use doc = JsonDocument.Parse(raw)
        JsonSerializer.Serialize(doc, JsonSerializerOptions(WriteIndented = true))
    with _ -> raw

let run() =
    let args =
        Environment.GetCommandLineArgs()
        |> Array.skipWhile (fun a -> not (a.EndsWith("stalled-list.fsx")))
        |> Array.skip 1
        |> Array.toList

    let (eco, lc, kind) =
        match args with
        | eco :: rest ->
            let rec loop acc = function
                | [] -> List.rev acc
                | "--kind" :: k :: t -> loop ("--kind:" + k :: acc) t
                | h :: t -> loop (h :: acc) t
            let p = loop [] rest
            let lcOpt = p |> List.tryFind (fun x -> not (x.StartsWith("--")))
            let kindOpt = p |> List.tryPick (fun x -> if x.StartsWith("--kind:") then Some(x.Substring(7)) else None)
            match kindOpt with
            | None | Some "" ->
                eprintfn "usage: stalled-list.fsx <eco> [lc] --kind <sideffect|prepared|timer|failed>"
                Environment.Exit(1)
                failwith "unreachable"
            | Some k -> (eco, lcOpt, k)
        | _ ->
            eprintfn "usage: stalled-list.fsx <eco> [lc] --kind <sideffect|prepared|timer|failed>"
            Environment.Exit(1)
            failwith "unreachable"

    let connString =
        match Environment.GetEnvironmentVariable("MSSQL_CONN") with
        | null | "" ->
            eprintfn "FAIL: set MSSQL_CONN or use conn.sh"
            Environment.Exit(1)
            ""
        | s -> ensureConnectTimeout s

    let lcWhere =
        match lc with
        | Some l -> sprintf "Lifecycle = '%s'" l
        | None -> ""

    let (sql, decodeCol) =
        match kind with
        | "sideffect" ->
            (sprintf """
                SELECT Subj, SideEffectSeqNumber, CreatedOn, FailureAckedUntil,
                       [<ECO>].[decode]([SideEffect]) AS DecodedSideEffect
                FROM [<ECO>].[AllStalledSideEffects] WITH (NOLOCK)
                %s
                ORDER BY CreatedOn DESC
                """ (if lcWhere = "" then "" else "WHERE " + lcWhere)
                |> fun s -> s.Replace("<ECO>", eco)),
            "DecodedSideEffect"
        | "prepared" ->
            (sprintf """
                SELECT Subj, SubjectTransactionId, SysStart,
                       [<ECO>].[decode]([PreparedTransactionalState]) AS DecodedPrepared
                FROM [<ECO>].[AllStalledPrepared] WITH (NOLOCK)
                %s
                ORDER BY SysStart DESC
                """ (if lcWhere = "" then "" else "WHERE " + lcWhere)
                |> fun s -> s.Replace("<ECO>", eco)),
            "DecodedPrepared"
        | "timer" ->
            (sprintf """
                SELECT Subj, NextTickOn, LastUpdatedOn,
                       [<ECO>].[decode]([NextTickContext]) AS DecodedContext
                FROM [<ECO>].[AllStalledTimers] WITH (NOLOCK)
                %s
                ORDER BY NextTickOn
                """ (if lcWhere = "" then "" else "WHERE " + lcWhere)
                |> fun s -> s.Replace("<ECO>", eco)),
            "DecodedContext"
        | "failed" ->
            (sprintf """
                SELECT Subj, SideEffectSeqNumber, CreatedOn, FailureSeverity, FailureReason, FailureAckedUntil,
                       [<ECO>].[decode]([SideEffect]) AS DecodedSideEffect
                FROM [<ECO>].[AllFailedSideEffects] WITH (NOLOCK)
                WHERE FailureSeverity IS NOT NULL
                %s
                ORDER BY CreatedOn DESC
                """ (if lcWhere = "" then "" else "AND " + lcWhere)
                |> fun s -> s.Replace("<ECO>", eco)),
            "DecodedSideEffect"
        | k ->
            eprintfn "Unknown kind: %s" k
            Environment.Exit(1)
            failwith "unreachable"

    use conn = new SqlConnection(connString)
    conn.Open()
    use cmd = new SqlCommand(sql, conn)
    use reader = cmd.ExecuteReader()

    let schema = reader.GetColumnSchema() |> Seq.map (fun c -> c.ColumnName) |> Seq.toArray
    let decodeIdx = schema |> Array.findIndex (fun c -> c = decodeCol)
    let nameWidth = 22

    for name in schema do
        printf "%-*s | " nameWidth name
    printfn ""

    while reader.Read() do
        for i in 0 .. schema.Length - 1 do
            let raw = reader.GetValue(i)
            let text =
                if i = decodeIdx then
                    prettyJson (string raw)
                else
                    match raw with
                    | null | :? DBNull -> "NULL"
                    | :? DateTimeOffset as dto -> tryLocal (Some dto)
                    | x -> string x
            printf "%-*s | " nameWidth (if text.Length > 80 then text.Substring(0, 80) + "..." else text)
        printfn ""

run()
