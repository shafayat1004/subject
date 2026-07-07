module Rn.Styles.Animation

open System
open Fable.Core
open Fable.Core.JsInterop
open LibClient

[<Fable.Core.JS.Pojo>]
type private AnimationLoopOptionsJs (?restartFrom: float) =
    member val restartFrom = restartFrom

[<Fable.Core.JS.Pojo>]
type private AnimatedTimingOptionsJs
    ( toValue: float, duration: float, useNativeDriver: bool,
      ?delay: float, ?easing: obj, ?loop: obj ) =
    member val toValue = toValue
    member val duration = duration
    member val useNativeDriver = useNativeDriver
    member val delay = delay
    member val easing = easing
    member val loop = loop

[<Fable.Core.JS.Pojo>]
type private AnimatedTimingSimpleOptionsJs
    ( toValue: float, duration: int, useNativeDriver: bool, ?easing: obj ) =
    member val toValue = toValue
    member val duration = duration
    member val useNativeDriver = useNativeDriver
    member val easing = easing

[<Fable.Core.JS.Pojo>]
type private InterpolationConfigJs ( inputRange: obj, outputRange: obj ) =
    member val inputRange = inputRange
    member val outputRange = outputRange

[<RequireQualifiedAccess>]
type Easing =
| Linear
| Out
| In
| InOut
| InBack
| OutBack
| InOutBack
| StepStart
| StepEnd
| Steps of Intervals: int * MaybeEnd: Option<bool>
| CubicBezier of Coords1: (double * double) * Coords2: (double * double)
with
    member this.ToRn : obj =
        let rnEasing = Rn.RnPrimitives.Easing

        match this with
        | Linear -> rnEasing?linear
        | Out -> rnEasing?out(rnEasing?ease)
        | In -> rnEasing?``in``(rnEasing?ease)
        | InOut -> rnEasing?inOut(rnEasing?ease)
        | InBack -> rnEasing?``in``(rnEasing?back())
        | OutBack -> rnEasing?out(rnEasing?back())
        | InOutBack -> rnEasing?inOut(rnEasing?back())
        | StepStart -> box (fun (t: float) -> if t > 0.0 then 1.0 else 0.0)
        | StepEnd -> box (fun (t: float) -> if t >= 1.0 then 1.0 else 0.0)
        | Steps (intervals, maybeEnd) ->
            let n = float intervals

            match maybeEnd with
            | Some true ->
                box (fun (t: float) -> if t >= 1.0 then 1.0 else Math.Floor(t * n) / n)
            | _ ->
                box (fun (t: float) -> if t <= 0.0 then 0.0 else Math.Min(1.0, Math.Ceiling(t * n) / n))
        | CubicBezier ((x1, y1), (x2, y2)) ->
            // TODO: would be nice to restrict values to 0 >= v <= 1
            rnEasing?bezier(x1, y1, x2, y2)

type InterpolationConfig internal(raw: obj) =
    static member Create(mappings: seq<double * double>) =
        InterpolationConfigJs(
            mappings |> Seq.map fst |> Seq.toArray |> box,
            mappings |> Seq.map snd |> Seq.toArray |> box
        )
        |> box
        |> InterpolationConfig

    static member Create(mappings: seq<double * Color>) =
        InterpolationConfigJs(
            mappings |> Seq.map fst |> Seq.toArray |> box,
            mappings |> Seq.map snd |> Seq.map (fun color -> color.ToRnString) |> Seq.toArray |> box
        )
        |> box
        |> InterpolationConfig

    member internal _.Raw = raw

type RawInterpolatedValue =
    abstract member interpolate: obj -> RawInterpolatedValue

type InterpolatedValue internal(raw: RawInterpolatedValue) =
    member _.Interpolate(config: InterpolationConfig): InterpolatedValue =
        raw.interpolate(config)
        |> InterpolatedValue

    member internal _.Raw = raw

type RawAnimatedValue =
    abstract member setValue: double -> unit
    abstract member interpolate: obj -> RawInterpolatedValue

type AnimatedValue internal(raw: RawAnimatedValue) =
    static member Create (value: double) : AnimatedValue =
        let raw: RawAnimatedValue =
            createNew (Rn.RnPrimitives.Animated?Value) value
            |> unbox

        AnimatedValue raw

    member internal _.Raw = raw

    member _.SetValue (value: double): unit =
        raw.setValue value

    member _.SetValue (value: int): unit =
        raw.setValue (double value)

    member _.Interpolate (config: InterpolationConfig): InterpolatedValue =
        raw.interpolate(config.Raw)
        |> InterpolatedValue

[<RequireQualifiedAccess>]
type AnimatableValue =
| Value of AnimatedValue
| Interpolated of InterpolatedValue
with
    member this.Raw =
        match this with
        | Value value -> box value.Raw
        | Interpolated interpolated -> box interpolated.Raw

type RawAnimation =
    abstract member start: Option<unit -> unit> -> unit
    abstract member stop:  unit -> unit

type Animation internal(raw: RawAnimation) =
    static member Timing(
            animatedValue: AnimatedValue,
            toValue: double,
            duration: TimeSpan,
            ?delay: TimeSpan,
            ?easing: Easing,
            ?restartFrom: double)
            : Animation =
        let maybeDelayMs = delay |> Option.map (fun d -> d.TotalMilliseconds)
        let maybeEasing = easing |> Option.map (fun e -> e.ToRn)
        let maybeLoop =
            restartFrom
            |> Option.map (fun v -> AnimationLoopOptionsJs(?restartFrom = Some v) |> box)

        let fields =
            AnimatedTimingOptionsJs(
                toValue,
                duration.TotalMilliseconds,
                false,
                ?delay = maybeDelayMs,
                ?easing = maybeEasing,
                ?loop = maybeLoop
            ) |> box

        Rn.RnPrimitives.Animated?timing(animatedValue.Raw, fields)
        |> Animation

    static member Parallel([<ParamArray>] animations: array<Animation>): Animation =
        Rn.RnPrimitives.Animated?("parallel")(animations |> Seq.map (fun animation -> animation.Raw) |> Seq.toArray)
        |> Animation

    static member Sequence([<ParamArray>] animations: array<Animation>): Animation =
        Rn.RnPrimitives.Animated?sequence(animations |> Seq.map (fun animation -> animation.Raw) |> Seq.toArray)
        |> Animation

    member internal _.Raw = raw

    member _.Start(?onComplete: unit -> unit) : unit =
        raw.start onComplete

    member _.Stop() : unit =
        raw.stop ()

// TODO: delete everything from here down once animation migration is complete
type GetOrCreateAnimatedValue      = (* key *) string -> (* initialValue *) double -> RawAnimatedValue
type AnimatedRulesConstructor      = GetOrCreateAnimatedValue -> RnStyleRulesObject
type AnimatedAnimationsConstructor = GetOrCreateAnimatedValue -> RawAnimation

[<AutoOpen>]
module RnAnimationExtensions =
    type RawAnimatedValue with
        static member Simple (key: string) (initialValue: double) : GetOrCreateAnimatedValue -> RawAnimatedValue =
            fun getOrCreateAnimatedValue -> getOrCreateAnimatedValue key initialValue

    type RawAnimation with
        static member Simple (toValue: double, durationMillis: int) : (RawAnimatedValue -> RawAnimation) =
            RawAnimation.Simple (toValue, durationMillis, ?easing = None)

        static member Simple (toValue: double, durationMillis: int, ?easing: Easing) : (RawAnimatedValue -> RawAnimation) =
            fun (value: RawAnimatedValue) ->
                let maybeEasing = easing |> Option.map (fun e -> e.ToRn)
                let fields =
                    AnimatedTimingSimpleOptionsJs(
                        toValue,
                        durationMillis,
                        true,
                        ?easing = maybeEasing
                    ) |> box

                Rn.RnPrimitives.Animated?timing(value, fields)

        static member Parallel (a1: RawAnimatedValue -> RawAnimation, a2: RawAnimatedValue -> RawAnimation) =
            fun (v1: RawAnimatedValue) (v2: RawAnimatedValue) ->
                Rn.RnPrimitives.Animated?("parallel")([
                    a1 v1
                    a2 v2
                ] |> Array.ofList)

        static member Parallel (a1: RawAnimatedValue -> RawAnimation, a2: RawAnimatedValue -> RawAnimation, a3: RawAnimatedValue -> RawAnimation) =
            fun (v1: RawAnimatedValue) (v2: RawAnimatedValue) (v3: RawAnimatedValue) ->
                Rn.RnPrimitives.Animated?("parallel")([
                    a1 v1
                    a2 v2
                    a3 v3
                ] |> Array.ofList)

        static member Sequence (a1: RawAnimatedValue -> RawAnimation, a2: RawAnimatedValue -> RawAnimation) =
            fun (v1: RawAnimatedValue) (v2: RawAnimatedValue) ->
                Rn.RnPrimitives.Animated?("sequence")([
                    a1 v1
                    a2 v2
                ] |> Array.ofList)

        // TODO trivially add `sequence` and higher arrity versions
