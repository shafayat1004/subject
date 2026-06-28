[<AutoOpen>]
module AppTodo.ErrorMessages

open LibUiSubject.Services.SubjectService
open SuiteTodo.Types

type TodoOpError with
    member this.ToDisplayString : string =
        match this with
        | TodoOpError.EmptyTitle -> "Title cannot be empty"

type OpErrors =
    static member ToDisplayString (value: TodoOpError) : string =
        value.ToDisplayString

    static member ToDisplayString (value: ActionOrConstructionError<TodoOpError>) : string =
        value.ToDisplayString OpErrors.ToDisplayString

    static member MapToDisplayString (result: Async<Result<'T, ActionOrConstructionError<TodoOpError>>>) : Async<Result<'T, string>> =
        result |> Async.Map (Result.mapError OpErrors.ToDisplayString)

type OpErrorsWithPossibleTimeout =
    class end
