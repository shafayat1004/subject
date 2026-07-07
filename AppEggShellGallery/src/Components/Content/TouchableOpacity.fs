[<AutoOpen>]
module AppEggShellGallery.Components.Content_TouchableOpacity

open Fable.React
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view = makeViewStyles {
        backgroundColor (Color.Grey "cc")
        size 300 200
    }

type private Helpers =
    [<Component>]
    static member BasicSample() : ReactElement =
        let isPressed = Hooks.useState false

        element {
            LC.TouchableOpacity(
                onPress = (fun _ -> isPressed.update true),
                testId = "touchable-opacity-click-me",
                children = [|
                    Rn.View(
                        styles = [| Styles.view |],
                        children = [| Rn.Text "Click Me" |]
                    )
                |]
            )

            if isPressed.current then
                LC.Text "Pressed!"
        }

    [<Component>]
    static member CustomLabelSample() : ReactElement =
        LC.TouchableOpacity(
            onPress = (fun _ -> ()),
            label = "Submit order",
            testId = "touchable-opacity-submit-order",
            children = [|
                Rn.View(
                    styles = [| Styles.view |],
                    children = [| Rn.Text "Place Order" |]
                )
            |]
        )

type Ui.Content with
    [<Component>]
    static member TouchableOpacity() : ReactElement =
        Ui.ComponentContent(
            displayName = "TouchableOpacity",
            props =
                ComponentContent.Manual(
                    Ui.ComponentProps(
                        data = {
                            Fields =
                                (Choice2Of2 [
                                    {
                                        Name = "onPress"
                                        Type = "ReactEvent.Action -> unit"
                                        Default = None
                                        Description = None
                                    }
                                    {
                                        Name = "children"
                                        Type = "ReactElements"
                                        Default = None
                                        Description = None
                                    }
                                    {
                                        Name = "label"
                                        Type = "string"
                                        Default = Some "\"Button\""
                                        Description = Some "Accessibility label for the overlay Pressable"
                                    }
                                    {
                                        Name = "pointerState"
                                        Type = "LC.Pointer.State.PointerState"
                                        Default = Some "None"
                                        Description = None
                                    }
                                ])
                            MaybeScrapeErrors = None
                        }
                    )
                ),
            notes =
                LC.Text "TouchableOpacity renders children and adds an overlay LC.Pressable on top (overlay = true) so the entire region is tappable while children remain visible underneath.",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.TouchableOpacity",
                    role = "button",
                    namePattern = "?label prop (defaults to \"Button\")",
                    stateNotes = "N/A — no disabled or busy states",
                    scalesWithFont = false
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        samples =
                            element {
                                Ui.ComponentSample(
                                    heading = "Basic",
                                    visuals = Helpers.BasicSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let isPressed = Hooks.useState false

LC.TouchableOpacity(
    onPress = (fun _ -> isPressed.update true),
    children = [|
        Rn.View(
            styles = [| Styles.view |],
            children = [| Rn.Text "Click Me" |]
        )
    |]
)

if isPressed.current then LC.Text "Pressed!"
"""
                                        )
                                )

                                Ui.ComponentSample(
                                    heading = "Custom label",
                                    visuals = Helpers.CustomLabelSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.TouchableOpacity(
    onPress = (fun _ -> ()),
    label = "Submit order",
    children = [|
        Rn.View(
            styles = [| Styles.view |],
            children = [| Rn.Text "Place Order" |]
        )
    |]
)
"""
                                        )
                                )
                            }
                    )
                }
        )
