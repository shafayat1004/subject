namespace S15SerializerRoundtrip

open System
open System.Threading.Tasks
open Orleans
open Orleans.Hosting
open Orleans.TestingHost
open Orleans.Serialization.Configuration
open Microsoft.Extensions.Configuration

module Program =

    type SiloConfigurator() =
        interface ISiloConfigurator with
            member _.Configure(siloBuilder: ISiloBuilder) =
                siloBuilder.Configure<TypeManifestOptions>(fun (options: TypeManifestOptions) ->
                    options.AllowAllTypes <- true)
                |> ignore

    type ClientConfigurator() =
        interface IClientBuilderConfigurator with
            member _.Configure(_configuration: IConfiguration, clientBuilder: IClientBuilder) =
                clientBuilder.Configure<TypeManifestOptions>(fun (options: TypeManifestOptions) ->
                    options.AllowAllTypes <- true)
                |> ignore

    let boxTask (t: Task<'a>) : Task<obj> = task { let! x = t in return box x }

    let annotatedSamples () =
        let id = Guid.NewGuid()
        let category = Some (Annotated.Category.Work ())
        let priority = Annotated.Priority.High ()
        let todo: Annotated.Todo = {
            Id                = Annotated.makeTodoId id
            Title             = "S15 round-trip"
            Done              = false
            ArchivedOn        = None
            QueuedForDeletion = false
            CreatedOn         = DateTimeOffset.UtcNow
            Priority          = priority
            DueOn             = Some (DateTimeOffset.UtcNow.AddDays(1.0))
            Category          = category
        }
        let err = Annotated.TodoError.Conflict(Guid.NewGuid())
        let collections: Annotated.CollectionsRecord = {
            Tags       = ["alpha"; "beta"; "gamma"]
            Scores     = [| 10; 20; 30 |]
            Categories = Set [Annotated.Category.Work (); Annotated.Category.Personal ()]
            Metadata   = Map ["one", 1; "two", 2]
            Nested     = Some (Some 7)
        }
        let nested: Annotated.NestedResultWrapper = { Value = Ok [Ok todo; Error err] }
        let resultW: Annotated.ResultWrapper = { Value = Ok todo }
        [
            "Priority",      box (Annotated.Priority.Medium ())
            "Action",        box (Annotated.TodoAction.SetPriority (Annotated.Priority.Low ()))
            "Constructor",   box (Annotated.makeTodoConstructor "title" (Annotated.Priority.Low ()) (Some (Annotated.Category.Health ())) None)
            "Todo",          box todo
            "LifeEvent",     box (Annotated.TodoLifeEvent.DoneToggled true)
            "Error",         box err
            "Result",        box resultW
            "Collections",   box collections
            "NestedResult",  box nested
        ], todo

    let runAnnotated (grain: IAnnotatedSpikeGrain) (samples: (string * obj) list) (todo: Annotated.Todo) : Task<(string * string * bool * string) list> =
        task {
            let results = ResizeArray<string * string * bool * string>()
            for name, value in samples do
                try
                    let! returned =
                        match name with
                        | "Priority"     -> grain.EchoPriority(unbox value) |> boxTask
                        | "Action"       -> grain.EchoAction(unbox value) |> boxTask
                        | "Constructor"  -> grain.EchoConstructor(unbox value) |> boxTask
                        | "Todo"         -> grain.EchoTodo(unbox value) |> boxTask
                        | "LifeEvent"    -> grain.EchoLifeEvent(unbox value) |> boxTask
                        | "Error"        -> grain.EchoError(unbox value) |> boxTask
                        | "Result"       -> grain.EchoResult(unbox value) |> boxTask
                        | "Collections"  -> grain.EchoCollections(unbox value) |> boxTask
                        | "NestedResult" -> grain.EchoNested(unbox value) |> boxTask
                        | _              -> failwith "unknown shape"
                    let pass = (value = returned)
                    results.Add(("annotated", name, pass, if pass then "" else "structural mismatch"))
                with ex ->
                    results.Add(("annotated", name, false, ex.Message))
            try
                do! grain.Store todo
                let! retrieved = grain.Retrieve()
                let pass = (Some todo = retrieved)
                results.Add(("annotated", "StoreRetrieve", pass, if pass then "" else "retrieved mismatch"))
            with ex ->
                results.Add(("annotated", "StoreRetrieve", false, ex.Message))
            return results |> Seq.toList
        }

    [<EntryPoint>]
    let main _argv =
        printfn "S15 -- Orleans 10.2.1 F# serializer round-trip spike"
        printfn "Booting 2-silo in-process test cluster..."

        let builder = TestClusterBuilder(2s)
        builder.AddSiloBuilderConfigurator<SiloConfigurator>()     |> ignore
        builder.AddClientBuilderConfigurator<ClientConfigurator>() |> ignore
        use cluster = builder.Build()

        cluster.DeployAsync().Wait()
        printfn "Cluster deployed."

        let client = cluster.Client
        let annotatedGrain = client.GetGrain<IAnnotatedSpikeGrain>("annotated")

        let annotatedSamples, annotatedTodo = annotatedSamples()

        let results =
            runAnnotated annotatedGrain annotatedSamples annotatedTodo
            |> Async.AwaitTask
            |> Async.RunSynchronously

        printfn ""
        printfn "%-12s %-14s %-6s %s" "Annotation" "Shape" "Result" "Note"
        printfn "%-12s %-14s %-6s %s" "----------" "-----" "------" "----"

        let mutable allPass = true
        for annotation, name, passed, note in results do
            let status = if passed then "PASS" else "FAIL"
            if not passed then allPass <- false
            printfn "%-12s %-14s %-6s %s" annotation name status note

        printfn ""
        if allPass then
            printfn "S15 PASS -- all shapes round-tripped across two silos."
            0
        else
            printfn "S15 FAIL -- at least one shape failed round-trip."
            1
