[<AutoOpen>]
module SuiteTodo.LifeCycles.AllLifeCycles

open LibLifeCycle
open SuiteTodo.Types
open SuiteTodo.LifeCycles

type TodoEcosystem = {
    TodoLifeCycle: LifeCycle<Todo, TodoAction, TodoOpError, TodoConstructor, TodoLifeEvent, TodoIndex, TodoId, AccessPredicateInput, NoSession, NoRole, TodoEnvironment>
    TodoListView:  View<NoInput, TodoListViewOutput, NoViewError, AccessPredicateInput, NoSession, NoRole, TodoListViewEnvironment>
    Ecosystem:     Ecosystem
}

let createTodoEcosystem () : TodoEcosystem =
    let ecosystem =
        EcosystemBuilder.newEcosystem todoDef.EcosystemDef
        |> EcosystemBuilder.withNoSessionHandler
        |> EcosystemBuilder.addLifeCycle todoLifeCycle
        |> EcosystemBuilder.addView todoListView
        |> EcosystemBuilder.build

    { Ecosystem     = ecosystem
      TodoLifeCycle = todoLifeCycle
      TodoListView  = todoListView }

let todoEcosystem = createTodoEcosystem ()
