module Todo.``Todo Tests``

open System
open FsUnit.Xunit
open SuiteTodo.LifeCycles
open SuiteTodo.Types

[<Simulation>]
let ``Construct todo, toggle done, life event and state reflect the change`` () =
    simulation {
        let! title = genTodoTitle

        let! todo =
            TodoConstructor.New (title, TodoPriority.Medium, None, None)
            |> Ecosystem.construct todoLifeCycle
            |> Ecosystem.thenAssertOk
            |> Ecosystem.thenAssert (fun t -> t.Title = title && not t.Done && t.ArchivedOn.IsNone)

        do! todo
            |> Ecosystem.act todoLifeCycle TodoAction.ToggleDone
            |> Ecosystem.thenAssertOk
            |> Ecosystem.thenAssert (fun t -> t.Done)
            |> Ecosystem.thenIgnore
    }

[<Simulation>]
let ``Empty title is rejected on construct`` () =
    simulation {
        let blankTitle = "   " |> NonemptyString.ofStringUnsafe

        do! TodoConstructor.New (blankTitle, TodoPriority.Medium, None, None)
            |> Ecosystem.construct todoLifeCycle
            |> Ecosystem.thenAssertConstructionOpError
            |> Ecosystem.thenAssert (fun err -> err = TodoOpError.EmptyTitle)
            |> Ecosystem.thenClearAllBadLogs
            |> Ecosystem.thenIgnore
    }

[<Simulation>]
let ``Empty title is rejected on set title`` () =
    simulation {
        let! title = genTodoTitle
        let blankTitle = "   " |> NonemptyString.ofStringUnsafe

        do! TodoConstructor.New (title, TodoPriority.Medium, None, None)
            |> Ecosystem.construct todoLifeCycle
            |> Ecosystem.thenAssertOk
            |> Ecosystem.thenAct todoLifeCycle (TodoAction.SetTitle blankTitle)
            |> Ecosystem.thenAssertTransitionOpError
            |> Ecosystem.thenAssert (fun err -> err = TodoOpError.EmptyTitle)
            |> Ecosystem.thenClearAllBadLogs
            |> Ecosystem.thenIgnore
    }

[<Simulation>]
let ``Done todo auto-archives after archive delay`` () =
    simulation {
        let! title = genTodoTitle

        do! TodoConstructor.New (title, TodoPriority.Medium, None, None)
            |> Ecosystem.construct todoLifeCycle
            |> Ecosystem.thenAssertOk
            |> Ecosystem.thenAct todoLifeCycle TodoAction.ToggleDone
            |> Ecosystem.thenAssertOk
            |> Ecosystem.thenAssert (fun t -> t.Done && t.ArchivedOn.IsNone)
            |> Ecosystem.thenMoveTimeForwardAndRunReminders TodoLifeCycle.archiveDelay
            |> Ecosystem.thenAssertEventually todoLifeCycle (fun t -> t.ArchivedOn.IsSome)
            |> Ecosystem.thenIgnore
    }

[<Simulation>]
let ``Title full-text search finds active todo`` () =
    simulation {
        let! title = genTodoTitle
        let keyword =
            title.Value.Split([|' '|], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.tryHead
            |> Option.defaultValue title.Value

        let! todo =
            TodoConstructor.New (title, TodoPriority.Medium, None, None)
            |> Ecosystem.construct todoLifeCycle
            |> Ecosystem.thenAssertOk

        let query =
            IndexPredicate.And (
                IndexPredicate.EqualToString (TodoStringIndex.ArchiveStatus TodoArchiveStatus.Active),
                IndexPredicate.Matches (UnionCase.ofCase TodoSearchIndex.Title, keyword))
            |> TodoIndex.PrepareQuery
                { Page = { Size = System.UInt16.MaxValue; Offset = 0UL }
                  OrderBy = OrderBy.FastestOrSingleSearchScoreIfAvailable }

        let! matches = Ecosystem.filterFetch todoLifeCycle query

        matches
        |> List.exists (fun t -> t.Id = todo.Id)
        |> should equal true
    }
