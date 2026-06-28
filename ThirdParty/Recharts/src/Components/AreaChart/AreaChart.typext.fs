// https://recharts.org/en-US/api/AreaChart

module ThirdParty.Recharts.Components.AreaChart

open LibClient
open LibClient.JsInterop

open Fable.Core
open Fable.React
open Fable.React.Props

open ThirdParty.Recharts.Components.Shared

let Horizontal = Layout.Horizontal
let Vertical   = Layout.Vertical

let Expand     = StackOffset.Expand
let Wiggle     = StackOffset.Wiggle
let Silhouette = StackOffset.Silhouette
let None       = StackOffset.None

type Props = (* GenerateMakeFunction *) {
    Layout:       Layout option         // defaultWithAutoWrap JsUndefined
    Width:        int option            // defaultWithAutoWrap JsUndefined
    Height:       int option            // defaultWithAutoWrap JsUndefined
    Data:         obj array option      // defaultWithAutoWrap JsUndefined
    Margin:       EdgeInsets option     // defaultWithAutoWrap Some { Top = 5; Bottom = 5; Left = 5; Right = 5; }
    StackOffset:  StackOffset option    // defaultWithAutoWrap JsUndefined
    OnClick:      (unit -> unit) option // defaultWithAutoWrap JsUndefined
    OnMouseEnter: (unit -> unit) option // defaultWithAutoWrap JsUndefined
    OnMouseMove:  (unit -> unit) option // defaultWithAutoWrap JsUndefined
    OnMouseLeave: (unit -> unit) option // defaultWithAutoWrap JsUndefined

}

[<Fable.Core.JS.Pojo>]
type private AreaChartPropsJs
    ( ?layout:       Layout,
      ?width:        int,
      ?height:       int,
      ?data:         obj array,
      ?margin:       obj,
      ?stackOffset:  StackOffset,
      ?onClick:      (unit -> unit),
      ?onMouseEnter: (unit -> unit),
      ?onMouseMove:  (unit -> unit),
      ?onMouseLeave: (unit -> unit) ) =
    member val layout       = layout
    member val width        = width
    member val height       = height
    member val data         = data
    member val margin       = margin
    member val stackOffset  = stackOffset
    member val onClick      = onClick
    member val onMouseEnter = onMouseEnter
    member val onMouseMove  = onMouseMove
    member val onMouseLeave = onMouseLeave

let private AreaChartRaw: obj = JsInterop.import "AreaChart" "recharts"
let Make (props: Props) (children: array<Fable.React.ReactElement>) =
    Fable.React.ReactBindings.React.createElement(
        AreaChartRaw,
        (AreaChartPropsJs(
            ?layout       = props.Layout,
            ?width        = props.Width,
            ?height       = props.Height,
            ?data         = props.Data,
            ?margin       = (props.Margin |> Option.map (fun v -> v.ToJS)),
            ?stackOffset  = props.StackOffset,
            ?onClick      = props.OnClick,
            ?onMouseEnter = props.OnMouseEnter,
            ?onMouseMove  = props.OnMouseMove,
            ?onMouseLeave = props.OnMouseLeave
        ) |> box),
        LibClient.ThirdParty.fixPotentiallySingleChild children
    )
