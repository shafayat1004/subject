[<AutoOpen>]
module Rn.Components.Picker

open Rn.Helpers

open Fable.Core.JsInterop
open Fable.Core
open Fable.React
open LibClient

// Android only: dialog = modal wheel, dropdown = inline spinner. Ignored on iOS + web.
[<StringEnum; RequireQualifiedAccess>]
type Mode =
| [<CompiledName("dialog")>]   Dialog
| [<CompiledName("dropdown")>] Dropdown

type PickerPropsItem = {
    label: string
    value: string
}

module private PickerRN =
    let PickerComponent : obj = import "Picker" "@react-native-picker/picker"
    // Picker.Item is exposed as a static property on the Picker class
    let PickerItem : obj = PickerComponent?Item

    let unboxStyles (styles: array<Rn.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    let renderItem (item: PickerPropsItem) : ReactElement =
        let itemProps = createEmpty
        itemProps?key   <- item.value
        itemProps?label <- item.label
        itemProps?value <- item.value
        ReactBindings.React.createElement(PickerItem, itemProps, [||])

type Rn.Components.Constructors.Rn with
    static member Picker(
        items:          array<PickerPropsItem>,
        selectedValue:  string,
        onValueChange:  string -> int -> unit,
        ?mode:          Mode,
        ?testId:        string,
        ?label:         string,
        ?styles:        array<Rn.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
    ) =
        let __props = createEmpty

        __props?selectedValue <- selectedValue
        __props?onValueChange <- onValueChange
        mode |> Option.iter (fun v -> __props?mode <- v)

        Rn.RnPrimitives.assignTestId __props testId
        Rn.RnPrimitives.assignAccessibility
            __props
            label
            (Some (box "combobox"))
            None None None None None None None
            None

        __props?style <- PickerRN.unboxStyles styles

        match xLegacyStyles with
        | Option.None | Option.Some [] -> ()
        | Option.Some ls -> __props?__style <- ls

        Rn.RnPrimitives.createElement
            PickerRN.PickerComponent
            __props
            (items |> Array.map PickerRN.renderItem |> tellReactArrayKeysAreOkay)
