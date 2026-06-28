module AppTodo.AppServices

open System
open AppTodo
open LibClient
open LibClient.EventBus
open LibClient.Services.HttpService.HttpService
open LibUiSubject.Services.RealTimeService
open LibUiSubject.Services.SubjectService
open LibUiSubject.Services.ViewService
open SuiteTodo.Types

#if DEBUG
open AppTodo.FakeData
#endif

let mutable private maybeConfig: Option<Config> = None

let initialize (config: Config) : unit =
    maybeConfig <- Some config

    let eventBus = EventBus()

    let httpService =
        let staticResourceUrlTransformSettings =
            StaticResourceUrlTransformSettings.Pattern (config.MaybeInBundleStaticResourceUrlPattern, config.MaybeExternalStaticResourceUrlPattern)

        HttpService (
            eventBus,
            staticResourceUrlTransformSettings,
            (fun url ->
                match config.BackendUrl with
                | None -> false
                | Some backendUrl -> url.StartsWith backendUrl),
            config.MaybeInBundleResourceUrlHashedDirectoryPrefix
        )

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

type TodoService =
    ISubjectService<Todo, Todo, TodoId, TodoIndex, TodoConstructor, TodoAction, TodoLifeEvent, TodoOpError>

type TodoListViewService =
    IViewService<NoInput, TodoListViewOutput, NoViewError>

let private lazyServices = lazy (
    match maybeConfig with
    | None -> failwith "AppTodo.AppServices.initialize was never called"
    | Some config ->
        let eventBus                = LibClient.ServiceInstances.services().EventBus
        let thothEncodedHttpService = LibClient.ServiceInstances.services().ThothEncodedHttp

        let todoService: TodoService =
            match config.BackendUrl with
            | Some backendUrl ->
                let realTimeService = RealTimeService (eventBus, backendUrl)
                SubjectService.Create
                    todoDef.LifeCycles.todo
                    reasonablyFreshTTLs
                    realTimeService
                    thothEncodedHttpService
                    eventBus
                    backendUrl
                :> TodoService
            | None ->
#if DEBUG
                FakeTodoService.service :> TodoService
#else
                failwith "No BackendUrl is set"
#endif

        let todoListViewService: TodoListViewService =
            match config.BackendUrl with
            | Some backendUrl ->
                ViewService.Create<NoInput, TodoListViewOutput, NoViewError>
                    "TodoList"
                    (TimeSpan.FromSeconds 30.)
                    thothEncodedHttpService
                    backendUrl
                :> TodoListViewService
            | None ->
#if DEBUG
                FakeViewService<NoInput, TodoListViewOutput, NoViewError> (
                    FakeDelay.NoDelay,
                    fun _ -> AsyncData.Available { Items = [] }
                )
                :> TodoListViewService
#else
                failwith "No BackendUrl is set"
#endif

        {|
            Http             = LibClient.ServiceInstances.services().Http
            ThothEncodedHttp = thothEncodedHttpService
            LocalStorage     = LibClient.Services.LocalStorageService.LocalStorageService "apptodo"
            Todo             = todoService
            TodoListView     = todoListViewService
        |}
)

let services () = lazyServices.Force()
