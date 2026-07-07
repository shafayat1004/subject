[<AutoOpen>]
module Rn.Components.TextInput

open Rn.Helpers

open Fable.Core.JsInterop
open Browser.Types
open Fable.Core

[<StringEnum>]
type KeyboardType =
| Default
| Numeric
| [<CompiledName("email-address")>] EmailAddress
| [<CompiledName("number-pad")>] NumberPad

[<StringEnum>]
type AutoCapitalize =
| [<CompiledName("none")>]Never
| Sentences
| Words
| Characters

[<StringEnum>]
type KeyboardAppearance =
| Default
| Light
| Dark

[<StringEnum>]
type ReturnKeyType =
| Done
| Go
| Next
| Search
| Send

[<StringEnum>]
type ClearButtonMode =
| Never
| [<CompiledName("while-editing")>] WhileEditing
| [<CompiledName("unless-editing")>] UnlessEditing
| Always

type ITextInputRef =
    abstract member selectAll:    unit -> unit;
    abstract member requestFocus: unit -> unit;
    abstract member blur: unit -> unit;

module private TextInputRN =
    let unboxStyles (styles: array<Rn.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    // Rn: float -> float -> unit; RN: {nativeEvent: {selection: {start, end}}}
    let wrapOnSelectionChange (f: (float -> float -> unit) option) : obj option =
        f |> Option.map (fun handler ->
            box (fun (e: obj) ->
                let s = e?nativeEvent?selection?start |> float
                let en = e?nativeEvent?selection?``end`` |> float
                handler s en))

    // Rn: float -> float -> unit (scrollX, scrollY); RN: scroll event
    let wrapOnScroll (f: (float -> float -> unit) option) : obj option =
        f |> Option.map (fun handler ->
            box (fun (e: obj) ->
                let x = e?nativeEvent?contentOffset?x |> float
                let y = e?nativeEvent?contentOffset?y |> float
                handler x y))

    let assignWebProps (props: obj) (onPaste: (ClipboardEvent -> unit) option) (tabIndex: int option) : unit =
        #if EGGSHELL_PLATFORM_IS_WEB
        onPaste   |> Option.iter (fun v -> props?onPaste <- v)
        tabIndex  |> Option.iter (fun v -> props?tabIndex <- v)
        #endif
        ()

type Rn.Components.Constructors.Rn with
    static member TextInput(
        ?autoCapitalize:           AutoCapitalize,
        ?autoCorrect:              bool,
        ?autoFocus:                bool,
        ?blurOnSubmit:             bool,
        ?defaultValue:             string,
        ?editable:                 bool,
        ?keyboardType:             KeyboardType,
        ?maxLength:                float,
        ?multiline:                bool,
        ?placeholder:              string,
        ?placeholderTextColor:     string,
        ?secureTextEntry:          bool,
        ?value:                    string,
        ?title:                    string,
        ?allowFontScaling:         bool,
        ?maxContentSizeMultiplier: float,
        ?keyboardAppearance:       KeyboardAppearance,
        ?returnKeyType:            ReturnKeyType,
        ?disableFullscreenUI:      bool,
        ?spellCheck:               bool,
        ?selectionColor:           string,
        ?tabIndex:                 int,
        ?clearButtonMode:          ClearButtonMode,
        ?onKeyPress:               KeyboardEvent -> unit,
        ?onFocus:                  FocusEvent -> unit,
        ?onBlur:                   FocusEvent -> unit,
        ?onPaste:                  ClipboardEvent -> unit,
        ?onChangeText:             string -> unit,
        ?onSelectionChange:        float -> float -> unit,
        ?onSubmitEditing:          unit -> unit,
        ?onScroll:                 float -> float -> unit,
        ?ref:                      LibClient.JsInterop.JsNullable<ITextInputRef> -> unit,
        ?accessibilityLabel:       string,
        ?accessibilityRole:        LibClient.Accessibility.AccessibilityRole,
        ?key:                      string,
        ?styles:                   array<Rn.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:            List<Rn.LegacyStyles.RuntimeStyles>
    ) =
        // title is Rn-only (iOS accessibility label fallback)
        ignore (title, xLegacyStyles)

        let __props = createEmpty

        __props?autoCapitalize           <- autoCapitalize
        __props?autoCorrect              <- autoCorrect
        __props?autoFocus                <- autoFocus
        __props?blurOnSubmit             <- blurOnSubmit
        __props?defaultValue             <- defaultValue
        __props?editable                 <- editable |> Option.orElse (Some true)
        __props?keyboardType             <- keyboardType
        __props?maxLength                <- maxLength
        __props?multiline                <- multiline
        __props?placeholder              <- placeholder
        __props?placeholderTextColor     <- placeholderTextColor
        __props?secureTextEntry          <- secureTextEntry
        __props?value                    <- value
        __props?allowFontScaling         <- allowFontScaling
        __props?maxContentSizeMultiplier <- maxContentSizeMultiplier
        __props?keyboardAppearance       <- keyboardAppearance
        __props?returnKeyType            <- returnKeyType
        __props?disableFullscreenUI      <- disableFullscreenUI
        __props?spellCheck               <- spellCheck |> Option.orElse (Some false)
        __props?selectionColor           <- selectionColor
        __props?clearButtonMode          <- clearButtonMode
        __props?onKeyPress               <- onKeyPress
        __props?onFocus                  <- onFocus
        __props?onBlur                   <- onBlur
        __props?onChangeText             <- onChangeText
        __props?onSelectionChange        <- TextInputRN.wrapOnSelectionChange onSelectionChange
        __props?onSubmitEditing          <- onSubmitEditing
        __props?onScroll                 <- TextInputRN.wrapOnScroll onScroll
        __props?ref                      <- ref
        __props?accessibilityLabel       <- accessibilityLabel
        __props?accessibilityRole        <- (accessibilityRole |> Option.bind Rn.RnPrimitives.mapAccessibilityRole)
        __props?key                      <- key
        __props?style                    <- TextInputRN.unboxStyles styles

        TextInputRN.assignWebProps __props onPaste tabIndex

        Rn.RnPrimitives.createElement Rn.RnPrimitives.TextInput __props [||]
