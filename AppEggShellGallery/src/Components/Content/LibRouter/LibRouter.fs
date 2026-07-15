[<AutoOpen>]
module AppEggShellGallery.Components.Content_LibRouter

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components
open LibRouter.Components.Constructors
open LibRouter.Components.With.Location
open LibRouter.Components.With.Route
open AppEggShellGallery.Navigation

type Ui.Content.LibRouter with
    [<Component>]
    static member Dialogs () : ReactElement =
        Ui.ComponentContent(
            displayName = "LR.Dialogs",
            props       = ComponentContent.ForFullyQualifiedName "LibRouter.Components.Dialogs",
            notes =
                element {
                    LC.Text "Renders the router dialog stack (resultless, resultful, ad-hoc, and system dialogs)."
                    LC.Text "In this gallery app it is wired once in App.fs inside LC.AppShell.Content.dialogs."
                },
            a11y =
                Ui.A11yPanel(
                    componentName  = "LR.Dialogs",
                    role           = "none (dialog stack renderer)",
                    namePattern    = "Each open dialog provides its own title and content",
                    stateNotes     = "Manages modal dialog stack; focus moves to top dialog",
                    scalesWithFont = true,
                    contrastNotes  = "Dialog content meets WCAG AA"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        heading = "Wired in AppShell",
                        visuals = LC.Text "Open a system dialog from the Dialogs page to see LR.Dialogs in action.",
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LR.Dialogs(
    nav,
    maybeNavigationFrame |> Option.map NavigationFrame.dialogs |> Option.getOrElse [],
    navigationState.DialogsState,
    makeResultless,
    makeResultful
)
"""
                            )
                    )
                }
        )

    [<Component>]
    static member LogRouteTransitions () : ReactElement =
        Ui.ComponentContent(
            displayName = "LR.LogRouteTransitions",
            props       = ComponentContent.ForFullyQualifiedName "LibRouter.Components.LogRouteTransitions",
            notes =
                element {
                    LC.Text "Invisible helper mounted at the app root. On each URL change it tracks a screen view and calls UiActionLog.setCurrentRoute."
                    LC.Text "In DEBUG builds, inspect window.__eggshell.AppEggShellGallery.uiLog() after navigating."
                },
            a11y =
                Ui.A11yPanel(
                    componentName  = "LR.LogRouteTransitions",
                    role           = "none (invisible route tracker)",
                    namePattern    = "N/A — renders nothing",
                    stateNotes     = "Logs route changes for analytics and debugging",
                    scalesWithFont = false,
                    contrastNotes  = "N/A — no visible UI"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        heading = "Mount at app root",
                        visuals = LC.Text "Navigate between gallery routes; this component renders nothing.",
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LR.LogRouteTransitions()
"""
                            )
                    )
                }
        )

    [<Component>]
    static member NativeBackButton () : ReactElement =
        Ui.ComponentContent(
            displayName = "LR.NativeBackButton",
            props       = ComponentContent.ForFullyQualifiedName "LibRouter.Components.NativeBackButton",
            notes =
                element {
                    LC.Text "React Native only. Registers a hardware back handler that calls the supplied goBack callback."
                    LC.Text "Must appear once in the tree (typically in AppContext). Duplicate LR.Router instances break back navigation."
                },
            a11y =
                Ui.A11yPanel(
                    componentName  = "LR.NativeBackButton",
                    role           = "none (hardware back handler)",
                    namePattern    = "N/A — native hardware back button",
                    stateNotes     = "Registers Android back handler; web is no-op",
                    scalesWithFont = false,
                    contrastNotes  = "N/A — no visible UI"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        heading = "Native hardware back",
                        visuals = LC.Text "Web gallery: no-op. See public-dev/docs/native/dev-experience.md.",
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LR.NativeBackButton(nav.GoBack)
"""
                            )
                    )
                }
        )

    [<Component>]
    static member WithLocation () : ReactElement =
        Ui.ComponentContent(
            displayName = "LR.With.Location",
            props       = ComponentContent.ForFullyQualifiedName "LibRouter.Components.With.Location",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LR.With.Location",
                    role           = "none (location provider)",
                    namePattern    = "Child render function displays location data",
                    stateNotes     = "Re-renders when URL location changes",
                    scalesWithFont = true,
                    contrastNotes  = "Child content contrast unchanged by wrapper"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        heading = "Current location",
                        visuals =
                            LR.With.Location(
                                ``with`` =
                                    fun location ->
                                        element {
                                            LC.Text "pathname:"
                                            LC.Pre location.pathname
                                            LC.Text "search:"
                                            LC.Pre location.search
                                            LC.Text "URL:"
                                            LC.Pre location.Url
                                        }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LR.With.Location (fun location ->
    element {
        LC.Text $"URL: {location.Url}"
    }
)
"""
                            )
                    )
                }
        )

    [<Component>]
    static member WithRoute () : ReactElement =
        Ui.ComponentContent(
            displayName = "LR.With.Route",
            props       = ComponentContent.ForFullyQualifiedName "LibRouter.Components.With.Route",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LR.With.Route",
                    role           = "none (route decoder provider)",
                    namePattern    = "Child render function displays decoded route",
                    stateNotes     = "Re-renders when navigation frame changes",
                    scalesWithFont = true,
                    contrastNotes  = "Child content contrast unchanged by wrapper"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        heading = "Decoded navigation frame",
                        visuals =
                            LR.With.Route(
                                spec = routesSpec(),
                                ``with`` =
                                    fun maybeFrame ->
                                        match maybeFrame with
                                        | None ->
                                            LC.InfoMessage(
                                                message = "Current location does not decode to a route.",
                                                level   = InfoMessage.Attention
                                            )
                                        | Some frame ->
                                            element {
                                                LC.Text "Route:"
                                                LC.Pre $"{frame.Route}"
                                                LC.Text $"Open dialogs: {List.length frame.Dialogs}"
                                            }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LR.With.Route(
    spec = routesSpec(),
    ``with`` = fun maybeFrame ->
        match maybeFrame with
        | None -> LC.Text "No route"
        | Some frame -> LC.Text $"{frame.Route}"
)
"""
                            )
                    )
                }
        )
