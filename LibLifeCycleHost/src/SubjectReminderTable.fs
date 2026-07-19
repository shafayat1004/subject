module LibLifeCycleHost.SubjectReminderTable

open System
open System.Collections.Concurrent
open System.Reflection
open System.Threading.Tasks
open Microsoft.Extensions.Options
open Orleans
open Orleans.Configuration
open Orleans.Serialization
open Orleans.Runtime
open Microsoft.Extensions.Logging
open Microsoft.Data.SqlClient
open LibLifeCycle
open LibLifeCycleCore
open System.Data
open FSharp.Control
open LibLifeCycleHost.SubjectReminder
open LibLifeCycleHost.Storage.SqlServer

// This is an optimized implementation of a reminder table that simply piggybacks upon the subject tables themselves
// Consequently no data is actually written to from here, and this is only used to read existing timers when the system
// loads up
type SubjectReminderTable
    (
        config:                     SqlServerConnectionStrings,
        ecosystem:                  Ecosystem,
        grainFactory:               IGrainFactory,
        lifeCycleAdapterCollection: HostedLifeCycleAdapterCollection,
        logger:                     ILogger<SubjectReminderTable>,
        clock:                      Service<Clock>,
        ringOptions:                IOptions<ConsistentRingOptions>,
        isDevHost:                  bool
    ) =

    let connectionString = config.ForEcosystem ecosystem.Name

    // Map from the Orleans grain type to our adapter so we can resolve a GrainId back to the lifecycle that owns the subject table.
    // GrainType in Orleans 10 does not implement IComparable, so a Map<GrainType,_> cannot be used; use a Dictionary instead.
    let grainTypeToLifeCycleAdapter : System.Collections.Generic.Dictionary<GrainType, IHostedLifeCycleAdapter> =
        let (GrainPartition defaultGrainPartitionGuid) = defaultGrainPartition
        let dict = System.Collections.Generic.Dictionary<GrainType, IHostedLifeCycleAdapter>()
        for adapter in lifeCycleAdapterCollection do
            let grainId =
                (grainFactory.GetGrain(adapter.SubjectGrainType, defaultGrainPartitionGuid, "subject") :?> GrainReference).GrainId
            dict.[grainId.Type] <- adapter
        dict

    let readAllRowsSql =
        let persistentAdapters =
            lifeCycleAdapterCollection
            |> Seq.where(fun adapter -> match adapter.Storage with | StorageType.Persistent _ -> true | _ -> false)
        let hasPersistentAdapters =
            persistentAdapters
            |> Seq.isNonempty
        match hasPersistentAdapters with
        | true ->
            let unionedSql =
                persistentAdapters
                |> Seq.map (fun adapter ->
                    if adapter.LifeCycleName <> MetaLifeCycleName then
                        sprintf "SELECT Id, '%s' AS LifeCycleName, NextTickOn, GrainIdHash FROM [%s].[%s] (NOLOCK) WHERE NextTickOn IS NOT NULL AND NextTickFired = 0 AND NextTickOn <= @maxNextTickOn"
                            adapter.LifeCycleName ecosystem.Name adapter.LifeCycleName
                    else
                        // read all rows for Meta, for keep alive tick
                        sprintf "SELECT Id, '%s' AS LifeCycleName, NextTickOn, GrainIdHash FROM [%s].[%s] (NOLOCK)"
                            adapter.LifeCycleName ecosystem.Name adapter.LifeCycleName)
                |> String.concat "\nUNION ALL\n"
            Some unionedSql
        | false -> None

    let initialBucketQueryTimestamp = ConcurrentDictionary<(* begin' *) uint32 * (* end' *) uint32, DateTimeOffset>()

    // TODO: eliminate ticks of subjects in Prepared (in transaction) state somehow. Not cheap to just look up _Prepared table.

    // TODO: don't read all rows, read TOP N ordered by NextTickOn (but first make sure SQL indices used well!),
    // this will self-regulate scenarios when system finds too many overdue reminders (e.g. after downtime) and then chokes on them
    // optimal top N = subjectReminderTableLookAheadLimit.TotalMilliseconds / (optimal-ms-between-overdue-reminders * ringOptions.NumVirtualBucketsConsistentRing)
    let readAllRowsSqlWithinRange =
        readAllRowsSql
        |> Option.map (sprintf "SELECT Id, LifeCycleName, NextTickOn FROM (%s) a WHERE GrainIdHash > @begin AND GrainIdHash <= @end")

    let readAllRowsSqlOutsideRange =
        readAllRowsSql
        |> Option.map (sprintf "SELECT Id, LifeCycleName, NextTickOn FROM (%s) a WHERE GrainIdHash > @begin OR GrainIdHash <= @end")

    // arbitrary date in the past i.e. due immediately, ticks every minute, random second start to avoid spikes of Meta reminders
    let rnd = Random()
    let metaKeepAliveReminderStartAt () = DateTimeOffset(2010, 1, 1, 0, 0, rnd.Next(0, 59), TimeSpan.Zero)

    interface IReminderTable with
        member this.Init() : Task =
            Task.CompletedTask

        member this.ReadRow(grainId: GrainId, reminderName: string): Task<ReminderEntry> =
            // TODO -- if we use multiple perisistent providers, we need to move this into the provider's interface
            if reminderName = SubjectReminderName then
                let adapter = grainTypeToLifeCycleAdapter.[grainId.Type]
                let sql = sprintf "SELECT (CASE WHEN NextTickFired = 0 THEN NextTickOn ELSE NULL END) FROM [%s].[%s] WHERE Id = @id" ecosystem.Name adapter.LifeCycleName
                let (_, pKey) = grainId.GetGuidKey()

                backgroundTask {
                    use connection = new SqlConnection(connectionString)
                    do! connection.OpenAsync()
                    use command = connection.CreateCommand()
                    command.CommandText <- sql
                    command.Parameters.Add("@id", SqlDbType.NVarChar).Value <- pKey
                    match! command.ExecuteScalarAsync() with
                    | null
                    | :? DBNull ->
                        return null
                    | nextTickOn ->
                        return newReminderEntry SubjectReminderName grainId (nextTickOn :?> DateTimeOffset)
                }
            elif reminderName = MetaKeepAliveReminderName then
                let adapter = grainTypeToLifeCycleAdapter.[grainId.Type]
                if adapter.LifeCycleName <> MetaLifeCycleName then
                    null
                else
                    newReminderEntry MetaKeepAliveReminderName grainId (metaKeepAliveReminderStartAt ())
                |> Task.FromResult
            else
                failwithf "Reminder with name %s is not supported by this implementation of Reminder Table" reminderName

        member this.ReadRows(grainId: GrainId): Task<ReminderTableData> =
            backgroundTask {
                let! reminder1 = (this :> IReminderTable).ReadRow(grainId, SubjectReminderName)
                let! reminder2 = (this :> IReminderTable).ReadRow(grainId, MetaKeepAliveReminderName)
                return ReminderTableData([reminder1; reminder2] |> Seq.filter (fun r -> r <> null))
            }

        member this.ReadRows(begin': uint32, end': uint32): Task<ReminderTableData> =
            // NOTE that begin can actually be higher than end, in which case, you're expected to return
            // all rows where hash > begin or hash < end
            // i.e. this is a "ring range", so think of it as a set of circular segments within the int32 space,
            // as opposed to linear segments

            let maybeSql = if begin' < end' then readAllRowsSqlWithinRange else readAllRowsSqlOutsideRange

            match maybeSql with
            | Some sql ->
                backgroundTask {
                    use connection = new SqlConnection(connectionString)
                    do! connection.OpenAsync()
                    use command = connection.CreateCommand()

                    let! nowBeforeQuery = clock.Query Now
                    let maxNextTickOn = nowBeforeQuery.Add subjectReminderTableLookAheadLimit

                    command.CommandText <- sql
                    command.Parameters.Add("@begin", SqlDbType.Int).Value <- SqlServerGrainStorageHandler.uint32ToInt32MaintainOrder begin'
                    command.Parameters.Add("@end",   SqlDbType.Int).Value <- SqlServerGrainStorageHandler.uint32ToInt32MaintainOrder end'
                    command.Parameters.Add("@maxNextTickOn", SqlDbType.DateTimeOffset).Value <- maxNextTickOn

                    use! reader = command.ExecuteReaderAsync()
                    let! listOfRawReminderEntryArgsSortedByNextTickOn =
                        AsyncSeq.unfoldAsync (
                            fun _ ->
                                async {
                                    match! reader.ReadAsync() |> Async.AwaitTask with
                                    | true ->
                                        let pKey = reader.GetString(0)
                                        let lifeCycleName = reader.GetString(1)
                                        let adapter =
                                            lifeCycleAdapterCollection
                                            |> Seq.find (fun a -> a.LifeCycleName = lifeCycleName)
                                        let (GrainPartition defaultGrainPartitionGuid) = defaultGrainPartition
                                        let grainId =
                                            (grainFactory.GetGrain(adapter.SubjectGrainType, defaultGrainPartitionGuid, pKey) :?> GrainReference).GrainId
                                        let maybeSubjectReminder =
                                            if (not <| reader.IsDBNull 2) then
                                                let nextTickOn = reader.GetDateTimeOffset(2)
                                                 // double check maxNextTickOn because Meta LC SQL query is not controlled for it
                                                if nextTickOn <= maxNextTickOn then
                                                    Some (SubjectReminderName, grainId, nextTickOn)
                                                else
                                                    None
                                            else
                                                None
                                        let maybeMetaKeepAliveReminder =
                                            if lifeCycleName = MetaLifeCycleName then
                                                Some (MetaKeepAliveReminderName, grainId, metaKeepAliveReminderStartAt())
                                            else
                                                None
                                        return Some([maybeSubjectReminder; maybeMetaKeepAliveReminder] |> List.choose id, Nothing)
                                    | false ->
                                        return None
                                }) Nothing
                        |> AsyncSeq.toListAsync
                        |> Async.Map (
                            List.collect id
                            // consider ORDER BY on SQL side if this is slow
                            >> List.sortBy (fun (_, _, nextTickOn) -> nextTickOn))
                        |> Async.StartAsTask

                    let listLength = listOfRawReminderEntryArgsSortedByNextTickOn.Length
                    if listLength > 1000 then // warn if many ticks
                        logger.LogWarning("Loaded {0} ticks for bucket {1}/{2}", listLength, begin', end')
                    else
                        logger.LogInformation("Loaded {0} ticks for bucket {1}/{2}", listOfRawReminderEntryArgsSortedByNextTickOn.Length, begin', end')

                    // Overdue reminders need to be overridden to fire "asap": if unchanged they will get delayed, here's how:
                    // for example a timer that was due 7 seconds ago will skip that tick and will fire in -7 sec + 1 minute period = 53 seconds from now.
                    // To prevent too many "asap" reminders firing the moment reminder service refreshes (including silo start up) we spread out all overdue ticks.
                    // It mostly matters for dev where system can be offline for many days and accumulate many overdue reminders that would choke the system if fired together,
                    // prod however rarely falls much behind

                    let initialBucketQuery =
                        // consider queries to the bucket within first few minutes as "initial" to decrease chance of a late tick while cluster is stabilizing
                        // attempts to do it only for the first query resulted in some late ticks during deployment
                        initialBucketQueryTimestamp.TryGetValue ((begin', end'))
                        |> function | true, timestamp -> nowBeforeQuery < timestamp.AddMinutes 16. | false, _ -> true

                    let! nowAfterQuery = clock.Query Now |> Async.AwaitTask
                    let earliestNewDueReminderStartAt = nowAfterQuery.Add subjectReminderTableNewDueReminderMinDelay

                    let listOfAmendedReminderEntryArgs =
                        let spreadMilliseconds =
                            // what's the good spreadMilliseconds interval?
                            if isDevHost then
                                // for dev host, ~15 milliseconds between subsequent overdue reminders _for all buckets_ on single silo
                                // is a good middle ground to process them fast enough and not choke (here we deal only with one bucket).
                                15 * ringOptions.Value.NumVirtualBucketsConsistentRing // there's 30 buckets by default i.e. 15 x 30 = 450
                            else
                                // on prod, pack them more tightly to avoid unnecessary overdue reminders
                                40
                        listOfRawReminderEntryArgsSortedByNextTickOn |>
                        List.fold (fun (acc, newDueRemindersCount) args ->
                            match args with
                            | reminderName, _, _ when reminderName = MetaKeepAliveReminderName ->
                                args :: acc, newDueRemindersCount
                            | reminderName, grainId, nextTickOn ->
                                let startAt, newDueRemindersCount =
                                    if (subjectReminderTableIsNewDueReminderBestGuess initialBucketQuery nowAfterQuery nextTickOn) then
                                        earliestNewDueReminderStartAt + ((spreadMilliseconds * int newDueRemindersCount) |> float |> TimeSpan.FromMilliseconds),
                                        (newDueRemindersCount + 1u)
                                    else
                                        nextTickOn + subjectReminderImplicitDelayToReduceEarlyTicks, newDueRemindersCount
                                (reminderName, grainId, startAt) :: acc, newDueRemindersCount)
                            ([], 0u)
                        |> fst
                        |> List.rev

                    initialBucketQueryTimestamp.TryAdd((begin', end'), nowAfterQuery) |> ignore

                    return
                        listOfAmendedReminderEntryArgs
                        |> Seq.map (fun (reminderName, grainId, startAt) -> newReminderEntry reminderName grainId startAt)
                        |> ReminderTableData
                }
            | None -> Task.FromResult(new ReminderTableData())


        member this.RemoveRow(_grainId: GrainId, _reminderName: string, _eTag: string): Task<bool> =
            // NO-OP. We will never do any writes from the Reminder system; all reminder peristence
            // is handled by SubjectGrain when it configures the nextTick as part of subject upgrade
            Task.FromResult true

        member this.TestOnlyClearTable(): Task =
            Task.CompletedTask

        member this.UpsertRow(_entry: ReminderEntry): Task<string> =
            // NO-OP. We will never do any writes from the Reminder system; all reminder peristence
            // is handled by SubjectGrain when it configures the nextTick as part of subject upgrade
            Guid.NewGuid()
            |> sprintf "%O"
            |> Task.FromResult
