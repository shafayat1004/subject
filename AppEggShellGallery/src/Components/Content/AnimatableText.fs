[<AutoOpen>]
module AppEggShellGallery.Components.Content_AnimatableText

open System
open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Components
open ReactXP.Styles
open ReactXP.Styles.Animation

[<RequireQualifiedAccess>]
module private Styles =
    let basic (fontSize: AnimatedValue) =
        makeAnimatableTextStyles {
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

            let animation = Animation.Timing(animatedValue.current, animateTo, TimeSpan.FromSeconds 1)
            animation.Start(fun () -> isAnimatedRef.current <- not isAnimatedRef.current)

        LC.Column(
            children =
                elements {
                    RX.AnimatableText(
                        children =
                            elements {
                                LC.Text "Here is some text"
                            },
                        styles = [| Styles.basic animatedValue.current |]
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
    static member AnimatableText () : ReactElement =
        Ui.ComponentContent (
            displayName = "AnimatableText",
            isResponsive = false,
            props = ComponentContent.Manual (
                Ui.ComponentProps (data = {
                    Fields = (Choice2Of2 [
                        {
                            Name = "children"
                            Type = "array<ReactElement>"
                            Default = None
                            Description = None
                        }
                        {
                            Name = "styles"
                            Type = "array<AnimatableTextStyles>"
                            Default = None
                            Description = Some "Text styles with animated properties (fontSize, color, etc.) via makeAnimatableTextStyles"
                        }
                    ])
                    MaybeScrapeErrors = None
                })
            ),
            notes = LC.Text """RX.AnimatableText is a ReactXP animation primitive. Use ReactXP.Styles.Animation (AnimatedValue, Animation.Timing, etc.) to drive animated text styles.""",
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Basic",
                                    visuals = Helpers.Basic(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
let animatedValue = Hooks.useRef (AnimatedValue.Create 32.0)

RX.AnimatableText(
    children = elements { LC.Text "Here is some text" },
    styles = [| makeAnimatableTextStyles {
        color Color.DevRed
        animatedFontSize (AnimatableValue.Value animatedValue.current)
    } |]
)

Animation.Timing(animatedValue.current, 8.0, TimeSpan.FromSeconds 1).Start(...)
""")
                                )
                            }
                        )
                    )
                }
            )
        )
