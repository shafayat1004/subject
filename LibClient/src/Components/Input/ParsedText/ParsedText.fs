// Public types (preserves LibClient.Components.Input.ParsedText.* path for callers)
namespace LibClient.Components.Input

open LibClient
open ReactXP.Styles
open ReactXP.Components

module ParsedText =

    type PropSuffixFactory = InputSuffixFactory

    type Value<'T> =
        private {
            PrivateRaw:    Option<NonemptyString>
            PrivateResult: Result<Option<'T>, string>
        }

        member this.Result : Result<Option<'T>, string> = this.PrivateResult

        static member OfRaw (parse: Option<NonemptyString> -> Result<Option<'T>, string>) (raw: Option<NonemptyString>) : Value<'T> =
            {
                PrivateRaw    = raw
                PrivateResult = parse raw
            }

        static member Wrap (value: 'T, stringRepresentation: string) : Value<'T> = {
            PrivateRaw    = NonemptyString.ofString stringRepresentation
            PrivateResult = Ok (Some value)
        }

    let getRawInputValueForFieldInternalProcessing (source: Value<'T>) : Option<NonemptyString> =
        source.PrivateRaw

    type KeyboardType  = TextInput.KeyboardType
    type ReturnKeyType = TextInput.ReturnKeyType


// Legacy styles module — kept for backward-compatible callers (Decimal, EmailAddress, etc.).
// Do NOT add new usages; prefer passing styles/theme params to the component directly.
namespace LibClient.Components.Input

module ParsedTextStyles =

    open ReactXP.LegacyStyles

    let private baseStyles = lazy (asBlocks [])

    type (* class to enable named parameters *) Theme() =
        static let customize = makeCustomize("LibClient.Components.Input.ParsedText", baseStyles)

        static member All (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : unit =
            customize [
                Theme.Rules(borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding)
            ]

        static member One (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : Styles =
            Theme.Rules(borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding) |> makeSheet

        static member MinHeight (theHeight: int) : Styles =
            makeSheet [
                "theme" ==> LibClient.Components.Input.TextStyles.Theme.MinHeight theHeight
            ]

        static member ZeroPadding (): Styles =
            makeSheet [
                "theme" ==> LibClient.Components.Input.TextStyles.Theme.ZeroPadding ()
            ]

        static member Rules (borderLabelBlurredColor: Color, borderLabelFocusedColor: Color, borderLabelInvalidColor: Color, textColor: Color, noneditableTextColor: Color, noneditableBackgroundColor: Color, invalidReasonColor: Color, placeholderColor: Color, theVerticalPadding: int) : List<ISheetBuildingBlock> = [
            "theme" ==> LibClient.Components.Input.TextStyles.Theme.One (borderLabelBlurredColor, borderLabelFocusedColor, borderLabelInvalidColor, textColor, noneditableTextColor, noneditableBackgroundColor, invalidReasonColor, placeholderColor, theVerticalPadding)
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
open LibClient.Accessibility
open LibClient.Components.Input
open ReactXP.Components
open ReactXP.Styles

[<AutoOpen>]
module Input_ParsedTextComponent =

    open LibClient.Components.Input.ParsedText

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member ParsedText(
                parse:                      Option<NonemptyString> -> Result<Option<'T>, string>,
                value:                      Value<'T>,
                validity:                   InputValidity,
                requestFocusOnMount:        bool,
                onChange:                   Value<'T> -> unit,
                ?children:                  ReactChildrenProp,
                ?editable:                  bool,
                ?keyboardType:              KeyboardType,
                ?returnKeyType:             ReturnKeyType,
                ?shouldShowValidationErrors: bool,
                ?label:                     string,
                ?placeholder:               string,
                ?prefix:                    string,
                ?suffix:                    InputSuffix,
                ?tabIndex:                  int,
                ?onKeyPress:                (Browser.Types.KeyboardEvent -> unit),
                ?onEnterKeyPress:           (ReactEvent.Keyboard -> unit),
                ?styles:                    array<ViewStyles>,
                ?testId:                    string,
                ?key:                       string,
                ?xLegacyStyles:             List<ReactXP.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            children |> ignore
            key |> ignore

            let editable                  = defaultArg editable true
            let keyboardType              = defaultArg keyboardType KeyboardType.Default
            let returnKeyType             = defaultArg returnKeyType ReturnKeyType.Done
            let shouldShowValidationErrors = defaultArg shouldShowValidationErrors true

            let computedValidity =
                if shouldShowValidationErrors then
                    match value.Result with
                    | Ok _      -> validity
                    | Error msg -> InputValidity.Invalid msg
                else
                    InputValidity.Valid

            let themeLegacyStyles =
                match xLegacyStyles with
                | Some ls ->
                    let s = ReactXP.LegacyStyles.Runtime.findApplicableStyles ls "theme"
                    if s.IsEmpty then None else Some s
                | None -> None

            let resolvedTestId =
                testId
                |> Option.orElse (label |> Option.map (A11ySlug.testId "input"))
                |> Option.defaultValue "input-parsed-text"

            RX.View(
                testId = resolvedTestId,
                children =
                    [|
                        LC.Input.Text(
                            validity            = computedValidity,
                            ?tabIndex           = tabIndex,
                            ?suffix             = suffix,
                            ?prefix             = prefix,
                            ?placeholder        = placeholder,
                            requestFocusOnMount = requestFocusOnMount,
                            multiline           = false,
                            ?onEnterKeyPress    = onEnterKeyPress,
                            ?onKeyPress         = onKeyPress,
                            onChange            = (Value.OfRaw parse >> onChange),
                            value               = getRawInputValueForFieldInternalProcessing value,
                            ?label              = label,
                            editable            = editable,
                            keyboardType        = keyboardType,
                            returnKeyType       = returnKeyType,
                            ?styles             = styles,
                            ?xLegacyStyles      = themeLegacyStyles
                        )
                    |]
            )
