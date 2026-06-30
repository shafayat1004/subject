[<AutoOpen>]
module LibClient.Components.With_Geolocation

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Browser.Types
open LibClient
open LibClient.Components

module LC =
    module With =
        module Geolocation =
            type LatLng = float * float

open LC.With.Geolocation

let private getCurrentPosition () : Async<obj> =
    Async.FromContinuations (fun (resolve, reject, _) ->
        let navigator = Browser.Dom.window?navigator

        if isNull navigator || isNull navigator?geolocation then
            reject (exn "Geolocation not available")
        else
            navigator?geolocation?getCurrentPosition(
                (fun pos -> resolve pos),
                (fun _err -> reject (exn "Geolocation error"))))

type LC.With with
    [<Component>]
    static member Geolocation (``with``: Option<LatLng> -> ReactElement) : ReactElement =
        let latLngState = Hooks.useState<Option<LatLng>> None

        Hooks.useEffect(
            fun () ->
                async {
                    let! latLngResult = getCurrentPosition () |> Async.TryCatch

                    let latLng =
                        match latLngResult with
                        | Ok latLng -> Some (latLng?coords?latitude, latLng?coords?longitude)
                        | Error _   -> None

                    latLngState.update latLng
                } |> startSafely
        )

        ``with`` latLngState.current
