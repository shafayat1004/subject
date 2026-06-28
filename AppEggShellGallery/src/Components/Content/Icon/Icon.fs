[<AutoOpen>]
module AppEggShellGallery.Components.Content_Icon

open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Styles
open AppEggShellGallery.Icons

module private Styles =
    let sendOrange =
        makeTextStyles {
            color    (Color.Hex "#fa6e1b")
            fontSize 50
        }

    let homePurple =
        makeTextStyles {
            color (Color.Hex "#9b48db")
        }

    let devPinkLarge =
        makeTextStyles {
            fontSize 60
            color    Color.DevPink
        }

type Ui.Content with
    [<Component>]
    static member Icon() : ReactElement =
        Ui.ComponentContent(
            displayName = "Icon",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Icon",
            notes =
                LC.Text
                    "By default, setting colors on icons is done directly though a parameter to the SVG, which makes icon colors the only visual aspect of the app outside of the styles system. This component allows us to overcome this limitation, and unify all styling.",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Icon",
                    role = "none (decorative by default)",
                    namePattern = "Pass ?accessibilityLabel when icon conveys meaning; otherwise hidden from screen readers",
                    stateNotes = "Static graphic; no interactive state",
                    scalesWithFont = true,
                    contrastNotes = "Icon color inherits from theme; pair with text when color is meaningful"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            element {
                                LC.Icon(icon = Icon.Megaphone)
                                LC.Icon(icon = Icon.Send, styles = [| Styles.sendOrange |])
                                LC.Icon(icon = Icon.Home, styles = [| Styles.homePurple |])
                                LC.Icon(icon = Icon.Send, styles = [| Styles.devPinkLarge |])
                                LC.Icon(icon = Icon.Home, styles = [| Styles.devPinkLarge |])
                            },
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.Icon(icon = Icon.Megaphone)
LC.Icon(icon = Icon.Send, styles = [| sendOrangeStyles |])
LC.Icon(icon = Icon.Home, styles = [| homePurpleStyles |])
LC.Icon(icon = Icon.Send, styles = [| devPinkLargeStyles |])
LC.Icon(icon = Icon.Home, styles = [| devPinkLargeStyles |])
""" |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Styles",
                                        children =
                                            [| LC.Text """
let sendOrangeStyles = makeTextStyles {
    color    (Color.Hex "#fa6e1b")
    fontSize 50
}

let homePurpleStyles = makeTextStyles {
    color (Color.Hex "#9b48db")
}

let devPinkLargeStyles = makeTextStyles {
    fontSize 60
    color    Color.DevPink
}
""" |]
                                    )
                                }
                            )
                    )
                }
        )
