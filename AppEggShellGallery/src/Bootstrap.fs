module AppEggShellGallery.Bootstrap

open Fable.Core.JsInterop

open LibClient
open AppEggShellGallery.Components

open ReactXP.Components
open ReactXP.Helpers
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
            ReactXP.Styles.Config.setIsDevMode config.InitializeReactXPInDevMode

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
                    | None -> ()
                }

            setLogLevel LogLevel.Info
            registerLogSinks logSinks

            // let initialPstoreData = serizlied data
            let initialPstoreData = Map.empty
            let pstore = InitializePersistentStore initialPstoreData
            pstore |> ignore

            let element = Ui.App.Root()

            ReactXPRaw?App?initialize ((* DEBUG *) config.InitializeReactXPInDebugMode, (* DEV *) config.InitializeReactXPInDevMode)
            ReactXPRaw?UserInterface?setContextWrapper (fun rootView ->
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
            ReactXPRaw?App?initialize((* DEBUG *) false, (* DEV *) false);

            LibClient.Components.Constructors.LC.AppShell.TopLevelErrorMessage(
                error = exn reason,
                retry = NoopFn // TODO this should be optional
            )

    async {
        do! LibClient.ServiceInstances.services().Image.WhenInitialized ()
        ReactXPRaw?UserInterface?setMainView element
    } |> startSafely

open Fable.Core
[<Global>]
let private eggshell: obj = jsNative
eggshell?AppEggShellGallery?configSourceOverrides
|> ConfigSource.Base.withOverrides
|> Config.tryOfSource
|> init
