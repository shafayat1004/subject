namespace LibLifeCycleCore

open System
open System.Threading
open System.Threading.Tasks
open System.Transactions
open LibLifeCycleCore.OrleansEx.TraceContextGrainCallFilter
open Orleans
open Orleans.Configuration
open Orleans.Connections.Security
open Orleans.Hosting
open Orleans.Messaging
open Orleans.Runtime.Membership
open LibLifeCycleCore
open LibLifeCycleCore.Certificates
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open LibLifeCycleCore.OrleansEx

// need this for external native clients, to avoid escalation to distributed transaction
// No need to use within host because it doesn't create a TransactionScope
type private AdoNetGatewayListProviderWithSuppressedTransaction (adoNetProvider: AdoNetGatewayListProvider) =
    interface IGatewayListProvider with
        member _.GetGateways() : Task<_> =
            backgroundTask {
                use _ = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)
                return! adoNetProvider.GetGateways()
            }

        member _.InitializeGatewayListProvider() : Task =
            backgroundTask {
                use _ = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled)
                return! adoNetProvider.InitializeGatewayListProvider()
            }

        member _.IsUpdatable : bool = adoNetProvider.IsUpdatable

        member _.MaxStaleness = adoNetProvider.MaxStaleness

module private Common =
    let createGrainConnector (serviceProvider: IServiceProvider) =
        let grainFactory = serviceProvider.GetRequiredService<IGrainFactory>()
        let grainPartition = serviceProvider.GetRequiredService<GrainPartition>()
        GrainConnector(grainFactory, grainPartition, SessionHandle.NoSession, CallOrigin.Internal)


type GrainConnectorProvider =
    inherit IDisposable
    abstract member CreateGrainConnector: unit -> GrainConnector


type AdoNetClusteringGrainConnectorProvider
    (ecosystemDef: EcosystemDef,
     membershipConnectionString: string,
     retryFilter:                exn -> Task<bool>,
     useDevelopmentCertificate:  bool) =

    let cancellationTokenSource = new CancellationTokenSource()
    let mutable host = Unchecked.defaultof<IHost>

    let rec buildAndStartClusterClient (attempt: int) : IHost =
        let clientHost =
            HostBuilder()
                .UseOrleansClient(fun _ctx (clientBuilder: IClientBuilder) ->
                    let (orleansTlsCertificate, orleansHostName) =
                        getOrleansTlsCertificateAndHostName ecosystemDef.Name useDevelopmentCertificate

                    clientBuilder
                        .UseTls(orleansTlsCertificate,
                            fun options ->
                                options.OnAuthenticateAsClient <-
                                    fun _connection sslOptions ->
                                        // Actual value doesn't matter, just required for SSL validation
                                        sslOptions.TargetHost <- orleansHostName
                                // Dev uses a self-signed cert (see Certificates); trust it so the client can
                                // validate the gateway. Production keeps default chain validation.
                                if useDevelopmentCertificate then
                                    options.AllowAnyRemoteCertificate() |> ignore)
                        .ConfigureServices(fun services ->
                            // Decompiled UseAdoNetClustering with wrapped gateway list provider
                            services.Configure(
                                fun (adoNetOptions: AdoNetClusteringClientOptions) ->
                                    adoNetOptions.ConnectionString <- membershipConnectionString
                                    adoNetOptions.Invariant        <- "Microsoft.Data.SqlClient")
                            |> ignore
                            services.AddSingleton<AdoNetGatewayListProvider, AdoNetGatewayListProvider>()                     |> ignore
                            services.AddSingleton<IGatewayListProvider, AdoNetGatewayListProviderWithSuppressedTransaction>() |> ignore
                            services.AddSingleton<IConfigurationValidator, AdoNetClusteringClientOptionsValidator>()          |> ignore
                            Serialization.registerSerializers ecosystemDef.LifeCycleDefs ecosystemDef.ViewDefs services
                            services.AddSingleton<IGrainFactory>(fun sp -> sp.GetRequiredService<IClusterClient>() :> IGrainFactory)
                            |> ignore)
                        .Configure<ClusterOptions>(
                            fun (opts: ClusterOptions) ->
                                opts.ClusterId <- ecosystemDef.Name
                                opts.ServiceId <- ecosystemDef.Name)
                        .AddOutgoingGrainCallFilter<TraceContextOutgoingGrainCallFilter>()
                    |> ignore)
                .Build()

        try
            clientHost
                .StartAsync(cancellationTokenSource.Token)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
            clientHost
        with ex ->
            (clientHost :> IDisposable).Dispose()
            if cancellationTokenSource.Token.IsCancellationRequested then
                raise ex
            else
                let retry =
                    (retryFilter ex)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult()
                if retry then
                    buildAndStartClusterClient (attempt + 1)
                else
                    raise ex

    do host <- buildAndStartClusterClient 1

    let serviceProvider =
        ServiceCollection()
            .AddSingleton<IGrainFactory>(fun _ -> host.Services.GetRequiredService<IClusterClient>() :> IGrainFactory)
            .AddScoped<GrainPartition>(fun _ -> defaultGrainPartition)
            .BuildServiceProvider()

    interface IDisposable with
        member this.Dispose() =
            cancellationTokenSource.Cancel()
            (cancellationTokenSource :> IDisposable).Dispose()
            (host :> IDisposable).Dispose()

    interface GrainConnectorProvider with
        member _.CreateGrainConnector() = Common.createGrainConnector serviceProvider
