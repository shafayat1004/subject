[<AutoOpen>]
module LibClient.Components.Nav_Bottom_Filler

open Fable.React
open LibClient
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            flex 1
        }

type LibClient.Components.Constructors.LC.Nav.Bottom with
    [<Component>]
    static member Filler(
            ?key: string) : ReactElement =
        key |> ignore

        Rn.View(
            styles = [| Styles.view |]
        )
