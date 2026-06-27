[<AutoOpen>]
module LibClient.Components.Dialog_Shell_FullScreen

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Components.Dialog.Base
open LibClient.Components.Dialog.Shell.FullScreen
open LibClient.Components.Legacy.TopNav.Base
open LibClient.Icons

open ReactXP.Components
open ReactXP.Styles

module LC =
    module Dialog =
        module Shell =
            module FullScreen =
                type Theme = {
                    TopNavHeight: int
                }

open LC.Dialog.Shell.FullScreen

[<RequireQualifiedAccess>]
module private FullScreenStyles =
    let defaultTopNavHeight = 44

    let wrapper =
        makeViewStyles {
            Position.Absolute
            FlexDirection.ColumnReverseZindexHack
            trbl            0 0 0 0
            backgroundColor Color.White
        }

    let children =
        makeViewStyles {
            flex 1
        }

    let scrollViewChildren =
        ViewStyles.Memoize(
            fun (maybeMinHeight: Option<int>) ->
                makeViewStyles {
                    flex 1

                    match maybeMinHeight with
                    | Some minimumHeight -> minHeight minimumHeight
                    | None               -> height 0
                }
        )

    let topNavView (theme: Theme) =
        makeViewStyles {
            backgroundColor Color.White
            height theme.TopNavHeight
        }

    let topNavHeading =
        makeTextStyles {
            color (Color.Grey "77")
        }

    let backButtonTestId = A11ySlug.testId "dialog-fullscreen" "back"

    module LegacyBridge =
        let viewStyles (xLegacyStyles: Option<List<ReactXP.LegacyStyles.RuntimeStyles>>) (className: string) : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let textStyles (xLegacyStyles: Option<List<ReactXP.LegacyStyles.RuntimeStyles>>) (className: string) : array<TextStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findApplicableStyles ls className with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<TextStyles> "ReactXP.Components.Text" styles |]
            | None -> [||]

type LibClient.Components.Constructors.LC.Dialog.Shell with
    [<Component>]
    static member FullScreen(
            ?children: ReactChildrenProp,
            ?backButton: BackButton,
            ?scroll: Scroll,
            ?heading: string,
            ?headerRight: ReactElement,
            ?bottomSection: ReactElement,
            ?theme: Theme -> Theme,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let backButton = defaultArg backButton BackButton.No
        let scroll = defaultArg scroll Scroll.Vertical
        let bottomSection = defaultArg bottomSection nothing
        let headerRight = defaultArg headerRight nothing
        let theTheme =
            Themes.GetMaybeUpdatedWith theme
            |> fun t ->
                if t.TopNavHeight <= 0 then { t with TopNavHeight = FullScreenStyles.defaultTopNavHeight }
                else t

        let showTopNav =
            heading.IsSome
            || match backButton with BackButton.Yes _ -> true | _ -> false

        let accessibilityLabel =
            heading |> Option.orElse (Some "Dialog")

        let dialogRole =
            accessibilityLabel |> Option.map (fun _ -> AccessibilityRole.Dialog)

        let childElements =
            children |> Option.defaultValue [||]

        LC.Dialog.Base(
            canClose = CanClose.Never,
            contentPosition = ContentPosition.Free,
            children =
                [|
                    RX.View(
                        styles =
                            [|
                                FullScreenStyles.wrapper
                                yield! FullScreenStyles.LegacyBridge.viewStyles xLegacyStyles "wrapper"
                            |],
                        ?accessibilityRole = dialogRole,
                        ?accessibilityLabel = accessibilityLabel,
                        children =
                            [|
                                bottomSection

                                LC.With.Layout(
                                    ``with`` =
                                        fun (onLayoutOption, maybeLayout) ->
                                            element {
                                            if scroll <> Scroll.NoScroll then
                                                RX.ScrollView(
                                                    horizontal = (scroll = Scroll.Both || scroll = Scroll.Horizontal),
                                                    vertical = (scroll = Scroll.Both || scroll = Scroll.Vertical),
                                                    ?onLayout = onLayoutOption,
                                                    children =
                                                        [|
                                                            RX.View(
                                                                styles =
                                                                    [|
                                                                        FullScreenStyles.scrollViewChildren (
                                                                            maybeLayout
                                                                            |> Option.map (fun layout -> layout.Height)
                                                                        )
                                                                        yield! FullScreenStyles.LegacyBridge.viewStyles xLegacyStyles "scroll-view-children"
                                                                    |],
                                                                children = childElements
                                                            )
                                                        |]
                                                )

                                            if scroll = Scroll.NoScroll then
                                                RX.View(
                                                    styles =
                                                        [|
                                                            FullScreenStyles.children
                                                            yield! FullScreenStyles.LegacyBridge.viewStyles xLegacyStyles "children"
                                                        |],
                                                    children = childElements
                                                )

                                            if showTopNav then
                                                LC.Legacy.TopNav.Base(
                                                    center = Center.Heading (heading |> Option.defaultValue ""),
                                                    left =
                                                        element {
                                                            match backButton.OnPressOption with
                                                            | None ->
                                                                LC.Legacy.TopNav.Filler()
                                                            | Some onPress ->
                                                                LC.IconButton(
                                                                    icon = Icon.Back,
                                                                    label = "Back",
                                                                    state =
                                                                        ButtonHighLevelState.LowLevel (
                                                                            ButtonLowLevelState.Actionable onPress
                                                                        ),
                                                                    theme =
                                                                        (fun iconTheme ->
                                                                            { iconTheme with
                                                                                Actionable =
                                                                                    { iconTheme.Actionable with
                                                                                        IconColor = Color.Grey "99"
                                                                                    }
                                                                            }
                                                                        ),
                                                                    testId = FullScreenStyles.backButtonTestId
                                                                )
                                                        },
                                                    right =
                                                        element {
                                                            if headerRight <> noElement then headerRight
                                                            else LC.Legacy.TopNav.Filler()
                                                        },
                                                    theme =
                                                        (fun topNavTheme ->
                                                            { topNavTheme with
                                                                Height = theTheme.TopNavHeight
                                                            }
                                                        ),
                                                    styles = [| FullScreenStyles.topNavView theTheme |],
                                                    headingStyles = [| FullScreenStyles.topNavHeading |],
                                                    ?xLegacyStyles = xLegacyStyles
                                                )
                                        }
                                )
                            |]
                    )
                |]
        )
