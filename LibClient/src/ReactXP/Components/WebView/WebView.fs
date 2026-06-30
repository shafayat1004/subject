[<AutoOpen>]
module ReactXP.Components.WebView

open LibClient.JsInterop
open ReactXP.Types
open Fable.Core.JsInterop
open Fable.Core
open Fable.React
open Browser.Types

type WebViewSource = {
    html:    string
    baseUrl: string option
}

type WebViewNavigationState = {
    canGoBack:    bool
    canGoForward: bool
    loading:      bool
    url:          string
    title:        string
}

type WebViewShouldStartLoadEvent = {
    url: string
}

type WebViewMessageEvent = {
    data:   string
    origin: string
}

// Bit-flag sandbox modes. Combine with WebViewSandboxMode.ofList for multi-flag use.
type WebViewSandboxMode =
| None                               =    0 // 0
| AllowForms                         =    1 // 1 << 0
| AllowModals                        =    2 // 1 << 1
| AllowOrientationLock               =    4 // 1 << 2
| AllowPointerLock                   =    8 // 1 << 3
| AllowPopups                        =   16 // 1 << 4
| AllowPopupsToEscapeSandbox         =   32 // 1 << 5
| AllowPresentation                  =   64 // 1 << 6
| AllowSameOrigin                    =  128 // 1 << 7
| AllowScripts                       =  256 // 1 << 8
| AllowTopNavigation                 =  512 // 1 << 9
| AllowMixedContentAlways            = 1024 // 1 << 10
| AllowMixedContentCompatibilityMode = 2048 // 1 << 11

module WebViewSandboxMode =
    let ofList (modes: List<WebViewSandboxMode>) : WebViewSandboxMode =
        modes |> List.reduce (fun a b -> a ||| b) :> obj :?> WebViewSandboxMode

type WebViewMethods = {
    goBack:      unit   -> unit
    goForward:   unit   -> unit
    reload:      unit   -> unit
    postMessage: string -> unit
}

module private WebViewRN =
    #if !EGGSHELL_PLATFORM_IS_WEB
    let WebViewComponent : obj = importDefault "react-native-webview"
    #endif

    // ReactXP url (string) → source { uri: string }; ReactXP source { html, baseUrl } → source { html, baseUrl }
    let makeSource (url: string option) (source: WebViewSource option) : obj option =
        match url, source with
        | Some u, _ ->
            let s = createEmpty
            s?uri <- u
            Some s
        | _, Some src ->
            let s = createEmpty
            s?html <- src.html
            src.baseUrl |> Option.iter (fun b -> s?baseUrl <- b)
            Some s
        | None, None -> None

    // react-native-webview onMessage: { nativeEvent: { data, url, ... } }
    // ReactXP onMessage: { data, origin }  (origin ≈ url)
    let wrapOnMessage (handler: WebViewMessageEvent -> unit) : obj -> unit =
        fun (e: obj) ->
            let ne = e?nativeEvent
            handler { data = ne?data; origin = ne?url |> unbox<string> }

    // react-native-webview onNavigationStateChange passes { url, canGoBack, canGoForward, loading, title }
    // which already matches WebViewNavigationState -- just unbox.
    let wrapOnNavigationStateChange (handler: WebViewNavigationState -> unit) : obj -> unit =
        fun (e: obj) ->
            handler {
                url          = e?url
                canGoBack    = e?canGoBack
                canGoForward = e?canGoForward
                loading      = e?loading
                title        = e?title
            }

    let wrapOnShouldStartLoadWithRequest (handler: WebViewShouldStartLoadEvent -> bool) : obj -> bool =
        fun (e: obj) -> handler { url = e?url }

    let unboxStyles (styles: array<ReactXP.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    // Map WebViewSandboxMode flags to the HTML sandbox attribute string (web only).
    let sandboxString (mode: WebViewSandboxMode) : string =
        let flag m label = if (mode &&& m) = m && m <> WebViewSandboxMode.None then Some label else None
        [
            flag WebViewSandboxMode.AllowForms                         "allow-forms"
            flag WebViewSandboxMode.AllowModals                        "allow-modals"
            flag WebViewSandboxMode.AllowOrientationLock               "allow-orientation-lock"
            flag WebViewSandboxMode.AllowPointerLock                   "allow-pointer-lock"
            flag WebViewSandboxMode.AllowPopups                        "allow-popups"
            flag WebViewSandboxMode.AllowPopupsToEscapeSandbox         "allow-popups-to-escape-sandbox"
            flag WebViewSandboxMode.AllowPresentation                  "allow-presentation"
            flag WebViewSandboxMode.AllowSameOrigin                    "allow-same-origin"
            flag WebViewSandboxMode.AllowScripts                       "allow-scripts"
            flag WebViewSandboxMode.AllowTopNavigation                 "allow-top-navigation"
        ]
        |> List.choose id
        |> String.concat " "

type ReactXP.Components.Constructors.RX with
    static member WebView(
        ?url:                             string,
        ?source:                          WebViewSource,
        ?headers:                         Headers,
        ?onLoad:                          Event -> unit,
        ?onNavigationStateChange:         WebViewNavigationState -> unit,
        ?scalesPageToFit:                 bool,
        ?injectedJavaScript:              string,
        ?javaScriptEnabled:               bool,
        ?mediaPlaybackRequiresUserAction: bool,
        ?allowsInlineMediaPlayback:       bool,
        ?startInLoadingState:             bool,
        ?domStorageEnabled:               bool,
        ?onShouldStartLoadWithRequest:    WebViewShouldStartLoadEvent -> bool,
        ?onLoadStart:                     Event -> unit,
        ?onError:                         Event -> unit,
        ?onMessage:                       WebViewMessageEvent -> unit,
        ?sandbox:                         WebViewSandboxMode,
        ?ref:                             JsNullable<WebViewMethods> -> unit,
        ?styles:                          array<ReactXP.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:                   List<ReactXP.LegacyStyles.RuntimeStyles>
    ) =
        ignore headers
        #if EGGSHELL_PLATFORM_IS_WEB
        // Web: render a plain <iframe>. WebView is only called from With.Native (native-only context),
        // so this path just needs to be a valid no-op that compiles and renders something inert.
        let iframeProps = createEmpty
        WebViewRN.makeSource url source |> Option.iter (fun s ->
            match url with
            | Some _ -> iframeProps?src    <- s?uri
            | None   -> iframeProps?srcDoc <- s?html)
        sandbox
        |> Option.filter (fun m -> m <> WebViewSandboxMode.None)
        |> Option.iter   (fun m -> iframeProps?sandbox <- WebViewRN.sandboxString m)
        iframeProps?style <- WebViewRN.unboxStyles styles
        ReactBindings.React.createElement("iframe", iframeProps, [||])
        #else
        let __props = createEmpty

        WebViewRN.makeSource url source |> Option.iter (fun s -> __props?source <- s)

        __props?javaScriptEnabled               <- javaScriptEnabled
        __props?domStorageEnabled               <- domStorageEnabled
        __props?scalesPageToFit                 <- scalesPageToFit
        __props?injectedJavaScript              <- injectedJavaScript
        __props?mediaPlaybackRequiresUserAction <- mediaPlaybackRequiresUserAction
        __props?allowsInlineMediaPlayback       <- allowsInlineMediaPlayback
        __props?startInLoadingState             <- startInLoadingState

        onLoad    |> Option.iter (fun v -> __props?onLoad    <- v)
        onLoadStart |> Option.iter (fun v -> __props?onLoadStart <- v)
        onError   |> Option.iter (fun v -> __props?onError   <- v)

        onMessage |> Option.iter (fun v ->
            __props?onMessage <- WebViewRN.wrapOnMessage v)
        onNavigationStateChange |> Option.iter (fun v ->
            __props?onNavigationStateChange <- WebViewRN.wrapOnNavigationStateChange v)
        onShouldStartLoadWithRequest |> Option.iter (fun v ->
            __props?onShouldStartLoadWithRequest <- WebViewRN.wrapOnShouldStartLoadWithRequest v)

        __props?ref   <- ref
        __props?style <- WebViewRN.unboxStyles styles

        match xLegacyStyles with
        | Option.None | Option.Some [] -> ()
        | Option.Some ls -> __props?__style <- ls

        ReactXP.RNSeam.createElement
            WebViewRN.WebViewComponent
            __props
            [||]
        #endif
