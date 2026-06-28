module AppTodo.Colors

open LibClient.ColorModule
open LibClient.ColorScheme

// Muted teal ramp matching the ui2 mockup (primary #458b8c, dark #377172).
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
    DueSoft: Color
    ChipBackground: Color
    ChipBorder: Color
    InputBackground: Color
    InputBorder: Color
    StatBackground: Color
}

module SemanticPalette =
    let light =
        {
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
            PriorityLowSoft = Color.Hex "#f1f5f9"
            CategoryBlueSoft = Color.Hex "#c2e2e9"
            CategoryBlueText = Color.Hex "#245763"
            CategoryGreenSoft = Color.Hex "#c6e4d1"
            CategoryGreenText = Color.Hex "#275c3c"
            DueSoft = Color.Hex "#f6e2d0"
            ChipBackground = Color.Hex "#f5f0ec"
            ChipBorder = Color.Hex "#ede4dc"
            InputBackground = Color.White
            InputBorder = Color.Hex "#e2d8cf"
            StatBackground = Color.Hex "#deecf0"
        }

    let dark =
        {
            PageBackground = Color.Hex "#160d08"
            CardBackground = Color.Hex "#251510"
            CardBorder = Color.Hex "#3d2217"
            FormBackground = Color.Hex "#1c1008"
            TextPrimary = Color.Hex "#f5ede8"
            TextSecondary = Color.Hex "#c4a898"
            TextMuted = Color.Hex "#7a5f54"
            HeadingText = Color.Hex "#c4927e"
            RowBackground = Color.Hex "#1c1008"
            RowBorder = Color.Hex "#3d2217"
            Accent = Color.Hex "#5ba5a6"
            AccentSoft = Color.Hex "#1a3840"
            Danger = Color.Hex "#f87171"
            Success = Color.Hex "#6dbf82"
            Warning = Color.Hex "#d09060"
            PriorityHigh = Color.Hex "#d08080"
            PriorityMedium = Color.Hex "#d09060"
            PriorityLow = Color.Hex "#7a5f54"
            PriorityHighSoft = Color.Hex "#3d1818"
            PriorityMediumSoft = Color.Hex "#3d2810"
            PriorityLowSoft = Color.Hex "#2a1a10"
            CategoryBlueSoft = Color.Hex "#1b3540"
            CategoryBlueText = Color.Hex "#7bc8d8"
            CategoryGreenSoft = Color.Hex "#16321d"
            CategoryGreenText = Color.Hex "#6dbf82"
            DueSoft = Color.Hex "#3d2810"
            ChipBackground = Color.Hex "#2a1a10"
            ChipBorder = Color.Hex "#3d2217"
            InputBackground = Color.Hex "#2a1a10"
            InputBorder = Color.Hex "#43271a"
            StatBackground = Color.Hex "#1b3540"
        }

    let forMode mode =
        match mode with
        | AppearanceMode.Light -> light
        | AppearanceMode.Dark -> dark
