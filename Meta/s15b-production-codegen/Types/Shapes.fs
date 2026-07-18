namespace S15BPRODUCTIONCODEGEN

open System
open System.Threading.Tasks
open Orleans
open Orleans.Concurrency

// F# types are NOT annotated with [<GenerateSerializer>]. The custom IGeneralizedCodec in the Host
// claims them via IsSupportedType and serializes them via System.TextJson (emulating the existing
// Fleece-based wire serializer in LibLifeCycleCore/src/OrleansEx/Serializer.fs). This sidesteps S15
// finding #7 (source-generated F# DU codecs mis-route multi-case DUs).

// InternalsVisibleTo: F# nullary union cases (e.g. | Foo) compile to private nested classes the C#
// source generator references when emitting invoker code. Without this attribute, those references
// fail with CS0122. dotnet/orleans issue #8717 (gfix confirmed the same workaround).
[<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("S15b-production-codegenCodegen")>]
do ()

// === F# types (mirror real subject shapes from SuiteTodo/Ecosystem/Todo.Types) ===

[<RequireQualifiedAccess>]
type Priority = | Low | Medium | High   // all nullary cases

[<RequireQualifiedAccess>]
type Category = | Personal | Work        // nullary cases (S15 finding #7 repro shape)

type TodoId = Guid

[<RequireQualifiedAccess>]
type TodoAction =
    | SetTitle of string
    | ToggleDone       // nullary -- exercises InternalsVisibleTo workaround
    | SetPriority of Priority
    | SetCategory of Option<Category>
    | Archive          // nullary -- exercises InternalsVisibleTo workaround

type Todo =
    {
        Id:       TodoId
        Title:    string
        Done:     bool
        Priority: Priority
        Category: Option<Category>
        DueOn:    Option<DateTimeOffset>
    }

[<RequireQualifiedAccess>]
type TodoOpError =
    | EmptyTitle       // nullary
    | Conflict of Guid
    | NotFound         // nullary

type BlobData =
    {
        BlobId: Guid
        Bytes:  byte[]
    }

// === Grain interfaces ===
// Tupled arg form (a: A * b: B -> ...) is required for Orleans interop (curried form compiles to
// FSharpFunc, which the Orleans runtime cannot dispatch).

// Non-generic concrete grain interface -- mirrors IBlobRepoGrain from GrainClientInterface.fs
type IBlobSpikeGrain =
    inherit IGrainWithGuidKey
    [<ReadOnly>]
    abstract member GetBlob: id: Guid -> Task<Option<BlobData>>
    abstract member SetBlob: id: Guid * data: BlobData -> Task<unit>

// Generic grain interface -- mirrors IViewClientGrain<'Input, 'Output, 'OpError> from GrainClientInterface.fs
// Uses F# interface constraints (production's real form: 'Input :> ViewInput<'Input> self-referential
// constraints; here simplified to plain interface constraints -- the codegen question is the same).
type ISpikeViewInput = abstract member Validate: unit -> bool
type ISpikeViewOutput = abstract member Display: unit -> string

type GetTodoInput =
    {
        QueryId: Guid
    }
    interface ISpikeViewInput with
        member _.Validate() = true

type GetTodoOutput =
    {
        Todos: Todo list
    }
    interface ISpikeViewOutput with
        member _.Display() = "ok"

type IViewSpikeGrain<'Input, 'Output
    when 'Input  :> ISpikeViewInput
    and  'Output :> ISpikeViewOutput> =
    inherit IGrainWithGuidKey
    [<ReadOnly>]
    abstract member Read: input: 'Input -> Task<Result<'Output, TodoOpError>>
    abstract member Mutate: action: TodoAction -> Task<Result<Todo, TodoOpError>>
