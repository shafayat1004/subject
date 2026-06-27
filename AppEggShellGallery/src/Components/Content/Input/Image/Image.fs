[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Image

open Fable.React
open LibClient
open LibClient.Components
open LibLifeCycleTypes.File

type private Helpers =
    [<Component>]
    static member Sample() : ReactElement =
        let state = Hooks.useState (Ok [])
        LC.Input.Image(
            onChange = state.update,
            value =
                (state.current
                 |> Result.recover (fun _ -> [])),
            validity =
                (state.current
                 |> Result.map (fun _ -> Valid)
                 |> Result.recover (fun m -> Invalid m))
        )

type Ui.Content.Input with
    [<Component>]
    static member Image () : ReactElement =
        Ui.ComponentContent (
            displayName = "Input.Image",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Image",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = Helpers.Sample(),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Input.Image(
    onChange = onChange,
    value    = files,
    validity = validity
)"""
                            )
                    )
                }
        )
