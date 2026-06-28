namespace LibClient.Components.Input

type Label =
| String of string
| Children


namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Icons
open LibClient.Components.Input

open ReactXP.Components
open ReactXP.Styles

[<AutoOpen>]
module Input_CheckboxComponent =

    module LC =
        module Input =
            module Checkbox =
                type Theme = {
                    IconCheckedColor: Color
                    IconUncheckedColor: Color
                    LabelColor: Color
                    IconSize: int
                }

    open LC.Input.Checkbox

    [<RequireQualifiedAccess>]
    module private Styles =
        let tapCapture =
            makeViewStyles {
                trbl -10 -10 -10 -10
            }

        let topLevel =
            makeViewStyles {
                Overflow.VisibleForTapCapture
            }

        let row =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                flex 1
                Overflow.VisibleForTapCapture
            }

        let labelBlock =
            makeViewStyles {
                flex 1
                paddingLeft 12
            }

        let invalidReason =
            makeTextStyles {
                color Color.DevRed
            }

        // Key on primitives (CSS string, int), not Theme — fresh Theme refs defeat fast-memoize.
        let iconTheme =
            TextStyles.Memoize(
                fun (iconSize: int) (colorCss: string) ->
                    makeTextStyles {
                        fontSize iconSize
                        color (Color.InternalString colorCss)
                    }
            )

        let labelTextTheme =
            TextStyles.Memoize(
                fun (labelColorCss: string) ->
                    makeTextStyles {
                        color (Color.InternalString labelColorCss)
                    }
            )

    let private legacyTopLevelStyles (xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles> option) : array<ViewStyles> =
        match xLegacyStyles with
        | Some ls ->
            match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
            | []     -> [||]
            | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
        | None -> [||]

    let private resolveAccessibilityLabel (label: Label) (accessibilityLabel: string option) (testId: string option) : string =
        match label with
        | String text -> text
        | Children ->
            accessibilityLabel
            |> Option.orElse testId
            |> Option.defaultValue "Checkbox"

    let private resolveTestId (label: Label) (testId: string option) (accessibilityLabel: string) : string =
        testId
        |> Option.orElse (
            match label with
            | String text -> Some (A11ySlug.testId "input-checkbox" text)
            | Children    -> Some (A11ySlug.testId "input-checkbox" accessibilityLabel)
        )
        |> Option.defaultValue "input-checkbox"

    let private checkedState (value: Option<bool>) : AccessibilityStateRecord =
        { AccessibilityStateRecord.empty with Checked = value }

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member Checkbox(
                onChange: bool -> unit,
                value: Option<bool>,
                validity: InputValidity,
                ?children: ReactChildrenProp,
                ?label: Label,
                ?accessibilityLabel: string,
                ?testId: string,
                ?styles: array<ViewStyles>,
                ?theme: Theme -> Theme,
                ?key: string,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            key |> ignore

            let label = defaultArg label Children
            let theTheme = Themes.GetMaybeUpdatedWith theme
            let isInvalid = validity.IsInvalid
            let childElements = children |> Option.map tellReactArrayKeysAreOkay |> Option.defaultValue [||]

            let a11yLabel = resolveAccessibilityLabel label accessibilityLabel testId
            let resolvedTestId = resolveTestId label testId a11yLabel

            let onPress (_e: ReactEvent.Action) : unit =
                onChange (value |> Option.getOrElse false |> not)

            let checkboxIcon =
                match value with
                | Some true  -> Icon.CheckboxChecked
                | Some false -> Icon.CheckboxEmpty
                | None       -> Icon.CheckboxUnknown

            let iconColorCss =
                if isInvalid then
                    Color.DevRed.ToCssString
                else
                    match value with
                    | Some true -> theTheme.IconCheckedColor.ToCssString
                    | Some false
                    | None        -> theTheme.IconUncheckedColor.ToCssString

            RX.View(
                styles =
                    [|
                        Styles.topLevel
                        yield! legacyTopLevelStyles xLegacyStyles
                        yield! styles |> Option.defaultValue [||]
                    |],
                children =
                    elements {
                        RX.View(
                            styles = [| Styles.row |],
                            children =
                                elements {
                                    LC.Icon(
                                        icon = checkboxIcon,
                                        styles = [| Styles.iconTheme theTheme.IconSize iconColorCss |]
                                    )

                                    match label with
                                    | String text ->
                                        RX.View(
                                            styles = [| Styles.labelBlock |],
                                            children =
                                                elements {
                                                    LC.UiText(
                                                        text,
                                                        styles = [| Styles.labelTextTheme theTheme.LabelColor.ToCssString |]
                                                    )
                                                }
                                        )
                                    | Children ->
                                        RX.View(
                                            styles = [| Styles.labelBlock |],
                                            children = childElements
                                        )

                                    LC.Pressable(
                                        onPress = onPress,
                                        label = a11yLabel,
                                        role = AccessibilityRole.CheckBox,
                                        testId = resolvedTestId,
                                        state = checkedState value,
                                        overlay = true,
                                        styles = [| Styles.tapCapture |],
                                        componentName = "LC.Input.Checkbox"
                                    )
                                }
                        )

                        match validity.InvalidReason with
                        | Some reason ->
                            LC.Text(
                                reason,
                                styles = [| Styles.invalidReason |]
                            )
                        | None ->
                            noElement
                    }
            )
