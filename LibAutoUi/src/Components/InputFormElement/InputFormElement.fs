[<AutoOpen>]
module LibAutoUi.Components.InputFormElement

open System
open Fable.React

open LibClient
open LibClient.Components
open LibClient.Components.Input_Picker
open LibClient.Components.Input.PickerModel
open LibAutoUi.Types
open LibAutoUi.TypeExtensions

open ReactXP.Components
open ReactXP.Styles

type PrimitiveInputFieldProps = {
    OnChange:   InputValue -> unit
    MaybeValue: Option<InputValue>
}

type PrimitiveInputComponents = Map<InputType, PrimitiveInputFieldProps -> seq<ReactElement> -> ReactElement>

[<RequireQualifiedAccess>]
module private Styles =
    let private labelColor = Color.Hex "#999999"
    let private inputBorderColor = Color.Hex "#cccccc"

    let view =
        makeViewStyles {
            padding 4
            paddingRight 0
        }

    let primitive =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Center
        }

    let inlineLabel =
        makeViewStyles {
            flex 1
            flexBasis 0
            marginRight 10
        }

    let inlineLabelText =
        makeTextStyles {
            color labelColor
        }

    let inlineValue =
        makeViewStyles {
            flex 3
            flexBasis 0
        }

    let record =
        makeViewStyles {
            marginVertical 8
        }

    let recordLabel =
        makeTextStyles {
            color labelColor
        }

    let indentedFields =
        makeViewStyles {
            paddingLeft 24
        }

    let unionCaseSelection =
        makeViewStyles {
            FlexDirection.Row
        }

    let picker =
        makeViewStyles {
            border 1 inputBorderColor
            borderRadius 0
            backgroundColor Color.White
        }

    let rangeValueSelection =
        makeViewStyles {
            FlexDirection.Row
        }

    let optionRow =
        makeViewStyles {
            FlexDirection.Row
        }

module private Helpers =
    let inlineLabelView (displayName: string) : ReactElement =
        RX.View(
            styles = [| Styles.inlineLabel |],
            children =
                elements {
                    LC.UiText(
                        value = displayName,
                        styles = [| Styles.inlineLabelText |]
                    )
                }
        )

    let findRangeIndex (rangeList: List<obj>) (value: obj) : Option<int> =
        rangeList
        |> List.tryFindIndex (fun item -> item = value)

    let renderRangePicker
            (path: Path)
            (displayName: string)
            (range: NonemptyList<obj>)
            (acc: Accumulator)
            (onChangeFromRange: Path -> obj -> Type -> unit)
            (formType: Type)
        : ReactElement =
        let rangeList = range.ToList
        let indices = rangeList |> List.mapi (fun i _ -> i) |> OrderedSet.ofList

        let maybeSelectedIndex =
            acc.GetCurrentDerivedValue path
            |> Option.bind (findRangeIndex rangeList)

        RX.View(
            styles = [| Styles.rangeValueSelection |],
            children =
                elements {
                    inlineLabelView displayName

                    RX.View(
                        styles = [| Styles.inlineValue |],
                        children =
                            elements {
                                LC.Input.Picker(
                                    items = Static (indices, fun index -> rangeList.[index].ToString()),
                                    itemView = PickerItemView.Default (fun index -> {| Label = rangeList.[index].ToString() |}),
                                    value =
                                        ExactlyOne (
                                            maybeSelectedIndex,
                                            fun index ->
                                                range.Item index
                                                |> Option.get
                                                |> fun objValue -> onChangeFromRange path objValue formType
                                        ),
                                    validity = Valid,
                                    showSearchBar = false,
                                    styles = [| Styles.picker |]
                                )
                            }
                    )
                }
        )

    let renderUnionPicker
            (path: Path)
            (displayName: string)
            (caseInputForms: List<CaseInputForm>)
            (acc: Accumulator)
            (onChange: Path -> InputValue -> unit)
        : ReactElement =
        let caseIndices =
            caseInputForms
            |> List.mapi (fun i _ -> i)
            |> OrderedSet.ofList

        let maybeSelectedIndex =
            match acc.GetCurrentValue path with
            | Some (NumericValue n) -> Some (int n)
            | _                     -> None

        RX.View(
            styles = [| Styles.unionCaseSelection |],
            children =
                elements {
                    inlineLabelView displayName

                    RX.View(
                        styles = [| Styles.inlineValue |],
                        children =
                            elements {
                                LC.Input.Picker(
                                    items = Static (caseIndices, fun index -> caseInputForms.[index].DisplayName),
                                    itemView = PickerItemView.Default (fun index -> {| Label = caseInputForms.[index].DisplayName |}),
                                    value =
                                        ExactlyOne (
                                            maybeSelectedIndex,
                                            fun index -> onChange path (NumericValue (decimal index))
                                        ),
                                    validity = Valid,
                                    showSearchBar = false,
                                    styles = [| Styles.picker |]
                                )
                            }
                    )
                }
        )

type UIAuto with
    [<Component>]
    static member InputFormElement<'T>(
            form:                     InputForm,
            accumulator:              Accumulator,
            onChange:                 Path -> InputValue -> unit,
            onChangeFromRange:        Path -> obj -> Type -> unit,
            primitiveInputComponents: PrimitiveInputComponents,
            ?xLegacyStyles:           List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:                     string
        ) : ReactElement =
        key |> ignore
        xLegacyStyles |> ignore

        let path = form.Path
        let displayName = form.DisplayName

        let rec render (form: InputForm) (acc: Accumulator) : ReactElement =
            let path = form.Path
            let displayName = form.DisplayName

            if acc.HiddenPaths.DoesNotContain path then
                match acc.ValueRanges.TryFind path with
                | Some range ->
                    Helpers.renderRangePicker path displayName range acc onChangeFromRange form.Type

                | None ->
                    match form.FormType with
                    | InputFormType.Primitive inputType ->
                        RX.View(
                            styles = [| Styles.primitive |],
                            children =
                                elements {
                                    match inputType with
                                    | UnitInput -> ()
                                    | _         -> Helpers.inlineLabelView displayName

                                    RX.View(
                                        styles = [| Styles.inlineValue |],
                                        children =
                                            elements {
                                                match primitiveInputComponents.TryFind inputType with
                                                | Some primitiveComponent ->
                                                    let primitiveProps = {
                                                        OnChange   = onChange path
                                                        MaybeValue = acc.GetCurrentValue path
                                                    }
                                                    primitiveComponent primitiveProps [||]

                                                | None ->
                                                    LC.UiText(
                                                        value = sprintf "No input component found for primitive type %A" inputType
                                                    )
                                            }
                                    )
                                }
                        )

                    | InputFormType.Option innerForm ->
                        let optionPath = path.Append PathSegment.Option

                        let maybeIsChecked =
                            match acc.GetCurrentValue optionPath with
                            | Some (BooleanValue v) -> Some v
                            | _                     -> None

                        let isSome =
                            match acc.GetCurrentValue optionPath with
                            | Some (BooleanValue true) -> true
                            | _                        -> false

                        element {
                            RX.View(
                                styles = [| Styles.optionRow |],
                                children =
                                    elements {
                                        Helpers.inlineLabelView displayName

                                        RX.View(
                                            styles = [| Styles.inlineValue |],
                                            children =
                                                elements {
                                                    LC.Input.Checkbox(
                                                        value = maybeIsChecked,
                                                        onChange =
                                                            (fun _ ->
                                                                onChange
                                                                    optionPath
                                                                    (BooleanValue (maybeIsChecked |> Option.getOrElse false |> not))
                                                            ),
                                                        validity = Valid
                                                    )
                                                }
                                        )
                                    }
                            )

                            if isSome then
                                render innerForm acc
                        }

                    | InputFormType.List innerForm ->
                        element {
                            LC.UiText(value = sprintf "List %s" displayName)
                            render innerForm acc
                        }

                    | InputFormType.Record fieldForms ->
                        let indentedFieldViews =
                            fieldForms
                            |> List.map (fun fieldForm -> render fieldForm acc)
                            |> List.toArray

                        RX.View(
                            styles = [| Styles.record |],
                            children =
                                elements {
                                    LC.UiText(
                                        value = displayName,
                                        styles = [| Styles.recordLabel |]
                                    )

                                    RX.View(
                                        styles = [| Styles.indentedFields |],
                                        children = indentedFieldViews
                                    )
                                }
                        )

                    | InputFormType.Union caseInputForms ->
                        let maybeSelectedCaseIndex =
                            match acc.GetCurrentValue path with
                            | Some (NumericValue n) -> Some (int n)
                            | _                     -> None

                        RX.View(
                            children =
                                elements {
                                    Helpers.renderUnionPicker path displayName caseInputForms acc onChange

                                    match maybeSelectedCaseIndex with
                                    | Some selectedCaseIndex ->
                                        let caseInputForm = caseInputForms |> List.item selectedCaseIndex
                                        let caseFieldViews =
                                            caseInputForm.FieldForms
                                            |> List.map (fun fieldForm -> render fieldForm acc)
                                            |> List.toArray

                                        RX.View(
                                            styles = [| Styles.indentedFields |],
                                            children = caseFieldViews
                                        )
                                    | None -> ()
                                }
                        )
            else
                noElement

        RX.View(
            styles = [| Styles.view |],
            children = [| render form accumulator |]
        )
