[<AutoOpen>]
module AppEggShellGallery.Components.ColorVariant.ColorBlock

open Fable.Core.JsInterop
open Fable.React
open LibClient

type AppEggShellGallery.Components.Constructors.Ui.ColorVariant with
    [<Component>]
    static member ColorBlock(
            color:          Color,
            ?children:      ReactChildrenProp,
            ?isMain:        bool,
            ?key:           string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)
        let isMain = defaultArg isMain false

        let props =
            createObj [
                "style" ==>
                    createObj [
                        "display"         ==> "flex"
                        "justifyContent"  ==> "center"
                        "alignItems"      ==> "center"
                        "marginBottom"    ==> 3
                        "color"           ==> "white"
                        "fontWeight"      ==> "900"
                        "width"           ==> 50
                        "height"          ==> 50
                        if isMain then "borderRadius" ==> "50%"
                        "backgroundColor" ==> color.ToCssString
                    ]
                if isMain then
                    "dangerouslySetInnerHTML" ==>
                        createObj [
                            "__html" ==> "Main"
                        ]
            ]

        ReactBindings.React.createElement("div", props, [||])
