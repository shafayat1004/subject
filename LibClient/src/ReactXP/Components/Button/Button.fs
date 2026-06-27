[<AutoOpen>]
module ReactXP.Components.Button

open LibClient

open ReactXP.Helpers

open Fable.Core.JsInterop
open Browser.Types

type ReactXP.Components.Constructors.RX with
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
        ?styles:                       array<ReactXP.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:                List<ReactXP.LegacyStyles.RuntimeStyles>
    ) =
        let __props = createEmpty
        __props?title                        <- title
        __props?disabled                     <- disabled
        __props?disabledOpacity              <- disabledOpacity
        __props?delayLongPress               <- delayLongPress
        __props?autoFocus                    <- autoFocus
        __props?onAccessibilityTapIOS        <- onAccessibilityTapIOS
        __props?onContextMenu                <- onContextMenu
        __props?onPress                      <- onPress
        __props?onPressIn                    <- onPressIn
        __props?onPressOut                   <- onPressOut
        __props?onLongPress                  <- onLongPress
        __props?onHoverStart                 <- onHoverStart
        __props?onHoverEnd                   <- onHoverEnd
        __props?onKeyPress                   <- onKeyPress
        __props?onFocus                      <- onFocus
        __props?onBlur                       <- onBlur
        __props?shouldRasterizeIOS           <- shouldRasterizeIOS
        __props?disableTouchOpacityAnimation <- disableTouchOpacityAnimation
        __props?activeOpacity                <- activeOpacity
        __props?underlayColor                <- underlayColor
        __props?id                           <- id
        __props?testId                       <- testId
        __props?accessibilityLabel           <- accessibilityLabel
        __props?accessibilityRole            <- accessibilityRole
        __props?accessibilityState           <- accessibilityState
        __props?accessibilityId              <- accessibilityId
        __props?importantForAccessibility    <- importantForAccessibility
        __props?accessibilityActions         <- accessibilityActions
        __props?onAccessibilityAction        <- onAccessibilityAction
        __props?ariaControls                 <- ariaControls
        __props?style                        <- styles

        match xLegacyStyles with
        | Option.None | Option.Some [] -> ()
        | Option.Some styles -> __props?__style <- styles

        Fable.React.ReactBindings.React.createElement(
            ReactXPRaw?Button,
            __props,
            ThirdParty.fixPotentiallySingleChild (Option.map tellReactArrayKeysAreOkay children |> Option.getOrElse [||])
        )
