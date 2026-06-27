[<AutoOpen>]
module AppEggShellGallery.Components.Content_LabelledFormField

open Fable.React
open LibClient
open LibClient.Components

type Ui.Content with
    [<Component>]
    static member LabelledFormField() : ReactElement =
        Ui.ComponentContent(
            displayName = "LabelledFormField",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.LabelledFormField",
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.LabelledFormField(
                                label = "Email",
                                testId = "gallery-labelled-form-field",
                                children =
                                    elements {
                                        LC.Input.Text(
                                            value = None,
                                            onChange = (fun _ -> ()),
                                            validity = InputValidity.Valid
                                        )
                                    }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Render,
                                LC.Text """
                    <LC.LabelledFormField Label='"Email"'>
                        <LC.Input.Text
                         Value='None'
                         OnChange='ignore'
                         Validity='Valid'/>
                    </LC.LabelledFormField>
            """
                            )
                    )
                }
        )
