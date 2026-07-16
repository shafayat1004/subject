// https://recharts.org/en-US/api/ResponsiveContainer

module ThirdParty.Recharts.Components.ResponsiveContainer

open LibClient
open LibClient.JsInterop

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open ThirdParty.Recharts.Components.Shared

type Props = (* GenerateMakeFunction *) {
    Aspect:    float option // defaultWithAutoWrap JsUndefined
    Width:     Size option  // defaultWithAutoWrap Some (Size.Percentage 100.)
    Height:    Size option  // defaultWithAutoWrap Some (Size.Percentage 100.)
    MinWidth:  int option   // defaultWithAutoWrap JsUndefined
    MinHeight: int option   // defaultWithAutoWrap JsUndefined
    Debounce:  int option   // defaultWithAutoWrap Some 0

}

[<Fable.Core.JS.Pojo>]
type private ResponsiveContainerPropsJs
    ( ?aspect:    float,
      ?width:     obj,
      ?height:    obj,
      ?minWidth:  int,
      ?minHeight: int,
      ?debounce:  int ) =
    member val aspect    = aspect
    member val width     = width
    member val height    = height
    member val minWidth  = minWidth
    member val minHeight = minHeight
    member val debounce  = debounce

let private ResponsiveContainerRaw: obj = JsInterop.import "ResponsiveContainer" "recharts"
let Make (props: Props) (children: array<Fable.React.ReactElement>) =
    Fable.React.ReactBindings.React.createElement(
        ResponsiveContainerRaw,
        (ResponsiveContainerPropsJs(
            ?aspect    = props.Aspect,
            ?width     = (props.Width |> Option.map (fun v -> v.ToJS)),
            ?height    = (props.Height |> Option.map (fun v -> v.ToJS)),
            ?minWidth  = props.MinWidth,
            ?minHeight = props.MinHeight,
            ?debounce  = props.Debounce
        ) |> box),
        LibClient.ThirdParty.fixPotentiallySingleChild children
    )
