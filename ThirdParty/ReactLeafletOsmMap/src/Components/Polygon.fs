[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.Polygon

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components
open Fable.Core.JsInterop

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private PolygonPropsJs
    ( positions: obj, key: string, ?strock: bool, ?color: string, ?weight: int, ?opacity: float,
      ?fill: bool, ?fillColor: string, ?fillOpacity: float, ?eventHandlers: obj ) =
    member val positions = positions
    member val key = key
    member val strock = strock
    member val color = color
    member val weight = weight
    member val opacity = opacity
    member val fill = fill
    member val fillColor = fillColor
    member val fillOpacity = fillOpacity
    member val eventHandlers = eventHandlers

let private PolygonComp: obj -> ReactElement = JsInterop.import "Polygon" "react-leaflet"

type OsmMap with
    [<Component>]
    static member Polygon (
        positions:      PolygonPositions,
        ?key:           NonemptyString,
        ?strock:        bool,
        ?color:         string,
        ?weight:        int,
        ?opacity:       float,
        ?fill:          bool,
        ?fillColor:     string,
        ?fillOpacity:   float,
        ?children:      ReactChildrenProp,
        ?eventHandlers: array<LeafletEvent>
        )
        : ReactElement =
        let wrappedProps =
            PolygonPropsJs(
                positions.ToJs(),
                (key |> Option.map (fun x -> x.Value) |> Option.defaultValue (System.Guid.NewGuid().ToString())),
                ?strock = strock,
                ?color = color,
                ?weight = weight,
                ?opacity = opacity,
                ?fill = fill,
                ?fillColor = fillColor,
                ?fillOpacity = fillOpacity,
                ?eventHandlers = (eventHandlers |> Option.map LeafletEvent.ToJsObj)
            ) |> box

        Fable.React.ReactBindings.React.createElement (PolygonComp, wrappedProps, (children |> Option.defaultValue Array.empty))

#endif