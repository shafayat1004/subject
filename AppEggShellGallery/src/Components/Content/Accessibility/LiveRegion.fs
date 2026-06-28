[<AutoOpen>]
module AppEggShellGallery.Components.Content_Accessibility_LiveRegion

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Button
open AppEggShellGallery

type private Helpers =
    [<Component>]
    static member AnnounceSample() : ReactElement =
        let count = Hooks.useState 0

        element {
            LC.Button(
                label = "Delete item",
                state =
                    PropStateFactory.MakeLowLevel (
                        Actionable (
                            fun _ ->
                                let next = count.current + 1
                                count.update next
                                LC.LiveRegion.announce
                                    (sprintf "%i item%s deleted" next (if next = 1 then "" else "s"))
                                    AccessibilityLiveRegion.Polite
                        )
                    )
            )

            LC.UiText (sprintf "Deleted count: %i (listen with screen reader after tapping Delete)" count.current)
        }

type Ui.Content.Accessibility with
    [<Component>]
    static member LiveRegion() : ReactElement =
        Ui.ComponentContent(
            displayName = "LiveRegion",
            notes =
                LC.Text "Use LC.LiveRegion.announce for one-off status messages (e.g. after delete). Mount LC.LiveRegion with ?liveRegion for persistent polite/assertive regions.",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.LiveRegion",
                    role = "live region (aria-live polite or assertive)",
                    namePattern = "Announcement text passed to LC.LiveRegion.announce or child content",
                    stateNotes = "Polite waits for pause; assertive interrupts immediately",
                    scalesWithFont = true,
                    deferredTags = [ "[rnw-blocked] full live-region parity on web pending RNW migration" ]
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = Helpers.AnnounceSample(),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Button(
    label = "Delete item",
    state = PropStateFactory.MakeLowLevel (
        Actionable (fun _ ->
            LC.LiveRegion.announce "1 item deleted" AccessibilityLiveRegion.Polite
        )
    )
)"""
                            )
                    )
                }
        )
