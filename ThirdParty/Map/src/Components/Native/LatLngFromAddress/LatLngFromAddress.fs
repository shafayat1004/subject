[<AutoOpen>]
module ThirdParty.Map.Components.Native.LatLngFromAddress

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Services.Subscription
open ThirdParty.Map.Types
open Fable.Core
open Fable.Core.JsInterop
open LibClient.Services.HttpService.HttpService
open LibClient.Services.HttpService.Types
open ThirdParty.Map.Components.Constructors

let private subscriptionImplementationValue : AdHocSubscriptionImplementation<AsyncData<LatLng>> =
    AdHocSubscriptionImplementation<AsyncData<LatLng>>(None, None)

type private IResponse =
    abstract candidates: obj [] with get, set
    abstract status:     string with get, set

type ThirdParty.Map.Components.Constructors.Map.Native with
    [<Component>]
    static member LatLngFromAddress(
            address:  string,
            ``with``: AsyncData<LatLng> -> ReactElement,
            apiKey:   string,
            ?key:     string
        ) : ReactElement =
        ignore key

        Hooks.useEffect(
            (fun () ->
                #if EGGSHELL_PLATFORM_IS_WEB
                failwith "Shouldn't be trying to run this on web"
                #else
                async {
                    subscriptionImplementationValue.Update WillStartFetchingSoonHack

                    let placesEndpoint = {
                        Action  = Get
                        Url     = fun () -> "https://maps.googleapis.com/maps/api/place/findplacefromtext/json?fields=geometry&input=" + address + "&inputtype=textquery&key=" + apiKey
                        Payload = NoPayload
                        Result  = mapHttpResult<IResponse>
                    }

                    let! response = LibClient.ServiceInstances.services().Http.Request placesEndpoint () () |> Async.TryCatch

                    match response with
                    | Ok results when results.status = "OK" ->
                        let result = results.candidates.[0]
                        let latLng = (result?geometry?location?lat, result?geometry?location?lng)
                        subscriptionImplementationValue.Update (Available latLng)
                    | _ -> subscriptionImplementationValue.Update Unavailable
                } |> startSafely
                #endif
            ),
            [| box address; box apiKey |]
        )

        LC.Subscribe(
            subscription = subscriptionImplementationValue.Subscribe,
            ``with``     = Subscribe.Raw ``with``
        )
