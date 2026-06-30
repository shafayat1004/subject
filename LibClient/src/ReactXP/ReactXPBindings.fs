namespace ReactXP

open Fable.Core
open Fable.Core.JsInterop
open Fable.React

/// ReactXP `Popup.show` options object (getAnchor, renderPopup, onDismiss).
[<Fable.Core.JS.Pojo>]
type PopupShowOptionsJs
    (getAnchor: unit -> obj, renderPopup: obj -> int -> int -> int -> ReactElement, onDismiss: unit -> unit) =
    member val getAnchor = getAnchor
    member val renderPopup = renderPopup
    member val onDismiss = onDismiss

module Helpers =
    let ReactXPRaw: obj = import "*" "@chaldal/reactxp"

    let popupShowOptions getAnchor renderPopup onDismiss : obj =
        PopupShowOptionsJs(getAnchor, renderPopup, onDismiss) |> box

    let extractProp<'T when 'T: null> (key: string) (props: obj) : Option<'T> =
        let value: 'T = props?(key)

        match isNull value with
        | true -> None
        | false -> Some value
