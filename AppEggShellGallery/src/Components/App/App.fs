[<AutoOpen>]
module AppEggShellGallery.Components.App

open Fable.React
open Fable.Core.JsInterop
open LibLang
open LibClient
open LibClient.Components
open LibRouter.Components
open LibRouter.Components.Constructors
open LibRouter.Components.With.Route
open LibRouter.RoutesSpec
open AppEggShellGallery.Colors
open AppEggShellGallery.Navigation
open AppEggShellGallery.Components
open System.Text.RegularExpressions

do
    ReactXP.LegacyStyles.Css.addCss (sprintf """

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

let private registerGlobalMarkdownLinkHandler (nav: Navigation) : ReactElement =
    Browser.Dom.window?globalMarkdownLinkHandler <- (fun (e: Browser.Types.PointerEvent) (markdownUrl: string) ->
        let actionEvent = ReactEvent.Pointer.OfBrowserEvent e |> ReactEvent.Action.Make

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
            | None       -> Browser.Dom.window.alert ("Could not decode in-gallery URL link from a markdown document. Please let the EggShell team know. " + encodedRoute)
            | Some frame -> nav.Go frame actionEvent

        else
            let route =
                if markdownUrl.StartsWith "./" then
                    let trimmedUrl = markdownUrl.Substring("./".Length)
                    if markdownUrl.StartsWith "./tools/" then
                        Tools trimmedUrl
                    else if markdownUrl.StartsWith "./how-to/" then
                        HowTo (HowToItem.Markdown trimmedUrl)
                    else
                        Docs trimmedUrl
                else
                    Docs markdownUrl

            nav.Go (None, route) actionEvent
    )
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
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
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
                            appShellContent pstoreKey maybeNavigationFrame
                        |]
        )
