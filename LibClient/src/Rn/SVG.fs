module Rn.SVG

open Fable.Core
open Fable.Core.JsInterop
open Fable.React

let private RNSvg  : obj = import "Svg"  "react-native-svg"
let private RNPath : obj = import "Path" "react-native-svg"

// height/width/viewBox props pass through unchanged (Icons.fs uses these exact names)
let ImageSvg (props: obj) (children: array<ReactElement>) : ReactElement =
    ReactBindings.React.createElement(RNSvg, props, children)

// Icons.fs passes {| fillColor = "..."; d = "..." |}
// react-native-svg Path uses `fill` not `fillColor` -- remap here so callers stay unchanged
let SvgPath (props: obj) (children: array<ReactElement>) : ReactElement =
    let rProps = createEmpty
    rProps?fill <- props?fillColor
    rProps?d    <- props?d
    ReactBindings.React.createElement(RNPath, rProps, children)
