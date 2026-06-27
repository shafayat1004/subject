// Public types (preserves LibClient.Components.Input.Decimal.* path for callers)
namespace LibClient.Components.Input

open LibClient
open System.Text.RegularExpressions
open ReactXP.Styles

module Decimal =

    let private onlyValidCharacters = Regex "^[\-0-9.]*$"

    let private parsePropImpl (raw: Option<NonemptyString>): Result<Option<decimal>, string> =
        match raw with
        | None -> Ok None
        | Some (NonemptyString nonemptyRaw) ->
            let hasOnlyValidCharacters : bool = onlyValidCharacters.IsMatch nonemptyRaw
            let parseResult = System.Decimal.ParseOption nonemptyRaw
            match (parseResult, hasOnlyValidCharacters) with
            | (None, _)
            | (_, false) -> Error "Only valid decimal values allowed"
            | (Some value, true) ->
                value |> Some |> Ok

    type PropSuffixFactory = InputSuffixFactory

    type Value = LibClient.Components.Input.ParsedText.Value<decimal>

    let parse (raw: Option<NonemptyString>) : Value =
        Value.OfRaw parsePropImpl raw

    let wrap (value: decimal) : Value = Value.Wrap (value, value.ToString())

    let empty : Value = parse None

    let parseProp = parsePropImpl


// Component extension
namespace LibClient.Components

open Fable.React
open LibClient
open LibClient.Input
open ReactXP.Components
open ReactXP.Styles

[<AutoOpen>]
module Input_DecimalComponent =

    open LibClient.Components.Input.Decimal

    module LC =
        module Input =
            module DecimalTypes =
                type Theme = {
                    BorderLabelBlurredColor:    Color
                    BorderLabelFocusedColor:    Color
                    BorderLabelInvalidColor:    Color
                    TextColor:                  Color
                    NoneditableTextColor:       Color
                    NoneditableBackgroundColor: Color
                    InvalidReasonColor:         Color
                    PlaceholderColor:           Color
                    TheVerticalPadding:         int
                }

    open LC.Input.DecimalTypes

    module private Styles =
        let legacyParsedText (theme: Theme) =
            LibClient.Components.Input.ParsedTextStyles.Theme.One (theme.BorderLabelBlurredColor, theme.BorderLabelFocusedColor, theme.BorderLabelInvalidColor, theme.TextColor, theme.NoneditableTextColor, theme.NoneditableBackgroundColor, theme.InvalidReasonColor, theme.PlaceholderColor, theme.TheVerticalPadding)
            |> legacyTheme

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member Decimal(
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
                ?theme:               Theme -> Theme,
                ?xLegacyStyles:       List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?key:                 string
            ) : ReactElement =
            key |> ignore

            let requestFocusOnMount = defaultArg requestFocusOnMount false
            let theTheme = Themes.GetMaybeUpdatedWith theme
            let xLegacyStyles = defaultArg xLegacyStyles []

            LC.Input.ParsedText(
                xLegacyStyles       = (Styles.legacyParsedText theTheme |> List.append xLegacyStyles),
                parse               = parseProp,
                value               = value,
                validity            = validity,
                requestFocusOnMount = requestFocusOnMount,
                onChange            = onChange,
                keyboardType        = KeyboardType.Numeric,
                ?label              = label,
                ?placeholder        = placeholder,
                ?prefix             = prefix,
                ?suffix             = suffix,
                ?tabIndex           = tabIndex,
                ?onKeyPress         = onKeyPress,
                ?onEnterKeyPress    = onEnterKeyPress,
                ?styles             = styles
            )
