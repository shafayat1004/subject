[<AutoOpen>]
module AppEggShellGallery.Components.Content_Accessibility_WithAccessibility

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery

type private Helpers =
    [<Component>]
    static member SettingsDisplay() : ReactElement =
        LC.With.Accessibility(fun settings ->
            element {
                LC.UiText $"Screen reader enabled: {settings.ScreenReaderEnabled}"
                LC.UiText $"Reduce motion: {settings.ReduceMotion}"
                LC.UiText $"Bold text / high contrast: {settings.BoldText}"
                LC.UiText $"Reduce transparency: {settings.ReduceTransparency}"
                LC.UiText $"Invert colors: {settings.InvertColors}"
                LC.UiText $"Grayscale: {settings.Grayscale}"
                LC.UiText $"Font scale: {settings.FontScale}"
            }
        )

type Ui.Content.Accessibility with
    [<Component>]
    static member WithAccessibility() : ReactElement =
        Ui.ComponentContent(
            displayName = "With.Accessibility",
            notes =
                LC.Text "LC.With.Accessibility subscribes to OS accessibility settings and re-renders when they change. Toggle settings in your OS (reduce motion, bold text, etc.) to see values update.",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.With.Accessibility",
                    role           = "provider (no visible role)",
                    namePattern    = "N/A — exposes AccessibilitySettings to children",
                    stateNotes     = "Reactive flags: ScreenReaderEnabled, ReduceMotion, BoldText, ReduceTransparency, InvertColors, Grayscale, FontScale",
                    scalesWithFont = false,
                    deferredTags =
                        [
                            "[web-only] prefers-reduced-motion, prefers-contrast, prefers-reduced-transparency, forced-colors via matchMedia"
                            "[native] AccessibilityInfo + PixelRatio on iOS/Android"
                        ]
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = Helpers.SettingsDisplay(),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.With.Accessibility(fun settings ->
    element {
        LC.UiText $"Reduce motion: {settings.ReduceMotion}"
        LC.UiText $"Font scale: {settings.FontScale}"
    }
)"""
                            )
                    )
                }
        )
