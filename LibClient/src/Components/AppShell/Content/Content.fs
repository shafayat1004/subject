[<AutoOpen>]
module LibClient.Components.AppShell_Content

open System

open Fable.React
open Fable.Core.JsInterop

open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Responsive

open Rn.Components
open Rn.Styles

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
        Kind          = if isVisible then LibClient.UiActionLog.UiActionKind.SidebarOpen else LibClient.UiActionLog.UiActionKind.SidebarClose
        TestId        = Some "eggshell-sidebar-menu"
        Label         = Some (if isVisible then "Open sidebar" else "Close sidebar")
        ComponentName = Some "LC.AppShell.Content"
        Detail        = Map.empty
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

    // The handheld sidebar overlays the content as a full-height drawer anchored to the left
    // edge. It hides/shows purely through the Draggable's animated translateX: baseOffset
    // parks it at (-width + 10) (fully off-screen, leaving a 10px transparent grab strip) and
    // opening animates translateX to 0. The drawer width is measured at runtime
    // (LC.With.Layout on the sidebarWrapper below) so it adapts to the sidebar content.
    //
    // Both this wrapper and sidebarWrapper are absolutely positioned with top:0/bottom:0 so the
    // drawer gets a definite full-viewport height -- the inner Sidebar ScrollView needs a bounded
    // height to overflow and scroll its (long) contents. The width measurement is reliable now
    // that the RNW View seam adapts react-native's onLayout event shape (see RnPrimitives.assignOnLayout);
    // previously e.width/e.height were read off the wrong event object and came back undefined (0),
    // which collapsed the drawer offset and left it stuck on-screen.
    let sidebarDraggableStyles: int -> ViewStyles =
        ViewStyles.Memoize (fun (itemWidth: int) ->
            makeViewStyles {
                Position.Absolute
                top 0
                bottom 0
                left 0
                width itemWidth
                Overflow.Visible
            }
        )

    let sidebarWrapper =
        makeViewStyles {
            Position.Absolute
            top 0
            bottom 0
            paddingRight 10
        }

    // The handheld drawer is anchored at top:0, so without this its first item underlaps the status
    // bar. Pad its top by the device inset and fill that strip with the drawer's white background so
    // the content starts below the status bar. Zero on web (SafeArea.useInsets returns 0 there).
    let sidebarTopInset =
        ViewStyles.Memoize (fun (insetTop: int) ->
            makeViewStyles {
                paddingTop      insetTop
                backgroundColor Color.White
            }
        )

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
            sidebar:             ReactElement,
            content:             ReactElement,
            dialogs:             ReactElement,
            onError:             (System.Exception * (unit -> unit)) -> ReactElement,
            ?children:           ReactChildrenProp,
            ?topStatus:          ReactElement,
            ?topNav:             ReactElement,
            ?bottomNav:          ReactElement,
            ?key:                string,
            ?xLegacyStyles:      List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key      |> ignore
        children |> ignore

        Hooks.useEffect(
            (fun () -> LibClient.FocusVisibleStyles.injectIfNeeded ()),
            [||]
        )

        let topStatus = defaultArg topStatus noElement
        let topNav = defaultArg topNav noElement
        let bottomNav = defaultArg bottomNav noElement

        let legacySafeInsetsViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        // Device safe-area insets (status bar / notch); zero on web. Used to inset the handheld drawer top.
        let insets = SafeArea.useInsets ()

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
                        // Rn's popup system has an interesting way of handling off-clicks.
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

        let renderOptionalBlock (block: ReactElement) (styles: array<ViewStyles>) (role: AccessibilityRole option) (navLabel: string option) =
            if block = noElement then
                noElement
            else
                Rn.View(
                    styles              = styles,
                    ?accessibilityRole  = role,
                    ?accessibilityLabel = navLabel,
                    children            = [| block |]
                )

        let renderTopNavShadow () =
            Rn.View(styles = [| Styles.topNavShadow |])

        let renderContentBlock (contentElement: ReactElement) (includeTopNavShadow: bool) =
            Rn.View(
                testId            = "eggshell-app-content",
                accessibilityRole = AccessibilityRole.Main,
                styles            = [| Styles.contentBlock |],
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
                                    isVisible        = isSidebarScrimVisibleHook.current,
                                    onPress          = setSidebarVisibility false,
                                    ?onPanHorizontal = (maybeSidebarDraggableHook.current |> Option.map (fun draggable -> draggable.OnPanHorizontal)),
                                    styles           = [| Styles.scrim |]
                                )
                                LC.Draggable(
                                    draggableRef = refSidebarDraggable,
                                    onChange     = Actions.onSidebarDraggableChange isSidebarScrimVisibleHook,
                                    // Park the closed drawer fully off-screen (translateX(-width)), not
                                    // at -width+10. The old 10px "peek" was a touch edge-swipe affordance,
                                    // but on web it left the GestureView's on-screen sliver overlaying the
                                    // content's left edge; because that sliver is a sibling subtree (not an
                                    // ancestor of the content ScrollView), mouse-wheel events over it could
                                    // not scroll the content beneath. Fully hiding makes the closed drawer
                                    // inert; it opens via the hamburger and closes via the scrim.
                                    baseOffset = (-width, 0),
                                    right      = {| ForwardThreshold = 30; Offset = width; BackwardThreshold = 50 |},
                                    styles     = [| Styles.sidebarDraggableStyles width |],
                                    children =
                                        elements {
                                            Rn.View(
                                                ?onLayout = onLayoutOption,
                                                styles =
                                                    [|
                                                        Styles.sidebarWrapper
                                                        if insets.Top > 0 then Styles.sidebarTopInset insets.Top
                                                    |],
                                                children = [| sidebar |]
                                            )
                                        }
                                )
                            }
                )

        let renderShell (sidebarArea: ReactElement) =
            // The top safe-area inset is applied by the header itself (LC.Nav.Top.Base) so the
            // coloured bar fills the status-bar strip; the shell root must NOT inset (that would
            // double-inset / leave a blank strip). See Rn.SafeArea + Nav/Top/Base.
            Rn.View(
                styles = [| Styles.safeInsetsView; yield! legacySafeInsetsViewStyles |],
                children =
                    elements {
                        LC.SkipLink()
                        Rn.View(
                            styles = [| Styles.view |],
                            children =
                                elements {
                                    renderOptionalBlock topStatus [||] None None
                                    renderOptionalBlock topNav [| Styles.topNavBlock |] (Some AccessibilityRole.Navigation) (Some "Top navigation")
                                    sidebarArea
                                    renderOptionalBlock bottomNav [| Styles.bottomNavBlock |] (Some AccessibilityRole.Navigation) (Some "Bottom navigation")
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
                                    Rn.View(
                                        styles = [| Styles.sidebarAndContentBlock |],
                                        children =
                                            elements {
                                                if desktopSidebarStyle = Fixed then
                                                    Rn.View(
                                                        styles   = [| Styles.sidebarBlockDesktop |],
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
                                                    Rn.View(
                                                        styles   = [| Styles.sidebarPopupWrapper |],
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
