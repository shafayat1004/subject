[<AutoOpen>]
module LibClient.Components.Form.Base

open Fable.React

open LibClient
open LibClient.Components

type Accumulator<'Acc> =
| ManageInternallyInitializingWith of InitialAcc: 'Acc
| ManagedExternally                of Acc: 'Acc * OnChangeAcc: ('Acc -> 'Acc) * ShouldShowValidationErrors: bool

// for ease of importing by other components
module Types =
    type ValidationErrors<'Field when 'Field: comparison> = {
        MissingRequiredFields: Set<'Field>
        InvalidValue:          Map<'Field, string>
    } with
        override this.ToString () : string =
            let missingFields =
                this.MissingRequiredFields
                |> Set.map (fun field -> field.ToString())
                |> String.concat ", "

            let invalidFields =
                this.InvalidValue
                |> Map.map (fun field message -> $"{field.ToString()}: {message}")
                |> Map.values
                |> String.concat ", "

            [
                if not (System.String.IsNullOrEmpty missingFields) then $"Missing {missingFields}"
                if not (System.String.IsNullOrEmpty invalidFields) then $"Invalid {invalidFields}"
            ]
            |> String.concat "\n"

        static member Empty : ValidationErrors<'Field> = {
            MissingRequiredFields = Set.empty
            InvalidValue          = Map.empty
        }

        static member Merge (e1: ValidationErrors<'Field>) (e2: ValidationErrors<'Field>) : ValidationErrors<'Field> =
            {
                MissingRequiredFields = Set.union e1.MissingRequiredFields e2.MissingRequiredFields
                InvalidValue          = Map.merge e1.InvalidValue          e2.InvalidValue
            }

        static member MergeList (list: List<ValidationErrors<'Field>>) : ValidationErrors<'Field> =
            list
            |> Seq.fold
                (fun acc curr ->
                    ValidationErrors.Merge acc curr
                )
                ValidationErrors.Empty

        member this.IsEmpty : bool =
            this.MissingRequiredFields.IsEmpty && this.InvalidValue.IsEmpty

        member this.AddMissingRequiredField (field: 'Field) : ValidationErrors<'Field> =
            { this with MissingRequiredFields = this.MissingRequiredFields.Add field }

        member this.AddInvalid (field: 'Field) (message: string) : ValidationErrors<'Field> =
            { this with InvalidValue = this.InvalidValue.Add (field, message) }

    let missingRequiredField (field: 'Field) : ValidationErrors<'Field> =
        ValidationErrors.Empty.AddMissingRequiredField field

    let invalidField (field: 'Field) (message: string) : ValidationErrors<'Field> =
        ValidationErrors.Empty.AddInvalid field message

    let invalidFields (invalidFields: Map<'Field, string>) : ValidationErrors<'Field> =
        {
            MissingRequiredFields = Set.empty
            InvalidValue          = invalidFields
        }

    type AbstractAcc<'Field, 'Acced when 'Field: comparison> =
        abstract member Validate: unit -> Result<'Acced, ValidationErrors<'Field>>;

    type NoFields = unit
    type UnitAcc() =
        interface AbstractAcc<NoFields, unit> with
            override this.Validate () : Result<unit, ValidationErrors<NoFields>> =
                Ok ()
    let unitAcc = UnitAcc()

    let submitActionKey = "__submit"

    type FormHandle<'Field, 'Acc, 'Acced when 'Field: comparison and 'Acc :> AbstractAcc<'Field, 'Acced>> = {
        Acc:                        'Acc
        UpdateAcc:                  ('Acc -> 'Acc) -> unit
        ValidationResult:           Result<'Acced, ValidationErrors<'Field>>
        ShouldShowValidationErrors: bool
        Executor:                   MakeExecutor
        SubmitExecutor:             Executor
        TrySubmit:                  (ReactEvent.Action -> UDAction) * Executor
        // use sparingly; mostly when binding form submission to OnKeyEnterPress
        TrySubmitLowLevel:          ReactEvent.Action -> unit
        ShowValidationErrors:       bool -> unit
    } with
        member this.IsSubmitInProgress : bool =
            match this.Executor submitActionKey with
            | Executor.InProgress -> true
            | _                   -> false

        member this.FieldValidity (field: 'Field) : InputValidity =
            if not this.ShouldShowValidationErrors then
                InputValidity.Valid
            else
                match this.ValidationResult with
                | Ok _ -> InputValidity.Valid
                | Error validationErrors ->
                    match validationErrors.InvalidValue.TryFind field with
                    | Some message -> InputValidity.Invalid message
                    | None ->
                        match validationErrors.MissingRequiredFields.Contains field with
                        | true  -> InputValidity.Missing
                        | false -> InputValidity.Valid

        member this.IfNotInProgress (eventToAction: ReactEvent.Action -> UDAction, key: UDActionKey) : (ReactEvent.Action -> UDAction) * Executor =
            match this.IsSubmitInProgress with
            | true  -> ((fun _e -> NoopUDAction), this.Executor key)
            | false -> (eventToAction, this.Executor key)

        member this.IfNotInProgress (action: UDAction, key: UDActionKey) : UDAction * Executor =
            match this.IsSubmitInProgress with
            | true  -> (NoopUDAction, this.Executor key)
            | false -> (action,       this.Executor key)

        member this.IfNotInProgress (f: unit -> unit, key: UDActionKey) : UDAction * Executor =
            match this.IsSubmitInProgress with
            | true  -> (NoopUDAction,               this.Executor key)
            | false -> (UDAction.ofSyncErrorless f, this.Executor key)

    type Forms =
        static member GetFieldValue<'F, 'T when 'F : comparison> (field: 'F, maybeValue: Option<'T>) : Result<'T, ValidationErrors<'F>> =
            match maybeValue with
            | Some value -> Ok value
            | None       -> Error (missingRequiredField field)

        static member GetFieldValue<'F, 'T when 'F : comparison> (field: 'F, valueResult: Result<Option<'T>, string>) : Result<'T, ValidationErrors<'F>> =
            match valueResult with
            | Ok (Some value) -> Ok value
            | Ok None         -> Error (missingRequiredField field)
            | Error message   -> Error (invalidField field message)

        static member GetFieldValue2<'F, 'T when 'F : comparison> (maybeValue: Option<'T>) : 'F -> Result<'T, ValidationErrors<'F>> =
            fun (field: 'F) ->
                match maybeValue with
                | Some value -> Ok value
                | None       -> Error (missingRequiredField field)

        static member GetFieldValue2<'F, 'T when 'F : comparison> (valueResult: Result<Option<'T>, string>) : 'F -> Result<'T, ValidationErrors<'F>> =
            fun (field: 'F) ->
                match valueResult with
                | Ok (Some value) -> Ok value
                | Ok None         -> Error (missingRequiredField field)
                | Error message   -> Error (invalidField field message)

        static member GetOptionalFieldValue<'F, 'T when 'F : comparison> (field: 'F, valueResult: Result<Option<'T>, string>) : Result<Option<'T>, ValidationErrors<'F>> =
            match valueResult with
            | Ok (Some value) -> Ok (Some value)
            | Ok None         -> Ok None
            | Error message   -> Error (invalidField field message)

        static member GetOptionalFieldValue2<'F, 'T when 'F : comparison> (valueResult: Result<Option<'T>, string>) (field: 'F) : Result<Option<'T>, ValidationErrors<'F>> =
            match valueResult with
            | Ok (Some value) -> Ok (Some value)
            | Ok None         -> Ok None
            | Error message   -> Error (invalidField field message)

    let (>>>) (r1: Result<'T1, ValidationErrors<'F>>) (r2: Result<'T2, ValidationErrors<'F>>) : Result<'T1 * 'T2, ValidationErrors<'F>> =
        match (r1, r2) with
        | (Ok v1,    Ok v2   ) -> Ok (v1, v2)
        | (Error e1, Error e2) -> Error (ValidationErrors.Merge e1 e2)
        | (Ok _,     Error e2) -> Error e2
        | (Error e1, Ok _    ) -> Error e1

    type ValidateForm() =
        member _.Bind(x: Result<'T1, ValidationErrors<'Field>>, f: 'T1 -> Result<'T2, ValidationErrors<'Field>>) : Result<'T2, ValidationErrors<'Field>> =
            match x with
            | Error error -> Error error
            | Ok value -> f value

        member _.Return(value: 'T): Result<'T, ValidationErrors<'Field>> =
            Ok value

        member _.ReturnFrom(wrappedValue: 'T) : 'T =
            wrappedValue

        member this.Zero () : Result<unit, ValidationErrors<'Field>> =
            this.Return ()

        member _.MergeSources (x1: Result<'T1, ValidationErrors<'Field>>, x2: Result<'T2, ValidationErrors<'Field>>) : Result<'T1 * 'T2, ValidationErrors<'Field>> =
            match (x1, x2) with
            | (Ok v1, Ok v2)       -> Ok (v1, v2)
            | (Error e1, Error e2) -> ValidationErrors.Merge e1 e2 |> Error
            | (Error e, Ok _)
            | (Ok _, Error e)      -> Error e

        member _.BindReturn (x: Result<'T, ValidationErrors<'Field>>, f: 'T -> 'U) : Result<'U, ValidationErrors<'Field>> =
            Result.map f x

    let validateForm = ValidateForm()


type AbstractAcc<'Field, 'Acced when 'Field: comparison> = Types.AbstractAcc<'Field, 'Acced>

// For ~ access
type FormHandle<'Field, 'Acc, 'Acced when 'Field: comparison and 'Acc :> AbstractAcc<'Field, 'Acced>> = Types.FormHandle<'Field, 'Acc, 'Acced>
type NoFields = Types.NoFields
type UnitAcc  = Types.UnitAcc
let unitAcc   = Types.unitAcc

type private Estate<'Acc> = {
    Acc:                        'Acc
    ShouldShowValidationErrors: bool
}

let private initialEstate<'Acc> (accumulator: Accumulator<'Acc>) : Estate<'Acc> =
    match accumulator with
    | ManageInternallyInitializingWith initialAcc              -> { Acc = initialAcc; ShouldShowValidationErrors = false }
    | ManagedExternally (acc, _, shouldShowValidationErrors)   -> { Acc = acc; ShouldShowValidationErrors = shouldShowValidationErrors }

let private makeFormHandle
        (executor: MakeExecutor)
        (estate: Estate<'Acc>)
        (updateAcc: ('Acc -> 'Acc) -> unit)
        (trySubmit: Executor -> ReactEvent.Action -> UDAction)
        (trySubmitLowLevel: Executor -> ReactEvent.Action -> unit)
        (showValidationErrors: bool -> unit)
        : FormHandle<'Field, 'Acc, 'Acced> =
    let submitExecutor = executor Types.submitActionKey
    {
        Acc                        = estate.Acc
        UpdateAcc                  = updateAcc
        ValidationResult           = estate.Acc.Validate()
        ShouldShowValidationErrors = estate.ShouldShowValidationErrors
        Executor                   = executor
        SubmitExecutor             = submitExecutor
        TrySubmit                  = (trySubmit submitExecutor, submitExecutor)
        TrySubmitLowLevel          = trySubmitLowLevel submitExecutor
        ShowValidationErrors       = showValidationErrors
    }

[<RequireQualifiedAccess>]
module private Actions =
    let updateAcc<'Acc> (estateHook: IStateHook<Estate<'Acc>>) (accumulator: Accumulator<'Acc>) (updater: 'Acc -> 'Acc) : unit =
        match accumulator with
        | ManagedExternally (acc, onChangeAcc, _) -> onChangeAcc (updater acc) |> ignore
        | ManageInternallyInitializingWith _      -> estateHook.update (fun estate -> { estate with Acc = updater estate.Acc })

    let trySubmit<'Field, 'Acc, 'Acced when 'Field: comparison and 'Acc :> AbstractAcc<'Field, 'Acced>>
            (estateHook: IStateHook<Estate<'Acc>>)
            (accumulator: Accumulator<'Acc>)
            (initializeAccOnSubmit: bool)
            (submit: 'Acced -> ReactEvent.Action -> UDAction)
            (submitExecutor: Executor)
            (e: ReactEvent.Action)
            : UDAction =
        match submitExecutor with
        | Executor.Actionable _ ->
            match estateHook.current.Acc.Validate () with
            | Ok acced ->
                if initializeAccOnSubmit then
                    let initialAcc =
                        match accumulator with
                        | ManageInternallyInitializingWith initialAcc -> initialAcc
                        | ManagedExternally (acc, _, _)               -> acc
                    estateHook.update (fun estate -> { estate with Acc = initialAcc })
                submit acced e

            | Error _ ->
                estateHook.update (fun estate -> { estate with ShouldShowValidationErrors = true })
                NoopUDAction

        | _ -> NoopUDAction

    let trySubmitLowLevel (trySubmit: Executor -> ReactEvent.Action -> UDAction) (submitExecutor: Executor) (e: ReactEvent.Action) : unit =
        submitExecutor.MaybeExecute (trySubmit submitExecutor e)

    let showValidationErrors<'Acc> (estateHook: IStateHook<Estate<'Acc>>) (value: bool) : unit =
        estateHook.update (fun estate -> { estate with ShouldShowValidationErrors = value })

type LibClient.Components.Constructors.LC.Form with
    [<Component>]
    static member Base<'Field, 'Acc, 'Acced when 'Field: comparison and 'Acc :> AbstractAcc<'Field, 'Acced>>(
            accumulator:           Accumulator<'Acc>,
            submit:                'Acced -> ReactEvent.Action -> UDAction,
            content:               FormHandle<'Field, 'Acc, 'Acced> -> ReactElement,
            ?children:             ReactChildrenProp,
            ?initializeAccOnSubmit: bool,
            ?executor:             MakeExecutor,
            ?key:                  string,
            ?xLegacyStyles:        List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        children |> ignore
        key |> ignore
        xLegacyStyles |> ignore

        let initializeAccOnSubmit = defaultArg initializeAccOnSubmit false

        let estateHook = Hooks.useState (initialEstate accumulator)

        Hooks.useEffect (
            (fun () ->
                match accumulator with
                | ManageInternallyInitializingWith _ -> ()
                | ManagedExternally (acc, _, shouldShowValidationErrors) ->
                    estateHook.update { Acc = acc; ShouldShowValidationErrors = shouldShowValidationErrors }
            ),
            [|
                match accumulator with
                | ManageInternallyInitializingWith initialAcc -> box initialAcc
                | ManagedExternally (acc, _, shouldShowValidationErrors) ->
                    box (acc, shouldShowValidationErrors)
            |]
        )

        let updateAcc (updater: 'Acc -> 'Acc) : unit =
            Actions.updateAcc estateHook accumulator updater

        let trySubmit (submitExecutor: Executor) (e: ReactEvent.Action) : UDAction =
            Actions.trySubmit estateHook accumulator initializeAccOnSubmit submit submitExecutor e

        let trySubmitLowLevel (submitExecutor: Executor) (e: ReactEvent.Action) : unit =
            Actions.trySubmitLowLevel trySubmit submitExecutor e

        let showValidationErrors (value: bool) : unit =
            Actions.showValidationErrors estateHook value

        let renderWithExecutor (makeExecutor: MakeExecutor) : ReactElement =
            content (makeFormHandle makeExecutor estateHook.current updateAcc trySubmit trySubmitLowLevel showValidationErrors)

        match executor with
        | Some makeExecutor -> renderWithExecutor makeExecutor
        | None              -> LC.Executor.AlertErrors(``with`` = renderWithExecutor)
