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

let prettyJson (raw: string) =
    try
        use doc = JsonDocument.Parse(raw)
        JsonSerializer.Serialize(doc, JsonSerializerOptions(WriteIndented = true))
    with _ -> raw

let run() =
    let args =
        Environment.GetCommandLineArgs()
        |> Array.skipWhile (fun a -> not (a.EndsWith("decode-subject.fsx")))
        |> Array.skip 1
        |> Array.toList

    let (eco, lc, subjectId, versionOpt) =
        match args with
        | eco :: lc :: sid :: rest ->
            let v =
                match rest with
                | "--version" :: n :: _ -> Some(Int32.Parse(n))
                | _ -> None
            (eco, lc, sid, v)
        | _ ->
            eprintfn "usage: decode-subject.fsx <eco> <lc> <subjectId> [--version N]"
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

    let versionFilter =
        match versionOpt with
        | Some v -> sprintf "AND Version = %d" v
        | None -> ""

    let sql =
        sprintf """
            SELECT [Version], [Operation], [IsConstruction], [SysStart], [SubjectLastUpdatedOn], [LastOperationBy],
                   [<ECO>].[decode]([Subject]) AS DecodedSubject
            FROM [<ECO>].[<LC>_HistoryWithCurrent] WITH (NOLOCK)
            WHERE Id = @id %s
            ORDER BY [Version]
            """ versionFilter
        |> fun s -> s.Replace("<ECO>", eco).Replace("<LC>", lc)

    use cmd = new SqlCommand(sql, conn)
    cmd.Parameters.AddWithValue("@id", subjectId) |> ignore
    use reader = cmd.ExecuteReader()

    let mutable foundAny = false
    let mutable constructionShown = false
    let mutable currentShown = false

    while reader.Read() do
        foundAny <- true
        let version = reader.GetInt64(0)
        let operation = reader.GetValue(1) |> string
        let isConstruction = reader.GetBoolean(2)
        let sysStart = reader.GetDateTimeOffset(3)
        let lastUpdated = reader.GetDateTimeOffset(4)
        let lastOpBy = reader.GetValue(5) |> string
        let decoded = reader.GetString(6)

        let marker =
            if isConstruction then "[CONSTRUCTION]"
            elif not currentShown then "[CURRENT]"
            else ""

        if isConstruction then constructionShown <- true
        if not currentShown && not isConstruction then currentShown <- true

        printfn "%s Version %d  Operation: %s  By: %s" marker version operation lastOpBy
        printfn "  SysStart: %s" (tryLocal sysStart)
        printfn "  SubjectLastUpdatedOn: %s" (tryLocal lastUpdated)
        printfn "%s" (prettyJson decoded)
        printfn ""

    if not foundAny then
        printfn "No rows for %s.%s Id=%s" eco lc subjectId

    reader.Close()

    // If no --version, also explicitly fetch construction row side-by-side when current != construction
    if versionOpt.IsNone && currentShown && not constructionShown then
        let sql2 =
            sprintf """
                SELECT [Version], [Operation], [SysStart], [SubjectLastUpdatedOn], [LastOperationBy],
                       [<ECO>].[decode]([Subject]) AS DecodedSubject
                FROM [<ECO>].[<LC>_HistoryWithCurrent] WITH (NOLOCK)
                WHERE Id = @id AND IsConstruction = 1
                ORDER BY [Version]
                """
            |> fun s -> s.Replace("<ECO>", eco).Replace("<LC>", lc)
        use cmd2 = new SqlCommand(sql2, conn)
        cmd2.Parameters.AddWithValue("@id", subjectId) |> ignore
        use reader2 = cmd2.ExecuteReader()
        while reader2.Read() do
            let version = reader2.GetInt64(0)
            let operation = reader2.GetValue(1) |> string
            let sysStart = reader2.GetDateTimeOffset(2)
            let lastUpdated = reader2.GetDateTimeOffset(3)
            let lastOpBy = reader2.GetValue(4) |> string
            let decoded = reader2.GetString(5)
            printfn "[CONSTRUCTION] Version %d  Operation: %s  By: %s" version operation lastOpBy
            printfn "  SysStart: %s" (tryLocal sysStart)
            printfn "  SubjectLastUpdatedOn: %s" (tryLocal lastUpdated)
            printfn "%s" (prettyJson decoded)

run()
