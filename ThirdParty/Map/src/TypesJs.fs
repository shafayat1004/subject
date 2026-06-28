[<AutoOpen>]
module ThirdParty.Map.TypesJs

open LibClient.JsInterop
open Fable.Core
open Fable.Core.JsInterop

[<Fable.Core.JS.Pojo>]
type private SizeJs(width: float, height: float) =
    member val width = width
    member val height = height

[<Fable.Core.JS.Pojo>]
type private PointJs(x: int, y: int) =
    member val x = x
    member val y = y

[<Fable.Core.JS.Pojo>]
type private LatLngJs(lat: float, lng: float) =
    member val lat = lat
    member val lng = lng

[<Fable.Core.JS.Pojo>]
type private IconJs(url: string, ?anchor: obj, ?labelOrigin: obj, ?origin: obj, ?scaledSize: obj, ?size: obj) =
    member val url = url
    member val anchor = anchor
    member val labelOrigin = labelOrigin
    member val origin = origin
    member val scaledSize = scaledSize
    member val size = size

[<Fable.Core.JS.Pojo>]
type private SymbolJs(path: string, ?anchor: obj, ?fillColor: string, ?fillOpacity: float, ?labelOrigin: obj, ?rotation: float, ?scale: float, ?strokeColor: string, ?strokeOpacity: float, ?strokeWeight: float) =
    member val path = path
    member val anchor = anchor
    member val fillColor = fillColor
    member val fillOpacity = fillOpacity
    member val labelOrigin = labelOrigin
    member val rotation = rotation
    member val scale = scale
    member val strokeColor = strokeColor
    member val strokeOpacity = strokeOpacity
    member val strokeWeight = strokeWeight

[<Fable.Core.JS.Pojo>]
type private IconSequenceJs(fixedRotation: bool, icon: obj, ?offset: obj, ?``repeat``: obj) =
    member val fixedRotation = fixedRotation
    member val icon = icon
    member val offset = offset
    member val ``repeat`` = ``repeat``

[<Fable.Core.JS.Pojo>]
type private MarkerLabelJs(text: string, ?className: string, ?color: string, ?fontFamily: string, ?fontSize: string, ?fontWeight: string) =
    member val text = text
    member val className = className
    member val color = color
    member val fontFamily = fontFamily
    member val fontSize = fontSize
    member val fontWeight = fontWeight

[<Fable.Core.JS.Pojo>]
type private MarkerJs(draggable: bool, ?label: obj, ?title: string, ?icon: obj, ?opacity: float, ?zIndex: int, ?animation: obj) =
    member val draggable = draggable
    member val label = label
    member val title = title
    member val icon = icon
    member val opacity = opacity
    member val zIndex = zIndex
    member val animation = animation

[<Fable.Core.JS.Pojo>]
type private InfoWindowJs(disableAutoPan: bool, ?minWidth: int, ?maxWidth: int, ?pixelOffset: obj, ?zIndex: int) =
    member val disableAutoPan = disableAutoPan
    member val minWidth = minWidth
    member val maxWidth = maxWidth
    member val pixelOffset = pixelOffset
    member val zIndex = zIndex

[<Fable.Core.JS.Pojo>]
type private PolylineJs(path: obj[], draggable: bool, editable: bool, geodesic: bool, visible: bool, strokeColor: string option, strokeOpacity: float option, strokeWeight: float option, ?icons: obj[], ?zIndex: int) =
    member val path = path
    member val draggable = draggable
    member val editable = editable
    member val geodesic = geodesic
    member val visible = visible
    member val strokeColor = strokeColor
    member val strokeOpacity = strokeOpacity
    member val strokeWeight = strokeWeight
    member val icons = icons
    member val zIndex = zIndex

[<Fable.Core.JS.Pojo>]
type private PolygonJs(paths: obj[][], draggable: bool, editable: bool, geodesic: bool, visible: bool, ?fillColor: string, ?fillOpacity: float, ?strokeColor: string, ?strokeOpacity: float, ?strokePosition: obj, ?strokeWeight: float, ?zIndex: int) =
    member val paths = paths
    member val draggable = draggable
    member val editable = editable
    member val geodesic = geodesic
    member val visible = visible
    member val fillColor = fillColor
    member val fillOpacity = fillOpacity
    member val strokeColor = strokeColor
    member val strokeOpacity = strokeOpacity
    member val strokePosition = strokePosition
    member val strokeWeight = strokeWeight
    member val zIndex = zIndex

[<Fable.Core.JS.Pojo>]
type private CircleJs(center: obj, radius: float, draggable: bool, editable: bool, visible: bool, ?fillColor: string, ?fillOpacity: float, ?strokeColor: string, ?strokeOpacity: float, ?strokePosition: obj, ?strokeWeight: float, ?zIndex: int) =
    member val center = center
    member val radius = radius
    member val draggable = draggable
    member val editable = editable
    member val visible = visible
    member val fillColor = fillColor
    member val fillOpacity = fillOpacity
    member val strokeColor = strokeColor
    member val strokeOpacity = strokeOpacity
    member val strokePosition = strokePosition
    member val strokeWeight = strokeWeight
    member val zIndex = zIndex

[<Fable.Core.JS.Pojo>]
type private WaypointJs(location: obj, stopover: bool) =
    member val location = location
    member val stopover = stopover

[<Fable.Core.JS.Pojo>]
type private DirectionsJs(origin: obj, destination: obj, travelMode: obj, ?waypoints: obj[]) =
    member val origin = origin
    member val destination = destination
    member val travelMode = travelMode
    member val waypoints = waypoints

[<Fable.Core.JS.Pojo>]
type private DirectionsRendererOptionsJs(draggable: bool, hideRouteList: bool, preserveViewport: bool) =
    member val draggable = draggable
    member val hideRouteList = hideRouteList
    member val preserveViewport = preserveViewport

[<Fable.Core.JS.Pojo>]
type private MapStyleJs(featureType: obj, elementType: obj, stylers: obj[]) =
    member val featureType = featureType
    member val elementType = elementType
    member val stylers = stylers

module Size =
    let toJs (size: Size) : obj =
        SizeJs(width = size.Width, height = size.Height) |> box

module PixelOrPercentage =
    let toJs (pixelOrPercentage: PixelOrPercentage) : obj =
        match pixelOrPercentage with
        | PixelOrPercentage.Pixel pixel -> $"{pixel}px"
        | PixelOrPercentage.Percentage percentage -> $"{percentage}%%"

module Point =
    let toJs ((x, y): Point) : obj =
        PointJs(x = x, y = y) |> box

module LatLng =
    let toJs ((lat, lng): LatLng) : obj =
        LatLngJs(lat = lat, lng = lng) |> box

    let fromJs (latLng: obj) : LatLng =
        LatLng(latLng?lat(), latLng?lng())

module Icon =
    let toJs (icon: Icon) : obj =
        (IconJs(
            icon.Url,
            ?anchor = (icon.Anchor |> Option.map Point.toJs),
            ?labelOrigin = (icon.LabelOrigin |> Option.map Point.toJs),
            ?origin = (icon.Origin |> Option.map Point.toJs),
            ?scaledSize = (icon.ScaledSize |> Option.map Size.toJs),
            ?size = (icon.Size |> Option.map Size.toJs)
        )) |> box

module Symbol =
    let toJs (symbol: Symbol) : obj =
        (SymbolJs(
            symbol.Path,
            ?anchor = (symbol.Anchor |> Option.map Point.toJs),
            ?fillColor = symbol.FillColor,
            ?fillOpacity = symbol.FillOpacity,
            ?labelOrigin = (symbol.LabelOrigin |> Option.map Point.toJs),
            ?rotation = symbol.Rotation,
            ?scale = symbol.Scale,
            ?strokeColor = symbol.StrokeColor,
            ?strokeOpacity = symbol.StrokeOpacity,
            ?strokeWeight = symbol.StrokeWeight
        )) |> box

module IconSequence =
    let toJs (iconSequence: IconSequence) : obj =
        (IconSequenceJs(
            iconSequence.FixedRotation,
            iconSequence.Icon |> Symbol.toJs,
            ?offset = (iconSequence.Offset |> Option.map PixelOrPercentage.toJs),
            ?``repeat`` = (iconSequence.Repeat |> Option.map PixelOrPercentage.toJs)
        )) |> box

module MarkerImage =
    let toJs (markerImage: MarkerImage) : obj =
        match markerImage with
        | MarkerImage.Icon icon -> icon |> Icon.toJs
        | MarkerImage.Symbol symbol -> symbol |> Symbol.toJs

module MarkerAnimation =
    let toJs (markerAnimation: MarkerAnimation) : obj =
        match markerAnimation with
        | MarkerAnimation.Bounce -> 1
        | MarkerAnimation.Drop -> 2

module MarkerLabel =
    let toJs (markerLabel: MarkerLabel) : obj =
        (MarkerLabelJs(
            markerLabel.Text,
            ?className = markerLabel.ClassName,
            ?color = markerLabel.Color,
            ?fontFamily = markerLabel.FontFamily,
            ?fontSize = markerLabel.FontSize,
            ?fontWeight = markerLabel.FontWeight
        )) |> box

module Marker =
    let toJs (marker: Marker) : obj =
        (MarkerJs(
            marker.Draggable,
            ?label = (marker.Label |> Option.map MarkerLabel.toJs),
            ?title = marker.Tooltip,
            ?icon = (marker.Image |> Option.map MarkerImage.toJs),
            ?opacity = marker.Opacity,
            ?zIndex = marker.ZIndex,
            ?animation = (marker.Animation |> Option.map MarkerAnimation.toJs)
        )) |> box

module InfoWindow =
    let toJs (infoWindow: InfoWindow) : obj =
        (InfoWindowJs(
            infoWindow.DisableAutoPan,
            ?minWidth = infoWindow.MinWidth,
            ?maxWidth = infoWindow.MaxWidth,
            ?pixelOffset = (infoWindow.PixelOffset |> Option.map Size.toJs),
            ?zIndex = infoWindow.ZIndex
        )) |> box

module StrokePosition =
    let toJs (strokePosition: StrokePosition) : obj =
        match strokePosition with
        | StrokePosition.Center -> 0
        | StrokePosition.Inside -> 1
        | StrokePosition.Outside -> 2

module Polyline =
    let toJs (polyline: Polyline) : obj =
        (PolylineJs(
            polyline.Path |> Array.map LatLng.toJs,
            polyline.Draggable,
            polyline.Editable,
            polyline.Geodesic,
            polyline.Visible,
            polyline.StrokeColor,
            polyline.StrokeOpacity,
            polyline.StrokeWeight,
            ?icons = (polyline.Icons |> Option.map (Array.map IconSequence.toJs)),
            ?zIndex = polyline.ZIndex
        )) |> box

module Polygon =
    let toJs (polygon: Polygon) : obj =
        (PolygonJs(
            polygon.Paths |> Array.map (fun path -> path |> Array.map LatLng.toJs),
            polygon.Draggable,
            polygon.Editable,
            polygon.Geodesic,
            polygon.Visible,
            ?fillColor = polygon.FillColor,
            ?fillOpacity = polygon.FillOpacity,
            ?strokeColor = polygon.StrokeColor,
            ?strokeOpacity = polygon.StrokeOpacity,
            ?strokePosition = (polygon.StrokePosition |> Option.map StrokePosition.toJs),
            ?strokeWeight = polygon.StrokeWeight,
            ?zIndex = polygon.ZIndex
        )) |> box

module Circle =
    let toJs (circle: Circle) : obj =
        (CircleJs(
            circle.Center |> LatLng.toJs,
            circle.Radius,
            circle.Draggable,
            circle.Editable,
            circle.Visible,
            ?fillColor = circle.FillColor,
            ?fillOpacity = circle.FillOpacity,
            ?strokeColor = circle.StrokeColor,
            ?strokeOpacity = circle.StrokeOpacity,
            ?strokePosition = (circle.StrokePosition |> Option.map StrokePosition.toJs),
            ?strokeWeight = circle.StrokeWeight,
            ?zIndex = circle.ZIndex
        )) |> box

module Shape =
    let toJs (shape: Shape) : obj =
        match shape with
        | Shape.Polyline polyline -> polyline |> Polyline.toJs
        | Shape.Polygon polygon -> polygon |> Polygon.toJs
        | Shape.Circle circle -> circle |> Circle.toJs

module Place =
    let toJs (place: Place) : obj =
        createObj [
            match place with
            | Place.Id id -> "placeId" ==> id
            | Place.LatLng latLng -> "location" ==> (latLng |> LatLng.toJs)
            | Place.Query query -> "query" ==> query
        ]

module TravelMode =
    let toJs (travelMode: TravelMode) : obj =
        match travelMode with
        | TravelMode.Driving -> "DRIVING"
        | TravelMode.Walking -> "WALKING"
        | TravelMode.Bicycling -> "BICYCLING"
        | TravelMode.Transit -> "TRANSIT"

module Waypoint =
    let toJs (waypoint: Waypoint) : obj =
        (WaypointJs(
            waypoint.Place |> Place.toJs,
            waypoint.IsStopover
        )) |> box

module Directions =
    let toJs (directions: Directions) : obj =
        (DirectionsJs(
            directions.Origin |> Place.toJs,
            directions.Destination |> Place.toJs,
            directions.TravelMode |> TravelMode.toJs,
            ?waypoints = (directions.Waypoints |> Option.map (Array.map Waypoint.toJs))
        )) |> box

module DirectionsRendererOptions =
    let toJs (directionsRendererOptions: DirectionsRendererOptions) : obj =
        (DirectionsRendererOptionsJs(
            directionsRendererOptions.Draggable,
            directionsRendererOptions.HideRouteList,
            directionsRendererOptions.PreserveViewport
        )) |> box

module MapFeatureType =
    let toJs (mapFeatureType: MapFeatureType) : obj =
        match mapFeatureType with
        | MapFeatureType.All -> "all"
        | MapFeatureType.Administrative -> "administrative"
        | MapFeatureType.AdministrativeCountry -> "administrative.country"
        | MapFeatureType.AdministrativeLandParcel -> "administrative.land_parcel"
        | MapFeatureType.AdministrativeLocality -> "administrative.locality"
        | MapFeatureType.AdministrativeNeighborhood -> "administrative.neighborhood"
        | MapFeatureType.AdministrativeProvince -> "administrative.province"
        | MapFeatureType.Landscape -> "landscape"
        | MapFeatureType.LandscapeManMade -> "landscape.man_made"
        | MapFeatureType.LandscapeNatural -> "landscape.natural"
        | MapFeatureType.LandscapeNaturalLandcover -> "landscape.natural.landcover"
        | MapFeatureType.LandscapeNaturalTerrain -> "landscape.natural.terrain"
        | MapFeatureType.PointsOfInterest -> "poi"
        | MapFeatureType.PointsOfInterestAttraction -> "poi.attraction"
        | MapFeatureType.PointsOfInterestBusiness -> "poi.business"
        | MapFeatureType.PointsOfInterestGovernment -> "poi.government"
        | MapFeatureType.PointsOfInterestMedical -> "poi.medical"
        | MapFeatureType.PointsOfInterestPark -> "poi.park"
        | MapFeatureType.PointsOfInterestPlaceOfWorship -> "poi.place_of_worship"
        | MapFeatureType.PointsOfInterestSchool -> "poi.school"
        | MapFeatureType.PointsOfInterestSportsComplex -> "poi.sports_complex"
        | MapFeatureType.Road -> "road"
        | MapFeatureType.RoadArterial -> "road.arterial"
        | MapFeatureType.RoadHighway -> "road.highway"
        | MapFeatureType.RoadHighwayControlledAccess -> "road.highway.controlled_access"
        | MapFeatureType.RoadLocal -> "road.local"
        | MapFeatureType.Transit -> "transit"
        | MapFeatureType.TransitLine -> "transit.line"
        | MapFeatureType.TransitStation -> "transit.station"
        | MapFeatureType.TransitStationAirport -> "transit.station.airport"
        | MapFeatureType.TransitStationBus -> "transit.station.bus"
        | MapFeatureType.TransitStationRail -> "transit.station.rail"
        | MapFeatureType.Water -> "water"

module MapElementType =
    let toJs (mapElementType: MapElementType) : obj =
        match mapElementType with
        | MapElementType.All -> "all"
        | MapElementType.Geometry -> "geometry"
        | MapElementType.GeometryFill -> "geometry.fill"
        | MapElementType.GeometryStroke -> "geometry.stroke"
        | MapElementType.Labels -> "labels"
        | MapElementType.LabelsIcon -> "labels.icon"
        | MapElementType.LabelsText -> "labels.text"
        | MapElementType.LabelsTextFill -> "labels.text.fill"
        | MapElementType.LabelsTextStroke -> "labels.text.stroke"

module MapStyler =
    let toJs (mapStyler: MapStyler) : obj =
        createObj [
            match mapStyler with
            | MapStyler.Color v -> "color" ==> v
            | MapStyler.Hue v -> "hue" ==> v
            | MapStyler.Lightness v -> "lightness" ==> v
            | MapStyler.Saturation v -> "saturation" ==> v
            | MapStyler.Gamma v -> "gamma" ==> v
            | MapStyler.InvertLightness v -> "invert_lightness" ==> v
            | MapStyler.Weight v -> "weight" ==> v
        ]

module MapStyle =
    let toJs (mapStyle: MapStyle) : obj =
        (MapStyleJs(
            mapStyle.FeatureType |> MapFeatureType.toJs,
            mapStyle.ElementType |> MapElementType.toJs,
            mapStyle.Stylers |> Array.map MapStyler.toJs
        )) |> box

module MapTypeId =
    let mapTypeIdString (maybeMapTypeId: Option<MapTypeId>) : Option<string> =
        maybeMapTypeId
        |> Option.map (fun mapTypeId ->
            match mapTypeId with
            | OSM       -> "OSM"
            | Roadmap   -> "roadmap"
            | Satellite -> "satellite"
            | Hybrid    -> "hybrid"
            | Terrain   -> "terrain"
        )
