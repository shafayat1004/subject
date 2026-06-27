module AppEggShellGallery.Components.SidebarRender

module FRH = Fable.React.Helpers
module FRP = Fable.React.Props
module FRS = Fable.React.Standard


open LibClient.Components
open LibRouter.Components
open ThirdParty.Map.Components
open ReactXP.Components
open ThirdParty.Recharts.Components
open ThirdParty.Showdown.Components
open ThirdParty.SyntaxHighlighter.Components
open LibUiAdmin.Components
open AppEggShellGallery.Components

open LibLang
open LibClient
open LibClient.Services.Subscription
open LibClient.RenderHelpers
open LibClient.Chars
open LibClient.ColorModule
open LibClient.Responsive
open AppEggShellGallery.RenderHelpers
open AppEggShellGallery.Navigation
open AppEggShellGallery.LocalImages
open AppEggShellGallery.Icons
open AppEggShellGallery.AppServices
open AppEggShellGallery

open AppEggShellGallery.Components.Sidebar



let render(children: array<ReactElement>, props: AppEggShellGallery.Components.Sidebar.Props, estate: AppEggShellGallery.Components.Sidebar.Estate, pstate: AppEggShellGallery.Components.Sidebar.Pstate, actions: AppEggShellGallery.Components.Sidebar.Actions, __componentStyles: ReactXP.LegacyStyles.RuntimeStyles) : Fable.React.ReactElement =
    // sadly #nowarn has file scope, so we have to emulate it manually
    (children, props, estate, pstate, actions) |> ignore
    let __class = (ReactXP.Helpers.extractProp "ClassName" props) |> Option.defaultValue ""
    let __mergedStyles = ReactXP.LegacyStyles.Runtime.mergeComponentAndPropsStyles __componentStyles props
    let __parentFQN = None
    let __parentFQN = Some "LibRouter.Components.With.CurrentRoute"
    LibRouter.Components.Constructors.LR.With.CurrentRoute(
        spec = (routesSpec()),
        fn =
            (fun (maybeCurrentRoute) ->
                    (castAsElementAckingKeysWarning [|
                        match (maybeCurrentRoute) with
                        | Some { SampleVisualsScreenSize = _; ActualRoute = currentRoute } ->
                            [|
                                let __parentFQN = Some "LibClient.Components.Sidebar.WithClose"
                                LibClient.Components.Constructors.LC.Sidebar.WithClose(
                                    ``with`` =
                                        (fun (close) ->
                                                (castAsElementAckingKeysWarning [|
                                                    let maybeFixedTop = (
                                                            (castAsElementAckingKeysWarning [|
                                                                (
                                                                    let show = fun (route: ActualRoute) e -> nav.Go (maybeCurrentRoute, route) e; close e
                                                                    let itemState = (
                                                                        fun route -> if route = currentRoute then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show route);
                                                                                             
                                                                    )
                                                                    let __parentFQN = Some "LibClient.Components.Responsive"
                                                                    LibClient.Components.Constructors.LC.Responsive(
                                                                        desktop =
                                                                            (fun (_screenSize) ->
                                                                                    (castAsElementAckingKeysWarning [|
                                                                                        noElement
                                                                                    |])
                                                                            ),
                                                                        handheld =
                                                                            (fun (_screenSize) ->
                                                                                    (castAsElementAckingKeysWarning [|
                                                                                        let __parentFQN = Some "LibClient.Components.Sidebar.Item"
                                                                                        LibClient.Components.Constructors.LC.Sidebar.Item(
                                                                                            state = (itemState (Docs "index.md")                               ),
                                                                                            label = ("Docs")
                                                                                        )
                                                                                        let __parentFQN = Some "LibClient.Components.Sidebar.Item"
                                                                                        LibClient.Components.Constructors.LC.Sidebar.Item(
                                                                                            state = (itemState (Tools "tools/index.md")                        ),
                                                                                            label = ("Tools")
                                                                                        )
                                                                                        let __parentFQN = Some "LibClient.Components.Sidebar.Item"
                                                                                        LibClient.Components.Constructors.LC.Sidebar.Item(
                                                                                            state = (itemState (Components Index)                              ),
                                                                                            label = ("Components")
                                                                                        )
                                                                                        let __parentFQN = Some "LibClient.Components.Sidebar.Item"
                                                                                        LibClient.Components.Constructors.LC.Sidebar.Item(
                                                                                            state = (itemState (HowTo (HowToItem.Markdown "how-to/index.md"))  ),
                                                                                            label = ("How To")
                                                                                        )
                                                                                        let __parentFQN = Some "LibClient.Components.Sidebar.Item"
                                                                                        LibClient.Components.Constructors.LC.Sidebar.Item(
                                                                                            state = (itemState (Design (DesignItem.Markdown "design/index.md"))),
                                                                                            label = ("Design")
                                                                                        )
                                                                                    |])
                                                                            )
                                                                    )
                                                                )
                                                            |])
                                                    )
                                                    match (currentRoute) with
                                                    | Home | TinyGuid ->
                                                        [|
                                                            let __parentFQN = Some "LibClient.Components.Responsive"
                                                            LibClient.Components.Constructors.LC.Responsive(
                                                                desktop =
                                                                    (fun (_screenSize) ->
                                                                            (castAsElementAckingKeysWarning [|
                                                                                noElement
                                                                            |])
                                                                    ),
                                                                handheld =
                                                                    (fun (_screenSize) ->
                                                                            (castAsElementAckingKeysWarning [|
                                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                                    fixedTop =
                                                                                            (castAsElementAckingKeysWarning [|
                                                                                                maybeFixedTop
                                                                                            |])
                                                                                )
                                                                            |])
                                                                    )
                                                            )
                                                        |]
                                                    | Docs url ->
                                                        [|
                                                            (
                                                                let show = fun url e -> nav.Go (maybeCurrentRoute, Docs url) e; close e
                                                                let itemState = (
                                                                    fun itemUrl -> if url = itemUrl then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show itemUrl);
                                                                                             
                                                                )
                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                    fixedTop =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                maybeFixedTop
                                                                            |]),
                                                                    scrollableMiddle =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                SidebarContent.docsItems itemState
                                                                            |])
                                                                )
                                                            )
                                                        |]
                                                    | Tools url ->
                                                        [|
                                                            (
                                                                let show = fun url e -> nav.Go (maybeCurrentRoute, Tools url) e; close e
                                                                let itemState = (
                                                                    fun itemUrl -> if url = itemUrl then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show itemUrl);
                                                                                             
                                                                )
                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                    fixedTop =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                maybeFixedTop
                                                                            |]),
                                                                    scrollableMiddle =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                SidebarContent.toolsItems itemState
                                                                            |])
                                                                )
                                                            )
                                                        |]
                                                    | HowTo currItem ->
                                                        [|
                                                            (
                                                                let show = fun item e -> nav.Go (maybeCurrentRoute, HowTo item) e; close e
                                                                let itemState = fun item -> if item = currItem then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show item)
                                                                let itemStateMarkdown = (
                                                                    fun url -> itemState (HowToItem.Markdown url);
                                                                                             
                                                                )
                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                    fixedTop =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                maybeFixedTop
                                                                            |]),
                                                                    scrollableMiddle =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                SidebarContent.howToItems itemStateMarkdown
                                                                            |])
                                                                )
                                                            )
                                                        |]
                                                    | Subject url ->
                                                        [|
                                                            (
                                                                let show = fun url e -> nav.Go (maybeCurrentRoute, Subject url) e; close e
                                                                let itemState = (
                                                                    fun itemUrl -> if url = itemUrl then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show itemUrl);
                                                                                             
                                                                )
                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                    fixedTop =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                maybeFixedTop
                                                                            |]),
                                                                    scrollableMiddle =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                SidebarContent.subjectItems itemState
                                                                            |])
                                                                )
                                                            )
                                                        |]
                                                    | Design currItem ->
                                                        [|
                                                            (
                                                                let show = fun item e -> nav.Go (maybeCurrentRoute, Design item) e; close e
                                                                let itemState = (
                                                                    fun item -> if currItem = item then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show item);
                                                                                             
                                                                )
                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                    fixedTop =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                maybeFixedTop
                                                                            |]),
                                                                    scrollableMiddle =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                SidebarContent.designItems itemState
                                                                            |])
                                                                )
                                                            )
                                                        |]
                                                    | Legacy currItem ->
                                                        [|
                                                            (
                                                                let show = fun item e -> nav.Go (maybeCurrentRoute, Legacy item) e; close e
                                                                let itemState = (
                                                                    fun item -> if currItem = item then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show item);
                                                                                             
                                                                )
                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                    fixedTop =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                maybeFixedTop
                                                                            |]),
                                                                    scrollableMiddle =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                SidebarContent.legacyItems itemState
                                                                            |])
                                                                )
                                                            )
                                                        |]
                                                    | Components content ->
                                                        [|
                                                            (
                                                                let show = fun content e -> nav.Go (maybeCurrentRoute, Components content) e; close e
                                                                let itemState = (
                                                                    fun itemContent -> if content = itemContent then LibClient.Components.Sidebar.Item.Selected else LibClient.Components.Sidebar.Item.Actionable (show itemContent);
                                                                                             
                                                                )
                                                                let __parentFQN = Some "LibClient.Components.Sidebar.Base"
                                                                LibClient.Components.Constructors.LC.Sidebar.Base(
                                                                    fixedTop =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                maybeFixedTop
                                                                            |]),
                                                                    scrollableMiddle =
                                                                            (castAsElementAckingKeysWarning [|
                                                                                SidebarContent.componentsItems itemState
                                                                            |])
                                                                )
                                                            )
                                                        |]
                                                    |> castAsElementAckingKeysWarning
                                                |])
                                        )
                                )
                            |]
                        | None ->
                            [|
                                makeTextNode2 __parentFQN "no sidebar"
                            |]
                        |> castAsElementAckingKeysWarning
                    |])
            )
    )
