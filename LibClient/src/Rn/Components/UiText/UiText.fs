[<AutoOpen>]
module Rn.Components.UiText

open LibClient.JsInterop

open Rn.Helpers

open Fable.Core.JsInterop
open Fable.React
open Fable.Core
open Browser.Types
open LibClient

module private UiTextRN =
    // ImportantForAccessibility is in scope from Text.fs ([<AutoOpen>], compiled before UiText.fs)
    let mapImportantForAccessibility (v: ImportantForAccessibility option) : obj option =
        v |> Option.map (function
            | ImportantForAccessibility.Auto              -> box "auto"
            | ImportantForAccessibility.Yes               -> box "yes"
            | ImportantForAccessibility.No                -> box "no"
            | ImportantForAccessibility.NoHideDescendants -> box "no-hide-descendants"
            | _                                           -> box "auto")

    let unboxStyles (styles: array<Rn.Styles.FSharpDialect.TextStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    let assignWebHandlers (props: obj) (onContextMenu: (MouseEvent -> unit) option) : unit =
        #if EGGSHELL_PLATFORM_IS_WEB
        onContextMenu |> Option.iter (fun v -> props?onContextMenu <- v)
        #endif
        ()

type Rn.Components.Constructors.Rn with
    static member UiText(
        value:                      string,
        ?selectable:                bool,
        ?numberOfLines:             int,
        ?allowFontScaling:          bool,
        ?maxContentSizeMultiplier:  float,
        ?ellipsizeMode:             EllipsizeMode,
        ?textBreakStrategy:         TextBreakStrategy,
        ?importantForAccessibility: ImportantForAccessibility,
        ?accessibilityId:           string,
        ?autoFocus:                 bool,
        ?onPress:                   PointerEvent -> unit,
        ?id:                        string,
        ?onContextMenu:             MouseEvent -> unit,
        ?key:                       string,
        ?xLegacyStyles:             List<Rn.LegacyStyles.RuntimeStyles>,
        ?styles:                    array<Rn.Styles.FSharpDialect.TextStyles>
    ) =
        Rn.Components.Constructors.Rn.UiText(
            children                   = [|Fable.React.Helpers.str value|],
            ?selectable                = selectable,
            ?numberOfLines             = numberOfLines,
            ?allowFontScaling          = allowFontScaling,
            ?maxContentSizeMultiplier  = maxContentSizeMultiplier,
            ?ellipsizeMode             = ellipsizeMode,
            ?textBreakStrategy         = textBreakStrategy,
            ?importantForAccessibility = importantForAccessibility,
            ?accessibilityId           = accessibilityId,
            ?autoFocus                 = autoFocus,
            ?onPress                   = onPress,
            ?id                        = id,
            ?onContextMenu             = onContextMenu,
            ?key                       = key,
            ?xLegacyStyles             = xLegacyStyles,
            ?styles                    = styles
        )

    static member UiText(
        ?children:                  ReactChildrenProp,
        ?selectable:                bool,
        ?numberOfLines:             int,
        ?allowFontScaling:          bool,
        ?maxContentSizeMultiplier:  float,
        ?ellipsizeMode:             EllipsizeMode,
        ?textBreakStrategy:         TextBreakStrategy,
        ?importantForAccessibility: ImportantForAccessibility,
        ?accessibilityId:           string,
        ?autoFocus:                 bool,
        ?onPress:                   PointerEvent -> unit,
        ?id:                        string,
        ?onContextMenu:             MouseEvent -> unit,
        ?key:                       string,
        ?xLegacyStyles:             List<Rn.LegacyStyles.RuntimeStyles>,
        ?styles:                    array<Rn.Styles.FSharpDialect.TextStyles>
    ) =
        ignore (accessibilityId, autoFocus)

        let __props = createEmpty

        __props?selectable               <- selectable |> Option.orElse (Some false)
        __props?numberOfLines            <- numberOfLines
        __props?allowFontScaling         <- allowFontScaling
        __props?maxContentSizeMultiplier <- maxContentSizeMultiplier
        __props?ellipsizeMode            <- ellipsizeMode
        __props?textBreakStrategy        <- textBreakStrategy
        __props?onPress                  <- onPress
        __props?nativeID                 <- id
        __props?key                      <- key
        __props?style                    <- UiTextRN.unboxStyles styles

        UiTextRN.mapImportantForAccessibility importantForAccessibility
        |> Option.iter (fun v -> __props?importantForAccessibility <- v)

        UiTextRN.assignWebHandlers __props onContextMenu

        match xLegacyStyles with
        | Option.None | Option.Some [] -> ()
        | Option.Some ls -> __props?__style <- ls

        Rn.RnPrimitives.createElement
            Rn.RnPrimitives.Text
            __props
            (ThirdParty.fixPotentiallySingleChild (Option.map tellReactArrayKeysAreOkay children |> Option.getOrElse [||]))
