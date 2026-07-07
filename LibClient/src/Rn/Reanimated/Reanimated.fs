[<AutoOpen>]
module Rn.Components.Reanimated

// Reanimated 4 / Moti animation seam. This replaces the legacy RN-`Animated` seam
// (`Rn.Styles.Animation` + `Rn.Components.Animatable*`). Two paths, both authored here so app and
// component code never touches a worklet:
//
//   * Declarative (preferred) — `Rn.MotiView`: pass `from`/`animate`/`transition` prop objects.
//     Moti drives the animation on the UI thread and marshals `onDidAnimate` back to the JS thread
//     for us (it wraps `runOnJS` internally), so completion callbacks are safe.
//
//   * Imperative (follow-the-finger drag / gesture-driven) — a `Reanimated.SharedValue` plus one of
//     the `useAnimated*` style hooks below, rendered through `Rn.ReanimatedView`. Drive the value
//     directly (`SetValue`) during a gesture and with `AnimateTiming`/`AnimateSpring` to settle.
//
// WORKLET RULE (hard): the only worklets in the framework live in this file, and their bodies do
// nothing but read a shared value into a plain style object. NO host-function calls (`runOnJS`,
// logging, JS callbacks) inside a worklet — those abort libworklets (SIGABRT at
// jsi::Function::getHostFunction). `useAnimatedStyle` auto-workletizes its argument (Metro's
// react-native-worklets/plugin on native; a plain JS-thread closure on web). Proven in the spike.

open Fable.Core
open Fable.Core.JsInterop

open Rn.Helpers
open Rn.Styles
open Rn.Types

open LibClient

module Reanimated =

    // --- Raw imports ------------------------------------------------------------------
    // Web bundles alias react-native-reanimated to its lib/module build (webpack.config.js);
    // native resolves it directly. The default export is the Animated namespace (`.View` etc.).
    // Imported functions are declared with explicit parameters (not as values of function type) so
    // Fable emits them at the right arity — the value form triggers "Change declaration of member".
    let private reanimatedDefault: obj = importDefault "react-native-reanimated"
    let private rawUseSharedValue (_initial: float) : obj = import "useSharedValue" "react-native-reanimated"
    let private rawUseAnimatedStyle (_worklet: unit -> obj) : obj = import "useAnimatedStyle" "react-native-reanimated"
    let private rawWithTiming (_toValue: float) (_config: obj) : obj = import "withTiming" "react-native-reanimated"
    let private rawWithSpring (_toValue: float) (_config: obj) : obj = import "withSpring" "react-native-reanimated"

    /// The Reanimated animated `View` component (accepts animated styles). Rendered via createElement.
    let AnimatedViewComponent: obj = reanimatedDefault?View

    let private rawEasing: obj = import "Easing" "react-native-reanimated"

    /// Reanimated `Easing` presets (for `AnimateTiming ?easing`).
    [<RequireQualifiedAccess>]
    module Easing =
        let Linear: obj = rawEasing?linear
        let InOut: obj = rawEasing?inOut(rawEasing?ease)
        let Out: obj = rawEasing?out(rawEasing?ease)
        let In: obj = rawEasing?``in``(rawEasing?ease)

    /// A Reanimated shared value (a mutable value the UI thread can read every frame). Create it with
    /// `useSharedValue` at the top level of a component; drive it imperatively or via the animate
    /// helpers. Read/write of `.Value` maps to the raw `.value` field.
    type SharedValue internal (raw: obj) =
        member _.Raw: obj = raw

        member _.Value
            with get (): float = raw?value
            and set (v: float) = raw?value <- v

        /// Snap the value immediately (no animation). Use during a follow-the-finger drag.
        member _.SetValue (v: float): unit = raw?value <- v
        member _.SetValue (v: int): unit = raw?value <- double v

        /// Animate to `toValue` over `durationMs` (default 300). `onComplete`, when given, is fired on
        /// the JS thread via a matching timer (worklet callbacks cannot safely call back into JS).
        member _.AnimateTiming (toValue: float, ?durationMs: float, ?easing: obj, ?onComplete: unit -> unit): unit =
            let durationMs = defaultArg durationMs 300.0
            let config = createObj [
                "duration" ==> durationMs
                match easing with
                | Some e -> "easing" ==> e
                | None -> ()
            ]
            raw?value <- rawWithTiming toValue config
            match onComplete with
            | Some callback -> LibClient.JsInterop.runLater (System.TimeSpan.FromMilliseconds durationMs) callback
            | None -> ()

        member this.AnimateTiming (toValue: int, ?durationMs: float, ?easing: obj, ?onComplete: unit -> unit): unit =
            this.AnimateTiming(double toValue, ?durationMs = durationMs, ?easing = easing, ?onComplete = onComplete)

        /// Spring to `toValue`. `config` is a raw Reanimated spring config object (damping/stiffness/…).
        member _.AnimateSpring (toValue: float, ?config: obj): unit =
            raw?value <- rawWithSpring toValue (defaultArg config (createObj []))

    /// Create a shared value. MUST be called at the top level of a `[<Component>]` (it is a hook).
    let useSharedValue (initial: float): SharedValue =
        SharedValue (rawUseSharedValue initial)

    // --- Animated style hooks (the ONLY worklets in the framework) --------------------
    // Each returns an animated style object to hand to `Rn.ReanimatedView animatedStyle=`. The
    // closures below are the worklets; they only read `raw.value` into a style — no host calls.

    /// Animated `transform: [{ translateX }]` bound to `sv`.
    let useAnimatedTranslateX (sv: SharedValue): obj =
        let raw = sv.Raw
        rawUseAnimatedStyle (fun () ->
            createObj [ "transform" ==> [| createObj [ "translateX" ==> raw?value ] |] ])

    /// Animated `transform: [{ translateY }]` bound to `sv`.
    let useAnimatedTranslateY (sv: SharedValue): obj =
        let raw = sv.Raw
        rawUseAnimatedStyle (fun () ->
            createObj [ "transform" ==> [| createObj [ "translateY" ==> raw?value ] |] ])

    /// Animated `opacity` bound to `sv`.
    let useAnimatedOpacity (sv: SharedValue): obj =
        let raw = sv.Raw
        rawUseAnimatedStyle (fun () ->
            createObj [ "opacity" ==> raw?value ])

    /// Animated 2-axis `transform: [{ translateX }, { translateY }]` bound to two shared values.
    let useAnimatedTranslateXY (svX: SharedValue) (svY: SharedValue): obj =
        let rawX = svX.Raw
        let rawY = svY.Raw
        rawUseAnimatedStyle (fun () ->
            createObj [
                "transform" ==> [|
                    createObj [ "translateX" ==> rawX?value ]
                    createObj [ "translateY" ==> rawY?value ]
                |]
            ])

// --- Components -----------------------------------------------------------------------

let private MakeReanimatedView: obj -> array<Fable.React.ReactElement> -> Fable.React.ReactElement =
    LibClient.ThirdParty.wrapComponent<obj> Reanimated.AnimatedViewComponent

let private MakeMotiView: obj -> array<Fable.React.ReactElement> -> Fable.React.ReactElement =
    LibClient.ThirdParty.wrapComponent<obj> (import "MotiView" "moti")

type Rn.Components.Constructors.Rn with

    /// A Reanimated animated view. Merge normal `styles` with an `animatedStyle` produced by one of
    /// the `Reanimated.useAnimated*` hooks. For gesture-driven / imperative animation.
    static member ReanimatedView(
            ?styles:        array<ViewStyles>,
            ?animatedStyle: obj,
            ?children:      array<Fable.React.ReactElement>,
            ?testId:        string,
            ?importantForAccessibility: LibClient.Accessibility.ImportantForAccessibility,
            ?onLayout:      obj -> unit,
            ?key:           string) =
        let __props = createEmpty

        let style: array<obj> =
            [|
                yield! (styles |> Option.defaultValue [||] |> Array.map box)
                match animatedStyle with
                | Some s -> yield s
                | None -> ()
            |]

        __props?style                     <- style
        __props?testID                    <- testId
        __props?importantForAccessibility <- importantForAccessibility
        __props?onLayout                  <- onLayout
        __props?key                       <- key

        let children =
            children
            |> Option.map tellReactArrayKeysAreOkay
            |> Option.getOrElse [||]
            |> LibClient.ThirdParty.fixPotentiallySingleChild

        MakeReanimatedView __props children

    /// A Moti view: declarative animation via `from` / `animate` / `transition` prop objects. Moti
    /// drives it on the UI thread and marshals `onDidAnimate` back to the JS thread for us.
    static member MotiView(
            ?from:         obj,
            ?animate:      obj,
            ?transition:   obj,
            ?exitState:    obj,
            ?onDidAnimate: string -> bool -> unit,
            ?styles:       array<ViewStyles>,
            ?children:     array<Fable.React.ReactElement>,
            ?testId:       string,
            ?importantForAccessibility: LibClient.Accessibility.ImportantForAccessibility,
            ?key:          string) =
        let __props = createEmpty

        __props?from                      <- from
        __props?animate                   <- animate
        __props?transition                <- transition
        __props?exit                      <- exitState
        __props?onDidAnimate              <- onDidAnimate
        __props?style                     <- styles
        __props?testID                    <- testId
        __props?importantForAccessibility <- importantForAccessibility
        __props?key                       <- key

        let children =
            children
            |> Option.map tellReactArrayKeysAreOkay
            |> Option.getOrElse [||]
            |> LibClient.ThirdParty.fixPotentiallySingleChild

        MakeMotiView __props children
