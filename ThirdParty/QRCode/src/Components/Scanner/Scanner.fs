[<AutoOpen>]
module ThirdParty.QRCode.Components.Scanner

open Fable.Core
open Fable.Core.JsInterop
open LibClient

#if !EGGSHELL_PLATFORM_IS_WEB
[<Fable.Core.JS.Pojo>]
type private ScannerPropsJs ( onRead: obj -> unit, ?bottomContent: ReactElement ) =
    member val onRead = onRead
    member val bottomContent = bottomContent

let private QRCodeScanner: obj -> ReactElement = importDefault "react-native-qrcode-scanner"

type ThirdParty.QRCode.Components.Constructors.QRCode with
    static member Scanner (onRead: obj -> unit, ?bottomContent: ReactElement) =
        let props =
            ScannerPropsJs(onRead, ?bottomContent = bottomContent) |> box
        Fable.React.ReactBindings.React.createElement(QRCodeScanner, props, [])
#else
type ThirdParty.QRCode.Components.Constructors.QRCode with
    static member Scanner (onRead: obj -> unit, ?bottomContent: ReactElement) =
        noElement
#endif