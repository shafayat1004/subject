module ThirdParty.GoogleAnalytics.Native

open Fable.Core
open Fable.Core.JsInterop
open LibClient
open ThirdParty.GoogleAnalytics.Types

let Analytics (_screenViewParam: obj): obj  = importDefault "@react-native-firebase/analytics"

let private setAnalyticsUserForTelemetryUser (user: TelemetryUser) : unit =
    match user with
    | TelemetryUser.Identified (userId, _) ->
        Analytics()?setUserId userId
    | TelemetryUser.Anonymous ->
        Analytics()?setUserId null

let TrackViewItem (itemId: string) (itemName: string) (price: decimal) (currency: string) =
    Telemetry.GetUser() |> setAnalyticsUserForTelemetryUser
    let item =
        (FirebaseCommerceItemJs(itemId, itemName, float price))
        |> box
    let viewParam =
        (FirebaseCommerceEventJs(currency, float price, [| item |]))
        |> box
    Analytics()?logViewItem viewParam

let TrackAddToCart (itemId: string) (itemName: string) (quantity: int) (price: decimal) (currency: string) =
    Telemetry.GetUser() |> setAnalyticsUserForTelemetryUser
    let item =
        (FirebaseCommerceItemJs(itemId, itemName, float price, ?quantity = Some quantity))
        |> box
    let cartParam =
        (FirebaseCommerceEventJs(currency, float price * float quantity, [| item |]))
        |> box
    Analytics()?logAddToCart cartParam

let TrackBeginCheckout (items: List<FirebaseItem>) (orderTotal: decimal) (currency: string) =
    Telemetry.GetUser() |> setAnalyticsUserForTelemetryUser
    let itemsInLibraryFormat : obj[] = items |> List.map (fun item -> item.toLibraryObj()) |> Array.ofList
    let beginCheckoutParam =
        (FirebaseBeginCheckoutJs(currency, float orderTotal, itemsInLibraryFormat))
        |> box
    Analytics()?logBeginCheckout beginCheckoutParam

let TrackPurchase (orderId: string) (items: List<FirebaseItem>) (orderTotal: decimal) (shippingCost: decimal) (currency: string) =
    Telemetry.GetUser() |> setAnalyticsUserForTelemetryUser
    let itemsInLibraryFormat: obj[] = items |> List.map (fun item -> item.toLibraryObj()) |> Array.ofList
    let purchaseParam =
        (FirebasePurchaseJs(orderId, currency, float orderTotal, float shippingCost, itemsInLibraryFormat))
        |> box
    Analytics()?logPurchase purchaseParam

type GoogleAnalyticsTelemetrySink () =
    interface ITelemetrySink with
        member _.TrackEvent (_: string) (_: TelemetryUser) (_: TelemetryProperties) : unit = ()

        member _.TrackScreenView (url: string) (user: TelemetryUser) (_: TelemetryProperties) : unit =
            setAnalyticsUserForTelemetryUser user

            Analytics()?logScreenView (FirebaseScreenViewJs(?screen_name = Some url) |> box)
            |> Async.AwaitPromise
            |> startSafely
