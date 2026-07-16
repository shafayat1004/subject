// Public types (preserves LibClient.Components.Input.PositiveInteger.* path for callers)
namespace LibClient.Components.Input

open LibClient
open LibClient.Input
open System.Text.RegularExpressions
open Rn.Styles

module PositiveInteger =

    let private onlyValidCharacters = Regex "^[0-9]*$"

    let private parsePropImpl (raw: Option<NonemptyString>) : Result<Option<Positive.PositiveInteger>, string> =
        match raw with
        | None -> Ok None
        | Some (NonemptyString nonemptyRaw) ->
            let hasOnlyValidCharacters = onlyValidCharacters.IsMatch nonemptyRaw
            let parseResult = System.Int32.ParseOption nonemptyRaw
            match (parseResult, hasOnlyValidCharacters) with
            | (None, _)
            | (_, false) -> Error "Only numbers allowed"
            | (Some value, true) ->
                match Positive.PositiveInteger.ofInt value with
                | None                 -> Error "Allowed only positive numbers"
                | Some positiveInteger -> positiveInteger |> Some |> Ok

    type PropSuffixFactory = InputSuffixFactory

    type Value = LibClient.Components.Input.ParsedText.Value<Positive.PositiveInteger>

    let parse (raw: Option<NonemptyString>) : Value = Value.OfRaw parsePropImpl raw

    let wrap (value: Positive.PositiveInteger) : Value = Value.Wrap (value, value.Value.ToString())

    let empty : Value = parse None

    // Expose parseProp for passing to ParsedText
    let parseProp = parsePropImpl


// Legacy styles module — kept for backward-compatible callers in DefaultComponentsTheme.fs and app .styles.fs files.
// Do NOT add new usages; prefer passing styles/theme params to the component directly.
namespace LibClient.Components.Input

module PositiveIntegerStyles =

    open Rn.LegacyStyles

    let private baseStyles = lazy (asBlocks [])

    type (* class to enable named parameters *) Theme() =
        static let customize = makeCustomize("LibClient.Components.Input.PositiveInteger", baseStyles)

        static member All (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : unit =
            customize [
                Theme.Rules(borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding)
            ]

        static member One (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : Styles =
            Theme.Rules(borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding) |> makeSheet

        // For controlling the min-height of multiline input
        static member MinHeight (theHeight: int) : Styles =
            makeSheet [
                "theme" ==> LibClient.Components.Input.ParsedTextStyles.Theme.MinHeight theHeight
            ]

        static member ZeroPadding (): Styles =
            makeSheet [
                "theme" ==> LibClient.Components.Input.ParsedTextStyles.Theme.ZeroPadding ()
            ]

        static member Rules (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : List<ISheetBuildingBlock> = [
            "theme" ==> LibClient.Components.Input.ParsedTextStyles.Theme.One (borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding)
        ]

    let styles = lazy (compile (List.concat [
        baseStyles.Value
        Theme.Rules (
            borderLabelBlurredColor    = Color.Grey "44",
            borderLabelFocusedColor    = Color.DevGreen,
            borderLabelInvalidColor    = Color.DevRed,
            textColor                  = Color.Grey "44",
            noneditableTextColor       = Color.Grey "22",
            noneditableBackgroundColor = Color.Grey "66",
            invalidReasonColor         = Color.DevRed,
            placeholderColor           = Color.Grey "aa",
            theVerticalPadding         = 10
        )
    ]))


// Component extension
namespace LibClient.Components

open Fable.React
open LibClient
open LibClient.Input
open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Input_PositiveIntegerComponent =

    open LibClient.Components.Input.PositiveInteger

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member PositiveInteger(
                value:                Value,
                validity:             InputValidity,
                onChange:             Value -> unit,
                ?label:               string,
                ?placeholder:         string,
                ?prefix:              string,
                ?suffix:              InputSuffix,
                ?editable:            bool,
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
                editable            = defaultArg editable true,
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
