namespace Rn.Styles

module Config =
    let mutable private isDevMode: bool = false
    let getIsDevMode () : bool = isDevMode
    let setIsDevMode (value: bool) =
        isDevMode <- value

[<RequireQualifiedAccess>]
type RawRnStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnViewStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnAnimatedViewStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnTextStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnAnimatedTextStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnFlexChildrenStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnFlexStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnAnimatedFlexStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnTransformStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

[<RequireQualifiedAccess>]
type RawRnAnimatedTransformStyleRule =
| WeOnlyWantOurHelperFunctionsToProduceThese

type Color = LibClient.ColorModule.Color
