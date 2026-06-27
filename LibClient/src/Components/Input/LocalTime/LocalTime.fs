// Public types (preserves LibClient.Components.Input.LocalTime.* path for callers)
namespace LibClient.Components.Input

open System
open LibClient

module LocalTime =

    type Field =
    | Hours
    | Minutes

    let private parseField (field: Field) (value: Option<NonemptyString>) : Result<Option<UnsignedInteger>, Field * string> =
        match value with
        | None -> Ok None
        | Some (NonemptyString nonemptyRaw) ->
            match System.Int32.ParseOption nonemptyRaw with
            | None -> Error (field, "Allowed numeric characters only")
            | Some value ->
                match Unsigned.UnsignedInteger.ofInt value with
                | None                 -> Error (field, "Allowed only positive numbers")
                | Some unsignedDecimal -> unsignedDecimal |> Some |> Ok

    let private validateHours (hours: UnsignedInteger) : Result<UnsignedInteger, Field * string> =
        if 0 < hours.Value && hours.Value < 13 then
            Ok hours
        else
            Error (Hours, "Hours should be between 1 and 12")

    let private validateMinutes (minutes: UnsignedInteger) : Result<UnsignedInteger, Field * string> =
        if minutes.Value < 60 then
            Ok minutes
        else
            Error (Minutes, "Minutes should be between 0 and 59")

    let private computeResult (rawHours: Option<NonemptyString>, rawMinutes: Option<NonemptyString>, periodOffset: int) : Result<Option<TimeSpan>, Field * string> = resultful {
        let! maybeHours   = parseField Hours   rawHours
        let! maybeMinutes = parseField Minutes rawMinutes

        match (maybeHours, maybeMinutes) with
        | (Some hours, Some minutes) ->
            let! validHours   = validateHours   hours
            let! validMinutes = validateMinutes minutes

            return
                (TimeSpan.FromHours (float ((validHours.Value % 12) + periodOffset))) + (TimeSpan.FromMinutes (float validMinutes.Value))
                |> Some

        | (Some hours, None) ->
            let! _validHours = validateHours hours
            return None

        | (None, Some minutes) ->
            let! _validMinutes = validateMinutes minutes
            return None

        | _ -> return None
    }

    let periodPickerItems: List<LibClient.Components.Legacy.Input.Picker.PickerItem<int>> = [
        { Label = "AM"; Item = 0  }
        { Label = "PM"; Item = 12 }
    ]

    type Value = {
        Raw:                      Option<NonemptyString> * Option<NonemptyString> * int
        InternalValidationResult: Option<Field * string>
        Result:                   Option<TimeSpan>
    } with
        member this.InternalFieldValidity (field: Field) : InputValidity =
            match this.InternalValidationResult with
            | Some (errField, _message) when errField = field -> Missing
            | _                                               -> Valid

        member this.InternalValidity : InputValidity =
            match this.InternalValidationResult with
            | Some (_, message) -> Invalid message
            | _                 -> Valid

        static member FromRaw (raw: Option<NonemptyString> * Option<NonemptyString> * int) : Value =
            let result = computeResult raw
            {
                Raw                      = raw
                InternalValidationResult = result |> Result.invert |> Result.toOption
                Result                   = result |> Result.toOption |> Option.getOrElse None
            }

        member this.SetPeriod (value: int) : Value =
            let (hours, minutes, _) = this.Raw
            Value.FromRaw (hours, minutes, value)

        member this.SetHours (value: Option<NonemptyString>) : Value =
            let (_, minutes, periodOffset) = this.Raw
            Value.FromRaw (value, minutes, periodOffset)

        member this.SetMinutes (value: Option<NonemptyString>) : Value =
            let (hours, _, periodOffset) = this.Raw
            Value.FromRaw (hours, value, periodOffset)

        override this.ToString () : string =
            let (hours, minutes, periodOffset) = this.Raw
            let hours        = hours   |> Option.mapOrElse "00" (fun h -> h.Value)
            let minutes      = minutes |> Option.mapOrElse "00" (fun m -> m.Value)
            let periodOffset = if periodOffset = 0 then "AM" else "PM"
            sprintf "%s:%s %s" hours minutes periodOffset

    let wrap (value: TimeSpan) : Value =
        let (hours, periodOffset) =
            match value.Hours with
            | hours when hours < 12 -> (hours,      0)
            | hours                 -> (hours % 12, 12)

        let twelveAdjustedHours =
            match hours with
            | 0 -> 12
            | _ -> hours

        Value.FromRaw (
            twelveAdjustedHours.ToString() |> NonemptyString.ofString,
            sprintf "%02i" value.Minutes   |> NonemptyString.ofString,
            periodOffset
        )

    let empty : Value = {
        Raw                      = (None, None, 0)
        InternalValidationResult = None
        Result                   = None
    }

    type Theme = {
        LabelBlurredColor:  Color
        LabelFocusedColor:  Color
        LabelInvalidColor:  Color
        InvalidReasonColor: Color
    }


// Component extension
namespace LibClient.Components

open Fable.React
open LibClient
open ReactXP.Components
open ReactXP.Styles

[<AutoOpen>]
module Input_LocalTimeComponent =

    open LibClient.Components.Input.LocalTime
    open LibClient.Components.Input.Text

    [<RequireQualifiedAccess>]
    module private Styles =
        let fields =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
            }

        // field, colon, picker, label, and invalid-reason styling are handled via
        // xLegacyStyles passed from the caller's render DSL class attributes.

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member LocalTime(
                value:               Value,
                validity:            InputValidity,
                onChange:            Value -> unit,
                ?label:              string,
                ?onEnterKeyPress:    (ReactEvent.Keyboard -> unit),
                ?requestFocusOnMount: bool,
                ?xLegacyStyles:      List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?key:                string
            ) : ReactElement =
            key |> ignore

            let requestFocusOnMount = defaultArg requestFocusOnMount false

            let isFocusedHook   = Hooks.useState false
            let maybeHoursInput = Hooks.useRef<Option<ITextRef>> None

            let legacyViewStyles : array<ViewStyles> =
                match xLegacyStyles with
                | Some ls ->
                    match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                    | []     -> [||]
                    | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
                | None -> [||]

            let legacyLabelStyles : List<ReactXP.LegacyStyles.RuntimeStyles> =
                match xLegacyStyles with
                | Some ls -> ls
                | None    -> []

            let refHoursInput (nullableInstance: LibClient.JsInterop.JsNullable<ITextRef>) : unit =
                maybeHoursInput.current <- nullableInstance.ToOption

            let onFocus (_e: Browser.Types.FocusEvent) : unit =
                isFocusedHook.update true

            let onBlur (_e: Browser.Types.FocusEvent) : unit =
                isFocusedHook.update false

            let focusHoursInput (_e: Browser.Types.Event) : unit =
                maybeHoursInput.current |> Option.sideEffect (fun input ->
                    input.RequestFocus()
                )

            let externalValidityForFields =
                match validity with
                | Valid -> Valid
                | _     -> Missing

            let (rawHours, rawMinutes, rawPeriodOffset) = value.Raw

            let isLabelInvalid = (value.InternalValidity.Or validity).IsInvalid
            let isFocused      = isFocusedHook.current

            RX.View(
                styles = [| yield! legacyViewStyles |],
                children =
                    [|
                        (match label with
                         | Some lbl ->
                            RX.View(
                                onPress  = focusHoursInput,
                                children =
                                    [|
                                        LC.LegacyText(
                                            xLegacyStyles =
                                                ReactXP.LegacyStyles.Runtime.findApplicableStyles
                                                    legacyLabelStyles
                                                    ("label"
                                                     + (if isLabelInvalid then " invalid" else "")
                                                     + (if isFocused      then " focused" else "")),
                                            children =
                                                [| makeTextNode2 (Some "LibClient.Components.LegacyText") (System.String.Format("{0}", lbl)) |]
                                        )
                                    |]
                            )
                         | None -> noElement)

                        RX.View(
                            styles   = [| Styles.fields |],
                            children =
                                [|
                                    LC.Input.Text(
                                        value               = rawHours,
                                        onChange            = (value.SetHours >> onChange),
                                        validity            = ((value.InternalFieldValidity Hours).Or externalValidityForFields),
                                        maxLength           = 2,
                                        placeholder         = "00",
                                        requestFocusOnMount = requestFocusOnMount,
                                        onFocus             = onFocus,
                                        onBlur              = onBlur,
                                        ref                 = refHoursInput,
                                        ?onEnterKeyPress    = onEnterKeyPress,
                                        ?xLegacyStyles      =
                                            (let s = ReactXP.LegacyStyles.Runtime.findApplicableStyles legacyLabelStyles "field"
                                             if s.IsEmpty then None else Some s)
                                    )

                                    LC.LegacyText(
                                        xLegacyStyles =
                                            ReactXP.LegacyStyles.Runtime.findApplicableStyles
                                                legacyLabelStyles "colon",
                                        children =
                                            [| makeTextNode2 (Some "LibClient.Components.LegacyText") ":" |]
                                    )

                                    LC.Input.Text(
                                        value            = rawMinutes,
                                        onChange         = (value.SetMinutes >> onChange),
                                        validity         = ((value.InternalFieldValidity Minutes).Or externalValidityForFields),
                                        maxLength        = 2,
                                        placeholder      = "00",
                                        onFocus          = onFocus,
                                        onBlur           = onBlur,
                                        ?onEnterKeyPress = onEnterKeyPress,
                                        ?xLegacyStyles   =
                                            (let s = ReactXP.LegacyStyles.Runtime.findApplicableStyles legacyLabelStyles "field"
                                             if s.IsEmpty then None else Some s)
                                    )

                                    LC.Legacy.Input.Picker(
                                        items    = periodPickerItems,
                                        value    = (LibClient.Components.Legacy.Input.Picker.ByItem rawPeriodOffset |> Some),
                                        onChange = (LibClient.Components.Legacy.Input.Picker.CannotUnselect (snd >> value.SetPeriod >> onChange)),
                                        validity = externalValidityForFields,
                                        ?xLegacyStyles =
                                            (let s = ReactXP.LegacyStyles.Runtime.findApplicableStyles legacyLabelStyles "picker"
                                             if s.IsEmpty then None else Some s)
                                    )
                                |]
                        )

                        (match (value.InternalValidity.Or validity).InvalidReason with
                         | Some reason ->
                            RX.View(
                                children =
                                    [|
                                        LC.LegacyText(
                                            xLegacyStyles =
                                                ReactXP.LegacyStyles.Runtime.findApplicableStyles
                                                    legacyLabelStyles "invalid-reason",
                                            children =
                                                [| makeTextNode2 (Some "LibClient.Components.LegacyText") (System.String.Format("{0}", reason)) |]
                                        )
                                    |]
                            )
                         | None -> noElement)
                    |]
            )
