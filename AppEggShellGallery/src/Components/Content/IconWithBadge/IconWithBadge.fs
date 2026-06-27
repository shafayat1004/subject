[<AutoOpen>]
module AppEggShellGallery.Components.Content_IconWithBadge

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.IconWithBadge
open AppEggShellGallery.Icons

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
            notes = LC.Text "IconWithBadge overlays a count or text badge on an icon. Use theme to customize icon and badge styling.",
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Badge types",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    heading = "Count badge",
                                    visuals =
                                        element {
                                            LC.IconWithBadge(icon = Icon.ShoppingCart, badge = Count 4)
                                        },
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.IconWithBadge(
    icon = Icon.ShoppingCart,
    badge = Count 4
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    heading = "Text badge",
                                    visuals =
                                        element {
                                            LC.IconWithBadge(icon = Icon.Home, badge = Text "Summer Sale!")
                                        },
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.IconWithBadge(
    icon = Icon.Home,
    badge = Text "Summer Sale!"
)"""
                                        )
                                )
                            }
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            element {
                                LC.IconWithBadge(
                                    icon = Icon.ShoppingCart,
                                    badge = Count 9,
                                    theme = SampleThemes.custom
                                )
                            },
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.IconWithBadge(
    icon = Icon.ShoppingCart,
    badge = Count 9,
    theme = SampleThemes.custom
)""" |]
                                    )
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Theme",
                                        children =
                                            [| LC.Text """
theme = fun theme ->
    { theme with
        IconColor = Color.Black
        IconSize  = 26
        Badge = { theme.Badge with FontSize = 22; FontColor = Color.Black }
    }
""" |]
                                    )
                                }
                            )
                    )
                }
        )
