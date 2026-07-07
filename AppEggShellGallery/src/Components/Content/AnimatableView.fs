[<AutoOpen>]
module AppEggShellGallery.Components.Content_AnimatableView

open System
open Fable.React
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles
open Rn.Styles.Animation
open AppEggShellGallery.LocalImages

[<RequireQualifiedAccess>]
module private Styles =
    let scale (animatableValue: AnimatableValue) =
        makeAnimatableViewStyles {
            animatedTransform
                [
                    [ animatedScale animatableValue ]
                ]
        }

    let scaleAndOpacity (animatableLeftValue: AnimatableValue) (animatableScaleValue: AnimatableValue) =
        makeAnimatableViewStyles {
            animatedOpacity animatableLeftValue
            animatedTransform [ [ animatedScale animatableScaleValue ] ]
        }

    let opacity (animatableValue: AnimatableValue) =
        makeAnimatableViewStyles {
            animatedOpacity animatableValue
        }

    let backgroundColor (interpolatedValue: InterpolatedValue) =
        makeAnimatableViewStyles {
            animatedBackgroundColor interpolatedValue
        }

    let text =
        makeTextStyles {
            fontSize 10
            TextAlign.Center
        }

    let image =
        makeViewStyles {
            width 200
            height 100
            AlignSelf.Center
        }

type private Helpers =
    [<Component>]
    static member private Image() : ReactElement =
        Rn.Image(
            localImage "/images/wlop2.jpg",
            styles = [| Styles.image |]
        )

    [<Component>]
    static member private ScaleAnimationTarget(animatableValue: AnimatableValue) : ReactElement =
        Rn.AnimatableView(
            children =
                elements {
                    Helpers.Image()
                },
            styles = [| Styles.scale animatableValue |]
        )

    [<Component>]
    static member private ColorAnimationTarget(interpolatedValue: InterpolatedValue) : ReactElement =
        Rn.AnimatableView(
            children =
                elements {
                    Helpers.Image()
                },
            styles = [| Styles.backgroundColor interpolatedValue |]
        )

    [<Component>]
    static member BasicTiming() : ReactElement =
        let min = 0.1
        let max = 1.0
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
                    Helpers.ScaleAnimationTarget(AnimatableValue.Value animatedValue.current)

                    LC.Button(
                        label = "Animate",
                        state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable animate)
                    )
                },
            gap = 12
        )

    [<Component>]
    static member Sequence() : ReactElement =
        let isAnimatedRef = Hooks.useRef false
        let animatedOpacityValue = Hooks.useRef (AnimatedValue.Create 0.1)
        let animatedScaleValue = Hooks.useRef (AnimatedValue.Create 0.5)

        let animate (_: ReactEvent.Action) =
            let animation =
                Animation.Sequence(
                    Animation.Timing(animatedOpacityValue.current, (if isAnimatedRef.current then 0.1 else 1.0), TimeSpan.FromSeconds 2.),
                    Animation.Timing(animatedScaleValue.current, (if isAnimatedRef.current then 0.5 else 1.0), TimeSpan.FromSeconds 0.75)
                )

            animation.Start(fun () -> isAnimatedRef.current <- not isAnimatedRef.current)

        LC.Column(
            children =
                elements {
                    Rn.AnimatableView(
                        children =
                            elements {
                                Helpers.Image()
                            },
                        styles = [| Styles.scaleAndOpacity (AnimatableValue.Value animatedOpacityValue.current) (AnimatableValue.Value animatedScaleValue.current) |]
                    )

                    LC.Button(
                        label = "Animate",
                        state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable animate)
                    )
                },
            gap = 12
        )

    [<Component>]
    static member Parallel() : ReactElement =
        let isAnimatedRef = Hooks.useRef false
        let animatedOpacityValue = Hooks.useRef (AnimatedValue.Create 0.1)
        let animatedScaleValue = Hooks.useRef (AnimatedValue.Create 0.5)

        let animate (_: ReactEvent.Action) =
            let animation =
                Animation.Parallel(
                    Animation.Timing(animatedOpacityValue.current, (if isAnimatedRef.current then 0.1 else 1.0), TimeSpan.FromSeconds 2.),
                    Animation.Timing(animatedScaleValue.current, (if isAnimatedRef.current then 0.5 else 1.0), TimeSpan.FromSeconds 0.75)
                )

            animation.Start(fun () -> isAnimatedRef.current <- not isAnimatedRef.current)

        LC.Column(
            children =
                elements {
                    Rn.AnimatableView(
                        children =
                            elements {
                                Helpers.Image()
                            },
                        styles = [| Styles.scaleAndOpacity (AnimatableValue.Value animatedOpacityValue.current) (AnimatableValue.Value animatedScaleValue.current) |]
                    )

                    LC.Button(
                        label = "Animate",
                        state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable animate)
                    )
                },
            gap = 12
        )

    [<Component>]
    static member NumericalInterpolation() : ReactElement =
        let min = 0.2
        let mid = 0.5
        let max = 1.0
        let isAnimatedRef = Hooks.useRef false
        let animatedValue = Hooks.useRef (AnimatedValue.Create min)
        let interpolatedValue =
            Hooks.useRef
                (
                    let interpolationConfig =
                        InterpolationConfig.Create(
                            [
                                (min, 0.5)
                                (mid, 2.0)
                                (max, 1.2)
                            ]
                        )
                    animatedValue.current.Interpolate interpolationConfig
                )

        let animate (_: ReactEvent.Action) =
            let animateTo =
                if isAnimatedRef.current then
                    min
                else
                    max

            // On web, interpolation only supports two values, so to transition between more than two values we use a sequence.
            // Doing so works consistently on both web and native.
            let animation =
                Animation.Sequence(
                    Animation.Timing(animatedValue.current, mid, TimeSpan.FromSeconds 0.5),
                    Animation.Timing(animatedValue.current, animateTo, TimeSpan.FromSeconds 1)
                )
            animation.Start(fun () -> isAnimatedRef.current <- not isAnimatedRef.current)

        LC.Column(
            children =
                elements {
                    Helpers.ScaleAnimationTarget(AnimatableValue.Interpolated interpolatedValue.current)

                    LC.Button(
                        label = "Animate",
                        state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable animate)
                    )
                },
            gap = 12
        )

    [<Component>]
    static member ColorInterpolation() : ReactElement =
        let min = 0.1
        let mid = 0.5
        let max = 1.0
        let isAnimatedRef = Hooks.useRef false
        let animatedValue = Hooks.useRef (AnimatedValue.Create min)
        let interpolatedValue =
            Hooks.useRef
                (
                    let interpolationConfig =
                        InterpolationConfig.Create(
                            [
                                (min, Color.DevBlue)
                                (mid, Color.DevGreen)
                                (max, Color.DevRed)
                            ]
                        )
                    animatedValue.current.Interpolate interpolationConfig
                )

        let animate (_: ReactEvent.Action) =
            let animateTo =
                if isAnimatedRef.current then
                    min
                else
                    max

            // On web, interpolation only supports two values, so to transition between more than two values we use a sequence.
            let animation =
                Animation.Sequence(
                    Animation.Timing(animatedValue.current, mid, TimeSpan.FromSeconds 0.5),
                    Animation.Timing(animatedValue.current, animateTo, TimeSpan.FromSeconds 1)
                )
            animation.Start(fun () -> isAnimatedRef.current <- not isAnimatedRef.current)

        LC.Column(
            children =
                elements {
                    Helpers.ColorAnimationTarget(interpolatedValue.current)

                    LC.Button(
                        label = "Animate",
                        state = ButtonHighLevelState.LowLevel (ButtonLowLevelState.Actionable animate)
                    )
                },
            gap = 12
        )

    [<Component>]
    static member private EaseAnimationTarget(easing: Easing, animatableValue: AnimatableValue) : ReactElement =
        let easingDisplayString (easing: Easing): string =
            match easing with
            | Easing.Linear -> "Linear"
            | Easing.Out -> "Out"
            | Easing.In -> "In"
            | Easing.InOut -> "In/Out"
            | Easing.InBack -> "In/Back"
            | Easing.OutBack -> "Out/Back"
            | Easing.InOutBack -> "In/Out/Back"
            | Easing.StepStart -> "Step Start"
            | Easing.StepEnd -> "Step End"
            | Easing.Steps _ -> "Steps"
            | Easing.CubicBezier _ -> "Cubic Bezier"

        LC.Column(
            children =
                elements {
                    Rn.AnimatableView(
                        children =
                            elements {
                                Helpers.Image()
                            },
                        styles = [| Styles.opacity animatableValue |]
                    )
                    LC.Text(
                        easingDisplayString easing,
                        styles = [| Styles.text |]
                    )
                },
            gap = 3
        )

    [<Component>]
    static member Easing() : ReactElement =
        let visibleOpacity = 1.0
        let nonVisibleOpacity = 0.1
        let isAnimatedRef = Hooks.useRef true

        let easingsWithAnimationValues =
            Hooks.useMemo(
                (fun () ->
                    [|
                        Easing.Linear
                        Easing.Out
                        Easing.In
                        Easing.InOut
                        Easing.InBack
                        Easing.OutBack
                        Easing.InOutBack
                        Easing.StepStart
                        Easing.StepEnd
                        Easing.Steps (5, Some true)
                        Easing.CubicBezier ((0.5, 0.2), (0.75, 0.95))
                    |]
                    |> Array.map (
                        fun easing ->
                            (easing, AnimatedValue.Create(if isAnimatedRef.current then visibleOpacity else nonVisibleOpacity))
                    )
                ),
                [||]
            )

        let animate (_: ReactEvent.Action) =
            let animateTo =
                if isAnimatedRef.current then
                    nonVisibleOpacity
                else
                    visibleOpacity

            easingsWithAnimationValues
            |> Array.map (
                fun (easing, animatedValue) ->
                    Animation.Timing(animatedValue, animateTo, TimeSpan.FromSeconds 1, easing = easing)
            )
            |> (fun animations ->
                let animation = Animation.Parallel(animations)

                animation.Start(fun () -> isAnimatedRef.current <- not isAnimatedRef.current)
            )

        LC.Column(
            children =
                elements {
                    easingsWithAnimationValues
                    |> Array.chunkBySize 4
                    |> Array.map (
                        fun easingsWithAnimationValues ->
                            LC.Row(
                                children =
                                    (
                                        easingsWithAnimationValues
                                        |> Array.map (fun (easing, animatedValue) -> Helpers.EaseAnimationTarget(easing, AnimatableValue.Value animatedValue))
                                    ),
                                gap = 10
                            )
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
    static member AnimatableView () : ReactElement =
        Ui.ComponentContent (
            displayName = "AnimatableView",
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
                            Type = "array<AnimatableViewStyles>"
                            Default = None
                            Description = Some "View styles with animated properties (opacity, transform, backgroundColor, etc.) via makeAnimatableViewStyles"
                        }
                    ])
                    MaybeScrapeErrors = None
                })
            ),
            notes = LC.Text """Rn.AnimatableView is a Rn animation primitive. Use Rn.Styles.Animation (AnimatedValue, Animation.Timing/Sequence/Parallel, InterpolationConfig, Easing) to drive animated view styles.""",
            a11y =
                Ui.A11yPanel(
                    componentName = "Rn.AnimatableView",
                    role = "none (animated container)",
                    namePattern = "Child content provides accessible names",
                    stateNotes = "Animated opacity/transform; honor reduce-motion where wired",
                    scalesWithFont = true,
                    contrastNotes = "Child content contrast unchanged by animation wrapper"
                ),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    heading = "Basic Timing",
                                    visuals = Helpers.BasicTiming(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
let animatedValue = Hooks.useRef (AnimatedValue.Create 1.0)

Rn.AnimatableView(
    children = elements { Rn.Image(imageUrl, styles = [| ... |]) },
    styles = [| makeAnimatableViewStyles {
        animatedTransform [ [ animatedScale (AnimatableValue.Value animatedValue.current) ] ]
    } |]
)

Animation.Timing(animatedValue.current, 0.1, TimeSpan.FromSeconds 1).Start(...)
""")
                                )

                                Ui.ComponentSample(
                                    heading = "Sequence",
                                    visuals = Helpers.Sequence(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
Animation.Sequence(
    Animation.Timing(animatedOpacityValue.current, targetOpacity, TimeSpan.FromSeconds 2.),
    Animation.Timing(animatedScaleValue.current, targetScale, TimeSpan.FromSeconds 0.75)
).Start(...)

Rn.AnimatableView(
    children = elements { Rn.Image(imageUrl, styles = [| ... |]) },
    styles = [| makeAnimatableViewStyles {
        animatedOpacity (AnimatableValue.Value animatedOpacityValue.current)
        animatedTransform [ [ animatedScale (AnimatableValue.Value animatedScaleValue.current) ] ]
    } |]
)
""")
                                )

                                Ui.ComponentSample(
                                    heading = "Parallel",
                                    visuals = Helpers.Parallel(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
Animation.Parallel(
    Animation.Timing(animatedOpacityValue.current, targetOpacity, TimeSpan.FromSeconds 2.),
    Animation.Timing(animatedScaleValue.current, targetScale, TimeSpan.FromSeconds 0.75)
).Start(...)

Rn.AnimatableView(
    children = elements { Rn.Image(imageUrl, styles = [| ... |]) },
    styles = [| makeAnimatableViewStyles {
        animatedOpacity (AnimatableValue.Value animatedOpacityValue.current)
        animatedTransform [ [ animatedScale (AnimatableValue.Value animatedScaleValue.current) ] ]
    } |]
)
""")
                                )

                                Ui.ComponentSample(
                                    heading = "Numerical Interpolation",
                                    visuals = Helpers.NumericalInterpolation(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
let interpolationConfig = InterpolationConfig.Create [ (0.2, 0.5); (0.5, 2.0); (1.0, 1.2) ]
let interpolatedValue = animatedValue.current.Interpolate interpolationConfig

Rn.AnimatableView(
    children = elements { Rn.Image(imageUrl, styles = [| ... |]) },
    styles = [| makeAnimatableViewStyles {
        animatedTransform [ [ animatedScale (AnimatableValue.Interpolated interpolatedValue) ] ]
    } |]
)

// Multi-stop interpolation uses Animation.Sequence on web
Animation.Sequence(
    Animation.Timing(animatedValue.current, 0.5, TimeSpan.FromSeconds 0.5),
    Animation.Timing(animatedValue.current, target, TimeSpan.FromSeconds 1)
).Start(...)
""")
                                )

                                Ui.ComponentSample(
                                    heading = "Color Interpolation",
                                    visuals = Helpers.ColorInterpolation(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
let interpolationConfig = InterpolationConfig.Create [
    (0.1, Color.DevBlue); (0.5, Color.DevGreen); (1.0, Color.DevRed)
]
let interpolatedValue = animatedValue.current.Interpolate interpolationConfig

Rn.AnimatableView(
    children = elements { Rn.Image(imageUrl, styles = [| ... |]) },
    styles = [| makeAnimatableViewStyles {
        animatedBackgroundColor interpolatedValue
    } |]
)
""")
                                )

                                Ui.ComponentSample(
                                    heading = "Easing",
                                    visuals = Helpers.Easing(),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
let easings = [ Easing.Linear; Easing.Out; Easing.InOut; Easing.CubicBezier ((0.5, 0.2), (0.75, 0.95)) ]

easings
|> List.map (fun easing ->
    Animation.Timing(animatedValue, targetOpacity, TimeSpan.FromSeconds 1, easing = easing))
|> Animation.Parallel
|> fun animation -> animation.Start(...)
""")
                                )
                            }
                        )
                    )
                }
            )
        )
