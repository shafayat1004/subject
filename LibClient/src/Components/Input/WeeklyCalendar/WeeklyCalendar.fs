namespace LibClient.Components.Input

open System

module WeeklyCalendar =

    let generateWeek (today: Date) : seq<Date> =
        seq { for i in 0 .. 6 -> Date.addDays i today }


namespace LibClient.Components

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Components.Input.WeeklyCalendar
open LibClient.Responsive
open LibClient.ServiceInstances

open ReactXP.Components
open ReactXP.Styles

[<AutoOpen>]
module Input_WeeklyCalendarComponent =

    module LC =
        module Input =
            module WeeklyCalendar =
                type Theme = {
                    DayOfWeekText: Color
                    DateTextUnavailable: Color
                    DateTextAvailable: Color
                    DateTextSelected: Color
                    Circle: Color
                    InvalidReason: Color
                }

    open LC.Input.WeeklyCalendar

    [<RequireQualifiedAccess>]
    module private Styles =
        let private dayWidthDesktop = 54
        let private dayWidthHandheld = 47

        let view = makeViewStyles { flex 1; FlexDirection.Row; AlignItems.Center }
        let weeklyCalendarContainer = makeViewStyles { minWidth 275; flexShrink 1 }

        let weeklyCalendar (screenSize: ScreenSize) =
            makeViewStyles {
                FlexDirection.Row; Overflow.VisibleForScrolling; marginTop 20
                if screenSize = ScreenSize.Desktop then JustifyContent.Center
            }

        let day (screenSize: ScreenSize) =
            makeViewStyles {
                FlexDirection.Column; AlignItems.Center; height 60
                Overflow.VisibleForTapCapture
                width (if screenSize = ScreenSize.Desktop then dayWidthDesktop else dayWidthHandheld)
            }

        let dayOfWeek (theme: Theme) = makeTextStyles { FontWeight.Bold; fontSize 16; color theme.DayOfWeekText }
        let date = makeViewStyles { AlignSelf.Stretch; FlexDirection.Column; JustifyContent.Center; AlignItems.Center; height 30 }

        let dateText (theme: Theme) (isSelected: bool) =
            makeTextStyles { FontWeight.Bold; fontSize 16; color (if isSelected then theme.DateTextSelected else theme.DateTextAvailable) }

        let circle (theme: Theme) (screenSize: ScreenSize) =
            let dayWidth = if screenSize = ScreenSize.Desktop then dayWidthDesktop else dayWidthHandheld
            let size = 30
            makeViewStyles {
                Position.Absolute; height size; width size; top 0; left ((dayWidth - size) / 2); borderRadius (size / 2)
                backgroundColor theme.Circle
                if screenSize = ScreenSize.Handheld then Overflow.Hidden
            }

        let label = makeViewStyles { marginBottom 4 }
        let labelText (theme: Theme) (isInvalid: bool) = makeTextStyles { if isInvalid then color theme.InvalidReason }
        let invalidReason (theme: Theme) = makeTextStyles { fontSize 12; color theme.InvalidReason }

    let private legacyTopLevelStyles xLegacyStyles =
        match xLegacyStyles with
        | Some ls ->
            match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
            | [] -> [||]
            | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
        | None -> [||]

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member WeeklyCalendar(
                value: Set<Date>,
                onChange: Set<Date> -> unit,
                validity: InputValidity,
                ?label: string,
                ?startDate: Date,
                ?styles: array<ViewStyles>,
                ?theme: Theme -> Theme,
                ?children: ReactChildrenProp,
                ?key: string,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            children |> ignore
            key |> ignore

            let theTheme = Themes.GetMaybeUpdatedWith theme
            let initialStartDate = startDate |> Option.defaultWith (fun () -> services().Date.GetNow |> Date.ofDateTimeOffset)
            let startDateHook = Hooks.useState initialStartDate

            Hooks.useEffect((fun () -> startDate |> Option.sideEffect (fun d -> startDateHook.update d)), [| startDate :> obj |])

            LC.With.ScreenSize(``with`` = fun screenSize ->
                RX.View(
                    styles = [| Styles.view; yield! legacyTopLevelStyles xLegacyStyles; yield! defaultArg styles [||] |],
                    children = [|
                        RX.View(styles = [| Styles.weeklyCalendarContainer |], children = [|
                            match label with
                            | Some labelText ->
                                RX.View(styles = [| Styles.label |], children = [|
                                    LC.LegacyText(styles = [| Styles.labelText theTheme validity.IsInvalid |],
                                        children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") labelText |])
                                |])
                            | None when validity = InputValidity.Missing ->
                                RX.View(styles = [| Styles.label |], children = [|
                                    LC.LegacyText(styles = [| Styles.labelText theTheme true |],
                                        children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") "Required" |])
                                |])
                            | None -> noElement

                            RX.ScrollView(horizontal = true, children = [|
                                RX.View(
                                    styles = [| Styles.weeklyCalendar screenSize |],
                                    children =
                                        (generateWeek startDateHook.current
                                         |> Seq.map (fun day ->
                                            let isSelected = Set.contains day value
                                            let dayName = unionCaseName day.DayOfTheWeek
                                            let dayLabel = sprintf "%s %i" dayName day.Day
                                            RX.View(styles = [| Styles.day screenSize |], children = [|
                                                LC.LegacyText(styles = [| Styles.dayOfWeek theTheme |], children = [|
                                                    makeTextNode2 (Some "LibClient.Components.LegacyText") (dayName.Substring(0, 3))
                                                |])
                                                RX.View(styles = [| Styles.date |], children = [|
                                                    if isSelected then RX.View(styles = [| Styles.circle theTheme screenSize |]) else noElement
                                                    LC.UiText(value = string day.Day, styles = [| Styles.dateText theTheme isSelected |])
                                                |])
                                                LC.Pressable(
                                                    onPress = (fun _ -> onChange (value.Toggle day)),
                                                    label = dayLabel,
                                                    testId = A11ySlug.testId "weekly-calendar" dayLabel,
                                                    role = AccessibilityRole.Button,
                                                    state = { AccessibilityStateRecord.empty with Selected = Some isSelected },
                                                    overlay = true,
                                                    componentName = "LC.Input.WeeklyCalendar"
                                                )
                                            |])
                                         )
                                         |> Array.ofSeq)
                                )
                            |])

                            validity.InvalidReason |> Option.map (fun reason -> RX.View(children = [|
                                LC.LegacyText(styles = [| Styles.invalidReason theTheme |],
                                    children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") reason |])
                            |])) |> Option.getOrElse noElement
                        |])
                    |]
                )
            )
