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
        |> Array.skipWhile (fun a -> not (a.EndsWith("failed-sideffects.fsx")))
        |> Array.skip 1
        |> Array.toList

    let (eco, lcOpt, subjectOpt, severityOpt) =
        match args with
        | eco :: rest ->
            let rec loop acc = function
                | [] -> List.rev acc
                | "--subject" :: v :: t -> loop ("--subject:" + v :: acc) t
                | "--severity" :: v :: t -> loop ("--severity:" + v :: acc) t
                | h :: t -> loop (h :: acc) t
            let p = loop [] rest
            let lc = p |> List.tryFind (fun x -> not (x.StartsWith("--")))
            let subj = p |> List.tryPick (fun x -> if x.StartsWith("--subject:") then Some(x.Substring(10)) else None)
            let sev = p |> List.tryPick (fun x -> if x.StartsWith("--severity:") then Some(Int32.Parse(x.Substring(11))) else None)
            (eco, lc, subj, sev)
        | _ ->
            eprintfn "usage: failed-sideffects.fsx <eco> [lc] [--subject <id>] [--severity 1|2]"
            Environment.Exit(1)
            failwith "unreachable"

    let connString =
        match Environment.GetEnvironmentVariable("MSSQL_CONN") with
        | null | "" ->
            eprintfn "FAIL: set MSSQL_CONN or use conn.sh"
            Environment.Exit(1)
            ""
        | s -> ensureConnectTimeout s

    let whereParts =
        [ yield "FailureSeverity IS NOT NULL"
          match lcOpt with Some lc -> yield sprintf "Lifecycle = '%s'" lc | None -> ()
          match subjectOpt with Some s -> yield sprintf "Subj = '%s'" s | None -> ()
          match severityOpt with Some sev -> yield sprintf "FailureSeverity = %d" sev | None -> () ]

    let sql =
        sprintf """
            SELECT Subj, SideEffectSeqNumber, CreatedOn, FailureSeverity, FailureReason, FailureAckedUntil,
                   [<ECO>].[decode]([SideEffect]) AS DecodedSideEffect
            FROM [<ECO>].[AllFailedSideEffects] WITH (NOLOCK)
            WHERE %s
            ORDER BY CreatedOn DESC
            """
            (String.Join(" AND ", whereParts))
        |> fun s -> s.Replace("<ECO>", eco)

    use conn = new SqlConnection(connString)
    conn.Open()
    use cmd = new SqlCommand(sql, conn)
    use reader = cmd.ExecuteReader()

    let mutable any = false
    while reader.Read() do
        any <- true
        let subj = reader.GetString(0)
        let seqNum = reader.GetInt64(1)
        let created = reader.GetDateTimeOffset(2)
        let sev = reader.GetInt32(3)
        let reason = if reader.IsDBNull(4) then "NULL" else reader.GetString(4)
        let ackUntilRaw = reader.GetValue(5)
        let decoded = reader.GetString(6)
        let ackUntil = if ackUntilRaw = box DBNull.Value then None else Some(unbox<DateTimeOffset> ackUntilRaw)
        let now = DateTimeOffset.UtcNow
        let acked = match ackUntil with Some u -> u > now | None -> false
        let stillInWindow = match ackUntil with Some u -> u > now | None -> false

        printfn "--- %s seq=%d severity=%d ---" subj seqNum sev
        printfn "CreatedOn: %s" (tryLocal (Some created))
        printfn "FailureReason: %s" reason
        printfn "FailureAckedUntil: %s" (tryLocal ackUntil)
        printfn "Acked: %b | Still in ack window: %b" acked stillInWindow
        printfn "%s" (prettyJson decoded)

    if not any then
        printfn "No failed sideffects match filters."

run()
