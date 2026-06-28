[<AutoOpen>]
module AppEggShellGallery.Components.Content_Scrim

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let sampleBlock = makeViewStyles { width 300; height 300 }
    let scrim =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

type private Helpers =
    [<Component>]
    static member Sample () : ReactElement =
        let isScrimVisible = Hooks.useState true

        element {
            RX.View(
                styles = [| Styles.sampleBlock |],
                children =
                    elements {
                        LC.Buttons(
                            children =
                                elements {
                                    LC.Button(
                                        label = "Greet",
                                        state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable Actions.greet)
                                    )
                                }
                        )

                        LC.Scrim(
                            isVisible = isScrimVisible.current,
                            onPress = (fun _ -> isScrimVisible.update false),
                            styles = [| Styles.scrim |]
                        )
                    }
            )

            LC.Buttons(
                children =
                    elements {
                        LC.Button(
                            label = "Toggle",
                            state =
                                ButtonHighLevelState.LowLevel(
                                    ButtonLowLevelState.Actionable (fun _ ->
                                        isScrimVisible.update (not isScrimVisible.current))
                                )
                        )
                    }
            )
        }

type Ui.Content with
    [<Component>]
    static member Scrim () : ReactElement =
        Ui.ComponentContent(
            displayName = "Scrim",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Scrim",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = Helpers.Sample(),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
let isScrimVisible = Hooks.useState true

RX.View(
    styles = [| makeViewStyles { width 300; height 300 } |],
    children = elements {
        LC.Buttons(
            children = elements {
                LC.Button(
                    label = "Greet",
                    state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable Actions.greet)
                )
            }
        )
        LC.Scrim(
            isVisible = isScrimVisible.current,
            onPress = (fun _ -> isScrimVisible.update false),
            styles = [| makeViewStyles { Position.Absolute; trbl 0 0 0 0 } |]
        )
    }
)

LC.Button(
    label = "Toggle",
    state = ButtonHighLevelState.LowLevel (
        ButtonLowLevelState.Actionable (fun _ -> isScrimVisible.update (not isScrimVisible.current))
    )
)"""
                            )
                    )
                }
        )
