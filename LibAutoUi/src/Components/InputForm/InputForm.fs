[<AutoOpen>]
module LibAutoUi.Components.InputForm

open System
open Fable.React

open LibClient
open LibAutoUi
open LibAutoUi.Types
open LibAutoUi.TypeExtensions
open LibAutoUi.Components.InputFormElement

open Rn.Components
open Rn.Styles

// TODO get rid of this eventually, we're not using this Plugin infra
let (* want private but can't have because used by inline *) inputRendererPlugin: InputRendererPlugin =
    fun defaultInputRenderer path name forType ->
        defaultInputRenderer path name forType |> Some

type FormWrapper<'T> = {
    Type:                 Type
    Form:                 InputForm
    PathToType:           Map<Path, Type>
    GetResult:            Accumulator -> InputValidationResult<'T>
    ReprocessAccumulator: Settings -> Accumulator -> Async<Accumulator>
} with
    static member inline Make<'T> (name: string) : FormWrapper<'T> =
        let theType = typeof<'T>
        {
            Type                 = theType
            Form                 = LibAutoUi.FormConstruction.buildInputForm inputRendererPlugin name theType
            PathToType           = TypeExtensions.computePathToType theType
            GetResult            = LibAutoUi.ValueConstruction.getResult<'T>
            ReprocessAccumulator = LibAutoUi.ValueConstruction.reprocessAccumulator<'T>
        }

type private Estate<'T> = {
    Accumulator:   Accumulator
    ReadyToRender: bool
}

let private defaultPrimitiveInputComponents: PrimitiveInputComponents =
    Map.ofList [
        (
            InputType.StringInput,
            fun props _children ->
                UIAuto.InputFieldString(
                    onChange   = props.OnChange,
                    maybeValue = props.MaybeValue
                )
        )
        (
            InputType.DateTimeInput,
            fun props _children ->
                UIAuto.InputFieldDateTime(
                    onChange   = props.OnChange,
                    maybeValue = props.MaybeValue
                )
        )
    ]

type InputValue with
    member this.GetValue : obj =
        match this with
        | UnitValue       -> () :> obj
        | StringValue   v -> v  :> obj
        | NumericValue  v -> v  :> obj
        | BooleanValue  v -> v  :> obj
        | GuidValue     v -> v  :> obj
        | DateTimeValue v -> v  :> obj
        | FileValue     v -> v  :> obj

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            paddingHorizontal 8
        }

type UIAuto with
    [<Component>]
    static member InputForm<'T>(
            formWrapper:    FormWrapper<'T>,
            settings:       Settings,
            onChange:       InputValidationResult<'T> -> unit,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key           |> ignore
        xLegacyStyles |> ignore

        let estateHook =
            Hooks.useState {
                Accumulator   = Accumulator.Empty formWrapper.Type formWrapper.PathToType
                ReadyToRender = false
            }

        let reprocessAccumulator (accumulator: Accumulator) : unit =
            async {
                let! updatedAccumulator = formWrapper.ReprocessAccumulator settings accumulator
                estateHook.update (fun _ ->
                    {
                        Accumulator   = updatedAccumulator
                        ReadyToRender = true
                    }
                )
                updatedAccumulator |> formWrapper.GetResult |> onChange
            }
            |> startSafely

        Hooks.useEffect(
            (fun () -> reprocessAccumulator estateHook.current.Accumulator),
            [||]
        )

        let onChangePath (path: Path) (value: InputValue) : unit =
            let updatedAccumulator = estateHook.current.Accumulator.UpdateUserInput path value
            estateHook.update (fun estate -> { estate with Accumulator = updatedAccumulator })
            reprocessAccumulator updatedAccumulator

        let onChangeFromRange (path: Path) (value: obj) (theType: Type) : unit =
            let updatedAccumulator = estateHook.current.Accumulator.UpdateUserInputFromRange path value theType
            estateHook.update (fun estate -> { estate with Accumulator = updatedAccumulator })
            reprocessAccumulator updatedAccumulator

        Rn.View(
            styles = [| Styles.view |],
            children =
                [|
                    if estateHook.current.ReadyToRender then
                        UIAuto.InputFormElement(
                            form                     = formWrapper.Form,
                            accumulator              = estateHook.current.Accumulator,
                            onChange                 = onChangePath,
                            onChangeFromRange        = onChangeFromRange,
                            primitiveInputComponents = defaultPrimitiveInputComponents
                        )
                    else
                        noElement
                |]
        )
