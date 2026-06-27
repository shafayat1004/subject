[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_File

open Fable.React
open LibClient
open LibClient.Components
open LibLifeCycleTypes.File

type private Helpers =
    [<Component>]
    static member Sample(?maxFileCount: Positive.PositiveInteger, ?maxFileSize: int<MB>) : ReactElement =
        let state = Hooks.useState (Ok [])
        LC.Input.File(
            onChange = state.update,
            value =
                (state.current
                 |> Result.recover (fun _ -> [])),
            validity =
                (state.current
                 |> Result.map (fun _ -> Valid)
                 |> Result.recover (fun m -> Invalid m)),
            ?maxFileCount = maxFileCount,
            ?maxFileSize =
                (maxFileSize
                 |> Option.map (fun size -> mBToKB size))
        )

type Ui.Content.Input with
    [<Component>]
    static member File () : ReactElement =
        Ui.ComponentContent (
            displayName = "Input.File",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.File",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = Helpers.Sample(),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Input.File(
    onChange = onChange,
    value    = files,
    validity = validity
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = Helpers.Sample(maxFileCount = PositiveInteger.ofLiteral 1, maxFileSize = 5<MB>),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Input.File(
    onChange     = onChange,
    maxFileCount = PositiveInteger.ofLiteral 1,
    maxFileSize  = mBToKB 5<MB>
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = Helpers.Sample(maxFileCount = PositiveInteger.ofLiteral 4, maxFileSize = 5<MB>),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Input.File(
    onChange     = onChange,
    maxFileCount = PositiveInteger.ofLiteral 4,
    maxFileSize  = mBToKB 5<MB>
)"""
                            )
                    )
                }
        )
