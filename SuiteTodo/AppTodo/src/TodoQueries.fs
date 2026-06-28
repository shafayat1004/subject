module AppTodo.TodoQueries

#if !FABLE_COMPILER
open LibLifeCycleTypes.SubjectTypes
#endif
open SuiteTodo.Types

let private defaultResultSetOptions =
    ResultSetOptions.dangerousAll OrderBy.FastestOrSingleSearchScoreIfAvailable

let private activeOnly =
    IndexPredicate.EqualToString (TodoStringIndex.ArchiveStatus TodoArchiveStatus.Active)

let activeTodosQuery () : IndexQuery<TodoIndex> =
    activeOnly
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

let queryForSearchInput (maybeSearch: Option<NonemptyString>) : IndexQuery<TodoIndex> =
    match maybeSearch with
    | None -> activeTodosQuery ()
    | Some term -> activeTodosSearchQuery term
