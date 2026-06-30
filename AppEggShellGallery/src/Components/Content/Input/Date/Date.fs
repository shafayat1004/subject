[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_Date

open System
open Fable.React
open LibClient
open LibClient.Components
open LC.Input.DateTypes

let private canSelectDate (date: DateOnly) =
    date.DayOfWeek <> DayOfWeek.Saturday && date.DayOfWeek <> DayOfWeek.Sunday

let private todayValue () =
    DateTimeOffset.UtcNow.ToString("dd/MM/yyyy")
    |> NonemptyString.ofString
    |> parse

type private Helpers =
    [<Component>]
    static member BasicSample() : ReactElement =
        let value = Hooks.useState (todayValue())

        LC.Input.Date(
            label               = "Date",
            value               = value.current,
            onChange            = value.update,
            validity            = Valid,
            requestFocusOnMount = true
        )

    [<Component>]
    static member RestrictedSample() : ReactElement =
        let value = Hooks.useState (todayValue())

        LC.Input.Date(
            value           = value.current,
            onChange        = value.update,
            minDate         = DateOnly.FromDateTime(DateTime.Now).AddMonths(-3),
            maxDate         = DateOnly.FromDateTime(DateTime.Now).AddMonths(1),
            canSelectDate   = canSelectDate,
            validity        = Valid
        )

type Ui.Content.Input with
    [<Component>]
    static member Date() : ReactElement =
        Ui.ComponentContent(
            displayName = "Input.Date",
            props       = ComponentContent.ForFullyQualifiedName "LC.Input.Date",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Input.Date",
                    role = "text field with date picker",
                    namePattern = "Floating label text",
                    stateNotes = "Invalid/Missing validity surfaces error text below the field",
                    scalesWithFont = true,
                    contrastNotes = "Label, input text, and error colors meet WCAG AA"
                ),
            samples     =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.BasicSample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Date(
    label               = "Date",
    value               = value,
    onChange            = setValue,
    validity            = Valid,
    requestFocusOnMount = true
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Date Restrictions",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.RestrictedSample(),
                                    code    =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.Input.Date(
    value         = value,
    onChange      = setValue,
    minDate       = DateOnly.FromDateTime(DateTime.Now).AddMonths(-3),
    maxDate       = DateOnly.FromDateTime(DateTime.Now).AddMonths(1),
    canSelectDate = fun date ->
        date.DayOfWeek <> DayOfWeek.Saturday && date.DayOfWeek <> DayOfWeek.Sunday,
    validity      = Valid
)"""
                                        )
                                )
                            }
                    )
                }
        )
