[<AutoOpen>]
module AppEggShellGallery.Components.Content_ErrorBoundary

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Button

type private Demos =
    [<Component>]
    static member WithoutBoundary (showTheBomb: IStateHook<Set<string>>) : ReactElement =
        element {
            LC.Text "Press the button below to cause an error to be throw in the render method."

            LC.Button(
                label = "The Bomb",
                state =
                    PropStateFactory.MakeLowLevel (
                        Actionable (fun _ -> showTheBomb.update (showTheBomb.current.Add "A"))
                    )
            )

            if showTheBomb.current.Contains "A" then
                LC.TheBomb()
        }

    [<Component>]
    static member WithBoundary (showTheBomb: IStateHook<Set<string>>) : ReactElement =
        LC.ErrorBoundary(
            ``try`` =
                element {
                    LC.Text "This is the try content."
                    LC.Text "Press the button below to cause an error to be throw in the render method."

                    LC.Button(
                        label = "The Bomb",
                        state =
                            PropStateFactory.MakeLowLevel (
                                Actionable (fun _ -> showTheBomb.update (showTheBomb.current.Add "B"))
                            )
                    )

                    if showTheBomb.current.Contains "B" then
                        LC.TheBomb()
                },
            catch =
                fun (error, retry) ->
                    element {
                        LC.Text "We caught an error!"
                        LC.Text $"{error}"

                        LC.Button(
                            label = "Reset",
                            level = Secondary,
                            state =
                                PropStateFactory.MakeLowLevel (
                                    Actionable (fun _ ->
                                        showTheBomb.update (showTheBomb.current.Remove "B")
                                        retry ()
                                    )
                                )
                        )
                    }
        )

type Ui.Content with
    [<Component>]
    static member ErrorBoundary() : ReactElement =
        let showTheBomb = Hooks.useState Set.empty

        Ui.ComponentContent(
            displayName = "ErrorBoundary",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.ErrorBoundary",
            samples =
                element {
                    Ui.ComponentSample(
                        heading = "Without an error boundary (there is one at the top of the AppShell.Content)",
                        visuals = Demos.WithoutBoundary showTheBomb,
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Text "Press the button below to cause an error to be throw in the render method."
LC.Button(
    label = "The Bomb",
    state = PropStateFactory.MakeLowLevel (Actionable (fun _ -> showTheBomb.update (showTheBomb.current.Add "A")))
)
if showTheBomb.current.Contains "A" then
    LC.TheBomb()
"""
                            )
                    )

                    Ui.ComponentSample(
                        heading = "With an error boundary",
                        visuals = Demos.WithBoundary showTheBomb,
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.ErrorBoundary(
    ``try`` = element {
        LC.Text "This is the try content."
        LC.Button(
            label = "The Bomb",
            state = PropStateFactory.MakeLowLevel (Actionable (fun _ -> showTheBomb.update (showTheBomb.current.Add "B")))
        )
        if showTheBomb.current.Contains "B" then
            LC.TheBomb()
    },
    catch = fun (error, retry) ->
        element {
            LC.Text "We caught an error!"
            LC.Text $"{error}"
            LC.Button(
                label = "Reset",
                level = Secondary,
                state = PropStateFactory.MakeLowLevel (Actionable (fun _ -> showTheBomb.update (showTheBomb.current.Remove "B"); retry ()))
            )
        }
)
"""
                            )
                    )
                }
        )
