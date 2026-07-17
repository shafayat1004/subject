[<AutoOpen>]
module AppEggShellGallery.Components.Content_AnimatableTextInput

open System
open Fable.React
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles
open Rn.Styles.Animation

[<RequireQualifiedAccess>]
module private Styles =
    let basic (fontSize: AnimatedValue) =
        makeAnimatableTextInputStyles {
            color Color.DevRed
            animatedFontSize (AnimatableValue.Value fontSize)
        }

type private Helpers =
    [<Component>]
    static member Basic() : ReactElement =
        let min = 8.0
        let max = 32.0
        let isAnimatedRef = Hooks.useRef false
        let animatedValue = Hooks.useRef (AnimatedValue.Create max)

        let animate (_: ReactEvent.Action) =
            let animateTo =
                if isAnimatedRef.current then
                    max
                else
                    min

            let animation = Animation.Timing(animatedValue.current, animateTo, TimeSpan.FromSeconds 1.0)
            animation.Start(fun () -> isAnimatedRef.current <- not isAnimatedRef.current)

        let textState = Hooks.useState "My name"

        LC.Column(
            children =
                elements {
                    Rn.AnimatableTextInput(
                        value        = textState.current,
                        placeholder  = "Name",
                        onChangeText = textState.update,
                        styles       = [| Styles.basic animatedValue.current |]
                    )

                    LC.Button(
                        label = "Animate",
                        state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable animate)
                    )
                },
            gap = 12
        )

type Ui.Content with
    [<Component>]
    static member AnimatableTextInput () : ReactElement =
        Ui.ComponentContent (
            displayName  = "AnimatableTextInput",
            isResponsive = false,
            props        = ComponentContent.Manual (
                Ui.ComponentProps (data = {
                    Fields = (Choice2Of2 [
                        {
                            Name        = "value"
                            Type        = "string"
                            Default     = None
                            Description = None
                        }
                        {
                            Name        = "placeholder"
                            Type        = "string"
                            Default     = None
                            Description = None
                        }
                        {
                            Name        = "onChangeText"
                            Type        = "string -> unit"
                            Default     = None
                            Description = None
                        }
                        {
                            Name        = "styles"
                            Type        = "array<AnimatableTextInputStyles>"
                            Default     = None
                            Description = Some "Input styles with animated properties (fontSize, color, etc.) via makeAnimatableTextInputStyles"
                        }
                    ])
                    MaybeScrapeErrors = None
                })
            ),
            notes = LC.Text """Rn.AnimatableTextInput is a Rn animation primitive. Use Rn.Styles.Animation (AnimatedValue, Animation.Timing, etc.) to drive animated input styles.""",
            a11y =
                Ui.A11yPanel(
                    componentName  = "Rn.AnimatableTextInput",
                    role           = "text field",
                    namePattern    = "placeholder or value text; pair with visible label in production",
                    stateNotes     = "Animated input styles; honor reduce-motion where wired",
                    scalesWithFont = true,
                    contrastNotes  = "Input text and placeholder colors meet WCAG AA"
                ),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Basic",
                                    visuals = Helpers.Basic(),
                                    code    = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
let animatedValue = Hooks.useRef (AnimatedValue.Create 32.0)
let text = Hooks.useState "My name"

Rn.AnimatableTextInput(
    value = text.current,
    placeholder = "Name",
    onChangeText = text.update,
    styles = [| makeAnimatableTextInputStyles {
        color Color.DevRed
        animatedFontSize (AnimatableValue.Value animatedValue.current)
    } |]
)

Animation.Timing(animatedValue.current, 8.0, TimeSpan.FromSeconds 1.0).Start(...)
""")
                                )
                            }
                        )
                    )
                }
            )
        )
