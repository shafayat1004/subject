[<AutoOpen>]
module ThirdParty.QRCode.Components.Generator

open Fable.Core
open Fable.Core.JsInterop
open LibClient

let private QRCode : obj -> ReactElement = importDefault "react-qr-code"

[<Fable.Core.JS.Pojo>]
type private GeneratorPropsJs ( value: string, size: int, bgColor: string, fgColor: string ) =
    member val value = value
    member val size = size
    member val bgColor = bgColor
    member val fgColor = fgColor

type ThirdParty.QRCode.Components.Constructors.QRCode with
    static member Generator (value: string, ?size: int, ?bgColor: Color, ?fgColor: Color) =
        let size = defaultArg size 200
        let bgColor = defaultArg bgColor Color.White
        let fgColor = defaultArg fgColor Color.Black

        let props =
            GeneratorPropsJs(value, size, bgColor.ToCssString, fgColor.ToCssString) |> box
        Fable.React.ReactBindings.React.createElement(QRCode, props, [])
