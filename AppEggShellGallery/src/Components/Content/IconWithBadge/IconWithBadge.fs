[<AutoOpen>]
module AppEggShellGallery.Components.Content_IconWithBadge

open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Styles

module private SampleThemes =
    let custom (theme: LC.IconWithBadge.Theme) : LC.IconWithBadge.Theme =
        { theme with
            IconColor = Color.Black
            IconSize  = 26
            Badge = {
                theme.Badge with
                    FontSize  = 22
                    FontColor = Color.Black
            }
        }

type Ui.Content with
    [<Component>]
    static member IconWithBadge() : ReactElement =
        Ui.ComponentContent(
            displayName = "IconWithBadge",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.IconWithBadge",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            element {
                                LC.IconWithBadge(icon = Icon.ShoppingCart, badge = LC.IconWithBadge.Count 4)
                            },
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.IconWithBadge(
    icon = Icon.ShoppingCart,
    badge = LC.IconWithBadge.Count 4
)"""
                            )
                    )
                    Ui.ComponentSample(
                        visuals =
                            element {
                                LC.IconWithBadge(icon = Icon.Home, badge = LC.IconWithBadge.Text "Summer Sale!")
                            },
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.IconWithBadge(
    icon = Icon.Home,
    badge = LC.IconWithBadge.Text "Summer Sale!"
)"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            element {
                                LC.IconWithBadge(
                                    icon = Icon.ShoppingCart,
                                    badge = LC.IconWithBadge.Count 9,
                                    theme = SampleThemes.custom
                                )
                            },
                        code =
                            ComponentSample.Children(
                                element {
                                    LC.Text """
LC.IconWithBadge(
    icon = Icon.ShoppingCart,
    badge = LC.IconWithBadge.Count 9,
    theme = fun theme -> { theme with IconColor = Color.Black; IconSize = 26; Badge = { theme.Badge with FontSize = 22; FontColor = Color.Black } }
)"""
                                    LC.Text(
                                        "Styles",
                                        styles = [| makeTextStyles { FontWeight.W700 } |]
                                    )
                                    LC.Text """
theme = fun theme ->
    { theme with
        IconColor = Color.Black
        IconSize  = 26
        Badge = { theme.Badge with FontSize = 22; FontColor = Color.Black }
    }
"""
                                }
                            )
                    )
                }
        )
