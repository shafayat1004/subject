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

[<RequireQualifiedAccess>]
type MetaChipKind =
| Priority
| Category
| Due

module Styles =
    let page =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isHandheld: bool) ->
                makeViewStyles {
                    flex 1
                    paddingVertical (if isHandheld then 12 else 28)
                    paddingHorizontal (if isHandheld then 12 else 28)
                    AlignItems.Center
                    backgroundColor palette.PageBackground
                }
        )

    let cardShell =
        ViewStyles.Memoize(
            fun (isHandheld: bool) ->
                makeViewStyles {
                    widthPercent 100
                    maxWidth (if isHandheld then 9999 else 1060)
                    AlignSelf.Stretch
                }
        )

    let card =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isHandheld: bool) ->
                makeViewStyles {
                    widthPercent 100
                    padding (if isHandheld then 18 else 32)
                    backgroundColor palette.CardBackground
                    borderRadius (if isHandheld then 24 else 26)
                    borderWidth 1
                    borderColor palette.CardBorder
                }
        )

    let pageScroll =
        makeScrollViewStyles {
            flex 1
            AlignSelf.Stretch
        }

    let pageScrollContent =
        makeViewStyles {
            paddingBottom 28
        }

    let headerRow =
        makeViewStyles {
            FlexDirection.Row
            JustifyContent.SpaceBetween
            AlignItems.FlexStart
            gap 12
        }

    let headerTitleBlock =
        makeViewStyles {
            flex 1
            minWidth 0
        }

    let headerActions =
        makeViewStyles {
            AlignSelf.FlexStart
            flexShrink 0
            marginTop 4
        }

    // Segmented Light/Dark theme toggle (pill track + two segments).
    let themeToggleTrack =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    FlexDirection.Row
                    borderRadius 999
                    padding 3
                    backgroundColor palette.ChipBackground
                    borderWidth 1
                    borderColor palette.ChipBorder
                }
        )

    let themeSegment =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isActive: bool) ->
                makeViewStyles {
                    paddingVertical 5
                    paddingHorizontal 12
                    borderRadius 999
                    if isActive then
                        backgroundColor palette.Accent
                }
        )

    let themeSegmentText =
        TextStyles.Memoize(
            fun (segmentColor: Color) ->
                makeTextStyles {
                    fontSize 12
                    FontWeight.W600
                    color segmentColor
                }
        )

    // Composer field cell: full-width stacked on handheld, equal flex columns when wide.
    let composerCell =
        ViewStyles.Memoize(
            fun (isHandheld: bool) ->
                makeViewStyles {
                    if isHandheld then
                        AlignSelf.Stretch
                    else
                        flex 1
                        minWidth 0
                }
        )

    let filterTabsRow =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    FlexDirection.Row
                    AlignSelf.Stretch
                    borderBottomWidth 1
                    borderColor palette.CardBorder
                }
        )

    let filterTabCell =
        makeViewStyles {
            flex 1
            AlignItems.Center
        }

    let categoryScroll =
        makeScrollViewStyles {
            flexGrow 0
            flexShrink 0
            AlignSelf.Stretch
        }

    let categoryScrollContent =
        makeViewStyles {
            FlexDirection.Row
            gap 8
            paddingVertical 4
            paddingRight 8
        }

    let categoryPill =
        ViewStyles.Memoize(
            fun (bg: Color) (border: Color) (isSelected: bool) ->
                makeViewStyles {
                    borderRadius 999
                    borderWidth (if isSelected then 2 else 1)
                    paddingVertical 4
                    paddingHorizontal 10
                    backgroundColor bg
                    borderColor border
                }
        )

    let headingText =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.HeadingText
                    fontSize 28
                    FontWeight.W700
                    marginBottom 2
                }
        )

    let subtitle =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    marginTop 4
                    color palette.TextSecondary
                    fontSize 14
                }
        )

    let listHeader =
        makeViewStyles {
            FlexDirection.Row
            JustifyContent.SpaceBetween
            AlignItems.Center
            gap 12
            marginTop 4
            marginBottom 2
        }

    let statsRow =
        makeViewStyles {
            FlexDirection.Row
            FlexWrap.Wrap
            gap 8
        }

    let subFiltersRow =
        makeViewStyles {
            FlexDirection.Row
            gap 8
            FlexWrap.Wrap
        }

    let subFilterPill =
        ViewStyles.Memoize(
            fun (bg: Color) ->
                makeViewStyles {
                    paddingVertical 6
                    paddingHorizontal 12
                    borderRadius 999
                    backgroundColor bg
                }
        )

    let subFilterPillText =
        TextStyles.Memoize(
            fun (textColor: Color) ->
                makeTextStyles {
                    color textColor
                    fontSize 12
                    FontWeight.W500
                }
        )

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

    let composerPanel =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isHandheld: bool) ->
                makeViewStyles {
                    gap (if isHandheld then 12 else 14)
                    padding (if isHandheld then 16 else 18)
                    borderRadius 24
                    backgroundColor palette.FormBackground
                    borderWidth 1
                    borderColor palette.ChipBorder
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
                    gap (if isHandheld then 12 else 14)
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

    let searchField =
        makeViewStyles {
            marginTop 4
        }

    let list =
        makeViewStyles {
            gap 12
            marginTop 4
        }

    let todoRow =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isDone: bool) (isHandheld: bool) ->
                makeViewStyles {
                    if isHandheld then
                        FlexDirection.Column
                        AlignItems.Stretch
                        gap 10
                    else
                        FlexDirection.Row
                        AlignItems.Center
                        gap 12
                    paddingVertical (if isHandheld then 16 else 14)
                    paddingHorizontal 16
                    backgroundColor palette.RowBackground
                    borderRadius 16
                    borderWidth 1
                    borderColor palette.RowBorder
                    opacity (if isDone then 0.82 else 1.0)
                }
        )

    let todoRowTop =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.FlexStart
            gap 12
        }

    let todoRowBody =
        makeViewStyles {
            flex 1
            minWidth 0
        }

    let todoMetaRow =
        makeViewStyles {
            FlexDirection.Row
            FlexWrap.Wrap
            gap 8
            marginTop 8
        }

    let metaChip =
        ViewStyles.Memoize(
            fun (chipBg: Color) (chipBorder: Color) ->
                makeViewStyles {
                    paddingVertical 5
                    paddingHorizontal 12
                    borderRadius 999
                    borderWidth 1
                    backgroundColor chipBg
                    borderColor chipBorder
                    AlignSelf.FlexStart
                }
        )

    let metaChipText =
        TextStyles.Memoize(
            fun (chipText: Color) ->
                makeTextStyles {
                    color chipText
                    fontSize 12
                    FontWeight.W700
                }
        )

    let titleTextActive =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.TextPrimary
                    fontSize 15
                    FontWeight.W700
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
            gap 8
            AlignItems.Center
        }

    let rowActionsHandheld =
        makeViewStyles {
            FlexDirection.Row
            gap 8
            AlignItems.Center
            JustifyContent.FlexEnd
            marginLeft 36
        }

    let tabsTheme (palette: SemanticPalette) : TabTheme =
        {
            BackgroundColor = palette.CardBackground
            BorderColor = palette.CardBorder
            SelectedColor = palette.Accent
            UnselectedColor = palette.TextSecondary
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

    let priorityChipColors (palette: SemanticPalette) (priority: TodoPriority) =
        match priority with
        | TodoPriority.High -> palette.PriorityHighSoft, palette.PriorityHigh, palette.PriorityHigh
        | TodoPriority.Medium -> palette.PriorityMediumSoft, palette.PriorityMedium, palette.PriorityMedium
        | TodoPriority.Low -> palette.PriorityLowSoft, palette.PriorityLow, palette.PriorityLow

    let categoryChipColorsByCategory (palette: SemanticPalette) (category: option<TodoCategory>) =
        match category with
        | Some TodoCategory.Work | Some TodoCategory.Personal ->
            palette.CategoryBlueSoft, palette.Accent, palette.CategoryBlueText
        | Some TodoCategory.Shopping | Some TodoCategory.Health ->
            palette.CategoryGreenSoft, palette.Success, palette.CategoryGreenText
        | Some TodoCategory.Other | None ->
            palette.ChipBackground, palette.ChipBorder, palette.TextMuted

    let dueChipColors (palette: SemanticPalette) =
        palette.DueSoft, palette.Warning, palette.Warning
