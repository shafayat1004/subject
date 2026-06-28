[<AutoOpen>]
module AppEggShellGallery.Components.Content_Tabs

open Fable.React
open LibClient
open LibClient.Components

type private TabItem =
| Home
| Profile
| Contact

module private Styles =
    let specialTheme (theme: LibClient.Components.Tabs.Theme) : LibClient.Components.Tabs.Theme =
        { theme with
            BackgroundColor = Color.Hex "#e8eaf6"
            BorderColor = Color.Hex "#6200ee"
        }

type private Helpers =
    [<Component>]
    static member BasicsSample() : ReactElement =
        let selectedTab = Hooks.useState Home

        element {
            LC.Tabs(
                label = "Section tabs",
                children = [|
                    LC.Tab(
                        label = "Home",
                        state =
                            (if selectedTab.current = Home then
                                 LC.Tab.Selected
                             else
                                 LC.Tab.Unselected(fun _ -> selectedTab.update Home))
                    )
                    LC.Tab(
                        label = "Profile",
                        state =
                            (if selectedTab.current = Profile then
                                 LC.Tab.Selected
                             else
                                 LC.Tab.Unselected(fun _ -> selectedTab.update Profile))
                    )
                    LC.Tab(
                        label = "Contact",
                        state =
                            (if selectedTab.current = Contact then
                                 LC.Tab.Selected
                             else
                                 LC.Tab.Unselected(fun _ -> selectedTab.update Contact))
                    )
                |]
            )

            match selectedTab.current with
            | Home -> LC.UiText "This is the HOME tab"
            | Profile -> LC.UiText "This is the PROFILE tab"
            | Contact -> LC.UiText "This is the CONTACT tab"
        }

    [<Component>]
    static member ManyTabsSample() : ReactElement =
        let selectedTab = Hooks.useState 0

        let labels =
            [|
                "Overview"
                "Details"
                "Settings"
                "History"
                "Reports"
                "Analytics"
                "Help"
            |]

        element {
            LC.Tabs(
                label = "Section tabs",
                children =
                    (labels
                     |> Array.mapi (fun i label ->
                         LC.Tab(
                             label = label,
                             state =
                                 (if selectedTab.current = i then
                                      LC.Tab.Selected
                                  else
                                      LC.Tab.Unselected(fun _ -> selectedTab.update i))
                         )))
            )

            LC.UiText (sprintf "Selected: %s" labels.[selectedTab.current])
        }

type Ui.Content with
    [<Component>]
    static member Tabs() : ReactElement =
        Ui.ComponentContent(
            displayName = "Tabs",
            isResponsive = true,
            props =
                ComponentContent.Manual(
                    element {
                        Ui.ScrapedComponentProps(
                            heading = "Tabs",
                            fullyQualifiedName = "LibClient.Components.Tabs"
                        )

                        Ui.ScrapedComponentProps(
                            heading = "Tab",
                            fullyQualifiedName = "LibClient.Components.Tab"
                        )
                    }
                ),
            notes =
                element {
                    LC.Text "Use LC.Tab.Selected for the active tab and LC.Tab.Unselected with an onPress handler for inactive tabs. Only unselected tabs render a Pressable overlay. Each tab gets testId via A11ySlug (e.g. tab-home). Pass label on LC.Tabs to name the tablist for screen readers."
                },
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Tabs / LC.Tab",
                    role = "tablist (LC.Tabs); tab (LC.Tab)",
                    namePattern = "Tab label text; tablist named via label prop on LC.Tabs",
                    stateNotes = "selected tab exposes selected state; inactive tabs are pressable",
                    scalesWithFont = true,
                    contrastNotes = "Active tab underline and text use theme colors meeting WCAG AA"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.BasicsSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
type TabItem = Home | Profile | Contact

let selectedTab = Hooks.useState Home

LC.Tabs(
    label = "Section tabs",
    children = [|
        LC.Tab(label = "Home",    state = (if selectedTab.current = Home    then LC.Tab.Selected else LC.Tab.Unselected (fun _ -> selectedTab.update Home)))
        LC.Tab(label = "Profile", state = (if selectedTab.current = Profile then LC.Tab.Selected else LC.Tab.Unselected (fun _ -> selectedTab.update Profile)))
        LC.Tab(label = "Contact", state = (if selectedTab.current = Contact then LC.Tab.Selected else LC.Tab.Unselected (fun _ -> selectedTab.update Contact)))
    |]
)
match selectedTab.current with
| Home    -> LC.UiText "This is the HOME tab"
| Profile -> LC.UiText "This is the PROFILE tab"
| Contact -> LC.UiText "This is the CONTACT tab"
"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Many tabs",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.ManyTabsSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let selectedTab = Hooks.useState 0
let labels = [| "Overview"; "Details"; "Settings"; "History"; "Reports"; "Analytics"; "Help" |]

LC.Tabs(
    label = "Section tabs",
    children =
        labels
        |> Array.mapi (fun i label ->
            LC.Tab(
                label = label,
                state = (if selectedTab.current = i then LC.Tab.Selected else LC.Tab.Unselected (fun _ -> selectedTab.update i))
            )
        )
)
"""
                                        )
                                )
                            }
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Tabs(
                                label = "Section tabs",
                                theme = Styles.specialTheme,
                                children = [|
                                    LC.Tab(label = "Active", state = LC.Tab.Selected)
                                    LC.Tab(label = "Inactive", state = LC.Tab.Unselected ignore)
                                    LC.Tab(label = "Another", state = LC.Tab.Unselected ignore)
                                |]
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.Tabs(
    theme = Styles.specialTheme,
    children = [|
        LC.Tab(label = "Active", state = LC.Tab.Selected)
        LC.Tab(label = "Inactive", state = LC.Tab.Unselected ignore)
        LC.Tab(label = "Another", state = LC.Tab.Unselected ignore)
    |]
)
""" |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Theme",
                                        children =
                                            [| LC.Text """
LC.Tabs(
    theme = fun theme ->
        { theme with
            BackgroundColor = Color.Hex "#e8eaf6"
            BorderColor = Color.Hex "#6200ee"
        },
    ...
)
""" |]
                                    )
                                }
                            )
                    )
                }
        )
