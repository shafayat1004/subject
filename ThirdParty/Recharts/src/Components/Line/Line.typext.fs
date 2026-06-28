// https://recharts.org/en-US/api/Line

module ThirdParty.Recharts.Components.Line

open LibClient
open LibClient.JsInterop

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

let InternalString = Color.InternalString
let InternalHex    = Color.InternalHex
let Rgb            = Color.Rgb
let Rgba           = Color.Rgba
let BlackAlpha     = Color.BlackAlpha
let WhiteAlpha     = Color.WhiteAlpha
let Black          = Color.Black
let White          = Color.White
let Transparent    = Color.Transparent

let Ease      = AnimationEasing.Ease
let EaseIn    = AnimationEasing.EaseIn
let EaseOut   = AnimationEasing.EaseOut
let EaseInOut = AnimationEasing.EaseInOut
let Linear    = AnimationEasing.Linear


[<RequireQualifiedAccess; StringEnum>]
type Type =
| Basis
| BasisClosed
| BasisOpen
| Linear
| LinearClosed
| Natural
| MonotoneX
| MonotoneY
| Monotone
| Step
| StepBefore
| StepAfter


type Props = (* GenerateMakeFunction *) {
    Type:              Type option            // defaultWithAutoWrap Some Type.Linear
    DataKey:           string option          // defaultWithAutoWrap JsUndefined
    LegendType:        LegendType option      // defaultWithAutoWrap JsUndefined
    Name:              string option          // defaultWithAutoWrap JsUndefined
    Stroke:            Color option           // defaultWithAutoWrap JsUndefined
    StrokeWidth:       int option             // defaultWithAutoWrap Some 1
    IsAnimationActive: bool option            // defaultWithAutoWrap JsUndefined
    AnimationEasing:   AnimationEasing option // defaultWithAutoWrap Some AnimationEasing.Ease
    OnAnimationStart:  (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnAnimationEnd:    (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnClick:           (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnMouseDown:       (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnMouseUp:         (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnMouseMove:       (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnMouseOver:       (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnMouseOut:        (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnMouseEnter:      (unit -> unit) option  // defaultWithAutoWrap JsUndefined
    OnMouseLeave:      (unit -> unit) option  // defaultWithAutoWrap JsUndefined
}

[<Fable.Core.JS.Pojo>]
type private LinePropsJs
    ( ?``type``:          Type,
      ?dataKey:           string,
      ?legendType:        LegendType,
      ?name:              string,
      ?stroke:            string,
      ?strokeWidth:       int,
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
    member val ``type``              = ``type``
    member val dataKey               = dataKey
    member val legendType            = legendType
    member val name                  = name
    member val stroke                = stroke
    member val strokeWidth           = strokeWidth
    member val isAnimationActive     = isAnimationActive
    member val animationEasing       = animationEasing
    member val onAnimationStart      = onAnimationStart
    member val onAnimationEnd        = onAnimationEnd
    member val onClick               = onClick
    member val onMouseDown           = onMouseDown
    member val onMouseUp             = onMouseUp
    member val onMouseMove           = onMouseMove
    member val onMouseOver           = onMouseOver
    member val onMouseOut            = onMouseOut
    member val onMouseEnter          = onMouseEnter
    member val onMouseLeave          = onMouseLeave

let private LineComponent: obj = JsInterop.import "Line" "recharts"
let Make =
    LibClient.ThirdParty.wrapComponentTransformingProps<Props>
        LineComponent
        (fun (props: Props) ->
            LinePropsJs(
                ?``type``          = props.Type,
                ?dataKey           = props.DataKey,
                ?legendType        = props.LegendType,
                ?name              = props.Name,
                ?stroke            = (props.Stroke |> Option.map (fun v -> v.ToReactXPString)),
                ?strokeWidth       = props.StrokeWidth,
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
 