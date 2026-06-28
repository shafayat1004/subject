module AppTodo.AppServices

open System
open AppTodo
open LibClient.EventBus
open LibClient.Services.HttpService.HttpService
open LibUiSubject.Services.RealTimeService
open LibUiSubject.Services.SubjectService
open LibUiSubject.Services.ViewService
open SuiteTodo.Types

let mutable private maybeConfig: Option<Config> = None

let initialize (config: Config) : unit =
    maybeConfig <- Some config

    let eventBus = EventBus()

    let httpService =
        let staticResourceUrlTransformSettings =
            StaticResourceUrlTransformSettings.Pattern (config.MaybeInBundleStaticResourceUrlPattern, config.MaybeExternalStaticResourceUrlPattern)
        HttpService (eventBus, staticResourceUrlTransformSettings, (fun url -> url.StartsWith(config.BackendUrl)), config.MaybeInBundleResourceUrlHashedDirectoryPrefix)

    LibClient.ServiceInstances.provideInstances {
        EventBus         = eventBus
        Date             = LibClient.Services.DateService.DateService()
        Http             = httpService
        ThothEncodedHttp = LibClient.Services.HttpService.ThothEncodedHttpService.ThothEncodedHttpService httpService
        PageTitle        = LibClient.Services.PageTitleService.PageTitleService("Todo")
        Image            = LibClient.Services.ImageService.ImageService.WithoutOptimizations httpService
    }

let private reasonablyFreshTTLs = {
    Subject = TimeSpan.FromSeconds 30.
    Query   = TimeSpan.FromSeconds 30.
}

let private lazyServices = lazy (
    match maybeConfig with
    | None -> failwith "AppTodo.AppServices.initialize was never called"
    | Some config ->
        let eventBus                = LibClient.ServiceInstances.services().EventBus
        let thothEncodedHttpService = LibClient.ServiceInstances.services().ThothEncodedHttp
        let realTimeService         = RealTimeService (eventBus, config.BackendUrl)

        {|
            Http             = LibClient.ServiceInstances.services().Http
            ThothEncodedHttp = thothEncodedHttpService
            RealTime         = realTimeService
            Todo =
                SubjectService.Create
                    todoDef.LifeCycles.todo
                    reasonablyFreshTTLs
                    realTimeService
                    thothEncodedHttpService
                    eventBus
                    config.BackendUrl
            TodoListView =
                ViewService.Create<NoInput, TodoListViewOutput, NoViewError>
                    "TodoList"
                    (TimeSpan.FromSeconds 30.)
                    thothEncodedHttpService
                    config.BackendUrl
        |}
)

let services () = lazyServices.Force()
