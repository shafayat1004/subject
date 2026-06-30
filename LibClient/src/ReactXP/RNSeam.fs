/// Phase 4 RNW seam: RN/RNW primitive imports + helpers. Web webpack aliases
/// react-native to react-native-web (Meta/LibFablePlus/webpack.config.js).
/// View is the reference primitive (ported); other primitives still use @chaldal/reactxp.
namespace ReactXP

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Browser.Types

type NativePlatform =
    | Android
    | IOS

type OS =
    | Windows
    | Linux
    | Mac
    | Android
    | IOS
    | Other

type Platform =
    | Web of OS
    | Native of NativePlatform

module RNSeam =
    // --- Primitives (Probe A pattern from eggshell-rnw-spike) -------------------------
    let View: obj = import "View" "react-native"
    let Text: obj = import "Text" "react-native"
    let TextInput: obj = import "TextInput" "react-native"
    let ScrollView: obj = import "ScrollView" "react-native"
    let Image: obj = import "Image" "react-native"
    let Pressable: obj = import "Pressable" "react-native"
    let ActivityIndicator: obj = import "ActivityIndicator" "react-native"
    let Animated: obj = import "Animated" "react-native"
    let Easing: obj = import "Easing" "react-native"

    let PlatformModule: obj = import "Platform" "react-native"
    let PixelRatioModule: obj = import "PixelRatio" "react-native"
    let DimensionsModule: obj = import "Dimensions" "react-native"
    let KeyboardModule: obj = import "Keyboard" "react-native"
    let LinkingModule: obj = import "Linking" "react-native"
    let ClipboardModule: obj = import "Clipboard" "react-native"
    let AccessibilityInfoModule: obj = import "AccessibilityInfo" "react-native"
    let Picker: obj = import "Picker" "@react-native-picker/picker"
    let FlatList: obj = import "FlatList" "react-native"

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
        ReactBindings.React.createElement (comp, props, children)

    /// RN uses `testID`; EggShell call sites pass `testId`. Apply when wiring primitives.
    let assignTestId (props: obj) (testId: string option) : unit =
        testId |> Option.iter (fun id -> props?testID <- id)

    // react-native(-web) delivers layout as `{ nativeEvent: { layout: { x, y, width, height } } }`,
    // whereas @chaldal/reactxp flattened those fields directly onto the event. EggShell's
    // `ViewOnLayoutEvent` and every onLayout consumer (With.Layout, Responsive.screenSizeOnLayout,
    // ScrollView) read the flat shape, so forwarding RN's event verbatim left `e.width`/`e.height`
    // undefined -- which coerced to 0 and silently collapsed every measurement (the handheld
    // sidebar drawer width, screen-size detection, etc.). Adapt RN's event to the flat shape.
    [<Emit("(function(e){ if(!e||!e.nativeEvent||!e.nativeEvent.layout){return;} var l=e.nativeEvent.layout; $0({x:l.x,y:l.y,width:l.width,height:l.height}); })")>]
    let private wrapOnLayout (callback: ReactXP.Types.ViewOnLayoutEvent -> unit) : obj = jsNative

    /// Wire an EggShell onLayout handler onto an RN/RNW primitive's props, adapting the native
    /// event shape to the flat `ViewOnLayoutEvent` every consumer expects.
    let assignOnLayout (props: obj) (maybeOnLayout: Option<ReactXP.Types.ViewOnLayoutEvent -> unit>) : unit =
        maybeOnLayout
        |> Option.iter (fun callback -> props?onLayout <- wrapOnLayout callback)

    /// RN onScroll event: `{ nativeEvent: { contentOffset: { x, y } } }`.
    /// EggShell's ScrollView expects `(int * int) -> unit`. Public so other seam
    /// components (e.g. wrapped lists) can reuse the adapter.
    let wrapOnScroll (f: (int * int -> unit) option) : obj option =
        f
        |> Option.map (fun handler ->
            box (fun (e: obj) ->
                let x = e?nativeEvent?contentOffset?x |> int
                let y = e?nativeEvent?contentOffset?y |> int
                handler (x, y)))

    /// Convert EggShell's integer `AccessibilityRole` enum to the string RNW expects.
    /// Delegates to the canonical mapping in LibClient.AccessibilityHelpers so we have a single source of truth.
    let mapAccessibilityRole (role: LibClient.Accessibility.AccessibilityRole) : string option =
        LibClient.AccessibilityHelpers.mapRoleToString role

    let mapImportantForAccessibility (v: LibClient.Accessibility.ImportantForAccessibility option) : obj option =
        v
        |> Option.map (function
            | LibClient.Accessibility.ImportantForAccessibility.Auto -> box "auto"
            | LibClient.Accessibility.ImportantForAccessibility.Yes -> box "yes"
            | LibClient.Accessibility.ImportantForAccessibility.No -> box "no"
            | LibClient.Accessibility.ImportantForAccessibility.NoHideDescendants -> box "no-hide-descendants"
            | _ -> box "auto")

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
        accessibilityLabel
        |> Option.iter (fun v ->
            props?accessibilityLabel <- v
            props?``aria-label`` <- v)

        accessibilityRole
        |> Option.iter (fun v ->
            props?accessibilityRole <- v
            props?role <- v)

        accessibilityState |> Option.iter (fun v ->
            props?accessibilityState <- v
            let inline readBool (key: string) =
                if isNull v then None
                else
                    let lowerKey = key.ToLower()
                    let lower = v?(lowerKey)
                    if not (isNull lower) then Some (!!lower : bool)
                    else
                        let pascalKey = key.Substring(0, 1).ToUpper() + key.Substring(1)
                        let raw = v?(pascalKey)
                        if isNull raw then None else Some (!!raw : bool)
            match readBool "selected" with | Some b -> props?``aria-selected`` <- b | None -> ()
            match readBool "checked"  with | Some b -> props?``aria-checked`` <- b | None -> ()
            match readBool "expanded" with | Some b -> props?``aria-expanded`` <- b | None -> ()
            match readBool "disabled" with | Some b -> props?``aria-disabled`` <- b | None -> ()
            match readBool "busy"     with | Some b -> props?``aria-busy`` <- b | None -> ()
        )

        importantForAccessibility
        |> Option.iter (fun v -> props?importantForAccessibility <- v)

        accessibilityLiveRegion
        |> Option.iter (fun v -> props?accessibilityLiveRegion <- v)

        accessibilityActions |> Option.iter (fun v -> props?accessibilityActions <- v)
        onAccessibilityAction |> Option.iter (fun v -> props?onAccessibilityAction <- v)
        ariaLabelledBy |> Option.iter (fun v -> props?``aria-labelledby`` <- v)

        ariaRoleDescription
        |> Option.iter (fun v -> props?``aria-roledescription`` <- v)

        tabIndex |> Option.iter (fun v -> props?tabIndex <- v)

    /// Convenience helper that wires testID, nativeID, and accessibility/automation props in one
    /// place. Callers still pass `accessibilityRole`/`importantForAccessibility` as EggShell types;
    /// the helper maps them to RN/RNW strings.
    let assignA11yAndAutomation
        (props: obj)
        (testId: string option)
        (accessibilityId: string option)
        (accessibilityLabel: string option)
        (accessibilityRole: LibClient.Accessibility.AccessibilityRole option)
        (accessibilityState: obj option)
        (importantForAccessibility: LibClient.Accessibility.ImportantForAccessibility option)
        (accessibilityLiveRegion: string option)
        (accessibilityActions: string array option)
        (onAccessibilityAction: (Event -> unit) option)
        (ariaLabelledBy: string option)
        (ariaRoleDescription: string option)
        (tabIndex: int option)
        : unit =
        assignTestId props testId
        accessibilityId |> Option.iter (fun v -> props?nativeID <- v)

        assignAccessibility
            props
            accessibilityLabel
            (accessibilityRole |> Option.bind mapAccessibilityRole |> Option.map box)
            accessibilityState
            (mapImportantForAccessibility importantForAccessibility)
            (accessibilityLiveRegion |> Option.map box)
            accessibilityActions
            onAccessibilityAction
            ariaLabelledBy
            ariaRoleDescription
            tabIndex

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
        let isLinux: bool = import "linux" "platform-detect"
        let isMac: bool = import "macos" "platform-detect"
        let isAndroid: bool = import "android" "platform-detect"
        let isIOS: bool = import "ios" "platform-detect"

        match (isWindows, isLinux, isMac, isAndroid, isIOS) with
        | (true, _, _, _, _) -> OS.Windows
        | (_, true, _, _, _) -> OS.Linux
        | (_, _, true, _, _) -> OS.Mac
        | (_, _, _, true, _) -> OS.Android
        | (_, _, _, _, true) -> OS.IOS
        | _ -> OS.Other

    /// Drop-in replacement for `ReactXP.Runtime.platform` once the seam is flipped.
    let platform () : Platform =
#if EGGSHELL_PLATFORM_IS_WEB
        Web(detectWebOs ())
#else
        match RNSeam.PlatformModule?OS with
        | "android" -> Native NativePlatform.Android
        | "ios" -> Native NativePlatform.IOS
        | other -> failwithf "Unsupported react-native Platform.OS: %s" other
#endif

    let pixelDensity () : float = RNSeam.PixelRatioModule?get() |> float

module Runtime =
    let platform: Platform = RNPlatform.platform ()

    let ifWeb (f: Document -> unit) : unit =
        match platform with
        | Web _ -> f Browser.Dom.document
        | _ -> ()

    let isWeb () : bool =
        match platform with
        | Web _ -> true
        | _ -> false

    let isDesktopWeb () : bool =
        match platform with
        | Web OS.Windows
        | Web OS.Linux
        | Web OS.Mac -> true
        | _ -> false

    let isNative () : bool =
        match platform with
        | Native _ -> true
        | _ -> false

module App =
    let initialize (_debug: bool, _dev: bool) : unit =
        ()

module UserInterface =
    let windowLayoutInfo () : ReactXP.Types.ViewOnLayoutEvent =
        let w = RNSeam.DimensionsModule?get("window")

        { x = 0
          y = 0
          width = w?width |> int
          height = w?height |> int }

    let pixelDensity () : float = RNPlatform.pixelDensity ()

    let dismissKeyboard () : unit = RNSeam.KeyboardModule?dismiss()

    let mutable contextWrapper: Fable.React.ReactElement -> Fable.React.ReactElement = id

    let setContextWrapper (wrapper: Fable.React.ReactElement -> Fable.React.ReactElement) : unit =
        contextWrapper <- wrapper

#if EGGSHELL_PLATFORM_IS_WEB
    let private createRoot: obj = import "createRoot" "react-dom/client"
#endif

    let mutable mainRoot: obj option = None

    let setMainView (element: Fable.React.ReactElement) : unit =
#if EGGSHELL_PLATFORM_IS_WEB
        let wrappedElement = contextWrapper element

        match mainRoot with
        | Some root ->
            root?render(wrappedElement)
        | None ->
            let container: Browser.Types.HTMLElement =
                let maybeContainer = Browser.Dom.document.querySelector ".app-container"

                if isNull maybeContainer then
                    let div = Browser.Dom.document.createElement "div"
                    Browser.Dom.document.body?appendChild(div) |> ignore
                    div
                else
                    maybeContainer :?> Browser.Types.HTMLElement

            let root = createRoot $ (container)
            mainRoot <- Some root
            root?render(wrappedElement)
#else
        failwith "RNSeam.UserInterface.setMainView native path not implemented."
#endif

module Linking =
    open Fable.Core

    let getInitialUrl () : Async<Option<string>> =
        async {
            let! maybeRawUrl = RNSeam.LinkingModule?getInitialURL() |> Async.AwaitPromise
            return maybeRawUrl |> Option.map string
        }

    let openUrl (url: string) : unit =
        RNSeam.LinkingModule?openURL(url) |> ignore

    let deepLinkRequestEvent (callback: string -> unit) : unit =
        RNSeam.LinkingModule?addEventListener(
            "url",
            fun (e: obj) ->
                let maybeUrl = e?url |> Option.ofObj |> Option.map string
                maybeUrl |> Option.iter callback
        )

module Clipboard =
    let setText (text: string) : unit =
#if EGGSHELL_PLATFORM_IS_WEB
        let _promise = Browser.Dom.window?navigator?clipboard?writeText (text)
        ()
#else
        failwith "Clipboard.setText native path requires @react-native-clipboard/clipboard (not installed)."
#endif

module Popup =
    open Fable.React

#if EGGSHELL_PLATFORM_IS_WEB
    let private createRoot: obj = import "createRoot" "react-dom/client"
#endif

    type private PopupEntry = {
        Container: Browser.Types.HTMLElement
        Root:      obj
    }

    let private entries = System.Collections.Generic.Dictionary<string, PopupEntry>()

    let popupShowOptions (getAnchor: unit -> obj) (renderPopup: obj -> int -> int -> int -> ReactElement) (onDismiss: unit -> unit) : obj =
        createObj [
            "getAnchor"   ==> getAnchor
            "renderPopup" ==> renderPopup
            "onDismiss"   ==> onDismiss
        ]

    let dismiss (id: string) : unit =
        match entries.TryGetValue id with
        | true, entry ->
            entry.Root?unmount()
            entry.Container?remove()
            entries.Remove id |> ignore
        | false, _ ->
            ()

    let isDisplayed (id: string) : bool =
        entries.ContainsKey id

    let private findDOMNode: obj -> obj = import "findDOMNode" "react-dom"

    let private domRectOf (anchor: obj) : obj =
        if isNull anchor then
            createObj [ "top" ==> 0.0; "left" ==> 0.0; "right" ==> 0.0; "bottom" ==> 0.0; "width" ==> 0.0; "height" ==> 0.0 ]
        elif not (isNull (anchor?getBoundingClientRect)) then
            anchor?getBoundingClientRect()
        else
            let node = findDOMNode anchor
            if isNull node || isNull (node?getBoundingClientRect) then
                createObj [ "top" ==> 0.0; "left" ==> 0.0; "right" ==> 0.0; "bottom" ==> 0.0; "width" ==> 0.0; "height" ==> 0.0 ]
            else
                node?getBoundingClientRect()

    let show (options: obj, id: string) : unit =
#if EGGSHELL_PLATFORM_IS_WEB
        if entries.ContainsKey id then dismiss id

        let anchorEl: obj = options?getAnchor()
        let rect: obj = domRectOf anchorEl

        let container = Browser.Dom.document.createElement "div"
        container?style?position <- "fixed"
        container?style?top       <- (rect?bottom |> string) + "px"
        container?style?left      <- (rect?left |> string) + "px"
        container?style?zIndex    <- "9999"
        Browser.Dom.document.body?appendChild(container) |> ignore

        let renderPopup: obj = options?renderPopup
        let popupEl: ReactElement = renderPopup $ (rect, 0, 0, 0)

        let root = createRoot $ (container)
        root?render(popupEl)

        entries.[id] <- { Container = container; Root = root }
#else
        failwith "RNSeam.Popup.show is only implemented for web."
#endif
