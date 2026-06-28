[<AutoOpen; CodecLib.CodecAutoGenerate>]
module SuiteTodo.Types.Todo

open System

open System

type TodoId = TodoId of Guid
with
    interface SubjectId with
        member this.IdString =
            let (TodoId guid) = this
            guid.ToString("D")

type Todo = {
    Id: TodoId
    Title: NonemptyString
    Done: bool
    ArchivedOn: Option<DateTimeOffset>
    QueuedForDeletion: bool
    CreatedOn: DateTimeOffset
}
with
    interface Subject<TodoId> with
        member this.SubjectCreatedOn =
            this.CreatedOn

        member this.SubjectId =
            this.Id

[<RequireQualifiedAccess>]
type TodoAction =
| SetTitle of NonemptyString
| ToggleDone
| Archive
| Delete
with interface LifeAction

[<RequireQualifiedAccess>]
type TodoOpError =
| EmptyTitle
with interface OpError

[<RequireQualifiedAccess>]
type TodoConstructor =
| New of Title: NonemptyString
with interface Constructor

[<RequireQualifiedAccess>]
type TodoLifeEvent =
| Created
| TitleChanged of NonemptyString
| DoneToggled of bool
| Archived
with interface LifeEvent

[<RequireQualifiedAccess>]
type TodoArchiveStatus =
| Active
| Archived

[<RequireQualifiedAccess>]
type TodoNumericIndex =
| CreatedOn of DateTimeOffset
| ArchivedOn of DateTimeOffset
with
    interface SubjectNumericIndex<TodoOpError> with
        member this.Primitive =
            match this with
            | CreatedOn dt -> IndexedNumber dt.UtcTicks
            | ArchivedOn dt -> IndexedNumber dt.UtcTicks

[<RequireQualifiedAccess>]
type TodoStringIndex =
| ArchiveStatus of TodoArchiveStatus
with
    interface SubjectStringIndex<TodoOpError> with
        member this.Primitive =
            match this with
            | ArchiveStatus TodoArchiveStatus.Active -> IndexedString "Active"
            | ArchiveStatus TodoArchiveStatus.Archived -> IndexedString "Archived"

[<RequireQualifiedAccess>]
type TodoSearchIndex =
| Title of NonemptyString
with
    interface SubjectSearchIndex with
        member this.Primitive =
            match this with
            | Title title -> IndexedPrimitiveSearchableText title.Value

type TodoGeographyIndex = NoGeographyIndex

type TodoIndex() =
    inherit SubjectIndex<TodoIndex, TodoNumericIndex, TodoStringIndex, TodoSearchIndex, TodoGeographyIndex, TodoOpError>()

module TodoValidation =
    let isBlankTitle (title: NonemptyString) =
        System.String.IsNullOrWhiteSpace title.Value


////////////////////////////////
// Generated code starts here //
////////////////////////////////

#if !FABLE_COMPILER

open CodecLib

#nowarn "69"


type TodoId with
    static member TypeLabel () = "Todo_TodoId"

    static member private get_ObjCodec_AllCases () =
        function
        | TodoId _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoId _ -> Some 0)
                and! payload = reqWith Codecs.guid "TodoId" (function TodoId x -> Some x)
                return TodoId payload
            }
        |> mergeUnionCases

    static member private get_ObjCodec () = TodoId.get_ObjCodec_AllCases ()
    static member get_Codec () = ofObjCodec <| TodoId.get_ObjCodec ()
    static member Init (typeLabel: string, _typeParams: _) = initializeInterfaceImplementation<SubjectId, TodoId> (fun () -> attachCodecTypeLabel ("__type_" + typeLabel) <| TodoId.get_ObjCodec ())


type Todo with
    static member TypeLabel () = "Todo"

    static member private get_ObjCodec_V1 () =
        codec {
            let! _version = reqWith Codecs.int "__v1" (fun _ -> Some 0)
            and! id = reqWith codecFor<_, TodoId> "Id" (fun x -> Some x.Id)
            and! title = reqWith codecFor<_, NonemptyString> "Title" (fun x -> Some x.Title)
            and! done_ = reqWith Codecs.boolean "Done" (fun x -> Some x.Done)
            and! archivedOn = reqWith (Codecs.option Codecs.dateTimeOffset) "ArchivedOn" (fun x -> Some x.ArchivedOn)
            and! queuedForDeletion = reqWith Codecs.boolean "QueuedForDeletion" (fun x -> Some x.QueuedForDeletion)
            and! createdOn = reqWith Codecs.dateTimeOffset "CreatedOn" (fun x -> Some x.CreatedOn)
            return {
                Id = id
                Title = title
                Done = done_
                ArchivedOn = archivedOn
                QueuedForDeletion = queuedForDeletion
                CreatedOn = createdOn
             }
        }

    static member private get_ObjCodec () = Todo.get_ObjCodec_V1 ()
    static member get_Codec () = ofObjCodec <| Todo.get_ObjCodec ()
    static member Init (typeLabel: string, _typeParams: _) = initializeInterfaceImplementation<Subject<TodoId>, Todo> (fun () -> attachCodecTypeLabel ("__type_" + typeLabel) <| Todo.get_ObjCodec ())


type TodoAction with
    static member TypeLabel () = "TodoAction"

    static member private get_ObjCodec_AllCases () =
        function
        | TodoAction.SetTitle _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoAction.SetTitle _ -> Some 0 | _ -> None)
                and! payload = reqWith codecFor<_, NonemptyString> "SetTitle" (function TodoAction.SetTitle x -> Some x | _ -> None)
                return TodoAction.SetTitle payload
            }
        | TodoAction.ToggleDone ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoAction.ToggleDone -> Some 0 | _ -> None)
                and! _ = reqWith Codecs.unit "ToggleDone" (function TodoAction.ToggleDone -> Some () | _ -> None)
                return TodoAction.ToggleDone
            }
        | TodoAction.Archive ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoAction.Archive -> Some 0 | _ -> None)
                and! _ = reqWith Codecs.unit "Archive" (function TodoAction.Archive -> Some () | _ -> None)
                return TodoAction.Archive
            }
        | TodoAction.Delete ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoAction.Delete -> Some 0 | _ -> None)
                and! _ = reqWith Codecs.unit "Delete" (function TodoAction.Delete -> Some () | _ -> None)
                return TodoAction.Delete
            }
        |> mergeUnionCases

    static member private get_ObjCodec () = TodoAction.get_ObjCodec_AllCases ()
    static member get_Codec () = ofObjCodec <| TodoAction.get_ObjCodec ()
    static member Init (typeLabel: string, _typeParams: _) = initializeInterfaceImplementation<LifeAction, TodoAction> (fun () -> attachCodecTypeLabel ("__type_" + typeLabel) <| TodoAction.get_ObjCodec ())


type TodoOpError with
    static member TypeLabel () = "TodoOpError"

    static member private get_ObjCodec_AllCases () =
        function
        | TodoOpError.EmptyTitle ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (fun _ -> Some 0)
                and! _ = reqWith Codecs.unit "EmptyTitle" (fun _ -> Some ())
                return TodoOpError.EmptyTitle
            }
        |> mergeUnionCases

    static member private get_ObjCodec () = TodoOpError.get_ObjCodec_AllCases ()
    static member get_Codec () = ofObjCodec <| TodoOpError.get_ObjCodec ()
    static member Init (typeLabel: string, _typeParams: _) = initializeInterfaceImplementation<OpError, TodoOpError> (fun () -> attachCodecTypeLabel ("__type_" + typeLabel) <| TodoOpError.get_ObjCodec ())


type TodoConstructor with
    static member TypeLabel () = "TodoConstructor"

    static member private get_ObjCodec_AllCases () =
        function
        | TodoConstructor.New _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (fun _ -> Some 0)
                and! payload = reqWith codecFor<_, NonemptyString> "New" (function TodoConstructor.New x -> Some x)
                return TodoConstructor.New payload
            }
        |> mergeUnionCases

    static member private get_ObjCodec () = TodoConstructor.get_ObjCodec_AllCases ()
    static member get_Codec () = ofObjCodec <| TodoConstructor.get_ObjCodec ()
    static member Init (typeLabel: string, _typeParams: _) = initializeInterfaceImplementation<Constructor, TodoConstructor> (fun () -> attachCodecTypeLabel ("__type_" + typeLabel) <| TodoConstructor.get_ObjCodec ())


type TodoLifeEvent with
    static member TypeLabel () = "Todo_TodoLifeEvent"

    static member private get_ObjCodec_AllCases () =
        function
        | TodoLifeEvent.Created ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoLifeEvent.Created -> Some 0 | _ -> None)
                and! _ = reqWith Codecs.unit "Created" (function TodoLifeEvent.Created -> Some () | _ -> None)
                return TodoLifeEvent.Created
            }
        | TodoLifeEvent.TitleChanged _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoLifeEvent.TitleChanged _ -> Some 0 | _ -> None)
                and! payload = reqWith codecFor<_, NonemptyString> "TitleChanged" (function TodoLifeEvent.TitleChanged x -> Some x | _ -> None)
                return TodoLifeEvent.TitleChanged payload
            }
        | TodoLifeEvent.DoneToggled _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoLifeEvent.DoneToggled _ -> Some 0 | _ -> None)
                and! payload = reqWith Codecs.boolean "DoneToggled" (function TodoLifeEvent.DoneToggled x -> Some x | _ -> None)
                return TodoLifeEvent.DoneToggled payload
            }
        | TodoLifeEvent.Archived ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoLifeEvent.Archived -> Some 0 | _ -> None)
                and! _ = reqWith Codecs.unit "Archived" (function TodoLifeEvent.Archived -> Some () | _ -> None)
                return TodoLifeEvent.Archived
            }
        |> mergeUnionCases

    static member private get_ObjCodec () = TodoLifeEvent.get_ObjCodec_AllCases ()
    static member get_Codec () = ofObjCodec <| TodoLifeEvent.get_ObjCodec ()
    static member Init (typeLabel: string, _typeParams: _) = initializeInterfaceImplementation<LifeEvent, TodoLifeEvent> (fun () -> attachCodecTypeLabel ("__type_" + typeLabel) <| TodoLifeEvent.get_ObjCodec ())


type TodoArchiveStatus with
    static member private get_ObjCodec_AllCases () =
        function
        | TodoArchiveStatus.Active ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoArchiveStatus.Active -> Some 0 | _ -> None)
                and! _ = reqWith Codecs.unit "Active" (function TodoArchiveStatus.Active -> Some () | _ -> None)
                return TodoArchiveStatus.Active
            }
        | TodoArchiveStatus.Archived ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoArchiveStatus.Archived -> Some 0 | _ -> None)
                and! _ = reqWith Codecs.unit "Archived" (function TodoArchiveStatus.Archived -> Some () | _ -> None)
                return TodoArchiveStatus.Archived
            }
        |> mergeUnionCases
    static member get_Codec () = ofObjCodec (TodoArchiveStatus.get_ObjCodec_AllCases ())


type TodoNumericIndex with
    static member private get_ObjCodec_AllCases () =
        function
        | TodoNumericIndex.CreatedOn _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoNumericIndex.CreatedOn _ -> Some 0 | _ -> None)
                and! payload = reqWith Codecs.dateTimeOffset "CreatedOn" (function TodoNumericIndex.CreatedOn x -> Some x | _ -> None)
                return TodoNumericIndex.CreatedOn payload
            }
        | TodoNumericIndex.ArchivedOn _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoNumericIndex.ArchivedOn _ -> Some 0 | _ -> None)
                and! payload = reqWith Codecs.dateTimeOffset "ArchivedOn" (function TodoNumericIndex.ArchivedOn x -> Some x | _ -> None)
                return TodoNumericIndex.ArchivedOn payload
            }
        |> mergeUnionCases
    static member get_Codec () = ofObjCodec (TodoNumericIndex.get_ObjCodec_AllCases ())


type TodoStringIndex with
    static member private get_ObjCodec_AllCases () =
        function
        | TodoStringIndex.ArchiveStatus _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (fun _ -> Some 0)
                and! payload = reqWith codecFor<_, TodoArchiveStatus> "ArchiveStatus" (function ArchiveStatus x -> Some x)
                return TodoStringIndex.ArchiveStatus payload
            }
        |> mergeUnionCases
    static member get_Codec () = ofObjCodec (TodoStringIndex.get_ObjCodec_AllCases ())


type TodoSearchIndex with
    static member private get_ObjCodec_AllCases () =
        function
        | TodoSearchIndex.Title _ ->
            codec {
                let! _version = reqWith Codecs.int "__v1" (function TodoSearchIndex.Title _ -> Some 0)
                and! payload = reqWith codecFor<_, NonemptyString> "Title" (function TodoSearchIndex.Title x -> Some x)
                return TodoSearchIndex.Title payload
            }
        |> mergeUnionCases
    static member get_Codec () = ofObjCodec (TodoSearchIndex.get_ObjCodec_AllCases ())

#endif // !FABLE_COMPILER
