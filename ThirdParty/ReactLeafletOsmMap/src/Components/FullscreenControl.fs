[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.FullscreenControl

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private FullscreenControlPropsJs
    ( ?position:            string, ?title: string, ?titleCancel: string, ?content: string,
      ?forceSeparateButton: bool, ?forcePseudoFullscreen: bool ) =
    member val position = position
    member val title = title
    member val titleCancel = titleCancel
    member val content = content
    member val forceSeparateButton = forceSeparateButton
    member val forcePseudoFullscreen = forcePseudoFullscreen

let private FullscreenControlComp: obj -> ReactElement = JsInterop.import "FullscreenControl" "react-leaflet-fullscreen"

type OsmMap with
    [<Component>]
    static member FullscreenControl (
        ?position:              ControlPosition,
        ?title:                 string,
        ?titleCancel:           string,
        ?content:               string,
        ?forceSeparateButton:   bool,
        ?forcePseudoFullscreen: bool)
        : ReactElement =
        let wrappedProps =
            FullscreenControlPropsJs(
                ?position              = (position |> Option.map (fun x -> x.ToJs())),
                ?title                 = title,
                ?titleCancel           = titleCancel,
                ?content               = content,
                ?forceSeparateButton   = forceSeparateButton,
                ?forcePseudoFullscreen = forcePseudoFullscreen
            ) |> box

        Fable.React.ReactBindings.React.createElement (FullscreenControlComp, wrappedProps, Seq.empty)

#endif
