[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Duration

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input.Duration

type private Helpers =
    [<Component>]
    static member Sample(validity: InputValidity) : ReactElement =
        let value = Hooks.useState empty
        LC.Input.Duration(
            label    = "Duration",
            value    = value.current,
            validity = validity,
            onChange = value.update
        )

type Ui.Content.Input with
    [<Component>]
    static member Duration () : ReactElement =
        Ui.ComponentContent (
            displayName = "Input.Duration",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.Duration",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        notes = LC.Text "Duration input uses separate numeric fields for hours and minutes. Hours must be between 0 and 23; minutes between 0 and 59. Pass shouldDisplayDays to add an optional days field. A complete duration is available in Value.Result only when all required fields are filled and valid.",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.Duration(
    label    = "Duration",
    value    = value,
    validity = Valid,
    onChange = setValue
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Validation (hardcoded, not dynamic)",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Missing",
                                    visuals = LC.Input.Duration(
                                        label    = "Duration",
                                        value    = empty,
                                        validity = Missing,
                                        onChange = ignore
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.Duration(
    label    = "Duration",
    value    = Duration.empty,
    validity = Missing,
    onChange = ignore
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Invalid with message",
                                    visuals = LC.Input.Duration(
                                        label    = "Duration",
                                        value    = empty,
                                        validity = Invalid "That is just a bad duration",
                                        onChange = ignore
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.Duration(
    label    = "Duration",
    value    = Duration.empty,
    validity = Invalid "That is just a bad duration",
    onChange = ignore
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
