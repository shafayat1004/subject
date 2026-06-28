[<AutoOpen>]
module ReactNative.Components.KeyboardAvoidingView

open LibClient
open Fable.Core
open Fable.Core.JsInterop

type KeyboardBehavior =
| Height
| Position
| Padding
    with
        member this.toJS =
            match this with
            | Height -> "height"
            | Position -> "position"
            | Padding -> "padding"

#if !EGGSHELL_PLATFORM_IS_WEB
let ReactNativeKeyboardAvoidingViewRaw: obj = import "KeyboardAvoidingView" "react-native"
let KeyboardRaw: obj = import "Keyboard" "react-native"

let private MakeReactNativeKeyboardAvoidingView: obj -> ReactElements -> ReactElement =
    LibClient.ThirdParty.wrapComponent<obj>(ReactNativeKeyboardAvoidingViewRaw)

[<Fable.Core.JS.Pojo>]
type private FlexOneStyleJs ( flex: int ) =
    member val flex = flex

[<Fable.Core.JS.Pojo>]
type private KeyboardAvoidingViewPropsJs
    ( behavior: string, keyboardVerticalOffset: int, style: obj, ?key: string ) =
    member val key = key
    member val behavior = behavior
    member val keyboardVerticalOffset = keyboardVerticalOffset
    member val style = style

type ReactNative.Components.Constructors.RN with
    static member KeyboardAvoidingView(
        ?children: ReactElements,
        ?behavior: KeyboardBehavior,
        ?keyboardVerticalOffset: int,
        ?key:      string
    ) =
        let keyboardBehavior = defaultArg behavior KeyboardBehavior.Height
        let keyboardVerticalOffset = defaultArg keyboardVerticalOffset 0

        let __props =
            KeyboardAvoidingViewPropsJs(
                keyboardBehavior.toJS,
                keyboardVerticalOffset,
                FlexOneStyleJs(1) |> box,
                ?key = key
            )
            |> box

        let children =
            children
            |> Option.map tellReactArrayKeysAreOkay
            |> Option.getOrElse [||]
            |> ThirdParty.fixPotentiallySingleChild

        let keyboardAvoidingView =
            MakeReactNativeKeyboardAvoidingView
                __props
                children

        RN.TouchableWithoutFeedback (
            onPress = (fun _ -> KeyboardRaw?dismiss()),
            children = [|keyboardAvoidingView|]
        )
#else
type ReactNative.Components.Constructors.RN with
    static member KeyboardAvoidingView(
        ?children: ReactElements,
        ?behavior: KeyboardBehavior,
        ?keyboardVerticalOffset: int,
        ?key:      string
    ) =
        ignore key
        ignore behavior
        ignore keyboardVerticalOffset
        ThirdParty.fixPotentiallySingleChild (Option.map tellReactArrayKeysAreOkay children |> Option.getOrElse [||])
#endif