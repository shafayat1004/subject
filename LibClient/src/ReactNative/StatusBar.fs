[<AutoOpen>]
module ReactNative.Components.StatusBar

#if !EGGSHELL_PLATFORM_IS_WEB
open LibClient
open Fable.Core
open Fable.Core.JsInterop
let private reactNativeStatusBar : obj = import "StatusBar" "react-native"
let private MakeReactNativeStatusBar: obj -> ReactElements -> ReactElement =
    ThirdParty.wrapComponent<obj>(reactNativeStatusBar)

[<RequireQualifiedAccess>]
type ReactNativeStatusBarStyle =
| Default
| Light
| Dark
    with
        member this.toJS =
            match this with
            | ReactNativeStatusBarStyle.Light   -> "light-content"
            | ReactNativeStatusBarStyle.Dark    -> "dark-content"
            | ReactNativeStatusBarStyle.Default -> "default"

[<Fable.Core.JS.Pojo>]
type private StatusBarPropsJs ( ?key: string, ?barStyle: string ) =
    member val key = key
    member val barStyle = barStyle

type RN with
    static member StatusBar (
        ?key:      string,
        ?barStyle: ReactNativeStatusBarStyle
    ) =
        let maybeBarStyle = barStyle |> Option.map (fun s -> s.toJS)
        let __props =
            StatusBarPropsJs(?key = key, ?barStyle = maybeBarStyle)
            |> box

        MakeReactNativeStatusBar
            __props
            [|  |]
#endif
