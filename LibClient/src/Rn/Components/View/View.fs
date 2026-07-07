[<AutoOpen>]
module Rn.Components.View

open LibClient.JsInterop

open Rn.Helpers
open Rn.Types

open Fable.Core.JsInterop
open Browser.Types
open Fable.Core
open LibClient

[<StringEnum>]
type ViewLayerType =
| [<CompiledName("none")>]Non
| Software
| Hardware

type LimitFocusType =
| Unlimited  = 0
| Limited    = 1
| Accessible = 2

type FocusCandidate = {
    ``component``:   obj (* Rn.FocusableComponent *)
    accessibilityId: string option
}

type FocusArbitrator = ((* candidates *) List<FocusCandidate>) -> Option<FocusCandidate> (* Undefined for None *)

// XXX the TypeScript definitions use an enum without an explicit start
// value, which is flimsy... hopefully the implementation doesn't change.
type AccessibilityLiveRegion =
| None      = 0
| Polite    = 1
| Assertive = 2

type IViewRef = Fable.React.ReactElement

module private ViewRN =
    let mapLiveRegion (region: AccessibilityLiveRegion option) : obj option =
        region
        |> Option.map (
            function
            | AccessibilityLiveRegion.None -> box "none"
            | AccessibilityLiveRegion.Polite -> box "polite"
            | AccessibilityLiveRegion.Assertive -> box "assertive"
            | _ -> box "none"
        )

    let unboxStyles (styles: array<Rn.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    let assignWebHandlers
            (props: obj)
            (onMouseEnter: (MouseEvent -> unit) option)
            (onMouseLeave: (MouseEvent -> unit) option)
            (onDragStart: (DragEvent -> unit) option)
            (onDrag: (DragEvent -> unit) option)
            (onDragEnd: (DragEvent -> unit) option)
            (onDragEnter: (DragEvent -> unit) option)
            (onDragOver: (DragEvent -> unit) option)
            (onDragLeave: (DragEvent -> unit) option)
            (onDrop: (DragEvent -> unit) option)
            (onMouseOver: (MouseEvent -> unit) option)
            (onMouseMove: (MouseEvent -> unit) option)
            (onContextMenu: (MouseEvent -> unit) option)
            : unit =
        #if EGGSHELL_PLATFORM_IS_WEB
        onMouseEnter |> Option.iter (fun v -> props?onMouseEnter <- v)
        onMouseLeave |> Option.iter (fun v -> props?onMouseLeave <- v)
        onDragStart |> Option.iter (fun v -> props?onDragStart <- v)
        onDrag |> Option.iter (fun v -> props?onDrag <- v)
        onDragEnd |> Option.iter (fun v -> props?onDragEnd <- v)
        onDragEnter |> Option.iter (fun v -> props?onDragEnter <- v)
        onDragOver |> Option.iter (fun v -> props?onDragOver <- v)
        onDragLeave |> Option.iter (fun v -> props?onDragLeave <- v)
        onDrop |> Option.iter (fun v -> props?onDrop <- v)
        onMouseOver |> Option.iter (fun v -> props?onMouseOver <- v)
        onMouseMove |> Option.iter (fun v -> props?onMouseMove <- v)
        onContextMenu |> Option.iter (fun v -> props?onContextMenu <- v)
        #endif
        ()

type Rn.Components.Constructors.Rn with
    static member View(
        ?children:                         ReactChildrenProp,
        ?title:                            string,
        ?ignorePointerEvents:              bool,
        ?blockPointerEvents:               bool,
        ?shouldRasterizeIOS:               bool,
        ?viewLayerTypeAndroid:             ViewLayerType,
        ?restrictFocusWithin:              bool,
        ?limitFocusWithin:                 LimitFocusType,
        ?autoFocus:                        bool,
        ?arbitrateFocus:                   FocusArbitrator,
        ?importantForLayout:               bool,
        ?id:                               string,
        ?testId:                           string,
        ?accessibilityLabel:               string,
        ?accessibilityRole:                LibClient.Accessibility.AccessibilityRole,
        ?accessibilityState:               obj,
        ?accessibilityId:                  string,
        ?importantForAccessibility:        LibClient.Accessibility.ImportantForAccessibility,
        ?accessibilityActions:             string array,
        ?onAccessibilityAction:            Event -> unit,
        ?ariaLabelledBy:                   string,
        ?ariaRoleDescription:              string,
        ?accessibilityLiveRegion:          AccessibilityLiveRegion,
        ?animateChildEnter:                bool,
        ?animateChildLeave:                bool,
        ?animateChildMove:                 bool,
        ?onAccessibilityTapIOS:            Event -> unit,
        ?onLayout:                         ViewOnLayoutEvent -> unit,
        ?onMouseEnter:                     MouseEvent -> unit,
        ?onMouseLeave:                     MouseEvent -> unit,
        ?onDragStart:                      DragEvent -> unit,
        ?onDrag:                           DragEvent -> unit,
        ?onDragEnd:                        DragEvent -> unit,
        ?onDragEnter:                      DragEvent -> unit,
        ?onDragOver:                       DragEvent -> unit,
        ?onDragLeave:                      DragEvent -> unit,
        ?onDrop:                           DragEvent -> unit,
        ?onMouseOver:                      MouseEvent -> unit,
        ?onMouseMove:                      MouseEvent -> unit,
        ?onPress:                          PointerEvent -> unit,
        ?onLongPress:                      PointerEvent -> unit,
        ?onKeyPress:                       KeyboardEvent -> unit,
        ?onFocus:                          FocusEvent -> unit,
        ?onBlur:                           FocusEvent -> unit,
        ?disableTouchOpacityAnimation:     bool,
        ?activeOpacity:                    float,
        ?underlayColor:                    string,
        ?onContextMenu:                    MouseEvent -> unit,
        ?onStartShouldSetResponder:        Event -> bool,
        ?onMoveShouldSetResponder:         Event -> bool,
        ?onStartShouldSetResponderCapture: Event -> bool,
        ?onMoveShouldSetResponderCapture:  Event -> bool,
        ?onResponderGrant:                 Event -> unit,
        ?onResponderReject:                Event -> unit,
        ?onResponderRelease:               Event -> unit,
        ?onResponderStart:                 TouchEvent -> unit,
        ?onResponderMove:                  TouchEvent -> unit,
        ?onResponderEnd:                   TouchEvent -> unit,
        ?onResponderTerminate:             Event -> unit,
        ?onResponderTerminationRequest:    Event -> bool,
        ?tabIndex:                         int,
        ?useSafeInsets:                    bool,
        ?ref:                              LibClient.JsInterop.JsNullable<IViewRef> -> unit,
        ?key:                              string,
        ?styles:                           array<Rn.Styles.FSharpDialect.ViewStyles>
    ) =
        // Rn-only props (title, focus arbitration, child FLIP animation, safe insets) are
        // intentionally dropped on the RN/RNW path; callers keep the same F# signature.
        ignore (title, shouldRasterizeIOS, viewLayerTypeAndroid, restrictFocusWithin, limitFocusWithin)
        ignore (autoFocus, arbitrateFocus, importantForLayout, accessibilityId)
        ignore (animateChildEnter, animateChildLeave, animateChildMove, onAccessibilityTapIOS, useSafeInsets)

        let __props = createEmpty

        Rn.RnPrimitives.assignPointerEvents __props ignorePointerEvents blockPointerEvents
        Rn.RnPrimitives.assignTestId __props testId
        id |> Option.iter (fun v -> __props?nativeID <- v)
        Rn.RnPrimitives.assignOnLayout __props onLayout

        Rn.RnPrimitives.assignAccessibility
            __props
            accessibilityLabel
            (accessibilityRole |> Option.bind Rn.RnPrimitives.mapAccessibilityRole |> Option.map box)
            accessibilityState
            (Rn.RnPrimitives.mapImportantForAccessibility importantForAccessibility)
            (ViewRN.mapLiveRegion accessibilityLiveRegion)
            accessibilityActions
            onAccessibilityAction
            ariaLabelledBy
            ariaRoleDescription
            tabIndex

        ViewRN.assignWebHandlers
            __props
            onMouseEnter
            onMouseLeave
            onDragStart
            onDrag
            onDragEnd
            onDragEnter
            onDragOver
            onDragLeave
            onDrop
            onMouseOver
            onMouseMove
            onContextMenu

        onKeyPress |> Option.iter (fun v -> __props?onKeyPress <- v)
        onFocus |> Option.iter (fun v -> __props?onFocus <- v)
        onBlur |> Option.iter (fun v -> __props?onBlur <- v)

        onStartShouldSetResponder |> Option.iter (fun v -> __props?onStartShouldSetResponder <- v)
        onMoveShouldSetResponder |> Option.iter (fun v -> __props?onMoveShouldSetResponder <- v)
        onStartShouldSetResponderCapture |> Option.iter (fun v -> __props?onStartShouldSetResponderCapture <- v)
        onMoveShouldSetResponderCapture |> Option.iter (fun v -> __props?onMoveShouldSetResponderCapture <- v)
        onResponderGrant |> Option.iter (fun v -> __props?onResponderGrant <- v)
        onResponderReject |> Option.iter (fun v -> __props?onResponderReject <- v)
        onResponderRelease |> Option.iter (fun v -> __props?onResponderRelease <- v)
        onResponderStart |> Option.iter (fun v -> __props?onResponderStart <- v)
        onResponderMove |> Option.iter (fun v -> __props?onResponderMove <- v)
        onResponderEnd |> Option.iter (fun v -> __props?onResponderEnd <- v)
        onResponderTerminate |> Option.iter (fun v -> __props?onResponderTerminate <- v)
        onResponderTerminationRequest |> Option.iter (fun v -> __props?onResponderTerminationRequest <- v)

        __props?ref <- ref
        __props?key <- key
        __props?style <- ViewRN.unboxStyles styles

        let kids =
            ThirdParty.fixPotentiallySingleChild (
                Option.map tellReactArrayKeysAreOkay children |> Option.getOrElse [||]
            )

        let usePressable =
            onPress.IsSome
            || onLongPress.IsSome
            || underlayColor.IsSome
            || (activeOpacity.IsSome && activeOpacity <> Some 1.0)

        if usePressable then
            onPress |> Option.iter (fun v -> __props?onPress <- v)
            onLongPress |> Option.iter (fun v -> __props?onLongPress <- v)
            Rn.RnPrimitives.assignPressableFeedback __props disableTouchOpacityAnimation activeOpacity underlayColor
            Rn.RnPrimitives.createElement Rn.RnPrimitives.Pressable __props kids
        else
            Rn.RnPrimitives.createElement Rn.RnPrimitives.View __props kids
