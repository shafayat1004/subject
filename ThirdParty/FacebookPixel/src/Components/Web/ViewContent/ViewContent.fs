[<AutoOpen>]
module ThirdParty.FacebookPixel.Components.Web.ViewContent

open Fable.React
open LibClient
open ReactXP.Components

type ThirdParty.FacebookPixel.Components.Constructors.FacebookPixel.Web with
    [<Component>]
    static member ViewContent(
            id:    string,
            price: UnsignedDecimal,
            ?key:  string
        ) : ReactElement =
        ignore key

        Hooks.useEffect(
            (fun () -> ThirdParty.FacebookPixel.Base.TrackViewContent id price),
            [| box id; box price |]
        )

        RX.View()
