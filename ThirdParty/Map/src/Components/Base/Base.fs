[<AutoOpen>]
module ThirdParty.Map.Components.Base

open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Components
open ReactXP.Styles
open ThirdParty.Map.Types
open ThirdParty.Map.Components.Constructors
open ThirdParty.Map.Components.Web.Map
open ThirdParty.Map.Components.Native.Map

type Value  = ThirdParty.Map.Types.Value
module Value =
    let Address = ThirdParty.Map.Types.Value.Address
    let LatLng  = ThirdParty.Map.Types.Value.LatLng

type LatLng = ThirdParty.Map.Types.LatLng
let dhakaLatLng = ThirdParty.Map.Types.dhakaLatLng

type MapPosition = ThirdParty.Map.Types.MapPosition

type Marker = ThirdParty.Map.Types.Marker

type MarkerPosition = ThirdParty.Map.Types.MarkerPosition

type MarkerImage = ThirdParty.Map.Types.MarkerImage
let Icon = MarkerImage.Icon
let Symbol = MarkerImage.Symbol

#if EGGSHELL_PLATFORM_IS_WEB
type InfoWindow = ThirdParty.Map.Types.InfoWindow
#endif

type Shape = ThirdParty.Map.Types.Shape
let Polyline = Shape.Polyline
let Polygon = Shape.Polygon
let Circle = Shape.Circle

type Directions = ThirdParty.Map.Types.Directions
type DirectionsRendererOptions = ThirdParty.Map.Types.DirectionsRendererOptions
type MapStyle = ThirdParty.Map.Types.MapStyle

type Place = ThirdParty.Map.Types.Place
let PlaceByQuery = Place.Query
let PlaceById = Place.Id
let PlaceByLatLng = Place.LatLng

type TravelMode = ThirdParty.Map.Types.TravelMode
let Driving = TravelMode.Driving
let Walking = TravelMode.Walking
let Bicycling = TravelMode.Bicycling
let Transit = TravelMode.Transit

type IRefReactNativeMapView = ThirdParty.Map.Types.IRefReactNativeMapView

type IWebMapViewRef = ThirdParty.Map.Types.IWebMapViewRef

type LocateToConfig = ThirdParty.Map.Types.LocateToConfig

type MapTypeId = ThirdParty.Map.Types.MapTypeId

type ThirdParty.Map.Components.Constructors.Map with
    [<Component>]
    static member Base(
            apiKey:            string,
            ?position:          MapPosition,
            ?onPositionChanged: MapPosition -> unit,
            ?zoom:              int,
            ?markers:           List<Marker>,
            ?shapes:            List<Shape>,
            ?fullScreen:        bool,
            ?backgroundColor:   string,
            ?clickableIcons:    bool,
            ?disableDefaultUI:  bool,
            ?minZoom:           float,
            ?maxZoom:           float,
            ?mapStyles:         List<MapStyle>,
            ?mapTypeId:         MapTypeId,
            ?directions:        List<Directions>,
            ?onLocatePress:     ReactEvent.Action -> LocateToConfig,
            ?styles:            array<ViewStyles>,
            ?xLegacyStyles:     List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:               string
        ) : ReactElement =
        ignore key
        xLegacyStyles |> ignore

        let position = defaultArg position MapPosition.Auto
        #if EGGSHELL_PLATFORM_IS_WEB
        let maybeWebRef = Hooks.useRef (None: Option<IWebMapViewRef>)
        #else
        let maybeNativeRef = Hooks.useRef (None: Option<IRefReactNativeMapView>)
        #endif

        let animateToCenter (config: LocateToConfig) =
            #if EGGSHELL_PLATFORM_IS_WEB
            maybeWebRef.current
            |> Option.sideEffect (fun webRef ->
                webRef.panTo config.location
                webRef.setZoom config.zoom
            )
            #else
            maybeNativeRef.current
            |> Option.sideEffect (fun nativeRef ->
                nativeRef.animateCamera ({
                    zoom = config.zoom
                    center = (config.location |> LatLng.asNativeMapViewCoordinates)
                })
            )
            #endif

        let onLocatePressHandler (e: ReactEvent.Action) =
            onLocatePress
            |> Option.sideEffect (fun handler ->
                handler e |> animateToCenter
            )

        RX.View(
            ?styles = styles,
            children =
                [|
                    #if EGGSHELL_PLATFORM_IS_WEB
                    Map.Web.Map(
                        apiKey = apiKey,
                        position = position,
                        ?onPositionChanged = onPositionChanged,
                        ?zoom = zoom,
                        ?markers = markers,
                        ?shapes = shapes,
                        ?directions = directions,
                        ?backgroundColor = backgroundColor,
                        ?clickableIcons = clickableIcons,
                        ?disableDefaultUI = disableDefaultUI,
                        ?minZoom = minZoom,
                        ?maxZoom = maxZoom,
                        ?fullScreen = fullScreen,
                        ?mapStyles = mapStyles,
                        ?mapTypeId = mapTypeId,
                        ref = (fun webRef -> maybeWebRef.current <- Some webRef)
                    )
                    #else
                    let isFullScreen = fullScreen |> Option.defaultValue false
                    Map.Native.Map(
                        apiKey = apiKey,
                        ?zoom = zoom,
                        value =
                            (match position with
                             | MapPosition.LatLng latLng -> latLng
                             | MapPosition.Auto -> failwith "MapPosition.Auto not supported on native"),
                        ?markers = markers,
                        fullScreen = isFullScreen,
                        ?shapes = shapes,
                        ref = (fun nativeRef -> maybeNativeRef.current <- Some nativeRef),
                        onChange =
                            (fun maybeLatLng ->
                                match onPositionChanged, maybeLatLng with
                                | Some handler, Some latLng -> latLng |> MapPosition.LatLng |> handler
                                | _ -> ())
                    )
                    #endif
                    match onLocatePress with
                    | Some _ ->
                        Map.Locate.LocateButtonWrapper(
                            children =
                                [|
                                    Map.Locate.LocateButton(onPress = onLocatePressHandler)
                                |]
                        )
                    | None -> noElement
                |]
        )
