[<AutoOpen>]
module Rn.Styles.FSharpDialect

open Fable.Core.JsInterop

[<RequireQualifiedAccess>]
type ScrollViewStyles =
| InternalRepresentationIsForJsRuntimeConsumptionOnly

[<RequireQualifiedAccess>]
type ViewStyles =
| InternalRepresentationIsForJsRuntimeConsumptionOnly

[<RequireQualifiedAccess>]
type AnimatableViewStyles =
| InternalRepresentationIsForJsRuntimeConsumptionOnly

[<RequireQualifiedAccess>]
type TextStyles =
| InternalRepresentationIsForJsRuntimeConsumptionOnly

[<RequireQualifiedAccess>]
type AnimatableTextStyles =
| InternalRepresentationIsForJsRuntimeConsumptionOnly

// TODO: we should also have TextInputStyles rather than re-using ViewStyles, since the latter does
// not permit some rules relevant to TextInput (fontSize and color)
[<RequireQualifiedAccess>]
type AnimatableTextInputStyles =
| InternalRepresentationIsForJsRuntimeConsumptionOnly

let legacyTheme (value: Rn.LegacyStyles.Styles) : List<Rn.LegacyStyles.RuntimeStyles> =
    value
    |> Rn.LegacyStyles.Designtime.processStyles
    |> List.ofOneItem

type ViewStylesBuilder() =
    member _.Zero () : seq<RawRnStyleRule> =
        Seq.empty

    member _.Yield (rule: RawRnViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexChildrenStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexChildrenStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnTransformStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnTransformStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Combine (a: seq<RawRnStyleRule>, b: seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a b

    member _.Combine (a: seq<RawRnStyleRule>, bf: unit -> seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a (bf())

    member _.Delay (expr: unit -> seq<RawRnStyleRule>) : unit -> seq<RawRnStyleRule> = expr

    member _.Run (f: unit -> seq<RawRnStyleRule>) : ViewStyles =
        !!(!!(f()) |> createObj |> Rn.RnPrimitives.createViewStyle)

let makeViewStyles = ViewStylesBuilder()

type AnimatableViewStylesBuilder() =
    member _.Zero () : seq<RawRnStyleRule> =
        Seq.empty

    member _.Yield (rule: RawRnViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexChildrenStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexChildrenStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedTransformStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedTransformStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Combine (a: seq<RawRnStyleRule>, b: seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a b

    member _.Combine (a: seq<RawRnStyleRule>, bf: unit -> seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a (bf())

    member _.Delay (expr: unit -> seq<RawRnStyleRule>) : unit -> seq<RawRnStyleRule> = expr

    member _.Run (f: unit -> seq<RawRnStyleRule>) : AnimatableViewStyles =
        !!(!!(f()) |> createObj |> Rn.RnPrimitives.createAnimatedViewStyle)

let makeAnimatableViewStyles = AnimatableViewStylesBuilder()

type ScrollViewStylesBuilder() =
    member _.Zero () : seq<RawRnStyleRule> =
        Seq.empty

    member _.Yield (rule: RawRnViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Combine (a: seq<RawRnStyleRule>, b: seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a b

    member _.Combine (a: seq<RawRnStyleRule>, bf: unit -> seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a (bf())

    member _.Delay (expr: unit -> seq<RawRnStyleRule>) : unit -> seq<RawRnStyleRule> = expr

    member _.Run (f: unit -> seq<RawRnStyleRule>) : ScrollViewStyles =
        !!(!!(f()) |> createObj |> Rn.RnPrimitives.createViewStyle)

let makeScrollViewStyles = ScrollViewStylesBuilder()

type TextStylesBuilder() =
    member _.Zero () : seq<RawRnStyleRule> =
        Seq.empty

    member _.Yield (rule: RawRnTextStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rule: RawRnViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexChildrenStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexChildrenStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnTransformStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnTransformStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Combine (a: seq<RawRnStyleRule>, b: seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a b

    member _.Combine (a: seq<RawRnStyleRule>, bf: unit -> seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a (bf())

    member _.Delay (expr: unit -> seq<RawRnStyleRule>) : unit -> seq<RawRnStyleRule> = expr

    member _.Run (f: unit -> seq<RawRnStyleRule>) : TextStyles =
        !!(!!(f()) |> createObj |> Rn.RnPrimitives.createTextStyle)

let makeTextStyles = TextStylesBuilder()

type AnimatableTextStylesBuilder() =
    member _.Zero () : seq<RawRnStyleRule> =
        Seq.empty

    member _.Yield (rule: RawRnTextStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnTextStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexChildrenStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexChildrenStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnTransformStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnTransformStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedTextStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedTextStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedTransformStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedTransformStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Combine (a: seq<RawRnStyleRule>, b: seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a b

    member _.Combine (a: seq<RawRnStyleRule>, bf: unit -> seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a (bf())

    member _.Delay (expr: unit -> seq<RawRnStyleRule>) : unit -> seq<RawRnStyleRule> = expr

    member _.Run (f: unit -> seq<RawRnStyleRule>) : AnimatableTextStyles =
        !!(!!(f()) |> createObj |> Rn.RnPrimitives.createAnimatedTextStyle)

let makeAnimatableTextStyles = AnimatableTextStylesBuilder()

type AnimatableTextInputStylesBuilder() =
    member _.Zero () : seq<RawRnStyleRule> =
        Seq.empty

    member _.Yield (rule: RawRnTextStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnTextStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedTextStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedTextStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnFlexChildrenStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnFlexChildrenStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedViewStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedViewStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedFlexStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedFlexStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Yield (rule: RawRnAnimatedTransformStyleRule) : seq<RawRnStyleRule> =
        Seq.ofOneItem !!rule

    member _.Yield (rules: array<RawRnAnimatedTransformStyleRule>) : seq<RawRnStyleRule> =
        Seq.ofArray !!rules

    member _.Combine (a: seq<RawRnStyleRule>, b: seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a b

    member _.Combine (a: seq<RawRnStyleRule>, bf: unit -> seq<RawRnStyleRule>) : seq<RawRnStyleRule> =
        Seq.append a (bf())

    member _.Delay (expr: unit -> seq<RawRnStyleRule>) : unit -> seq<RawRnStyleRule> = expr

    member _.Run (f: unit -> seq<RawRnStyleRule>) : AnimatableTextInputStyles =
        !!(!!(f()) |> createObj |> Rn.RnPrimitives.createAnimatedTextInputStyle)

let makeAnimatableTextInputStyles = AnimatableTextInputStylesBuilder()

open LibClient.Memoize

type ViewStyles with
    static member Memoize (fn: 'a -> ViewStyles) : 'a -> ViewStyles =
        memoize fn

    static member Memoize (fn: 'a -> 'b -> ViewStyles) : 'a -> 'b -> ViewStyles =
        memoize2 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> ViewStyles) : 'a -> 'b -> 'c -> ViewStyles =
        memoize3 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> ViewStyles) : 'a -> 'b -> 'c -> 'd -> ViewStyles =
        memoize4 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> 'e -> ViewStyles) : 'a -> 'b -> 'c -> 'd -> 'e -> ViewStyles =
        memoize5 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> ViewStyles) : 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> ViewStyles =
        memoize6 fn

type ScrollViewStyles with
    static member Memoize (fn: 'a -> ScrollViewStyles) : 'a -> ScrollViewStyles =
        memoize fn

    static member Memoize (fn: 'a -> 'b -> ScrollViewStyles) : 'a -> 'b -> ScrollViewStyles =
        memoize2 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> ScrollViewStyles) : 'a -> 'b -> 'c -> ScrollViewStyles =
        memoize3 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> ScrollViewStyles) : 'a -> 'b -> 'c -> 'd -> ScrollViewStyles =
        memoize4 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> 'e -> ScrollViewStyles) : 'a -> 'b -> 'c -> 'd -> 'e -> ScrollViewStyles =
        memoize5 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> ScrollViewStyles) : 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> ScrollViewStyles =
        memoize6 fn

type TextStyles with
    static member Memoize (fn: 'a -> TextStyles) : 'a -> TextStyles =
        memoize fn

    static member Memoize (fn: 'a -> 'b -> TextStyles) : 'a -> 'b -> TextStyles =
        memoize2 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> TextStyles) : 'a -> 'b -> 'c -> TextStyles =
        memoize3 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> TextStyles) : 'a -> 'b -> 'c -> 'd -> TextStyles =
        memoize4 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> 'e -> TextStyles) : 'a -> 'b -> 'c -> 'd -> 'e -> TextStyles =
        memoize5 fn

    static member Memoize (fn: 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> TextStyles) : 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> TextStyles =
        memoize6 fn

