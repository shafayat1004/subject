namespace LibLifeCycleHost.Host.K8S

#nowarn "0044" // Suppress deprecation Needed for IHostingEnvironment

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.Cors.Infrastructure
open System
open Microsoft.Extensions.Configuration
open LibLifeCycleHost
open LibLifeCycleHost.Web
open Microsoft.Extensions.Primitives
open Orleans
open Orleans.Hosting
open LibLifeCycleHost.Storage.SqlServer
open LibLifeCycleHost.OrleansEx.SiloBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open System.Threading.Tasks
open Microsoft.AspNetCore.HttpOverrides
open System.Net
open Microsoft.AspNetCore.DataProtection
open LibLifeCycleHost.Web.Config
open LibLifeCycleHost.Web.HttpHandler
open LibLifeCycleHost.Web.RealTimeHandler
open LibLifeCycleHost.Certificates
open Microsoft.AspNetCore.Server.Kestrel.Https
open LibLifeCycle
open LibLifeCycle.Config
open LibLifeCycleCore
open LibLifeCycleCore.Certificates
open System.Net.Http
open System.Threading
open LibLifeCycleHost.ApplicationInsights
open LibLifeCycleHost.Host
open System.Reflection
open LibLifeCycleHost.TelemetryModel

type BuildAssembly = {
    Assembly: Assembly
}

// Holds the Orleans 10 IHost that owns the IClusterClient, so InitializeClusterClientService
// can Start/Stop it via the IHostedService lifecycle (Orleans 10 has no IClusterClient.Connect()).
type ClusterClientHostHolder() =
    let mutable host: Microsoft.Extensions.Hosting.IHost option = None
    member _.SetHost(h: Microsoft.Extensions.Hosting.IHost) = host <- Some h
    member _.Host = host |> Option.defaultWith (fun () -> failwith "ClusterClientHostHolder.Host accessed before SetHost")

type Startup(ecosystem: Ecosystem, _buildAssembly: BuildAssembly, hostConfiguration: HostConfiguration, configuration: IConfiguration) =

    // Please excuse the mutable. Unfortunately required due to the way orleans structures its APIs
    // (i.e. we have no direct access to a field that contains the connected gateway count, and instead
    // we get notified of connection status via events
    [<VolatileField>]
    let mutable isClusterClientConnected = false

    let sqlServerConfig = configuration.GetSection("Storage.SqlServer").GetAndValidate<SqlServerConfiguration>()

    let orleansMembershipConnectionString = configuration.GetSection("Orleans.Clustering")["MembershipConnectionString"];

    let certificateConfig      = configuration.GetSection("Certificate").Get<CertificateConfiguration>()
    let maybeAppInsightsConfig = configuration.GetSection("AppInsights").TryGetAndTryValidate<AppInsightsConfiguration>()

    let (orleansTlsCertificate, orleansHostName) = getOrleansTlsCertificateAndHostName ecosystem.Name certificateConfig.UseDevelopmentCertificate

    let configureCors (builder: CorsPolicyBuilder) =
        let lifeCycleNameLower = ecosystem.Name.ToLowerInvariant()
        let customCorsHostNames =
            configuration.GetSection("Application").TryGetAndValidate<ApplicationConfiguration>()
            |> Option.map (fun config -> config.CorsHostNamesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            |> Option.defaultValue [||]
            |> Array.map (sprintf "https://%s")

        builder
            .WithOrigins(
                [|
                    $"https://{lifeCycleNameLower}.dev.subject.dev"
                    $"https://{lifeCycleNameLower}admin.dev.subject.dev"
                    $"https://{lifeCycleNameLower}.subject.dev"
                    $"https://{lifeCycleNameLower}admin.subject.dev"
                    $"https://{lifeCycleNameLower}.dev.subject.app"
                    $"https://{lifeCycleNameLower}admin.dev.subject.app"
                    $"https://{lifeCycleNameLower}.subject.app"
                    $"https://{lifeCycleNameLower}admin.subject.app"
                |]
                |> Array.append customCorsHostNames
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithMethods("GET", "POST", "PUT", "OPTIONS")
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours 0.5)
            .WithExposedHeaders("X-Subject-Id", "X-Total-Count")
        |> ignore

    let webApp =
        choose  [
            GET >=> choose [
                route "/hello" >=> text "world"

                // This is monitored by traefik and the node is blocked from serving traffic,
                // if we lose connectivity to the cluster
                route "/healthcheck" >=> (fun _next ctx ->
                    if isClusterClientConnected then
                        ctx.SetStatusCode 200
                        ctx.WriteStringAsync "OK"
                    else
                        ctx.SetStatusCode 503
                        ctx.WriteStringAsync "No cluster connection")
            ]
        ]

    member _.ConfigureServices(services: IServiceCollection) : unit =
        match maybeAppInsightsConfig with
        | Some config ->
            addTelemetry config.InstrumentationKey (sprintf "%s.Api" ecosystem.Name) services (* developerMode *) false (* isWorkerService *) false
        | None ->
            services.AddSingleton<OperationTracker>(noopOperationTracker) |> ignore

        configuration.GetSection("Http").TryGetAndValidate<HttpCookieConfiguration>()
        |> Option.defaultWith (fun _ -> { DefaultAppCookieDomain = ""; HostNameSuffixToAppCookieDomainQueryString = "" })
        |> services.AddSingleton<HttpCookieConfiguration>
        |> ignore

        services
            .AddSingleton<HttpClient>(new HttpClient(new HttpClientHandler(UseCookies = false), Timeout = TimeSpan.FromMinutes 5.0))
            .AddGiraffe()
            .AddCors()
            .AddEcosystem(ecosystem, EcosystemStorageSetup.Proper)
            .AddSingleton<IBiosphereGrainProvider, BiosphereGrainProvider>(fun serviceProvider ->
                let hostEcosystemGrainFactory = serviceProvider.GetRequiredService<IGrainFactory>()
                let operationTracker = serviceProvider.GetRequiredService<OperationTracker>()
                BiosphereGrainProvider(ecosystem, hostEcosystemGrainFactory, orleansMembershipConnectionString, operationTracker, certificateConfig.UseDevelopmentCertificate))
            .AddRealTimeEndpoints(ecosystem)
            .AddSingleton<ApiSessionCryptographer>(fun serviceProvider ->
                let dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>()
                ApiSessionCryptographer(dataProtectionProvider))
            .AddScoped<GrainPartition>(fun _ -> defaultGrainPartition)
            .AddSingleton<SqlServerConnectionStrings>({ ByEcosystemName = NonemptyMap.ofOneItem (ecosystem.Name, sqlServerConfig.ConnectionString) })
            .AddSingleton<IGrainFactory>(
                fun serviceProvider ->
                    let hostingEnv = serviceProvider.GetRequiredService<IHostingEnvironment>() // if no longer required, remove #nowarn at the top of the file
                    let operationTracker = serviceProvider.GetRequiredService<OperationTracker>()

                    let clientHost =
                        HostBuilder()
                            .UseOrleansClient(fun _ctx clientBuilder ->
                                clientBuilder
                                    .AddGatewayCountChangedHandler(fun _ eventArgs ->
                                        if eventArgs.ConnectionRecovered then
                                            isClusterClientConnected <- true
                                            // todo: report-health to dapr dashboard
                                            //reportHealth ecosystem statelessService ClusterConnectionStatus.Connected
                                    )
                                    .AddClusterConnectionLostHandler(fun _ _ ->
                                        isClusterClientConnected <- false
                                        // todo: report-health to dapr dashboard
                                        // reportHealth ecosystem statelessService ClusterConnectionStatus.ConnectionLost
                                    )
                                    .ConfigureSiloClientForEcosystem(
                                        ecosystem,
                                        EcosystemSiloClientSetup.ApiToHost
                                            { OrleansTlsCertificate      = orleansTlsCertificate
                                              OrleansHostName            = orleansHostName
                                              MembershipConnectionString = orleansMembershipConnectionString
                                              HostingEnvironment         = hostingEnv
                                              OperationTracker           = operationTracker
                                              MaybeAppInsightsConfig     = maybeAppInsightsConfig })
                                |> ignore)
                            .Build()
                    // Expose the host so InitializeClusterClientService can Start/Stop it.
                    serviceProvider
                        .GetRequiredService<ClusterClientHostHolder>()
                        .SetHost(clientHost)
                    clientHost.Services.GetRequiredService<IClusterClient>() :> IGrainFactory
            )
            .AddSingleton<ClusterClientHostHolder>(fun _ -> ClusterClientHostHolder())
            .AddSingleton<HttpsConnectionAdapterOptions>(
                    let defaultCertificate =
                        if certificateConfig.UseDevelopmentCertificate then
                            starDotDevDotSubjectDotAppTlsCertificate
                        else
                            allSubjectDotAppTlsCertificates.Value
                            |> tryGetPreferredSubjectDotAppTlsCertificate DateTimeOffset.Now
                            |> Option.get

                    let sniSelector =
                        if certificateConfig.UseDevelopmentCertificate then
                            [starDotDevDotSubjectDotAppTlsCertificate]
                        else
                            starDotDevDotSubjectDotAppTlsCertificate :: allSubjectDotAppTlsCertificates.Value
                        |> prepareSniCertificateSelector

                    let httpsOptions = HttpsConnectionAdapterOptions()
                    httpsOptions.ServerCertificateSelector <-
                        fun _ctx sniName ->
                            if String.IsNullOrWhiteSpace sniName then
                                defaultCertificate
                            else
                                sniSelector DateTimeOffset.Now sniName
                                |> Option.defaultValue defaultCertificate

                    httpsOptions
            )
            .AddHostedService(fun serviceProvider -> InitializeClusterClientService(serviceProvider, ecosystem))
            .AddHttpContextAccessor()
            .AddDataProtection()
            .SetApplicationName(null)
            .AddKeyManagementOptions(
                fun options ->
                    options.XmlRepository <-
                        SqlServerDataProtectionXmlRepository(
                            sqlServerConfig,
                            ecosystem.Name
                        )
            )
            |> fun dataProtectionBuilder ->
                if certificateConfig.UseDevelopmentCertificate then
                    // don't protect key in dev environment to make Dev Fabric similar to Dev Host
                    dataProtectionBuilder
                else
                    let allMatchingSecretsCertificates = getSecretsCertificatesFromStore ecosystem.Name
                    let preferredSecretsCertificate    = getPreferredSecretsCertificate System.DateTimeOffset.Now allMatchingSecretsCertificates

                    dataProtectionBuilder
                        .ProtectKeysWithCertificate(preferredSecretsCertificate)
                        .UnprotectKeysWithAnyCertificate(List.toArray allMatchingSecretsCertificates)
                |> ignore

        hostConfiguration.ConfigureServices(configuration, services)

    member _.Configure(app: IApplicationBuilder, _env: IWebHostEnvironment) :unit =
        // Save original X-F-F Header and RemoteIP Information for logging
        // The Forwarded Middleware below processes it
        app
            .Use(
                fun context (next: Func<Task>) ->
                    let existingXff = context.Request.Headers["X-Forwarded-For"]
                    let remoteIp = context.Connection.RemoteIpAddress |> fun ip -> (if ip.IsIPv4MappedToIPv6 then ip.MapToIPv4() else ip).ToString()
                    context.Request.Headers.Add("X-Egg-Original-IP-Chain", StringValues(Seq.append existingXff [remoteIp] |> Array.ofSeq));
                    // context.Items["X-Egg-Original-IP-Chain"] <- existingXff
                    next.Invoke()
            )
            .UseForwardedHeaders()
            .Use(
                fun context (next: Func<Task>) ->
                    if context.Connection.RemoteIpAddress.IsIPv4MappedToIPv6 then
                        context.Connection.RemoteIpAddress <- context.Connection.RemoteIpAddress.MapToIPv4()
                    next.Invoke()
            )
            .UseDeveloperExceptionPage()
            .UseCors(configureCors)
            .UseCookiePolicy()
            .UseRouting()
            .UseRealTimeEndpoints(ecosystem)
            .UseHttpEndpoints((* suppressExceptionDetails *) true)
            |> hostConfiguration.Configure

        app.UseGiraffe(webApp)


and InitializeClusterClientService(serviceProvider: IServiceProvider, _ecosystem: Ecosystem) =
    let stopCancellationTokenSource = new CancellationTokenSource()

    //let statelessService = serviceProvider.GetRequiredService<StatelessService>()

    interface IHostedService with

        member _.StartAsync(cancellationToken: Threading.CancellationToken): Task =
            let anyCancelationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopCancellationTokenSource.Token)

            // Don't block the start-up of the API service on cluster client initializing
            // While it is desirable to wait until we get a cluster connection, this essentially means we're
            // treating start-up differently from ongoing loss of connection.
            // Also while this is blocked, we don't get a signal if the process is shutting down
            Task.Run<unit>(fun () ->
                let startTime = DateTimeOffset.Now

                let anyCancellationToken = anyCancelationTokenSource.Token

                let mutable attempt = 1

                let clusterClientHost = serviceProvider.GetRequiredService<ClusterClientHostHolder>().Host

                // Orleans 10: IClusterClient has no public Connect(). Retry is driven by host.StartAsync()
                // in a loop on the same host instance. Connection lifecycle is now driven by
                // IHostedService.
                let rec tryStart () =
                    task {
                        try
                            do! clusterClientHost.StartAsync(anyCancellationToken)
                        with _exnBeforeRetry ->
                            let _elapsed = DateTimeOffset.Now - startTime
                            // todo: report-health to dapr dashboard
                            //reportHealth ecosystem statelessService
                            //    (ClusterConnectionStatus.NotYetConnected (exnBeforeRetry, elapsed, anyCancellationToken.IsCancellationRequested, attempt))

                            if anyCancellationToken.IsCancellationRequested then
                                ()
                            else
                                attempt <- attempt + 1
                                do! Task.Delay(TimeSpan.FromSeconds 2.0, anyCancellationToken)
                                do! tryStart ()
                    }

                tryStart ()

            , anyCancelationTokenSource.Token)
            |> ignore

            Task.CompletedTask

        member _.StopAsync(forceShutdownRequested: Threading.CancellationToken): Task =
            stopCancellationTokenSource.Cancel()

            let biosphereGrainProvider =
                serviceProvider.GetRequiredService<IBiosphereGrainProvider>()

            if not forceShutdownRequested.IsCancellationRequested then
                biosphereGrainProvider.Close()
            else
                Task.CompletedTask
