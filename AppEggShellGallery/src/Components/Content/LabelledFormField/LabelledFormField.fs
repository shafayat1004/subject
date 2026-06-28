[<AutoOpen>]
module AppEggShellGallery.Components.Content_LabelledFormField

open Fable.React
open LibClient
open LibClient.Components

module private Styles =
    let specialTheme (theme: LC.LabelledFormField.Theme) : LC.LabelledFormField.Theme =
        { theme with
            LabelWidth = 120
            LabelColor = Color.DevBlue
        }

type private Helpers =
    [<Component>]
    static member EmailSample() : ReactElement =
        let email = Hooks.useState None

        LC.LabelledFormField(
            label = "Email",
            testId = "gallery-labelled-form-field-email",
            children =
                elements {
                    LC.Input.Text(
                        value = email.current,
                        onChange = email.update,
                        validity = InputValidity.Valid
                    )
                }
        )

    [<Component>]
    static member InvalidSample() : ReactElement =
        let email = Hooks.useState (Some (NonemptyString.ofLiteral "not-an-email"))

        LC.LabelledFormField(
            label = "Email",
            testId = "gallery-labelled-form-field-invalid",
            children =
                elements {
                    LC.Input.Text(
                        value = email.current,
                        onChange = email.update,
                        validity = InputValidity.Invalid "Please enter a valid email address"
                    )
                }
        )

    [<Component>]
    static member ThemedSample() : ReactElement =
        let name = Hooks.useState None

        LC.LabelledFormField(
            label = "Full name",
            theme = Styles.specialTheme,
            testId = "gallery-labelled-form-field-themed",
            children =
                elements {
                    LC.Input.Text(
                        value = name.current,
                        onChange = name.update,
                        validity = InputValidity.Valid
                    )
                }
        )

type Ui.Content with
    [<Component>]
    static member LabelledFormField() : ReactElement =
        Ui.ComponentContent(
            displayName = "LabelledFormField",
            props =
                ComponentContent.ForFullyQualifiedName
                    "LibClient.Components.LabelledFormField",
            notes = LC.Text "LabelledFormField lays out a label beside (desktop) or above (handheld) a form control. Pass theme to customize label width and color.",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.LabelledFormField",
                    role = "none (layout); label view exposes accessibilityLabel",
                    namePattern = "label prop on field; child input supplies control name and role",
                    stateNotes = "Invalid state comes from child input validity",
                    scalesWithFont = true,
                    contrastNotes = "Themed label color meets WCAG AA on typical backgrounds"
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.EmailSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let email = Hooks.useState None

LC.LabelledFormField(
    label = "Email",
    testId = "gallery-labelled-form-field-email",
    children = elements {
        LC.Input.Text(
            value = email.current,
            onChange = email.update,
            validity = InputValidity.Valid
        )
    }
)
"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Validation",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.InvalidSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.LabelledFormField(
    label = "Email",
    children = elements {
        LC.Input.Text(
            value = email.current,
            onChange = email.update,
            validity = InputValidity.Invalid "Please enter a valid email address"
        )
    }
)
"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Optional",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.ThemedSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
LC.LabelledFormField(
    label = "Full name",
    theme = fun theme -> { theme with LabelWidth = 120; LabelColor = Color.DevBlue },
    children = elements {
        LC.Input.Text(
            value = name.current,
            onChange = name.update,
            validity = InputValidity.Valid
        )
    }
)
"""
                                        )
                                )
                            }
                    )
                }
        )
