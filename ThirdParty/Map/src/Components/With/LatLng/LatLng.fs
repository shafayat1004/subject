[<AutoOpen>]
module ThirdParty.Map.Components.With.LatLng

open Fable.React
open LibClient
open Rn.Components
open ThirdParty.Map.Components.Constructors
open ThirdParty.Map.Components.Web.LatLngFromAddress
open ThirdParty.Map.Components.Native.LatLngFromAddress

type LatLngType = ThirdParty.Map.Types.LatLng

type ThirdParty.Map.Components.Constructors.Map.With with
    [<Component>]
    static member LatLng(
            address: string,
            ``with``: AsyncData<LatLngType> -> ReactElement,
            apiKey:  string,
            ?key:    string
        ) : ReactElement =
        ignore key

        Rn.View(
            children =
                [|
                    #if EGGSHELL_PLATFORM_IS_WEB
                    Map.Web.LatLngFromAddress(address = address, apiKey = apiKey, ``with`` = ``with``)
                    #else
                    Map.Native.LatLngFromAddress(address = address, apiKey = apiKey, ``with`` = ``with``)
                    #endif
                |]
        )
