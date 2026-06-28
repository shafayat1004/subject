module ThirdParty.GoogleAnalytics.Types

open Fable.Core


type FirebaseConfig = {
    ApiKey:        string
    AppId:         string
    MeasurementId: string
    ProjectId:     string
}

[<Fable.Core.JS.Pojo>]
type FirebaseItemJs(item_id: string, price: float) =
    member val item_id = item_id
    member val price = price

[<Fable.Core.JS.Pojo>]
type FirebaseCommerceItemJs(item_id: string, item_name: string, price: float, ?quantity: int) =
    member val item_id = item_id
    member val item_name = item_name
    member val price = price
    member val quantity = quantity

[<Fable.Core.JS.Pojo>]
type FirebaseCommerceEventJs(currency: string, value: float, items: obj[]) =
    member val currency = currency
    member val value = value
    member val items = items

[<Fable.Core.JS.Pojo>]
type FirebaseBeginCheckoutJs(currency: string, value: float, items: obj[]) =
    member val currency = currency
    member val value = value
    member val items = items

[<Fable.Core.JS.Pojo>]
type FirebasePurchaseJs(transaction_id: string, currency: string, value: float, shipping: float, items: obj[]) =
    member val transaction_id = transaction_id
    member val currency = currency
    member val value = value
    member val shipping = shipping
    member val items = items

[<Fable.Core.JS.Pojo>]
type FirebaseConfigJs(apiKey: string, appId: string, measurementId: string, projectId: string) =
    member val apiKey = apiKey
    member val appId = appId
    member val measurementId = measurementId
    member val projectId = projectId

[<Fable.Core.JS.Pojo>]
type FirebaseScreenViewJs(?firebase_screen: string, ?screen_name: string) =
    member val firebase_screen = firebase_screen
    member val screen_name = screen_name

type FirebaseItem =
    {
        ItemId: string
        Price:  decimal
    }
    member this.toLibraryObj () : obj =
        (FirebaseItemJs(this.ItemId, float this.Price)) |> box
