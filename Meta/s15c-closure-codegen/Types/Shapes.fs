namespace S15C

open System.Threading.Tasks
open Orleans

// S15c spike types. The primary question is NOT serialization (S15b proved that) -- it is whether
// the Orleans 10 C# source generator misclassifies F# COMPILER-GENERATED CLOSURE classes (from
// `backgroundTask { }` CEs and F# object expressions implementing grain-observer interfaces) as
// InvokableInterfaceImplementations, emitting broken C# referencing F# mangled names (@ / $ chars).
// See CodeGenerator.cs:248-259 (v10.2.1): any non-abstract public/internal class implementing an
// interface annotated (inherited) with [GenerateMethodSerializers] is added to
// InvokableInterfaceImplementations. IGrainObserver : IAddressable, and IAddressable carries
// [GenerateMethodSerializers], so ANY F# type implementing an IGrainObserver-derived interface --
// including compiler-emitted object-expression closures -- gets flagged.

[<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("s15c-closure-codegenCodegen")>]
do ()

// Payload delivered to the observer (mirrors VersionedSubject + LifeEvent shape). The custom
// IGeneralizedCodec in the Host claims + serializes it (no [<GenerateSerializer>]).
type PingPayload =
    {
        Seq:     int
        Message: string
    }

// Grain-observer interface. GENERIC with a constraint -- mirrors the production
// ILifeEventAwaiter<'Subject,'LifeEvent,'SubjectId when ... : comparison> and
// ISubjectGrainObserver<'Subject,'SubjectId> shape. Inherits IGrainObserver so it is grain-callable
// via CreateObjectReference. THIS is the interface whose F# object-expression implementations get
// misclassified.
type IPingObserver<'Payload when 'Payload : equality> =
    inherit IGrainObserver
    abstract member Notify: payload: 'Payload -> unit

// Grain that receives an observer reference and pings it N times (mirrors the ISubjectGrain
// subscribe -> OnUpdate push path).
type IPingGrain =
    inherit IGrainWithGuidKey
    abstract member PingObserver: observer: IPingObserver<PingPayload> * count: int -> Task<unit>
