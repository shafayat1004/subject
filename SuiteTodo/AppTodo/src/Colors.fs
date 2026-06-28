module AppTodo.Colors

open LibClient.ColorModule
open LibClient.ColorScheme

type AppTodoColorScheme() =
    inherit ColorSchemeWithDefaults()

    override this.Primary   : Variants = MaterialDesignColors.``Light Green``
    override this.Secondary : Variants = MaterialDesignColors.Cyan

let colors = AppTodoColorScheme()

[<RequireQualifiedAccess>]
type AppearanceMode =
| Light
| Dark

type SemanticPalette = {
    PageBackground: Color
    CardBackground: Color
    CardBorder: Color
    TextPrimary: Color
    TextSecondary: Color
    TextMuted: Color
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
    ChipBackground: Color
    ChipBorder: Color
    InputBackground: Color
    StatBackground: Color
}

module SemanticPalette =
    let light =
        {
            PageBackground = Color.Hex "#eef2f7"
            CardBackground = Color.White
            CardBorder = Color.Hex "#d8e0ea"
            TextPrimary = Color.Hex "#0f172a"
            TextSecondary = Color.Hex "#475569"
            TextMuted = Color.Hex "#94a3b8"
            RowBackground = Color.Hex "#f8fafc"
            RowBorder = Color.Hex "#e2e8f0"
            Accent = colors.Primary.Main
            AccentSoft = Color.Hex "#dcfce7"
            Danger = Color.Hex "#dc2626"
            Success = Color.Hex "#16a34a"
            Warning = Color.Hex "#d97706"
            PriorityHigh = Color.Hex "#ef4444"
            PriorityMedium = Color.Hex "#f59e0b"
            PriorityLow = Color.Hex "#64748b"
            ChipBackground = Color.Hex "#f1f5f9"
            ChipBorder = Color.Hex "#cbd5e1"
            InputBackground = Color.White
            StatBackground = Color.Hex "#f1f5f9"
        }

    let dark =
        {
            PageBackground = Color.Hex "#0b1220"
            CardBackground = Color.Hex "#111827"
            CardBorder = Color.Hex "#1f2937"
            TextPrimary = Color.Hex "#f8fafc"
            TextSecondary = Color.Hex "#cbd5e1"
            TextMuted = Color.Hex "#64748b"
            RowBackground = Color.Hex "#0f172a"
            RowBorder = Color.Hex "#1e293b"
            Accent = colors.Primary.Main
            AccentSoft = Color.Hex "#14532d"
            Danger = Color.Hex "#f87171"
            Success = Color.Hex "#4ade80"
            Warning = Color.Hex "#fbbf24"
            PriorityHigh = Color.Hex "#f87171"
            PriorityMedium = Color.Hex "#fbbf24"
            PriorityLow = Color.Hex "#94a3b8"
            ChipBackground = Color.Hex "#1e293b"
            ChipBorder = Color.Hex "#334155"
            InputBackground = Color.Hex "#0f172a"
            StatBackground = Color.Hex "#1e293b"
        }

    let forMode mode =
        match mode with
        | AppearanceMode.Light -> light
        | AppearanceMode.Dark -> dark
