[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.CircleMarker

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components
open Fable.Core.JsInterop

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private CircleMarkerPropsJs
    ( center: obj, radius: int, key: string, ?color: string, ?weight: int, ?opacity: float,
      ?fill: bool, ?fillColor: string, ?fillOpacity: float, ?eventHandlers: obj ) =
    member val center = center
    member val radius = radius
    member val key = key
    member val color = color
    member val weight = weight
    member val opacity = opacity
    member val fill = fill
    member val fillColor = fillColor
    member val fillOpacity = fillOpacity
    member val eventHandlers = eventHandlers

let private CircleMarkerComp: obj -> ReactElement = JsInterop.import "CircleMarker" "react-leaflet"

type OsmMap with
    [<Component>]
    static member CircleMarker (
        center:        GeoLocation,
        radius:        int,
        ?key:          NonemptyString,
        ?color:        string,
        ?weight:       int,
        ?opacity:      float,
        ?fill:         bool,
        ?fillColor:    string,
        ?fillOpacity:  float,
        ?eventHandlers: array<LeafletEvent>,
        ?children:     ReactChildrenProp
        )
        : ReactElement =
        let wrappedProps =
            CircleMarkerPropsJs(
                center.ToJs(),
                radius,
                (key |> Option.map (fun x -> x.Value) |> Option.defaultValue (System.Guid.NewGuid().ToString())),
                ?color = color,
                ?weight = weight,
                ?opacity = opacity,
                ?fill = fill,
                ?fillColor = fillColor,
                ?fillOpacity = fillOpacity,
                ?eventHandlers = (eventHandlers |> Option.map LeafletEvent.ToJsObj)
            ) |> box

        Fable.React.ReactBindings.React.createElement (CircleMarkerComp, wrappedProps, (children |> Option.defaultValue Array.empty))

#endif



