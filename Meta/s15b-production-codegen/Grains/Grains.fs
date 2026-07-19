namespace S15BPRODUCTIONCODEGEN_Grains

open System
open System.Threading.Tasks
open Orleans
open Orleans.Concurrency
open S15BPRODUCTIONCODEGEN

// === F# grain impls. The C# Codegen project (sibling) scans THIS assembly (via
// GenerateCodeForDeclaringAssembly) and emits invokers + grain class metadata.
// Then the F# Host loads both this assembly and the Codegen assembly via
// Orleans.ApplicationPartAttribute wiring.

type BlobSpikeGrain() =
    inherit Grain()
    let mutable data : Option<BlobData> = None
    interface IBlobSpikeGrain with
        [<ReadOnly>]
        member _.GetBlob(id: Guid) : Task<Option<BlobData>> = Task.FromResult data
        member _.SetBlob(id: Guid, blobData: BlobData) : Task<unit> =
            data <- Some blobData
            Task.FromResult ()

type ViewSpikeGrain() =
    inherit Grain()
    let mutable todo : Todo =
        {
            Id       = Guid.NewGuid()
            Title    = "spike-todo"
            Done     = false
            Priority = Priority.Medium
            Category = Some Category.Work
            DueOn    = Some (DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero))
        }
    interface IViewSpikeGrain<GetTodoInput, GetTodoOutput> with
        [<ReadOnly>]
        member _.Read(_input: GetTodoInput) : Task<Result<GetTodoOutput, TodoOpError>> =
            Task.FromResult (Ok { Todos = [ todo ] })
        member _.Mutate(action: TodoAction) : Task<Result<Todo, TodoOpError>> =
            let result =
                match action with
                | TodoAction.SetTitle s when System.String.IsNullOrEmpty s ->
                    Error TodoOpError.EmptyTitle
                | TodoAction.SetTitle s ->
                    Ok { todo with Title = s }
                | TodoAction.ToggleDone ->
                    Ok { todo with Done = not todo.Done }
                | TodoAction.SetPriority p ->
                    Ok { todo with Priority = p }
                | TodoAction.SetCategory c ->
                    Ok { todo with Category = c }
                | TodoAction.Archive ->
                    Ok { todo with Category = None }
            match result with
            | Ok t -> todo <- t; Task.FromResult (Ok t)
            | Error e -> Task.FromResult (Error e)
