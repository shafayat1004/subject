[<AutoOpen>]
module ThirdParty.ReactLeafletOsmMap.Components.FeatureGroup

open Fable.Core
open Fable.React
open LibClient
open LibClient.JsInterop

open ThirdParty.ReactLeafletOsmMap.Components

#if EGGSHELL_PLATFORM_IS_WEB

[<Fable.Core.JS.Pojo>]
type private FeatureGroupPropsJs ( ?key: string, ?ref: obj -> unit ) =
    member val key = key
    member val ``ref`` = ``ref``

let private FeatureGroupComp: obj -> ReactElement = JsInterop.import "FeatureGroup" "react-leaflet"

type OsmMap with
    [<Component>]
    static member FeatureGroup (
        ?key:                 string,
        ?onfeatureGroupReady: obj -> unit,
        ?children:            ReactChildrenProp)
        : ReactElement =
        let wrappedProps =
            FeatureGroupPropsJs(?key = key, ?ref = onfeatureGroupReady) |> box

        Fable.React.ReactBindings.React.createElement (FeatureGroupComp, wrappedProps, (children |> Option.defaultValue Array.empty))

#endif