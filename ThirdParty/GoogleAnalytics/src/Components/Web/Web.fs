module ThirdParty.GoogleAnalytics.Web

open Fable.Core.JsInterop
open LibClient
open ThirdParty.GoogleAnalytics.Types

let private initializeApp: obj -> obj = import "initializeApp" "firebase/app"
let private getAnalytics:  obj -> obj = import "getAnalytics" "firebase/analytics"

let private logEvent  (_analytics: obj, _eventName: string, _properties: obj): unit = import "logEvent" "firebase/analytics"
let private setUserId (_analytics: obj, _userId: string): unit                      = import "setUserId" "firebase/analytics"

let mutable private maybeAnalytics : Option<obj> = None

let private initialize (app: obj)=
    maybeAnalytics <- Some (getAnalytics app)

let TrackViewItem (itemId: string) (itemName: string) (price: decimal) (currency: string) =
    maybeAnalytics
    |> Option.sideEffect (fun analytics ->
        let item =
            (FirebaseCommerceItemJs(itemId, itemName, float price))
            |> box
        let viewParam =
            (FirebaseCommerceEventJs(currency, float price, [| item |]))
            |> box
        logEvent(analytics, "view_item", viewParam)
    )

let TrackAddToCart (itemId: string) (itemName: string) (quantity: int) (price: decimal) (currency: string) =
    maybeAnalytics
    |> Option.sideEffect (fun analytics ->
        let item =
            (FirebaseCommerceItemJs(itemId, itemName, float price, ?quantity = Some quantity))
            |> box
        let cartParam =
            (FirebaseCommerceEventJs(currency, float price * float quantity, [| item |]))
            |> box
        logEvent(analytics, "add_to_cart", cartParam)
    )

let TrackBeginCheckout (items: List<Types.FirebaseItem>) (orderTotal: decimal) (currency: string) =
    maybeAnalytics
    |> Option.sideEffect (fun analytics ->
        let itemsInLibraryFormat : obj[] = items |> List.map (fun item -> item.toLibraryObj()) |> Array.ofList
        let beginCheckoutParam =
            (FirebaseBeginCheckoutJs(currency, float orderTotal, itemsInLibraryFormat))
            |> box
        logEvent(analytics, "begin_checkout", beginCheckoutParam)
    )

let TrackPurchase (orderId: string) (items: List<Types.FirebaseItem>) (orderTotal: decimal) (shippingCost: decimal) (currency: string) =
    maybeAnalytics
    |> Option.sideEffect (fun analytics ->
        let itemsInLibraryFormat : obj[] = items |> List.map (fun item -> item.toLibraryObj()) |> Array.ofList
        let purchaseParam =
            (FirebasePurchaseJs(orderId, currency, float orderTotal, float shippingCost, itemsInLibraryFormat))
            |> box
        logEvent(analytics, "purchase", purchaseParam)
    )

type GoogleAnalyticsTelemetrySink (config: Types.FirebaseConfig) =
    do (FirebaseConfigJs(config.ApiKey, config.AppId, config.MeasurementId, config.ProjectId)
        |> initializeApp
        |> initialize)

    interface ITelemetrySink with
        override _.TrackEvent (_: string) (_: TelemetryUser) (_: TelemetryProperties) : unit = ()

        override _.TrackScreenView (url: string) (user: TelemetryUser) (_: TelemetryProperties) : unit =
            maybeAnalytics
            |> Option.sideEffect (fun analytics ->
                match user with
                | TelemetryUser.Identified (userId, _) ->
                    setUserId (analytics, userId)
                | TelemetryUser.Anonymous ->
                    setUserId (analytics, null)

                logEvent (analytics, "screen_view", (FirebaseScreenViewJs(?firebase_screen = Some url) |> box))
            )
