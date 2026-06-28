[<AutoOpen>]
module LibRouter.Components.NativeBackButton

open Fable.Core.JsInterop
open Fable.React
open LibClient
open LibRouter.Components.Constructors

let private BackHandler: obj = import "BackHandler" "react-native"

type LR with
    [<Component>]
    static member NativeBackButton (goBack: unit -> unit, ?key: string) : ReactElement =
        ignore key

        Hooks.useEffectDisposableFn(
            (fun () ->
                BackHandler?addEventListener(
                    "hardwareBackPress",
                    (fun () ->
                        goBack ()
                        true
                    )
                )
            ),
            (fun () -> ()),
            [| box goBack |]
        )

        noElement
