[<AutoOpen>]
module AppEggShellGallery.Components.Content_Nav_Bottom

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Responsive
open ReactXP.Styles

open AppEggShellGallery.Icons
open AppEggShellGallery.Actions

type Ui.Content.Nav with
    [<Component>]
    static member Bottom () : ReactElement =
        Ui.ComponentContent (
            displayName = "Nav.Bottom",
            isResponsive = true,
            props = ComponentContent.Manual (element {
                Ui.ScrapedComponentProps (heading = "Nav.Bottom.Base",   fullyQualifiedName = "LibClient.Components.Nav.Bottom.Base")
                Ui.ScrapedComponentProps (heading = "Nav.Bottom.Item",   fullyQualifiedName = "LibClient.Components.Nav.Bottom.Item")
                Ui.ScrapedComponentProps (heading = "Nav.Bottom.Button", fullyQualifiedName = "LibClient.Components.Nav.Bottom.Button")
                Ui.ScrapedComponentProps (heading = "Nav.Bottom.Filler", fullyQualifiedName = "LibClient.Components.Nav.Bottom.Filler")
            }),
            samples = (element {
                Ui.ComponentSampleGroup (
                    samples = (element {
                        Ui.ComponentSample (
                            heading = "Basics",
                            layout = ComponentSample.Layout.CodeBelowSamples,
                            visuals = (
                                LC.With.Context (
                                    context = AppEggShellGallery.SampleVisualsScreenSize.sampleVisualsScreenSizeContext,
                                    ``with`` = (fun sampleVisualsScreenSize ->
                                        LC.ForceContext (context = screenSizeContext, value = sampleVisualsScreenSize, children = [|
                                            LC.Nav.Bottom.Base (
                                                desktop = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Design",      state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Components",  state = Nav.Bottom.Item.Selected)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Develop",     state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",        state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass, state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (
                                                        style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3),
                                                        state = Nav.Bottom.Item.Actionable ignore
                                                    )
                                                    LC.Nav.Bottom.Button (
                                                        label = "Cart",
                                                        state = Nav.Bottom.Button.PropStateFactory.MakeLowLevel (Nav.Bottom.Button.Actionable AppEggShellGallery.Actions.greet),
                                                        icon = Nav.Bottom.Button.Icon.Left Icon.ShoppingCart,
                                                        badge = Nav.Bottom.Button.Count 3
                                                    )
                                                },
                                                handheld = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Design",      state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Components",  state = Nav.Bottom.Item.Selected)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Develop",     state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",        state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass, state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (
                                                        style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3),
                                                        state = Nav.Bottom.Item.Actionable ignore
                                                    )
                                                    LC.Nav.Bottom.Button (
                                                        label = "Cart",
                                                        state = Nav.Bottom.Button.PropStateFactory.MakeLowLevel (Nav.Bottom.Button.Actionable AppEggShellGallery.Actions.greet),
                                                        icon = Nav.Bottom.Button.Icon.Left Icon.ShoppingCart,
                                                        badge = Nav.Bottom.Button.Count 3
                                                    )
                                                }
                                            )
                                        |])
                                    )
                                )
                            ),
                            code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
                                            LC.Nav.Bottom.Base (
                                                desktop = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Design",      state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Components",  state = Nav.Bottom.Item.Selected)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Develop",     state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",        state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass, state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (
                                                        style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3),
                                                        state = Nav.Bottom.Item.Actionable ignore
                                                    )
                                                    LC.Nav.Bottom.Button (
                                                        label = "Cart",
                                                        state = Nav.Bottom.Button.PropStateFactory.MakeLowLevel (Nav.Bottom.Button.Actionable AppEggShellGallery.Actions.greet),
                                                        icon = Nav.Bottom.Button.Icon.Left Icon.ShoppingCart,
                                                        badge = Nav.Bottom.Button.Count 3
                                                    )
                                                },
                                                handheld = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Design",      state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Components",  state = Nav.Bottom.Item.Selected)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Develop",     state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",        state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass, state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (
                                                        style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3),
                                                        state = Nav.Bottom.Item.Actionable ignore
                                                    )
                                                    LC.Nav.Bottom.Button (
                                                        label = "Cart",
                                                        state = Nav.Bottom.Button.PropStateFactory.MakeLowLevel (Nav.Bottom.Button.Actionable AppEggShellGallery.Actions.greet),
                                                        icon = Nav.Bottom.Button.Icon.Left Icon.ShoppingCart,
                                                        badge = Nav.Bottom.Button.Count 3
                                                    )
                                                }
                                            )
                            """)
                        )
                    })
                )

                Ui.ComponentSampleGroup (
                    heading = "Style Sample",
                    samples = (element {
                        Ui.ComponentSample (
                            layout = ComponentSample.Layout.CodeBelowSamples,
                            visuals = (
                                LC.With.Context (
                                    context = AppEggShellGallery.SampleVisualsScreenSize.sampleVisualsScreenSizeContext,
                                    ``with`` = (fun sampleVisualsScreenSize ->
                                        LC.ForceContext (context = screenSizeContext, value = sampleVisualsScreenSize, children = [|
                                            LC.Nav.Bottom.Base (
                                                desktop = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Store", badge = Nav.Bottom.Item.Text "Summer Sale"), state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (icon = Icon.Bell, badge = Nav.Bottom.Item.Count 2),               state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",                                                                          state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass,                                                                state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3), state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.X, state = Nav.Bottom.Item.Actionable ignore, theme = (fun t -> { t with IconVerticalAdjust = 10 }))
                                                },
                                                handheld = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Store", badge = Nav.Bottom.Item.Text "Summer Sale"), state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (icon = Icon.Bell, badge = Nav.Bottom.Item.Count 2),               state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",                                                                          state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass,                                                                state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3), state = Nav.Bottom.Item.Actionable ignore)
                                                }
                                            )
                                        |])
                                    )
                                )
                            ),
                            code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
                                            LC.Nav.Bottom.Base (
                                                desktop = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Store", badge = Nav.Bottom.Item.Text "Summer Sale"), state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (icon = Icon.Bell, badge = Nav.Bottom.Item.Count 2),               state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",                                                                          state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass,                                                                state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3), state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.X, state = Nav.Bottom.Item.Actionable ignore, theme = (fun t -> { t with IconVerticalAdjust = 10 }))
                                                },
                                                handheld = fun _ -> element {
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Store", badge = Nav.Bottom.Item.Text "Summer Sale"), state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (icon = Icon.Bell, badge = Nav.Bottom.Item.Count 2),               state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.labelOnly "Blog",                                                                          state = Nav.Bottom.Item.Disabled)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.iconOnly Icon.MagnifyingGlass,                                                                state = Nav.Bottom.Item.Actionable ignore)
                                                    LC.Nav.Bottom.Item (style = Nav.Bottom.Item.Style.With (label = "Cart", icon = Icon.ShoppingCart, badge = Nav.Bottom.Item.Count 3), state = Nav.Bottom.Item.Actionable ignore)
                                                }
                                            )

                                            // Per-icon vertical adjustment: set IconVerticalAdjust on Nav.Bottom.Item theme
                                            // (legacy class cascade "adjust-icon" ==> ItemStyles.Theme.IconVerticalPositionAdjustment)
                            """)
                        )
                    })
                )
            })
        )
