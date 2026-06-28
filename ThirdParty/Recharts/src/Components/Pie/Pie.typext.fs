// https://recharts.org/en-US/api/Pie

module ThirdParty.Recharts.Components.Pie

open LibClient

open Fable.Core
open Fable.Core.JsInterop

open ThirdParty.Recharts.Components.Shared

let Line      = LegendType.Line
let PlainLine = LegendType.PlainLine
let Square    = LegendType.Square
let Rectangle = LegendType.Rectangle
let Circle    = LegendType.Circle
let Cross     = LegendType.Cross
let Diamond   = LegendType.Diamond
let Star      = LegendType.Star
let Triangle  = LegendType.Triangle
let Wye       = LegendType.Wye
let None      = LegendType.None

let Ease      = AnimationEasing.Ease
let EaseIn    = AnimationEasing.EaseIn
let EaseOut   = AnimationEasing.EaseOut
let EaseInOut = AnimationEasing.EaseInOut
let Linear    = AnimationEasing.Linear

type LegendType = Shared.LegendType
type Offset     = Shared.Offset
type Radius     = Shared.Radius


type Props = (* GenerateMakeFunction *) {
    Cx:                Offset option             // defaultWithAutoWrap Some (Offset.Percentage 50.)
    Cy:                Offset option             // defaultWithAutoWrap Some (Offset.Percentage 50.)
    InnerRadius:       Radius option             // defaultWithAutoWrap Some (Radius.Number 0)
    OuterRadius:       Radius option             // defaultWithAutoWrap Some (Radius.Percentage 80.)
    StartAngle:        float option              // defaultWithAutoWrap Some 0.
    EndAngle:          float option              // defaultWithAutoWrap Some 360.
    MinAngle:          float option              // defaultWithAutoWrap Some 0.
    PaddingAngle:      float option              // defaultWithAutoWrap Some 0.
    NameKey:           string option             // defaultWithAutoWrap Some "Name"
    DataKey:           string option             // defaultWithAutoWrap JsUndefined
    Fill:              Color option              // defaultWithAutoWrap JsUndefined
    LegendType:        LegendType option         // defaultWithAutoWrap Some LegendType.Rectangle
    Data:              obj array option          // defaultWithAutoWrap JsUndefined
    IsAnimationActive: bool option               // defaultWithAutoWrap JsUndefined
    AnimationEasing:   AnimationEasing option    // defaultWithAutoWrap Some AnimationEasing.Ease
    OnAnimationStart:  (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnAnimationEnd:    (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnClick:           (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnMouseDown:       (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnMouseUp:         (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnMouseMove:       (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnMouseOver:       (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnMouseOut:        (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnMouseEnter:      (unit -> unit) option     // defaultWithAutoWrap JsUndefined
    OnMouseLeave:      (unit -> unit) option     // defaultWithAutoWrap JsUndefined

}

[<Fable.Core.JS.Pojo>]
type private PiePropsJs
    ( ?cx:                obj,
      ?cy:                obj,
      ?innerRadius:       obj,
      ?outerRadius:       obj,
      ?startAngle:        float,
      ?endAngle:          float,
      ?minAngle:          float,
      ?paddingAngle:      float,
      ?nameKey:           string,
      ?dataKey:           string,
      ?fill:              string,
      ?legendType:        LegendType,
      ?data:              obj array,
      ?isAnimationActive: bool,
      ?animationEasing:   AnimationEasing,
      ?onAnimationStart:  (unit -> unit),
      ?onAnimationEnd:    (unit -> unit),
      ?onClick:           (unit -> unit),
      ?onMouseDown:       (unit -> unit),
      ?onMouseUp:         (unit -> unit),
      ?onMouseMove:       (unit -> unit),
      ?onMouseOver:       (unit -> unit),
      ?onMouseOut:        (unit -> unit),
      ?onMouseEnter:      (unit -> unit),
      ?onMouseLeave:      (unit -> unit) ) =
    member val cx                = cx
    member val cy                = cy
    member val innerRadius       = innerRadius
    member val outerRadius       = outerRadius
    member val startAngle        = startAngle
    member val endAngle          = endAngle
    member val minAngle          = minAngle
    member val paddingAngle      = paddingAngle
    member val nameKey           = nameKey
    member val dataKey           = dataKey
    member val fill              = fill
    member val legendType        = legendType
    member val data              = data
    member val isAnimationActive = isAnimationActive
    member val animationEasing   = animationEasing
    member val onAnimationStart  = onAnimationStart
    member val onAnimationEnd    = onAnimationEnd
    member val onClick           = onClick
    member val onMouseDown       = onMouseDown
    member val onMouseUp         = onMouseUp
    member val onMouseMove       = onMouseMove
    member val onMouseOver       = onMouseOver
    member val onMouseOut        = onMouseOut
    member val onMouseEnter      = onMouseEnter
    member val onMouseLeave      = onMouseLeave

let private Pie: obj = JsInterop.import "Pie" "recharts"
let Make =
    LibClient.ThirdParty.wrapComponentTransformingProps<Props>
        Pie
        (fun (props: Props) ->
            PiePropsJs(
                ?cx                = (props.Cx |> Option.map (fun v -> v.ToJS)),
                ?cy                = (props.Cy |> Option.map (fun v -> v.ToJS)),
                ?innerRadius       = (props.InnerRadius |> Option.map (fun v -> v.ToJS)),
                ?outerRadius       = (props.OuterRadius |> Option.map (fun v -> v.ToJS)),
                ?startAngle        = props.StartAngle,
                ?endAngle          = props.EndAngle,
                ?minAngle          = props.MinAngle,
                ?paddingAngle      = props.PaddingAngle,
                ?nameKey           = props.NameKey,
                ?dataKey           = props.DataKey,
                ?fill              = (props.Fill |> Option.map (fun v -> v.ToReactXPString)),
                ?legendType        = props.LegendType,
                ?data              = props.Data,
                ?isAnimationActive = props.IsAnimationActive,
                ?animationEasing   = props.AnimationEasing,
                ?onAnimationStart  = props.OnAnimationStart,
                ?onAnimationEnd    = props.OnAnimationEnd,
                ?onClick           = props.OnClick,
                ?onMouseDown       = props.OnMouseDown,
                ?onMouseUp         = props.OnMouseUp,
                ?onMouseMove       = props.OnMouseMove,
                ?onMouseOver       = props.OnMouseOver,
                ?onMouseOut        = props.OnMouseOut,
                ?onMouseEnter      = props.OnMouseEnter,
                ?onMouseLeave      = props.OnMouseLeave
            ) |> box
        )

