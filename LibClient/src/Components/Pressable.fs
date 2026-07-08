[<AutoOpen>]
module LibClient.Components.Pressable

open Fable.React
open Fable.Core.JsInterop
open Browser.Types
open LibClient
open LibClient.Accessibility
open LibClient.AccessibilityHelpers
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let overlayContainer =
        makeViewStyles {
            Position.Relative
        }

    let overlayFill =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

    let overlayButton =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            widthPercent 100
            heightPercent 100
        }

type private Props = {
    OnPress: ReactEvent.Action -> unit
    A11y: A11yProps
    Disabled: bool
    MaybePointerState: LC.Pointer.State.PointerState option
    MaybeStyles: ViewStyles array option
    MaybeOverlayStyles: ViewStyles array option
    MaybeChildren: ReactElement array option
    Overlay: bool
    RegistryKey: string option
    ComponentName: string
    key: string option
}

type private PressableComponent(initialProps: Props) =
    inherit PureStatelessComponent<Props>(initialProps)

    let mutable maybeTimeoutReference: int option = None
    let mutable maybePressInCoords: (float * float) option = None

    let registryKey (props: Props) =
        props.RegistryKey
        |> Option.defaultWith (fun () ->
            props.A11y.TestId
            |> Option.defaultWith (fun () ->
                props.A11y.Label |> Option.defaultValue (System.Guid.NewGuid().ToString())))

    let stateMap (props: Props) =
        let s = props.A11y.State
        [
            yield! (s.Disabled |> Option.map (fun v -> ("disabled", string v)) |> Option.toList)
            yield! (s.Selected |> Option.map (fun v -> ("selected", string v)) |> Option.toList)
            yield! (s.Checked |> Option.map (fun v -> ("checked", string v)) |> Option.toList)
            yield! (s.Expanded |> Option.map (fun v -> ("expanded", string v)) |> Option.toList)
            yield! (s.Busy |> Option.map (fun v -> ("busy", string v)) |> Option.toList)
        ]
        |> Map.ofList

    let onPressIn
            (maybePointerState: LC.Pointer.State.PointerState option)
            (e: PointerEvent) =
        maybePressInCoords <- e.CrossPlatformPageXY
        maybePointerState |> Option.iter (fun ps -> ps.SetIsDepressed true e)

    // The actual press effect: dismiss the keyboard, settle the hover/depressed state, log the
    // interaction, and invoke the caller's OnPress. Shared by the native and web entry points below.
    let firePress (source: ReactElement) (props: Props) (e: PointerEvent) =
        Rn.UserInterface.dismissKeyboard()
        if (e.cancelable && Rn.Runtime.isWeb()) || Rn.Runtime.isNative() then
            e.stopPropagation()
        props.MaybePointerState
        |> Option.iter (fun pointerState ->
            pointerState.SetIsHovered false e
            maybeTimeoutReference <-
                Some (Fable.Core.JS.setTimeout (fun () -> pointerState.SetIsDepressed false e) 1500))
        let action = (ReactEvent.Pointer.OfBrowserEvent e).WithSource source |> ReactEvent.Action.Make
        UiActionLog.record {
            Kind = UiActionLog.UiActionKind.Press
            TestId = props.A11y.TestId
            Label = props.A11y.Label
            ComponentName = Some props.ComponentName
            Detail = Map.empty
        }
        props.OnPress action

    // Native press path. RN's `onPress` fires ONLY for a completed tap -- when an ancestor
    // ScrollView claims the touch (a scroll), RN cancels the press and never calls `onPress`. So we
    // drive the press from here on native. The old path fired from `onPressOut`, which RN calls even
    // on a scroll-cancelled press, and RN reports its coordinates at the press-DOWN point, so the
    // pressIn/pressOut movement guard saw ~0px and could not tell a scroll from a tap -- scrolling a
    // list fired the item under the finger (RW8 defect 3). `onPressOut` still resets the visual
    // pressed state on native, but no longer fires the press.
    let onPress
            (source: ReactElement)
            (props: Props)
            (e: PointerEvent) =
        if not props.Disabled && Rn.Runtime.isNative() then
            firePress source props e

    let onPressOut
            (source: ReactElement)
            (props: Props)
            (e: PointerEvent) =
        if props.Disabled then ()
        else
            props.MaybePointerState |> Option.iter (fun ps -> ps.SetIsDepressed false e)

            // Web keeps the historical onPressOut path with a small movement guard (react-native-web
            // has no scroll-cancel on onPressOut, and this preserves web click semantics). Native
            // fires from `onPress` above, so onPressOut only resets the pressed state there.
            if Rn.Runtime.isWeb() then
                let isDrag =
                    match (maybePressInCoords, e.CrossPlatformPageXY) with
                    | Some (px, py), Some (x, y) ->
                        let dx = x - px
                        let dy = y - py
                        dx * dx + dy * dy |> sqrt > 5.
                    | _ -> false
                maybePressInCoords <- None
                if not isDrag then
                    firePress source props e
            else
                maybePressInCoords <- None

    let buttonStyles (props: Props) =
        [|
            if props.Overlay then Styles.overlayButton
            yield! (props.MaybeOverlayStyles |> Option.defaultValue [||])
            yield! (props.MaybeStyles |> Option.defaultValue [||])
        |]

    member private this.RenderButton (styles: ViewStyles array) (children: ReactElement array) =
        let props = this.props
        let __props = createEmpty
        AccessibilityHelpers.applyToProps __props props.A11y props.Disabled
        __props?style <- styles
        __props?onPress <- onPress this props
        __props?onPressIn <- onPressIn props.MaybePointerState
        __props?onPressOut <- onPressOut this props
        props.MaybePointerState
        |> Option.iter (fun ps ->
            __props?onHoverStart <- ps.SetIsHovered true
            __props?onHoverEnd <- ps.SetIsHovered false)
        __props?onLayout <- ignore
        __props?disableTouchOpacityAnimation <- false
        Fable.React.ReactBindings.React.createElement(
            Rn.RnPrimitives.Pressable,
            __props,
            ThirdParty.fixPotentiallySingleChild children
        )

    override this.componentDidMount () =
        let props = this.props
        UiActionLog.registerInteractive
            (registryKey props)
            props.A11y.TestId
            props.A11y.Label
            (Some (roleName props.A11y.Role))
            (Some props.ComponentName)
            true
            (stateMap props)

    override this.componentWillUnmount () =
        UiActionLog.unregisterInteractive (registryKey this.props)
        maybeTimeoutReference |> Option.iter Fable.Core.JS.clearTimeout

    override this.render () =
        let props = this.props
        let children = props.MaybeChildren |> Option.defaultValue [||]

        if props.Overlay then
            if Array.isEmpty children then
                Rn.View(
                    styles =
                        [|
                            Styles.overlayFill
                            yield! (props.MaybeStyles |> Option.defaultValue [||])
                        |],
                    children =
                        [|
                            if not props.Disabled then
                                this.RenderButton (buttonStyles props) [||]
                        |]
                )
            else
                Rn.View(
                    styles =
                        [|
                            Styles.overlayContainer
                            yield! (props.MaybeStyles |> Option.defaultValue [||])
                        |],
                    children =
                        [|
                            yield! children
                            if not props.Disabled then
                                this.RenderButton (buttonStyles props) [||]
                        |]
                )
        else
            this.RenderButton (buttonStyles props) children

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Pressable(
            onPress: ReactEvent.Action -> unit,
            ?label: string,
            ?role: AccessibilityRole,
            ?state: AccessibilityStateRecord,
            ?testId: string,
            ?accessibilityId: string,
            ?importantForAccessibility: LibClient.Accessibility.ImportantForAccessibility,
            ?liveRegion: LibClient.Accessibility.AccessibilityLiveRegion,
            ?tabIndex: int,
            ?actions: string list,
            ?disabled: bool,
            ?pointerState: LC.Pointer.State.PointerState,
            ?overlay: bool,
            ?styles: ViewStyles array,
            ?overlayStyles: ViewStyles array,
            ?registryKey: string,
            ?componentName: string,
            ?children: ReactChildrenProp,
            ?key: string
        ) : ReactElement =
        let a11y = {
            A11yProps.defaults with
                Label = label
                Role = defaultArg role AccessibilityRole.Button
                State = defaultArg state AccessibilityStateRecord.empty
                TestId = testId
                AccessibilityId = accessibilityId
                ImportantForAccessibility = importantForAccessibility
                LiveRegion = liveRegion
                TabIndex = tabIndex
                Actions = defaultArg actions []
        }
        let props = {
            OnPress = onPress
            A11y = a11y
            Disabled = defaultArg disabled false
            MaybePointerState = pointerState
            MaybeStyles = styles
            MaybeOverlayStyles = overlayStyles
            MaybeChildren = children
            Overlay = defaultArg overlay false
            RegistryKey = registryKey
            ComponentName = defaultArg componentName "LC.Pressable"
            key = key
        }
        Fable.React.Helpers.ofType<PressableComponent, _, _> props Seq.empty
