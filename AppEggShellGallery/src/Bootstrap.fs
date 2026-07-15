module AppEggShellGallery.Bootstrap

open Fable.Core.JsInterop

open LibClient
open AppEggShellGallery.Components

open Rn.Components
open Rn.Helpers
open LibClient.Components

LibClient.Initialize.initialize (AppEggShellGallery.LocalImages.localImage)

let init(configRes: Result<AppEggShellGallery.Config, string>) =
    LibClient.ComponentRegistration.registerAllTheThings()
    LibUiAdmin.ComponentRegistration.registerAllTheThings()
    LibRouter.ComponentRegistration.registerAllTheThings()
    ThirdParty.Showdown.ComponentRegistration.registerAllTheThings()
    ThirdParty.Map.ComponentRegistration.registerAllTheThings()
    AppEggShellGallery.ComponentRegistration.registerAllTheThings()

    let element =
        match configRes with
        | Ok config ->
            Rn.Styles.Config.setIsDevMode config.InitializeRnInDevMode

            AppServices.initialize config
            AppEggShellGallery.Config.initialize config

            AppEggShellGallery.ComponentsTheme.applyTheme()

            let maybeTelemetrySink =
                config.MaybeAppInsightsConfig
                |> Option.map (fun appInsightsConfig -> new LibClient.ApplicationInsights.ApplicationInsightsSink(appInsightsConfig))

            let logSinks: seq<ILogSink> =
                seq {
                    ConsoleLogSink()

                    match maybeTelemetrySink with
                    | Some telemetrySink -> telemetrySink
                    | None               -> ()
                }

            setLogLevel LogLevel.Info
            registerLogSinks logSinks

            // let initialPstoreData = serizlied data
            let initialPstoreData = Map.empty
            let pstore = InitializePersistentStore initialPstoreData
            pstore |> ignore

            let element = Ui.App.Root()

            Rn.App.initialize ((* DEBUG *) config.InitializeRnInDebugMode, (* DEV *) config.InitializeRnInDevMode)
            // NB: no app-wide Rn.GestureHandlerRootView here. The gallery drives its
            // drawer/scrim/draggable through the JS-responder path (Rn.GestureView); an
            // app-wide RNGH root took over native touch arbitration and made the sidebar
            // close on a vertical scroll. RNGH is scoped to the one page that needs it
            // (the HorizontalPanArea demo wraps itself in Rn.GestureHandlerRootView).
            Rn.UserInterface.setContextWrapper (fun rootView ->
                Ui.AppContext (children = [|rootView|])
            )

            #if DEBUG
            LibClient.UiActionLog.installGlobalHook Browser.Dom.window "AppEggShellGallery"
            #endif

            #if EGGSHELL_PLATFORM_IS_WEB
            let consoleTestBindings =
                createObj [
                    "app"                     ==> element
                    "pstore"                  ==> pstore
                    "setTapCaptureVisibility" ==> LibClient.Components.TapCaptureDebugVisibility.setVisibleForDebug
                ]
            Browser.Dom.window?eggshell?AppEggShellGallery?test <- consoleTestBindings
            #endif

            element

        | Error reason ->
            Log.Error ("App initialization failed because config construction failed: " + reason)
            Rn.App.initialize((* DEBUG *) false, (* DEV *) false);

            LibClient.Components.Constructors.LC.AppShell.TopLevelErrorMessage(
                error = exn reason,
                retry = NoopFn // TODO this should be optional
            )

    async {
        do! LibClient.ServiceInstances.services().Image.WhenInitialized ()
        Rn.UserInterface.setMainView element
    } |> startSafely

open Fable.Core
[<Global>]
let private eggshell: obj = jsNative
eggshell?AppEggShellGallery?configSourceOverrides
|> ConfigSource.Base.withOverrides
|> Config.tryOfSource
|> init
