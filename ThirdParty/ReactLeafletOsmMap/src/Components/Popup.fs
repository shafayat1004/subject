[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.Popup

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private PopupPropsJs
    ( key: string, offset: obj, maxWidth: int, minWidth: int, autoPan: bool, keepInView: bool,
      closeButton: bool, autoClose: bool, closeOnClick: bool, closeOnEscapeKey: bool,
      ?position: obj, ?maxHeight: int, ?autoPanPadding: obj ) =
    member val key = key
    member val offset = offset
    member val position = position
    member val maxWidth = maxWidth
    member val minWidth = minWidth
    member val maxHeight = maxHeight
    member val autoPan = autoPan
    member val autoPanPadding = autoPanPadding
    member val keepInView = keepInView
    member val closeButton = closeButton
    member val autoClose = autoClose
    member val closeOnClick = closeOnClick
    member val closeOnEscapeKey = closeOnEscapeKey

let private PopupComp: obj -> ReactElement = JsInterop.import "Popup" "react-leaflet"

type OsmMap with
    [<Component>]
    static member Popup (
        key:               string,
        ?position:         GeoLocation,
        ?offset:           Point,
        ?maxWidth:         int,
        ?minWidth:         int,
        ?maxHeight:        int,
        ?autoPan:          bool,
        ?autoPanPadding:   Point,
        ?keepInView:       bool,
        ?closeButton:      bool,
        ?autoClose:        bool,
        ?closeOnClick:     bool,
        ?closeOnEscapeKey: bool,
        ?children:         ReactChildrenProp)
        : ReactElement =
        let wrappedProps =
            PopupPropsJs(
                key,
                (offset |> Option.defaultValue (Point (0, 7)) |> fun x -> x.ToJs()),
                (maxWidth |> Option.defaultValue 300),
                (minWidth |> Option.defaultValue 50),
                (autoPan |> Option.defaultValue false),
                (keepInView |> Option.defaultValue false),
                (closeButton |> Option.defaultValue true),
                (autoClose |> Option.defaultValue true),
                (closeOnClick |> Option.defaultValue true),
                (closeOnEscapeKey |> Option.defaultValue true),
                ?position = (position |> Option.map (fun x -> x.ToJs())),
                ?maxHeight = maxHeight,
                ?autoPanPadding = (autoPanPadding |> Option.map (fun x -> x.ToJs()))
            ) |> box

        Fable.React.ReactBindings.React.createElement (PopupComp, wrappedProps, (children |> Option.defaultValue Array.empty))

#endif