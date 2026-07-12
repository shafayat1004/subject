[<AutoOpen>]
module LibClient.Components.ToggleButtons

open Fable.React

open LibClient
open LibClient.Accessibility

open Rn.Components
open Rn.Styles

module LC =
    module ToggleButtons =
        type SelectableValue<'T when 'T : comparison> = LibClient.Input.SelectableValue<'T>
        let AtMostOne  = SelectableValue.AtMostOne
        let ExactlyOne = SelectableValue.ExactlyOne
        let AtLeastOne = SelectableValue.AtLeastOne
        let Any        = SelectableValue.Any

        type Group<'T> = {
            IsSelected: 'T -> bool
            Toggle:     'T -> ReactEvent.Action -> unit
        }

open LC.ToggleButtons

// TODO: delete after RenderDSL migration
let AtMostOne  = LC.ToggleButtons.AtMostOne
let ExactlyOne = LC.ToggleButtons.ExactlyOne
let AtLeastOne = LC.ToggleButtons.AtLeastOne
let Any        = LC.ToggleButtons.Any
type Group<'T> = LC.ToggleButtons.Group<'T>

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            FlexDirection.Row
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member ToggleButtons<'T when 'T: comparison>(
            buttons: Group<'T> -> ReactElement,
            value:   SelectableValue<'T>,
            ?label:  string,
            ?key:    string,
            ?styles: array<ViewStyles>
        ) : ReactElement =
        key |> ignore

        let getGroupState () =
            {
                IsSelected = fun (v: 'T) -> value.IsSelected v
                Toggle     = fun (v: 'T) (_: ReactEvent.Action) -> value.Toggle v
            }

        let groupHook: IStateHook<Group<'T>> = () |> getGroupState |> Hooks.useState

        Hooks.useEffect(
            (fun () -> () |> getGroupState |> groupHook.update),
            [| value |]
        )

        Rn.View(
            ?accessibilityLabel = label,
            accessibilityRole   = AccessibilityRole.RadioGroup,
            styles              = [|
                Styles.view
                yield! (styles |> Option.defaultValue [||])
            |],
            children =
                elements {
                    buttons groupHook.current
                }
        )
