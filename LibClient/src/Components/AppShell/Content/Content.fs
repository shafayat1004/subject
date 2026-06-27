[<AutoOpen>]
module LibClient.Components.AppShell.Content

open System

open Fable.React
open Fable.Core.JsInterop

open LibClient
open LibClient.Components
open LibClient.Responsive

open ReactXP.Components
open ReactXP.Styles

type DesktopSidebarStyle =
| Fixed
| Popup

[<RequireQualifiedAccess>]
type SidebarVisibilityEvent =
| Set    of IsVisible: bool * Event: ReactEvent.Action
| Toggle of Event: ReactEvent.Action

let private sidebarVisibilityQueue: LibClient.EventBus.Queue<SidebarVisibilityEvent> =
    LibClient.EventBus.Queue "sidebarVisibility"

let setSidebarVisibility (isVisible: bool) (e: ReactEvent.Action) : unit =
    LibClient.UiActionLog.record {
        Kind = if isVisible then LibClient.UiActionLog.UiActionKind.SidebarOpen else LibClient.UiActionLog.UiActionKind.SidebarClose
        TestId = Some "eggshell-sidebar-menu"
        Label = Some (if isVisible then "Open sidebar" else "Close sidebar")
        ComponentName = Some "LC.AppShell.Content"
        Detail = Map.empty
    }
    SidebarVisibilityEvent.Set (isVisible, e)
    |> LibClient.ServiceInstances.services().EventBus.Broadcast sidebarVisibilityQueue

let toggleSidebarVisibility (e: ReactEvent.Action) : unit =
    SidebarVisibilityEvent.Toggle e
    |> LibClient.ServiceInstances.services().EventBus.Broadcast sidebarVisibilityQueue

[<RequireQualifiedAccess>]
module private Styles =
    let safeInsetsView =
        makeViewStyles {
            flex 1
            backgroundColor Color.White
        }

    let view =
        makeViewStyles {
            flex 1
        }

    let topNavBlock =
        makeViewStyles {
            Overflow.Hidden
        }

    let bottomNavBlock =
        makeViewStyles {
            Overflow.VisibleForDropShadow
        }

    let sidebarAndContentBlock =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Stretch
            flex 1
        }

    let contentBlock =
        makeViewStyles {
            flex 1
        }

    let sidebarBlockDesktop =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Stretch
            flex 0
        }

    let sidebarDraggableLegacyStyles (width: int) =
        ReactXP.LegacyStyles.Designtime.processDynamicStyles [|
            ReactXP.LegacyStyles.RulesBasic.Position.Absolute
            ReactXP.LegacyStyles.RulesBasic.top 0
            ReactXP.LegacyStyles.RulesBasic.bottom 0
            ReactXP.LegacyStyles.RulesBasic.left 0
            ReactXP.LegacyStyles.RulesBasic.width width
        |]

    let sidebarWrapper =
        makeViewStyles {
            Position.Absolute
            top 0
            bottom 0
            paddingRight 10
        }

    let scrim =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

    let sidebarPopupWrapper =
        makeViewStyles {
            borderBottom 1 (Color.Grey "cc")
            shadow (Color.BlackAlpha 0.3) 4 (0, 1)
            maxHeight 750
        }

    let topNavShadow =
        makeViewStyles {
            Position.Absolute
            flex 0
            right 0
            left 0
            height 3
            marginTop -3
            shadow (Color.BlackAlpha 0.2) 3 (0, 2)
            borderBottom 1 (Color.Grey "cc")
        }

[<RequireQualifiedAccess>]
module private Actions =
    let onSidebarDraggableChange (isSidebarScrimVisibleHook: IStateHook<bool>) (change: LibClient.Components.Draggable.Change) : unit =
        match change with
        | LibClient.Components.Draggable.Change.DragInducedAnimationStarted LibClient.Components.Draggable.Position.Right
        | LibClient.Components.Draggable.Change.PositionChanged (LibClient.Components.Draggable.Position.Right, LibClient.Components.Draggable.PositionChangeReason.ManualDrag) ->
            isSidebarScrimVisibleHook.update true
        | LibClient.Components.Draggable.Change.DragInducedAnimationStarted LibClient.Components.Draggable.Position.Base
        | LibClient.Components.Draggable.Change.PositionChanged (LibClient.Components.Draggable.Position.Base, LibClient.Components.Draggable.PositionChangeReason.ManualDrag) ->
            isSidebarScrimVisibleHook.update false
        | _ -> Noop

    let onError (onError: (System.Exception * (unit -> unit)) -> ReactElement) (error: System.Exception, retry: unit -> unit) : ReactElement =
        Log.Error ("AppShell.Content error boundary: {Error}", error)
        onError (error, retry)

type LibClient.Components.Constructors.LC.AppShell with
    [<Component>]
    static member Content(
            desktopSidebarStyle: DesktopSidebarStyle,
            sidebar: ReactElement,
            content: ReactElement,
            dialogs: ReactElement,
            onError: (System.Exception * (unit -> unit)) -> ReactElement,
            ?children: ReactChildrenProp,
            ?topStatus: ReactElement,
            ?topNav: ReactElement,
            ?bottomNav: ReactElement,
            ?key: string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        children |> ignore

        let topStatus = defaultArg topStatus noElement
        let topNav = defaultArg topNav noElement
        let bottomNav = defaultArg bottomNav noElement

        let legacySafeInsetsViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let isSidebarScrimVisibleHook = Hooks.useState false
        let sidebarPopupConnectorHook = Hooks.useStateLazy (fun () -> LibClient.Components.Popup.Connector())
        let maybeSidebarDraggableHook = Hooks.useState None

        let maybeSidebarDraggableRef = Hooks.useRef None
        maybeSidebarDraggableRef.current <- maybeSidebarDraggableHook.current

        let desktopSidebarStyleRef = Hooks.useRef desktopSidebarStyle
        desktopSidebarStyleRef.current <- desktopSidebarStyle

        Hooks.useEffectDisposable(
            (fun () ->
                let popupConnector = sidebarPopupConnectorHook.current

                popupConnector.OnDismiss (fun () ->
                    async {
                        // ReactXP's popup system has an interesting way of handling off-clicks.
                        // They hide the popup, but they also let the click do its thing. The
                        // consequence of that is that the button that's set to "toggleSidebarVisibility"
                        // will call the toggle function _after_ the OnDismiss callback is called, which
                        // means that instead of hiding the open popup, it would open it again, because the
                        // click itself hid the popup.
                        // We could wire this more robustly, by completely getting rid of the "toggle" functionality
                        // and always using "set", but I'm fairly convinced that adding this delay here
                        // solves our problem without the need for rewiring. So in the interest of
                        // saving time and moving on to other work, we try this delay.
                        do! Async.Sleep (TimeSpan.FromMilliseconds 300.0)
                        isSidebarScrimVisibleHook.update false
                    } |> startSafely
                )

                let subscription =
                    LibClient.ServiceInstances.services().EventBus.On sidebarVisibilityQueue (fun sidebarVisibilityEvent ->
                        let (isVisible, e) =
                            match sidebarVisibilityEvent with
                            | SidebarVisibilityEvent.Set (isVisible, e) -> (isVisible, e)
                            | SidebarVisibilityEvent.Toggle e ->
                                (not isSidebarScrimVisibleHook.current, e)

                        isSidebarScrimVisibleHook.update isVisible

                        if desktopSidebarStyleRef.current = Popup then
                            match isVisible with
                            | true  -> popupConnector.Show (e.MaybeSource |> Option.getOrElseRaise (exn "Can't open a popup without an anchor"))
                            | false -> popupConnector.Hide ()

                        maybeSidebarDraggableRef.current |> Option.sideEffect (fun (sidebarDraggable: LibClient.Components.Draggable.IDraggableRef) ->
                            let position =
                                match isVisible with
                                | true  -> LibClient.Components.Draggable.Position.Right
                                | false -> LibClient.Components.Draggable.Position.Base
                            sidebarDraggable.SetPosition position |> ignore
                        )
                    )

                { new IDisposable with
                    member _.Dispose() =
                        subscription.Off ()
                }
            ),
            [||]
        )

        let refSidebarDraggable (nullableInstance: LibClient.JsInterop.JsNullable<LibClient.Components.Draggable.IDraggableRef>) =
            maybeSidebarDraggableHook.update (nullableInstance.ToOption)

        let renderOptionalBlock (block: ReactElement) (styles: array<ViewStyles>) =
            if block = noElement then
                noElement
            else
                RX.View(styles = styles, children = [| block |])

        let renderTopNavShadow () =
            RX.View(styles = [| Styles.topNavShadow |])

        let renderContentBlock (contentElement: ReactElement) (includeTopNavShadow: bool) =
            RX.View(
                testId = "eggshell-app-content",
                styles = [| Styles.contentBlock |],
                children =
                    elements {
                        contentElement
                        if includeTopNavShadow then
                            renderTopNavShadow ()
                    }
            )

        let renderHandheldSidebar () =
            if sidebar = noElement then
                noElement
            else
                LC.With.Layout(
                    ``with`` =
                        fun (onLayoutOption, maybeLayout) ->
                            let width = maybeLayout |> Option.map (fun layout -> layout.Width) |> Option.getOrElse 300

                            element {
                                LC.Scrim(
                                    isVisible = isSidebarScrimVisibleHook.current,
                                    onPress = setSidebarVisibility false,
                                    ?onPanHorizontal = (maybeSidebarDraggableHook.current |> Option.map (fun draggable -> draggable.OnPanHorizontal)),
                                    styles = [| Styles.scrim |]
                                )
                                LC.Draggable(
                                    ref = refSidebarDraggable,
                                    onChange = Actions.onSidebarDraggableChange isSidebarScrimVisibleHook,
                                    baseOffset = (-width + 10, 0),
                                    right = {| ForwardThreshold = 30; Offset = width - 10; BackwardThreshold = 50 |},
                                    xLegacyStyles = Styles.sidebarDraggableLegacyStyles width,
                                    children =
                                        elements {
                                            RX.View(
                                                ?onLayout = onLayoutOption,
                                                styles = [| Styles.sidebarWrapper |],
                                                children = [| sidebar |]
                                            )
                                        }
                                )
                            }
                )

        let renderShell (sidebarArea: ReactElement) =
            RX.View(
                useSafeInsets = true,
                styles = [| Styles.safeInsetsView; yield! legacySafeInsetsViewStyles |],
                children =
                    elements {
                        RX.View(
                            styles = [| Styles.view |],
                            children =
                                elements {
                                    renderOptionalBlock topStatus [||]
                                    renderOptionalBlock topNav [| Styles.topNavBlock |]
                                    sidebarArea
                                    renderOptionalBlock bottomNav [| Styles.bottomNavBlock |]
                                }
                        )
                        dialogs
                    }
            )

        LC.ErrorBoundary(
            catch = Actions.onError onError,
            ``try`` =
                LC.Responsive(
                    desktop =
                        (fun _ ->
                            renderShell (
                                element {
                                    RX.View(
                                        styles = [| Styles.sidebarAndContentBlock |],
                                        children =
                                            elements {
                                                if desktopSidebarStyle = Fixed then
                                                    RX.View(
                                                        styles = [| Styles.sidebarBlockDesktop |],
                                                        children = [| sidebar |]
                                                    )
                                                renderContentBlock content false
                                                renderTopNavShadow ()
                                            }
                                    )
                                    if desktopSidebarStyle = Popup then
                                        LC.Popup(
                                            connector = sidebarPopupConnectorHook.current,
                                            render =
                                                fun () ->
                                                    RX.View(
                                                        styles = [| Styles.sidebarPopupWrapper |],
                                                        children = [| sidebar |]
                                                    )
                                        )
                                }
                            )),
                    handheld =
                        (fun _ ->
                            renderShell (
                                element {
                                    renderContentBlock content true
                                    renderHandheldSidebar ()
                                }
                            ))
                )
        )
