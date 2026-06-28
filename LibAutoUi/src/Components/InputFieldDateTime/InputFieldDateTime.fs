[<AutoOpen>]
module LibAutoUi.Components.InputFieldDateTime

open Fable.React

open LibClient
open LibClient.Components
open LibAutoUi.Types

type UIAuto with
    [<Component>]
    static member InputFieldDateTime(
            onChange:         InputValue -> unit,
            maybeValue:       Option<InputValue>,
            ?xLegacyStyles:   List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:             string
        ) : ReactElement =
        key |> ignore
        xLegacyStyles |> ignore

        let maybeSelectedDate =
            match maybeValue with
            | Some (DateTimeValue dto) -> Some (dto.ToDateOnly())
            | _                        -> None

        LC.DateSelector(
            maybeSelected = maybeSelectedDate,
            onChange =
                (fun date ->
                    onChange (
                        DateTimeValue (
                            date.ToDateTimeOffset(offset = System.DateTimeOffset.Now.Offset)
                        )
                    )
                )
        )
