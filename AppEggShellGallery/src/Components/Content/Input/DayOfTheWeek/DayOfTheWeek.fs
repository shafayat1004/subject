[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_DayOfTheWeek

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input.DayOfTheWeek

type private Helpers =
    [<Component>]
    static member MaybeOneSample(validity: InputValidity) : ReactElement =
        let value = Hooks.useState<Option<DayOfTheWeek>> None
        LC.Input.DayOfTheWeek(
            label    = "Select at most one day",
            mode     = MaybeOne (value.current, value.update),
            validity = validity
        )

    [<Component>]
    static member OneSample(validity: InputValidity) : ReactElement =
        let value = Hooks.useState<Option<DayOfTheWeek>> None
        LC.Input.DayOfTheWeek(
            label    = "Select exactly one day",
            mode     = One (value.current, fun day -> value.update (Some day)),
            validity = validity
        )

    [<Component>]
    static member SetSample(validity: InputValidity) : ReactElement =
        let value = Hooks.useState<Set<DayOfTheWeek>> Set.empty
        LC.Input.DayOfTheWeek(
            label    = "Select some days",
            mode     = Set (value.current, value.update),
            validity = validity
        )

type Ui.Content.Input with
    [<Component>]
    static member DayOfTheWeek() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.DayOfTheWeek",
            props       = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.DayOfTheWeek",
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Input.DayOfTheWeek",
                    role           = "group of checkboxes or radio buttons",
                    namePattern    = "label prop names the group; each day button labeled by day name",
                    stateNotes     = "Selected days expose checked/selected state; Invalid/Missing validity surfaces error text",
                    scalesWithFont = true,
                    contrastNotes  = "Day labels and selection highlight meet WCAG AA"
                ),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Modes",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.MaybeOneSample(validity = Valid),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.DayOfTheWeek(
    label    = "Select at most one day",
    mode     = MaybeOne (value, setValue),
    validity = Valid
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.OneSample(validity = Valid),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.DayOfTheWeek(
    label    = "Select exactly one day",
    mode     = One (value, setValue),
    validity = Valid
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.SetSample(validity = Valid),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.DayOfTheWeek(
    label    = "Select some days",
    mode     = Set (value, setValue),
    validity = Valid
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
                                    visuals = LC.Input.DayOfTheWeek(
                                        label    = "Select some days",
                                        mode     = Set (Set.empty, ignore),
                                        validity = Missing
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.DayOfTheWeek(
    label    = "Select some days",
    mode     = Set (Set.empty, ignore),
    validity = Missing
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "No label",
                                    visuals = LC.Input.DayOfTheWeek(
                                        mode     = Set (Set.empty, ignore),
                                        validity = Missing
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
// No label — shows "Required" placeholder when Missing
LC.Input.DayOfTheWeek(
    mode     = Set (Set.empty, ignore),
    validity = Missing
)""")
                                )

                                Ui.ComponentSample(
                                    visuals = LC.Input.DayOfTheWeek(
                                        label    = "Select some days",
                                        mode     = Set (Set.empty, ignore),
                                        validity = Invalid "You chose poorly!"
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.DayOfTheWeek(
    label    = "Select some days",
    mode     = Set (Set.empty, ignore),
    validity = Invalid "You chose poorly!"
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "No label",
                                    visuals = LC.Input.DayOfTheWeek(
                                        mode     = Set (Set.empty, ignore),
                                        validity = Invalid "You chose poorly!"
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
// No label — invalid message shown below days
LC.Input.DayOfTheWeek(
    mode     = Set (Set.empty, ignore),
    validity = Invalid "You chose poorly!"
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
