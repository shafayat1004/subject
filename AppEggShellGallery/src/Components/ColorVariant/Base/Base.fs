[<AutoOpen>]
module AppEggShellGallery.Components.ColorVariant.Base

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.Components
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view = makeViewStyles {
        AlignItems.Center
    }

let render (name: string) (color: Color) (isMain: bool) : ReactElement =
    RX.View(
        styles = [| Styles.view |],
        children =
            [|
                Ui.ColorVariant.ColorBlock(color = color, isMain = isMain)
                LC.Text name
            |]
    )

type AppEggShellGallery.Components.Constructors.Ui.ColorVariant with
    [<Component>]
    static member Base(
            color:          Color,
            name:           string,
            isMain:         bool,
            ?children:      ReactChildrenProp,
            ?key:           string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)
        render name color isMain
