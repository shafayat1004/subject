module AppTodo.TodoTheme

open AppTodo.Colors
open LibClient
open LibClient.Components
open LibClient.Components.Tabs
open LibClient.Responsive
open ReactXP.Styles
open ReactXP.Styles.Animation
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
            fun (palette: SemanticPalette) (usePhoneChrome: bool) ->
                makeViewStyles {
                    flex 1
                    widthPercent 100
                    if usePhoneChrome then
                        paddingVertical 28
                        paddingHorizontal 16
                        AlignItems.Center
                        backgroundColor palette.CanvasBackground
                    else
                        backgroundColor palette.PageBackground
                }
        )

    let cardShell =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (usePhoneChrome: bool) ->
                makeViewStyles {
                    widthPercent 100
                    maxWidth (if usePhoneChrome then 420 else 1060)
                    if usePhoneChrome then
                        AlignSelf.Center
                        borderRadius 40
                        backgroundColor palette.PageBackground
                        shadow (Color.BlackAlpha 0.1) 20 (0, 10)
                        Overflow.Hidden
                    else
                        AlignSelf.Stretch
                        flex 1
                }
        )

    let card =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (usePhoneChrome: bool) ->
                makeViewStyles {
                    widthPercent 100
                    padding 24
                    backgroundColor palette.PageBackground
                    if not usePhoneChrome then
                        borderRadius 26
                        borderWidth 1
                        borderColor palette.CardBorder
                }
        )

    let pageScroll =
        makeScrollViewStyles {
            flex 1
            widthPercent 100
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
            marginTop 20
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

    // Match the mockup's radius-md (16) on primary buttons; framework Button
    // hardcodes 4, and caller styles win (appended last).
    let addButton =
        makeViewStyles {
            borderRadius 16
            AlignSelf.Stretch
            minHeight 48
            paddingVertical 14
        }

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
                    borderBottomWidth 2
                    borderColor palette.TabBorder
                    paddingBottom 8
                }
        )

    let filterTabCell =
        makeViewStyles {
            flex 1
            minHeight 44
            AlignItems.Center
            JustifyContent.Center
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
            FlexWrap.Nowrap
            gap 8
            paddingBottom 14
            paddingRight 8
            AlignItems.Center
        }

    let categoryPill =
        ViewStyles.Memoize(
            fun (bg: Color) (border: Color) (isSelected: bool) ->
                makeViewStyles {
                    minHeight 44
                    flexShrink 0
                    borderRadius 999
                    borderWidth (if isSelected then 2 else 1)
                    paddingVertical 4
                    paddingHorizontal 10
                    JustifyContent.Center
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
                    FontWeight.Normal
                    marginBottom 4
                }
        )

    let subtitle =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.TextSecondary
                    fontSize 14
                    lineHeight 20
                }
        )

    let fieldLabel =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.TextPrimary
                    fontSize 14
                    FontWeight.W500
                    marginBottom 6
                }
        )

    let fieldStack =
        makeViewStyles {
            AlignSelf.Stretch
        }

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
                    paddingVertical 6
                    paddingHorizontal 12
                    borderRadius 999
                    backgroundColor palette.StatBackground
                    borderWidth 1
                    borderColor (Color.BlackAlpha (13.0 / 255.0))
                }
        )

    let statChipText =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.StatText
                    fontSize 12
                    FontWeight.W500
                }
        )

    let composerPanel =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isHandheld: bool) ->
                makeViewStyles {
                    gap (if isHandheld then 12 else 14)
                    padding 16
                    borderRadius 24
                    backgroundColor palette.FormBackground
                    borderWidth 0
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
            AlignSelf.Stretch
        }

    let searchInputWrap =
        ViewStyles.Memoize(
            fun (_palette: SemanticPalette) ->
                makeViewStyles {
                    AlignSelf.Stretch
                }
        )

    let searchInput =
        ViewStyles.Memoize(
            fun (_palette: SemanticPalette) ->
                makeViewStyles {
                    Noop
                }
        )

    let list =
        makeViewStyles {
            gap 10
            marginTop 4
        }

    let todoRowOuter =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (mode: AppearanceMode) ->
                makeViewStyles {
                    borderRadius 16
                    Overflow.Hidden
                    if mode = AppearanceMode.Dark then
                        borderWidth 1
                        borderColor palette.RowBorder
                }
        )

    let todoRowSurface =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (mode: AppearanceMode) ->
                makeViewStyles {
                    FlexDirection.Column
                    AlignItems.Stretch
                    gap 8
                    paddingVertical 10
                    paddingHorizontal 12
                    backgroundColor palette.RowBackground
                    borderRadius 16
                    borderRight 3 palette.SwipeHintColor
                    if mode = AppearanceMode.Light then
                        shadow palette.RowShadowColor 8 (0, 2)
                }
        )

    let todoRow =
        ViewStyles.Memoize(
            fun (_palette: SemanticPalette) (_isDone: bool) (_isHandheld: bool) ->
                makeViewStyles {
                    // Done-state fade belongs on title text only — row opacity lets swipe Delete show through.
                    Noop
                }
        )

    let todoMetaRow =
        makeViewStyles {
            FlexDirection.Row
            FlexWrap.Wrap
            gap 4
        }

    let todoBodyRow =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Center
            gap 10
            minHeight 44
        }

    let todoContent =
        makeViewStyles {
            flex 1
            minWidth 0
        }

    let actionIconButton =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    width 44
                    height 44
                    borderRadius 8
                    borderWidth 2
                    borderColor Color.Transparent
                    JustifyContent.Center
                    AlignItems.Center
                    flexShrink 0
                    backgroundColor Color.Transparent
                    // Hover/focus tint approximated via pressed state in RN; color encodes mockup intent.
                    opacity 1
                }
        )

    let swipeDeleteWidth = 80

    let swipeRowHost =
        makeViewStyles {
            Position.Relative
            Overflow.Hidden
            borderRadius 16
        }

    let swipeGradientOverlay =
        ViewStyles.Memoize(
            fun (isVisible: bool) ->
                makeViewStyles {
                    Position.Absolute
                    trbl 0 0 0 0
                    backgroundColor (Color.Hex "#dc2626")
                    opacity (if isVisible then 1.0 else 0.0)
                }
        )

    let swipeDeleteSlot =
        makeViewStyles {
            Position.Absolute
            top 0
            bottom 0
            right 0
            width swipeDeleteWidth
            JustifyContent.Center
            AlignItems.Center
        }

    let swipeDeleteButtonText =
        makeTextStyles {
            color Color.White
            fontSize 14
            FontWeight.W600
        }

    let swipeContentAnimated (translateX: AnimatableValue) (palette: SemanticPalette) =
        makeAnimatableViewStyles {
            backgroundColor palette.RowBackground
            borderRadius 16
            Overflow.Hidden
            animatedTransform [
                [ animatedTranslateX translateX ]
            ]
        }

    let swipeReducedMotionRow =
        makeViewStyles {
            FlexDirection.Row
            gap 8
            AlignItems.Stretch
        }

    let swipeReducedMotionContent =
        makeViewStyles {
            flex 1
            minWidth 0
        }

    let swipeReducedMotionDelete =
        makeViewStyles {
            minWidth 72
            minHeight 44
            paddingHorizontal 14
            borderRadius 16
            JustifyContent.Center
            AlignItems.Center
            backgroundColor (Color.Hex "#dc2626")
            flexShrink 0
            AlignSelf.Stretch
        }

    let metaChip =
        ViewStyles.Memoize(
            fun (chipBg: Color) (chipBorder: Color) ->
                makeViewStyles {
                    paddingVertical 2
                    paddingHorizontal 6
                    borderRadius 999
                    borderWidth 0
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
                    fontSize 10
                    FontWeight.W600
                    lineHeight 14
                }
        )

    let titleTextActive =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    color palette.TextPrimary
                    fontSize 15
                    FontWeight.W500
                }
        )

    let titleTextDone =
        TextStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeTextStyles {
                    textDecorationLine TextDecorationLine.LineThrough
                    color palette.TextSecondary
                    fontSize 15
                    FontWeight.W500
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
            UnselectedColor = Color.Hex "#666666"
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
            palette.CategoryGreenSoft, palette.Accent, palette.CategoryGreenText
        | Some TodoCategory.Other | None ->
            palette.ChipNeutralBackground, palette.Accent, palette.ChipNeutralText

    let dueChipColors (palette: SemanticPalette) =
        palette.DueSoft, palette.Warning, palette.Warning
