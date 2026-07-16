[<AutoOpen>]
module SuiteTodo.Types.EcosystemDef

let todoDef =
    let ecosystemDef : EcosystemDef = newEcosystemDef "Todo"

    let (todo: LifeCycleDef<Todo, TodoAction, TodoOpError, TodoConstructor, TodoLifeEvent, TodoIndex, TodoId>,
             ecosystemDef) =
        addLifeCycleDef ecosystemDef "Todo"

    let (todoListView: ViewDef<NoInput, TodoListViewOutput, NoViewError>,
             ecosystemDef) =
        addViewDef ecosystemDef "TodoList"

    {|
        EcosystemDef = ecosystemDef
        LifeCycles =
            {|
                todo = todo
            |}
        Views =
            {|
                todoListView = todoListView
            |}
    |}
