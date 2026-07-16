// https://recharts.org/en-US/api/PieChart

module ThirdParty.Recharts.Components.PieChart

open LibClient

open Fable.Core
open Fable.Core.JsInterop

open ThirdParty.Recharts.Components.Shared

type Props = (* GenerateMakeFunction *) {
    Width:        int option            // defaultWithAutoWrap JsUndefined
    Height:       int option            // defaultWithAutoWrap JsUndefined
    Margin:       EdgeInsets option     // defaultWithAutoWrap Some { Top = 5; Bottom = 5; Left = 5; Right = 5; }
    OnClick:      (unit -> unit) option // defaultWithAutoWrap JsUndefined
    OnMouseEnter: (unit -> unit) option // defaultWithAutoWrap JsUndefined
    OnMouseLeave: (unit -> unit) option // defaultWithAutoWrap JsUndefined

}

[<Fable.Core.JS.Pojo>]
type private PieChartPropsJs
    ( ?width:        int,
      ?height:       int,
      ?margin:       obj,
      ?onClick:      (unit -> unit),
      ?onMouseEnter: (unit -> unit),
      ?onMouseLeave: (unit -> unit) ) =
    member val width        = width
    member val height       = height
    member val margin       = margin
    member val onClick      = onClick
    member val onMouseEnter = onMouseEnter
    member val onMouseLeave = onMouseLeave

let private PieChart: obj = JsInterop.import "PieChart" "recharts"
let Make =
    LibClient.ThirdParty.wrapComponentTransformingProps<Props>
        PieChart
        (fun (props: Props) ->
            PieChartPropsJs(
                ?width        = props.Width,
                ?height       = props.Height,
                ?margin       = (props.Margin |> Option.map (fun v -> v.ToJS)),
                ?onClick      = props.OnClick,
                ?onMouseEnter = props.OnMouseEnter,
                ?onMouseLeave = props.OnMouseLeave
            ) |> box
        )
