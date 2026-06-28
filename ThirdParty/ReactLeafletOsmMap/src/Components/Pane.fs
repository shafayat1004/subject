[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.Pane

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private PanePropsJs ( name: string, ?style: obj ) =
    member val name = name
    member val style = style

let private PaneComp: obj -> ReactElement = JsInterop.import "Pane" "react-leaflet"

type OsmMap with
    [<Component>]
    static member PaneComp (
            name:      string,
            ?children: ReactChildrenProp,
            ?style:    PaneStyle
        )
        : ReactElement =
        let wrappedProps =
            PanePropsJs(
                name,
                ?style = (style |> Option.map (fun x -> x.ToJs()))
            ) |> box

        Fable.React.ReactBindings.React.createElement (PaneComp, wrappedProps, children |> Option.defaultValue Array.empty)

#endif