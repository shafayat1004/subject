namespace LibClient.Components.Input

module ChoiceListItem =
    type Label =
    | String of string
    | Children

    [<RequireQualifiedAccess>]
    type IconPosition =
    | Left
    | Right


namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Components.Input.ChoiceList
open LibClient.Components.Input.ChoiceListItem
open LibClient.Icons

open ReactXP.Components
open ReactXP.Styles

[<AutoOpen>]
module ChoiceListItem_fs =

    [<RequireQualifiedAccess>]
    module private ChoiceListItemStyles =
        let view =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                flex 1
                padding 4
            }

        let label =
            makeViewStyles {
                flex 1
            }

        let labelLeft =
            makeViewStyles {
                paddingLeft 12
            }

        let labelRight =
            makeViewStyles {
                paddingRight 12
            }

        let iconChecked =
            makeTextStyles {
                fontSize 20
                color Color.DevGreen
            }

        let iconUnchecked =
            makeTextStyles {
                fontSize 20
                color (Color.Grey "aa")
            }

        let labelStyles (iconPosition: IconPosition) =
            [|
                label
                match iconPosition with
                | IconPosition.Left  -> labelLeft
                | IconPosition.Right -> labelRight
            |]

    [<RequireQualifiedAccess>]
    module private ChoiceListItemHelpers =
        let isCheckboxMode (value: SelectableValue<'T>) =
            match value with
            | SelectableValue.AtLeastOne _ | SelectableValue.Any _ -> true
            | SelectableValue.AtMostOne _ | SelectableValue.ExactlyOne _ -> false

        let accessibilityRole (value: SelectableValue<'T>) =
            if isCheckboxMode value then
                AccessibilityRole.CheckBox
            else
                AccessibilityRole.Radio

        let accessibilityState (value: SelectableValue<'T>) (isSelected: bool) =
            if isCheckboxMode value then
                { AccessibilityStateRecord.empty with Checked = Some isSelected }
            else
                { AccessibilityStateRecord.empty with Selected = Some isSelected }

        let selectionIcon (value: SelectableValue<'T>) (isSelected: bool) =
            match value with
            | SelectableValue.AtLeastOne _ | SelectableValue.Any _ ->
                if isSelected then Icon.CheckboxChecked else Icon.CheckboxEmpty
            | SelectableValue.AtMostOne _ | SelectableValue.ExactlyOne _ ->
                if isSelected then Icon.RadioButtonFilled else Icon.RadioButtonEmpty

        let maybeLabelString (label: Label) =
            match label with
            | Label.String text -> Some text
            | Label.Children -> None

        let resolveTestId (maybeLabel: Option<string>) (value: 'T) =
            match maybeLabel with
            | Some label -> Some (A11ySlug.testId "choice-list-item" label)
            | None -> Some (sprintf "choice-list-item-%A" value)

    type Constructors.LC.Input with
        [<Component>]
        static member ChoiceListItem<'T when 'T: comparison>(
                value: 'T,
                group: Group<'T>,
                ?children: ReactChildrenProp,
                ?label: Label,
                ?iconPosition: IconPosition,
                ?styles: array<ViewStyles>,
                ?key: string,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            key |> ignore
            xLegacyStyles |> ignore

            let label = defaultArg label Label.Children
            let iconPosition = defaultArg iconPosition IconPosition.Left
            let isSelected = group.IsSelected value
            let maybeLabelString = ChoiceListItemHelpers.maybeLabelString label
            let a11yRole = ChoiceListItemHelpers.accessibilityRole group.Value
            let a11yState = ChoiceListItemHelpers.accessibilityState group.Value isSelected
            let iconStyles = if isSelected then ChoiceListItemStyles.iconChecked else ChoiceListItemStyles.iconUnchecked

            let iconElement =
                LC.Icon(
                    icon = ChoiceListItemHelpers.selectionIcon group.Value isSelected,
                    styles = [| iconStyles |]
                )

            RX.View(
                styles =
                    [|
                        ChoiceListItemStyles.view
                        yield! (styles |> Option.defaultValue [||])
                    |],
                children =
                    elements {
                        if iconPosition = IconPosition.Left then
                            iconElement

                        match label with
                        | Label.Children ->
                            RX.View(
                                styles = ChoiceListItemStyles.labelStyles iconPosition,
                                children = (children |> Option.defaultValue [||])
                            )
                        | Label.String text ->
                            RX.View(
                                styles = ChoiceListItemStyles.labelStyles iconPosition,
                                children = [| LC.UiText text |]
                            )

                        if iconPosition = IconPosition.Right then
                            iconElement

                        LC.Pressable(
                            onPress = group.Toggle value,
                            ?label = maybeLabelString,
                            ?testId = ChoiceListItemHelpers.resolveTestId maybeLabelString value,
                            role = a11yRole,
                            state = a11yState,
                            overlay = true,
                            componentName = "LC.Input.ChoiceListItem"
                        )
                    }
            )
