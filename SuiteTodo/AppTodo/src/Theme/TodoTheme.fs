module AppTodo.TodoTheme

open AppTodo.Colors
open LibClient
open LibClient.Components
open LibClient.Components.Tabs
open LibClient.Responsive
open Rn.Styles
open SuiteTodo.Types

type TabTheme = {
    BackgroundColor: Color
    BorderColor:     Color
    SelectedColor:   Color
    UnselectedColor: Color
}

[<RequireQualifiedAccess>]
type MetaChipKind =
| Priority
| Category
| Due

module Styles =
    // ── High-contrast neumorphism helpers ─────────────────────────────────────
    // Raised physical surface = a DEFINED dark drop shadow falling bottom-right (the
    // lift), biased to the bottom-right so it reads as cast by a top-left light. A thin
    // inner light bevel on the top edge catches light on dark fills (e.g. the teal Add
    // button); on near-white fills the outer dark shadow does all the lifting.
    // Emits ONE CSS `boxShadow` string on both web and native: RN 0.86 (New Arch / Fabric)
    // supports `boxShadow` (incl. `inset` + multiple layers) — the New-arch `shadow` helper is
    // itself a boxShadow underneath. Using boxShadow on native (not the single-outer `shadow`
    // fallback) is what gives iOS/Android the same carved neumorphism as web.
    let private neuRaised (palette: SemanticPalette) (blur: int) (offset: int) =
        [| boxShadow (sprintf "%dpx %dpx %dpx %s, inset 1px 1px 2px %s"
                        offset offset blur palette.SurfaceShadow.ToCssString
                        palette.SurfaceHighlight.ToCssString) |]

    let private neuRaisedStrong (palette: SemanticPalette) (blur: int) (offset: int) =
        [| boxShadow (sprintf "%dpx %dpx %dpx %s, inset 1px 1px 2px %s, inset -1px -1px 2px rgba(0,0,0,0.14)"
                        offset offset blur palette.SurfaceShadowStrong.ToCssString
                        palette.SurfaceHighlight.ToCssString) |]

    // Inset (carved well): dark inner shadow biased to the top-left inner lip (the
    // depth) + a lighter inner bevel on the bottom-right (the lit lower edge). The
    // asymmetry (dark top-left, light bottom-right) is what reads as "carved in."
    // Beveled carve, not a hard step: a crisp dark lip top-left, a larger soft dark wall behind
    // it (graduated chamfer), and a lit rim bottom-right. The soft wall is what turns a sharp
    // edge into a beveled one. Emitted on web AND native (RN 0.86 boxShadow inset).
    let private neuInset (palette: SemanticPalette) (blur: int) (offset: int) =
        [| boxShadow (sprintf "inset %dpx %dpx %dpx %s, inset %dpx %dpx %dpx %s, inset -%dpx -%dpx %dpx %s"
                        offset offset (blur + 3) palette.SurfaceShadowStrong.ToCssString
                        (offset * 2) (offset * 2) (blur * 2) palette.SurfaceShadow.ToCssString
                        offset offset (blur + 2) palette.SurfaceHighlight.ToCssString) |]

    // Carved rim (scale-independent): a SHARP metallic channel that reads identically at any
    // container size. neuInset above works on the small theme-toggle track but breaks on large
    // panels: its wide `offset*2 / blur*2` wall spreads a soft dark wash tens of px into the
    // interior, so a big form reads as a blended gradient instead of a rim. This helper keeps
    // every layer tight against the border — nothing reaches the interior — so the cutout edge
    // stays crisp whether the surface is 60px or 600px wide:
    //   • a hard dark inner lip top-left (blur 1, the shadowed wall of the cut),
    //   • a soft-but-shallow dark backing just behind it (blur 4, still edge-bound, gives the
    //     lip thickness without bleeding),
    //   • a bright specular catch bottom-right (the lit opposite wall — the "metallic" glint).
    let private neuInsetRim (palette: SemanticPalette) =
        [| boxShadow (sprintf "inset 2px 2px 1px %s, inset 3px 3px 4px %s, inset -2px -2px 1px %s, inset -3px -3px 4px %s"
                        palette.SurfaceShadowStrong.ToCssString
                        palette.SurfaceShadow.ToCssString
                        palette.SurfaceHighlight.ToCssString
                        palette.SurfaceHighlight.ToCssString) |]

    // Rim as a TOP overlay. An inset box-shadow painted on a container renders BELOW that
    // container's children, so anything that scrolls or slides to the container edge (the category
    // pill row, the swipe-to-delete surface) passes OVER the rim — it looks like it floats above
    // the lip. To make content pass UNDER the lip instead, the rim must paint ABOVE the children:
    // put it on a transparent, absolutely-filling sibling rendered LAST, with pointer-events off
    // (set `ignorePointerEvents = true` on the Rn.View). `radius` must match the container's.
    let cutoutRimOverlay =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (radius: int) ->
                makeViewStyles {
                    Position.Absolute
                    top 0
                    left 0
                    right 0
                    bottom 0
                    borderRadius radius
                    neuInsetRim palette
                }
        )

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
                        shadow (Color.BlackAlpha 0.12) 28 (0, 14)
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
                        neuRaised palette 18 5
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

    // Primary Add button: radius override + neumorphic raised shadow.
    // Framework Button hardcodes radius 4; caller styles win (appended last).
    let addButton =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    borderRadius 16
                    AlignSelf.Stretch
                    minHeight 48
                    paddingVertical 14
                    neuRaisedStrong palette 16 5
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
            borderRadius 12
        }

    // Selected filter tab: PRESSED into the raised bar (inset), not a floating raised pill. A
    // floating pill in a track reads as a slider thumb; a pressed-in key reads as the active tab.
    let filterTabCellSelected =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    flex 1
                    minHeight 44
                    AlignItems.Center
                    JustifyContent.Center
                    borderRadius 12
                    backgroundColor palette.FormBackground
                    neuInset palette 8 2
                }
        )

    let categoryScroll =
        makeScrollViewStyles {
            flexGrow 0
            flexShrink 0
            AlignSelf.Stretch
        }

    // Spans the composer panel's padding (marginHorizontal -16) so the pill row reaches the inner
    // walls, then clips there: scrolled pills slide UNDER the panel edge instead of stopping short
    // of it. Vertical padding leaves room for the raised-pill shadows so Overflow.Hidden (horizontal
    // clip) never flattens their tops/bottoms.
    let categorySlideWrap =
        makeViewStyles {
            marginHorizontal -16
            Overflow.Hidden
        }

    let categoryScrollContent =
        makeViewStyles {
            FlexDirection.Row
            FlexWrap.Nowrap
            gap 8
            paddingTop 6
            paddingBottom 14
            paddingHorizontal 16
            AlignItems.Center
        }

    // Neumorphic category pill. Unselected = RAISED (protrudes from the surface). Selected =
    // pressed/carved (INSET) with an accent border — a pressed button reads as "chosen", not raised.
    let categoryPill =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (bg: Color) (border: Color) (isSelected: bool) ->
                makeViewStyles {
                    minHeight 44
                    flexShrink 0
                    borderRadius 999
                    borderWidth (if isSelected then 2 else 1)
                    paddingVertical 4
                    paddingHorizontal 10
                    JustifyContent.Center
                    backgroundColor bg
                    borderColor (if isSelected then palette.Accent else border)
                    if isSelected then
                        neuInset palette 8 3
                    else
                        neuRaised palette 10 3
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

    // Short engraved groove under a section label: a dark hairline with a light companion line
    // directly beneath (the shadowed top wall + lit bottom wall of a cut channel). A small carved
    // accent, the neumorphic separator language from the reference kits.
    let labelGroove =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    width 24
                    height 1
                    borderRadius 1
                    marginBottom 10
                    backgroundColor palette.SurfaceShadow
                    boxShadow (sprintf "0px 1px 0px %s" palette.SurfaceHighlight.ToCssString)
                }
        )

    // One short engraved dash of a grip cluster (carved: dark hairline + light companion beneath).
    let gripDash =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    width 12
                    height 1
                    borderRadius 1
                    marginVertical 2
                    backgroundColor palette.SurfaceShadow
                    boxShadow (sprintf "0px 1px 0px %s" palette.SurfaceHighlight.ToCssString)
                }
        )

    // Grip cluster: a stack of short engraved dashes used as a neumorphic drag-affordance handle
    // (e.g. the leading edge of a swipeable row). Decorative — hide from the accessibility tree.
    let gripCluster =
        makeViewStyles {
            AlignItems.Center
            JustifyContent.Center
            flexShrink 0
            paddingRight 4
            opacity 0.65
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
            fun (palette: SemanticPalette) (bg: Color) ->
                makeViewStyles {
                    paddingVertical 6
                    paddingHorizontal 12
                    borderRadius 999
                    backgroundColor bg
                    neuRaised palette 12 4
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
                    neuRaised palette 12 4
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

    // RAISED field for the composer text input + picker. The carved rim lives on the composer
    // PANEL (the outer container); each field protrudes from that recess like the toggle thumb
    // sits in its track — NOT a second carved cutout nested inside the first. Lighter fill +
    // raised shadow so it reads as sitting up out of the panel floor.
    let composerInputWell =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    AlignSelf.Stretch
                    borderRadius 20
                    backgroundColor palette.CardBackground
                    neuRaised palette 10 3
                }
        )

    // Inset well for the theme toggle segmented control (pressed-in track).
    let themeToggleWell =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    borderRadius 999
                    padding 3
                    backgroundColor palette.ThemeTrackBackground
                    neuInsetRim palette
                }
        )

    // Raised bar for the filter tab bar — the selected tab is pressed INTO it (inset), giving the
    // physical "this key is pushed in" tab read rather than a slider thumb on a track.
    let filterTabsWell =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    FlexDirection.Row
                    AlignSelf.Stretch
                    borderRadius 16
                    padding 4
                    gap 4
                    backgroundColor palette.CardBackground
                    neuRaised palette 12 4
                }
        )

    // Raised well for the todo row checkbox (physical press feel). Slightly darker
    // than the row so it stands out, with a raised dual shadow.
    let checkboxWell =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    width 36
                    height 36
                    borderRadius 10
                    backgroundColor palette.PageBackground
                    JustifyContent.Center
                    AlignItems.Center
                    neuRaised palette 8 2
                }
        )

    let composerPanel =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (isHandheld: bool) ->
                makeViewStyles {
                    gap (if isHandheld then 12 else 14)
                    padding 16
                    borderRadius 28
                    // The carved rim is painted by a `cutoutRimOverlay` sibling on TOP (see Todos.fs)
                    // so the scrolling category pill row slides UNDER the lip, not over it. Position
                    // relative so that absolute overlay anchors to this panel.
                    Position.Relative
                    backgroundColor palette.ThemeTrackBackground
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
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    AlignSelf.Stretch
                    borderRadius 999
                    // Sharp carved rim (scale-independent) so the search well reads like the toggle cutout.
                    backgroundColor palette.ThemeTrackBackground
                    neuInsetRim palette
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
            gap 16
            marginTop 4
        }

    let todoRowOuter =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (mode: AppearanceMode) ->
                makeViewStyles {
                    borderRadius 16
                    Overflow.Hidden
                    // Anchor for the carved-rim overlay (see Todos.fs): the rim lives on this OUTER
                    // frame and paints on top, so the sliding swipe surface passes under the lip.
                    Position.Relative
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
                    // Flat recessed floor of the row cutout. The carved rim is painted by the
                    // `cutoutRimOverlay` on the OUTER row frame (todoRowOuter) so the swipe surface
                    // slides UNDER the lip — this inner surface must stay flat (no nested carve).
                    backgroundColor palette.ThemeTrackBackground
                    borderRadius 16
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
                    borderRadius 12
                    borderWidth 0
                    JustifyContent.Center
                    AlignItems.Center
                    flexShrink 0
                    backgroundColor Color.Transparent
                    neuRaised palette 10 3
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

    // FULL-WIDTH Danger background behind the sliding content. The entire row goes red — as the
    // content slides left, red fills the whole card area behind it (not just an 80px slot on the
    // right). The delete text sits right-aligned with padding so it stays in the revealed area.
    let swipeDeleteSlot =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    Position.Absolute
                    top 0
                    bottom 0
                    left 0
                    right 0
                    paddingRight 14
                    JustifyContent.Center
                    AlignItems.FlexEnd
                    backgroundColor palette.Danger
                }
        )

    let swipeDeleteButtonText =
        makeTextStyles {
            color Color.White
            fontSize 14
            FontWeight.W600
        }

    // The sliding content surface. FLAT + opaque (recessed floor tone) so it slides UNDER the row's
    // fixed carved rim (painted by cutoutRimOverlay on todoRowOuter) and cleanly occludes the red
    // Danger slot behind it until swiped. No raised shadow here — the rim belongs to the outer frame,
    // not the sliding element, so the surface reads as content sliding within the cutout.
    let swipeContentBase (palette: SemanticPalette) =
        makeViewStyles {
            backgroundColor palette.ThemeTrackBackground
            borderRadius 16
            Overflow.Hidden
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
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) ->
                makeViewStyles {
                    width 72
                    minHeight 44
                    paddingHorizontal 14
                    borderRadius 16
                    JustifyContent.Center
                    AlignItems.Center
                    backgroundColor palette.Danger
                    flexShrink 0
                }
        )

    let metaChip =
        ViewStyles.Memoize(
            fun (palette: SemanticPalette) (chipBg: Color) (chipBorder: Color) ->
                makeViewStyles {
                    paddingVertical 2
                    paddingHorizontal 6
                    borderRadius 999
                    borderWidth 1
                    backgroundColor chipBg
                    borderColor chipBorder
                    AlignSelf.FlexStart
                    neuRaised palette 8 3
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
            BorderColor     = palette.CardBorder
            SelectedColor   = palette.Accent
            UnselectedColor = palette.TextSecondary
        }

    let filterTabTheme (tabBase: TabTheme) : LC.Tab.Theme =
        {
            SelectedColor   = tabBase.SelectedColor
            UnselectedColor = tabBase.UnselectedColor
        }

    let tabsScrollTheme (tabBase: TabTheme) : Theme =
        {
            BackgroundColor = tabBase.BackgroundColor
            BorderColor     = tabBase.BorderColor
            BorderWidth     = 1
        }

    let priorityColor (palette: SemanticPalette) (priority: TodoPriority) =
        match priority with
        | TodoPriority.High   -> palette.PriorityHigh
        | TodoPriority.Medium -> palette.PriorityMedium
        | TodoPriority.Low    -> palette.PriorityLow

    let priorityChipColors (palette: SemanticPalette) (priority: TodoPriority) =
        match priority with
        | TodoPriority.High   -> palette.PriorityHighSoft, palette.PriorityHigh, palette.PriorityHigh
        | TodoPriority.Medium -> palette.PriorityMediumSoft, palette.PriorityMedium, palette.PriorityMedium
        | TodoPriority.Low    -> palette.PriorityLowSoft, palette.PriorityLow, palette.PriorityLow

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
