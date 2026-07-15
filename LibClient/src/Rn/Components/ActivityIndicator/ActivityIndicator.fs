[<AutoOpen>]
module Rn.Components.ActivityIndicator

open Rn.Helpers

open Fable.Core.JsInterop
open Fable.Core

[<StringEnum>]
type Size =
| Large
| Medium
| Small
| Tiny

module private ActivityIndicatorRN =
    let unboxStyles (styles: array<Rn.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    // RN ActivityIndicator only knows 'small' | 'large'; map Rn's 4-value enum down.
    let mapSize (size: Size option) : obj option =
        size |> Option.map (function
            | Large  -> box "large"
            | Medium -> box "large"
            | Small  -> box "small"
            | Tiny   -> box "small")

type Rn.Components.Constructors.Rn with
    static member ActivityIndicator(
        color:          string,
        ?size:          Size,
        ?deferTime:     int,
        ?key:           string,
        ?styles:        array<Rn.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
    ) =
        // deferTime is Rn-only (deferred render); no RN equivalent
        ignore (deferTime, xLegacyStyles)

        let __props = createEmpty

        __props?color <- color
        __props?size  <- ActivityIndicatorRN.mapSize size
        __props?key   <- key
        __props?style <- ActivityIndicatorRN.unboxStyles styles

        Rn.RnPrimitives.createElement Rn.RnPrimitives.ActivityIndicator __props [||]
