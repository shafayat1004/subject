[<AutoOpen>]
module ThirdParty.SafeAreaContext.Components.SafeAreView

open LibClient
open Rn.Styles

#if !EGGSHELL_PLATFORM_IS_WEB
open Fable.Core
open Fable.Core.JsInterop

[<Fable.Core.JS.Pojo>]
type private SafeAreaViewPropsJs ( ?style: obj, ?key: string ) =
    member val style = style
    member val key = key

let private safeAreaView : obj = import "SafeAreaView" "react-native-safe-area-context"
let private MakeSafeAreaView: obj -> ReactElements -> ReactElement =
    ThirdParty.wrapComponent<obj>(safeAreaView)
#else
open Rn.Components
#endif

type SafeAreaContext with
    static member SafeAreaView (
        ?styles:   array<ViewStyles>,
        ?children: ReactElements,
        ?key:      string
    ) =
        let children =
            children
            |> Option.map tellReactArrayKeysAreOkay
            |> Option.getOrElse [||]
            |> ThirdParty.fixPotentiallySingleChild

        #if !EGGSHELL_PLATFORM_IS_WEB
        let __props =
            SafeAreaViewPropsJs(
                ?style = (styles |> Option.map box),
                ?key   = key
            ) |> box

        MakeSafeAreaView
            __props
            children
        #else
        Rn.View (?styles = styles, children = children)
        #endif
