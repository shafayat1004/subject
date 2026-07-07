namespace Rn.Styles

open Rn
open Rn.Styles
open LibClient.ColorModule

[<AutoOpen>]
module RulesAdditional =
    let trbl (t: int) (r: int) (b: int) (l: int) : array<RawRnFlexStyleRule> =
        [|
            top    t
            right  r
            bottom b
            left   l
        |]

    let size (w: int) (h: int) : array<RawRnFlexStyleRule> =
        [|
            width  w
            height h
        |]

    let border (width: int) (color: Color) : array<RawRnViewStyleRule> =
        [|
            borderWidth width
            borderColor color
        |]

    // NOTE crappy Rn doesn't support setting color for just one side, so these helper function may eventually confuse some users
    let borderTop (width: int) (color: Color) : array<RawRnViewStyleRule> =
        [|
            borderTopWidth width
            borderColor color
        |]

    // NOTE crappy Rn doesn't support setting color for just one side, so these helper function may eventually confuse some users
    let borderBottom (width: int) (color: Color) : array<RawRnViewStyleRule> =
        [|
            borderBottomWidth width
            borderColor       color
        |]

    // NOTE crappy Rn doesn't support setting color for just one side, so these helper function may eventually confuse some users
    let borderLeft (width: int) (color: Color) : array<RawRnViewStyleRule> =
        [|
            borderLeftWidth width
            borderColor color
        |]

    // NOTE crappy Rn doesn't support setting color for just one side, so these helper function may eventually confuse some users
    let borderRight (width: int) (color: Color) : array<RawRnViewStyleRule> =
        [|
            borderRightWidth width
            borderColor color
        |]

    let paddingHV (h: int) (v: int) : array<RawRnFlexStyleRule> =
        [|
            paddingHorizontal h
            paddingVertical v
        |]

    let shadow (color: Color) (blur: int) (offsetXY: int * int) : array<RawRnViewStyleRule> =
        let (offsetX, offsetY) = offsetXY

        [|
            shadowColor  color
            shadowRadius blur
            shadowOffset { width = offsetX; height = offsetY }
            shadowOpacity 1

            #if !EGGSHELL_PLATFORM_IS_WEB
            // TODO: Do this inside Rn
            if Rn.Runtime.platform = Native NativePlatform.Android then
                let colorWithoutAlpha =
                    match color with
                    | Color.Rgba (r, g, b, _) -> Color.Rgb (r, g, b)
                    | Color.WhiteAlpha _      -> Color.White
                    | Color.BlackAlpha _      -> Color.Black
                    | this -> this

                shadowColor colorWithoutAlpha
                elevation (max 3 (blur / 2)) // Approximate elevation based on blur
            #endif
        |]