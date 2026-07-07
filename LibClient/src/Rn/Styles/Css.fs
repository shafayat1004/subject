[<AutoOpen>]
module Rn.LegacyStyles.Css

open Fable.Core.JsInterop

let addCss (source: string) : unit =
    Rn.Runtime.ifWeb(fun document ->
        let styleElement = document.createElement("style")
        styleElement.innerHTML <- source
        document.head?append styleElement
    )