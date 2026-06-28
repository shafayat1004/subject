[<AutoOpen; CodecLib.CodecAutoGenerate>]
module SuiteTodo.Types.TodoListView

open System

#if !FABLE_COMPILER
open CodecLib
#nowarn "69"
#endif

type TodoListItem = {
    Id: TodoId
    Title: NonemptyString
    Done: bool
    CreatedOn: DateTimeOffset
}

#if !FABLE_COMPILER

type TodoListItem with
    static member private get_ObjCodec_V1 () =
        codec {
            let! _version = reqWith Codecs.int "__v1" (fun _ -> Some 0)
            and! id = reqWith codecFor<_, TodoId> "Id" (fun x -> Some x.Id)
            and! title = reqWith codecFor<_, NonemptyString> "Title" (fun x -> Some x.Title)
            and! done_ = reqWith Codecs.boolean "Done" (fun x -> Some x.Done)
            and! createdOn = reqWith Codecs.dateTimeOffset "CreatedOn" (fun x -> Some x.CreatedOn)
            return {
                Id = id
                Title = title
                Done = done_
                CreatedOn = createdOn
             }
        }

    static member get_Codec () = ofObjCodec (TodoListItem.get_ObjCodec_V1 ())

#endif

type TodoListViewOutput = {
    Items: list<TodoListItem>
}
with
#if !FABLE_COMPILER
    static member TypeLabel () = "TodoListViewOutput"

    static member private get_ObjCodec_V1 () =
        codec {
            let! _version = reqWith Codecs.int "__v1" (fun _ -> Some 0)
            and! items = reqWith (Codecs.list codecFor<_, TodoListItem>) "Items" (fun x -> Some x.Items)
            return { Items = items }
        }

    static member private get_ObjCodec () = TodoListViewOutput.get_ObjCodec_V1 ()
    static member get_Codec () = ofObjCodec <| TodoListViewOutput.get_ObjCodec ()
    static member Init (typeLabel: string, _typeParams: _) =
        initializeInterfaceImplementation<ViewOutput<TodoListViewOutput>, TodoListViewOutput>
            (fun () -> attachCodecTypeLabel ("__type_" + typeLabel) <| TodoListViewOutput.get_ObjCodec ())

    interface ViewOutput<TodoListViewOutput> with
        static member Codec () = TodoListViewOutput.get_Codec ()
#else
    interface ViewOutput<TodoListViewOutput>
#endif
