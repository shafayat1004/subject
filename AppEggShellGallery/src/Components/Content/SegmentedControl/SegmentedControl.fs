[<AutoOpen>]
module AppEggShellGallery.Components.Content_SegmentedControl

open Fable.React
open LibClient
open LibClient.Components

type private Mode =
| Light
| Dark

type private Helpers =
    [<Component>]
    static member ThemeSample() : ReactElement =
        let modeHook = Hooks.useState Light

        LC.SegmentedControl(
            accessibilityGroupLabel = "Theme",
            testId                  = "gallery-segmented-control",
            selected                = modeHook.current,
            onSelect                = modeHook.update,
            segments =
                [|
                    {
                        Label        = "Light"
                        Value        = Light
                        TestIdSuffix = Some "light"
                    }
                    {
                        Label        = "Dark"
                        Value        = Dark
                        TestIdSuffix = Some "dark"
                    }
                |],
            theme =
                (fun _ ->
                    {
                        TrackBackground      = Color.Hex "#eae0d9"
                        ThumbBackground      = Color.Hex "#2d4c4c"
                        SelectedLabelColor   = Color.White
                        UnselectedLabelColor = Color.Hex "#536174"
                        TrackWidth           = 152
                        TrackPadding         = 4
                    })
        )

type Ui.Content with
    [<Component>]
    static member SegmentedControl() : ReactElement =
        Ui.ComponentContent(
            displayName = "SegmentedControl",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.SegmentedControl",
            notes =
                LC.Text "Pill segmented control with sliding thumb, tap selection, and horizontal drag. Uses explicit pixel segment widths so labels stay in separate halves on web and native.",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.SegmentedControl",
                    role           = "radiogroup; each segment is role=radio with selected state",
                    namePattern    = "accessibilityGroupLabel names the group; segment labels name each option",
                    stateNotes     = "Selected segment exposes selected via accessibilityState",
                    scalesWithFont = true,
                    contrastNotes  = "Pass SelectedLabelColor / UnselectedLabelColor in theme for WCAG AA on thumb and track"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Two segments",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.ThemeSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.SegmentedControl(
    accessibilityGroupLabel = "Theme",
    selected = current,
    onSelect = onSelect,
    segments = [| { Label = "Light"; Value = Light; TestIdSuffix = Some "light" }; ... |],
    theme = (fun _ -> { TrackBackground = ...; ThumbBackground = ...; ... })
)"""
                                        )
                                )
                            }
                    )
                }
        )
