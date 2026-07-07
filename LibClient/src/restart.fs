[<AutoOpen>]
module LibClient.Restart

open LibClient
open LibClient.Services.ImageService
open LibClient.Components
open Rn.Components
open Rn.Helpers
open LibClient.JsInterop

let private restartElement =
    LC.Centered (
        Rn.ActivityIndicator (
            color = "#cccccc"
        )
    )

let restartApp (element: ReactElement) () : unit =
    Rn.UserInterface.setMainView restartElement

    runLater (TimeSpan.FromMilliseconds 2) (fun () ->
        Rn.UserInterface.setMainView element
    )
