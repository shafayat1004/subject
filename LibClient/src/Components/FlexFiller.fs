[<AutoOpen>]
module LibClient.Components.FlexFiller

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

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member FlexFiller() : ReactElement =
        Rn.View(
            styles   = [| Styles.view |],
            children = [| |]
        )
