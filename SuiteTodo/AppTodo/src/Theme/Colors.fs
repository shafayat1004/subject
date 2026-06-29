module AppTodo.Colors

open LibClient.ColorModule
open LibClient.ColorScheme

// Muted teal ramp matching ui2.html (primary #458B8C, hover #377172).
let appTeal =
    Variants (function
        | B050              -> Color.Hex "#eaf3f3"
        | B100              -> Color.Hex "#cbe0e1"
        | B200              -> Color.Hex "#a9cdce"
        | B300 | MainMinus2 -> Color.Hex "#86baba"
        | B400 | MainMinus1 -> Color.Hex "#6aa6a7"
        | B500 | Main       -> Color.Hex "#458b8c"
        | B600 | MainPlus1  -> Color.Hex "#3d7e7f"
        | B700 | MainPlus2  -> Color.Hex "#377172"
        | B800              -> Color.Hex "#2d5c5c"
        | B900              -> Color.Hex "#234747"
    )

type AppTodoColorScheme() =
    inherit ColorSchemeWithDefaults()

    override this.Primary   : Variants = appTeal
    override this.Secondary : Variants = appTeal

let colors = AppTodoColorScheme()

[<RequireQualifiedAccess>]
type AppearanceMode =
| Light
| Dark

type SemanticPalette = {
    CanvasBackground: Color
    PageBackground: Color
    CardBackground: Color
    CardBorder: Color
    FormBackground: Color
    TextPrimary: Color
    TextSecondary: Color
    TextMuted: Color
    HeadingText: Color
    RowBackground: Color
    RowBorder: Color
    Accent: Color
    AccentSoft: Color
    Danger: Color
    Success: Color
    Warning: Color
    PriorityHigh: Color
    PriorityMedium: Color
    PriorityLow: Color
    PriorityHighSoft: Color
    PriorityMediumSoft: Color
    PriorityLowSoft: Color
    CategoryBlueSoft: Color
    CategoryBlueText: Color
    CategoryGreenSoft: Color
    CategoryGreenText: Color
    ChipNeutralBackground: Color
    ChipNeutralText: Color
    DueSoft: Color
    ChipBackground: Color
    ChipBorder: Color
    ThemeTrackBackground: Color
    ThemeToggleSelected: Color
    InputBackground: Color
    InputBorder: Color
    SearchBackground: Color
    StatBackground: Color
    StatText: Color
    TabBorder: Color
    SwipeHintColor: Color
    RowShadowColor: Color
}

module SemanticPalette =
    let light =
        {
            CanvasBackground = Color.Hex "#e5e5e5"
            PageBackground = Color.Hex "#fdf9f6"
            CardBackground = Color.White
            CardBorder = Color.Hex "#ede4dc"
            FormBackground = Color.Hex "#efe6df"
            TextPrimary = Color.Hex "#1f2937"
            TextSecondary = Color.Hex "#536174"
            TextMuted = Color.Hex "#94a3b8"
            HeadingText = Color.Hex "#9c7063"
            RowBackground = Color.White
            RowBorder = Color.Hex "#ede4dc"
            Accent = Color.Hex "#458b8c"
            AccentSoft = Color.Hex "#c2e2e9"
            Danger = Color.Hex "#dc2626"
            Success = Color.Hex "#275c3c"
            Warning = Color.Hex "#7a4e24"
            PriorityHigh = Color.Hex "#7a2e2e"
            PriorityMedium = Color.Hex "#7a4e24"
            PriorityLow = Color.Hex "#64748b"
            PriorityHighSoft = Color.Hex "#f4d3d3"
            PriorityMediumSoft = Color.Hex "#f6e2d0"
            PriorityLowSoft = Color.Hex "#e5e7eb"
            CategoryBlueSoft = Color.Hex "#c2e2e9"
            CategoryBlueText = Color.Hex "#245763"
            CategoryGreenSoft = Color.Hex "#c6e4d1"
            CategoryGreenText = Color.Hex "#275c3c"
            ChipNeutralBackground = Color.Hex "#e5e7eb"
            ChipNeutralText = Color.Hex "#4b5563"
            DueSoft = Color.Hex "#f6e2d0"
            ChipBackground = Color.Hex "#f5f0ec"
            ChipBorder = Color.Hex "#ede4dc"
            ThemeTrackBackground = Color.Hex "#eae0d9"
            ThemeToggleSelected = Color.Hex "#2d4c4c"
            InputBackground = Color.WhiteAlpha 0.4
            InputBorder = Color.BlackAlpha (26.0 / 255.0)
            SearchBackground = Color.Hex "#efe6df"
            StatBackground = Color.Hex "#deecf0"
            StatText = Color.Hex "#3b6b78"
            TabBorder = Color.Hex "#eae0d9"
            SwipeHintColor = Color.Hex "#dc2626"
            RowShadowColor = Color.BlackAlpha 0.03
        }

    let dark =
        {
            CanvasBackground = Color.Hex "#0a0604"
            PageBackground = Color.Hex "#160d08"
            CardBackground = Color.Hex "#251510"
            CardBorder = Color.Hex "#3d2217"
            FormBackground = Color.Hex "#1c1008"
            TextPrimary = Color.Hex "#f5ede8"
            TextSecondary = Color.Hex "#c4a898"
            TextMuted = Color.Hex "#9a8070"
            HeadingText = Color.Hex "#e8b8a4"
            RowBackground = Color.Hex "#251510"
            RowBorder = Color.Hex "#3d2217"
            Accent = Color.Hex "#5ba5a6"
            AccentSoft = Color.Hex "#1a3840"
            Danger = Color.Hex "#f87171"
            Success = Color.Hex "#6dbf82"
            Warning = Color.Hex "#d09060"
            PriorityHigh = Color.Hex "#f0a0a0"
            PriorityMedium = Color.Hex "#e8b080"
            PriorityLow = Color.Hex "#c4a898"
            PriorityHighSoft = Color.Hex "#4a2020"
            PriorityMediumSoft = Color.Hex "#4a3018"
            PriorityLowSoft = Color.Hex "#2a1a10"
            CategoryBlueSoft = Color.Hex "#1f3f4c"
            CategoryBlueText = Color.Hex "#9ddce9"
            CategoryGreenSoft = Color.Hex "#1a3d24"
            CategoryGreenText = Color.Hex "#8ad99a"
            ChipNeutralBackground = Color.Hex "#2a1a10"
            ChipNeutralText = Color.Hex "#c4a898"
            DueSoft = Color.Hex "#4a3018"
            ChipBackground = Color.Hex "#2a1a10"
            ChipBorder = Color.Hex "#3d2217"
            ThemeTrackBackground = Color.Hex "#2a1a10"
            ThemeToggleSelected = Color.Hex "#5ba5a6"
            InputBackground = Color.Hex "#2a1a10"
            InputBorder = Color.Hex "#43271a"
            SearchBackground = Color.Hex "#2a1a10"
            StatBackground = Color.Hex "#1f3f4c"
            StatText = Color.Hex "#9ddce9"
            TabBorder = Color.Hex "#3d2217"
            SwipeHintColor = Color.Hex "#dc2626"
            RowShadowColor = Color.BlackAlpha 0.0
        }

    let forMode mode =
        match mode with
        | AppearanceMode.Light -> light
        | AppearanceMode.Dark -> dark
