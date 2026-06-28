namespace AppTodo

type ConfigSource = {
    AppUrlBase:                                    Option<string>
    BackendUrl:                                    Option<string>
    InitializeReactXPInDevMode:                    Option<string>
    InitializeReactXPInDebugMode:                  Option<string>
    MaybeInBundleImageServiceBaseUrl:              Option<string>
    MaybeInBundleStaticResourceUrlPattern:         Option<string>
    MaybeExternalImageServiceBaseUrl:              Option<string>
    MaybeExternalStaticResourceUrlPattern:         Option<string>
    MaybeInBundleResourceUrlHashedDirectoryPrefix: Option<string>
} with
    static member Base : ConfigSource = {
        AppUrlBase                                    = None
        BackendUrl                                    = None
        InitializeReactXPInDevMode                    = Some "false"
        InitializeReactXPInDebugMode                  = Some "false"
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
    BackendUrl:                                    Option<string>
    InitializeReactXPInDevMode:                    bool
    InitializeReactXPInDebugMode:                  bool
    MaybeInBundleImageServiceBaseUrl:              Option<string>
    MaybeInBundleStaticResourceUrlPattern:         Option<string>
    MaybeExternalImageServiceBaseUrl:              Option<string>
    MaybeExternalStaticResourceUrlPattern:         Option<string>
    MaybeInBundleResourceUrlHashedDirectoryPrefix: Option<string>
} with
    static member tryOfSource (source: ConfigSource) : Result<Config, string> =
        resultful {
            let! theAppUrlBase                   = source.AppUrlBase |> Result.ofOption "Missing AppUrlBase"
            let! theInitializeReactXPInDevMode   = source.InitializeReactXPInDevMode   |> Option.flatMap System.Boolean.ParseOption |> Result.ofOption "Missing InitializeReactXPInDevMode"
            let! theInitializeReactXPInDebugMode = source.InitializeReactXPInDebugMode |> Option.flatMap System.Boolean.ParseOption |> Result.ofOption "Missing InitializeReactXPInDebugMode"

            return {
                AppUrlBase                                    = theAppUrlBase
                BackendUrl                                    = source.BackendUrl
                InitializeReactXPInDevMode                    = theInitializeReactXPInDevMode
                InitializeReactXPInDebugMode                  = theInitializeReactXPInDebugMode
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

    let current () : Config =
        match maybeConfig with
        | Some config -> config
        | None        -> failwith "Config has not been initialized, make sure you have a Config.initialize call in Bootstrap.fs"
