[<AutoOpen>]
module AppEggShellGallery.Components.Content_Icons

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.Colors
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            FlexDirection.Row
            FlexWrap.Wrap
            paddingVertical 10
        }

    let iconContainer =
        makeViewStyles {
            margin 5
            padding 5
            width 130
            Overflow.Visible
            AlignItems.Center
        }

    let icon =
        makeTextStyles {
            color (colors.Neutral.B600)
            fontSize 45
        }

    let iconLabel =
        makeTextStyles {
            fontSize 10
            marginVertical 2
        }

type Ui.Content with
    [<Component>]
    static member Icons () : ReactElement =
        RX.View(
            [|
                yield!
                    AppEggShellGallery.ScrapedData.Icons.iconsList
                    |> List.map (fun (iconName, icon) ->
                        RX.View(
                            styles = [| Styles.iconContainer |],
                            children = [|
                                LC.Icon(icon, styles = [| Styles.icon |])
                                LC.UiText(iconName, styles = [| Styles.iconLabel |])
                            |]
                        )
                    )
            |],
            styles = [| Styles.view |]
        )
