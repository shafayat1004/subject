[<AutoOpen>]
module ThirdParty.Map.Components.Web.LatLngFromAddress

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Services.Subscription
open ThirdParty.Map.Types
open Fable.Core.JsInterop
open ThirdParty.Map.Components.Constructors

type private LoaderOptions = {
    apiKey:    string
    version:   string
    libraries: array<string>
}

let private subscriptionImplementationValue : AdHocSubscriptionImplementation<AsyncData<LatLng>> =
    AdHocSubscriptionImplementation<AsyncData<LatLng>>(None, None)

let private onMapAnchorLoaded (apiKey: string) (address: string) (div: Browser.Types.Element) : unit =
    #if EGGSHELL_PLATFORM_IS_WEB
    promise {
        let loader =
            createNew
                (Fable.Core.JsInterop.import "Loader" "@googlemaps/js-api-loader")
                {
                    apiKey    = apiKey
                    version   = "weekly"
                    libraries = [| "places"; "drawing" |]
                }
        let! google = loader?load ()
        let placesService = createNew google?maps?places?PlacesService div

        let placeRequest = createObj [
            "query"  ==> address
            "fields" ==> [| "geometry" |]
        ]

        subscriptionImplementationValue.Update WillStartFetchingSoonHack

        placesService?findPlaceFromQuery(placeRequest, fun maybeResults ->
            match isNull maybeResults with
            | true -> subscriptionImplementationValue.Update Unavailable
            | false ->
                let results : array<obj> = maybeResults
                let result = results.[0]
                let latLng = (result?geometry?location?lat (), result?geometry?location?lng ())
                subscriptionImplementationValue.Update (Available latLng)
        )
    } |> ignore
    #else
    ignore apiKey
    ignore address
    ignore div
    failwith "Shouldn't be trying to run this on native"
    #endif

type ThirdParty.Map.Components.Constructors.Map.Web with
    [<Component>]
    static member LatLngFromAddress(
            address:  string,
            ``with``: AsyncData<LatLng> -> ReactElement,
            apiKey:   string,
            ?key:     string
        ) : ReactElement =
        ignore key

        LC.With.RefDom(
            onInitialize = (fun div -> onMapAnchorLoaded apiKey address div),
            ``with`` =
                (fun (bindDivRef, _) ->
                    element {
                        Fable.React.Standard.div [ Fable.React.Props.Ref bindDivRef ] [||]
                        LC.Subscribe(
                            subscription = subscriptionImplementationValue.Subscribe,
                            ``with``     = Subscribe.Raw ``with``
                        )
                    }
                )
        )
