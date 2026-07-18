namespace S15D_Grains

open System.Threading.Tasks
open Orleans
open S15D

// F# grain impl (scanned by the C# Codegen project via GenerateCodeForDeclaringAssembly). Returns the
// leaf types wrapped in F# Option and Result -- the invoker codegen references Option<PingPayload> and
// Result<PingPayload, PingError>, which Orleans resolves via FSharpOptionCodec / FSharpResultCodec,
// each delegating to the inner leaf codec.
type PingGrain() =
    inherit Grain()

    interface IPingGrain with
        member _.GetMaybe() : Task<Option<PingPayload>> =
            Task.FromResult(Some { Seq = 7; Message = "hello" })

        member _.TryPing(n: int) : Task<Result<PingPayload, PingError>> =
            if n < 0 then Task.FromResult(Error { Code = 400; Reason = "negative" })
            else Task.FromResult(Ok { Seq = n; Message = sprintf "ping-%d" n })
