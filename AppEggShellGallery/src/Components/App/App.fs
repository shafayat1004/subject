[<AutoOpen>]
module AppEggShellGallery.Components.App

open Fable.React
open Fable.Core
open Fable.Core.JsInterop
open LibLang
open LibClient
open LibClient.Components
open LibRouter.Components
open LibRouter.Components.Constructors
open LibRouter.Components.With.Route
open LibRouter.Components.With.Location
open LibRouter.RoutesSpec
open AppEggShellGallery.Colors
open AppEggShellGallery.Navigation
open AppEggShellGallery.Components
open System.Text.RegularExpressions

do
    Rn.LegacyStyles.Css.addCss (sprintf """

.markdown-tag {
    background-color: %s;
    color:            %s;
    border-radius:    20px;
    padding:          4px 10px;
    margin:           0px 10px;
    font-size:        12px;
}

"""
        colors.Secondary.B200.ToCssString
        colors.Secondary.Main.ToCssString
    )

[<Fable.Core.Emit("globalThis")>]
let private jsGlobalThis: obj = jsNative

// Platform-neutral: turn a markdown link href into in-app navigation (or external open).
// Shared by the web onclick handler and the native react-native-render-html anchor onPress.
let private handleMarkdownLink (nav: Navigation) (actionEvent: ReactEvent.Action) (rawMarkdownUrl: string) : unit =
    // react-native-render-html resolves a relative href against an "about://" base, so a native
    // anchor arrives as e.g. "about:///modernization/x.md". Normalize it back to the "./modernization/x.md"
    // form the rest of this handler (and the web onclick) expects. No-op on web (hrefs are already "./x").
    let markdownUrl =
        if rawMarkdownUrl.StartsWith "about://" then
            let stripped = rawMarkdownUrl.Substring("about://".Length)
            if stripped.StartsWith "/" then "." + stripped else "./" + stripped
        else
            rawMarkdownUrl

    if Regex.IsMatch (markdownUrl, "^(http|https)://") then
        nav.GoExternalMaybeInNewTab markdownUrl actionEvent

    else if markdownUrl.StartsWith "gallery://act/" then
        match markdownUrl.Substring("gallery://act/".Length) with
        | "toggleTapCaptureDebugVisualization" ->
            LibClient.Components.TapCaptureDebugVisibility.toggleVisibleForDebug ()
        | _ -> Noop

    else if markdownUrl.StartsWith "gallery://" then
        let encodedRoute = markdownUrl.Substring("gallery://".Length)
        match routesSpec().FromLocation (Location.ofPath encodedRoute) with
        | None       -> Log.Error ("Could not decode in-gallery URL link from a markdown document: " + encodedRoute)
        | Some frame -> nav.Go frame actionEvent

    else
        let route =
            if markdownUrl.StartsWith "./" then
                let trimmedUrl = markdownUrl.Substring("./".Length)
                if markdownUrl.StartsWith "./tools/" then
                    Tools trimmedUrl
                else if markdownUrl.StartsWith "./how-to/" then
                    HowTo (HowToItem.Markdown trimmedUrl)
                else if markdownUrl.StartsWith "./subject/" then
                    Subject trimmedUrl
                else if markdownUrl.StartsWith "./architecture/" then
                    Architecture trimmedUrl
                else if markdownUrl.StartsWith "./modernization/" then
                    Modernization trimmedUrl
                else if markdownUrl.StartsWith "./runbooks/" then
                    Runbooks trimmedUrl
                else if markdownUrl.StartsWith "./accessibility/" then
                    Accessibility trimmedUrl
                else if markdownUrl.StartsWith "./knowledge-base/" then
                    KnowledgeBase trimmedUrl
                else
                    Docs trimmedUrl
            else
                Docs markdownUrl

        nav.Go (None, route) actionEvent

let private registerGlobalMarkdownLinkHandler (nav: Navigation) : ReactElement =
#if EGGSHELL_PLATFORM_IS_WEB
    Browser.Dom.window?globalMarkdownLinkHandler <- (fun (e: Browser.Types.PointerEvent) (markdownUrl: string) ->
        handleMarkdownLink nav (ReactEvent.Pointer.OfBrowserEvent e |> ReactEvent.Action.Make) markdownUrl)
#else
    // Native has no DOM event; the render-html anchor onPress passes just the href.
    jsGlobalThis?globalMarkdownLinkHandler <- (fun (markdownUrl: string) ->
        handleMarkdownLink nav ReactEvent.Action.NonUserOriginatingAction markdownUrl)

    // Deep-link handler: navigate to a path from an incoming URL (adb am start -a VIEW -d
    // "http://example.app/components/Accessibility_Group"). The AndroidManifest already has an
    // intent filter for host "example.app". This lets Appium / Tier-2 automation navigate
    // without synthesising touches (which RNGH under Fabric swallows). The URL path maps
    // directly to a gallery route via the existing handleMarkdownLink "gallery://" path.
    let initDeepLinkNav () : unit =
        let navigateFromUrl (rawUrl: string) : unit =
            try
                // Strip scheme://host, keep the path segments. The AndroidManifest
                // intent filter has host "example.app". Incoming format:
                //   http://example.app/components/Accessibility_Group
                // The gallery routesSpec uses /{json}/Components/{json} format, e.g.
                //   /"Desktop"/Components/"Accessibility_Group"
                // So we map the incoming path to the routesSpec format.
                let path =
                    let u = rawUrl.Replace("http://", "").Replace("https://", "")
                    let afterHost = u.IndexOf("example.app")
                    if afterHost >= 0 then
                        let p = u.Substring(afterHost + "example.app".Length)
                        if p.StartsWith "/" then p.Substring(1) else p
                    else
                        rawUrl
                let galleryUrl =
                    // Map "components/Accessibility_Group" → "/\"Desktop\"/Components/\"Accessibility_Group\""
                    let parts = path.Split('/')
                    if parts.Length >= 2 then
                        let section = parts.[0]
                        let item = parts.[1]
                        sprintf "/\"Desktop\"/%s/\"%s\"" (System.Char.ToUpper(section.[0]).ToString() + section.Substring(1)) item
                    else
                        path
                jsGlobalThis?globalMarkdownLinkHandler("gallery://" + galleryUrl) |> ignore
            with exn ->
                Log.Error ("Deep-link navigation failed: " + rawUrl + " — " + string exn.Message)

        Rn.Linking.deepLinkRequestEvent (navigateFromUrl)
        // Also handle the initial URL that launched the app (cold start via deep link).
        Async.StartImmediate (async {
            let! maybeUrl = Rn.Linking.getInitialUrl ()
            maybeUrl |> Option.iter navigateFromUrl
        })

    initDeepLinkNav ()
#endif
    noElement

let private routeContent (pstoreKey: string) (maybeRoute: Option<Route>) =
    match maybeRoute with
    | Some { SampleVisualsScreenSize = sampleVisualsScreenSize; ActualRoute = route } ->
        match route with
        | Home ->
            Ui.Route.Home(pstoreKey + "-Route-Home")
        | Docs url ->
            Ui.Route.Docs(pstoreKey + "-Route-Docs", url)
        | Components content ->
            Ui.Route.Components(pstoreKey + "-Route-Components", sampleVisualsScreenSize, content)
        | Tools url ->
            Ui.Route.Tools(pstoreKey + "-Route-Tools", url)
        | HowTo item ->
            Ui.Route.HowTo(pstoreKey + "-Route-HowTo", item)
        | Subject url ->
            Ui.Route.Subject(pstoreKey + "-Route-Subject", url)
        | Architecture url ->
            Ui.Route.Docs(pstoreKey + "-Route-Architecture", url)
        | Modernization url ->
            Ui.Route.Docs(pstoreKey + "-Route-Modernization", url)
        | Runbooks url ->
            Ui.Route.Docs(pstoreKey + "-Route-Runbooks", url)
        | Accessibility url ->
            Ui.Route.Docs(pstoreKey + "-Route-Accessibility", url)
        | KnowledgeBase url ->
            Ui.Route.Docs(pstoreKey + "-Route-KnowledgeBase", url)
        | Design item ->
            Ui.Route.Design(pstoreKey + "-Route-Design", item)
        | Legacy item ->
            Ui.Route.Legacy(pstoreKey + "-Route-Legacy", item)
        | TinyGuid ->
            Ui.Route.TinyGuid()
    | None ->
        LC.InfoMessage(message = "Route Not Found", level = InfoMessage.Attention)

let private appShellContent (pstoreKey: string) (maybeNavigationFrame: Option<NavigationFrame<Route, ResultlessDialog>>) =
    let maybeRoute = maybeNavigationFrame |> Option.map NavigationFrame.route

    LC.AppShell.Content(
        desktopSidebarStyle = AppShell_Content.DesktopSidebarStyle.Fixed,
        onError = LC.AppShell.TopLevelErrorMessage,
        dialogs =
            Ui.With.Navigation(
                ``with`` =
                    fun nav ->
                        LR.Dialogs(
                            nav,
                            (maybeNavigationFrame
                             |> Option.map NavigationFrame.dialogs
                             |> Option.getOrElse []),
                            navigationState.DialogsState,
                            (fun (resultlessDialog, _close) ->
                                match resultlessDialog with
                                | ResultlessDialog.Sentinel -> noElement),
                            (fun (resultfulDialog, _close) ->
                                match resultfulDialog with
                                | ResultfulDialog.Sentinel -> noElement)
                        )
            ),
        sidebar = Ui.Sidebar(maybeRoute = maybeRoute),
        topNav = Ui.TopNav(maybeRoute = maybeRoute),
        content = routeContent pstoreKey maybeRoute
    )

type AppEggShellGallery.Components.Constructors.Ui.App with
    [<Component>]
    static member Root(
            ?pstoreKey:     string,
            ?children:      ReactChildrenProp,
            ?key:           string,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)

        let pstoreKey = defaultArg pstoreKey "app"

        LR.With.Route(
            spec = routesSpec(),
            ``with`` =
                fun maybeNavigationFrame ->
                    castAsElement
                        [|
                            LR.LogRouteTransitions()
                            Ui.With.Navigation(
                                ``with`` = registerGlobalMarkdownLinkHandler
                            )
                            // Native hardware back / edge-swipe back: pop the router history instead
                            // of closing the app. LR.NativeBackButton handles Android's hardware Back
                            // (no-op on web/iOS); LR.EdgeSwipeBack handles the iOS left-edge swipe
                            // (no-op on web/Android). Both are noElement/overlays, safe to mount
                            // unconditionally. canGoBack is false at the root route (pathname "/")
                            // so Back at Home still lets the OS close the app instead of trapping
                            // the user.
                            Ui.With.Navigation(
                                ``with`` = fun nav ->
                                    LR.With.Location(
                                        ``with`` = fun location ->
                                            let canGoBack = (fun () -> location.pathname <> "/")
                                            castAsElementAckingKeysWarning
                                                [|
                                                    LR.NativeBackButton(nav.GoBack, canGoBack = canGoBack, key = "nativeBack")
                                                    LR.EdgeSwipeBack(nav.GoBack, canGoBack = canGoBack, key = "edgeSwipeBack")
                                                |]
                                    )
                            )
                            appShellContent pstoreKey maybeNavigationFrame
                        |]
        )
