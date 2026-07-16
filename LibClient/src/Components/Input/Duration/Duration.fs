// Public types (preserves LibClient.Components.Input.Duration.* path for callers)
namespace LibClient.Components.Input

open System
open LibClient

module Duration =

    type Field =
        | Days
        | Hours
        | Minutes

    let private parseField
        (field: Field)
        (value: Option<NonemptyString>)
        : Result<Option<UnsignedInteger>, Field * string> =
        match value with
        | None -> Ok None
        | Some(NonemptyString nonemptyRaw) ->
            match System.Int32.ParseOption nonemptyRaw with
            | None -> Error(field, "Allowed numeric characters only")
            | Some value ->
                match Unsigned.UnsignedInteger.ofInt value with
                | None                 -> Error(field, "Allowed only positive numbers")
                | Some unsignedDecimal -> unsignedDecimal |> Some |> Ok

    let private validateDays (days: UnsignedInteger) : Result<UnsignedInteger, Field * string> = Ok days

    let private validateHours (hours: UnsignedInteger) : Result<UnsignedInteger, Field * string> =
        if hours.Value < 24 then
            Ok hours
        else
            Error(Hours, "Hours should be between 0 and 23")

    let private validateMinutes (minutes: UnsignedInteger) : Result<UnsignedInteger, Field * string> =
        if minutes.Value < 60 then
            Ok minutes
        else
            Error(Minutes, "Minutes should be between 0 and 59")

    let private computeResult
        (rawDays: Option<NonemptyString>, rawHours: Option<NonemptyString>, rawMinutes: Option<NonemptyString>)
        : Result<Option<TimeSpan>, Field * string> =
        resultful {
            let! maybeDays = parseField Days rawDays
            let! maybeHours = parseField Hours rawHours
            let! maybeMinutes = parseField Minutes rawMinutes

            match (maybeDays, maybeHours, maybeMinutes) with
            | (Some days, Some hours, Some minutes) ->
                let! validDays = validateDays days
                let! validHours = validateHours hours
                let! validMinutes = validateMinutes minutes

                return
                    (TimeSpan.FromDays(float validDays.Value))
                    + (TimeSpan.FromHours(float validHours.Value))
                    + (TimeSpan.FromMinutes(float validMinutes.Value))
                    |> Some

            | (None, Some hours, Some minutes) ->
                let! validHours = validateHours hours
                let! validMinutes = validateMinutes minutes

                return
                    (TimeSpan.FromHours(float validHours.Value))
                    + (TimeSpan.FromMinutes(float validMinutes.Value))
                    |> Some

            | (None, Some hours, None) ->
                let! _validHours = validateHours hours
                return None

            | (None, None, Some minutes) ->
                let! _validMinutes = validateMinutes minutes
                return None

            | _ -> return None
        }

    type Value =
        { Raw:                      Option<NonemptyString> * Option<NonemptyString> * Option<NonemptyString>
          InternalValidationResult: Option<Field * string>
          Result:                   Option<TimeSpan> }

        member this.InternalFieldValidity(field: Field) : InputValidity =
            match this.InternalValidationResult with
            | Some(errField, _message) when errField = field -> Missing
            | _                                              -> Valid

        member this.InternalValidity: InputValidity =
            match this.InternalValidationResult with
            | Some(_, message) -> Invalid message
            | _                -> Valid

        static member FromRaw(raw: Option<NonemptyString> * Option<NonemptyString> * Option<NonemptyString>) : Value =
            let result = computeResult raw

            { Raw                      = raw
              InternalValidationResult = result |> Result.invert |> Result.toOption
              Result                   = result |> Result.toOption |> Option.getOrElse None }

        member this.SetDays(value: Option<NonemptyString>) : Value =
            let (_, hours, minutes) = this.Raw
            Value.FromRaw(value, hours, minutes)

        member this.SetHours(value: Option<NonemptyString>) : Value =
            let (days, _, minutes) = this.Raw
            Value.FromRaw(days, value, minutes)

        member this.SetMinutes(value: Option<NonemptyString>) : Value =
            let (days, hours, _) = this.Raw
            Value.FromRaw(days, hours, value)

        override this.ToString() : string =
            let (maybeDays, hours, minutes) = this.Raw
            let hours = hours |> Option.mapOrElse "00" (fun h -> h.Value)
            let minutes = minutes |> Option.mapOrElse "00" (fun m -> m.Value)

            match maybeDays with
            | Some days -> sprintf "%s:%s:%s" days.Value hours minutes
            | None      -> sprintf "%s:%s" hours minutes

    let wrap (value: TimeSpan) : Value =
        Value.FromRaw(
            value.Days.ToString()        |> NonemptyString.ofString,
            value.Hours.ToString()       |> NonemptyString.ofString,
            sprintf "%02i" value.Minutes |> NonemptyString.ofString
        )

    let empty: Value = {
        Raw                      = (None, None, None)
        InternalValidationResult = None
        Result                   = None
    }


// Component extension
namespace LibClient.Components

open Fable.React
open LibClient
open LibClient.Accessibility
open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Input_DurationComponent =

    open LibClient.Components.Input.Duration
    open LibClient.Components.Input.Text

    [<RequireQualifiedAccess>]
    module private Styles =
        let fields =
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                gap 8
                paddingVertical 4
            }

        let field = makeViewStyles { width 44 }

        // 0.02, not 0: Fabric iOS treats alpha < ~0.01 as non-interactive, so an opacity-0
        // (or 0.01) overlay Pressable never receives taps (react-native #50465).
        let pressableOverlay = makeViewStyles { opacity 0.02 }

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member Duration
            (
                value:                Value,
                validity:             InputValidity,
                onChange:             Value -> unit,
                ?label:               string,
                ?testId:              string,
                ?onEnterKeyPress:     (ReactEvent.Keyboard -> unit),
                ?requestFocusOnMount: bool,
                ?shouldDisplayDays:   bool,
                ?xLegacyStyles:       List<Rn.LegacyStyles.RuntimeStyles>,
                ?key:                 string
            ) : ReactElement =
            key |> ignore

            let requestFocusOnMount = defaultArg requestFocusOnMount false
            let shouldDisplayDays = defaultArg shouldDisplayDays false

            let isFocusedHook = Hooks.useState false
            let maybeDaysInput = Hooks.useRef<Option<ITextRef>> None
            let maybeHoursInput = Hooks.useRef<Option<ITextRef>> None

            let legacyViewStyles: array<ViewStyles> =
                match xLegacyStyles with
                | Some ls ->
                    match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                    | [] -> [||]
                    | styles ->
                        [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles>
                               "Rn.Components.View"
                               styles |]
                | None -> [||]

            let legacyLabelStyles: List<Rn.LegacyStyles.RuntimeStyles> =
                match xLegacyStyles with
                | Some ls -> ls
                | None    -> []

            let fieldXLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles> option =
                match Rn.LegacyStyles.Runtime.findApplicableStyles legacyLabelStyles "field" with
                | [] -> None
                | s  -> Some s

            let refDaysInput (nullableInstance: LibClient.JsInterop.JsNullable<ITextRef>) : unit =
                maybeDaysInput.current <- nullableInstance.ToOption

            let refHoursInput (nullableInstance: LibClient.JsInterop.JsNullable<ITextRef>) : unit =
                maybeHoursInput.current <- nullableInstance.ToOption

            let onFocus (_e: Browser.Types.FocusEvent) : unit = isFocusedHook.update true

            let onBlur (_e: Browser.Types.FocusEvent) : unit = isFocusedHook.update false

            let focusHoursInput () : unit =
                maybeHoursInput.current |> Option.sideEffect (fun input -> input.RequestFocus())

            let externalValidityForFields =
                match validity with
                | Valid -> Valid
                | _     -> Missing

            let (rawDays, rawHours, rawMinutes) = value.Raw

            let isLabelInvalid = (value.InternalValidity.Or validity).IsInvalid
            let isFocused = isFocusedHook.current

            let resolvedTestId =
                testId
                |> Option.orElse (label |> Option.map (A11ySlug.testId "input"))
                |> Option.defaultValue "input-duration"

            Rn.View(
                testId = resolvedTestId,
                styles = [| yield! legacyViewStyles |],
                children =
                    [| (match label with
                        | Some lbl ->
                            Rn.View(
                                children =
                                    [| LC.LegacyText(
                                           xLegacyStyles =
                                               Rn.LegacyStyles.Runtime.findApplicableStyles
                                                   legacyLabelStyles
                                                   ("label"
                                                    + (if isLabelInvalid then " invalid" else "")
                                                    + (if isFocused then " focused" else "")),
                                           children =
                                               [| makeTextNode2
                                                      (Some "LibClient.Components.LegacyText")
                                                      (System.String.Format("{0}", lbl)) |]
                                       )
                                       LC.Pressable(
                                           onPress       = (fun _ -> focusHoursInput ()),
                                           label         = lbl,
                                           testId        = sprintf "%s-focus" resolvedTestId,
                                           role          = AccessibilityRole.Button,
                                           overlay       = true,
                                           styles        = [| Styles.pressableOverlay |],
                                           componentName = "LC.Input.Duration.Focus"
                                       ) |]
                            )
                        | None -> noElement)

                       Rn.View(
                           styles = [| Styles.fields |],
                           children =
                               [| (if shouldDisplayDays then
                                       LC.Input.Text(
                                           value            = rawDays,
                                           onChange         = (value.SetDays >> onChange),
                                           validity         = ((value.InternalFieldValidity Days).Or externalValidityForFields),
                                           maxLength        = 3,
                                           placeholder      = "00",
                                           testId           = A11ySlug.testId resolvedTestId "days",
                                           onFocus          = onFocus,
                                           onBlur           = onBlur,
                                           ref              = refDaysInput,
                                           styles           = [| Styles.field |],
                                           ?onEnterKeyPress = onEnterKeyPress,
                                           ?xLegacyStyles   = fieldXLegacyStyles
                                       )
                                   else
                                       noElement)

                                  (if shouldDisplayDays then
                                       LC.LegacyText(
                                           children =
                                               [| makeTextNode2 (Some "LibClient.Components.LegacyText") "days" |]
                                       )
                                   else
                                       noElement)

                                  LC.Input.Text(
                                      value               = rawHours,
                                      onChange            = (value.SetHours >> onChange),
                                      validity            = ((value.InternalFieldValidity Hours).Or externalValidityForFields),
                                      maxLength           = 2,
                                      placeholder         = "00",
                                      testId              = A11ySlug.testId resolvedTestId "hours",
                                      requestFocusOnMount = requestFocusOnMount,
                                      onFocus             = onFocus,
                                      onBlur              = onBlur,
                                      ref                 = refHoursInput,
                                      styles              = [| Styles.field |],
                                      ?onEnterKeyPress    = onEnterKeyPress,
                                      ?xLegacyStyles      = fieldXLegacyStyles
                                  )

                                  LC.LegacyText(
                                      children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") "hrs" |]
                                  )

                                  LC.Input.Text(
                                      value            = rawMinutes,
                                      onChange         = (value.SetMinutes >> onChange),
                                      validity         = ((value.InternalFieldValidity Minutes).Or externalValidityForFields),
                                      maxLength        = 2,
                                      placeholder      = "00",
                                      testId           = A11ySlug.testId resolvedTestId "minutes",
                                      onFocus          = onFocus,
                                      onBlur           = onBlur,
                                      styles           = [| Styles.field |],
                                      ?onEnterKeyPress = onEnterKeyPress,
                                      ?xLegacyStyles   = fieldXLegacyStyles
                                  )

                                  LC.LegacyText(
                                      xLegacyStyles =
                                          Rn.LegacyStyles.Runtime.findApplicableStyles legacyLabelStyles "colon",
                                      children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") "mins" |]
                                  ) |]
                       )

                       (match (value.InternalValidity.Or validity).InvalidReason with
                        | Some reason ->
                            Rn.View(
                                children =
                                    [| LC.LegacyText(
                                           xLegacyStyles =
                                               Rn.LegacyStyles.Runtime.findApplicableStyles
                                                   legacyLabelStyles
                                                   "invalid-reason",
                                           children =
                                               [| makeTextNode2
                                                      (Some "LibClient.Components.LegacyText")
                                                      (System.String.Format("{0}", reason)) |]
                                       ) |]
                            )
                        | None -> noElement) |]
            )
