module LibClient.DefaultComponentsTheme


open ReactXP.LegacyStyles
open LibClient
open LibClient.Components
open LibClient.ColorModule
open LibClient.ColorScheme
open LibClient.Responsive

type RFontWeight = ReactXP.Styles.RulesRestricted.FontWeight

let private applyTheme (primary: Variants) (secondary: Variants) (neutral: Variants) (attention: Variants) (caution: Variants) : unit =
    let defaultIconSize = 32

    Themes.Set<LC.Avatar.Theme> {
        Size = 60
    }

    Themes.Set<LC.Text.Theme> {
        FontFamily = "sans-serif"
    }

    Themes.Set<LC.UiText.Theme> {
        FontFamily = "sans-serif"
    }

    Themes.Set<LC.HeaderCell.Theme> {
        FontColor = Color.Grey "45"
        FontSize = 18
    }

    Themes.Set<LC.Badge.Theme> {
        FontSize        = 14
        FontWeight      = ReactXP.Styles.RulesRestricted.FontWeight.Normal
        FontColor       = Color.White
        BackgroundColor = caution.Main
    }

    Themes.Set<LC.IconWithBadge.Theme> {
        IconColor       = Color.DevRed
        IconSize        = 16
        BadgeMarginLeft = 4
        Badge = {
            FontSize        = 14
            FontWeight      = ReactXP.Styles.RulesRestricted.FontWeight.Normal
            FontColor       = Color.White
            BackgroundColor = caution.Main
        }
    }

    Themes.Set<LC.Button.Theme>(
        {
            IconSize         = defaultIconSize
            DesktopLabelFontSize  = 20
            HandheldLabelFontSize = 14
            DesktopHeight    = 46
            HandheldHeight   = 38
            Primary = {
                Actionable = { TextColor = Color.White; BorderColor = secondary.Main; BackgroundColor = secondary.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                Disabled   = { TextColor = Color.White; BorderColor = secondary.Main; BackgroundColor = secondary.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                InProgress = { TextColor = Color.White; BorderColor = secondary.Main; BackgroundColor = secondary.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
            }
            Secondary = {
                Actionable = { TextColor = secondary.Main; BorderColor = secondary.Main; BackgroundColor = Color.White; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                Disabled   = { TextColor = secondary.Main; BorderColor = secondary.Main; BackgroundColor = Color.White; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                InProgress = { TextColor = secondary.Main; BorderColor = secondary.Main; BackgroundColor = Color.White; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
            }
            PrimaryB = {
                Actionable = { TextColor = Color.White; BorderColor = secondary.Main; BackgroundColor = secondary.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                Disabled   = { TextColor = Color.White; BorderColor = secondary.Main; BackgroundColor = secondary.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                InProgress = { TextColor = Color.White; BorderColor = secondary.Main; BackgroundColor = secondary.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
            }
            SecondaryB = {
                Actionable = { TextColor = secondary.Main; BorderColor = secondary.Main; BackgroundColor = Color.White; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                Disabled   = { TextColor = secondary.Main; BorderColor = secondary.Main; BackgroundColor = Color.White; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                InProgress = { TextColor = secondary.Main; BorderColor = secondary.Main; BackgroundColor = Color.White; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
            }
            Tertiary = {
                Actionable = { TextColor = secondary.Main; BorderColor = Color.Transparent; BackgroundColor = Color.Transparent; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                Disabled   = { TextColor = secondary.Main; BorderColor = Color.Transparent; BackgroundColor = Color.Transparent; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                InProgress = { TextColor = secondary.Main; BorderColor = Color.Transparent; BackgroundColor = Color.Transparent; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
            }
            Cautionary = {
                Actionable = { TextColor = Color.White; BorderColor = caution.Main; BackgroundColor = caution.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                Disabled   = { TextColor = Color.White; BorderColor = caution.Main; BackgroundColor = caution.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
                InProgress = { TextColor = Color.White; BorderColor = caution.Main; BackgroundColor = caution.Main; FontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal }
            }
        }
    )

    Themes.Set<LC.DateSelector.Theme> {
        HeaderBackgroundColor       = primary.Main
        SelectedDateBackgroundColor = primary.B100
    }

    Themes.Set<LC.TextButton.Theme>(
        {
            Primary = {
                Actionable = {
                    TextColor = secondary.Main
                    FontSize = 14
                    Opacity = 1.0
                }
                Disabled = {
                    TextColor = neutral.B500
                    FontSize = 14
                    Opacity = 0.5
                }
                InProgress = {
                    TextColor = neutral.B500
                    FontSize = 14
                    Opacity = 1.0
                }
            }
            Secondary = {
                Actionable = {
                    TextColor = secondary.Main
                    FontSize = 10
                    Opacity = 1.0
                }
                Disabled = {
                    TextColor = secondary.Main
                    FontSize = 10
                    Opacity = 0.5
                }
                InProgress = {
                    TextColor = secondary.Main
                    FontSize = 10
                    Opacity = 1.0
                }
            }
        }
    )

    Themes.Set<LC.ToggleButton.Theme>(
        {
            Selected =
                {
                    TextColor = Color.White
                    BorderColor = secondary.MainPlus1
                    BackgroundColor = secondary.Main
                }
            Unselected =
                {
                    TextColor = Color.White
                    BorderColor = neutral.B400
                    BackgroundColor = neutral.B300
                }
        }
    )

    Themes.Set<LC.FloatingActionButton.Theme> {
        Size = 56
        Actionable =
            {
                BackgroundColor = secondary.Main
                IconColor       = Color.White
                IconSize        = defaultIconSize
            }
        Disabled =
            {
                BackgroundColor = secondary.Main
                IconColor       = Color.White
                IconSize        = defaultIconSize
            }
        InProgress =
            {
                BackgroundColor = secondary.Main
                IconColor       = secondary.Main
                IconSize        = defaultIconSize
            }
    }

    Themes.Set<LC.IconButton.Theme> {
        Actionable =
            {
                IconColor = secondary.Main
                IconSize = defaultIconSize
                TapTargetMargin = (-10, -10, -10, -10)
            }
        Disabled =
            {
                IconColor = secondary.Main
                IconSize = defaultIconSize
                TapTargetMargin = (-10, -10, -10, -10)
            }
        InProgress =
            {
                IconColor = secondary.Main
                IconSize = defaultIconSize
                TapTargetMargin = (-10, -10, -10, -10)
            }
    }

    Themes.Set<LC.Tag.Theme>(
        {
            Tags = {
                Selected = {
                    TextColor = Color.White
                    BackgroundColor = secondary.Main
                }
                Unselected = {
                    TextColor    = secondary.Main
                    BackgroundColor = secondary.B100
                }
            }
            Sizes = {
                Desktop = {
                    FontSize = 14
                    PaddingHorizontal = 15
                    PaddingVertical = 6
                }
                Handheld = {
                    FontSize = 12
                    PaddingHorizontal = 10
                    PaddingVertical = 4
                }
            }
        }
    )

    Themes.Set<LC.Input.DateTypes.Theme> {
        BorderLabelBlurredColor = Color.Grey "44"
        BorderLabelFocusedColor = Color.DevGreen
        BorderLabelInvalidColor = Color.DevRed
        TextColor = Color.Grey "44"
        NoneditableTextColor = Color.Grey "22"
        NoneditableBackgroundColor = Color.Grey "66"
        InvalidReasonColor = Color.DevRed
        PlaceholderColor = Color.Grey "aa"
        TheVerticalPadding = 10
        CalendarButtonColor = Color.DevGreen
        CalendarButtonBackgroundColor = Color.Transparent
        CalendarButtonIconSize = 20
    }

    Themes.Set<LC.Input.EmailAddressTypes.Theme> {
        BorderLabelBlurredColor    = neutral.B300
        BorderLabelFocusedColor    = secondary.Main
        BorderLabelInvalidColor    = caution.Main
        TextColor                  = neutral.Main
        NoneditableTextColor       = neutral.B500
        NoneditableBackgroundColor = neutral.B200
        InvalidReasonColor         = caution.Main
        PlaceholderColor           = neutral.B300
        TheVerticalPadding         = 10
    }

    Themes.Set<LC.Input.PhoneNumberTypes.Theme> {
        BorderLabelBlurredColor    = neutral.B300
        BorderLabelFocusedColor    = secondary.Main
        BorderLabelInvalidColor    = caution.Main
        TextColor                  = neutral.Main
        NoneditableTextColor       = neutral.B500
        NoneditableBackgroundColor = neutral.B200
        InvalidReasonColor         = caution.Main
        PlaceholderColor           = neutral.B300
        TheVerticalPadding         = 10
    }

    Themes.Set<LC.Input.QuantityTypes.Theme> {
        NormalColors = {
            Border = secondary.Main
            Value = neutral.Main
            Icons = secondary.Main
        }
        InvalidColors = {
            Border = caution.Main
            Value = neutral.Main
            Icons = caution.Main
        }
        InvalidMessageColor = caution.Main
    }

    Themes.Set<LC.Input.DayOfTheWeek.Theme> {
        UnselectedTextColor       = neutral.Main
        UnselectedBackgroundColor = neutral.B050
        SelectedTextColor         = Color.White
        SelectedBackgroundColor   = secondary.Main
        LabelColor                = neutral.Main
        InvalidColor              = caution.Main
    }

    Themes.Set<LC.LabelledFormField.Theme> {
        LabelWidth = 100
        LabelColor = Color.Grey "66"
    }

    Themes.Set<LC.Input.File.Theme> {
        InvalidColor = caution.Main
    }

    Themes.Set<Legacy.Card.Theme> { FlatBorderColor = neutral.B300 }

    Themes.Set<LC.Card.Theme> LC.Card.Theme.ShadowedCard

    Themes.Set<LC.Thumb.Theme>({Size = 60; SelectedBorderColor = secondary.Main})

    Themes.Set<Legacy.TopNav.Base.Theme> {
        BackgroundColor = primary.Main
        TextColor       = Color.White
        Height          = 44
    }

    Themes.Set<LC.Legacy.TopNav.IconButtonTypes.Theme> {
        IconColor = Color.White
        IconSize = 30
    }

    Themes.Set<LC.Legacy.Sidebar.Item.Theme>(
        {
            PrimaryBackgroundColor           = Color.White
            PrimaryTextColor                 = Color.Black
            PrimarySelectedBackgroundColor   = Color.DevPink
            PrimarySelectedTextColor         = Color.Black
            SecondaryBackgroundColor         = Color.White
            SecondaryTextColor               = Color.Black
            SecondarySelectedBackgroundColor = Color.DevPink
            SecondarySelectedTextColor       = Color.Black
            BottomBorderColor                = Color.Black
            CountBackgroundColor             = Color.DevRed
            CountTextColor                   = Color.White
        }
    )

    Themes.Set<LC.Legacy.Sidebar.Filler.Theme>(
        {
            BottomBorderColor = Color.Black
        }
    )

    Themes.Set<LC.Carousel.Theme> {
        DotColor = Color.White
        SelectedDotColor = secondary.Main
        NavigationButtonColor = Color.White
        NavigationButtonBackgroundColor = Color.Transparent
        ButtonIconSize = defaultIconSize
        NavigationButtonStyle = None
        DotContainerStyle = None
        InactiveDotStyle = None
        ActiveDotStyle = None
    }

    Themes.Set<LC.Dialog.ImageViewer.Theme> {
        DotColor = Color.White
        SelectedDotColor = secondary.Main
        NavigationButtonColor = Color.White
        NavigationButtonBackgroundColor = Color.Grey "44"
        ButtonIconSize = 26
    }

    Themes.Set<LC.Dialog.Shell.WhiteRounded.Raw.Theme> {
        Width                   = None
        MaxSizeLimiterPadding   = None
        WhiteRoundedBasePadding = None
        BoundaryRadius          = None
        BackgroundColor         = None
    }

    Themes.Set<LC.Dialog.Shell.WhiteRounded.Base.Theme> {
        Width = None
    }

    Themes.Set<LC.Dialog.Shell.FullScreen.Theme> {
        TopNavHeight = 44
    }

    Themes.Set<LC.Input.Text.Theme> {
        BorderLabelBlurredColor    = neutral.B500
        BorderLabelFocusedColor    = secondary.Main
        BorderLabelInvalidColor    = caution.Main
        TextColor                  = neutral.Main
        NoneditableTextColor       = neutral.B500
        NoneditableBackgroundColor = neutral.B200
        EditableBackgroundColor    = Color.White
        LabelBackgroundColor       = Color.White
        InvalidReasonColor         = caution.Main
        PlaceholderColor           = neutral.B300
        TheVerticalPadding         = 10
        BorderRadius               = 4
    }

    Themes.Set<LC.Input.DecimalTypes.Theme> {
        BorderLabelBlurredColor    = neutral.B500
        BorderLabelFocusedColor    = secondary.Main
        BorderLabelInvalidColor    = caution.Main
        TextColor                  = neutral.Main
        NoneditableTextColor       = neutral.B500
        NoneditableBackgroundColor = neutral.B200
        InvalidReasonColor         = caution.Main
        PlaceholderColor           = neutral.B300
        TheVerticalPadding         = 10
    }

    Input.PositiveIntegerStyles.Theme.All(
        borderLabelBlurredColor    = neutral.B500,
        borderLabelFocusedColor    = secondary.Main,
        borderLabelInvalidColor    = caution.Main,
        textColor                  = neutral.Main,
        noneditableTextColor       = neutral.B500,
        noneditableBackgroundColor = neutral.B200,
        invalidReasonColor         = caution.Main,
        placeholderColor           = neutral.B300,
        theVerticalPadding         = 10
    )

    Themes.Set<LC.Input.UnsignedDecimalTypes.Theme> {
        BorderLabelBlurredColor    = neutral.B500
        BorderLabelFocusedColor    = secondary.Main
        BorderLabelInvalidColor    = caution.Main
        TextColor                  = neutral.Main
        NoneditableTextColor       = neutral.B500
        NoneditableBackgroundColor = neutral.B200
        InvalidReasonColor         = caution.Main
        PlaceholderColor           = neutral.B300
        TheVerticalPadding         = 10
    }

    Themes.Set<LibClient.Components.Input.LocalTime.Theme> {
        LabelBlurredColor  = neutral.B500
        LabelFocusedColor  = secondary.Main
        LabelInvalidColor  = caution.Main
        InvalidReasonColor = caution.Main
    }

    Themes.Set<LibClient.Components.Input.PickerInternals.Field.Theme> {
        BorderLabelColor        = neutral.B500
        BorderLabelFocusColor   = secondary.Main
        BorderLabelInvalidColor = caution.Main
        TextColor               = neutral.Main
        InvalidReasonColor      = caution.Main
        PlaceholderColor        = neutral.B300
        IconSize                = 20
        TheVerticalPadding      = 10
        BackgroundColor         = Color.White
        BorderRadius            = 4
        LabelBackgroundColor    = Color.White
    }

    Themes.Set<LibClient.Components.Input.PickerInternals.Popup.Theme> {
        BackgroundColor         = Color.White
        BorderColor             = neutral.B300
        ItemTextColor           = neutral.B500
        ItemTextHighlightColor  = neutral.Main
        ItemHighlightBackground = neutral.B200
        ItemBorderColor         = neutral.B300
        SelectedIconColor       = neutral.B300
        BorderRadius            = 4
    }

    Themes.Set<LC.Input.Checkbox.Theme>(
        {
            IconCheckedColor   = secondary.Main
            IconUncheckedColor = secondary.Main
            LabelColor         = neutral.Main
            IconSize           = 20
        }
    )

    Themes.Set<LC.InfoMessage.Theme>(
        {
            InfoColor      = neutral.Main
            AttentionColor = attention.Main
            CautionColor   = caution.Main
        }
    )

    Themes.Set<LC.Stars.Theme>(
        {
            OnColor = secondary.Main
            OffColor = neutral.B300
            IconSize = 20
        }
    )

    Themes.Set<LC.Tab.Theme>(
        {
            SelectedColor = secondary.Main
            UnselectedColor = neutral.B500
        }
    )

    Themes.Set<LibClient.Components.Tabs.Theme>(
        {
            BackgroundColor = Color.White
            BorderColor     = neutral.B700
            BorderWidth     = 1
        }
    )

    Themes.Set<LC.Sidebar.Heading.Theme>(
        {
            PrimaryTextColor   = neutral.B500
            PrimaryFontSize    = 18
            SecondaryTextColor = neutral.B500
            SecondaryFontSize  = 14
        }
    )

    Themes.Set<LC.Sidebar.Item.Theme>(
        {
            IconFontSize  = 24
            LabelFontSize = 16
            BadgeFontSize = 16
            ItemHeight    = 42
            Actionable = {
                Base = {
                    Label = neutral.Main; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
                Hovered = {
                    Label = neutral.Main; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = primary.B050; Border = Color.Transparent
                    LeftIcon = primary.MainMinus2; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
                Depressed = {
                    Label = neutral.Main; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
            }
            Selected = {
                Base = {
                    Label = neutral.B700; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
                Hovered = {
                    Label = neutral.B700; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
                Depressed = {
                    Label = neutral.B700; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
            }
            Disabled = {
                Base = {
                    Label = neutral.B200; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B100; RightIcon = neutral.B100; BadgeBackground = caution.B200; BadgeText = Color.White
                }
                Hovered = {
                    Label = neutral.B200; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B100; RightIcon = neutral.B100; BadgeBackground = caution.B200; BadgeText = Color.White
                }
                Depressed = {
                    Label = neutral.B200; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B100; RightIcon = neutral.B100; BadgeBackground = caution.B200; BadgeText = Color.White
                }
            }
            InProgress = {
                Base = {
                    Label = neutral.B200; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
                Hovered = {
                    Label = neutral.B200; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
                Depressed = {
                    Label = neutral.B200; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal; Background = Color.White; Border = Color.Transparent
                    LeftIcon = neutral.B400; RightIcon = neutral.B400; BadgeBackground = caution.Main; BadgeText = Color.White
                }
            }
        }
    )

    Themes.Set<LC.AppGlobalStatus.Theme> {
        BackgroundColor = caution.Main
    }

    Themes.Set<Nav.Top.Base.Theme>(
        {
            DesktopHeight   = 72
            HandheldHeight  = 55
            BackgroundColor = primary.Main
            HideShadow      = false
        }
    )

    Nav.Top.HeadingStyles.Theme.All(
        screenSizeToSizes = (function
            | ScreenSize.Desktop  -> {| FontSize = 24; |}
            | ScreenSize.Handheld -> {| FontSize = 16; |}
        ),
        theColor = Color.White
    )

    Themes.Set<LC.Nav.Top.Item.Theme>(
        {
            IconVerticalAdjust = 10
            Desktop = {
                IconFontSize = 24; LabelFontSize = 16; Height = 72
                BadgeFontSize = 14; BadgeTop = -10; BadgeLeft = -10
            }
            Handheld = {
                IconFontSize = 20; LabelFontSize = 14; Height = 42
                BadgeFontSize = 12; BadgeTop = -6; BadgeLeft = -10
            }
            Actionable = {
                Base = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = primary.MainMinus2; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.Main; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.B050; Border = primary.MainMinus2; Icon = primary.Main
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = primary.MainMinus2; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            Selected = {
                Base = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            SelectedActionable = {
                Base = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            Disabled = {
                Base = {
                    Label = primary.MainMinus2; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = primary.MainMinus2
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.MainMinus2; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = primary.MainMinus2
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.MainMinus2; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = primary.MainMinus2
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            InProgress = {
                Base = {
                    Label = Color.Transparent; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = Color.Transparent
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = Color.Transparent; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = Color.Transparent
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = Color.Transparent; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = Color.Transparent
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
        }
    )

    Themes.Set<Nav.Bottom.Base.Theme>(
        {
            DesktopHeight   = 72
            HandheldHeight  = 48
            BackgroundColor = primary.Main
            HideShadow      = false
        }
    )

    Themes.Set<LC.Nav.Bottom.Item.Theme>(
        {
            IconVerticalAdjust = 10
            Desktop = {
                IconFontSize = 24; LabelFontSize = 16; Height = 72
                BadgeFontSize = 14; BadgeTop = -10; BadgeLeft = -10
            }
            Handheld = {
                IconFontSize = 20; LabelFontSize = 14; Height = 42
                BadgeFontSize = 12; BadgeTop = -6; BadgeLeft = -10
            }
            Actionable = {
                Base = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = primary.MainMinus2; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.Main; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.B050; Border = primary.MainMinus2; Icon = primary.Main
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = primary.MainMinus2; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            Selected = {
                Base = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            SelectedActionable = {
                Base = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.B050; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold
                    Background = primary.Main; Border = primary.B050; Icon = primary.B050
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            Disabled = {
                Base = {
                    Label = primary.MainMinus2; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = primary.MainMinus2
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = primary.MainMinus2; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = primary.MainMinus2
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = primary.MainMinus2; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = primary.MainMinus2
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
            InProgress = {
                Base = {
                    Label = Color.Transparent; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = Color.Transparent
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Hovered = {
                    Label = Color.Transparent; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = Color.Transparent
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
                Depressed = {
                    Label = Color.Transparent; LabelWeight = ReactXP.Styles.RulesRestricted.FontWeight.Normal
                    Background = primary.Main; Border = Color.Transparent; Icon = Color.Transparent
                    BadgeFontColor = Color.White; BadgeFontWeight = ReactXP.Styles.RulesRestricted.FontWeight.Bold; BadgeBackgroundColor = Color.DevRed
                }
            }
        }
    )

    Themes.Set<LC.Input.WeeklyCalendar.Theme> {
        DayOfWeekText       = neutral.Main
        DateTextUnavailable = neutral.B200
        DateTextAvailable   = primary.Main
        DateTextSelected    = Color.White
        Circle              = secondary.Main
        InvalidReason       = caution.Main
    }

    Themes.Set<LC.ItemList.Theme> { SeeAll = { Height = 50; MarginVertical = 0 } }

    Themes.Set<LC.Input.NamedFile.Theme> {
        InvalidColor        = Color.DevRed
        DropZoneBorderColor = primary.B200
    }

module ApplyTheme =
    let primaryPrimary (scheme: ColorScheme) : unit =
        applyTheme scheme.Primary scheme.Primary scheme.Neutral scheme.Attention scheme.Caution

    let primarySecondary (scheme: ColorScheme) : unit =
        applyTheme scheme.Primary scheme.Secondary scheme.Neutral scheme.Attention scheme.Caution
