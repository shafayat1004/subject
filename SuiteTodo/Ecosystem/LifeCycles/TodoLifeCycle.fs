[<AutoOpen>]
module SuiteTodo.LifeCycles.TodoLifeCycle

open System
open LibLifeCycle
open SuiteTodo.Types
open LibLifeCycle.LifeCycleAccessBuilder
open Microsoft.Extensions.Logging
open type AccessTo<TodoAction, TodoConstructor>

[<Literal>]
let ArchiveDelayMinutes = 5

let archiveDelay = TimeSpan.FromMinutes (float ArchiveDelayMinutes)

type TodoEnvironment = {
    Clock:  Service<Clock>
    Unique: Service<Unique>
    Logger: ILogger<TodoEnvironment>
} with interface Env

let private transition (env: TodoEnvironment) (todo: Todo) (action: TodoAction)
    : TransitionResult<Todo, TodoAction, TodoOpError, TodoLifeEvent, TodoConstructor> =
    transition {
        match action with
        | TodoAction.SetTitle title ->
            if TodoValidation.isBlankTitle title then
                return TodoOpError.EmptyTitle
            else
                if title <> todo.Title then
                    yield TodoLifeEvent.TitleChanged title
                return { todo with Title = title }

        | TodoAction.ToggleDone ->
            let done' = not todo.Done
            yield TodoLifeEvent.DoneToggled done'
            env.Logger.LogInformation(
                "Todo {TodoId} done toggled to {Done}",
                (todo.Id :> SubjectId).IdString,
                done')
            return { todo with Done = done' }

        | TodoAction.Archive ->
            if todo.ArchivedOn.IsSome then
                return todo
            else
                let! now = env.Clock.Query Now
                yield TodoLifeEvent.Archived
                env.Logger.LogInformation(
                    "Todo {TodoId} archived",
                    (todo.Id :> SubjectId).IdString)
                return { todo with ArchivedOn = Some now }

        | TodoAction.Delete ->
            env.Logger.LogInformation(
                "Todo {TodoId} queued for deletion",
                (todo.Id :> SubjectId).IdString)
            return { todo with QueuedForDeletion = true }

        | TodoAction.SetPriority priority ->
            return { todo with Priority = priority }

        | TodoAction.SetCategory category ->
            return { todo with Category = category }

        | TodoAction.SetDueOn dueOn ->
            return { todo with DueOn = dueOn }
    }

let private construction (env: TodoEnvironment) (id: TodoId) (ctor: TodoConstructor)
    : ConstructionResult<Todo, TodoAction, TodoOpError, TodoLifeEvent> =
    construction {
        match ctor with
        | TodoConstructor.New (title, priority, category, dueOn) ->
            if TodoValidation.isBlankTitle title then
                return TodoOpError.EmptyTitle
            else
                let! now = env.Clock.Query Now
                yield TodoLifeEvent.Created
                env.Logger.LogInformation(
                    "Todo {TodoId} created with title {Title}",
                    (id :> SubjectId).IdString,
                    title.Value)
                return {
                    Id                = id
                    Title             = title
                    Done              = false
                    ArchivedOn        = None
                    QueuedForDeletion = false
                    CreatedOn         = now
                    Priority          = priority
                    DueOn             = dueOn
                    Category          = category
                }
    }

let private idGeneration (env: TodoEnvironment) (_ctor: TodoConstructor)
    : IdGenerationResult<TodoId, TodoOpError> =
    idgen {
        let! guid = env.Unique.Query NewUuid
        return TodoId guid
    }

let private indices (todo: Todo) : seq<TodoIndex> =
    IndicesWorkflow.indices {
        TodoNumericIndex.CreatedOn todo.CreatedOn
        TodoStringIndex.ArchiveStatus (
            if todo.ArchivedOn.IsSome then TodoArchiveStatus.Archived else TodoArchiveStatus.Active)
        TodoSearchIndex.Title todo.Title
        todo.ArchivedOn |> Option.map TodoNumericIndex.ArchivedOn
    }

let private timers (todo: Todo) : list<Timer<TodoAction>> =
    [
        if todo.QueuedForDeletion then
            { TimerAction = TimerAction.DeleteSelf; Schedule = Schedule.Now }
        elif todo.Done && todo.ArchivedOn.IsNone then
            { TimerAction = TimerAction.RunAction TodoAction.Archive
              Schedule    = Schedule.AfterLastTransition archiveDelay }
    ]

let private shouldSendTelemetry =
    function
    | ShouldSendTelemetryFor.Constructor (TodoConstructor.New _)
    | ShouldSendTelemetryFor.LifeAction _
    | ShouldSendTelemetryFor.LifeEvent _ ->
        true

let private lifeEventSatisfies (input: LifeEventSatisfiesInput<TodoLifeEvent>) =
    match input.Subscribed, input.Raised with
    | TodoLifeEvent.Created, TodoLifeEvent.Created               -> true
    | TodoLifeEvent.TitleChanged _, TodoLifeEvent.TitleChanged _ -> true
    | TodoLifeEvent.DoneToggled _, TodoLifeEvent.DoneToggled _   -> true
    | TodoLifeEvent.Archived, TodoLifeEvent.Archived             -> true
    | _                                                          -> false

let todoLifeCycle =
    newTodoLifeCycle todoDef.LifeCycles.todo
    |> LifeCycleBuilder.withTransition transition
    |> LifeCycleBuilder.withIdGeneration idGeneration
    |> LifeCycleBuilder.withConstruction construction
    |> LifeCycleBuilder.withIndices indices
    |> LifeCycleBuilder.withTimers timers
    |> LifeCycleBuilder.withLifeEventSatisfies lifeEventSatisfies
    |> LifeCycleBuilder.withoutApiAccess
    |> LifeCycleBuilder.withStorage
        (StorageType.Persistent
            (PromotedIndicesConfig.Empty,
             System.TimeSpan.FromDays 1.
             |> PersistentHistoryExpiration.AfterSubjectDeletion
             |> Some
             |> PersistentHistoryRetention.Unfiltered))
    |> LifeCycleBuilder.withTelemetryRules shouldSendTelemetry
    |> LifeCycleBuilder.build
