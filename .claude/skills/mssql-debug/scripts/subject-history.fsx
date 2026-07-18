#r "nuget: Microsoft.Data.SqlClient, 5.2.3"

open System
open Microsoft.Data.SqlClient
open System.Text.Json

let ensureConnectTimeout (conn: string) =
    if conn.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) then conn
    else conn + ";Connect Timeout=5"

let tryLocal (dto: DateTimeOffset) =
    let local = dto.ToLocalTime()
    sprintf "%s UTC / %s %s" (dto.ToString("o")) (local.ToString("yyyy-MM-dd HH:mm:ss")) (local.ToString("zzz"))

let run() =
    let args =
        Environment.GetCommandLineArgs()
        |> Array.skipWhile (fun a -> not (a.EndsWith("subject-history.fsx")))
        |> Array.skip 1
        |> Array.toList

    let (eco, lc, subjectId, fromOpt, toOpt) =
        match args with
        | eco :: lc :: sid :: rest ->
            let rec loop acc = function
                | [] -> List.rev acc
                | [x] -> List.rev (x :: acc)
                | "--from" :: v :: t -> loop ("--from:" + v :: acc) t
                | "--to" :: v :: t -> loop ("--to:" + v :: acc) t
                | h :: t -> loop (h :: acc) t
            let p = loop [] rest
            let fromOpt = p |> List.tryPick (fun x -> if x.StartsWith("--from:") then Some(x.Substring(7)) else None)
            let toOpt = p |> List.tryPick (fun x -> if x.StartsWith("--to:") then Some(x.Substring(5)) else None)
            (eco, lc, sid, fromOpt, toOpt)
        | _ ->
            eprintfn "usage: subject-history.fsx <eco> <lc> <subjectId> [--from <iso>] [--to <iso>]"
            Environment.Exit(1)
            failwith "unreachable"

    let connString =
        match Environment.GetEnvironmentVariable("MSSQL_CONN") with
        | null | "" ->
            eprintfn "FAIL: set MSSQL_CONN or use conn.sh"
            Environment.Exit(1)
            ""
        | s -> ensureConnectTimeout s

    use conn = new SqlConnection(connString)
    conn.Open()

    let fromClause =
        match fromOpt with
        | Some f -> sprintf "AND SubjectLastUpdatedOn >= '%s'" f
        | None -> ""
    let toClause =
        match toOpt with
        | Some t -> sprintf "AND SubjectLastUpdatedOn <= '%s'" t
        | None -> ""

    let sql =
        sprintf """
            SELECT [Version], [IsConstruction], [Operation], [LastOperationBy],
                   [SysStart], [SysEnd], [Tombstone], [SubjectLastUpdatedOn],
                   LEFT([<ECO>].[decode]([Subject]), 200) AS JsonPreview
            FROM [<ECO>].[<LC>_HistoryWithCurrent] WITH (NOLOCK)
            WHERE Id = @id %s %s
            ORDER BY [Version]
            """ fromClause toClause
        |> fun s -> s.Replace("<ECO>", eco).Replace("<LC>", lc)

    use cmd = new SqlCommand(sql, conn)
    cmd.Parameters.AddWithValue("@id", subjectId) |> ignore
    use reader = cmd.ExecuteReader()

    printfn "Version | IsConstruction | Operation           | LastOperationBy     | SysStart(local)          | SysEnd(local)            | State"
    while reader.Read() do
        let version = reader.GetInt64(0)
        let isConstruction = reader.GetBoolean(1)
        let operation = reader.GetValue(2) |> string
        let lastOpBy = reader.GetValue(3) |> string
        let sysStart = tryLocal (reader.GetDateTimeOffset(4))
        let sysEndRaw = reader.GetValue(5)
        let tombstone = reader.GetBoolean(6)
        let lastUpdated = tryLocal (reader.GetDateTimeOffset(7))
        let jsonPreview = reader.GetValue(8) |> string

        let markers =
            [ if isConstruction then yield "[CONSTRUCTION]"
              if tombstone then yield "[DELETED]" ]
            |> String.concat " "

        let sysEndStr =
            if sysEndRaw = box DBNull.Value then "NULL"
            else tryLocal (unbox<DateTimeOffset> sysEndRaw)

        printfn "%-7d | %-14b | %-19s | %-19s | %-24s | %-24s | %s" version isConstruction (operation.PadRight(19)) (lastOpBy.PadRight(19)) sysStart sysEndStr markers
        printfn "  lastUpdated: %s" lastUpdated
        printfn "  json: %s" jsonPreview

run()
