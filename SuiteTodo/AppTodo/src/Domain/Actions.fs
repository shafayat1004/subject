module AppTodo.Actions

open System
open LibClient
open AppTodo.AppServices
open SuiteTodo.Types

let addTodo
        (title: NonemptyString)
        (priority: TodoPriority)
        (category: Option<TodoCategory>)
        (dueOn: Option<DateTimeOffset>)
        : UDActionResult =
    services().Todo.Construct (TodoConstructor.New (title, priority, category, dueOn))
    |> Async.Map (Result.map ignore)
    |> OpErrors.MapToDisplayString

let toggleTodo (todoId: TodoId) : UDActionResult =
    services().Todo.Act todoId TodoAction.ToggleDone
    |> OpErrors.MapToDisplayString

let deleteTodo (todoId: TodoId) : UDActionResult =
    services().Todo.Act todoId TodoAction.Delete
    |> OpErrors.MapToDisplayString

let archiveTodo (todoId: TodoId) : UDActionResult =
    services().Todo.Act todoId TodoAction.Archive
    |> OpErrors.MapToDisplayString

let setTodoTitle (todoId: TodoId) (title: NonemptyString) : UDActionResult =
    services().Todo.Act todoId (TodoAction.SetTitle title)
    |> OpErrors.MapToDisplayString

let setTodoPriority (todoId: TodoId) (priority: TodoPriority) : UDActionResult =
    services().Todo.Act todoId (TodoAction.SetPriority priority)
    |> OpErrors.MapToDisplayString

let setTodoCategory (todoId: TodoId) (category: Option<TodoCategory>) : UDActionResult =
    services().Todo.Act todoId (TodoAction.SetCategory category)
    |> OpErrors.MapToDisplayString

let setTodoDueOn (todoId: TodoId) (dueOn: Option<DateTimeOffset>) : UDActionResult =
    services().Todo.Act todoId (TodoAction.SetDueOn dueOn)
    |> OpErrors.MapToDisplayString
