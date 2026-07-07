module LibClient.Components.Input_Picker

open Fable.React

open LibClient
open LibClient.Components
open LibClient.Components.Input.PickerModel
open LibClient.Responsive

open Rn.Components
open Rn.Styles

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

// Wrapper so picker hooks run in a real component render, not inside
// LC.With.ScreenSize's useMemo factory (Rules of Hooks).
[<RequireQualifiedAccess>]
type private PickerHost =
    [<Component>]
    static member Render<'Item when 'Item : comparison>(
            items: Items<'Item>,
            itemView: PickerItemView<'Item>,
            value: SelectableValue<'Item>,
            validity: InputValidity,
            screenSize: ScreenSize,
            showSearchBar: bool,
            label: string option,
            placeholder: string option,
            testId: string option,
            pickerId: string option,
            styles: ViewStyles array option,
            xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles> option
        ) : ReactElement =
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
            pickerId,
            styles,
            xLegacyStyles
        )

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
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore

        let showSearchBar = defaultArg showSearchBar true
        let pickerIdHook = Hooks.useState (System.Guid.NewGuid())

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    PickerHost.Render(
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
