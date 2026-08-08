module AppTodo.Feedback

// Native haptics via react-native-haptic-feedback (Taptic Engine on iOS, HapticFeedbackConstants
// on Android). Web has no Taptic Engine; navigator.vibrate is the closest analog and is only
// honored by some Android browsers, so it is a best-effort no-op elsewhere.
//
// GOTCHA (see runbooks/troubleshooting.md, RN 0.86 prebuilt-React section): adding this pod
// previously broke the Fabric link with undefined facebook::react::Sealable symbols. Root cause
// was NOT this library -- RN 0.86's default RCT_USE_PREBUILT_RNCORE=1 checksum-pins a prebuilt
// ReactNativeDependencies.xcframework, and any `pod install` that adds a new pod can re-resolve
// past that pin. Fix is in ios/Podfile (RCT_USE_PREBUILT_RNCORE=0 / RCT_USE_RN_DEP=0 forces a
// from-source React-Core so pod resolution can't drift from the linked binary).

#if EGGSHELL_PLATFORM_IS_WEB
let buttonTap () : unit = ()
let toggleSwitch () : unit = ()
#else
open Fable.Core
open Fable.Core.JsInterop

[<Import("default", "react-native-haptic-feedback")>]
let private haptics: obj = jsNative

let private options =
    createObj [
        "enableVibrateFallback"       ==> true
        "ignoreAndroidSystemSettings" ==> false
    ]

let buttonTap () : unit =
    haptics?trigger ("impactMedium", options)

let toggleSwitch () : unit =
    haptics?trigger ("rigid", options)
#endif
