[<AutoOpen>]
module Rn.Components.Link

open Rn.Helpers

open Fable.Core.JsInterop
open Browser.Types
open LibClient

module private LinkRN =
    let unboxStyles (styles: array<Rn.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    let assignWebHandlers
            (props: obj)
            (onHoverStart:  (Event -> unit) option)
            (onHoverEnd:    (Event -> unit) option)
            (onContextMenu: (MouseEvent -> unit) option)
            : unit =
        #if EGGSHELL_PLATFORM_IS_WEB
        onHoverStart  |> Option.iter (fun v -> props?onMouseEnter <- v)
        onHoverEnd    |> Option.iter (fun v -> props?onMouseLeave <- v)
        onContextMenu |> Option.iter (fun v -> props?onContextMenu <- v)
        #endif
        ()

type Rn.Components.Constructors.Rn with
    static member Link(
        url:                       string,
        ?children:                 ReactChildrenProp,
        ?title:                    string,
        ?selectable:               bool,
        ?numberOfLines:            float,
        ?allowFontScaling:         bool,
        ?maxContentSizeMultiplier: float,
        ?tabIndex:                 int,
        ?accessibilityId:          string,
        ?autoFocus:                bool,
        ?onPress:                  (Event -> string -> unit),
        ?onLongPress:              (Event -> string -> unit),
        ?onHoverStart:             (Event -> unit),
        ?onHoverEnd:               (Event -> unit),
        ?onContextMenu:            (MouseEvent -> unit),
        ?styles:                   array<Rn.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:            List<Rn.LegacyStyles.RuntimeStyles>
    ) =
        // Text-layout props (selectable, numberOfLines, allowFontScaling, maxContentSizeMultiplier)
        // and Rn-only props are dropped on the RN/RNW path; callers keep the same F# signature.
        ignore (selectable, numberOfLines, allowFontScaling, maxContentSizeMultiplier)
        ignore (accessibilityId, autoFocus)

        let __props = createEmpty

        // href on web renders as <a href> via RNW, giving keyboard activation + :focus-visible free
        #if EGGSHELL_PLATFORM_IS_WEB
        __props?href <- url
        #endif

        Rn.RnPrimitives.assignTestId __props (title |> Option.orElse (Some url))
        __props?tabIndex <- tabIndex

        // role="link" baked in; no need to map through AccessibilityRole DU
        Rn.RnPrimitives.assignAccessibility
            __props
            (title |> Option.orElse (Some url))
            (Some (box "link"))
            None None None None None None None
            tabIndex

        // onPress/onLongPress: Rn passes (event, url); Pressable passes only the event
        onPress     |> Option.iter (fun handler -> __props?onPress     <- (fun (e: obj) -> handler (unbox e) url))
        onLongPress |> Option.iter (fun handler -> __props?onLongPress <- (fun (e: obj) -> handler (unbox e) url))

        LinkRN.assignWebHandlers __props onHoverStart onHoverEnd onContextMenu

        __props?style <- LinkRN.unboxStyles styles

        match xLegacyStyles with
        | Option.None | Option.Some [] -> ()
        | Option.Some ls               -> __props?__style <- ls

        Rn.RnPrimitives.createElement
            Rn.RnPrimitives.Pressable
            __props
            (ThirdParty.fixPotentiallySingleChild (Option.map tellReactArrayKeysAreOkay children |> Option.getOrElse [||]))
