[<AutoOpen>]
module AppEggShellGallery.Components.Content_ThirdParty_ToolWindowContent

open Fable.React
open LibClient
open LibClient.Components
open ThirdParty.Map
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let content = makeViewStyles { FlexDirection.Column }

type Ui.Content.ThirdParty with
    [<Component>]
    static member ToolWindowContent (handle: InfoWindowHandle) : ReactElement =
        RX.View(
            styles = [| Styles.content |],
            children = [|
                LC.Heading(level = Heading.Secondary, children = [| LC.UiText "Info Window" |])
                LC.UiText "This is an example of an info window."
                LC.Button(
                    label = "Close",
                    state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable (fun _ -> handle.Close()))
                )
            |]
        )
