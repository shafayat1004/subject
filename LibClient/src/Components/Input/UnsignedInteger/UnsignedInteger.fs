// Public types (preserves LibClient.Components.Input.UnsignedInteger.* path for callers)
namespace LibClient.Components.Input

open LibClient
open LibClient.Input
open System.Text.RegularExpressions
open Rn.Styles

module UnsignedInteger =

    let private onlyValidCharacters = Regex "^[0-9]*$"

    let private parsePropImpl (raw: Option<NonemptyString>) : Result<Option<Unsigned.UnsignedInteger>, string> =
        match raw with
        | None -> Ok None
        | Some (NonemptyString nonemptyRaw) ->
            let hasOnlyValidCharacters = onlyValidCharacters.IsMatch nonemptyRaw
            let parseResult = System.Int32.ParseOption nonemptyRaw
            match (parseResult, hasOnlyValidCharacters) with
            | (None, _)
            | (_, false) -> Error "Only numbers allowed"
            | (Some value, true) ->
                match Unsigned.UnsignedInteger.ofInt value with
                | None                 -> Error "Allowed only nonnegative numbers"
                | Some unsignedInteger -> unsignedInteger |> Some |> Ok

    type PropSuffixFactory = InputSuffixFactory

    type Value = LibClient.Components.Input.ParsedText.Value<Unsigned.UnsignedInteger>

    let parse (raw: Option<NonemptyString>) : Value = Value.OfRaw parsePropImpl raw

    let wrap (value: Unsigned.UnsignedInteger) : Value = Value.Wrap (value, value.Value.ToString())

    let empty : Value = parse None

    // Expose parseProp for passing to ParsedText
    let parseProp = parsePropImpl


// Component extension
namespace LibClient.Components

open Fable.React
open LibClient
open LibClient.Input
open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Input_UnsignedIntegerComponent =

    open LibClient.Components.Input.UnsignedInteger

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member UnsignedInteger(
                value:                Value,
                validity:             InputValidity,
                onChange:             Value -> unit,
                ?label:               string,
                ?placeholder:         string,
                ?prefix:              string,
                ?suffix:              InputSuffix,
                ?requestFocusOnMount: bool,
                ?tabIndex:            int,
                ?onKeyPress:          (Browser.Types.KeyboardEvent -> unit),
                ?onEnterKeyPress:     (ReactEvent.Keyboard -> unit),
                ?styles:              array<ViewStyles>,
                ?xLegacyStyles:       List<Rn.LegacyStyles.RuntimeStyles>,
                ?key:                 string
            ) : ReactElement =
            key |> ignore
            LC.Input.ParsedText(
                parse               = parseProp,
                value               = value,
                validity            = validity,
                requestFocusOnMount = defaultArg requestFocusOnMount false,
                onChange            = onChange,
                keyboardType        = KeyboardType.Numeric,
                ?label              = label,
                ?placeholder        = placeholder,
                ?prefix             = prefix,
                ?suffix             = suffix,
                ?tabIndex           = tabIndex,
                ?onKeyPress         = onKeyPress,
                ?onEnterKeyPress    = onEnterKeyPress,
                ?styles             = styles,
                ?xLegacyStyles      = xLegacyStyles
            )
