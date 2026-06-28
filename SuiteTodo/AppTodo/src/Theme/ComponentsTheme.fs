module AppTodo.ComponentsTheme

open AppTodo.Colors
open LibClient
open LibClient.ColorModule
open LibClient.Components

let applyTheme () : unit =
    LibClient.DefaultComponentsTheme.ApplyTheme.primarySecondary colors
    LibRouter.DefaultComponentsTheme.ApplyTheme.primarySecondary colors

/// Re-themes text inputs and pickers for the active appearance (light/dark).
/// Must run before inputs render so dark mode does not flash white fills.
let applyInputThemes (palette: SemanticPalette) : unit =
    Themes.Set<LC.Input.Text.Theme> {
        BorderLabelBlurredColor    = palette.InputBorder
        BorderLabelFocusedColor    = palette.Accent
        BorderLabelInvalidColor    = palette.Danger
        TextColor                  = palette.TextPrimary
        NoneditableTextColor       = palette.TextMuted
        NoneditableBackgroundColor = palette.ChipBackground
        EditableBackgroundColor    = palette.InputBackground
        LabelBackgroundColor       = palette.FormBackground
        InvalidReasonColor         = palette.Danger
        PlaceholderColor           = palette.TextMuted
        TheVerticalPadding         = 12
        BorderRadius               = 16
    }
    Themes.Set<LibClient.Components.Input.PickerInternals.Field.Theme> {
        BorderLabelColor        = palette.InputBorder
        BorderLabelFocusColor   = palette.Accent
        BorderLabelInvalidColor = palette.Danger
        TextColor               = palette.TextPrimary
        InvalidReasonColor      = palette.Danger
        PlaceholderColor        = palette.TextMuted
        IconSize                = 20
        TheVerticalPadding      = 12
        BackgroundColor         = palette.InputBackground
        BorderRadius            = 16
        LabelBackgroundColor    = palette.FormBackground
    }
