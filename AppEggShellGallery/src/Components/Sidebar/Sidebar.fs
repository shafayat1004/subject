[<AutoOpen>]
module AppEggShellGallery.Components.Sidebar

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components
open LibClient.Responsive
open AppEggShellGallery.Navigation
open AppEggShellGallery.Components.SidebarContent

module SI = LibClient.Components.Sidebar.Item

let private fixedTopBlades (maybeCurrentRoute: Option<Route>) (currentRoute: ActualRoute) (close: ReactEvent.Action -> unit) : ReactElement =
    let show route e =
        nav.Go (maybeCurrentRoute, route) e
        close e

    let itemState route =
        if route = currentRoute then SI.Selected else SI.Actionable (show route)

    LC.Responsive(
        desktop = (fun _ -> noElement),
        handheld =
            (fun _ ->
                castAsElement [|
                    LC.Sidebar.Item(label = "Docs",       testId = "sidebar-blade-docs",       state = itemState (Docs "index.md"))
                    LC.Sidebar.Item(label = "Tools",      testId = "sidebar-blade-tools",      state = itemState (Tools "tools/index.md"))
                    LC.Sidebar.Item(label = "Components", testId = "sidebar-blade-components", state = itemState (Components Index))
                    LC.Sidebar.Item(label = "How To",     testId = "sidebar-blade-how-to",     state = itemState (HowTo (HowToItem.Markdown "how-to/index.md")))
                    LC.Sidebar.Item(label = "Design",     testId = "sidebar-blade-design",     state = itemState (Design (DesignItem.Markdown "design/index.md")))
                |]
            )
    )

let private routeSidebar (maybeCurrentRoute: Option<Route>) (currentRoute: ActualRoute) (maybeFixedTop: ReactElement) (close: ReactEvent.Action -> unit) : ReactElement =
    match currentRoute with
    | Home | TinyGuid ->
        LC.Responsive(
            desktop = (fun _ -> noElement),
            handheld = (fun _ -> LC.Sidebar.Base(fixedTop = maybeFixedTop))
        )

    | Docs url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Docs itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(fixedTop = maybeFixedTop, scrollableMiddle = docsItems itemState)

    | Tools url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Tools itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(fixedTop = maybeFixedTop, scrollableMiddle = toolsItems itemState)

    | HowTo currItem ->
        let show item e =
            nav.Go (maybeCurrentRoute, HowTo item) e
            close e

        let itemState item =
            if item = currItem then SI.Selected else SI.Actionable (show item)

        let itemStateMarkdown url = itemState (HowToItem.Markdown url)

        LC.Sidebar.Base(fixedTop = maybeFixedTop, scrollableMiddle = howToItems itemStateMarkdown)

    | Subject url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Subject itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(fixedTop = maybeFixedTop, scrollableMiddle = subjectItems itemState)

    | Design currItem ->
        let show item e =
            nav.Go (maybeCurrentRoute, Design item) e
            close e

        let itemState item =
            if currItem = item then SI.Selected else SI.Actionable (show item)

        LC.Sidebar.Base(fixedTop = maybeFixedTop, scrollableMiddle = designItems itemState)

    | Legacy currItem ->
        let show item e =
            nav.Go (maybeCurrentRoute, Legacy item) e
            close e

        let itemState item =
            if currItem = item then SI.Selected else SI.Actionable (show item)

        LC.Sidebar.Base(fixedTop = maybeFixedTop, scrollableMiddle = legacyItems itemState)

    | Components content ->
        let show itemContent e =
            nav.Go (maybeCurrentRoute, Components itemContent) e
            close e

        let itemState itemContent =
            if content = itemContent then SI.Selected else SI.Actionable (show itemContent)

        LC.Sidebar.Base(fixedTop = maybeFixedTop, scrollableMiddle = componentsItems itemState)

let private sidebarBody (maybeCurrentRoute: Option<Route>) : ReactElement =
    match maybeCurrentRoute with
    | Some { SampleVisualsScreenSize = _; ActualRoute = currentRoute } ->
        LC.Sidebar.WithClose(fun close ->
            let maybeFixedTop = fixedTopBlades maybeCurrentRoute currentRoute close
            routeSidebar maybeCurrentRoute currentRoute maybeFixedTop close
        )

    | None ->
        LC.Text "no sidebar"

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member Sidebar(
            ?maybeRoute:      Option<Route>,
            ?children:        ReactChildrenProp,
            ?key:             string,
            ?xLegacyStyles:   List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        maybeRoute |> ignore
        children |> ignore
        xLegacyStyles |> ignore

        LR.With.CurrentRoute(
            spec = routesSpec(),
            fn = fun maybeCurrentRoute -> sidebarBody maybeCurrentRoute
        )
