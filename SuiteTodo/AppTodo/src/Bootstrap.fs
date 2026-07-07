module AppTodo.Bootstrap

open Fable.Core.JsInterop
open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Components
open ReactXP.Helpers
open AppTodo.Components
open AppTodo.I18nGlobal
open AppTodo.AppServices

LibClient.Initialize.initialize AppTodo.LocalImages.localImage

let init (configRes: Result<AppTodo.Config, string>) =
    LibClient.ComponentRegistration.registerAllTheThings()
    LibRouter.ComponentRegistration.registerAllTheThings()
    LibUiSubject.ComponentRegistration.registerAllTheThings()

    let element =
        match configRes with
        | Ok config ->
            ReactXP.Styles.Config.setIsDevMode config.InitializeReactXPInDevMode

            AppServices.initialize config
            AppTodo.Config.initialize config
            AppTodo.ComponentsTheme.applyTheme()

            setLogLevel LogLevel.Info
            registerLogSinks [ ConsoleLogSink() ]

            let initialPstoreData = Map.empty
            let pstore = InitializePersistentStore initialPstoreData
            pstore |> ignore

            let mutable rootElement = None
            i18n.StartWithDefault ((fun () -> rootElement.Value), LibClient.I18n.Language.En)
            AppTodo.I18nGlobal.start ()

            let element = Ui.App.Root()
            rootElement <- Some element

            ReactXP.App.initialize (config.InitializeReactXPInDebugMode, config.InitializeReactXPInDevMode)

            ReactXP.UserInterface.setContextWrapper (fun rootView ->
#if EGGSHELL_PLATFORM_IS_WEB
                Ui.AppContext (children = [| rootView |])
#else
                // gesture-handler needs a root view ancestor or all Pan gestures no-op.
                RX.GestureHandlerRootView(
                    children = [| Ui.AppContext (children = [| rootView |]) |]
                )
#endif
            )

            #if DEBUG
            LibClient.UiActionLog.installGlobalHook Browser.Dom.window "AppTodo"
            #endif

            #if EGGSHELL_PLATFORM_IS_WEB
            let consoleTestBindings =
                createObj [
                    "app"    ==> element
                    "pstore" ==> pstore
                ]

            Browser.Dom.window?eggshell?AppTodo?test <- consoleTestBindings
            #endif

            element

        | Error reason ->
            Log.Error ("App initialization failed because config construction failed: " + reason)
            ReactXP.App.initialize (false, false)

            LC.AppShell.TopLevelErrorMessage(
                error = exn reason,
                retry = NoopFn
            )

    async {
        do! LibClient.ServiceInstances.services().Image.WhenInitialized ()
        ReactXP.UserInterface.setMainView element

        #if EGGSHELL_PLATFORM_IS_WEB
        Browser.Dom.document.getElementById("app-pre-bootstrap-loader").remove()
        #endif
    } |> startSafely

open Fable.Core
[<Global>]
let private eggshell: obj = jsNative

eggshell?AppTodo?configSourceOverrides
|> AppTodo.ConfigSource.Base.withOverrides
|> AppTodo.Config.tryOfSource
|> init
