[<AutoOpen>]
module Rn.Components.SafeArea

// Safe-area seam. Exposes the device safe-area insets (status bar, notch, home indicator,
// nav gesture bar) to framework layout code. Native reads react-native-safe-area-context's
// `useSafeAreaInsets` hook -- the app root is wrapped in `SafeAreaProvider`
// (see RnPrimitives.setMainView). Web has no hardware insets, so it returns zeros without
// importing the module, keeping react-native-safe-area-context out of the web bundle.
//
// `useInsets` is a React hook: call it once, unconditionally, at the top of a [<Component>]
// body (like any other hook). On web it is a plain constant, so hook order stays consistent.

open Fable.Core.JsInterop

module SafeArea =

    type Insets = {
        Top:    int
        Right:  int
        Bottom: int
        Left:   int
    }

    let zero : Insets = { Top = 0; Right = 0; Bottom = 0; Left = 0 }

#if !EGGSHELL_PLATFORM_IS_WEB
    // Declared with an explicit parameter (not a value of function type) so Fable emits the call
    // at the right arity -- same reason as the Reanimated seam's raw imports.
    let private rawUseSafeAreaInsets () : obj = import "useSafeAreaInsets" "react-native-safe-area-context"

    let private toInt (v: obj) : int =
        System.Math.Round(unbox<float> v) |> int
#endif

    /// React hook: current device safe-area insets. Must be called from a component body.
    let useInsets () : Insets =
#if EGGSHELL_PLATFORM_IS_WEB
        zero
#else
        let o = rawUseSafeAreaInsets ()
        {
            Top    = toInt o?top
            Right  = toInt o?right
            Bottom = toInt o?bottom
            Left   = toInt o?left
        }
#endif
