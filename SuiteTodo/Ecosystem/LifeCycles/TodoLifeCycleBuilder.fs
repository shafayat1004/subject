[<AutoOpen>]
module SuiteTodo.LifeCycles.TodoLifeCycleBuilder

open LibLifeCycle
open SuiteTodo.Types

let newTodoLifeCycle
    (def: LifeCycleDef<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId>) =
    LifeCycleBuilder.newLifeCycle<'Subject, 'LifeAction, 'OpError, 'Constructor, 'LifeEvent, 'SubjectIndex, 'SubjectId, NoSession, NoRole>
        def

let newTodoView
    (def: ViewDef<'Input, 'Output, 'OpError>) =
    ViewBuilder.newView<'Input, 'Output, 'OpError, NoSession, NoRole>
        def
