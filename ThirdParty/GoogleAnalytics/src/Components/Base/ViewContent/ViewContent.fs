[<AutoOpen>]
module ThirdParty.GoogleAnalytics.Components.Base.ViewContent

open Fable.React
open LibClient
open ReactXP.Components

type ThirdParty.GoogleAnalytics.Components.Constructors.GoogleAnalytics.Base with
    [<Component>]
    static member ViewContent(
            id:       string,
            name:     string,
            price:    UnsignedDecimal,
            currency: string,
            ?key:     string
        ) : ReactElement =
        ignore key

        Hooks.useEffect(
            (fun () -> ThirdParty.GoogleAnalytics.Base.TrackViewItem id name price.Value currency),
            [| box id; box name; box price; box currency |]
        )

        RX.View()
