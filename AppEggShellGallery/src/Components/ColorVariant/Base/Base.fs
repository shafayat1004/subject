[<AutoOpen>]
module AppEggShellGallery.Components.ColorVariant.Base

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.Components
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view = makeViewStyles {
        AlignItems.Center
    }

let render (name: string) (color: Color) (isMain: bool) : ReactElement =
    Rn.View(
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
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)
        render name color isMain
