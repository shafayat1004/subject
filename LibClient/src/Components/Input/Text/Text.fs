namespace LibClient.Components.Input

open Fable.Core.JsInterop
open LibClient
open Rn.Components

module Text =

    type ITextRef =
        abstract member SelectAll:    unit -> unit
        abstract member RequestFocus: unit -> unit
        abstract member Blur:         unit -> unit

    type PropSuffixFactory = InputSuffixFactory

    type KeyboardType = TextInput.KeyboardType
    type ReturnKeyType = TextInput.ReturnKeyType


// Legacy styles module — kept for backward-compatible callers (ParsedText, PositiveInteger, etc.).
namespace LibClient.Components.Input

module TextStyles =

    open Rn.LegacyStyles

    let private baseStyles =
        lazy
            (asBlocks
                [ "view" => [ Overflow.Visible ]

                  "withLabel" => [ marginTop 6 ]

                  "border"
                  => [ AlignItems.Center
                       FlexDirection.Row
                       borderWidth 1
                       backgroundColor Color.White
                       paddingHorizontal 10 ]

                  "prefix" => [ flex 0; paddingTop 1 ]

                  "suffix-icon" => [ fontSize 20 ]

                  "focus-preserving-sentinel" => [ width 0; height 0 ]

                  "text-input" => [ flex 1 ]

                  "single-line" => [ minHeight 21 ]

                  "text-input-noneditable" => [ flex 1 ]

                  "invalid-reason" => [ fontSize 12 ]

                  "label"
                  => [ Position.Absolute
                       top 13
                       left 10
                       paddingHorizontal 3
                       backgroundColor Color.White ]
                  && [ "small" => [ top -6; left 10 ] ]

                  "label-text" => [ fontSize 16 ]
                  && [ "small" => [ fontSize 12; FontWeight.W700 ] ] ])

    type (* class to enable named parameters *) Theme() =
        static member Customize = makeCustomize ("LibClient.Components.Input.Text", baseStyles)

        static member All
            (
                borderLabelBlurredColor:    Color,
                borderLabelFocusedColor:    Color,
                borderLabelInvalidColor:    Color,
                textColor:                  Color,
                noneditableTextColor:       Color,
                noneditableBackgroundColor: Color,
                invalidReasonColor:         Color,
                placeholderColor:           Color,
                theVerticalPadding:         int,
                ?theBorderRadius:           int
            ) : unit =
            Theme.Customize
                [ Theme.Rules(
                      borderLabelBlurredColor,
                      borderLabelFocusedColor,
                      borderLabelInvalidColor,
                      textColor,
                      noneditableTextColor,
                      noneditableBackgroundColor,
                      invalidReasonColor,
                      placeholderColor,
                      theVerticalPadding,
                      ?theBorderRadius = theBorderRadius
                  ) ]

        static member One
            (
                borderLabelBlurredColor:    Color,
                borderLabelFocusedColor:    Color,
                borderLabelInvalidColor:    Color,
                textColor:                  Color,
                noneditableTextColor:       Color,
                noneditableBackgroundColor: Color,
                invalidReasonColor:         Color,
                placeholderColor:           Color,
                theVerticalPadding:         int,
                ?theBorderRadius:           int
            ) : Styles =
            Theme.Rules(
                borderLabelBlurredColor,
                borderLabelFocusedColor,
                borderLabelInvalidColor,
                textColor,
                noneditableTextColor,
                noneditableBackgroundColor,
                invalidReasonColor,
                placeholderColor,
                theVerticalPadding,
                ?theBorderRadius = theBorderRadius
            )
            |> makeSheet

        static member MinHeight(theHeight: int) : Styles =
            makeSheet [ "text-input" => [ minHeight theHeight ] ]

        static member ZeroPadding() : Styles =
            makeSheet [ "border" => [ paddingVertical 0; paddingHorizontal 0 ] ]

        static member FontFamily(theFontFamily: string) : List<ISheetBuildingBlock> =
            [ "defaults" => [ fontFamily theFontFamily ] ]

        static member Rules
            (
                borderLabelBlurredColor:    Color,
                borderLabelFocusedColor:    Color,
                borderLabelInvalidColor:    Color,
                textColor:                  Color,
                noneditableTextColor:       Color,
                noneditableBackgroundColor: Color,
                invalidReasonColor:         Color,
                placeholderColor:           Color,
                theVerticalPadding:         int,
                ?theBorderRadius:           int
            ) : List<ISheetBuildingBlock> =
            let theBorderRadius = defaultArg theBorderRadius 4

            [ "border"
              => [ borderRadius theBorderRadius
                   borderColor borderLabelBlurredColor
                   paddingVertical theVerticalPadding ]
              && [ "border-focused" => [ borderColor borderLabelFocusedColor ]
                   "border-invalid" => [ borderColor borderLabelInvalidColor ]
                   "border-noneditable" => [ backgroundColor noneditableBackgroundColor ] ]

              "suffix-icon" => [ color textColor ]

              "suffix-text" => [ color textColor ]

              "invalid-reason" => [ color invalidReasonColor ]

              "label-text" => [ color borderLabelBlurredColor ]
              && [ "focused" => [ color borderLabelFocusedColor ]
                   "invalid" => [ color borderLabelInvalidColor ] ]

              "text-input" => [ color textColor ]
              && [ "noneditable"
                   => [ color noneditableTextColor; backgroundColor noneditableBackgroundColor ] ]

              "prefix" => [ color textColor ]

              "SPECIAL-placeholder" => [ color placeholderColor ] ]

        static member Border(rules: seq<RuleFunctionReturnedStyleRules>) : List<ISheetBuildingBlock> =
            [ "border" => rules ]

        static member CustomStyles(rules: seq<RuleFunctionReturnedStyleRules>) =
            let customStyle: List<List<ISheetBuildingBlock>> = [ [ "text-input" => rules ] ]
            List.concat (baseStyles.Value :: customStyle) |> makeSheet

        static member TextInputCustomStyles(rules: seq<RuleFunctionReturnedStyleRules>) : List<ISheetBuildingBlock> =
            [ "text-input" => rules ]

        static member LabelStyles(rules: seq<RuleFunctionReturnedStyleRules>) : List<ISheetBuildingBlock> =
            [ "label" => rules ]

        static member LabelTextStyles(rules: seq<RuleFunctionReturnedStyleRules>) : List<ISheetBuildingBlock> =
            [ "label-text" => rules ]

        static member InvalidReasonStyles(rules: seq<RuleFunctionReturnedStyleRules>) : List<ISheetBuildingBlock> =
            [ "invalid-reason" => rules ]

    let styles =
        lazy
            (compile (
                List.concat
                    [ baseStyles.Value
                      Theme.Rules(
                          borderLabelBlurredColor    = Color.Grey "44",
                          borderLabelFocusedColor    = Color.DevGreen,
                          borderLabelInvalidColor    = Color.DevRed,
                          textColor                  = Color.Grey "44",
                          noneditableTextColor       = Color.Grey "22",
                          noneditableBackgroundColor = Color.Grey "66",
                          invalidReasonColor         = Color.DevRed,
                          placeholderColor           = Color.Grey "aa",
                          theVerticalPadding         = 10
                      ) ]
            ))


namespace LibClient.Components

open Fable.Core.JsInterop
open Fable.React
open Browser.Types

open LibClient
open LibClient.Accessibility
open LibClient.Components.Input
open LibClient.Components.Input.Text
open LibClient.Icons

open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Input_TextComponent =

    module LC =
        module Input =
            module Text =
                type Theme = {
                    BorderLabelBlurredColor:    Color
                    BorderLabelFocusedColor:    Color
                    BorderLabelInvalidColor:    Color
                    TextColor:                  Color
                    NoneditableTextColor:       Color
                    NoneditableBackgroundColor: Color
                    EditableBackgroundColor:    Color
                    LabelBackgroundColor:       Color
                    InvalidReasonColor:         Color
                    PlaceholderColor:           Color
                    TheVerticalPadding:         int
                    BorderRadius:               int
                }

    open LC.Input.Text

    [<RequireQualifiedAccess>]
    module private Styles =
        let private outlineColorFor (theTheme: Theme) (isInvalid: bool) (isFocused: bool) =
            if isInvalid then theTheme.BorderLabelInvalidColor
            elif isFocused then theTheme.BorderLabelFocusedColor
            else theTheme.BorderLabelBlurredColor

        let view =
            ViewStyles.Memoize(fun (hasLabel: bool) ->
                makeViewStyles {
                    Overflow.Visible

                    if hasLabel then
                        marginTop 6
                })

        let border =
            ViewStyles.Memoize
                (fun (cornerRadius: int) (verticalPadding: int) (fillColor: Color) (outlineColor: Color) ->
                    makeViewStyles {
                        AlignItems.Center
                        FlexDirection.Row
                        borderWidth 1
                        borderRadius cornerRadius
                        backgroundColor fillColor
                        paddingHorizontal 10
                        paddingVertical verticalPadding
                        borderColor outlineColor
                        Overflow.Hidden
                    })

        let borderFor (theTheme: Theme) (isInvalid: bool) (isFocused: bool) (editable: bool) =
            let fillColor =
                if editable then
                    theTheme.EditableBackgroundColor
                else
                    theTheme.NoneditableBackgroundColor

            border
                theTheme.BorderRadius
                theTheme.TheVerticalPadding
                fillColor
                (outlineColorFor theTheme isInvalid isFocused)

        let prefixIconWrap =
            makeViewStyles {
                flexShrink 0
                marginRight 8
                AlignItems.Center
                JustifyContent.Center
            }

        let prefix =
            TextStyles.Memoize(fun (textColor: Color) ->
                makeTextStyles {
                    flex 0
                    paddingTop 1
                    color textColor
                })

        let suffixText =
            TextStyles.Memoize(fun (textColor: Color) -> makeTextStyles { color textColor })

        let suffixIcon =
            TextStyles.Memoize(fun (textColor: Color) ->
                makeTextStyles {
                    fontSize 20
                    color textColor
                })

        let focusPreservingSentinel =
            makeViewStyles {
                width 0
                height 0
            }

        let textInput =
            ViewStyles.Memoize(fun (noneditableFill: Color) (editable: bool) (singleLine: bool) ->
                makeViewStyles {
                    flex 1
                    minWidth 0

                    if not editable then
                        backgroundColor noneditableFill

                    if singleLine then
                        minHeight 21
#if EGGSHELL_PLATFORM_IS_WEB
                    backgroundColor Color.Transparent
                    borderWidth 0
                    padding 0
                    margin 0
#endif
                })

        let textInputText =
            TextStyles.Memoize(fun (textColor: Color) -> makeTextStyles { color textColor })

        let invalidReason =
            TextStyles.Memoize(fun (reasonColor: Color) ->
                makeTextStyles {
                    fontSize 12
                    color reasonColor
                })

        let label =
            ViewStyles.Memoize(fun (labelBg: Color) (isSmall: bool) ->
                makeViewStyles {
                    Position.Absolute
                    top (if isSmall then -6 else 13)
                    left 10
                    paddingHorizontal 3
                    backgroundColor labelBg
                })

        let labelText =
            TextStyles.Memoize(fun (labelColor: Color) (isSmall: bool) ->
                makeTextStyles {
                    fontSize (if isSmall then 12 else 16)

                    if isSmall then
                        FontWeight.W700

                    color labelColor
                })

        let labelTextFor (theTheme: Theme) (isInvalid: bool) (isFocused: bool) (isSmall: bool) =
            labelText (outlineColorFor theTheme isInvalid isFocused) isSmall

        let pressableOverlay = makeViewStyles { opacity 0. }

    type private TextRef(textInputRef: IRefHook<Option<ITextInputRef>>) =
        interface ITextRef with
            member _.SelectAll() : unit =
                textInputRef.current |> Option.sideEffect (fun input -> input.selectAll ())

            member _.RequestFocus() : unit =
                textInputRef.current |> Option.sideEffect (fun input -> input.requestFocus ())

            member _.Blur() : unit =
                textInputRef.current |> Option.sideEffect (fun input -> input.blur ())

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member Text
            (
                value:                Option<NonemptyString>,
                onChange:             Option<NonemptyString> -> unit,
                validity:             InputValidity,
                ?label:               string,
                ?accessibilityLabel:  string,
                ?accessibilityRole:   AccessibilityRole,
                ?onKeyPress:          KeyboardEvent -> unit,
                ?onEnterKeyPress:     ReactEvent.Keyboard -> unit,
                ?onFocus:             FocusEvent -> unit,
                ?onBlur:              FocusEvent -> unit,
                ?placeholder:         string,
                ?prefix:              string,
                ?prefixIcon:          LibClient.Icons.IconConstructor,
                ?suffix:              InputSuffix,
                ?maxLength:           int,
                ?tabIndex:            int,
                ?editable:            bool,
                ?blurOnSubmit:        bool,
                ?multiline:           bool,
                ?requestFocusOnMount: bool,
                ?secureTextEntry:     bool,
                ?keyboardType:        KeyboardType,
                ?returnKeyType:       ReturnKeyType,
                ?autoCapitalize:      AutoCapitalize,
                ?styles:              array<ViewStyles>,
                ?theme:               Theme -> Theme,
                ?testId:              string,
                ?ref:                 LibClient.JsInterop.JsNullable<ITextRef> -> unit,
                ?key:                 string,
                ?xLegacyStyles:       List<Rn.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            key |> ignore

            let editable = defaultArg editable true
            let blurOnSubmit = defaultArg blurOnSubmit false
            let multiline = defaultArg multiline false
            let requestFocusOnMount = defaultArg requestFocusOnMount false
            let secureTextEntry = defaultArg secureTextEntry false
            let keyboardType = defaultArg keyboardType KeyboardType.Default
            let returnKeyType = defaultArg returnKeyType ReturnKeyType.Done
            let autoCapitalize = defaultArg autoCapitalize AutoCapitalize.Never

            let theTheme = Themes.GetMaybeUpdatedWith theme

            let legacyStyles: array<ViewStyles> =
                match xLegacyStyles with
                | Some ls ->
                    match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                    | [] -> [||]
                    | styles ->
                        [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles>
                               "Rn.Components.View"
                               styles |]
                | None -> [||]

            let isFocusedHook = Hooks.useState false
            let textInputRef = Hooks.useRef<Option<ITextInputRef>> None

#if EGGSHELL_PLATFORM_IS_WEB
            // Web: parent value is synchronous; draft avoids flicker when props lag.
            let draftValueHook = Hooks.useState (value |> NonemptyString.optionToString)

            Hooks.useEffect (
                (fun () ->
                    if not isFocusedHook.current then
                        draftValueHook.update (value |> NonemptyString.optionToString)

                    ()),
                [| value |]
            )
#endif

            let handleChangeText (text: string) : unit =
#if EGGSHELL_PLATFORM_IS_WEB
                draftValueHook.update text
#endif
                onChange (NonemptyString.ofString text)

            Hooks.useEffect (
                (fun () ->
                    match ref with
                    | Some bindRef ->
                        let textRef = TextRef(textInputRef) :> ITextRef
                        bindRef (textRef :> obj :?> LibClient.JsInterop.JsNullable<ITextRef>)
                    | None -> Noop),
                [| ref |]
            )

            let isLabelSmall = value.IsSome || isFocusedHook.current || placeholder.IsSome

            let handleOnKeyPress (e: KeyboardEvent) : unit =
                onKeyPress |> Option.sideEffect (fun f -> f e)

                match (multiline, e.key, onEnterKeyPress) with
                | (false, KeyboardEvent.Key.Enter, Some onEnterKeyPress) ->
                    onEnterKeyPress { Event = e; MaybeSource = None }
                | (true, KeyboardEvent.Key.Enter, _) ->
                    Log.Error "`OnEnterKeyPress` does not fire when `Multiline='true'`"
                | _ -> Noop

            let onKeyPressOption =
                match (onKeyPress, onEnterKeyPress) with
                | (None, None) -> None
                | _            -> Some handleOnKeyPress

            let bindTextInput (nullableInstance: LibClient.JsInterop.JsNullable<ITextInputRef>) : unit =
                textInputRef.current <-
                    nullableInstance.ToOption
                    |> Option.map (fun raw ->
                        let rawObj = raw :> obj

                        { new ITextInputRef with
                            member _.selectAll() : unit =
                                rawObj?focus () |> ignore

                                let len =
                                    match rawObj?value with
                                    | null        -> 0
                                    | (s: string) -> s.Length

                                rawObj?setSelection (0, len) |> ignore

                            member _.requestFocus() : unit = rawObj?focus () |> ignore
                            member _.blur() : unit = rawObj?blur ()          |> ignore })

            let resolvedTestId =
                testId
                |> Option.orElse (label |> Option.map (A11ySlug.testId "input"))
                |> Option.defaultValue "input-text"

            let inputA11yLabel =
                accessibilityLabel |> Option.orElse label |> Option.orElse placeholder

            // When used as a search field, expose the container as a search landmark so screen
            // readers announce the region. The inner Rn.TextInput also gets role=searchbox via RNW.
            let containerRole =
                match accessibilityRole with
                | Some AccessibilityRole.Search -> accessibilityRole
                | _                             -> None

            Rn.View(
                testId             = resolvedTestId,
                ?accessibilityRole = containerRole,
                styles =
                    [| Styles.view label.IsSome
                       yield! legacyStyles
                       yield! (defaultArg styles [||]) |],
                children =
                    [| Rn.View(
                           styles = [| Styles.borderFor theTheme validity.IsInvalid isFocusedHook.current editable |],
                           children =
                               [| match prefixIcon with
                                  | Some iconCtor ->
                                      Rn.View(
                                          importantForAccessibility =
                                              LibClient.Accessibility.ImportantForAccessibility.No,
                                          styles   = [| Styles.prefixIconWrap |],
                                          children = [| iconCtor theTheme.PlaceholderColor 16 |]
                                      )
                                  | None -> noElement

                                  match (isLabelSmall, prefix) with
                                  | (true, Some prefixText) ->
                                      LC.UiText(value = prefixText, styles = [| Styles.prefix theTheme.TextColor |])
                                  | _ -> Rn.View(styles = [| Styles.focusPreservingSentinel |])

#if EGGSHELL_PLATFORM_IS_WEB
                                  Rn.TextInput(
                                      styles =
                                          [| Styles.textInput
                                                 theTheme.NoneditableBackgroundColor
                                                 editable
                                                 (not multiline) |],
                                      value               = draftValueHook.current,
                                      onChangeText        = handleChangeText,
                                      ?accessibilityLabel = inputA11yLabel,
                                      ?accessibilityRole  = accessibilityRole,
                                      onFocus =
                                          (fun e ->
                                              isFocusedHook.update true
                                              onFocus |> Option.sideEffect (fun f -> f e)),
                                      onBlur =
                                          (fun e ->
                                              isFocusedHook.update false
                                              onBlur |> Option.sideEffect (fun f -> f e)),
                                      multiline            = multiline,
                                      autoFocus            = requestFocusOnMount,
                                      editable             = editable,
                                      blurOnSubmit         = blurOnSubmit,
                                      placeholder          = (placeholder |> Option.defaultValue ""),
                                      placeholderTextColor = theTheme.PlaceholderColor.ToRnString,
                                      ``ref``              = bindTextInput,
                                      secureTextEntry      = secureTextEntry,
                                      keyboardType         = keyboardType,
                                      returnKeyType        = returnKeyType,
                                      autoCapitalize       = autoCapitalize
                                  )
#else
                                  // Native: Rn TextInput patched via LibClient/vendor (metro resolveRequest)
                                  // to skip prop sync while focused; do not remount on focus (breaks iOS keyboard).
                                  Rn.TextInput(
                                      styles =
                                          [| Styles.textInput
                                                 theTheme.NoneditableBackgroundColor
                                                 editable
                                                 (not multiline) |],
                                      value               = (value |> NonemptyString.optionToString),
                                      onChangeText        = handleChangeText,
                                      ?accessibilityLabel = inputA11yLabel,
                                      ?accessibilityRole  = accessibilityRole,
                                      onFocus =
                                          (fun e ->
                                              isFocusedHook.update true
                                              onFocus |> Option.sideEffect (fun f -> f e)),
                                      onBlur =
                                          (fun e ->
                                              isFocusedHook.update false
                                              onBlur |> Option.sideEffect (fun f -> f e)),
                                      multiline            = multiline,
                                      autoFocus            = requestFocusOnMount,
                                      editable             = editable,
                                      blurOnSubmit         = blurOnSubmit,
                                      placeholder          = (placeholder |> Option.defaultValue ""),
                                      placeholderTextColor = theTheme.PlaceholderColor.ToRnString,
                                      ``ref``              = bindTextInput,
                                      secureTextEntry      = secureTextEntry,
                                      keyboardType         = keyboardType,
                                      returnKeyType        = returnKeyType,
                                      autoCapitalize       = autoCapitalize
                                  )
#endif

                                  match (isLabelSmall, suffix) with
                                  | (true, Some(InputSuffix.Text text)) ->
                                      LC.UiText(value = text, styles = [| Styles.suffixText theTheme.TextColor |])
                                  | (true, Some(InputSuffix.Icon icon)) ->
                                      LC.Icon(icon = icon, styles = [| Styles.suffixIcon theTheme.TextColor |])
                                  | (true, Some(InputSuffix.Element element)) -> element
                                  | _                                         -> Rn.View(styles = [| Styles.focusPreservingSentinel |]) |]
                       )

                       match validity.InvalidReason with
                       | Some reason ->
                           LC.UiText(value = reason, styles = [| Styles.invalidReason theTheme.InvalidReasonColor |])
                       | None -> noElement

                       match label with
                       | Some labelText ->
                           Rn.View(
                               styles = [| Styles.label theTheme.LabelBackgroundColor isLabelSmall |],
                               children =
                                   [| LC.UiText(
                                          value = labelText,
                                          styles =
                                              [| Styles.labelTextFor
                                                     theTheme
                                                     validity.IsInvalid
                                                     isFocusedHook.current
                                                     isLabelSmall |]
                                      )
                                      LC.Pressable(
                                          onPress =
                                              (fun _ ->
                                                  textInputRef.current
                                                  |> Option.sideEffect (fun textInput -> textInput.requestFocus ())),
                                          label         = labelText,
                                          testId        = sprintf "%s-focus" resolvedTestId,
                                          role          = AccessibilityRole.Button,
                                          overlay       = true,
                                          styles        = [| Styles.pressableOverlay |],
                                          componentName = "LC.Input.Text.Focus"
                                      ) |]
                           )
                       | None -> Rn.View(styles = [| Styles.focusPreservingSentinel |]) |]
            )
