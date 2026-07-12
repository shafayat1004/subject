[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.MapContainer

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components
open LibClient.Components

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private MapContainerPropsJs
    ( ?id:    string, ?center: obj, ?zoom: int, ?bounds: obj, ?scrollWheelZoom: bool,
      ?style: obj, ?ref: IRefValue<obj> ) =
    member val id = id
    member val center = center
    member val zoom = zoom
    member val bounds = bounds
    member val scrollWheelZoom = scrollWheelZoom
    member val style = style
    member val ``ref`` = ``ref``

let private MapContainerComp: obj -> ReactElement = JsInterop.import "MapContainer" "react-leaflet"
let useMap: unit -> obj                           = JsInterop.import "useMap" "react-leaflet"
let useMapEvents: obj -> obj                      = JsInterop.import "useMapEvents" "react-leaflet"
let leaflet: obj                                  = JsInterop.import "*" "leaflet"

type OsmMap with
    [<Component>]
    static member MapContainer (
        ?id:              string,
        ?center:          GeoLocation,
        ?zoom:            int,
        ?bounds:          LatLngBounds,
        ?scrollWheelZoom: bool,
        ?style:           OsmMapStyle,
        ?children:        ReactChildrenProp,
        ?mapRef:          IRefValue<obj>)
        : ReactElement =
        let wrappedProps =
            MapContainerPropsJs(
                ?id              = id,
                ?center          = (center |> Option.map (fun x -> x.ToJs())),
                ?zoom            = zoom,
                ?bounds          = (bounds |> Option.map (fun x -> x.ToJs())),
                ?scrollWheelZoom = scrollWheelZoom,
                ?style           = (style |> Option.map (fun x -> x.ToJs())),
                ?ref             = mapRef
            ) |> box

        Fable.React.ReactBindings.React.createElement (MapContainerComp, wrappedProps, children |> Option.defaultValue Array.empty)

#endif
