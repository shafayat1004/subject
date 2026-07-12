[<AutoOpen>]
module AppEggShellGallery.Components.Route_Home

open Fable.React
open LibClient
open LibClient.Components
open LibClient.ContextMenus
open LibRouter.Components
open Rn.Components
open Rn.Styles
open AppEggShellGallery.LocalImages

module private Styles =
    let content = makeViewStyles {
        FlexDirection.Column
        AlignItems.Center
    }

    let contentText = makeTextStyles {
        fontSize   16
        lineHeight (16. * 1.5 |> int)
    }

    let logoImage = makeViewStyles {
        height 300
        width  400
    }

    let subtitle = makeViewStyles {
        marginTop    20
        marginBottom 20
    }

    let table = makeViewStyles {
        width       500
        marginRight 32
    }

    let row = makeViewStyles {
        FlexDirection.Row
        marginBottom 16
    }

    let cellLeft = makeViewStyles {
        flex        0
        width       120
        marginRight 20
        JustifyContent.Center
    }

    let label = makeTextStyles {
        FontWeight.Bold
        TextAlign.Right
    }

    let cellRight = makeViewStyles {
        flex 1
    }

let private techRow (name: string) (description: string) : ReactElement =
    Rn.View(
        styles   = [| Styles.row |],
        children = [|
            Rn.View(
                styles   = [| Styles.cellLeft |],
                children = [| LC.Text(name, styles = [| Styles.label |]) |]
            )
            Rn.View(
                styles   = [| Styles.cellRight |],
                children = [| LC.Text description |]
            )
        |]
    )

type Ui.Route with
    [<Component>]
    static member Home(pstoreKey: string) : ReactElement =
        element {
            LC.SetPageMetadata(title = "Home")
            LR.Route(
                scroll   = LibRouter.Components.Route.Vertical,
                children = [|
                    LC.Section.Padded(
                        styles   = [| Styles.content |],
                        children = [|
                            Rn.Image(
                                source = localImage "/images/logo-sketch.jpg",
                                size   = Rn.Components.Image.FromStyles,
                                styles = [| Styles.logoImage |]
                            )
                            Rn.View(
                                styles   = [| Styles.subtitle |],
                                children = [|
                                    LC.Text("EggShell is a tech stack for front end apps.", styles = [| Styles.contentText |])
                                    LC.Text("It combines a number of technologies:", styles        = [| Styles.contentText |])
                                |]
                            )
                            Rn.View(
                                styles   = [| Styles.table |],
                                children = [|
                                    techRow "F#"      "a functional programming language with a powerful type system"
                                    techRow "React"   "a JS library for building UIs declaratively"
                                    techRow "Fable"   "an F# to JS compiler"
                                    techRow "Rn" "a layer on top of React that allows targeting ReactDOM and ReactNative from the same code base"
                                |]
                            )
                        |]
                    )
                |]
            )
        }
