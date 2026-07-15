namespace Rn

open Fable.Core
open Fable.Core.JsInterop

module Helpers =
    let extractProp<'T when 'T: null> (key: string) (props: obj) : Option<'T> =
        let value: 'T = props?(key)

        match isNull value with
        | true  -> None
        | false -> Some value
