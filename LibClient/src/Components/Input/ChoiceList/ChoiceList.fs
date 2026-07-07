namespace LibClient.Components.Input

open LibClient
open LibClient.Input

module ChoiceList =
    type SelectableValue<'T when 'T : comparison> = LibClient.Input.SelectableValue<'T>
    let AtMostOne  = SelectableValue.AtMostOne
    let ExactlyOne = SelectableValue.ExactlyOne
    let AtLeastOne = SelectableValue.AtLeastOne
    let Any        = SelectableValue.Any

    type Group<'T when 'T : comparison> = {
        IsSelected: 'T -> bool
        Toggle:     'T -> ReactEvent.Action -> unit
        Value:      SelectableValue<'T>
    }


namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Components.Input.ChoiceList

open Rn.Components
open Rn.Styles

[<AutoOpen>]
module ChoiceList_fs =

    [<RequireQualifiedAccess>]
    module private ChoiceListStyles =
        let invalidReason =
            makeTextStyles {
                color Color.DevRed
                padding 4
            }

    type Constructors.LC.Input with
        [<Component>]
        static member ChoiceList<'T when 'T: comparison>(
                items: Group<'T> -> ReactElement,
                value: SelectableValue<'T>,
                validity: InputValidity,
                ?children: ReactChildrenProp,
                ?styles: array<ViewStyles>,
                ?key: string,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            key |> ignore
            children |> ignore
            xLegacyStyles |> ignore

            let getGroupState () =
                {
                    IsSelected = fun (v: 'T) -> value.IsSelected v
                    Toggle     = fun (v: 'T) (_: ReactEvent.Action) -> value.Toggle v
                    Value      = value
                }

            let groupHook: IStateHook<Group<'T>> = () |> getGroupState |> Hooks.useState

            Hooks.useEffect(
                (fun () -> () |> getGroupState |> groupHook.update),
                [| value |]
            )

            element {
                Rn.View(
                    styles = [| yield! (styles |> Option.defaultValue [||]) |],
                    children =
                        elements {
                            items groupHook.current
                        }
                )

                match validity.InvalidReason with
                | Some reason ->
                    LC.Text(reason, styles = [| ChoiceListStyles.invalidReason |])
                | None ->
                    noElement
            }
