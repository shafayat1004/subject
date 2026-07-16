[<AutoOpen>]
module LibClient.Components.Legacy_TopNav_Filler

open Fable.React
open LibClient
open Rn.Components
open Rn.Styles

module private Styles =
    let stub = makeViewStyles { width 42 }

type LibClient.Components.Constructors.LC.Legacy.TopNav with
    [<Component>]
    static member Filler(?children: ReactChildrenProp, ?count: int, ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key           |> ignore
        children      |> ignore
        xLegacyStyles |> ignore
        let n = defaultArg count 1
        Rn.View(
            children = [|
                castAsElement [| for _ in 0..n do Rn.View(styles = [| Styles.stub |]) |]
            |]
        )
