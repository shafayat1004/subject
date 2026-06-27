[<AutoOpen>]
module AppEggShellGallery.Components.Content_Tag

open Fable.React
open LibClient
open LibClient.Components

let private cautionTheme (theme: LC.Tag.Theme) : LC.Tag.Theme =
    { theme with
        Tags =
            { theme.Tags with
                Selected =
                    { theme.Tags.Selected with
                        TextColor = Color.White
                        BackgroundColor = Color.DevRed
                    }
                Unselected =
                    { theme.Tags.Selected with
                        TextColor = Color.White
                        BackgroundColor = Color.DevOrange
                    }
            }
        Sizes =
            { theme.Sizes with
                Desktop =
                    { theme.Sizes.Desktop with
                        FontSize = 20
                    }
            }
    }

type Ui.Content with
    [<Component>]
    static member Tag () : ReactElement =
        Ui.ComponentContent(
            displayName = "Tag",
            isResponsive = true,
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Tag",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Tag(text = "Sweets", state = LC.Tag.ViewOnly),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """LC.Tag(text = "Sweets", state = LC.Tag.ViewOnly)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            (["Apple"; "Orange"; "Banana"]
                             |> List.map (fun tag -> LC.Tag(text = tag, state = LC.Tag.ViewOnly))
                             |> List.toArray
                             |> castAsElement),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
for tag in ["Apple"; "Orange"; "Banana"] do
    LC.Tag(text = tag, state = LC.Tag.ViewOnly)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Tags(
                                children =
                                    (["Apple"; "Orange"; "Banana"]
                                     |> List.map (fun tag -> LC.Tag(text = tag, state = LC.Tag.ViewOnly))
                                     |> List.toArray)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Tags(
    children = elements {
        for tag in ["Apple"; "Orange"; "Banana"] do
            LC.Tag(text = tag, state = LC.Tag.ViewOnly)
    }
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Tags(
                                children =
                                    elements {
                                        LC.Tag(text = "View Only", state = LC.Tag.ViewOnly)
                                        LC.Tag(text = "Selected", state = LC.Tag.ViewOnly, isSelected = true)
                                        LC.Tag(text = "Actionable", state = LC.Tag.Actionable (fun _ -> Action.alert "You pressed a tag"))
                                        LC.Tag(text = "InProgress", state = LC.Tag.InProgress)
                                        LC.Tag(text = "Disabled", state = LC.Tag.Disabled)
                                    }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Tags(
    children = elements {
        LC.Tag(text = "View Only",  state = LC.Tag.ViewOnly)
        LC.Tag(text = "Selected",   state = LC.Tag.ViewOnly, isSelected = true)
        LC.Tag(text = "Actionable", state = LC.Tag.Actionable (fun _ -> Action.alert "You pressed a tag"))
        LC.Tag(text = "InProgress", state = LC.Tag.InProgress)
        LC.Tag(text = "Disabled",   state = LC.Tag.Disabled)
    }
)"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Tags(
                                children =
                                    elements {
                                        LC.Tag(text = "View Only", theme = cautionTheme, state = LC.Tag.ViewOnly)
                                        LC.Tag(text = "Selected", theme = cautionTheme, state = LC.Tag.ViewOnly, isSelected = true)
                                        LC.Tag(text = "Actionable", theme = cautionTheme, state = LC.Tag.Actionable (fun _ -> Action.alert "You pressed a themed tag"))
                                        LC.Tag(text = "InProgress", theme = cautionTheme, state = LC.Tag.InProgress)
                                        LC.Tag(text = "Disabled", theme = cautionTheme, state = LC.Tag.Disabled)
                                    }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
let cautionTheme (theme: LC.Tag.Theme) : LC.Tag.Theme =
    { theme with
        Tags =
            { theme.Tags with
                Selected = { theme.Tags.Selected with TextColor = Color.White; BackgroundColor = Color.DevRed }
                Unselected = { theme.Tags.Unselected with TextColor = Color.White; BackgroundColor = Color.DevOrange }
            }
    }

LC.Tags(
    children = elements {
        LC.Tag(text = "View Only", theme = cautionTheme, state = LC.Tag.ViewOnly)
        LC.Tag(text = "Selected",   theme = cautionTheme, state = LC.Tag.ViewOnly, isSelected = true)
        ...
    }
)"""
                            )
                    )
                }
        )
