[<AutoOpen>]
module AppEggShellGallery.Components.Content_ThirdParty_ImagePicker

open Fable.React
open LibClient
open LibClient.Components
open LibLifeCycleTypes.File
open ThirdParty.ImagePicker.Components.Constructors
open ThirdParty.ImagePicker.Components.Base

type private Helpers =
    [<Component>]
    static member Sample(?maxFileCount: Positive.PositiveInteger, ?maxFileSize: int<MB>) : ReactElement =
        let state = Hooks.useState (Ok [])
        ThirdParty.ImagePicker.Components.Constructors.ImagePicker.Base(
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

type Ui.Content.ThirdParty with
    [<Component>]
    static member ImagePicker () : ReactElement =
        Ui.ComponentContent(
            displayName = "ImagePicker",
            props = ComponentContent.ForFullyQualifiedName "ThirdParty.ImagePicker.Components.Base",
            notes =
                LC.Text
                    "ImagePicker wraps LC.Input.Image on web and a native picker on mobile. Same value/validity/onChange contract as Input.File.",
            a11y =
                Ui.A11yPanel(
                    componentName = "ImagePicker",
                    role = "button (file picker) with image preview",
                    namePattern = "Inherits LC.Input.Image label pattern",
                    stateNotes = "Invalid/Missing validity surfaces error text; selected files shown as thumbnails",
                    scalesWithFont = true,
                    contrastNotes = "Label, button text, and error colors meet WCAG AA"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = Helpers.Sample(),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
ImagePicker.Base(
    onChange = onChange,
    value    = files,
    validity = validity
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals = Helpers.Sample(maxFileCount = PositiveInteger.ofLiteral 1, maxFileSize = 5<MB>),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
ImagePicker.Base(
    onChange     = onChange,
    maxFileCount = PositiveInteger.ofLiteral 1,
    maxFileSize  = mBToKB 5<MB>
)"""
                            )
                    )
                }
        )
