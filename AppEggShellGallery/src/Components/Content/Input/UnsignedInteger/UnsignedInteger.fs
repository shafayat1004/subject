[<AutoOpen>]
module AppEggShellGallery.Components.Content_Input_UnsignedInteger

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Input.UnsignedInteger

type private Helpers =
    [<Component>]
    static member Sample(initialText: string, validity: InputValidity) : ReactElement =
        let value = Hooks.useState (parse (NonemptyString.ofString initialText))
        LC.Input.UnsignedInteger(
            label    = "Quantity",
            value    = value.current,
            validity = validity,
            onChange = value.update
        )

type Ui.Content.Input with
    [<Component>]
    static member UnsignedInteger () : ReactElement =
        Ui.ComponentContent (
            displayName = "Input.UnsignedInteger",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Input.UnsignedInteger",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = Helpers.Sample(initialText = "42", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.UnsignedInteger(
    label    = "Quantity",
    value    = value,
    validity = Valid,
    onChange = setValue
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Internal Validation",
                        notes = LC.Text "This component performs internal validation of the input, which has higher precedence for display than externally supplied validation. e.g. if the input is non-numeric and the caller is also passing InputValidity.Missing, then it's the non-numeric error message that'll be displayed. Note that an empty field is not considered internally invalid — the component returns Ok None so the caller can supply Missing on form submission.",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Non-numeric input",
                                    visuals = Helpers.Sample(initialText = "thirteen", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// "thirteen" -> internal error: Only numbers allowed""")
                                )

                                Ui.ComponentSample(
                                    heading = "Negative number",
                                    visuals = Helpers.Sample(initialText = "-13", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// "-13" -> internal error: Only numbers allowed""")
                                )

                                Ui.ComponentSample(
                                    heading = "Empty field (no error)",
                                    visuals = Helpers.Sample(initialText = "", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// empty -> Ok None (no internal error)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "External Validation",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Missing",
                                    visuals = Helpers.Sample(initialText = "12", validity = Missing),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.UnsignedInteger(
    label    = "Quantity",
    value    = value,
    validity = Missing,
    onChange = setValue
)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Invalid with message",
                                    visuals = Helpers.Sample(initialText = "11", validity = Invalid "This input is just bad"),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Input.UnsignedInteger(
    label    = "Quantity",
    value    = value,
    validity = Invalid "This input is just bad",
    onChange = setValue
)""")
                                )
                            }
                        )
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Edge Cases",
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Zero (valid unsigned)",
                                    visuals = Helpers.Sample(initialText = "0", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// "0" -> Ok (Some 0) — zero is a valid unsigned integer""")
                                )

                                Ui.ComponentSample(
                                    heading = "Negative zero",
                                    visuals = Helpers.Sample(initialText = "-0", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// "-0" -> internal error: Only numbers allowed (dash disallowed)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Int32 max value",
                                    visuals = Helpers.Sample(initialText = "2147483647", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// "2147483647" -> Ok (Some 2147483647)""")
                                )

                                Ui.ComponentSample(
                                    heading = "Int32 overflow",
                                    visuals = Helpers.Sample(initialText = "2147483648", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// "2147483648" -> internal error: Int32 parse fails""")
                                )

                                Ui.ComponentSample(
                                    heading = "Whitespace",
                                    visuals = Helpers.Sample(initialText = "   1", validity = Valid),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """// "   1" -> internal error: Only numbers allowed""")
                                )
                            }
                        )
                    )
                }
            )
        )
