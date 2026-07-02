[<AutoOpen>]
module AppEggShellGallery.Components.TopNav

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Responsive
open LibRouter.Components
open ReactXP.Components
open ReactXP.Styles
open AppEggShellGallery.Colors
open AppEggShellGallery.Icons
open AppEggShellGallery.Navigation

module NavTopItem = LibClient.Components.Nav.Top.Item

[<RequireQualifiedAccess>]
module private Styles =
    let logo =
        makeViewStyles {
            Position.Relative
            marginRight 20
            paddingTop  5
        }

    let logoIcon =
        makeTextStyles {
            fontSize 64
            color    colors.Primary.B050
        }

    let sampleVisualsScreenSize =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Center
            marginRight 30
        }

    let label =
        makeTextStyles {
            marginRight 6
            color Color.White
        }

let private desktopHeading (maybeCurrentActualRoute: Option<ActualRoute>) =
    let text =
        match maybeCurrentActualRoute with
        | Some (Docs _)          -> "EggShell — Docs"
        | Some (Architecture _)  -> "EggShell — Architecture"
        | Some (Modernization _) -> "EggShell — Modernization"
        | Some (Runbooks _)      -> "EggShell — Runbooks"
        | Some (Accessibility _) -> "EggShell — Accessibility"
        | Some (KnowledgeBase _) -> "EggShell — Knowledge Base"
        | Some (Tools _)         -> "EggShell — Tools"
        | Some (HowTo _)         -> "EggShell — How To"
        | Some (Subject _)       -> "EggShell — Subject"
        | Some (Design _)        -> "EggShell — Design"
        | Some (Components _)    -> "EggShell — Components"
        | _                      -> "EggShell"

    LC.Nav.Top.Heading(text = text)

let private navItemState (maybeCurrentActualRoute: Option<ActualRoute>) (matchRoute: ActualRoute -> bool) (go: ReactEvent.Action -> unit) =
    match maybeCurrentActualRoute with
    | Some route when matchRoute route -> NavTopItem.SelectedActionable go
    | _                                -> NavTopItem.Actionable go

let private desktopNav (maybeCurrentRoute: Option<Route>) (maybeCurrentActualRoute: Option<ActualRoute>) =
    let sampleVisualsScreenSize = nav.CurrentSampleVisualsScreenSizeOrDefault maybeCurrentRoute

    castAsElement
        [|
            RX.View(
                styles = [| Styles.logo |],
                children =
                    [|
                        LC.Icon(icon = AppEggShellGallery.Icons.Icon.EggShell, styles = [| Styles.logoIcon |])
                        LC.Pressable(
                            onPress = nav.Go (maybeCurrentRoute, Home),
                            label = "Home",
                            testId = "topnav-logo-home",
                            overlay = true,
                            componentName = "Ui.TopNav"
                        )
                    |]
            )

            desktopHeading maybeCurrentActualRoute

            RX.View(
                styles = [| Styles.sampleVisualsScreenSize |],
                children =
                    [|
                        LC.Text("Visuals", styles = [| Styles.label |])

                        LC.ToggleButtons(
                            value =
                                LC.ToggleButtons.ExactlyOne(
                                    Some sampleVisualsScreenSize,
                                    fun value ->
                                        nav.SetSampleVisualsScreenSize maybeCurrentRoute value
                                        Telemetry.TrackEvent
                                            "TopNavScreenSizeToggleButtonPressed"
                                            ([ ("Value", value.ToString() |> box) ] |> Map.ofList)
                                ),
                            buttons =
                                fun group ->
                                    castAsElement
                                        [|
                                            LC.ToggleButton(
                                                value = ScreenSize.Desktop,
                                                style = LC.ToggleButton.Label "Desktop",
                                                group = group,
                                                position = LC.ToggleButton.Position.First
                                            )
                                            LC.ToggleButton(
                                                value = ScreenSize.Handheld,
                                                style = LC.ToggleButton.Label "Handheld",
                                                group = group,
                                                position = LC.ToggleButton.Position.Last
                                            )
                                        |]
                        )
                    |]
            )

            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Docs _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Docs "index.md")),
                style = NavTopItem.labelOnly "Docs"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Architecture _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Architecture "architecture/index.md")),
                style = NavTopItem.labelOnly "Architecture"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Modernization _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Modernization "modernization/index.md")),
                style = NavTopItem.labelOnly "Modernization"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Runbooks _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Runbooks "runbooks/index.md")),
                style = NavTopItem.labelOnly "Runbooks"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Accessibility _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Accessibility "accessibility/index.md")),
                style = NavTopItem.labelOnly "Accessibility"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function KnowledgeBase _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, KnowledgeBase "knowledge-base/index.md")),
                style = NavTopItem.labelOnly "Knowledge Base"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Tools _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Tools "tools/index.md")),
                style = NavTopItem.labelOnly "Tools"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Components _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Components Index)),
                style = NavTopItem.labelOnly "Components"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function HowTo _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, HowTo (HowToItem.Markdown "how-to/index.md"))),
                style = NavTopItem.labelOnly "How To"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Subject _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Subject "subject/index.md")),
                style = NavTopItem.labelOnly "Subject"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Design _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Design (DesignItem.Markdown "design/index.md"))),
                style = NavTopItem.labelOnly "Design"
            )
            LC.Nav.Top.Item(
                state = navItemState maybeCurrentActualRoute (function Legacy _ -> true | _ -> false) (nav.Go (maybeCurrentRoute, Legacy (LegacyItem.Markdown "legacy/index.md"))),
                style = NavTopItem.labelOnly "Legacy"
            )
        |]

let private handheldNav (_: unit) =
    castAsElement
        [|
            LR.Nav.Top.BackButton()
            LC.Nav.Top.Heading(text = "EggShell")
            LC.Nav.Top.ShowSidebarButton()
        |]

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member TopNav(
            ?maybeRoute:    Option<Route>,
            ?children:      ReactChildrenProp,
            ?key:           string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (maybeRoute, children, key, xLegacyStyles)

        LR.With.CurrentRoute(
            spec = routesSpec(),
            fn =
                fun maybeCurrentRoute ->
                    let maybeCurrentActualRoute = nav.CurrentActualRoute maybeCurrentRoute

                    LC.Nav.Top.Base(
                        desktop = (fun _ -> desktopNav maybeCurrentRoute maybeCurrentActualRoute),
                        handheld = handheldNav
                    )
        )
