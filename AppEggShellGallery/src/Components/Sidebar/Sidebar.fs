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

// On handheld the sidebar has two logical regions: the top-level section navigator
// ("blades") and the current section's sub-page list.  Putting them all in one
// scrollable region (scrollableMiddle) avoids the blades consuming 50%+ of the
// viewport height as a non-scrolling fixedTop, which hides sub-items behind the fold.
let private bladesForScrollable (maybeCurrentRoute: Option<Route>) (currentRoute: ActualRoute) (close: ReactEvent.Action -> unit) : ReactElement =
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
                    LC.Sidebar.Item(label = "Docs",          testId = "sidebar-blade-docs",          state = itemState (Docs "index.md"))
                    LC.Sidebar.Item(label = "Architecture",  testId = "sidebar-blade-architecture",  state = itemState (Architecture "architecture/index.md"))
                    LC.Sidebar.Item(label = "Modernization", testId = "sidebar-blade-modernization", state = itemState (Modernization "modernization/index.md"))
                    LC.Sidebar.Item(label = "Runbooks",      testId = "sidebar-blade-runbooks",      state = itemState (Runbooks "runbooks/index.md"))
                    LC.Sidebar.Item(label = "Accessibility", testId = "sidebar-blade-accessibility", state = itemState (Accessibility "accessibility/index.md"))
                    LC.Sidebar.Item(label = "Knowledge Base", testId = "sidebar-blade-knowledge-base", state = itemState (KnowledgeBase "knowledge-base/index.md"))
                    LC.Sidebar.Item(label = "Tools",         testId = "sidebar-blade-tools",         state = itemState (Tools "tools/index.md"))
                    LC.Sidebar.Item(label = "Components",    testId = "sidebar-blade-components",    state = itemState (Components Index))
                    LC.Sidebar.Item(label = "How To",        testId = "sidebar-blade-how-to",        state = itemState (HowTo (HowToItem.Markdown "how-to/index.md")))
                    LC.Sidebar.Item(label = "Design",        testId = "sidebar-blade-design",        state = itemState (Design (DesignItem.Markdown "design/index.md")))
                    LC.Sidebar.Divider()
                |]
            )
    )

let private routeSidebar (maybeCurrentRoute: Option<Route>) (currentRoute: ActualRoute) (bladesScrollable: ReactElement) (close: ReactEvent.Action -> unit) : ReactElement =
    // Combine blade nav + section sub-items into one scrollable region.
    // On desktop bladesScrollable is noElement, so this is a no-op there.
    let withBlades subItems = castAsElement [| bladesScrollable; subItems |]

    match currentRoute with
    | Home | TinyGuid ->
        LC.Responsive(
            desktop = (fun _ -> noElement),
            handheld = (fun _ -> LC.Sidebar.Base(scrollableMiddle = bladesScrollable))
        )

    | Docs url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Docs itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (docsItems itemState))

    | Tools url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Tools itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (toolsItems itemState))

    | HowTo currItem ->
        let show item e =
            nav.Go (maybeCurrentRoute, HowTo item) e
            close e

        let itemState item =
            if item = currItem then SI.Selected else SI.Actionable (show item)

        let itemStateMarkdown url = itemState (HowToItem.Markdown url)

        LC.Sidebar.Base(scrollableMiddle = withBlades (howToItems itemStateMarkdown))

    | Subject url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Subject itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (subjectItems itemState))

    | Architecture url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Architecture itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (architectureItems itemState))

    | Modernization url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Modernization itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (modernizationItems itemState))

    | Runbooks url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Runbooks itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (runbooksItems itemState))

    | Accessibility url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, Accessibility itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (accessibilityItems itemState))

    | KnowledgeBase url ->
        let show itemUrl e =
            nav.Go (maybeCurrentRoute, KnowledgeBase itemUrl) e
            close e

        let itemState itemUrl =
            if url = itemUrl then SI.Selected else SI.Actionable (show itemUrl)

        LC.Sidebar.Base(scrollableMiddle = withBlades (knowledgeBaseItems itemState))

    | Design currItem ->
        let show item e =
            nav.Go (maybeCurrentRoute, Design item) e
            close e

        let itemState item =
            if currItem = item then SI.Selected else SI.Actionable (show item)

        LC.Sidebar.Base(scrollableMiddle = withBlades (designItems itemState))

    | Legacy currItem ->
        let show item e =
            nav.Go (maybeCurrentRoute, Legacy item) e
            close e

        let itemState item =
            if currItem = item then SI.Selected else SI.Actionable (show item)

        LC.Sidebar.Base(scrollableMiddle = withBlades (legacyItems itemState))

    | Components content ->
        let show itemContent e =
            nav.Go (maybeCurrentRoute, Components itemContent) e
            close e

        let itemState itemContent =
            if content = itemContent then SI.Selected else SI.Actionable (show itemContent)

        LC.Sidebar.Base(scrollableMiddle = withBlades (componentsItems itemState))

let private sidebarBody (maybeCurrentRoute: Option<Route>) : ReactElement =
    match maybeCurrentRoute with
    | Some { SampleVisualsScreenSize = _; ActualRoute = currentRoute } ->
        LC.Sidebar.WithClose(fun close ->
            let bladesScrollable = bladesForScrollable maybeCurrentRoute currentRoute close
            routeSidebar maybeCurrentRoute currentRoute bladesScrollable close
        )

    | None ->
        LC.Text "no sidebar"

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member Sidebar(
            ?maybeRoute:      Option<Route>,
            ?children:        ReactChildrenProp,
            ?key:             string,
            ?xLegacyStyles:   List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        maybeRoute |> ignore
        children |> ignore
        xLegacyStyles |> ignore

        LR.With.CurrentRoute(
            spec = routesSpec(),
            fn = fun maybeCurrentRoute -> sidebarBody maybeCurrentRoute
        )
