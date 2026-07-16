// https://recharts.org/en-US/api/Legend

module ThirdParty.Recharts.Components.Legend

open LibClient
open LibClient.JsInterop

open Fable.Core
open ThirdParty.Recharts.Components.Shared

let Horizontal = Layout.Horizontal
let Vertical = Layout.Vertical

let Bottom = VerticalAlignment.Bottom
let Middle = VerticalAlignment.Middle
let Top = VerticalAlignment.Top

let Left = HorizontalAlignment.Left
let Center = HorizontalAlignment.Center
let Right = HorizontalAlignment.Right

type Props = (* GenerateMakeFunction *) {
    Width:               int option                 // defaultWithAutoWrap JsUndefined
    Height:              int option                 // defaultWithAutoWrap JsUndefined
    Layout:              Layout option              // defaultWithAutoWrap Some Layout.Horizontal
    HorizontalAlignment: HorizontalAlignment option // defaultWithAutoWrap Some HorizontalAlignment.Center
    VerticalAlignment:   VerticalAlignment option   // defaultWithAutoWrap Some VerticalAlignment.Bottom
    IconSize:            int option                 // defaultWithAutoWrap Some 14
    Type:                LegendType option          // defaultWithAutoWrap JsUndefined
    Margin:              EdgeInsets option          // defaultWithAutoWrap JsUndefined
    OnClick:             (unit -> unit) option      // defaultWithAutoWrap JsUndefined
    OnMouseDown:         (unit -> unit) option      // defaultWithAutoWrap JsUndefined
    OnMouseUp:           (unit -> unit) option      // defaultWithAutoWrap JsUndefined
    OnMouseMove:         (unit -> unit) option      // defaultWithAutoWrap JsUndefined
    OnMouseOver:         (unit -> unit) option      // defaultWithAutoWrap JsUndefined
    OnMouseOut:          (unit -> unit) option      // defaultWithAutoWrap JsUndefined
    OnMouseEnter:        (unit -> unit) option      // defaultWithAutoWrap JsUndefined
    OnMouseLeave:        (unit -> unit) option      // defaultWithAutoWrap JsUndefined
}

[<Fable.Core.JS.Pojo>]
type private LegendPropsJs
    ( ?width:         int,
      ?height:        int,
      ?layout:        Layout,
      ?align:         HorizontalAlignment,
      ?verticalAlign: VerticalAlignment,
      ?iconSize:      int,
      ?iconType:      LegendType,
      ?margin:        obj,
      ?onClick:       (unit -> unit),
      ?onMouseDown:   (unit -> unit),
      ?onMouseUp:     (unit -> unit),
      ?onMouseMove:   (unit -> unit),
      ?onMouseOver:   (unit -> unit),
      ?onMouseOut:    (unit -> unit),
      ?onMouseEnter:  (unit -> unit),
      ?onMouseLeave:  (unit -> unit) ) =
    member val width         = width
    member val height        = height
    member val layout        = layout
    member val align         = align
    member val verticalAlign = verticalAlign
    member val iconSize      = iconSize
    member val iconType      = iconType
    member val margin        = margin
    member val onClick       = onClick
    member val onMouseDown   = onMouseDown
    member val onMouseUp     = onMouseUp
    member val onMouseMove   = onMouseMove
    member val onMouseOver   = onMouseOver
    member val onMouseOut    = onMouseOut
    member val onMouseEnter  = onMouseEnter
    member val onMouseLeave  = onMouseLeave

let private Legend: obj = JsInterop.import "Legend" "recharts"
let Make =
    LibClient.ThirdParty.wrapComponentTransformingProps<Props>
        Legend
        (fun (props: Props) ->
            LegendPropsJs(
                ?width         = props.Width,
                ?height        = props.Height,
                ?layout        = props.Layout,
                ?align         = props.HorizontalAlignment,
                ?verticalAlign = props.VerticalAlignment,
                ?iconSize      = props.IconSize,
                ?iconType      = props.Type,
                ?margin        = (props.Margin |> Option.map (fun v -> v.ToJS)),
                ?onClick       = props.OnClick,
                ?onMouseDown   = props.OnMouseDown,
                ?onMouseUp     = props.OnMouseUp,
                ?onMouseMove   = props.OnMouseMove,
                ?onMouseOver   = props.OnMouseOver,
                ?onMouseOut    = props.OnMouseOut,
                ?onMouseEnter  = props.OnMouseEnter,
                ?onMouseLeave  = props.OnMouseLeave
            ) |> box
        )
