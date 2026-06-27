[<AutoOpen>]
module AppEggShellGallery.Components.Content_Forms

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Form_Base
open LibClient.Components.Form_Base.Types
open LibClient.Components.Input

type Gender =
| Male
| Female
| Other
| Undisclosed

type Profile = {
    Name:      NonemptyString
    Age:       PositiveInteger
    Gender:    Gender
    WantsSpam: bool
}

type PickerItem<'T> = LibClient.Components.Legacy.Input.Picker.PickerItem<'T>

[<RequireQualifiedAccess>]
type Field = Name | Age | Gender | WantsSpam

type Acc = {
    GenderChoices: List<PickerItem<Gender>>
    Name:          Option<NonemptyString>
    Age:           LibClient.Components.Input.PositiveInteger.Value
    Gender:        Option<Gender>
    WantsSpam:     bool
} with
    static member Initial : Acc =
        {
            GenderChoices = [ Male; Female; Other; Undisclosed ] |> List.map (fun g -> { Item = g; Label = g.ToString() })
            Name          = None
            Age           = LibClient.Components.Input.PositiveInteger.empty
            Gender        = None
            WantsSpam     = true
        }

    interface AbstractAcc<Field, Profile> with
        member this.Validate () : Result<Profile, ValidationErrors<Field>> = validateForm {
            let! name   = Forms.GetFieldValue2 this.Name       Field.Name
            and! age    = Forms.GetFieldValue2 this.Age.Result Field.Age
            and! gender = Forms.GetFieldValue2 this.Gender     Field.Gender

            return {
                Name      = name
                Age       = age
                Gender    = gender
                WantsSpam = this.WantsSpam
            }
        }

module private FormSample =
    let submit (_profile: Profile) (_e: ReactEvent.Action) () : UDActionResult =
        async {
            Action.alert "A profile record successfully input!"
            return Ok ()
        }

    [<Component>]
    let Render () : ReactElement =
        LC.Form.Base(
            accumulator = Accumulator.ManageInternallyInitializingWith Acc.Initial,
            submit = submit,
            content =
                fun (form: FormHandle<Field, Acc, Profile>) ->
                    element {
                        LC.Input.Text(
                            label = "Name",
                            validity = form.FieldValidity Field.Name,
                            value = form.Acc.Name,
                            onEnterKeyPress = (ReactEvent.Action.Make >> form.TrySubmitLowLevel),
                            onChange = (fun value -> form.UpdateAcc (fun acc -> { acc with Name = value }))
                        )
                        LC.Input.PositiveInteger(
                            label = "Age",
                            validity = form.FieldValidity Field.Age,
                            value = form.Acc.Age,
                            onEnterKeyPress = (ReactEvent.Action.Make >> form.TrySubmitLowLevel),
                            onChange = (fun value -> form.UpdateAcc (fun acc -> { acc with Age = value }))
                        )
                        LC.Legacy.Input.Picker(
                            label = "Gender",
                            validity = form.FieldValidity Field.Gender,
                            items = form.Acc.GenderChoices,
                            value = (form.Acc.Gender |> Option.map LibClient.Components.Legacy.Input.Picker.ByItem),
                            onChange =
                                LibClient.Components.Legacy.Input.Picker.CannotUnselect (
                                    fun (_, value) -> form.UpdateAcc (fun acc -> { acc with Gender = Some value })
                                )
                        )
                        LC.Input.Checkbox(
                            label = Label.String "Subscribe to email",
                            value = Some form.Acc.WantsSpam,
                            onChange = (fun value -> form.UpdateAcc (fun acc -> { acc with WantsSpam = value })),
                            validity = form.FieldValidity Field.WantsSpam
                        )
                        LC.Buttons(
                            children =
                                elements {
                                    LC.Button(
                                        label = "Submit",
                                        state = Button.PropStateFactory.Make form.TrySubmit
                                    )
                                }
                        )
                    }
        )

type Ui.Content with
    [<Component>]
    static member Forms () : ReactElement =
        Ui.ComponentContent(
            displayName = "Forms",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Form.Base",
            notes =
                element {
                    LC.Text "We think of forms as a chunk of UI whose job is to produce a value of type 'T. The main building block for a form is the accumulator (Acc), which holds partially filled data and provides Validate."
                    LC.Text "Form elements are regular input elements bound to the form's validation. Currently we have Text, PositiveInteger, UnsignedDecimal, and Picker wrapped."
                },
            samples =
                element {
                    Ui.ComponentSample(
                        verticalAlignment = ComponentSample.VerticalAlignment.Top,
                        visuals = FormSample.Render(),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = ComponentSample.Fsharp,
                                        children =
                                            [| LC.Text """
type Gender = Male | Female | Other | Undisclosed

type Profile = {
    Name: NonemptyString
    Age: PositiveInteger
    Gender: Gender
    WantsSpam: bool
}

type Acc = { ... } with
    static member Initial = { ... }
    interface AbstractAcc<Field, Profile> with
        member this.Validate () = validateForm { ... }

LC.Form.Base(
    accumulator = Accumulator.ManageInternallyInitializingWith Acc.Initial,
    submit = submit,
    content = fun form ->
        element {
            LC.Input.Text(...)
            LC.Input.PositiveInteger(...)
            LC.Legacy.Input.Picker(...)
            LC.Input.Checkbox(...)
            LC.Buttons [ LC.Button(label = "Submit", state = Button.PropStateFactory.Make form.TrySubmit) ]
        }
)
""" |]
                                    )
                                }
                            )
                    )
                }
        )
