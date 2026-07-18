#r "nuget: Microsoft.Data.SqlClient, 5.2.3"

open System
open Microsoft.Data.SqlClient

let ensureConnectTimeout (conn: string) =
    if conn.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) then conn
    else conn + ";Connect Timeout=5"

let statusMap =
    [ 0, "Created"
      1, "Joining"
      2, "Active"
      3, "ShuttingDown"
      4, "Stopping"
      5, "Stopped"
      6, "Dead" ]
    |> Map.ofList

let statusName (i: int) = statusMap |> Map.tryFind i |> Option.defaultValue (sprintf "Unknown(%d)" i)

let humanAge (ts: DateTimeOffset option) =
    match ts with
    | None -> "N/A"
    | Some t ->
        let d = DateTimeOffset.UtcNow - t
        sprintf "%dm %ds" (int d.TotalMinutes) d.Seconds

let tryLocal (dto: DateTimeOffset option) =
    match dto with
    | Some d ->
        let local = d.ToLocalTime()
        sprintf "%s UTC / %s %s" (d.ToString("o")) (local.ToString("yyyy-MM-dd HH:mm:ss")) (local.ToString("zzz"))
    | None -> "NULL"

let decodeSuspect (value: obj) =
    match value with
    | null | :? DBNull -> "NULL"
    | :? (byte[]) as bytes ->
        try System.Text.Encoding.UTF8.GetString(bytes) with _ -> sprintf "<binary %d bytes>" bytes.Length
    | x -> string x

let run() =
    let args =
        Environment.GetCommandLineArgs()
        |> Array.skipWhile (fun a -> not (a.EndsWith("cluster-health.fsx")))
        |> Array.skip 1
        |> Array.toList

    let connString =
        match Environment.GetEnvironmentVariable("MSSQL_CONN") with
        | null | "" ->
            eprintfn "FAIL: set MSSQL_CONN or use conn.sh"
            Environment.Exit(1)
            ""
        | s -> ensureConnectTimeout s

    use conn = new SqlConnection(connString)
    conn.Open()

    match args with
    | [] ->
        printfn "DeploymentIds in OrleansMembershipTable:"
        use cmd = new SqlCommand("SELECT DISTINCT DeploymentId FROM dbo.OrleansMembershipTable WITH (NOLOCK)", conn)
        use reader = cmd.ExecuteReader()
        while reader.Read() do
            printfn "  %s" (reader.GetString(0))
    | deploymentId :: _ ->
        use cmd = new SqlCommand("""
            SELECT SiloName, HostName, Status, ProxyPort, Port, Generation, StartTime, IAmAliveTime, SuspectTimes
            FROM dbo.OrleansMembershipTable WITH (NOLOCK)
            WHERE DeploymentId = @did
            ORDER BY StartTime DESC
            """, conn)
        cmd.Parameters.AddWithValue("@did", deploymentId) |> ignore
        use reader = cmd.ExecuteReader()
        printfn "SiloName            | HostName            | Status        | Address:Port       | Generation | StartTime(local)         | IAmAlive(local)          | age_suspect | SuspectTimes"
        while reader.Read() do
            let siloName = reader.GetString(0)
            let hostName = reader.GetString(1)
            let status = reader.GetInt32(2)
            let proxyPort = if reader.IsDBNull(3) then None else Some(reader.GetInt32(3))
            let port = if reader.IsDBNull(4) then None else Some(reader.GetInt32(4))
            let gen = if reader.IsDBNull(5) then 0L else reader.GetInt64(5)
            let startTime = if reader.IsDBNull(6) then None else Some(reader.GetDateTimeOffset(6))
            let iAmAlive = if reader.IsDBNull(7) then None else Some(reader.GetDateTimeOffset(7))
            let suspect = reader.GetValue(8) |> decodeSuspect

            let addrPort =
                let p = proxyPort |> Option.defaultValue (port |> Option.defaultValue 0)
                sprintf "%s:%d" hostName p

            let age = humanAge iAmAlive
            let suspectMarker =
                match iAmAlive with
                | Some t when (DateTimeOffset.UtcNow - t).TotalSeconds > 60.0 -> " SUSPECT"
                | _ -> ""

            printfn "%-19s | %-19s | %-13s | %-18s | %-10d | %-24s | %-24s | %s%s"
                (siloName.PadRight(19))
                (hostName.PadRight(19))
                (statusName status)
                addrPort
                gen
                (tryLocal startTime)
                (tryLocal iAmAlive)
                age
                suspectMarker
            if suspect <> "NULL" then
                printfn "  SuspectTimes: %s" suspect

run()
