module LibClient.Components.FormBaseInterop

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Form.Base

let renderBase<'Field, 'Acc, 'Acced when 'Field: comparison and 'Acc :> AbstractAcc<'Field, 'Acced>>
    (accumulator: Accumulator<'Acc>)
    (submit: 'Acced -> ReactEvent.Action -> UDAction)
    (content: FormHandle<'Field, 'Acc, 'Acced> -> ReactElement)
    : ReactElement =
    LC.Form.Base(
        accumulator = accumulator,
        submit = submit,
        content = content
    )
