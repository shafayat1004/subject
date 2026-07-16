[<AutoOpen>]
module AppEggShellGallery.Components.ColorVariants

open Fable.React
open LibClient
open AppEggShellGallery.Components.ColorVariant.Base
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view = makeViewStyles {
        FlexDirection.Row
    }

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member ColorVariants(
            value:          Variants,
            ?children:      ReactChildrenProp,
            ?key:           string,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)

        let main = value.Main

        Rn.View(
            styles = [| Styles.view |],
            children =
                [|
                    render "900" value.B900 (value.B900 = main)
                    render "800" value.B800 (value.B800 = main)
                    render "700" value.B700 (value.B700 = main)
                    render "600" value.B600 (value.B600 = main)
                    render "500" value.B500 (value.B500 = main)
                    render "400" value.B400 (value.B400 = main)
                    render "300" value.B300 (value.B300 = main)
                    render "200" value.B200 (value.B200 = main)
                    render "100" value.B100 (value.B100 = main)
                    render "050" value.B050 (value.B050 = main)
                |]
        )
