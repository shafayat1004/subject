[<AutoOpen>]
module AppEggShellGallery.Components.Content_ColorVariants

open Fable.React
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let colorVariantContainer =
        makeViewStyles {
            FlexDirection.Row
            marginVertical 20
            Overflow.VisibleForScrolling
        }

    let variant = makeViewStyles { marginVertical 5 }

type Ui.Content with
    [<Component>]
    static member ColorVariants () : ReactElement =
        element {
            LC.Heading(children = [| LC.UiText "Color Variants" |])

            Rn.ScrollView(
                children =
                    (AppEggShellGallery.ScrapedData.Colors.colorVariantsData
                     |> List.map (fun (colorName, colorVariantFn) ->
                         Rn.View(
                             styles   = [| Styles.colorVariantContainer |],
                             children = [|
                                 Rn.View(
                                     children = [|
                                         LC.Heading(level = Heading.Secondary, children = [| LC.UiText colorName |])
                                         Rn.View(
                                             styles   = [| Styles.variant |],
                                             children = [| Ui.ColorVariants(value = colorVariantFn) |]
                                         )
                                     |]
                                 )
                             |]
                         ))
                     |> List.toArray),
                horizontal = true
            )
        }
