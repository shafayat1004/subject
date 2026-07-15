[<AutoOpen>]
module AppEggShellGallery.Components.Content_Buttons

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Button
open AppEggShellGallery.Actions
open AppEggShellGallery.Icons

type Ui.Content with
    [<Component>]
    static member Buttons() : ReactElement =
        let buttonRow (align: Option<LibClient.Components.Buttons.Align>) =
            LC.Buttons(
                ?align = align,
                children =
                    elements {
                        LC.Button(
                            icon  = Left Icon.Home,
                            label = "Home",
                            state = PropStateFactory.MakeLowLevel (Actionable greet)
                        )

                        LC.Button(
                            icon  = Left Icon.Submit,
                            label = "Submit",
                            state = PropStateFactory.MakeLowLevel (Actionable greet)
                        )
                    }
            )

        Ui.ComponentContent(
            displayName = "Buttons",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Buttons",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Buttons",
                    role           = "none (layout container)",
                    namePattern    = "N/A — child LC.Button elements supply names",
                    stateNotes     = "N/A — child buttons expose disabled and busy state",
                    scalesWithFont = false
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = buttonRow (Some LibClient.Components.Buttons.Left),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Buttons(
    align = LibClient.Components.Buttons.Left,
    children = elements {
        LC.Button(
            icon = Left Icon.Home,
            label = "Home",
            state = PropStateFactory.MakeLowLevel (Actionable greet)
        )
        LC.Button(
            icon = Left Icon.Submit,
            label = "Submit",
            state = PropStateFactory.MakeLowLevel (Actionable greet)
        )
    }
)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = buttonRow None,
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Buttons(
    children = elements {
        LC.Button(
            icon = Left Icon.Home,
            label = "Home",
            state = PropStateFactory.MakeLowLevel (Actionable greet)
        )
        LC.Button(
            icon = Left Icon.Submit,
            label = "Submit",
            state = PropStateFactory.MakeLowLevel (Actionable greet)
        )
    }
)
"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = buttonRow (Some LibClient.Components.Buttons.Right),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Buttons(
    align = LibClient.Components.Buttons.Right,
    children = elements {
        LC.Button(
            icon = Left Icon.Home,
            label = "Home",
            state = PropStateFactory.MakeLowLevel (Actionable greet)
        )
        LC.Button(
            icon = Left Icon.Submit,
            label = "Submit",
            state = PropStateFactory.MakeLowLevel (Actionable greet)
        )
    }
)
"""
                            )
                    )
                }
        )
