module LibRouter.Components.Link

open Fable.Core
open Fable.Core.JsInterop

[<Fable.Core.JS.Pojo>]
type private LinkPropsJs ( ``to``: string, children: obj ) =
    member val ``to`` = ``to``
    member val children = children

type Props = (* GenerateMakeFunction *) {
    To:  string
    key: string option // defaultWithAutoWrap LibClient.JsInterop.Undefined
}

let Link: obj = Fable.Core.JsInterop.import "Link" "react-router-dom"

let Make =
    LibClient.ThirdParty.wrapComponentTransformingProps<Props>
        Link
        (fun (props: Props) ->
            LinkPropsJs(props.To, props?children) |> box
        )
