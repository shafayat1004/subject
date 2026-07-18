[<AutoOpen>]
module LibLifeCycleHost.BiosphereGrainFactory

open System
open System.Threading
open LibLifeCycle
open LibLifeCycleCore.Certificates
open LibLifeCycleHost
open LibLifeCycleHost.OrleansEx.SiloBuilder
open LibLifeCycleHost.TelemetryModel
open Orleans
open Microsoft.Extensions.Hosting
open Orleans.Hosting
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection

type BiosphereConnectionException(message:string, innerException: System.Exception) =
   inherit System.Exception(message, innerException)

// Lazy creation of remote grain factories.  Why not just create them eagerly, is it expensive?
// Probably expensive, but more importantly we only know statically referenced ecosystems but not dynamic.

type BiosphereGrainProvider (
        hostEcosystem:              Ecosystem,
        hostEcosystemGrainFactory:  IGrainFactory,
        membershipConnectionString: string,
        operationTracker:           OperationTracker,
        useDevelopmentCertificate:  bool) =
    let semaphore = new SemaphoreSlim ((* maxCount *) 1)
    let mutable remoteClientsByEcosystemName: Map<string, IClusterClient> = Map.empty
    let mutable remoteHostsByEcosystemName: Map<string, IHost> = Map.empty
with
    interface IBiosphereGrainProvider with
        member _.GetGrainFactory (ecosystemName: string) =
            if hostEcosystem.Name = ecosystemName then
                // lock-free shortcut for host ecosystem (string comparison penalty should be negligible)
                Task.FromResult hostEcosystemGrainFactory
            else
                match remoteClientsByEcosystemName.TryFind ecosystemName with
                | Some factory ->
                    Task.FromResult factory
                | None ->
                    // interlocked creation. ConcurrentDictionary isn't good enough because GetOrAdd can invoke valueFactory more than once.
                    backgroundTask {

                        do! semaphore.WaitAsync()
                        try
                            // see if was created concurrently
                            match remoteClientsByEcosystemName.TryFind ecosystemName with
                            | Some factory ->
                                return factory :> IGrainFactory
                            | None ->
                                let (orleansTlsCertificate, orleansHostName) =
                                    getOrleansTlsCertificateAndHostName ecosystemName useDevelopmentCertificate

                                let clientHost =
                                    HostBuilder()
                                        .UseOrleansClient(fun _ctx clientBuilder ->
                                            clientBuilder.ConfigureSiloClientForEcosystem(
                                                hostEcosystem,
                                                EcosystemSiloClientSetup.HostToRemoteHost
                                                    { OrleansTlsCertificate      = orleansTlsCertificate
                                                      OrleansHostName            = orleansHostName
                                                      MembershipConnectionString = membershipConnectionString
                                                      OperationTracker           = operationTracker
                                                      RemoteEcosystemName        = ecosystemName })
                                            |> ignore)
                                        .Build()

                                try
                                    do! clientHost.StartAsync()
                                with
                                | ex ->
                                    (clientHost :> IDisposable).Dispose()
                                    BiosphereConnectionException ($"Unable to connect to referenced ecosystem %s{ecosystemName}", ex)
                                    |> fun ex -> TransientSubjectException("GetGrainFactory", ex.ToString())
                                    |> raise

                                let client = clientHost.Services.GetRequiredService<IClusterClient>()

                                remoteHostsByEcosystemName <-
                                    remoteHostsByEcosystemName
                                    |> Map.add ecosystemName clientHost

                                remoteClientsByEcosystemName <-
                                    remoteClientsByEcosystemName
                                    |> Map.add ecosystemName client
                                return client :> IGrainFactory
                        finally
                            semaphore.Release() |> ignore
                    }

        member _.IsHostedLifeCycle (lcKey: LifeCycleKey) =
            hostEcosystem.Name = lcKey.EcosystemName

        member _.Close () =
            remoteHostsByEcosystemName.Values
            |> Seq.map (fun host -> host.Dispose(); Task.CompletedTask)
            |> Array.ofSeq
            |> Task.WhenAll
