[<AutoOpen>]
module ReactXP.Components.ActivityIndicator

open ReactXP.Helpers

open Fable.Core.JsInterop
open Fable.Core

[<StringEnum>]
type Size =
| Large
| Medium
| Small
| Tiny

module private ActivityIndicatorRN =
    let unboxStyles (styles: array<ReactXP.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    // RN ActivityIndicator only knows 'small' | 'large'; map ReactXP's 4-value enum down.
    let mapSize (size: Size option) : obj option =
        size |> Option.map (function
            | Large  -> box "large"
            | Medium -> box "large"
            | Small  -> box "small"
            | Tiny   -> box "small")

type ReactXP.Components.Constructors.RX with
    static member ActivityIndicator(
        color:          string,
        ?size:          Size,
        ?deferTime:     int,
        ?key:           string,
        ?styles:        array<ReactXP.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
    ) =
        // deferTime is ReactXP-only (deferred render); no RN equivalent
        ignore (deferTime, xLegacyStyles)

        let __props = createEmpty

        __props?color <- color
        __props?size  <- ActivityIndicatorRN.mapSize size
        __props?key   <- key
        __props?style <- ActivityIndicatorRN.unboxStyles styles

        ReactXP.RNSeam.createElement ReactXP.RNSeam.ActivityIndicator __props [||]
