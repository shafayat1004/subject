/// Phase 4 RNW seam: RN/RNW primitive imports + helpers. Web webpack aliases
/// react-native to react-native-web (Meta/LibFablePlus/webpack.config.js).
/// View is the reference primitive (ported); other primitives still use @chaldal/reactxp.
namespace ReactXP

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Browser.Types

module RNSeam =
    // --- Primitives (Probe A pattern from eggshell-rnw-spike) -------------------------
    let View : obj = import "View" "react-native"
    let Text : obj = import "Text" "react-native"
    let TextInput : obj = import "TextInput" "react-native"
    let ScrollView : obj = import "ScrollView" "react-native"
    let Image : obj = import "Image" "react-native"
    let Pressable : obj = import "Pressable" "react-native"
    let ActivityIndicator : obj = import "ActivityIndicator" "react-native"

    let PlatformModule : obj = import "Platform" "react-native"
    let PixelRatioModule : obj = import "PixelRatio" "react-native"

    // --- Styles -------------------------------------------------------------------
    // ReactXP's web Styles.createViewStyle expanded `flex: N` into explicit
    // flexGrow/flexShrink without setting flexBasis, so the element sized to
    // content (flexBasis defaults to auto). Passing `flex: N` as a plain CSS
    // shorthand maps to `flex N 1 0%` (flex-basis 0%), collapsing zero-flex
    // containers like the AppShell sidebar. We replicate the ReactXP expansion.
    [<Emit("""(function(r) {
      if (!r || r.flex == null) return r;
      var f = r.flex, o = Object.assign({}, r);
      delete o.flex;
      if (f > 0)      { o.flexGrow = f; o.flexShrink = 1; }
      else if (f < 0) { o.flexGrow = 0; o.flexShrink = -f; }
      else            { o.flexGrow = 0; o.flexShrink = 0; }
      return o;
    })($0)""")>]
    let createViewStyle (rules: obj) : obj = jsNative

    // ReactXP _adaptStyles converted numeric lineHeight to "Npx" strings so the browser
    // treats it as pixels, not as a CSS unitless multiplier (which would be N × font-size).
    // e.g. lineHeight: 24 without "px" = CSS line-height:24 = 24 × 16px = 384px (huge gap).
    [<Emit("""(function(r) {
      if (!r) return r;
      var o = Object.assign({}, r);
      if (o.flex != null) {
        var f = o.flex; delete o.flex;
        if (f > 0)      { o.flexGrow = f; o.flexShrink = 1; }
        else if (f < 0) { o.flexGrow = 0; o.flexShrink = -f; }
        else            { o.flexGrow = 0; o.flexShrink = 0; }
      }
      if (typeof o.lineHeight === 'number') { o.lineHeight = o.lineHeight + 'px'; }
      return o;
    })($0)""")>]
    let createTextStyle (rules: obj) : obj = jsNative

    let createAnimatedViewStyle (rules: obj) : obj = rules
    let createAnimatedTextStyle (rules: obj) : obj = rules
    let createAnimatedTextInputStyle (rules: obj) : obj = rules

    let inline createElement (comp: obj) (props: obj) (children: ReactElement array) : ReactElement =
        ReactBindings.React.createElement(comp, props, children)

    /// RN uses `testID`; EggShell call sites pass `testId`. Apply when wiring primitives.
    let assignTestId (props: obj) (testId: string option) : unit =
        testId |> Option.iter (fun id -> props?testID <- id)

    /// Convert EggShell's integer `AccessibilityRole` enum to the string RNW expects.
    /// Returns None for roles RNW explicitly ignores (imagebutton, keyboardkey, text).
    let mapAccessibilityRole (role: LibClient.Accessibility.AccessibilityRole) : string option =
        match role with
        | LibClient.Accessibility.AccessibilityRole.Alert        -> Some "alert"
        | LibClient.Accessibility.AccessibilityRole.Imagebutton  -> None
        | LibClient.Accessibility.AccessibilityRole.Keyboardkey  -> None
        | LibClient.Accessibility.AccessibilityRole.ProgressBar  -> Some "progressbar"
        | LibClient.Accessibility.AccessibilityRole.Radio        -> Some "radio"
        | LibClient.Accessibility.AccessibilityRole.RadioGroup   -> Some "radiogroup"
        | LibClient.Accessibility.AccessibilityRole.ScrollBar    -> Some "scrollbar"
        | LibClient.Accessibility.AccessibilityRole.SpinButton   -> Some "spinbutton"
        | LibClient.Accessibility.AccessibilityRole.Timer        -> Some "timer"
        | LibClient.Accessibility.AccessibilityRole.ToggleButton -> Some "button"
        | LibClient.Accessibility.AccessibilityRole.Toolbar      -> Some "toolbar"
        | LibClient.Accessibility.AccessibilityRole.Summary      -> Some "summary"
        | LibClient.Accessibility.AccessibilityRole.Adjustable   -> Some "adjustable"
        | LibClient.Accessibility.AccessibilityRole.Button       -> Some "button"
        | LibClient.Accessibility.AccessibilityRole.Tab          -> Some "tab"
        | LibClient.Accessibility.AccessibilityRole.Link         -> Some "link"
        // "banner" → propsToAccessibilityComponent renders as <header> (correct site-level
        // landmark); "header" → ARIA "heading" → <h1> (wrong, causes 2em font-size inheritance).
        | LibClient.Accessibility.AccessibilityRole.Header       -> Some "banner"
        | LibClient.Accessibility.AccessibilityRole.Search       -> Some "search"
        | LibClient.Accessibility.AccessibilityRole.Image        -> Some "image"
        | LibClient.Accessibility.AccessibilityRole.Text         -> None
        | LibClient.Accessibility.AccessibilityRole.Menu         -> Some "menu"
        | LibClient.Accessibility.AccessibilityRole.MenuItem     -> Some "menuitem"
        | LibClient.Accessibility.AccessibilityRole.MenuBar      -> Some "menubar"
        | LibClient.Accessibility.AccessibilityRole.TabList      -> Some "tablist"
        | LibClient.Accessibility.AccessibilityRole.List         -> Some "list"
        | LibClient.Accessibility.AccessibilityRole.ListItem     -> Some "listitem"
        | LibClient.Accessibility.AccessibilityRole.ListBox      -> Some "listbox"
        | LibClient.Accessibility.AccessibilityRole.Group        -> Some "group"
        | LibClient.Accessibility.AccessibilityRole.CheckBox     -> Some "checkbox"
        | LibClient.Accessibility.AccessibilityRole.Checked      -> Some "checkbox"
        | LibClient.Accessibility.AccessibilityRole.ComboBox     -> Some "combobox"
        | LibClient.Accessibility.AccessibilityRole.Log          -> Some "log"
        | LibClient.Accessibility.AccessibilityRole.Status       -> Some "status"
        | LibClient.Accessibility.AccessibilityRole.Dialog       -> Some "dialog"
        | LibClient.Accessibility.AccessibilityRole.HasPopup     -> Some "button"
        | LibClient.Accessibility.AccessibilityRole.Option       -> Some "option"
        | LibClient.Accessibility.AccessibilityRole.Switch       -> Some "switch"
        | LibClient.Accessibility.AccessibilityRole.None         -> Some "none"
        | _                                                      -> None

    let mapImportantForAccessibility (v: LibClient.Accessibility.ImportantForAccessibility option) : obj option =
        v |> Option.map (function
            | LibClient.Accessibility.ImportantForAccessibility.Auto              -> box "auto"
            | LibClient.Accessibility.ImportantForAccessibility.Yes               -> box "yes"
            | LibClient.Accessibility.ImportantForAccessibility.No                -> box "no"
            | LibClient.Accessibility.ImportantForAccessibility.NoHideDescendants -> box "no-hide-descendants"
            | _                                                                   -> box "auto")

    let assignAccessibility
            (props: obj)
            (accessibilityLabel: string option)
            (accessibilityRole: obj option)
            (accessibilityState: obj option)
            (importantForAccessibility: obj option)
            (accessibilityLiveRegion: obj option)
            (accessibilityActions: string array option)
            (onAccessibilityAction: (Event -> unit) option)
            (ariaLabelledBy: string option)
            (ariaRoleDescription: string option)
            (tabIndex: int option)
            : unit =
        accessibilityLabel |> Option.iter (fun v -> props?accessibilityLabel <- v)
        accessibilityRole |> Option.iter (fun v -> props?accessibilityRole <- v)
        accessibilityState |> Option.iter (fun v -> props?accessibilityState <- v)
        importantForAccessibility |> Option.iter (fun v -> props?importantForAccessibility <- v)
        accessibilityLiveRegion |> Option.iter (fun v -> props?accessibilityLiveRegion <- v)
        accessibilityActions |> Option.iter (fun v -> props?accessibilityActions <- v)
        onAccessibilityAction |> Option.iter (fun v -> props?onAccessibilityAction <- v)
        ariaLabelledBy |> Option.iter (fun v -> props?``aria-labelledby`` <- v)
        ariaRoleDescription |> Option.iter (fun v -> props?``aria-roledescription`` <- v)
        tabIndex |> Option.iter (fun v -> props?tabIndex <- v)

    /// Unbox opaque `ViewStyles` / `ScrollViewStyles` wrappers for RN `style` prop.
    let toRNStyleArray (styles: array<obj> option) : obj option =
        styles |> Option.map (fun arr -> arr |> Array.map id |> box)

    let assignPointerEvents (props: obj) (ignorePointerEvents: bool option) (blockPointerEvents: bool option) : unit =
        match ignorePointerEvents with
        | Some true -> props?pointerEvents <- "none"
        | _ ->
            match blockPointerEvents with
            | Some true -> props?pointerEvents <- "box-none"
            | _ -> ()

    let assignPressableFeedback
            (props: obj)
            (disableTouchOpacityAnimation: bool option)
            (activeOpacity: float option)
            (underlayColor: string option)
            : unit =
        let disableAnim = disableTouchOpacityAnimation |> Option.defaultValue true
        if not disableAnim then
            activeOpacity |> Option.iter (fun v -> props?activeOpacity <- v)
        underlayColor |> Option.iter (fun v -> props?underlayColor <- v)

module RNPlatform =
    open ReactXP

    let private detectWebOs () : OS =
        let isWindows: bool = import "windows" "platform-detect"
        let isLinux:   bool = import "linux"   "platform-detect"
        let isMac:     bool = import "macos"   "platform-detect"
        let isAndroid: bool = import "android" "platform-detect"
        let isIOS:     bool = import "ios"     "platform-detect"

        match (isWindows, isLinux, isMac, isAndroid, isIOS) with
        | (true, _, _, _, _) -> OS.Windows
        | (_, true, _, _, _) -> OS.Linux
        | (_, _, true, _, _) -> OS.Mac
        | (_, _, _, true, _) -> OS.Android
        | (_, _, _, _, true) -> OS.IOS
        | _                  -> OS.Other

    /// Drop-in replacement for `ReactXP.Runtime.platform` once the seam is flipped.
    let platform () : Platform =
        #if EGGSHELL_PLATFORM_IS_WEB
        Web (detectWebOs ())
        #else
        match RNSeam.PlatformModule?OS with
        | "android" -> Native NativePlatform.Android
        | "ios"     -> Native NativePlatform.IOS
        | other     -> failwithf "Unsupported react-native Platform.OS: %s" other
        #endif

    let pixelDensity () : float =
        RNSeam.PixelRatioModule?get() |> float
