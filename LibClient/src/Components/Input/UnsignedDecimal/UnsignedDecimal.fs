// Public types (preserves LibClient.Components.Input.UnsignedDecimal.* path for callers)
namespace LibClient.Components.Input

open LibClient
open LibClient.Input
open System.Text.RegularExpressions
open Rn.Styles

module UnsignedDecimal =

    let private onlyValidCharacters = Regex "^[0-9.]*$"

    let private parsePropImpl (raw: Option<NonemptyString>) : Result<Option<Unsigned.UnsignedDecimal>, string> =
        match raw with
        | None -> Ok None
        | Some (NonemptyString nonemptyRaw) ->
            let hasOnlyValidCharacters : bool = onlyValidCharacters.IsMatch nonemptyRaw
            let parseResult = System.Decimal.ParseOption nonemptyRaw
            match (parseResult, hasOnlyValidCharacters) with
            | (None, _)
            | (_, false) -> Error "Only numbers and period allowed"
            | (Some value, true) ->
                match Unsigned.UnsignedDecimal.ofDecimal value with
                | None                 -> Error "Allowed only nonnegative numbers"
                | Some unsignedDecimal -> unsignedDecimal |> Some |> Ok

    type PropSuffixFactory = InputSuffixFactory

    type Value = LibClient.Components.Input.ParsedText.Value<Unsigned.UnsignedDecimal>

    let parse (raw: Option<NonemptyString>) : Value = Value.OfRaw parsePropImpl raw

    let wrap (value: Unsigned.UnsignedDecimal) : Value = Value.Wrap (value, value.Value.ToString())

    let empty : Value = parse None

    // Expose parseProp for passing to ParsedText
    let parseProp = parsePropImpl


// Backward-compatible styles module — referenced by external callers (e.g. SuiteCookups).
// This section opens Rn.LegacyStyles in its own isolated scope.
namespace LibClient.Components.Input

module UnsignedDecimalStyles =

    open Rn.LegacyStyles

    let private baseStyles = lazy (asBlocks [])

    type (* class to enable named parameters *) Theme() =
        static let customize = makeCustomize("LibClient.Components.Input.UnsignedDecimal", baseStyles)

        static member All (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : unit =
            customize [
                Theme.Rules(borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding)
            ]

        static member One (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : Styles =
            Theme.Rules(borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding) |> makeSheet

        static member MinHeight (theHeight: int) : Styles =
            makeSheet [
                "theme" ==> LibClient.Components.Input.ParsedTextStyles.Theme.MinHeight theHeight
            ]

        static member ZeroPadding () : Styles =
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
module Input_UnsignedDecimalComponent =

    open LibClient.Components.Input.UnsignedDecimal

    module LC =
        module Input =
            module UnsignedDecimalTypes =
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

    open LC.Input.UnsignedDecimalTypes

    module private Styles =
        let legacyParsedText (theme: Theme) =
            LibClient.Components.Input.ParsedTextStyles.Theme.One (theme.BorderLabelBlurredColor, theme.BorderLabelFocusedColor, theme.BorderLabelInvalidColor, theme.TextColor, theme.NoneditableTextColor, theme.NoneditableBackgroundColor, theme.InvalidReasonColor, theme.PlaceholderColor, theme.TheVerticalPadding)
            |> legacyTheme

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member UnsignedDecimal(
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
                ?xLegacyStyles:       List<Rn.LegacyStyles.RuntimeStyles>,
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


// Backward-compatible render module — referenced by ComponentRegistration.fs RegisterRender.
// The registry-based render is a no-op at runtime for modern [<Component>] components,
// but the module must compile to satisfy the existing ComponentRegistration.fs reference.
namespace LibClient.Components.Input

module UnsignedDecimalRender =

    // Stub types so the render signature matches RegisterRender's generic constraints.
    // The render path is never invoked for modern [<Component>] components.
    type Props  = unit
    type Estate = unit
    type Pstate = unit
    type Actions = unit

    let render
            (_children:        array<Fable.React.ReactElement>,
             _props:           Props,
             _estate:          Estate,
             _pstate:          Pstate,
             _actions:         Actions,
             _componentStyles: Rn.LegacyStyles.RuntimeStyles)
            : Fable.React.ReactElement =
        failwith "UnsignedDecimal is a modern [<Component>] — this render path is never called"
