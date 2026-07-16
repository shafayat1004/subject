[<AutoOpen>]
module AppEggShellGallery.Components.Content_HorizontalPanArea

open Fable.React
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles
open AppEggShellGallery

[<RequireQualifiedAccess>]
module private Styles =
    let track =
        makeViewStyles {
            height 80
            JustifyContent.Center
            backgroundColor (Color.Grey "f0")
            borderRadius 12
            Overflow.Hidden
        }

    let handle =
        makeViewStyles {
            width 120
            height 56
            marginLeft 12
            AlignItems.Center
            JustifyContent.Center
            backgroundColor Color.DevBlue
            borderRadius 10
        }

    let handleText =
        makeTextStyles {
            color Color.White
            fontSize 14
        }

    let readout =
        makeTextStyles {
            fontSize 13
            color (Color.Grey "66")
        }

type private Helpers =
    [<Component>]
    static member LiveDrag() : ReactElement =
        // A box that follows the horizontal pan and springs back to centre on release.
        // On native, Rn.HorizontalPanArea arbitrates in the native gesture system
        // (react-native-gesture-handler), so a surrounding vertical scroll still works.
        let translateX = Reanimated.useSharedValue 0.0
        let animatedStyle = Reanimated.useAnimatedTranslateX translateX
        let offset = Hooks.useState 0.0

        let onStart () = ()

        let onUpdate (translationX: float) =
            translateX.SetValue translationX
            offset.update translationX

        let onEnd (_: float) =
            offset.update 0.0
            translateX.AnimateTiming(0.0, durationMs = 220.0)

        // The pannable track. HorizontalPanArea uses react-native-gesture-handler on
        // native, which needs a GestureHandlerRootView ancestor -- but the gallery must
        // NOT mount one app-wide (it would hijack the drawer's JS-responder pan). So the
        // root is scoped to this demo only, sized to content (fillParent=false) so it
        // does not stretch the sample. On web no root is needed.
        let track =
            Rn.View(
                styles = [| Styles.track |],
                children =
                    elements {
                        Rn.ReanimatedView(
                            animatedStyle = animatedStyle,
                            children =
                                elements {
                                    Rn.HorizontalPanArea(
                                        onStart       = onStart,
                                        onUpdate      = onUpdate,
                                        onEnd         = onEnd,
                                        activeOffsetX = 12.0,
                                        // Generous vertical tolerance so a quick, slightly
                                        // diagonal drag is not abandoned mid-swipe (the demo
                                        // has no surrounding vertical scroll to yield to).
                                        failOffsetY = 24.0,
                                        children =
                                            [|
                                                Rn.View(
                                                    styles   = [| Styles.handle |],
                                                    children = elements { LC.Text("Drag me ↔", styles = [| Styles.handleText |]) }
                                                )
                                            |]
                                    )
                                }
                        )
                    }
            )

        LC.Column(
            children =
                elements {
#if EGGSHELL_PLATFORM_IS_WEB
                    track
#else
                    Rn.GestureHandlerRootView(children = [| track |], fillParent = false)
#endif

                    LC.Text(sprintf "translationX: %.0f px" offset.current, styles = [| Styles.readout |])
                },
            gap = 10
        )

type Ui.Content with
    [<Component>]
    static member HorizontalPanArea() : ReactElement =
        Ui.ComponentContent(
            displayName  = "HorizontalPanArea",
            isResponsive = false,
            props =
                ComponentContent.Manual(
                    Ui.ComponentProps(data = {
                        Fields = (Choice2Of2 [
                            { Name = "onStart";       Type = "unit -> unit";             Default = None; Description = Some "Fired when the horizontal pan activates." }
                            { Name = "onUpdate";      Type = "float -> unit";            Default = None; Description = Some "Live translationX in px from the gesture start (negative = leftward)." }
                            { Name = "onEnd";         Type = "float -> unit";            Default = None; Description = Some "Final translationX when the gesture completes." }
                            { Name = "children";      Type = "array<ReactElement>";      Default = None; Description = None }
                            { Name = "activeOffsetX"; Type = "float";                    Default = Some "15.0"; Description = Some "How far horizontally a drag must travel before the swipe activates." }
                            { Name = "failOffsetY";   Type = "float";                    Default = Some "12.0"; Description = Some "How far vertically a drag may travel before the swipe is abandoned (yields to vertical scroll)." }
                        ])
                        MaybeScrapeErrors = None
                    })
                ),
            notes = LC.Text """Rn.HorizontalPanArea is the framework's horizontal-swipe primitive. On native it uses react-native-gesture-handler's PanGestureHandler with activeOffsetX/failOffsetY, so horizontal-vs-vertical arbitration happens in the native gesture system (a surrounding vertical ScrollView keeps working). On web it uses the GestureView responder path. Drive a Reanimated shared value (Reanimated.useSharedValue) via Rn.ReanimatedView for a follow-the-finger effect that runs on the UI thread; provide a non-gesture alternative (button/rotor action) for any destructive swipe action.""",
            a11y =
                Ui.A11yPanel(
                    componentName  = "Rn.HorizontalPanArea",
                    role           = "none (gesture container)",
                    namePattern    = "Child content provides the accessible name",
                    stateNotes     = "Swipe is a gesture; pair swipe actions with a visible button or an accessibilityAction so motor / screen-reader users have a non-gesture path (WCAG 2.5.1).",
                    scalesWithFont = true,
                    contrastNotes  = "Child content contrast unchanged by the gesture wrapper"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        samples =
                            element {
                                Ui.ComponentSample(
                                    heading = "Follow-the-finger drag (springs back)",
                                    visuals = Helpers.LiveDrag(),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
let translateX = Reanimated.useSharedValue 0.0
let animatedStyle = Reanimated.useAnimatedTranslateX translateX

Rn.ReanimatedView(
    animatedStyle = animatedStyle,
    children = elements {
        Rn.HorizontalPanArea(
            onStart  = (fun () -> ()),
            onUpdate = (fun translationX -> translateX.SetValue translationX),
            onEnd    = (fun _ -> translateX.AnimateTiming(0.0, durationMs = 220.0)),
            activeOffsetX = 12.0,
            failOffsetY   = 12.0,
            children = [| (* your row / handle *) |]
        )
    }
)
""")
                                )
                            }
                    )
                }
        )
