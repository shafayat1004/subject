[<AutoOpen>]
module Rn.Components.Button

open LibClient

open Rn.Helpers

open Fable.Core.JsInterop
open Browser.Types

module private ButtonRN =
    let unboxStyles (styles: array<Rn.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    let assignWebHandlers
            (props: obj)
            (onContextMenu: (MouseEvent -> unit) option)
            (onHoverStart: (PointerEvent -> unit) option)
            (onHoverEnd: (PointerEvent -> unit) option)
            (ariaControls: string option)
            : unit =
        #if EGGSHELL_PLATFORM_IS_WEB
        onContextMenu |> Option.iter (fun v -> props?onContextMenu <- v)
        // RNW Pressable uses onHoverIn/onHoverOut
        onHoverStart |> Option.iter (fun v -> props?onHoverIn <- v)
        onHoverEnd   |> Option.iter (fun v -> props?onHoverOut <- v)
        ariaControls |> Option.iter (fun v -> props?``aria-controls`` <- v)
        #endif
        ()

type Rn.Components.Constructors.Rn with
    static member Button(
        ?children:                     ReactChildrenProp,
        ?title:                        string,
        ?disabled:                     bool,
        ?disabledOpacity:              float,
        ?delayLongPress:               float,
        ?autoFocus:                    bool,
        ?onAccessibilityTapIOS:        Event -> unit,
        ?onContextMenu:                MouseEvent -> unit,
        ?onPress:                      PointerEvent -> unit,
        ?onPressIn:                    PointerEvent -> unit,
        ?onPressOut:                   PointerEvent -> unit,
        ?onLongPress:                  PointerEvent -> unit,
        ?onHoverStart:                 PointerEvent -> unit,
        ?onHoverEnd:                   PointerEvent -> unit,
        ?onKeyPress:                   KeyboardEvent -> unit,
        ?onFocus:                      FocusEvent -> unit,
        ?onBlur:                       FocusEvent -> unit,
        ?shouldRasterizeIOS:           bool,
        ?disableTouchOpacityAnimation: bool,
        ?activeOpacity:                float,
        ?underlayColor:                string,
        ?id:                           string,
        ?testId:                       string,
        ?accessibilityLabel:           string,
        ?accessibilityRole:            LibClient.Accessibility.AccessibilityRole,
        ?accessibilityState:           obj,
        ?accessibilityId:              string,
        ?importantForAccessibility:    LibClient.Accessibility.ImportantForAccessibility,
        ?accessibilityActions:         string array,
        ?onAccessibilityAction:        Event -> unit,
        ?ariaControls:                 string,
        ?styles:                       array<Rn.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:                List<Rn.LegacyStyles.RuntimeStyles>
    ) =
        // Rn-only props
        ignore (title, shouldRasterizeIOS, autoFocus, onAccessibilityTapIOS, accessibilityId, disabledOpacity)

        let __props = createEmpty

        __props?disabled       <- disabled
        __props?delayLongPress <- delayLongPress
        __props?onPress        <- onPress
        __props?onPressIn      <- onPressIn
        __props?onPressOut     <- onPressOut
        __props?onLongPress    <- onLongPress
        __props?onKeyPress     <- onKeyPress
        __props?onFocus        <- onFocus
        __props?onBlur         <- onBlur
        __props?nativeID       <- id
        __props?style          <- ButtonRN.unboxStyles styles

        Rn.RnPrimitives.assignTestId __props testId

        Rn.RnPrimitives.assignAccessibility
            __props
            accessibilityLabel
            (accessibilityRole |> Option.bind Rn.RnPrimitives.mapAccessibilityRole |> Option.map box)
            accessibilityState
            (Rn.RnPrimitives.mapImportantForAccessibility importantForAccessibility)
            None   // no liveRegion on Button
            accessibilityActions
            onAccessibilityAction
            None   // no ariaLabelledBy on Button
            None   // no ariaRoleDescription on Button
            None   // no tabIndex on Button

        Rn.RnPrimitives.assignPressableFeedback __props disableTouchOpacityAnimation activeOpacity underlayColor

        ButtonRN.assignWebHandlers __props onContextMenu onHoverStart onHoverEnd ariaControls

        match xLegacyStyles with
        | Option.None | Option.Some [] -> ()
        | Option.Some ls -> __props?__style <- ls

        Rn.RnPrimitives.createElement
            Rn.RnPrimitives.Pressable
            __props
            (ThirdParty.fixPotentiallySingleChild (Option.map tellReactArrayKeysAreOkay children |> Option.getOrElse [||]))
