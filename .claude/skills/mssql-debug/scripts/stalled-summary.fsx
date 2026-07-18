#r "nuget: Microsoft.Data.SqlClient, 5.2.3"

open System
open Microsoft.Data.SqlClient

let ensureConnectTimeout (conn: string) =
    if conn.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) then conn
    else conn + ";Connect Timeout=5"

let tryLocal (dto: DateTimeOffset option) =
    match dto with
    | Some d ->
        let local = d.ToLocalTime()
        sprintf "%s UTC / %s %s" (d.ToString("o")) (local.ToString("yyyy-MM-dd HH:mm:ss")) (local.ToString("zzz"))
    | None -> "NULL"

let run() =
    let args =
        Environment.GetCommandLineArgs()
        |> Array.skipWhile (fun a -> not (a.EndsWith("stalled-summary.fsx")))
        |> Array.skip 1
        |> Array.toList

    let eco =
        match args with
        | e :: _ -> e
        | _ ->
            eprintfn "usage: stalled-summary.fsx <eco>"
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

    let checkMetaSql =
        sprintf """
            SELECT COUNT(*) FROM [<ECO>].[_Meta_Index] WITH (NOLOCK)
            WHERE [Key] = 'RebuildingTimersAndSubs'
            """
        |> fun s -> s.Replace("<ECO>", eco)
    use metaCmd = new SqlCommand(checkMetaSql, conn)
    let metaOk = (metaCmd.ExecuteScalar() :?> int) > 0
    if not metaOk then
        let warn =
            "WARN: [<ECO>].[_Meta_Index] has no 'RebuildingTimersAndSubs' -- AllStalledTimers view may be empty for this reason."
                .Replace("<ECO>", eco)
        eprintfn "%s" warn

    let healthSql =
        sprintf """
SELECT ISNULL(SUM(StalledCount), 0), ISNULL(STRING_AGG(Subj, ';'), '') FROM
( SELECT Subj, COUNT(*) AS StalledCount FROM [<ECO>].[AllStalledPrepared] GROUP BY Subj ) x

SELECT ISNULL(SUM(WarningCount), 0), ISNULL(STRING_AGG(Subj, ';'), '') FROM
( SELECT Subj, COUNT(*) AS WarningCount FROM [<ECO>].[AllFailedSideEffects]
  WHERE FailureSeverity = 1 AND (FailureAckedUntil IS NULL OR [FailureAckedUntil] < GETUTCDATE())
  GROUP BY Subj ) x

SELECT ISNULL(SUM(ErrorCount), 0), ISNULL(STRING_AGG(Subj, ';'), '') FROM
( SELECT Subj, COUNT(*) AS ErrorCount FROM [<ECO>].[AllFailedSideEffects]
  WHERE FailureSeverity = 2 AND (FailureAckedUntil IS NULL OR [FailureAckedUntil] < GETUTCDATE())
  GROUP BY Subj ) x

SELECT ISNULL(SUM(StalledCount), 0), ISNULL(STRING_AGG(Subj, ';'), '') FROM
( SELECT Subj, COUNT(*) AS StalledCount FROM [<ECO>].[AllStalledSideEffects] GROUP BY Subj ) x

SELECT ISNULL(SUM(StalledCount), 0), ISNULL(STRING_AGG(Subj, ';'), ''), MIN(OldestNextTickOn) AS OldestNextTickOn FROM
( SELECT Subj, COUNT(*) AS StalledCount, MIN(NextTickOn) AS OldestNextTickOn
  FROM [<ECO>].[AllStalledTimers] GROUP BY Subj ) x
"""
        |> fun s -> s.Replace("<ECO>", eco)

    use cmd = new SqlCommand(healthSql, conn)
    use reader = cmd.ExecuteReader()

    let labels = ["prepared"; "failed warnings"; "failed errors"; "stalled sideffects"; "stalled timers"]
    let mutable i = 0
    let mutable hasMore = true
    while hasMore do
        if i < labels.Length then
            printfn "--- %s ---" labels.[i]
        while reader.Read() do
            let count = reader.GetValue(0) |> unbox<int>
            let subjs = reader.GetValue(1) |> string
            let subjList = if String.IsNullOrWhiteSpace(subjs) then "<none>" else subjs
            printfn "count: %d" count
            printfn "subjects: %s" subjList
            if i = 4 && not (reader.IsDBNull(2)) then
                let oldest = reader.GetDateTimeOffset(2)
                printfn "oldest NextTickOn: %s" (tryLocal (Some oldest))
        hasMore <- reader.NextResult()
        i <- i + 1

run()
