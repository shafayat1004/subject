[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.Tooltip

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private TooltipPropsJs
    ( direction: string, permanent: bool, ?key: string, ?sticky: bool, ?opacity: float,
      ?offset: obj, ?position: obj ) =
    member val key = key
    member val direction = direction
    member val permanent = permanent
    member val sticky = sticky
    member val opacity = opacity
    member val offset = offset
    member val position = position

let private TooltipComp: obj -> ReactElement = JsInterop.import "Tooltip" "react-leaflet"

type OsmMap with
    [<Component>]
    static member Tooltip (
        ?key:       string,
        ?position:  GeoLocation,
        ?direction: TooltipDirection,
        ?permanent: bool,
        ?sticky:    bool,
        ?opacity:   float,
        ?offset:    Point,
        ?children:  ReactChildrenProp)
        : ReactElement =
        let wrappedProps =
            TooltipPropsJs(
                (direction |> Option.defaultValue TooltipDirection.Auto |> fun x -> x.ToJs()),
                (permanent |> Option.defaultValue false),
                ?key = key,
                ?sticky = sticky,
                ?opacity = opacity,
                ?offset = (offset |> Option.map (fun x -> x.ToJs())),
                ?position = (position |> Option.map (fun x -> x.ToJs()))
            ) |> box

        Fable.React.ReactBindings.React.createElement (TooltipComp, wrappedProps, (children |> Option.defaultValue Array.empty))

#endif