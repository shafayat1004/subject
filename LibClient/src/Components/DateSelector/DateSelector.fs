[<AutoOpen>]
module LibClient.Components.DateSelector

open System
open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Icons
open LibClient.ServiceInstances

open ReactXP.Components
open ReactXP.Styles

type DateOnly = System.DateOnly

module LC =
    module DateSelector =
        type Theme = {
            HeaderBackgroundColor: Color
            SelectedDateBackgroundColor: Color
        }

open LC.DateSelector

[<RequireQualifiedAccess>]
module private Helpers =
    let isPreviousMonthOutsideSelectableRange (firstDayOfCurrentMonth: DateOnly) (maybeMinDate: Option<DateOnly>) : bool =
        match maybeMinDate with
        | Some minDate ->
            let lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1)
            lastDayOfPreviousMonth < minDate
        | None -> false

    let isNextMonthOutsideSelectableRange (firstDayOfCurrentMonth: DateOnly) (maybeMaxDate: Option<DateOnly>) : bool =
        match maybeMaxDate with
        | Some maxDate ->
            let firstDayOfNextMonth = firstDayOfCurrentMonth.AddMonths(1)
            firstDayOfNextMonth > maxDate
        | None -> false

    let isOutsideSelectableRange (day: DateOnly) (maybeMinDate: Option<DateOnly>) (maybeMaxDate: Option<DateOnly>) : bool =
        match maybeMinDate, maybeMaxDate with
        | Some minDate, Some maxDate -> day < minDate || day > maxDate
        | Some minDate, None         -> day < minDate
        | None, Some maxDate         -> day > maxDate
        | None, None                 -> false

    let canSelectDate (date: DateOnly) (maybeCanSelectDate: Option<DateOnly -> bool>) : bool =
        match maybeCanSelectDate with
        | Some predicate -> predicate date
        | None -> true

    let buildRows (firstOfMonth: DateOnly) : List<List<DateOnly>> =
        let lastOfMonth    = firstOfMonth.AddMonths(1).AddDays(-1)
        let firstWeekStart = firstOfMonth.AddDays(- (int firstOfMonth.DayOfWeek))
        let lastWeekEnd    = lastOfMonth.AddDays(6 - (int lastOfMonth.DayOfWeek))
        let dayCount       =
            int (
                lastWeekEnd.ToDateTimeOffset(offset = DateTimeOffset.Now.Offset)
                    .Subtract(firstWeekStart.ToDateTimeOffset(offset = DateTimeOffset.Now.Offset))
                    .TotalDays
            )
        [0..dayCount]
        |> Seq.map firstWeekStart.AddDays
        |> Seq.chunkBySize 7
        |> Seq.map Seq.toList
        |> Seq.toList

    let initialMonthFirstDay (maybeSelected: Option<DateOnly>) : DateOnly =
        let date =
            maybeSelected
            |> Option.getOrElse (DateTimeOffset.Now.ToDateOnly())
        date.AddDays(1 - date.Day)

    let dayLabel (day: DateOnly) =
        sprintf "%A, %B %d, %d" day.DayOfWeek day.Month day.Day day.Year

    let dayTestId (testIdPrefix: string) (day: DateOnly) =
        sprintf "%s-day-%04d-%02d-%02d" testIdPrefix day.Year day.Month day.Day

[<RequireQualifiedAccess>]
module private Styles =
    let private dayWidth = 30

    let view =
        makeViewStyles {
            width    270
            minWidth (7 * dayWidth)
            height   330
        }

    let header =
        ViewStyles.Memoize (fun (headerBackgroundColor: Color) ->
            makeViewStyles {
                paddingTop        40
                paddingBottom     5
                paddingHorizontal 10
                backgroundColor   headerBackgroundColor
            }
        )

    let headerText =
        makeTextStyles {
            FontWeight.Bold
            fontSize 16
            color Color.White
        }

    let navigationControls =
        makeViewStyles {
            JustifyContent.SpaceBetween
            FlexDirection.Row
            AlignItems.Center
            marginVertical    10
            paddingHorizontal 10
        }

    let navigationControlsText =
        makeTextStyles {
            fontSize 12
            color (Color.Grey "33")
        }

    let arrowContainer =
        makeViewStyles {
            FlexDirection.Row
        }

    let iconButtonTheme (theme: LC.IconButton.Theme) : LC.IconButton.Theme =
        { theme with
            Actionable =
                { theme.Actionable with
                    IconColor = Color.Grey "99"
                }
        }

    let weekdayHeadersRow =
        makeViewStyles {
            FlexDirection.Row
            JustifyContent.SpaceBetween
        }

    let weekdayHeader =
        makeViewStyles {
            width  dayWidth
            height 15
        }

    let weekdayHeaderText =
        makeTextStyles {
            color (Color.Grey "33")
            fontSize 12
            TextAlign.Center
        }

    let row =
        makeViewStyles {
            FlexDirection.Row
            JustifyContent.SpaceBetween
        }

    let day =
        ViewStyles.Memoize (fun (selectedDateBackgroundColor: Color) (isSelected: bool) ->
            makeViewStyles {
                JustifyContent.Center
                Cursor.Pointer
                height dayWidth
                width  dayWidth

                if isSelected then
                    borderColor  (Color.Hex "#c5d7ff")
                    borderRadius (dayWidth / 2)
                    backgroundColor selectedDateBackgroundColor
            }
        )

    let dayOtherMonth =
        makeViewStyles {
            JustifyContent.Center
            height dayWidth
            width  dayWidth
            Cursor.Default
        }

    let dayText =
        TextStyles.Memoize (fun (isToday: bool) (isDisabled: bool) ->
            makeTextStyles {
                TextAlign.Center
                fontSize 12

                if isDisabled then
                    color (Color.Grey "cc")
                elif isToday then
                    FontWeight.Bold
                    color Color.Black
                else
                    color Color.Black
            }
        )

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member DateSelector(
            onChange: DateOnly -> unit,
            maybeSelected: Option<DateOnly>,
            ?minDate: DateOnly,
            ?maxDate: DateOnly,
            ?canSelectDate: DateOnly -> bool,
            ?theme: Theme -> Theme,
            ?testId: string,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme
        let testIdPrefix = defaultArg testId "date-selector"

        let monthFirstDayHook =
            Hooks.useState (Helpers.initialMonthFirstDay maybeSelected)

        let currentDateHook = Hooks.useState (services().Date.GetToday.ToDateOnly())

        Hooks.useEffectDisposable (
            (fun () ->
                let subscription =
                    services().Date.SubscribeToToday
                        (fun newToday -> currentDateHook.update (newToday.ToDateOnly()))

                { new IDisposable with
                    member _.Dispose() = subscription.Off ()
                }
            ),
            [||]
        )

        let rows = Helpers.buildRows monthFirstDayHook.current
        let currentMonthFirstDay = monthFirstDayHook.current

        let prevMonth (_e: ReactEvent.Action) =
            monthFirstDayHook.update (currentMonthFirstDay.AddMonths(-1))

        let nextMonth (_e: ReactEvent.Action) =
            monthFirstDayHook.update (currentMonthFirstDay.AddMonths(1))

        let weekdayHeaders =
            [|"S"; "M"; "T"; "W"; "T"; "F"; "S"|]
            |> Array.map (fun day ->
                RX.View(
                    styles = [| Styles.weekdayHeader |],
                    children =
                        elements {
                            LC.UiText(day, styles = [| Styles.weekdayHeaderText |])
                        }
                )
            )

        let dayCells (row: List<DateOnly>) =
            row
            |> List.map (fun day ->
                let isOtherMonth = day.Month <> currentMonthFirstDay.Month

                if isOtherMonth then
                    RX.View(styles = [| Styles.dayOtherMonth |])
                else
                    let canSelect =
                        not (Helpers.isOutsideSelectableRange day minDate maxDate)
                        && Helpers.canSelectDate day canSelectDate

                    let isSelected =
                        match maybeSelected with
                        | Some selected -> day = selected
                        | None -> false

                    let isToday = day = currentDateHook.current
                    let label = Helpers.dayLabel day

                    RX.View(
                        styles = [| Styles.day theTheme.SelectedDateBackgroundColor isSelected |],
                        children =
                            elements {
                                LC.UiText(
                                    string day.Day,
                                    styles = [| Styles.dayText isToday (not canSelect) |]
                                )

                                if canSelect then
                                    LC.Pressable(
                                        onPress = (fun _ -> onChange day),
                                        label = label,
                                        role = AccessibilityRole.Button,
                                        testId = Helpers.dayTestId testIdPrefix day,
                                        state =
                                            { AccessibilityStateRecord.empty with
                                                Selected = Some isSelected
                                            },
                                        overlay = true,
                                        componentName = "LC.DateSelector"
                                    )
                            }
                    )
            )
            |> List.toArray

        RX.View(
            testId = testIdPrefix,
            styles = [| Styles.view |],
            children =
                [|
                    RX.View(
                        styles = [| Styles.header theTheme.HeaderBackgroundColor |],
                        children =
                            elements {
                                match maybeSelected with
                                | Some selectedDate ->
                                    LC.Timestamp(
                                        value = LC.Timestamp.UniDateTime.Of selectedDate,
                                        format = "ddd, MMM dd",
                                        offset = DateTimeExtensions.bdTzOffset,
                                        styles = [| Styles.headerText |]
                                    )
                                | None ->
                                    LC.UiText("Select Day", styles = [| Styles.headerText |])
                            }
                    )

                    RX.View(
                        styles = [| Styles.navigationControls |],
                        children =
                            elements {
                                LC.Timestamp(
                                    value = LC.Timestamp.UniDateTime.Of currentMonthFirstDay,
                                    format = "MMMM yyyy",
                                    offset = DateTimeExtensions.bdTzOffset,
                                    styles = [| Styles.navigationControlsText |]
                                )

                                RX.View(
                                    styles = [| Styles.arrowContainer |],
                                    children =
                                        elements {
                                            LC.IconButton(
                                                label = "Previous month",
                                                testId = sprintf "%s-prev-month" testIdPrefix,
                                                icon = Icon.ChevronLeft,
                                                theme = Styles.iconButtonTheme,
                                                state =
                                                    ButtonHighLevelState.LowLevel (
                                                        if Helpers.isPreviousMonthOutsideSelectableRange currentMonthFirstDay minDate then
                                                            LC.IconButton.Disabled
                                                        else
                                                            LC.IconButton.Actionable prevMonth
                                                    )
                                            )

                                            LC.IconButton(
                                                label = "Next month",
                                                testId = sprintf "%s-next-month" testIdPrefix,
                                                icon = Icon.ChevronRight,
                                                theme = Styles.iconButtonTheme,
                                                state =
                                                    ButtonHighLevelState.LowLevel (
                                                        if Helpers.isNextMonthOutsideSelectableRange currentMonthFirstDay maxDate then
                                                            LC.IconButton.Disabled
                                                        else
                                                            LC.IconButton.Actionable nextMonth
                                                    )
                                            )
                                        }
                                )
                            }
                    )

                    RX.View(
                        styles = [| Styles.weekdayHeadersRow |],
                        children = weekdayHeaders
                    )

                    yield!
                        rows
                        |> List.map (fun row ->
                            RX.View(
                                styles = [| Styles.row |],
                                children = dayCells row
                            )
                        )
                        |> List.toArray
                |]
        )
