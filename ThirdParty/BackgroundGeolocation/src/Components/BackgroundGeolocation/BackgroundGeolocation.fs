module ThirdParty.BackgroundGeolocation.Components.BackgroundGeolocation

open Fable.Core
open Fable.Core.JsInterop
open Microsoft.FSharp.Core.LanguagePrimitives
open System

let private BackgroundGeolocation: obj -> obj = importDefault "react-native-background-geolocation"

[<Fable.Core.JS.Pojo>]
type private BackgroundPermissionRationaleJs(title: string, message: string, positiveAction: string, negativeAction: string) =
    member val title = title
    member val message = message
    member val positiveAction = positiveAction
    member val negativeAction = negativeAction

[<Fable.Core.JS.Pojo>]
type private CurrentPositionRequestJs(timeout: int, persist: bool, maximumAge: int, desiredAccuracy: int, samples: int) =
    member val timeout = timeout
    member val persist = persist
    member val maximumAge = maximumAge
    member val desiredAccuracy = desiredAccuracy
    member val samples = samples

[<Fable.Core.JS.Pojo>]
type private GeofenceOptionJs(identifier: string, radius: int, latitude: float, longitude: float, notifyOnEntry: bool, notifyOnExit: bool, loiteringDelay: int, notifyOnDwell: bool) =
    member val identifier = identifier
    member val radius = radius
    member val latitude = latitude
    member val longitude = longitude
    member val notifyOnEntry = notifyOnEntry
    member val notifyOnExit = notifyOnExit
    member val loiteringDelay = loiteringDelay
    member val notifyOnDwell = notifyOnDwell

[<Measure>] type radian
[<Measure>] type degree
[<Measure>] type m
type LatLong = { Lat : float<degree>; Long : float<degree> }

[<StringEnum>]
type LocationAuthorizationRequest =
| [<CompiledName("Always")>] Always
| [<CompiledName("WhenInUse")>] WhenInUse
| [<CompiledName("Any")>] Any

type IBackgroundGeolocationOption =
    abstract desiredAccuracy:               int
    abstract triggerActivities:             string
    abstract locationUpdateInterval:        int
    abstract fastestLocationUpdateInterval: int
    abstract debug:                         bool
    abstract stopOnTerminate:               bool
    abstract startOnBoot:                   bool
    abstract maxRecordsToPersist:           int
    abstract heartbeatInterval:             int
    abstract enabled:                       bool
    abstract schedulerEnabled:              bool
    abstract odometer:                      int
    abstract didLaunchInBackground:         bool
    abstract didDeviceReboot:               bool
    abstract enableHeadless:                bool
    abstract backgroundPermissionRationale: obj
    abstract locationAuthorizationRequest:  LocationAuthorizationRequest
    abstract allowIdenticalLocations:       bool
    abstract distanceFilter:                int
    abstract stopTimeout:                   int

type ISubscription =
    abstract remove: unit -> unit

type Coordination = {
    Latitude:  decimal
    Longitude: decimal
    Accuracy:  decimal
    Speed:     decimal
    Heading:   decimal
    Altitude:  decimal
}

type DeviceActivity = {
    Type:       TriggerActivity
    Confidence: int
}

and  TriggerActivity =
| OnFoot
| Walking
| Running
| OnBicycle
| InVehicle
| Still
    member this.toLibraryFormat () =
        match this with
        | OnFoot    -> "on_foot"
        | Walking   -> "walking"
        | Running   -> "running"
        | OnBicycle -> "on_bicycle"
        | InVehicle -> "in_vehicle"
        | Still     -> "still"
    static member fromLibraryFormat (activity: string) : TriggerActivity =
        match activity with
        | "on_foot"    -> OnFoot
        | "walking"    -> Walking
        | "running"    -> Running
        | "on_bicycle" -> OnBicycle
        | "in_vehicle" -> InVehicle
        | "still"      -> Still
        | _            -> Still

type Battery = {
    Level:      float
    IsCharging: bool
}

[<StringEnum>]
type LocationEvent =
| [<CompiledName("motionchange")>] Motionchange
| [<CompiledName("geofence")>] Geofence
| [<CompiledName("heartbeat")>] Heartbeat
| [<CompiledName("providerchange")>] Providerchange

type Location =
    {
        Timestamp: JS.Date
        Event:     LocationEvent
        IsMoving:  bool   // <-- The motion-state when location was recorded.
        Uuid:      string // <-- Universally unique identifier
        Activity:  DeviceActivity
        Coords:    Coordination
        Battery:   Battery
        Sample:    bool
    }
    static member fromJsObj(jsLocation: obj) : Location =
        let coordination: Coordination = {
            Latitude  = jsLocation?coords?latitude
            Longitude = jsLocation?coords?longitude
            Accuracy  = jsLocation?coords?accuracy
            Speed     = jsLocation?coords?speed
            Heading   = jsLocation?coords?heading
            Altitude  = jsLocation?coords?altitude
        }

        let activity: DeviceActivity = {
            Type       = jsLocation?activity?``type`` |> TriggerActivity.fromLibraryFormat
            Confidence = jsLocation?activity?confidence
        }

        let battery: Battery = {
            Level      = jsLocation?battery?level
            IsCharging = jsLocation?battery?is_charging
        }

        {
            Timestamp = jsLocation?timestamp
            Event     = jsLocation?event
            IsMoving  = jsLocation?is_moving
            Uuid      = jsLocation?uuid
            Activity  = activity
            Coords    = coordination
            Battery   = battery
            Sample    = jsLocation?sample
        }

type HeartbeatEvent =
    abstract location: Location

type LocationError =
| Unknown
| PermissionDenied
| NetworkError
| LocationTimeout
| RequestCancelled
    static member fromLibraryResponse (errorCode: int) : LocationError =
        match errorCode with
        | 0   -> Unknown
        | 1   -> PermissionDenied
        | 2   -> NetworkError
        | 408 -> LocationTimeout
        | 499 -> RequestCancelled
        | _   -> Unknown

type DesiredAccuracy =
| Navigation = -2
| High       = -1
| Medium     = 10
| Low        = 100
| VeryLow    = 3000
| Lowest     = 1000

[<Fable.Core.JS.Pojo>]
type private GeolocationOptionJs(
    backgroundPermissionRationale: obj,
    debug:                         bool,
    desiredAccuracy:               DesiredAccuracy,
    triggerActivities:             string,
    startOnBoot:                   bool,
    stopOnTerminate:               bool,
    maxRecordsToPersist:           int,
    heartbeatInterval:             int,
    locationUpdateInterval:        int,
    fastestLocationUpdateInterval: int,
    locationAuthorizationRequest:  LocationAuthorizationRequest,
    enableHeadless:                bool,
    allowIdenticalLocations:       bool,
    distanceFilter:                int,
    stopTimeout:                   int
) =
    member val backgroundPermissionRationale = backgroundPermissionRationale
    member val debug = debug
    member val desiredAccuracy = desiredAccuracy
    member val triggerActivities = triggerActivities
    member val startOnBoot = startOnBoot
    member val stopOnTerminate = stopOnTerminate
    member val maxRecordsToPersist = maxRecordsToPersist
    member val heartbeatInterval = heartbeatInterval
    member val locationUpdateInterval = locationUpdateInterval
    member val fastestLocationUpdateInterval = fastestLocationUpdateInterval
    member val locationAuthorizationRequest = locationAuthorizationRequest
    member val enableHeadless = enableHeadless
    member val allowIdenticalLocations = allowIdenticalLocations
    member val distanceFilter = distanceFilter
    member val stopTimeout = stopTimeout

type BackgroundPermissionRationale =
    {
        Title:          string
        Message:        string
        PositiveAction: string
        NegativeAction: string
    }
    member this.toLibraryFormat () : obj =
        (BackgroundPermissionRationaleJs(this.Title, this.Message, this.PositiveAction, this.NegativeAction))
        |> box
    static member fromLibraryFormat (backgroundPermissionRationale: obj) : BackgroundPermissionRationale =
        {
            Title          = backgroundPermissionRationale?title
            Message        = backgroundPermissionRationale?message
            PositiveAction = backgroundPermissionRationale?positiveAction
            NegativeAction = backgroundPermissionRationale?negativeAction
        }

type GeolocationOption =
    {
        DesiredAccuracy:               DesiredAccuracy
        TriggerActivities:             List<TriggerActivity>
        LocationUpdateInterval:        int
        FastestLocationUpdateInterval: int
        Debug:                         bool
        StopOnTerminate:               bool
        StartOnBoot:                   bool
        MaxRecordsToPersist:           int
        HeartbeatInterval:             int
        BackgroundPermissionRationale: BackgroundPermissionRationale
        LocationAuthorizationRequest:  LocationAuthorizationRequest
        EnableHeadless:                bool
        AllowIdenticalLocations:       bool
        DistanceFilter:                int
        StopTimeout:                   int

    }
    member this.toLibraryFormat () : obj =
        (GeolocationOptionJs(
            this.BackgroundPermissionRationale.toLibraryFormat(),
            this.Debug,
            this.DesiredAccuracy,
            (this.TriggerActivities |> List.fold (fun activityAccumulator activity -> activityAccumulator + activity.toLibraryFormat() + ", ") ""),
            this.StartOnBoot,
            this.StopOnTerminate,
            this.MaxRecordsToPersist,
            this.HeartbeatInterval,
            this.LocationUpdateInterval,
            this.FastestLocationUpdateInterval,
            this.LocationAuthorizationRequest,
            this.EnableHeadless,
            this.AllowIdenticalLocations,
            this.DistanceFilter,
            this.StopTimeout
        )) |> box
    static member fromLibraryResponse (option: IBackgroundGeolocationOption): GeolocationOption =
        {
            Debug                         = option.debug
            DesiredAccuracy               = EnumOfValue<int, DesiredAccuracy>(option.desiredAccuracy)
            LocationUpdateInterval        = option.locationUpdateInterval
            FastestLocationUpdateInterval = option.fastestLocationUpdateInterval
            StopOnTerminate               = option.stopOnTerminate
            StartOnBoot                   = option.startOnBoot
            MaxRecordsToPersist           = option.maxRecordsToPersist
            HeartbeatInterval             = option.heartbeatInterval
            TriggerActivities             = option.triggerActivities.Split([|", "|], StringSplitOptions.None)
                                            |> Array.map TriggerActivity.fromLibraryFormat
                                            |> Array.toList
            BackgroundPermissionRationale = option.backgroundPermissionRationale |> BackgroundPermissionRationale.fromLibraryFormat
            LocationAuthorizationRequest  = option.locationAuthorizationRequest
            EnableHeadless                = option.enableHeadless
            AllowIdenticalLocations       = option.allowIdenticalLocations
            DistanceFilter                = option.distanceFilter
            StopTimeout                   = option.stopTimeout
        }

type GeolocationState =
    {
        Enabled:               bool
        SchedulerEnabled:      bool
        Odometer:              int
        DidLaunchInBackground: bool
        DidDeviceReboot:       bool
        Option:                GeolocationOption
    }
    static member fromLibraryResponse (optionWithState: IBackgroundGeolocationOption) : GeolocationState =
        {
            Enabled               = optionWithState.enabled
            SchedulerEnabled      = optionWithState.schedulerEnabled
            Odometer              = optionWithState.odometer
            DidLaunchInBackground = optionWithState.didLaunchInBackground
            DidDeviceReboot       = optionWithState.didDeviceReboot
            Option                = optionWithState |> GeolocationOption.fromLibraryResponse
        }

let onLocation (callback: Location -> unit) (errorHandler: LocationError -> unit): ISubscription =
    let customCallbackHandler (jsLocation: obj) =
        jsLocation
        |> Location.fromJsObj
        |> callback

    let customErrorHandler (error: int) =
        error
        |> LocationError.fromLibraryResponse
        |> errorHandler

    BackgroundGeolocation?onLocation customCallbackHandler customErrorHandler

type CurrentPositionRequest =
    abstract timeout:         int
    abstract desiredAccuracy: int
    abstract samples:         int
    abstract maximumAge:      int
    abstract persist:         bool

let currentPositionRequest =
    (CurrentPositionRequestJs(
        30,    // 30 second timeout to fetch location
        true,  // Defaults to state.enabled
        0,     // Always request updated location
        1,     // Try to fetch a location with an accuracy of  `10` meters.
        3
    ))
    |> box

let getCurrentPositionInCallback (option: obj) (callback: Location -> unit) (errorHandler: LocationError -> unit) =
    let customCallbackHandler (jsLocation: obj) =
        jsLocation
        |> Location.fromJsObj
        |> callback

    let customErrorHandler (error: int) =
        error
        |> LocationError.fromLibraryResponse
        |> errorHandler

    BackgroundGeolocation?getCurrentPosition option customCallbackHandler customErrorHandler

let getCurrentPosition (option: obj): Async<Option<Location>> =
    BackgroundGeolocation?getCurrentPosition option
    |> Promise.map (fun location ->
        location |> Location.fromJsObj |> Some
    )
    |> Promise.catch(fun _ ->
        None
    )
    |> Async.AwaitPromise

let onHeartbeat (callback: Option<Location> -> unit): ISubscription =
    let customCallbackHandler (heartbeatEvent: obj) =
        let maybeLocation: LibClient.JsInterop.JsNullable<obj> = heartbeatEvent?location

        maybeLocation.ToOption
        |> Option.map (fun location -> Location.fromJsObj location)
        |> callback

    BackgroundGeolocation?onHeartbeat customCallbackHandler

type AuthorizationStatus =
| AUTHORIZATION_STATUS_NOT_DETERMINED = 0
| AUTHORIZATION_STATUS_RESTRICTED     = 1
| AUTHORIZATION_STATUS_DENIED         = 2
| AUTHORIZATION_STATUS_ALWAYS         = 3
| AUTHORIZATION_STATUS_WHEN_IN_USE    = 4

type ProviderChangeEvent =
    abstract status: AuthorizationStatus

let onProviderChange (callback: ProviderChangeEvent -> unit) =
    BackgroundGeolocation?onProviderChange callback

let addGeofence (locationPoint: LatLong) (geofenceRadius: PositiveDecimal) (identifier: string) =
    let lat:  float = locationPoint.Lat  |> float
    let long: float = locationPoint.Long |> float
    let rad:  int   = geofenceRadius.Value |> int
    let geofenceOption =
        (GeofenceOptionJs(identifier, rad, lat, long, true, true, 100, true))
        |> box
    BackgroundGeolocation?addGeofence geofenceOption ()

let getGeofences () =
    promise {
        let! geofencItems = BackgroundGeolocation?getGeofences ()
        return geofencItems
    } |> Async.AwaitPromise

let onGeofence (callback ) =
    BackgroundGeolocation?onGeofence (callback)

let ready (option: GeolocationOption) =
    BackgroundGeolocation?ready (option.toLibraryFormat ())
    |> Promise.map (fun jsResponse ->
        jsResponse
        |> GeolocationState.fromLibraryResponse
        |> Ok
    )
    |> Promise.catch (fun errorMessage -> (Error errorMessage))
    |> Async.AwaitPromise

let start () =
    BackgroundGeolocation?start ()
    |> Promise.map (fun jsResponse ->
        jsResponse
        |> GeolocationState.fromLibraryResponse
        |> Ok
    )
    |> Promise.catch (fun errorMessage -> (Error errorMessage))
    |> Async.AwaitPromise

let stop () =
    BackgroundGeolocation?stop ()
    |> Promise.map (fun jsResponse ->
        jsResponse
        |> GeolocationState.fromLibraryResponse
        |> Ok
    )
    |> Promise.catch (fun errorMessage -> (Error errorMessage))
    |> Async.AwaitPromise

let getState () =
    BackgroundGeolocation?getState ()
    |> Promise.map (fun jsResponse ->
        jsResponse
        |> GeolocationState.fromLibraryResponse
        |> Ok
    )
    |> Promise.catch (fun errorMessage -> (Error errorMessage))
    |> Async.AwaitPromise

let changePace (shouldChange: bool) =
    BackgroundGeolocation?changePace shouldChange

let permission (): Async<AuthorizationStatus> = BackgroundGeolocation?requestPermission () |> Async.AwaitPromise

let BGL = BackgroundGeolocation
