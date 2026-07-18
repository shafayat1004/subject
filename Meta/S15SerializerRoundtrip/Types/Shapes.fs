namespace S15SerializerRoundtrip

open System
open System.Threading.Tasks
open Orleans

module Annotated =

    [<GenerateSerializer>]
    type TodoId =
        | [<Id(0u)>] TodoId of Guid

    let makeTodoId guid = TodoId guid

    [<RequireQualifiedAccess; GenerateSerializer>]
    type Priority =
        | [<Id(0u)>] Low    of unit
        | [<Id(1u)>] Medium of unit
        | [<Id(2u)>] High   of unit

    [<RequireQualifiedAccess; GenerateSerializer>]
    type Category =
        | [<Id(0u)>] Work     of unit
        | [<Id(1u)>] Personal of unit
        | [<Id(2u)>] Shopping of unit
        | [<Id(3u)>] Health   of unit
        | [<Id(4u)>] Other    of unit

    [<RequireQualifiedAccess; GenerateSerializer>]
    type TodoAction =
        | [<Id(0u)>] SetTitle    of string
        | [<Id(1u)>] ToggleDone  of unit
        | [<Id(2u)>] Archive     of unit
        | [<Id(3u)>] Delete      of unit
        | [<Id(4u)>] SetPriority of Priority
        | [<Id(5u)>] SetCategory of Option<Category>
        | [<Id(6u)>] SetDueOn    of Option<DateTimeOffset>

    [<GenerateSerializer>]
    type TodoConstructor =
        | [<Id(0u)>] New of Title : string * Priority : Priority * Category : Option<Category> * DueOn : Option<DateTimeOffset>

    let makeTodoConstructor title priority category dueOn =
        New(title, priority, category, dueOn)

    [<GenerateSerializer>]
    type Todo = {
        [<Id(0u)>] Id                : TodoId
        [<Id(1u)>] Title             : string
        [<Id(2u)>] Done              : bool
        [<Id(3u)>] ArchivedOn        : Option<DateTimeOffset>
        [<Id(4u)>] QueuedForDeletion : bool
        [<Id(5u)>] CreatedOn         : DateTimeOffset
        [<Id(6u)>] Priority          : Priority
        [<Id(7u)>] DueOn             : Option<DateTimeOffset>
        [<Id(8u)>] Category          : Option<Category>
    }

    [<RequireQualifiedAccess; GenerateSerializer>]
    type TodoLifeEvent =
        | [<Id(0u)>] Created      of unit
        | [<Id(1u)>] TitleChanged of string
        | [<Id(2u)>] DoneToggled  of bool
        | [<Id(3u)>] Archived     of unit

    [<RequireQualifiedAccess; GenerateSerializer>]
    type TodoError =
        | [<Id(0u)>] EmptyTitle of unit
        | [<Id(1u)>] Conflict   of Guid

    [<GenerateSerializer>]
    type CollectionsRecord = {
        [<Id(0u)>] Tags       : string list
        [<Id(1u)>] Scores     : int array
        [<Id(2u)>] Categories : Set<Category>
        [<Id(3u)>] Metadata   : Map<string, int>
        [<Id(4u)>] Nested     : Option<Option<int>>
    }

    [<GenerateSerializer>]
    type ResultWrapper = {
        [<Id(0u)>] Value : Result<Todo, TodoError>
    }

    [<GenerateSerializer>]
    type NestedResultWrapper = {
        [<Id(0u)>] Value : Result<Result<Todo, TodoError> list, TodoError>
    }
