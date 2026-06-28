module ThirdParty.Recharts.Components.Shared

open Fable.Core
open Fable.Core.JsInterop

[<Fable.Core.JS.Pojo>]
type private EdgeInsetsJs(top: int, bottom: int, left: int, right: int) =
    member val top    = top
    member val bottom = bottom
    member val left   = left
    member val right  = right

[<Fable.Core.JS.Pojo>]
type private ViewBoxJs(x: int, y: int, width: int, height: int) =
    member val x      = x
    member val y      = y
    member val width  = width
    member val height = height

[<Fable.Core.JS.Pojo>]
type private PositionJs(x: int, y: int) =
    member val x = x
    member val y = y

type EdgeInsets = {
    Top:    int
    Bottom: int
    Left:   int
    Right:  int
}
with
    member this.ToJS =
        EdgeInsetsJs(this.Top, this.Bottom, this.Left, this.Right) |> box

type ViewBox = {
    X:      int
    Y:      int
    Width:  int
    Height: int
}
with
    member this.ToJS =
        ViewBoxJs(this.X, this.Y, this.Width, this.Height) |> box

type Position = {
    X: int
    Y: int
}
with
    member this.ToJS =
        PositionJs(this.X, this.Y) |> box

type Size =
| Percentage of float
| Number of int
with
    member this.ToJS =
        match this with
        | Percentage v -> sprintf "%f%%" v |> box
        | Number v -> box v

type Offset =
| Percentage of float
| Number of int
with
    member this.ToJS =
        match this with
        | Percentage v -> sprintf "%f%%" v |> box
        | Number v -> box v

type Radius =
| Percentage of float
| Number of int
with
    member this.ToJS =
        match this with
        | Percentage v -> sprintf "%f%%" v |> box
        | Number v -> box v

type AxisId =
| String of string
| Number of int
with
    member this.ToJS =
        match this with
        | String v -> box v
        | Number v -> box v

type AxisName =
| String of string
| Number of int
with
    member this.ToJS =
        match this with
        | String v -> box v
        | Number v -> box v

type AxisUnit =
| String of string
| Number of int
with
    member this.ToJS =
        match this with
        | String v -> box v
        | Number v -> box v

[<RequireQualifiedAccess; StringEnum>]
type LegendType =
| Line
| PlainLine
| Square
| [<CompiledName("rect")>] Rectangle
| Circle
| Cross
| Diamond
| Star
| Triangle
| Wye
| None

[<RequireQualifiedAccess; StringEnum>]
type AnimationEasing =
| Ease
| [<CompiledName("ease-in")>] EaseIn
| [<CompiledName("ease-out")>] EaseOut
| [<CompiledName("ease-in-out")>] EaseInOut
| Linear

[<RequireQualifiedAccess; StringEnum>]
type Layout =
| Horizontal
| Vertical

[<RequireQualifiedAccess; StringEnum>]
type HorizontalAlignment =
| Left
| Center
| Right

[<RequireQualifiedAccess; StringEnum>]
type VerticalAlignment =
| Top
| Middle
| Bottom

[<RequireQualifiedAccess; StringEnum>]
type XAxisOrientation =
| Bottom
| Top

[<RequireQualifiedAccess; StringEnum>]
type YAxisOrientation =
| Left
| Right

[<RequireQualifiedAccess; StringEnum>]
type AxisType =
| Number
| Category

type AxisInterval =
| PreserveStart
| PreserveEnd
| PreserveStartEnd
| Every of int
with
    member this.ToJS =
        match this with
        | PreserveStart    -> box "preserveStart"
        | PreserveEnd      -> box "preserveEnd"
        | PreserveStartEnd -> box "preserveStartEnd"
        | Every v          -> box v

[<RequireQualifiedAccess; StringEnum>]
type AxisScale =
| Auto
| Linear
| Pow
| Sqrt
| Log
| Identity
| Time
| Band
| Point
| Ordinal
| Quantile
| Quantize
| Utc
| Sequential
| Threshold

[<RequireQualifiedAccess>]
type AxisDomainRange =
| Auto
| Number of float
with
    member this.ToJS =
        match this with
        | Auto     -> "auto"
        | Number v -> string v

type AxisDomain = {
    Lower: AxisDomainRange
    Upper: AxisDomainRange
}
with
    member this.ToJS =
        [|
            this.Lower.ToJS
            this.Upper.ToJS
        |]

[<RequireQualifiedAccess; StringEnum>]
type StackOffset =
| Expand
| Wiggle
| Silhouette
| None
