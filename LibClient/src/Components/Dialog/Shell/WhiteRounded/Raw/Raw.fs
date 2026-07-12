[<AutoOpen>]
module LibClient.Components.Dialog_Shell_WhiteRounded_Raw

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Browser.Types

open LibClient
open LibClient.Accessibility
open LibClient.Components.Dialog.Base
open LibClient.Components.Dialog.Shell.WhiteRounded.Raw
open LibClient.Icons
open LibClient.Responsive

open Rn.Components
open Rn.Styles

module LC =
    module Dialog =
        module Shell =
            module WhiteRounded =
                module Raw =
                    type Theme = {
                        Width:                   Option<int>
                        MaxSizeLimiterPadding:   Option<int>
                        WhiteRoundedBasePadding: Option<int>
                        BoundaryRadius:          Option<int * int * int * int>
                        BackgroundColor:         Option<Color>
                    }

open LC.Dialog.Shell.WhiteRounded.Raw

[<Emit("undefined")>]
let private undefinedOnPress: PointerEvent -> unit = jsNative

[<RequireQualifiedAccess>]
module private RawStyles =
    let private defaultMaxSizeLimiterPadding = 20
    let private defaultWhiteRoundedBasePadding = 20

    let maxSizeLimiter =
        ViewStyles.Memoize(
            fun (position: DialogPosition) (theme: Theme) ->
                let limiterPadding =
                    theme.MaxSizeLimiterPadding
                    |> Option.defaultValue defaultMaxSizeLimiterPadding

                makeViewStyles {
                    AlignItems.Center
                    AlignContent.Center
                    AlignSelf.Stretch
                    flex    1
                    padding limiterPadding

                    match position with
                    | DialogPosition.Top    -> JustifyContent.FlexStart
                    | DialogPosition.Center -> JustifyContent.Center
                    | DialogPosition.Bottom -> JustifyContent.FlexEnd
                }
        )

    let whiteRoundedBase =
        ViewStyles.Memoize(
            fun (screenSize: ScreenSize) (theme: Theme) ->
                let basePadding =
                    theme.WhiteRoundedBasePadding
                    |> Option.defaultValue defaultWhiteRoundedBasePadding

                makeViewStyles {
                    backgroundColor (
                        theme.BackgroundColor
                        |> Option.defaultValue Color.White
                    )
                    padding         basePadding
                    flex            -1

                    match screenSize with
                    | ScreenSize.Desktop ->
                        borderRadius 10
                    | ScreenSize.Handheld ->
                        borderRadius 16
                        AlignSelf.Stretch

                    match theme.Width with
                    | Some dialogWidth ->
                        width dialogWidth
                        AlignSelf.Auto
                    | None -> ()

                    match theme.BoundaryRadius with
                    | Some (topLeft, topRight, bottomLeft, bottomRight) ->
                        borderTopLeftRadius     topLeft
                        borderTopRightRadius    topRight
                        borderBottomLeftRadius  bottomLeft
                        borderBottomRightRadius bottomRight
                    | None -> ()
                }
        )

    let content =
        makeViewStyles {
            Overflow.Hidden
            flex 1
        }

    let inProgress =
        makeViewStyles {
            Position.Absolute
            JustifyContent.Center
            AlignItems.Center
            trbl            0 0 0 0
            backgroundColor (Color.WhiteAlpha 0.7)
        }

    let closeButton =
        ViewStyles.Memoize(
            fun (theme: Theme) ->
                makeViewStyles {
                    backgroundColor (
                        theme.BackgroundColor
                        |> Option.defaultValue Color.White
                    )
                    borderRadius 100
                    width 42
                    height 42
                    Position.Absolute
                    top 2
                    right 2
                }
        )

    let closeButtonTheme (theme: LC.IconButton.Theme): LC.IconButton.Theme =
        { theme with
            Actionable =
                { theme.Actionable with
                    IconColor = Color.Grey "50"
                    IconSize  = 22
                }
        }

    let closeButtonTestId = A11ySlug.testId "dialog-shell" "close"

    module LegacyBridge =
        let viewStyles (xLegacyStyles: Option<List<Rn.LegacyStyles.RuntimeStyles>>) (className: string) : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match Rn.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

type LibClient.Components.Constructors.LC.Dialog.Shell.WhiteRounded with
    [<Component>]
    static member Raw(
            canClose:            CanClose,
            ?children:           ReactChildrenProp,
            ?position:           DialogPosition,
            ?inProgress:         bool,
            ?accessibilityLabel: string,
            ?theme:              Theme -> Theme,
            ?xLegacyStyles:      List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:                string
        ) : ReactElement =
        key |> ignore

        let position = defaultArg position DialogPosition.Center
        let inProgress = defaultArg inProgress false
        let theTheme = Themes.GetMaybeUpdatedWith theme

        let panelOnPress =
            if Rn.Runtime.isNative() then undefinedOnPress
            else fun (e: PointerEvent) -> e.stopPropagation()

        let dialogRole =
            accessibilityLabel |> Option.map (fun _ -> AccessibilityRole.Dialog)

        let childElements =
            children |> Option.defaultValue [||]

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    let contentPosition =
                        ContentPosition.Center

                    LC.Dialog.Base(
                        canClose        = canClose,
                        contentPosition = contentPosition,
                        children =
                            [|
                                Rn.View(
                                    styles =
                                        [|
                                            RawStyles.maxSizeLimiter position theTheme
                                            yield! RawStyles.LegacyBridge.viewStyles xLegacyStyles "max-size-limiter"
                                        |],
                                    children =
                                        [|
                                            Rn.View(
                                                onPress = panelOnPress,
                                                styles =
                                                    [|
                                                        RawStyles.whiteRoundedBase screenSize theTheme
                                                        yield! RawStyles.LegacyBridge.viewStyles xLegacyStyles "white-rounded-base"
                                                    |],
                                                children =
                                                    elements {
                                                        Rn.View(
                                                            styles =
                                                                [|
                                                                    RawStyles.content
                                                                    yield! RawStyles.LegacyBridge.viewStyles xLegacyStyles "content"
                                                                |],
                                                            children = childElements
                                                        )

                                                        if inProgress then
                                                            Rn.View(
                                                                styles =
                                                                    [|
                                                                        RawStyles.inProgress
                                                                        yield! RawStyles.LegacyBridge.viewStyles xLegacyStyles "in-progress"
                                                                    |],
                                                                children =
                                                                    [|
                                                                        Rn.ActivityIndicator(
                                                                            color = (Color.Grey "cc").ToRnString
                                                                        )
                                                                    |]
                                                            )

                                                        if canClose.ShouldShowCloseButton then
                                                            LC.IconButton(
                                                                styles = [| RawStyles.closeButton theTheme |],
                                                                icon   = Icon.X,
                                                                label  = "Close",
                                                                state =
                                                                    ButtonHighLevelState.LowLevel (
                                                                        ButtonLowLevelState.Actionable canClose.OnClose
                                                                    ),
                                                                theme  = RawStyles.closeButtonTheme,
                                                                testId = RawStyles.closeButtonTestId
                                                            )
                                                    },
                                                ?accessibilityRole  = dialogRole,
                                                ?accessibilityLabel = accessibilityLabel
                                            )
                                        |]
                                )
                            |]
                    )
        )
