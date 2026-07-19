module LibLifeCycleHost.OrleansEx.SiloBuilder

#nowarn "0044" // Suppress deprecation Needed for IHostingEnvironment

open System.Net
open System.Security.Cryptography.X509Certificates
open LibLifeCycleHost.ApplicationInsights
open LibLifeCycleHost.Storage.SqlServer
open LibLifeCycleHost.SubjectReminderTable
open LibLifeCycleHost.TelemetryModel
open LibLifeCycleHost.TraceContextGrainCallFilter
open Orleans
open Orleans.Connections.Security
open Orleans.Hosting
open Orleans.Configuration
open Orleans.Runtime
open LibLifeCycleCore.OrleansEx
open LibLifeCycleHost
open System
open System.Runtime.CompilerServices
open LibLifeCycle
open System.Reflection
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options

let private configureSiloEcosystemDefSerializers
    (siloBuilder: ISiloBuilder)
    (lifeCycleDefs: List<LifeCycleDef>)
    (viewDefs: List<IViewDef>)
    : ISiloBuilder =
        siloBuilder
            .ConfigureServices(Serialization.registerSerializers lifeCycleDefs viewDefs)

let private configureSiloClientSerializers
    (clientBuilder: IClientBuilder)
    (lifeCycleDefs: List<LifeCycleDef>)
    (viewDefs: List<IViewDef>)
    : IClientBuilder =
        clientBuilder
            .ConfigureServices(Serialization.registerSerializers lifeCycleDefs viewDefs)

type EcosystemProperSiloSetup =
| ProperSiloProd
| ProperSiloDev of ShouldResetMembershipTable: bool

[<RequireQualifiedAccess>]
type EcosystemSiloSetup =
| Proper of
    EcosystemProperSiloSetup *
    MembershipConnectionString: string *
    AdvertisedIP:               IPAddress *
    SiloPort:                   int *
    GatewayPort:                int *
    ListenOnAnyHostAddress:     bool *
    MaybeDashboardPort:         Option<int>
| Test
| TestDataSeeding


[<RequireQualifiedAccess>]
type EcosystemSiloClientSetup =
| TestCluster
| TestClusterDataSeeding
// can't use public anonymous payload types because LibLifeCycleHostBuild C# codegen freaks out. Why do we even have these types generated??
| ApiToHost        of EcosystemSiloClientSetupApiToHostPayload
| HostToRemoteHost of EcosystemSiloClientSetupHostToRemoteHostPayload

and EcosystemSiloClientSetupApiToHostPayload =
    {
       OrleansTlsCertificate:      X509Certificate2
       OrleansHostName:            string
       MembershipConnectionString: string
       HostingEnvironment:         Microsoft.Extensions.Hosting.IHostingEnvironment // if no longer required, remove #nowarn at the top of the file
       OperationTracker:           OperationTracker
       MaybeAppInsightsConfig:     Option<AppInsightsConfiguration>
    }
and EcosystemSiloClientSetupHostToRemoteHostPayload =
    {
       OrleansTlsCertificate:      X509Certificate2
       OrleansHostName:            string
       MembershipConnectionString: string
       OperationTracker:           OperationTracker
       RemoteEcosystemName:        string
    }

[<Extension>]
type SiloConfigurationExtensions =
    [<Extension>]
    static member ConfigureSiloForEcosystem (
        siloBuilder: ISiloBuilder,
        ecosystem:   Ecosystem,
        siloSetup:   EcosystemSiloSetup)
        : unit =

        siloBuilder
            .Configure<ClusterOptions>(
                fun (opts: ClusterOptions) ->
                    opts.ClusterId <- ecosystem.Name
                    opts.ServiceId <- ecosystem.Name
            )
            .AddOutgoingGrainCallFilter<TraceContextOutgoingGrainCallFilter>()
            .AddIncomingGrainCallFilter<TraceContextIncomingGrainCallFilter>()

        |> fun siloBuilder ->
            // register serializer for both host & referenced ecosystems.
            // Referenced serializers required  to deserialize life events in subscriptions
            // TODO: to minimize chance of collisions register absolute minimum of referenced types.
            Seq.fold
                (fun (siloBuilder: ISiloBuilder) (referencedEcosystem: ReferencedEcosystem) ->
                    let referencedLifeCycleDefs =
                        referencedEcosystem.LifeCycles
                        |> List.map (fun rlc -> rlc.Def)
                    let referencedViewDefs =
                        referencedEcosystem.Views
                        |> List.map (fun rv -> rv.Def)
                    configureSiloEcosystemDefSerializers siloBuilder referencedLifeCycleDefs referencedViewDefs)
                (configureSiloEcosystemDefSerializers siloBuilder ecosystem.LifeCycleDefs ecosystem.ViewDefs)
                ecosystem.ReferencedEcosystems.Values

        |> fun siloBuilder ->
            match siloSetup with
            | EcosystemSiloSetup.Test ->
                siloBuilder
                    .UseInMemoryReminderService()
                    .ConfigureLogging(fun logging ->
                        logging
                            .ClearProviders()
                            .AddConsole() |> ignore)

                    .Configure<ClientMessagingOptions>(
                        fun(opts: ClientMessagingOptions) ->
                            opts.ResponseTimeout <- TimeSpan.FromSeconds 60.
                            opts.ResponseTimeoutWithDebugger <- TimeSpan.FromDays 1.
                    )
                    .Configure<SiloMessagingOptions>(
                        fun(opts: SiloMessagingOptions) ->
                             opts.ResponseTimeout <- TimeSpan.FromSeconds 60.
                             opts.ResponseTimeoutWithDebugger <- TimeSpan.FromDays 1.
                    )


            | EcosystemSiloSetup.TestDataSeeding ->
                siloBuilder
                    .AddStartupTask<SqlServerSetupStartupTask>(ServiceLifecycleStage.First)
                    .AddStartupTask<CustomStorageInitStartupTask>(ServiceLifecycleStage.First)
                    .UseInMemoryReminderService()
                    .ConfigureLogging(fun logging ->
                        logging
                            .ClearProviders()
                            .AddConsole() |> ignore)

                    .Configure<ClientMessagingOptions>(
                        fun(opts: ClientMessagingOptions) ->
                            opts.ResponseTimeout <- TimeSpan.FromSeconds 60.
                            opts.ResponseTimeoutWithDebugger <- TimeSpan.FromDays 1.
                    )
                    .Configure<SiloMessagingOptions>(
                        fun(opts: SiloMessagingOptions) ->
                             opts.ResponseTimeout <- TimeSpan.FromSeconds 60.
                             opts.ResponseTimeoutWithDebugger <- TimeSpan.FromDays 1.
                    )


            | EcosystemSiloSetup.Proper (properSiloSetup, membershipConnectionString, advertisedIP, siloPort, gatewayPort, listenOnAnyHostAddress, maybeDashboardPort) ->
                let shouldResetMembershipTable =
                    match properSiloSetup with
                    | ProperSiloDev isSingleProcess ->
                        isSingleProcess
                    | ProperSiloProd ->
                        false

                siloBuilder
                    .ConfigureEndpoints(advertisedIP, siloPort, gatewayPort, listenOnAnyHostAddress)
                    // Ideally we want to just be able to say .UseTls(x509), but unfortunately there's no way to inject in configuration
                    .Configure<SiloConnectionOptions>(
                        fun (options: SiloConnectionOptions) ->
                            options.ConfigureSiloInboundConnection(
                                fun connectionBuilder ->
                                    connectionBuilder.ApplicationServices.GetRequiredService<TlsOptions>()
                                    |> connectionBuilder.UseServerTls
                            )

                            options.ConfigureGatewayInboundConnection(
                                fun connectionBuilder ->
                                    connectionBuilder.ApplicationServices.GetRequiredService<TlsOptions>()
                                    |> connectionBuilder.UseServerTls
                            )

                            options.ConfigureSiloOutboundConnection(
                                fun connectionBuilder ->
                                    connectionBuilder.ApplicationServices.GetRequiredService<TlsOptions>()
                                    |> connectionBuilder.UseClientTls
                            )
                    )
                    .AddStartupTask<SqlServerSetupStartupTask>(ServiceLifecycleStage.First)
                    .AddStartupTask<CustomStorageInitStartupTask>(ServiceLifecycleStage.First)
                    .AddStartupTask<LifeCycleHostStartupTask>(ServiceLifecycleStage.Active)
                    // Orleans 7+ split reminders into Microsoft.Orleans.Reminders; the reminder service
                    // (IReminderRegistry) is no longer auto-registered by core. Registering a custom
                    // IReminderTable alone is not enough -- without AddReminders(), SubjectGrain.SetTickReminder
                    // throws "No service for type 'Orleans.Timers.IReminderRegistry'". The Test/TestDataSeeding
                    // branches get this implicitly via UseInMemoryReminderService(); the Proper branch must
                    // register the service explicitly, then supply the custom table below.
                    .AddReminders()
                    .ConfigureServices(fun services ->
                        services.AddSingleton<IReminderTable, SubjectReminderTable>(fun serviceProvider ->
                            let isDevHost = match properSiloSetup with | ProperSiloDev _ -> true | ProperSiloProd -> false
                            SubjectReminderTable(
                                serviceProvider.GetRequiredService<SqlServerConnectionStrings>(),
                                serviceProvider.GetRequiredService<Ecosystem>(),
                                serviceProvider.GetRequiredService<IGrainFactory>(),
                                serviceProvider.GetRequiredService<HostedLifeCycleAdapterCollection>(),
                                serviceProvider.GetRequiredService<ILogger<SubjectReminderTable>>(),
                                serviceProvider.GetRequiredService<Service<Clock>>(),
                                serviceProvider.GetRequiredService<IOptions<ConsistentRingOptions>>(),
                                isDevHost))
                        |> ignore)
                    .UseAdoNetClustering(fun (adoNetOptions: AdoNetClusteringSiloOptions) ->
                        if shouldResetMembershipTable then
                            // This is needed on dev environments where the process may not have closed cleanly
                            // and there's no other server to mark this one as dead
                            SqlServerSetup.runOrleansSetup membershipConnectionString
                            SqlServerSetup.resetMembershipTable membershipConnectionString ecosystem.Name

                        adoNetOptions.ConnectionString <- membershipConnectionString
                        adoNetOptions.Invariant        <- "Microsoft.Data.SqlClient"
                    )
                    |> fun siloBuilder ->
                            match maybeDashboardPort with
                            | Some _dashboardEndpointPort ->
                                // TODO S11: Orleans 10 dashboard moved to Microsoft.Orleans.Dashboard 10.2.1 + ASP.NET Core MapOrleansDashboard;
                                // DashboardOptions has no Port. Wire during S11 observability spike.
                                siloBuilder
                            | None ->
                                siloBuilder
        |> ignore

    [<Extension>]
    static member ConfigureSiloClientForEcosystem (
        clientBuilder: IClientBuilder,
        hostEcosystem: Ecosystem,
        clientSetup:   EcosystemSiloClientSetup)
        : IClientBuilder =

        let ecosystemName =
            match clientSetup with
            | EcosystemSiloClientSetup.TestCluster
            | EcosystemSiloClientSetup.TestClusterDataSeeding
            | EcosystemSiloClientSetup.ApiToHost _ ->
                hostEcosystem.Name
            | EcosystemSiloClientSetup.HostToRemoteHost x ->
                x.RemoteEcosystemName

        clientBuilder
            .Configure<ClusterOptions>(
                fun (opts: ClusterOptions) ->
                    opts.ClusterId <- ecosystemName
                    opts.ServiceId <- ecosystemName
            )
            .AddOutgoingGrainCallFilter<TraceContextOutgoingGrainCallFilter>()

        |> fun clientBuilder ->
            match clientSetup with
            | EcosystemSiloClientSetup.TestCluster
            | EcosystemSiloClientSetup.TestClusterDataSeeding ->
                // Orleans 10's client-startup AnalyzeSerializerAvailability eagerly decomposes declared
                // grain param/return types (Option/Result/tuples) and asks the DI for an IFieldCodec<T>
                // for each bare leaf. Without registering EggShellSubjectGrainsCodec on the client the
                // validator throws CodecNotFoundException for any user F# leaf (BlobData,
                // GrainRefreshTimersAndSubsError, ...). Under Orleans 3.7 codec resolution was lazy, so
                // this registration was silently skipped for the in-process TestCluster; Orleans 10 makes
                // the absence fatal. See spikes/s15d-fsharp-wrapper-codecs.md.
                (configureSiloClientSerializers clientBuilder hostEcosystem.LifeCycleDefs hostEcosystem.ViewDefs)
                // Sadly ClientBuilder and the host container don't share the service container
                // so we have to explicitly bind services that we expect the Orleans client
                // to use
                // https://github.com/dotnet/orleans/issues/4744
                    .ConfigureServices(fun services ->
                        services
                            .AddSingleton<OperationTracker>(noopOperationTracker)
                            |> ignore
                    )

            | EcosystemSiloClientSetup.ApiToHost x ->
                (configureSiloClientSerializers clientBuilder hostEcosystem.LifeCycleDefs hostEcosystem.ViewDefs)
                    // Sadly ClientBuilder and ASP.NET Core don't share the service container
                    // so we have to explicitly bind services that we expect the Orleans client
                    // to use
                    // https://github.com/dotnet/orleans/issues/4744
                    .ConfigureServices(fun services ->
                        services
                            // This is needed by clientBuilder.AddApplicationInsightsTelemetryConsumer below
                            .AddSingleton<Microsoft.Extensions.Hosting.IHostingEnvironment>(x.HostingEnvironment) // if no longer required, remove #nowarn at the top of the file
                            .AddSingleton<OperationTracker>(x.OperationTracker)
                            |> ignore
                    )
                    .UseTls(x.OrleansTlsCertificate,
                        fun options ->
                            options.OnAuthenticateAsClient <-
                                fun _connection sslOptions ->
                                    // Actual value doesn't matter, just required for SSL validation
                                    sslOptions.TargetHost <- x.OrleansHostName
                    )
                    .UseAdoNetClustering(
                        fun (adoNetOptions: AdoNetClusteringClientOptions) ->
                            adoNetOptions.ConnectionString <- x.MembershipConnectionString
                            adoNetOptions.Invariant        <- "Microsoft.Data.SqlClient"
                    )

            | EcosystemSiloClientSetup.HostToRemoteHost x ->
                match hostEcosystem.ReferencedEcosystems.TryFind x.RemoteEcosystemName with
                | None ->
                    // technically for dynamic ecosystems we need only subscription dispatcher interface, but registering whole ecosystem will do anyway
                    configureSiloClientSerializers clientBuilder hostEcosystem.LifeCycleDefs hostEcosystem.ViewDefs
                | Some referencedEcosystem ->
                    let referencedLifeCyclesDefs =
                        referencedEcosystem.LifeCycles
                        |> List.map (fun rlc -> rlc.Def)
                    let referencedViewDefs =
                        referencedEcosystem.Views
                        |> List.map (fun rv -> rv.Def)
                    configureSiloClientSerializers clientBuilder referencedLifeCyclesDefs referencedViewDefs

                |> fun clientBuilder ->
                    clientBuilder
                        .ConfigureServices(fun services ->
                            services
                                .AddSingleton<OperationTracker>(x.OperationTracker)
                                |> ignore
                        )
                        .UseTls(x.OrleansTlsCertificate,
                            fun options ->
                                options.OnAuthenticateAsClient <-
                                    fun _connection sslOptions ->
                                        // Actual value doesn't matter, just required for SSL validation
                                        sslOptions.TargetHost <- x.OrleansHostName
                        )
                        .UseAdoNetClustering(
                            fun (adoNetOptions: AdoNetClusteringClientOptions) ->
                                adoNetOptions.ConnectionString <- x.MembershipConnectionString
                                adoNetOptions.Invariant        <- "Microsoft.Data.SqlClient"
                        )
