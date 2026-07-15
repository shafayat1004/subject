[<AutoOpen>]
module Rn.Components.Reanimated

// Reanimated 4 animation seam. This replaces the legacy RN-`Animated` seam
// (`Rn.Styles.Animation` + `Rn.Components.Animatable*`). App and component code never touches a
// worklet: create a `Reanimated.SharedValue`, drive it (`SetValue` during a gesture,
// `AnimateTiming`/`AnimateSpring` to settle), and render `Rn.ReanimatedView` with one of the
// `useAnimated*` style hooks. The seam depends only on react-native-reanimated + worklets (both
// already shipped by every app and the scaffold) — no extra animation dependency.
//
// `AnimateTiming`'s optional `onComplete` fires on the JS thread via a matching timer, because a
// worklet completion callback cannot safely call back into JS.
//
// NO WORKLETS ARE AUTHORED IN F#. Fable-emitted closures are NOT recognised by
// react-native-worklets/plugin, so a closure passed to `useAnimatedStyle` runs as a JS "remote
// function" and throws on the UI runtime ("Tried to synchronously call a Remote Function"). Instead
// the `useAnimated*` helpers embed the shared value *object* directly in a plain style object and
// Reanimated's animated View drives that prop on the UI thread automatically ("inline shared
// values") — worklet-free, and identical on web and native.

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
                | None   -> ()
            ]
            raw?value <- rawWithTiming toValue config
            match onComplete with
            | Some callback -> LibClient.JsInterop.runLater (System.TimeSpan.FromMilliseconds durationMs) callback
            | None          -> ()

        member this.AnimateTiming (toValue: int, ?durationMs: float, ?easing: obj, ?onComplete: unit -> unit): unit =
            this.AnimateTiming(double toValue, ?durationMs = durationMs, ?easing = easing, ?onComplete = onComplete)

        /// Spring to `toValue`. `config` is a raw Reanimated spring config object (damping/stiffness/…).
        member _.AnimateSpring (toValue: float, ?config: obj): unit =
            raw?value <- rawWithSpring toValue (defaultArg config (createObj []))

    /// Create a shared value. MUST be called at the top level of a `[<Component>]` (it is a hook).
    let useSharedValue (initial: float): SharedValue =
        SharedValue (rawUseSharedValue initial)

    // --- Animated style helpers (Reanimated "inline shared values", NO worklets) ------
    // Each returns a plain style object with the shared value *object itself* embedded (not its
    // `.value`). Reanimated's animated View detects a shared value inside its `style` and drives that
    // prop on the UI thread automatically — no `useAnimatedStyle` worklet. This is deliberate: a
    // Fable-emitted closure passed to `useAnimatedStyle` is NOT recognised by react-native-worklets/
    // plugin, so it runs as a JS "remote function" and throws on the UI runtime ("Tried to
    // synchronously call a Remote Function"). Inline shared values sidestep worklets entirely, so the
    // seam needs no worklet authoring and works the same on web and native. These are plain functions
    // (not hooks); the "useAnimated" prefix is kept for call-site familiarity/stability.

    /// `transform: [{ translateX }]` driven by `sv`.
    let useAnimatedTranslateX (sv: SharedValue): obj =
        createObj [ "transform" ==> [| createObj [ "translateX" ==> sv.Raw ] |] ]

    /// `transform: [{ translateY }]` driven by `sv`.
    let useAnimatedTranslateY (sv: SharedValue): obj =
        createObj [ "transform" ==> [| createObj [ "translateY" ==> sv.Raw ] |] ]

    /// `opacity` driven by `sv`.
    let useAnimatedOpacity (sv: SharedValue): obj =
        createObj [ "opacity" ==> sv.Raw ]

    /// 2-axis `transform: [{ translateX }, { translateY }]` driven by two shared values.
    let useAnimatedTranslateXY (svX: SharedValue) (svY: SharedValue): obj =
        createObj [
            "transform" ==> [|
                createObj [ "translateX" ==> svX.Raw ]
                createObj [ "translateY" ==> svY.Raw ]
            |]
        ]

// --- Components -----------------------------------------------------------------------

let private MakeReanimatedView: obj -> array<Fable.React.ReactElement> -> Fable.React.ReactElement =
    LibClient.ThirdParty.wrapComponent<obj> Reanimated.AnimatedViewComponent

type Rn.Components.Constructors.Rn with

    /// A Reanimated animated view. Merge normal `styles` with an `animatedStyle` produced by one of
    /// the `Reanimated.useAnimated*` hooks. For gesture-driven / imperative animation.
    static member ReanimatedView(
            ?styles:                    array<ViewStyles>,
            ?animatedStyle:             obj,
            ?children:                  array<Fable.React.ReactElement>,
            ?testId:                    string,
            ?importantForAccessibility: LibClient.Accessibility.ImportantForAccessibility,
            ?onLayout:                  obj -> unit,
            ?key:                       string) =
        let __props = createEmpty

        let style: array<obj> =
            [|
                yield! (styles |> Option.defaultValue [||] |> Array.map box)
                match animatedStyle with
                | Some s -> yield s
                | None   -> ()
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
