[<AutoOpen>]
module AppEggShellGallery.Components.Content_Tabs

open Fable.React
open LibClient
open LibClient.Components

type private TabItem =
    | Home
    | Profile
    | Contact

type private Helpers =
    [<Component>]
    static member Sample() : ReactElement =
        let selectedTab = Hooks.useState Home
        element {
            LC.Tabs(
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
        }

type Ui.Content with
    [<Component>]
    static member Tabs() : ReactElement =
        Ui.ComponentContent(
            displayName = "Tabs",
            props = ComponentContent.Manual (element {
                Ui.ScrapedComponentProps(heading = "Tabs", fullyQualifiedName = "LibClient.Components.Tabs")
                Ui.ScrapedComponentProps(heading = "Tab",  fullyQualifiedName = "LibClient.Components.Tab")
            }),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
type TabItem = Home | Profile | Contact

// In your component:
let selectedTab = Hooks.useState Home

LC.Tabs(
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
""")
                                )
                            }
                        )
                    )
                }
            )
        )
