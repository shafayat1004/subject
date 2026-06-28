module LibClient.Components.Input_Picker

open Fable.React

open LibClient
open LibClient.Components
open LibClient.Components.Input.PickerModel

open ReactXP.Components
open ReactXP.Styles

type SelectableValue<'T when 'T : comparison> = LibClient.Input.SelectableValue<'T>
let AtMostOne  = SelectableValue.AtMostOne
let ExactlyOne = SelectableValue.ExactlyOne
let AtLeastOne = SelectableValue.AtLeastOne
let Any        = SelectableValue.Any

let Static = Items.Static
let Async  = Items.Async

let Default = PickerItemView.Default
let Custom  = PickerItemView.Custom

type PropItemViewFactory =
    static member Make<'T when 'T : comparison> (itemToLabel: 'T -> NonemptyString) : PickerItemView<'T> =
        (fun (item: 'T) ->
            {|
                Label = (itemToLabel item).Value
            |}
        )
        |> PickerItemView.Default

    static member Make<'T when 'T : comparison> (itemToLabel: 'T -> string) : PickerItemView<'T> =
        (fun (item: 'T) ->
            {|
                Label = itemToLabel item
            |}
        )
        |> PickerItemView.Default

    static member Make<'T when 'T : comparison> (itemToVisuals: 'T -> PickerItemVisuals) : PickerItemView<'T> =
        PickerItemView.Default itemToVisuals

type LibClient.Components.Constructors.LC.Input with
    [<Component>]
    static member Picker<'T when 'T : comparison>(
            items: Items<'T>,
            itemView: PickerItemView<'T>,
            value: SelectableValue<'T>,
            validity: InputValidity,
            ?showSearchBar: bool,
            ?label: string,
            ?placeholder: string,
            ?testId: string,
            ?styles: array<ViewStyles>,
            ?key: string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore

        let showSearchBar = defaultArg showSearchBar true
        let pickerIdHook = Hooks.useState (System.Guid.NewGuid())

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    LibClient.Components.Input.PickerInternals.Base.renderPickerBase(
                        items,
                        itemView,
                        value,
                        validity,
                        screenSize,
                        showSearchBar,
                        label,
                        placeholder,
                        testId,
                        Some (pickerIdHook.current.ToString()),
                        styles,
                        xLegacyStyles
                    )
        )
