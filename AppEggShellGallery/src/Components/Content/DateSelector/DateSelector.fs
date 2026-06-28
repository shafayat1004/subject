[<AutoOpen>]
module AppEggShellGallery.Components.Content_DateSelector

open System
open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Styles
open AppEggShellGallery.Colors

type DateOnly = LibClient.Components.DateSelector.DateOnly

module private Styles =
    let specialTheme (theme: LC.DateSelector.Theme) : LC.DateSelector.Theme =
        { theme with
            HeaderBackgroundColor       = Color.Hex "#6200ee"
            SelectedDateBackgroundColor = Color.Hex "#a7aeff"
        }

type Ui.Content with
    [<Component>]
    static member DateSelector() : ReactElement =
        let selectedDatesHook =
            Hooks.useState (
                Map.ofList [
                    ("A", None)
                    ("B", None)
                ]
            )

        let onChange (sampleKey: string) (date: DateOnly) =
            selectedDatesHook.update (
                selectedDatesHook.current.AddOrUpdate (sampleKey, Some date)
            )

        let maybeSelected sampleKey =
            selectedDatesHook.current
            |> Map.tryFind sampleKey
            |> Option.flatten

        Ui.ComponentContent(
            displayName = "DateSelector",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.DateSelector",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.DateSelector",
                    role = "button per selectable day; LC.IconButton for month navigation",
                    namePattern = "Day cells use formatted day label; prev/next month use label prop on IconButton",
                    stateNotes = "selected day exposes Selected; out-of-range days are not pressable",
                    scalesWithFont = true,
                    contrastNotes = "Header, weekday, and selected-day colors meet WCAG AA (theme customizable)"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.DateSelector(
                                onChange = onChange "A",
                                minDate = DateOnly.FromDateTime DateTime.Now,
                                maybeSelected = maybeSelected "A",
                                testId = "gallery-date-selector-a"
                            ),
                        code =
                            ComponentSample.SingleBlock (
                                ComponentSample.Fsharp,
                                LC.Text """
LC.DateSelector(
    onChange = onChange,
    minDate = DateOnly.FromDateTime DateTime.Now,
    maybeSelected = maybeSelected,
    testId = "date-selector"
)
"""
                            )
                    )
                },
            themeSamples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.DateSelector(
                                onChange = onChange "B",
                                maybeSelected = maybeSelected "B",
                                theme = Styles.specialTheme,
                                testId = "gallery-date-selector-b"
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
LC.DateSelector(
    onChange = onChange "B",
    maybeSelected = maybeSelected "B",
    testId = "date-selector-b"
)
                """ |]
                                    )

                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        heading = "Theme",
                                        children =
                                            [| LC.Text """
                    LC.DateSelector(
                        theme = fun theme ->
                            { theme with
                                HeaderBackgroundColor = colors.Primary.Main
                                SelectedDateBackgroundColor = colors.Primary.B100
                            },
                        ...
                    )
                """ |]
                                    )
                                }
                            )
                    )
                }
        )
