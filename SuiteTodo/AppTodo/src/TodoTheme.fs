module AppTodo.TodoTheme

open AppTodo.Colors
open LibClient
open LibClient.Components
open LibClient.Components.Tabs
open LibClient.Responsive
open ReactXP.Styles
open SuiteTodo.Types

type TabTheme = {
    BackgroundColor: Color
    BorderColor: Color
    SelectedColor: Color
    UnselectedColor: Color
}

module Styles =
    let page =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isHandheld: bool) ->
                makeViewStyles {
                    flex 1
                    paddingVertical (if isHandheld then 16 else 32)
                    paddingHorizontal (if isHandheld then 16 else 32)
                    AlignItems.Center
                    backgroundColor palette.PageBackground
                }
        )

    let cardShell =
        ViewStyles.Memoize(
            fun (isHandheld: bool) ->
                makeViewStyles {
                    widthPercent 100
                    maxWidth (if isHandheld then 9999 else 720)
                    AlignSelf.Stretch
                }
        )

    let card =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isHandheld: bool) ->
                makeViewStyles {
                    widthPercent 100
                    padding (if isHandheld then 20 else 28)
                    backgroundColor palette.CardBackground
                    borderRadius 16
                    borderWidth 1
                    borderColor palette.CardBorder
                }
        )

    let headerRow =
        ViewStyles.Memoize(
            fun (isHandheld: bool) ->
                makeViewStyles {
                    if isHandheld then
                        FlexDirection.Column
                        AlignItems.Stretch
                        gap 12
                    else
                        FlexDirection.Row
                        JustifyContent.SpaceBetween
                        AlignItems.Center
                        gap 12
                }
        )

    let headerActions =
        makeViewStyles {
            AlignSelf.FlexStart
            flexShrink 0
        }

    let categoryScroll =
        makeScrollViewStyles {
            flexGrow 0
            flexShrink 1
        }

    let categoryRow =
        makeViewStyles {
            FlexDirection.Row
            gap 8
            paddingVertical 2
        }

    let subtitle =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    marginTop 4
                    color palette.TextSecondary
                    fontSize 14
                }
        )

    let statsRow =
        makeViewStyles {
            FlexDirection.Row
            FlexWrap.Wrap
            gap 8
        }

    let statChip =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    paddingVertical 8
                    paddingHorizontal 12
                    borderRadius 999
                    backgroundColor palette.StatBackground
                    borderWidth 1
                    borderColor palette.ChipBorder
                }
        )

    let statChipText =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.TextSecondary
                    fontSize 12
                    FontWeight.W600
                }
        )

    let inputFlex =
        makeViewStyles {
            flex 1
            minWidth 0
        }

    let composerGrid =
        ViewStyles.Memoize(
            fun (isHandheld: bool) ->
                makeViewStyles {
                    gap (if isHandheld then 12 else 16)
                }
        )

    let composerRow =
        ViewStyles.Memoize(
            fun (isHandheld: bool) ->
                makeViewStyles {
                    if isHandheld then
                        FlexDirection.Column
                        gap 12
                    else
                        FlexDirection.Row
                        gap 12
                        AlignItems.Center
                }
        )

    let list =
        makeViewStyles {
            gap 10
            marginTop 4
        }

    let todoRow =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isDone: bool) ->
                makeViewStyles {
                    FlexDirection.Row
                    AlignItems.Center
                    gap 12
                    paddingVertical 14
                    paddingHorizontal 16
                    backgroundColor palette.RowBackground
                    borderRadius 12
                    borderWidth 1
                    borderColor palette.RowBorder
                    opacity (if isDone then 0.82 else 1.0)
                }
        )

    let todoMetaRow =
        makeViewStyles {
            FlexDirection.Row
            FlexWrap.Wrap
            gap 6
            marginTop 4
        }

    let metaChip =
        ViewStyles.Memoize(
            fun (chipColor: Color) (palette: SemanticPalette) ->
                makeViewStyles {
                    paddingVertical 2
                    paddingHorizontal 8
                    borderRadius 999
                    backgroundColor palette.ChipBackground
                    borderWidth 1
                    borderColor chipColor
                }
        )

    let metaChipText =
        TextStyles.Memoize(
            fun (chipColor: Color) ->
                makeTextStyles {
                    color chipColor
                    fontSize 11
                    FontWeight.W600
                }
        )

    let titleTextActive =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.TextPrimary
                    fontSize 15
                    FontWeight.W600
                }
        )

    let titleTextDone =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    textDecorationLine TextDecorationLine.LineThrough
                    color palette.TextMuted
                    fontSize 15
                }
        )

    let rowActions =
        makeViewStyles {
            FlexDirection.Row
            gap 4
            AlignItems.Center
        }

    let tabsTheme (palette: SemanticPalette) : TabTheme =
        {
            BackgroundColor = palette.CardBackground
            BorderColor = palette.CardBorder
            SelectedColor = palette.Accent
            UnselectedColor = palette.TextMuted
        }

    let filterTabTheme (tabBase: TabTheme) : LC.Tab.Theme =
        {
            SelectedColor = tabBase.SelectedColor
            UnselectedColor = tabBase.UnselectedColor
        }

    let tabsScrollTheme (tabBase: TabTheme) : Theme =
        {
            BackgroundColor = tabBase.BackgroundColor
            BorderColor = tabBase.BorderColor
            BorderWidth = 1
        }

    let priorityColor (palette: SemanticPalette) (priority: TodoPriority) =
        match priority with
        | TodoPriority.High -> palette.PriorityHigh
        | TodoPriority.Medium -> palette.PriorityMedium
        | TodoPriority.Low -> palette.PriorityLow
