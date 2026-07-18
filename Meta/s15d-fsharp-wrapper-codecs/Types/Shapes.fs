namespace S15D

open System.Threading.Tasks
open Orleans

// S15d spike. Question: does Orleans 10 serialize F# Option/ValueOption/Choice/Result WHOLESALE (so a
// custom IGeneralizedCodec can claim `Option<X>` / `Result<X, E>` as one blob), or does it DECOMPOSE
// them natively (via the FSharp package's FSharpOptionCodec / FSharpResultCodec / FSharpChoiceCodec)
// and delegate each generic arg to that arg's own codec -- meaning the custom codec must claim the
// bare INNER leaf types (X, E), never the wrapper?
//
// This gates the S15b custom serializer (LibLifeCycleCore/src/OrleansEx/Serializer.fs), which registers
// whole wrappers like `Result<unit, GrainRefreshTimersAndSubsError>` and `Option<BlobData>`. If Orleans
// decomposes, those registrations are wrong and the silo startup validator throws
// CodecNotFoundException for the bare inner leaf.
//
// The FSharp package (Microsoft.Orleans.Serialization.FSharp 10.2.1) ships: FSharpUnitCodec,
// FSharpOptionCodec`1, FSharpValueOptionCodec`1, FSharpChoiceCodec`2..6, and -- NEW vs the S15 finding
// -- FSharpResultCodec`2. So Result IS natively decomposed.

[<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("s15d-fsharp-wrapper-codecsCodegen")>]
do ()

// Two F# leaf types (records so System.Text.Json handles them without extra converters). Neither
// carries [<GenerateSerializer>] -- the custom IGeneralizedCodec in the Host claims them.
type PingPayload =
    {
        Seq:     int
        Message: string
    }

type PingError =
    {
        Code:   int
        Reason: string
    }

// Grain whose methods return the leaf types wrapped in F# Option and Result -- the exact shape the
// production ISubjectGrain/ISubjectRepoGrain methods use (e.g. Task<Option<BlobData>>,
// Task<Result<unit, GrainRefreshTimersAndSubsError>>).
type IPingGrain =
    inherit IGrainWithGuidKey
    abstract member GetMaybe: unit -> Task<Option<PingPayload>>
    abstract member TryPing:  n: int -> Task<Result<PingPayload, PingError>>
