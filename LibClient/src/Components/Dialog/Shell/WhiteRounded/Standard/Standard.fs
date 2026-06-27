[<AutoOpen>]
module LibClient.Components.Dialog_Shell_WhiteRounded_Standard

open Fable.React

open LibClient
open LibClient.Components.Dialog.Shell.WhiteRounded.Standard
open LibClient.Responsive

module ShellStandard = LibClient.Components.Dialog.Shell.WhiteRounded.Standard

open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private StandardStyles =
    let dialogContents =
        ViewStyles.Memoize(
            fun (screenSize: ScreenSize) ->
                makeViewStyles {
                    flex      1
                    minWidth  200
                    minHeight 100

                    match screenSize with
                    | ScreenSize.Desktop ->
                        paddingHorizontal 40
                        paddingTop        40
                        paddingBottom     24
                    | ScreenSize.Handheld ->
                        padding 14
                }
        )

    let headingView =
        ViewStyles.Memoize(
            fun (screenSize: ScreenSize) ->
                makeViewStyles {
                    flex             0
                    marginHorizontal 20

                    match screenSize with
                    | ScreenSize.Desktop -> marginBottom 40
                    | ScreenSize.Handheld  -> marginBottom 22
                }
        )

    let headingText =
        TextStyles.Memoize(
            fun (screenSize: ScreenSize) ->
                makeTextStyles {
                    match screenSize with
                    | ScreenSize.Desktop -> fontSize 32
                    | ScreenSize.Handheld  -> fontSize 20
                }
        )

    let scrollView =
        makeScrollViewStyles {
            flex 1
        }

    let body =
        makeViewStyles {
            flex 1
        }

    let buttons =
        makeViewStyles {
            FlexDirection.Row
            JustifyContent.Center
            marginTop 40
        }

    let inProgress =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            backgroundColor (Color.WhiteAlpha 0.5)
        }

    let error =
        makeTextStyles {
            TextAlign.Center
            marginTop 20
            color     Color.DevRed
        }

    module LegacyBridge =
        let viewStyles (xLegacyStyles: Option<List<ReactXP.LegacyStyles.RuntimeStyles>>) (className: string) : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let scrollViewStyles (xLegacyStyles: Option<List<ReactXP.LegacyStyles.RuntimeStyles>>) (className: string) : array<ScrollViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ScrollViewStyles> "ReactXP.Components.ScrollView" styles |]
            | None -> [||]

        let textStyles (xLegacyStyles: Option<List<ReactXP.LegacyStyles.RuntimeStyles>>) (className: string) : array<TextStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<TextStyles> "ReactXP.Components.Text" styles |]
            | None -> [||]

type LibClient.Components.Constructors.LC.Dialog.Shell.WhiteRounded with
    [<Component>]
    static member Standard(
            canClose: CanClose,
            ?children: ReactChildrenProp,
            ?body: ReactElement,
            ?buttons: ReactElement,
            ?mode: ShellStandard.Mode,
            ?heading: string,
            ?accessibilityLabel: string,
            ?key: string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        children |> ignore

        let body = defaultArg body noElement
        let buttons = defaultArg buttons noElement
        let mode = defaultArg mode ShellStandard.Mode.Default

        let accessibilityLabel =
            accessibilityLabel
            |> Option.orElseWith (fun () -> heading |> Option.orElse (Some "Dialog"))

        let stopPropagation (e: Browser.Types.PointerEvent) = e.stopPropagation()

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    LC.Dialog.Shell.WhiteRounded.Raw(
                        canClose = canClose,
                        ?accessibilityLabel = accessibilityLabel,
                        children =
                            [|
                                RX.View(
                                    onPress = stopPropagation,
                                    styles =
                                        [|
                                            StandardStyles.dialogContents screenSize
                                            yield! StandardStyles.LegacyBridge.viewStyles xLegacyStyles "dialog-contents"
                                        |],
                                    children =
                                        [|
                                            match heading with
                                            | None -> nothing
                                            | Some headingText ->
                                                RX.View(
                                                    styles =
                                                        [|
                                                            StandardStyles.headingView screenSize
                                                            yield! StandardStyles.LegacyBridge.viewStyles xLegacyStyles "heading"
                                                        |],
                                                    children =
                                                        [|
                                                            LC.UiText(
                                                                value = headingText,
                                                                styles =
                                                                    [|
                                                                        StandardStyles.headingText screenSize
                                                                        yield! StandardStyles.LegacyBridge.textStyles xLegacyStyles "heading"
                                                                    |]
                                                            )
                                                        |]
                                                )

                                            RX.ScrollView(
                                                vertical = true,
                                                styles =
                                                    [|
                                                        StandardStyles.scrollView
                                                        yield! StandardStyles.LegacyBridge.scrollViewStyles xLegacyStyles "scroll-view"
                                                    |],
                                                children =
                                                    [|
                                                        RX.View(
                                                            styles =
                                                                [|
                                                                    StandardStyles.body
                                                                    yield! StandardStyles.LegacyBridge.viewStyles xLegacyStyles "body"
                                                                |],
                                                            children = [| body |]
                                                        )

                                                        match mode with
                                                        | ShellStandard.Mode.Error message ->
                                                            RX.View(
                                                                styles =
                                                                    [|
                                                                        yield! StandardStyles.LegacyBridge.viewStyles xLegacyStyles "error"
                                                                    |],
                                                                children =
                                                                    [|
                                                                        LC.UiText(
                                                                            value = message,
                                                                            styles =
                                                                                [|
                                                                                    StandardStyles.error
                                                                                    yield! StandardStyles.LegacyBridge.textStyles xLegacyStyles "error"
                                                                                |]
                                                                        )
                                                                    |]
                                                            )
                                                        | _ -> nothing

                                                        if buttons <> noElement then
                                                            RX.View(
                                                                styles =
                                                                    [|
                                                                        StandardStyles.buttons
                                                                        yield! StandardStyles.LegacyBridge.viewStyles xLegacyStyles "buttons"
                                                                    |],
                                                                children = [| buttons |]
                                                            )
                                                    |]
                                            )

                                            if mode = ShellStandard.Mode.InProgress then
                                                RX.View(
                                                    styles =
                                                        [|
                                                            StandardStyles.inProgress
                                                            yield! StandardStyles.LegacyBridge.viewStyles xLegacyStyles "in-progress"
                                                        |]
                                                )
                                        |]
                                )
                            |]
                    )
        )
