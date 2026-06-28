module AppTodo.TodoQueries

#if !FABLE_COMPILER
open LibLifeCycleTypes.SubjectTypes
#endif
open SuiteTodo.Types

[<RequireQualifiedAccess>]
type TodoListFilter =
| Open
| Done
| All
| Archived

let private defaultResultSetOptions =
    ResultSetOptions.dangerousAll OrderBy.FastestOrSingleSearchScoreIfAvailable

let private activeOnly =
    IndexPredicate.EqualToString (TodoStringIndex.ArchiveStatus TodoArchiveStatus.Active)

let private archivedOnly =
    IndexPredicate.EqualToString (TodoStringIndex.ArchiveStatus TodoArchiveStatus.Archived)

let activeTodosQuery () : IndexQuery<TodoIndex> =
    activeOnly
    |> TodoIndex.PrepareQuery defaultResultSetOptions

let archivedTodosQuery () : IndexQuery<TodoIndex> =
    archivedOnly
    |> TodoIndex.PrepareQuery defaultResultSetOptions

let activeTodosSearchQuery (term: NonemptyString) : IndexQuery<TodoIndex> =
    let trimmed = term.Value.Trim()
    if System.String.IsNullOrWhiteSpace trimmed then
        activeTodosQuery ()
    else
        IndexPredicate.And (
            activeOnly,
            IndexPredicate.Matches (UnionCase.ofCase TodoSearchIndex.Title, trimmed))
        |> TodoIndex.PrepareQuery defaultResultSetOptions

let archivedTodosSearchQuery (term: NonemptyString) : IndexQuery<TodoIndex> =
    let trimmed = term.Value.Trim()
    if System.String.IsNullOrWhiteSpace trimmed then
        archivedTodosQuery ()
    else
        IndexPredicate.And (
            archivedOnly,
            IndexPredicate.Matches (UnionCase.ofCase TodoSearchIndex.Title, trimmed))
        |> TodoIndex.PrepareQuery defaultResultSetOptions

let queryForFilterAndSearch (filter: TodoListFilter) (maybeSearch: Option<NonemptyString>) : IndexQuery<TodoIndex> =
    match filter, maybeSearch with
    | TodoListFilter.Archived, None -> archivedTodosQuery ()
    | TodoListFilter.Archived, Some term -> archivedTodosSearchQuery term
    | _, None -> activeTodosQuery ()
    | _, Some term -> activeTodosSearchQuery term

let private priorityScore (priority: TodoPriority) =
    match priority with
    | TodoPriority.High -> 3
    | TodoPriority.Medium -> 2
    | TodoPriority.Low -> 1

let filterTodosClientSide (filter: TodoListFilter) (todos: seq<Todo>) : list<Todo> =
    todos
    |> Seq.filter (fun todo ->
        match filter with
        | TodoListFilter.Archived -> todo.ArchivedOn.IsSome
        | TodoListFilter.Open -> todo.ArchivedOn.IsNone && not todo.Done
        | TodoListFilter.Done -> todo.ArchivedOn.IsNone && todo.Done
        | TodoListFilter.All -> todo.ArchivedOn.IsNone)
    |> Seq.sortByDescending (fun t -> int64 (priorityScore t.Priority) * 1_000_000_000_000L + t.CreatedOn.UtcTicks)
    |> Seq.toList
