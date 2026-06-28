module AppTodo.Actions

open LibClient
open AppTodo.AppServices
open SuiteTodo.Types

let addTodo (title: NonemptyString) : UDActionResult =
    services().Todo.Construct (TodoConstructor.New title)
    |> Async.Map (Result.map ignore)
    |> OpErrors.MapToDisplayString

let toggleTodo (todoId: TodoId) : UDActionResult =
    services().Todo.Act todoId TodoAction.ToggleDone
    |> OpErrors.MapToDisplayString

let deleteTodo (todoId: TodoId) : UDActionResult =
    services().Todo.Act todoId TodoAction.Delete
    |> OpErrors.MapToDisplayString
