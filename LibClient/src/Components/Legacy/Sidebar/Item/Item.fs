namespace LibClient.Components.Legacy.Sidebar

open LibClient

module Item =
    type [<RequireQualifiedAccess>] Right =
    | Count of int
    | Icon  of (int -> LibClient.Icons.Icon)

    type Value =
    | Primary   of Label: string * MaybeLeftIcon: Option<int -> LibClient.Icons.Icon> * MaybeRight: Option<Right>
    | Secondary of Label: string

namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Accessibility

open ReactXP.Components
open ReactXP.Styles

open Legacy.Sidebar.Item

[<AutoOpen>]
module Legacy_Sidebar_Item =

    module LC =
        module Legacy =
            module Sidebar =
                module Item =
                    type Theme = {
                        PrimaryBackgroundColor:           Color
                        PrimaryTextColor:                 Color
                        PrimarySelectedBackgroundColor:   Color
                        PrimarySelectedTextColor:         Color
                        SecondaryBackgroundColor:         Color
                        SecondaryTextColor:               Color
                        SecondarySelectedBackgroundColor: Color
                        SecondarySelectedTextColor:       Color
                        BottomBorderColor:                Color
                        CountBackgroundColor:             Color
                        CountTextColor:                   Color
                    }

    open LC.Legacy.Sidebar.Item

    let private labelOfValue (value: Value) =
        match value with
        | Value.Primary (label, _, _) -> label
        | Value.Secondary label         -> label

    let private itemTestId (label: string) (testId: string option) =
        testId |> Option.orElse (Some (A11ySlug.testId "legacy-sidebar-item" label))

    [<RequireQualifiedAccess>]
    module private Styles =
        let itemPrimary (theme: Theme) (isSelected: bool) =
            makeViewStyles {
                FlexDirection.Column
                paddingHorizontal 16
                Cursor.Pointer
                borderBottom      1 theme.BottomBorderColor
                height 52
                backgroundColor (
                    if isSelected then theme.PrimarySelectedBackgroundColor
                    else theme.PrimaryBackgroundColor
                )
            }

        let itemSecondary (theme: Theme) (isSelected: bool) =
            makeViewStyles {
                FlexDirection.Column
                paddingHorizontal 16
                Cursor.Pointer
                borderBottom      1 theme.BottomBorderColor
                height 40
                backgroundColor (
                    if isSelected then theme.SecondarySelectedBackgroundColor
                    else theme.SecondaryBackgroundColor
                )
            }

        let content =
            makeViewStyles {
                flex 1
                FlexDirection.Row
                AlignItems.Center
            }

        let iconLeft =
            makeViewStyles {
                marginBottom 1
                flex         0
                marginRight  10
            }

        let iconRight =
            makeViewStyles {
                marginBottom 1
                flex        0
                marginLeft  10
            }

        let textPrimary (theme: Theme) (isSelected: bool) =
            makeTextStyles {
                flex       1
                FontWeight.Normal
                fontSize   18
                color (
                    if isSelected then theme.PrimarySelectedTextColor
                    else theme.PrimaryTextColor
                )
            }

        let textSecondary (theme: Theme) (isSelected: bool) =
            makeTextStyles {
                flex       1
                FontWeight.Normal
                fontSize   16
                color (
                    if isSelected then theme.SecondarySelectedTextColor
                    else theme.SecondaryTextColor
                )
            }

        let countContainer (theme: Theme) =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                JustifyContent.Center
                Cursor.Pointer
                height            30
                minWidth          30
                paddingHorizontal 8
                flex              0
                borderRadius      15
                backgroundColor   theme.CountBackgroundColor
            }

        let countText (theme: Theme) =
            makeTextStyles {
                FontWeight.W600
                color theme.CountTextColor
            }

    type LibClient.Components.Constructors.LC.Legacy.Sidebar with
        [<Component>]
        static member Item(
                value:      Value,
                isSelected: bool,
                onPress:    ReactEvent.Action -> unit,
                ?children:  ReactChildrenProp,
                ?testId:    string,
                ?theme:     Theme -> Theme,
                ?key:       string
            ) : ReactElement =
            key |> ignore
            children |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme
            let label = labelOfValue value
            let a11yState =
                { AccessibilityStateRecord.empty with Selected = Some isSelected }

            RX.View(
                styles =
                    [|
                        match value with
                        | Value.Primary _   -> Styles.itemPrimary theTheme isSelected
                        | Value.Secondary _ -> Styles.itemSecondary theTheme isSelected
                    |],
                children =
                    elements {
                        RX.View(
                            styles = [| Styles.content |],
                            children =
                                elements {
                                    match value with
                                    | Value.Primary (primaryLabel, maybeLeftIcon, maybeRight) ->
                                        match maybeLeftIcon with
                                        | Some icon ->
                                            RX.View(
                                                styles = [| Styles.iconLeft |],
                                                children = elements { icon 22 }
                                            )
                                        | None ->
                                            noElement

                                        LC.UiText(
                                            value = primaryLabel,
                                            styles = [| Styles.textPrimary theTheme isSelected |]
                                        )

                                        match maybeRight with
                                        | Some (Right.Count count) ->
                                            RX.View(
                                                styles = [| Styles.countContainer theTheme |],
                                                children =
                                                    elements {
                                                        LC.UiText(
                                                            value = string count,
                                                            styles = [| Styles.countText theTheme |]
                                                        )
                                                    }
                                            )
                                        | Some (Right.Icon icon) ->
                                            RX.View(
                                                styles = [| Styles.iconRight |],
                                                children = elements { icon 22 }
                                            )
                                        | None ->
                                            noElement

                                    | Value.Secondary secondaryLabel ->
                                        LC.UiText(
                                            value = secondaryLabel,
                                            styles = [| Styles.textSecondary theTheme isSelected |]
                                        )
                                }
                        )

                        LC.Pressable(
                            onPress = onPress,
                            label = label,
                            role = AccessibilityRole.MenuItem,
                            state = a11yState,
                            testId = (itemTestId label testId |> Option.defaultValue (A11ySlug.testId "legacy-sidebar-item" label)),
                            overlay = true,
                            componentName = "LC.Legacy.Sidebar.Item"
                        )
                    }
            )
