[<AutoOpen>]
module SuiteTodo.LifeCycles.TodoListView

open LibLifeCycle
open SuiteTodo.Types

type TodoListViewEnvironment = {
    AllTodos: Service<All<Todo, TodoId, TodoIndex, TodoOpError>>
} with interface Env

let private toListItem (todo: Todo) : TodoListItem =
    {
        Id        = todo.Id
        Title     = todo.Title
        Done      = todo.Done
        CreatedOn = todo.CreatedOn
    }

let private view (env: TodoListViewEnvironment) (_input: NoInput)
    : ViewResult<TodoListViewOutput, NoViewError> =
    view {
        let! todos =
            IndexPredicate.EqualToString (TodoStringIndex.ArchiveStatus TodoArchiveStatus.Active)
            |> TodoIndex.PrepareQuery
                { Page = { Size = System.UInt16.MaxValue; Offset = 0UL }
                  OrderBy = OrderBy.FastestOrSingleSearchScoreIfAvailable }
            |> env.AllTodos.Query FilterFetchSubjects

        let items =
            todos
            |> List.sortBy (fun t -> t.CreatedOn)
            |> List.map toListItem

        return { Items = items }
    }

let todoListView =
    newTodoView todoDef.Views.todoListView
    |> ViewBuilder.withoutApiAccess
    |> ViewBuilder.withRead view
    |> ViewBuilder.build
