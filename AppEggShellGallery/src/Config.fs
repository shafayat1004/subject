namespace AppEggShellGallery

type ConfigSource = {
    AppUrlBase:                                    Option<string>
    BackendUrl:                                    Option<string>
    MaybeAppInsightsInstrumentationKey:            Option<string>
    MaybeAppInsightsCloudRole:                     Option<string>
    InitializeRnInDevMode:                    Option<string>
    InitializeRnInDebugMode:                  Option<string>
    GoogleMapsApiKey:                              Option<string>
    MaybeInBundleImageServiceBaseUrl:              Option<string>
    MaybeInBundleStaticResourceUrlPattern:         Option<string>
    MaybeExternalImageServiceBaseUrl:              Option<string>
    MaybeExternalStaticResourceUrlPattern:         Option<string>
    MaybeInBundleResourceUrlHashedDirectoryPrefix: Option<string>
} with
    static member Base : ConfigSource = {
        AppUrlBase                                    = None // TODO location.origin for web
        BackendUrl                                    = Some "http://localhost:8080"
        MaybeAppInsightsInstrumentationKey            = None
        MaybeAppInsightsCloudRole                     = None
        InitializeRnInDevMode                    = Some "false"
        InitializeRnInDebugMode                  = Some "false"
        GoogleMapsApiKey                              = Some "YOUR_KEY_HERE"
        MaybeInBundleImageServiceBaseUrl              = None
        MaybeInBundleStaticResourceUrlPattern         = None
        MaybeExternalImageServiceBaseUrl              = None
        MaybeExternalStaticResourceUrlPattern         = None
        MaybeInBundleResourceUrlHashedDirectoryPrefix = None
    }

    member this.withOverrides (overrides: obj) : ConfigSource =
        LibClient.JsInterop.extendRecordWithObj overrides this

type Config = {
    AppUrlBase:                                    string
    BackendUrl:                                    string
    MaybeAppInsightsConfig:                        Option<LibClient.ApplicationInsights.AppInsightsConfig>
    InitializeRnInDevMode:                    bool
    InitializeRnInDebugMode:                  bool
    GoogleMapsApiKey:                              string
    MaybeInBundleImageServiceBaseUrl:              Option<string>
    MaybeInBundleStaticResourceUrlPattern:         Option<string>
    MaybeExternalImageServiceBaseUrl:              Option<string>
    MaybeExternalStaticResourceUrlPattern:         Option<string>
    MaybeInBundleResourceUrlHashedDirectoryPrefix: Option<string>
} with
    static member tryOfSource (source: ConfigSource) : Result<Config, string> =
        resultful {
            let! theAppUrlBase                   = source.AppUrlBase |> Result.ofOption "Missing AppUrlBase"
            let! theBackendUrl                   = source.BackendUrl |> Result.ofOption "Missing BackendUrl"
            let! theGoogleMapsApiKey             = source.GoogleMapsApiKey |> Result.ofOption "Missing GoogleMapsApiKey"
            let! theInitializeRnInDevMode   = source.InitializeRnInDevMode   |> Option.flatMap System.Boolean.ParseOption |> Result.ofOption "Missing InitializeRnInDevMode"
            let! theInitializeRnInDebugMode = source.InitializeRnInDebugMode |> Option.flatMap System.Boolean.ParseOption |> Result.ofOption "Missing InitializeRnInDebugMode"

            let maybeAppInsightsConfig: Option<LibClient.ApplicationInsights.AppInsightsConfig> =
                match (source.MaybeAppInsightsInstrumentationKey, source.MaybeAppInsightsCloudRole) with
                | (Some key, Some role) -> Some { InstrumentationKey = key; CloudRole = role }
                | _ -> None

            return {
                AppUrlBase                                    = theAppUrlBase
                BackendUrl                                    = theBackendUrl
                MaybeAppInsightsConfig                        = maybeAppInsightsConfig
                InitializeRnInDevMode                    = theInitializeRnInDevMode
                InitializeRnInDebugMode                  = theInitializeRnInDebugMode
                GoogleMapsApiKey                              = theGoogleMapsApiKey
                MaybeInBundleImageServiceBaseUrl              = source.MaybeInBundleImageServiceBaseUrl
                MaybeInBundleStaticResourceUrlPattern         = source.MaybeInBundleStaticResourceUrlPattern
                MaybeExternalImageServiceBaseUrl              = source.MaybeExternalImageServiceBaseUrl
                MaybeExternalStaticResourceUrlPattern         = source.MaybeExternalStaticResourceUrlPattern
                MaybeInBundleResourceUrlHashedDirectoryPrefix = source.MaybeInBundleResourceUrlHashedDirectoryPrefix
            }
        }

module Config =
    let mutable private maybeConfig: Option<Config> = None

    let initialize (config: Config) : unit =
        maybeConfig <- Some config

    // NOTE this is done mainly to allow access to the config from within
    // components. Services should take config as a constructor parameter.
    // Don't use for other things without thinking twice.
    let current () : Config =
        match maybeConfig with
        | Some config -> config
        | None        -> failwith "Config has not been initialize, make sure you have a Config.initialize call in Bootstrap.fs"
