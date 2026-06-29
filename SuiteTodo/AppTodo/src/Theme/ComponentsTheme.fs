module AppTodo.ComponentsTheme

open AppTodo.Colors
open LibClient
open LibClient.ColorModule
open LibClient.Components

let applyTheme () : unit =
    LibClient.DefaultComponentsTheme.ApplyTheme.primarySecondary colors
    LibRouter.DefaultComponentsTheme.ApplyTheme.primarySecondary colors

/// Re-themes text inputs, pickers, and picker popups for the active appearance (light/dark).
/// Must run before inputs render so dark mode does not flash white fills.
let applyInputThemes (palette: SemanticPalette) : unit =
    Themes.Set<LC.Input.Text.Theme> {
        BorderLabelBlurredColor    = palette.TextSecondary
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
        BorderLabelColor        = palette.TextSecondary
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
    Themes.Set<LibClient.Components.Input.PickerInternals.Popup.Theme> {
        BackgroundColor         = palette.RowBackground
        BorderColor             = palette.InputBorder
        ItemTextColor           = palette.TextPrimary
        ItemTextHighlightColor  = palette.Accent
        ItemHighlightBackground = palette.AccentSoft
        ItemBorderColor         = palette.InputBorder
        SelectedIconColor       = palette.Accent
        BorderRadius            = 16
    }
    Themes.Set<LC.Dialog.Shell.WhiteRounded.Raw.Theme> {
        Width                   = None
        MaxSizeLimiterPadding   = Some 16
        WhiteRoundedBasePadding = Some 16
        BoundaryRadius          = None
        BackgroundColor         = Some palette.CardBackground
    }
