#r "nuget: Microsoft.Data.SqlClient, 5.2.3"

open System
open Microsoft.Data.SqlClient

let ensureConnectTimeout (conn: string) =
    if conn.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) then conn
    else conn + ";Connect Timeout=5"

let localString (dt: DateTimeOffset) =
    let local = dt.ToLocalTime()
    sprintf "%s UTC / %s %s" (dt.ToString("o")) (local.ToString("yyyy-MM-dd HH:mm:ss")) (local.ToString("zzz"))

let formatValue (value: obj) =
    match value with
    | null -> "NULL"
    | :? DBNull -> "NULL"
    | :? DateTimeOffset as dto -> localString dto
    | :? DateTime as dt ->
        let dto = DateTimeOffset(dt, TimeSpan.Zero)
        localString dto
    | :? byte[] as bytes -> sprintf "<binary %d bytes>" bytes.Length
    | x -> x.ToString()

let run() =
    let args = Environment.GetCommandLineArgs()
    let argList = args |> Array.skipWhile (fun a -> a.EndsWith("query.fsx")) |> Array.toList
    match argList with
    | _ :: connString :: sqlParts ->
        let sql = String.Join(" ", sqlParts)
        try
            let connString = ensureConnectTimeout connString
            use conn = new SqlConnection(connString)
            conn.Open()
            use cmd = new SqlCommand(sql, conn)
            use reader = cmd.ExecuteReader()
            let mutable resultSet = 1
            let mutable hasMore = true
            while hasMore do
                if reader.FieldCount = 0 then
                    hasMore <- reader.NextResult()
                else
                    if resultSet > 1 then printfn "--- resultset %d ---" resultSet
                    let schema = reader.GetColumnSchema()
                    let names = schema |> Seq.map (fun c -> c.ColumnName) |> Seq.toArray
                    let widths = Array.init names.Length (fun i -> max (names.[i].Length) 8)

                    let rows = ResizeArray<string[]>()
                    while reader.Read() do
                        let values = Array.init names.Length (fun i ->
                            let raw = reader.GetValue(i)
                            formatValue raw
                        )
                        rows.Add(values)
                        values |> Array.iteri (fun i v -> widths.[i] <- max widths.[i] (min v.Length 80))

                    let pad i (s: string) = s.PadRight(widths.[i]).Substring(0, widths.[i])
                    let sep () = printf "|"; Array.iteri (fun i _ -> printf " %s |" (String('-', widths.[i]))) names; printfn ""

                    printf "|"; Array.iteri (fun i n -> printf " %s |" (pad i n)) names; printfn ""
                    sep ()
                    for values in rows do
                        printf "|"; Array.iteri (fun i v -> printf " %s |" (pad i v)) values; printfn ""

                    hasMore <- reader.NextResult()
                    resultSet <- resultSet + 1
            Environment.Exit(0)
        with
        | :? SqlException as ex ->
            eprintfn "SQL error: %s" ex.Message
            Environment.Exit(1)
        | ex ->
            eprintfn "Error: %s" ex.Message
            Environment.Exit(1)
    | _ ->
        eprintfn "usage: query.fsx <conn-string> <sql>"
        Environment.Exit(1)

run()
