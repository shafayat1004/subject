[<AutoOpen>]
module AppEggShellGallery.Components.Content_Dialogs

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.LocalImages
open AppEggShellGallery.Navigation

let private imageSources =
    [1..4]
    |> List.map (fun i -> $"/images/wlop%i{i}.jpg")
    |> List.map localImage

type Ui.Content with
    [<Component>]
    static member Dialogs () : ReactElement =
        Ui.ComponentContent(
            displayName = "Dialogs",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Dialog.* (Alert, Confirm, ImageViewer, …)",
                    role           = "dialog on modal shells",
                    namePattern    = "accessibilityLabel from heading + details on Confirm/Alert; action buttons use visible label",
                    stateNotes     = "Confirm shell cycles InProgress/Error modes; dismiss via history back where allowed",
                    scalesWithFont = true,
                    contrastNotes  = "Dialog body text and button pairs meet WCAG AA"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Button(
                                label = "Alert",
                                state =
                                    ButtonHighLevelStateFactory.MakeGo(
                                        Alert (None, "Something happened and you should probably do something about it."),
                                        nav
                                    )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Button(
    label = "Alert",
    state = ButtonHighLevelStateFactory.MakeGo(
        Alert (None, "Something happened and you should probably do something about it."),
        nav
    )
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Button(
                                label = "Confirm",
                                state =
                                    ButtonHighLevelStateFactory.MakeGo(
                                        Confirm ((Some "Confirm"), "Okay to delete all the things?", "No", "Yes", ignore),
                                        nav
                                    )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Button(
    label = "Confirm",
    state = ButtonHighLevelStateFactory.MakeGo(
        Confirm ((Some "Confirm"), "Okay to delete all the things?", "No", "Yes", ignore),
        nav
    )
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Button(
                                label = "Custom Confirm",
                                state =
                                    ButtonHighLevelStateFactory.MakeGo(
                                        ConfirmCustom (
                                            (Some "Confirm"),
                                            "Send the dark forest deterrence broadcast?",
                                            [
                                                LibClient.Components.Dialog.Confirm.Cancel ("No", LibClient.Components.Button.Level.Primary, ignore)
                                                LibClient.Components.Dialog.Confirm.Confirm ("Yes", LibClient.Components.Button.Secondary, ignore)
                                            ]
                                        ),
                                        nav
                                    )
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Button(
    label = "Custom Confirm",
    state = ButtonHighLevelStateFactory.MakeGo(
        ConfirmCustom (
            (Some "Confirm"),
            "Send the dark forest deterrence broadcast?",
            [
                Dialog.Confirm.Cancel ("No", Button.Level.Primary, ignore)
                Dialog.Confirm.Confirm ("Yes", Button.Secondary, ignore)
            ]
        ),
        nav
    )
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Button(
                                label = "Image Viewer",
                                state = ButtonHighLevelStateFactory.MakeGo (ImageViewer (imageSources, 0u), nav)
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Button(
    label = "Image Viewer",
    state = ButtonHighLevelStateFactory.MakeGo (ImageViewer (imageSources, 0u), nav)
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = LC.Text "TODO add contents about WhiteRounded, WhiteRoundedStandard, FullScreen and other dialogs here",
                        code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text "")
                    )
                }
        )
