namespace LibClient.Components.Legacy.Input

open LibClient
open LibClient.JsInterop

module PositiveInteger =

    type InputPositiveIntegerRef =
        abstract member SelectAll:    unit -> unit
        abstract member RequestFocus: unit -> unit


namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Components.Legacy.Input.PositiveInteger
open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Legacy_Input_PositiveIntegerComponent =

    let private legacyTopLevelStyles (xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles> option) : array<ViewStyles> =
        match xLegacyStyles with
        | Some ls ->
            match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
            | []     -> [||]
            | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
        | None -> [||]

    type LC.Legacy.Input with
        [<Component>]
        static member PositiveInteger(
                onChange: Result<Positive.PositiveInteger, InputValidationError> -> unit,
                ?children: ReactChildrenProp,
                ?initialValue: Positive.PositiveInteger,
                ?onKeyPress: Browser.Types.KeyboardEvent -> unit,
                ?ref: LibClient.JsInterop.JsNullable<InputPositiveIntegerRef> -> unit,
                ?key: string,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            children |> ignore
            key |> ignore

            let initialText =
                initialValue
                |> Option.map (fun v -> v.Value.ToString())
                |> Option.getOrElse ""

            let currInputHook = Hooks.useState initialText
            let maybeTextInput = Hooks.useRef<Option<Rn.Components.TextInput.ITextInputRef>> None

            let refImpl =
                Hooks.useMemo(
                    (fun () ->
                        { new InputPositiveIntegerRef with
                            member _.SelectAll () : unit =
                                maybeTextInput.current |> Option.sideEffect (fun textInput -> textInput.selectAll())

                            member _.RequestFocus () : unit =
                                maybeTextInput.current |> Option.sideEffect (fun textInput -> textInput.requestFocus())
                        }),
                    [| |]
                )

            Hooks.useEffect(
                (fun () ->
                    ref |> Option.sideEffect (fun setRef ->
                        setRef (refImpl :> obj :?> LibClient.JsInterop.JsNullable<InputPositiveIntegerRef>)
                    )
                ),
                [| ref :> obj; refImpl :> obj |]
            )

            Hooks.useEffect(
                (fun () ->
                    let initialResult =
                        match initialValue with
                        | None              -> Error InputValidationError.Missing
                        | Some initialValue -> Ok initialValue

                    onChange initialResult
                ),
                [| |]
            )

            let refTextInput (nullableInstance: LibClient.JsInterop.JsNullable<Rn.Components.TextInput.ITextInputRef>) : unit =
                maybeTextInput.current <- nullableInstance.ToOption

            let onChangeText (stringValue: string) : unit =
                currInputHook.update stringValue

                let result =
                    match stringValue with
                    | ""                  -> Error InputValidationError.Missing
                    | nonemptyStringValue ->
                        nonemptyStringValue
                        |> System.Int32.ParseOption
                        |> Option.flatMap Positive.PositiveInteger.ofInt
                        |> Option.map Ok
                        |> Option.getOrElse (Error InputValidationError.InvalidValue)

                onChange result

            Rn.View(
                styles = legacyTopLevelStyles xLegacyStyles,
                children =
                    [|
                        Rn.TextInput(
                            value        = currInputHook.current,
                            onChangeText = onChangeText,
                            ?onKeyPress  = onKeyPress,
                            ref          = refTextInput
                        )
                    |]
            )
