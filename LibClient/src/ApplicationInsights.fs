module LibClient.ApplicationInsights

open Fable.Core
open Fable.Core.JsInterop
open LibClient.MessageTemplates

let private AppInsightsRaw: obj = import "ApplicationInsights" "@microsoft/applicationinsights-web"

[<Fable.Core.JS.Pojo>]
type private AppInsightsInnerConfigJs
    ( instrumentationKey: string, maxBatchInterval: int, extensions: obj array ) =
    member val instrumentationKey = instrumentationKey
    member val maxBatchInterval = maxBatchInterval
    member val extensions = extensions

[<Fable.Core.JS.Pojo>]
type private AppInsightsConstructorOptionsJs ( config: obj ) =
    member val config = config

[<Fable.Core.JS.Pojo>]
type private TrackTraceJs ( message: string, properties: obj ) =
    member val message = message
    member val properties = properties

[<Fable.Core.JS.Pojo>]
type private TrackEventJs ( name: string, properties: obj ) =
    member val name = name
    member val properties = properties

[<Fable.Core.JS.Pojo>]
type private TrackPageViewJs ( name: string, properties: obj ) =
    member val name = name
    member val properties = properties

let private getAppInsightExtensions () =
    #if !EGGSHELL_PLATFORM_IS_WEB
    let ReactNativePlugin: obj = import "ReactNativePlugin" "@microsoft/applicationinsights-react-native"
    [| createNew ReactNativePlugin () |]
    #else
    [||]
    #endif

type AppInsightsConfig = {
    InstrumentationKey: string
    CloudRole:          string
}

type ApplicationInsightsSink (config: AppInsightsConfig) =
    let mutable globalProperties: TelemetryProperties = Map.empty

    let propertiesToPojo (properties: TelemetryProperties) : obj =
        properties
        |> Map.toSeq
        |> Seq.map (fun (key, value) -> (key, (stringifyValueForTelemetry value) :> obj))
        |> createObj

    let mergeProperties (first: TelemetryProperties) (second: TelemetryProperties) : TelemetryProperties =
        Map.merge first second

    let mergeEventProperties (event: Event) (properties: TelemetryProperties) : TelemetryProperties =
        mergeProperties properties (event.GetProperties() |> Seq.map (fun property -> property.NamedHole.Name, property.Value) |> Map.ofSeq)

    let mergeGlobalProperties (properties: TelemetryProperties) : TelemetryProperties =
        mergeProperties globalProperties properties

    let mergeUserProperties (user: TelemetryUser) (properties: TelemetryProperties): TelemetryProperties =
        match user with
        | TelemetryUser.Anonymous ->
            properties
        | TelemetryUser.Identified (_, userProperties) ->
            Map.merge properties userProperties

    let appInsights =
        createNew AppInsightsRaw (
            AppInsightsConstructorOptionsJs(
                AppInsightsInnerConfigJs(
                    config.InstrumentationKey,
                    5000,
                    getAppInsightExtensions ()
                ) |> box
            ) |> box
        )

    let setUser (user: TelemetryUser) : unit =
        match user with
        | TelemetryUser.Anonymous              -> appInsights?clearAuthenticatedUserContext()
        | TelemetryUser.Identified (userId, _) -> appInsights?setAuthenticatedUserContext userId

    let trackTrace (level: LogLevel) (maybeCategory: Option<string>) (properties: LogProperties) (event: Event) : Unit =
        let propertiesPojo =
            [
                (
                    "severity",
                    match level with
                    | LogLevel.Debug -> box "debug"
                    | LogLevel.Info  -> box "info"
                    | LogLevel.Warn  -> box "warning"
                    | LogLevel.Error -> box "error"
                )

                match maybeCategory with
                | Some category ->
                    ("category", box category)
                | None ->
                    ()
            ]
            |> Map.ofSeq
            |> mergeProperties properties
            |> mergeEventProperties event
            |> mergeGlobalProperties
            |> propertiesToPojo

        appInsights?trackTrace (
            TrackTraceJs(string event, propertiesPojo) |> box
        )

    do
        appInsights?loadAppInsights()
        appInsights?addTelemetryInitializer (fun envelope ->
            envelope?tags?("ai.cloud.role") <- config.CloudRole
        )

    interface ITelemetrySink with
        override _.TrackEvent (name: string) (user: TelemetryUser) (properties: TelemetryProperties) : unit =
            setUser user

            let propertiesPojo =
                properties
                |> mergeUserProperties user
                |> mergeGlobalProperties
                |> propertiesToPojo
            appInsights?trackEvent (
                TrackEventJs(name, propertiesPojo) |> box
            )

        override _.TrackScreenView (url: string) (user: TelemetryUser) (properties: TelemetryProperties) : unit =
            setUser user

            let propertiesPojo =
                properties
                |> mergeUserProperties user
                |> mergeGlobalProperties
                |> propertiesToPojo
            // AppInsights backend expects page-view name <= 512 chars; trim encoded URLs.
            appInsights?trackPageView (
                TrackPageViewJs(
                    url.Substring(0, min 512 url.Length),
                    propertiesPojo
                ) |> box
            )

    interface ILogSink with
        member _.Log(level: LogLevel, maybeCategory: Option<string>, properties: LogProperties, event: Event) : Unit =
            trackTrace level maybeCategory properties event
