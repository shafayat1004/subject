[<AutoOpen>]
module AppTodo.Components.Route_Todos

open System
open Fable.Core.JsInterop
open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Input
open LibClient.Components.Input_Picker
open LibClient.Icons
open LibClient.Responsive
open LibUiSubject
open LibUiSubject.Components.Constructors
open LibUiSubject.Components.With.Subjects
open LibClient.Services.Subscription
open ReactXP.Components
open ReactXP.Styles
open ReactXP.Styles.Animation
open AppTodo.Actions
open AppTodo.Colors
open AppTodo.I18nGlobal
open AppTodo.TodoDisplay
open AppTodo.TodoQueries
open AppTodo.TodoTheme
open SuiteTodo.Types

module private AppearanceStorage =
    let private storageKey = "apptodo-appearance"

    let loadAsync () : Async<AppearanceMode> =
        async {
            let! stored = AppTodo.AppServices.services().LocalStorage.Get storageKey Json.FromString<string>
            return
                match stored with
                | Some "dark" -> AppearanceMode.Dark
                | _ -> AppearanceMode.Light
        }

    let save (mode: AppearanceMode) : unit =
        let value =
            match mode with
            | AppearanceMode.Light -> "light"
            | AppearanceMode.Dark -> "dark"
        async {
            do! AppTodo.AppServices.services().LocalStorage.Put storageKey value Json.ToString<string>
        }
        |> startSafely

module private SwipeGesture =
    let deleteWidth = Styles.swipeDeleteWidth
    let openThreshold = 40
    let fullDeleteRatio = 0.5
    /// Ignore micro-movements; real swipe must exceed this before we track or snap open.
    let activationThreshold = 10

    let currentOrLastDatafulGestureState
            (maybeLastGestureStateRef: IRefValue<Option<ReactXP.Components.GestureView.PanGestureState>>)
            (current: ReactXP.Components.GestureView.PanGestureState)
            : ReactXP.Components.GestureView.PanGestureState =
        if current.isComplete && current.isTouch && isNullOrUndefined current.pageX then
            match maybeLastGestureStateRef.current with
            | Some last ->
                if last.initialPageX = current.initialPageX && last.initialPageY = current.initialPageY then
                    LibClient.JsInterop.extendRecord
                        [
                            "pageX" ==> last.pageX
                            "pageY" ==> last.pageY
                        ]
                        current
                else
                    current
            | None -> current
        else
            maybeLastGestureStateRef.current <- Some current
            current

    let clampOffset rowWidth offset =
        max (-rowWidth) (min 0 offset)

    let panDelta (gs: ReactXP.Components.GestureView.PanGestureState) =
        int gs.pageX - int gs.initialPageX

module private ThemeToggleGesture =
    let trackPadding = 4
    let activationThreshold = 8
    /// Fixed track width keeps Light/Dark labels in equal halves (GestureView otherwise shrink-wraps text).
    let trackWidth = Styles.themeToggleWidth

    let segmentWidth (trackWidthPx: int) =
        max 0 ((trackWidthPx - trackPadding * 2) / 2)

    let clampOffset segmentWidth offset =
        max 0 (min segmentWidth offset)

    let modeForOffset segmentWidth offset =
        if segmentWidth <= 0 then
            AppearanceMode.Light
        elif offset >= segmentWidth / 2 then
            AppearanceMode.Dark
        else
            AppearanceMode.Light

    let targetOffset (mode: AppearanceMode) segmentWidth =
        match mode with
        | AppearanceMode.Light -> 0
        | AppearanceMode.Dark -> segmentWidth

type private Helpers =
    [<Component>]
    static member FieldLabel(palette: SemanticPalette, text: string) : ReactElement =
        LC.Text(styles = [| Styles.fieldLabel palette |], value = text)

    [<Component>]
    static member FilterTabs(
            palette: SemanticPalette,
            tabTheme: TabTheme,
            current: TodoListFilter,
            isHandheld: bool,
            onSelect: TodoListFilter -> unit)
        : ReactElement =
        let mkTab (filter: TodoListFilter) (testSuffix: string) =
            let label = filterLabel filter
            let testId = A11ySlug.testId "todo-filter" testSuffix
            let tab =
                LC.Tab(
                    label = label,
                    state =
                        (if current = filter then
                            LC.Tab.Selected
                         else
                            LC.Tab.Unselected (fun _ -> onSelect filter)),
                    theme = (fun _ -> Styles.filterTabTheme tabTheme),
                    testId = testId
                )

            if isHandheld then
                RX.View(styles = [| Styles.filterTabCell |], children = [| tab |])
            else
                tab

        let tabItems = [|
            mkTab TodoListFilter.Open "open"
            mkTab TodoListFilter.Done "done"
            mkTab TodoListFilter.All "all"
            mkTab TodoListFilter.Archived "archived"
        |]

        if isHandheld then
            RX.View(
                styles = [| Styles.filterTabsRow palette |],
                accessibilityRole = AccessibilityRole.TabList,
                accessibilityLabel = i18n.t.FilterTabsLabel,
                children =
                    elements {
                        tellReactArrayKeysAreOkay tabItems
                    }
            )
        else
            LC.Tabs(
                label = i18n.t.FilterTabsLabel,
                theme = (fun _ -> Styles.tabsScrollTheme tabTheme),
                children = tabItems
            )

    [<Component>]
    static member CategoryPill(
            bg: Color,
            border: Color,
            label: string,
            isSelected: bool,
            testId: string,
            onPress: ReactEvent.Action -> unit)
        : ReactElement =
        RX.View(
            styles = [| Styles.categoryPill bg border isSelected |],
            children = [|
                LC.TextButton(
                    label = label,
                    role = AccessibilityRole.Radio,
                    accessibilityState = AccessibilityStateRecord.selected isSelected,
                    state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable onPress),
                    testId = testId
                )
            |]
        )

    [<Component>]
    static member LanguageToggle() : ReactElement =
        let nextLanguage =
            match i18n.CurrentLanguage with
            | LibClient.I18n.Language.En -> LibClient.I18n.Language.Bn
            | LibClient.I18n.Language.Bn -> LibClient.I18n.Language.En

        let label =
            match nextLanguage with
            | LibClient.I18n.Language.En -> i18n.t.LanguageEn
            | LibClient.I18n.Language.Bn -> i18n.t.LanguageBn

        LC.TextButton(
            label = label,
            state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> AppTodo.I18nGlobal.setLanguage nextLanguage)),
            testId = A11ySlug.testId "todo" "language-toggle"
        )

    [<Component>]
    static member ThemeToggle(
            palette: SemanticPalette,
            current: AppearanceMode,
            onSelect: AppearanceMode -> unit)
        : ReactElement =
        let segmentWidthRef = Hooks.useRef 0
        let translateXRef = Hooks.useRef (AnimatedValue.Create 0.0)
        let maybeAnimationRef: IRefHook<Option<Animation>> = Hooks.useRef None
        let restOffsetRef = Hooks.useRef 0
        let isDraggingHook = Hooks.useState false
        let panStartBaseRef = Hooks.useRef 0
        let gestureActiveRef = Hooks.useRef false
        let lastDragOffsetRef = Hooks.useRef 0
        let lastGestureStateRef = Hooks.useRef None
        let thumbInitializedRef = Hooks.useRef false

        let stopAnimation () =
            maybeAnimationRef.current
            |> Option.iter (fun animation ->
                maybeAnimationRef.current <- None
                animation.Stop())

        let animateTo (target: int) (onComplete: Option<unit -> unit>) =
            stopAnimation ()
            let animation =
                Animation.Timing(
                    translateXRef.current,
                    toValue = double target,
                    duration = TimeSpan.FromMilliseconds 220
                )
            maybeAnimationRef.current <- Some animation
            animation.Start (fun () ->
                if maybeAnimationRef.current = Some animation then
                    maybeAnimationRef.current <- None
                    restOffsetRef.current <- target
                    onComplete |> Option.iter (fun fn -> fn ()))

        let syncThumbToCurrent () =
            let target = ThemeToggleGesture.targetOffset current segmentWidthRef.current
            translateXRef.current.SetValue (double target)
            restOffsetRef.current <- target

        let selectMode (mode: AppearanceMode) =
            if mode <> current then
                animateTo (ThemeToggleGesture.targetOffset mode segmentWidthRef.current) None
                onSelect mode

        Hooks.useEffect(
            (fun () ->
                segmentWidthRef.current <- ThemeToggleGesture.segmentWidth ThemeToggleGesture.trackWidth
                if not thumbInitializedRef.current then
                    syncThumbToCurrent ()
                    thumbInitializedRef.current <- true
                ()),
            [| |]
        )

        Hooks.useEffect(
            (fun () ->
                if not isDraggingHook.current && thumbInitializedRef.current && Option.isNone maybeAnimationRef.current then
                    let target = ThemeToggleGesture.targetOffset current segmentWidthRef.current
                    if restOffsetRef.current <> target then
                        animateTo target None
                ()),
            [| current |]
        )

        let settleFromOffset (offset: int) =
            let segW = segmentWidthRef.current
            let mode = ThemeToggleGesture.modeForOffset segW offset
            let target = ThemeToggleGesture.targetOffset mode segW
            animateTo target None
            if mode <> current then
                onSelect mode

        let onPanHorizontal (rawGs: ReactXP.Components.GestureView.PanGestureState) =
            let gs = SwipeGesture.currentOrLastDatafulGestureState lastGestureStateRef rawGs
            let delta = SwipeGesture.panDelta gs
            let segW = segmentWidthRef.current

            if segW <= 0 then
                ()
            elif gs.isComplete then
                let wasActive = gestureActiveRef.current
                gestureActiveRef.current <- false
                isDraggingHook.update false

                if not wasActive && abs delta < ThemeToggleGesture.activationThreshold then
                    animateTo restOffsetRef.current None
                else
                    let offset =
                        if wasActive then
                            lastDragOffsetRef.current
                        else
                            ThemeToggleGesture.clampOffset segW (panStartBaseRef.current + delta)

                    settleFromOffset offset

                lastGestureStateRef.current <- None
            elif abs delta < ThemeToggleGesture.activationThreshold then
                ()
            else
                if not gestureActiveRef.current then
                    gestureActiveRef.current <- true
                    panStartBaseRef.current <- restOffsetRef.current

                isDraggingHook.update true
                let offset = ThemeToggleGesture.clampOffset segW (panStartBaseRef.current + delta)
                lastDragOffsetRef.current <- offset
                translateXRef.current.SetValue (double offset)

        let segment (label: string) (mode: AppearanceMode) (testSuffix: string) =
            let isActive = current = mode
            let segmentColor =
                if not isActive then
                    palette.TextSecondary
                elif current = AppearanceMode.Dark then
                    palette.PageBackground
                else
                    Color.White

            RX.View(
                styles = [| Styles.themeSegment |],
                children = [|
                    LC.TextButton(
                        label = label,
                        role = AccessibilityRole.Radio,
                        accessibilityState = AccessibilityStateRecord.selected isActive,
                        styles = [| Styles.themeSegmentText segmentColor |],
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> selectMode mode)),
                        testId = A11ySlug.testId "todo" ("theme-" + testSuffix)
                    )
                |]
            )

        LC.RadioGroup(
            label = i18n.t.ThemeGroupLabel,
            testId = A11ySlug.testId "todo" "theme-toggle",
            children =
                tellReactArrayKeysAreOkay [|
                    LC.With.ReducedMotion (fun reduceMotion ->
                        let segW = ThemeToggleGesture.segmentWidth ThemeToggleGesture.trackWidth
                        let thumbOffset = ThemeToggleGesture.targetOffset current segW

                        RX.View(
                            styles = [| Styles.themeToggleTrack palette; Styles.themeToggleHost |],
                            children = [|
                                if segW > 0 then
                                    if reduceMotion then
                                        RX.View(
                                            importantForAccessibility = LibClient.Accessibility.ImportantForAccessibility.No,
                                            styles = [| Styles.themeToggleThumbStatic palette segW thumbOffset |],
                                            children = [||]
                                        )
                                    else
                                        RX.AnimatableView(
                                            styles =
                                                [|
                                                    Styles.themeToggleThumbAnimated
                                                        (AnimatableValue.Value translateXRef.current)
                                                        palette
                                                        segW
                                                |],
                                            children = [||]
                                        )

                                RX.GestureView(
                                    preferredPan = ReactXP.Components.GestureView.PreferredPanGesture.Horizontal,
                                    panPixelThreshold = float ThemeToggleGesture.activationThreshold,
                                    onPanHorizontal = (if reduceMotion then (fun _ -> ()) else onPanHorizontal),
                                    styles = [| Styles.themeToggleGestureHost |],
                                    children =
                                        [|
                                            RX.View(
                                                styles = [| Styles.themeToggleSegments |],
                                                children =
                                                    tellReactArrayKeysAreOkay [|
                                                        segment i18n.t.ThemeLight AppearanceMode.Light "light"
                                                        segment i18n.t.ThemeDark AppearanceMode.Dark "dark"
                                                    |]
                                            )
                                        |]
                                )
                            |]
                        )
                    )
                |]
        )

    [<Component>]
    static member NewCategoryPicker(
            palette: SemanticPalette,
            selected: Option<TodoCategory>,
            onSelect: Option<TodoCategory> -> unit)
        : ReactElement =
        let pill (noneOrCategory: Option<TodoCategory>) (label: string) (testId: string) (isSelected: bool) (onPress: ReactEvent.Action -> unit) =
            let bg, border, _ = Styles.categoryChipColorsByCategory palette noneOrCategory
            Helpers.CategoryPill(
                bg = bg,
                border = border,
                label = label,
                isSelected = isSelected,
                testId = testId,
                onPress = onPress
            )

        let pills =
            tellReactArrayKeysAreOkay [|
                pill None i18n.t.CategoryNone (A11ySlug.testId "todo" "new-category-none") selected.IsNone (fun _ -> onSelect None)
                yield!
                    allCategories
                    |> List.map (fun category ->
                        pill
                            (Some category)
                            (categoryLabel category)
                            (A11ySlug.testId "todo" ("new-category-" + (categoryLabel category).ToLower()))
                            (selected = Some category)
                            (fun _ -> onSelect (Some category)))
            |]

        LC.RadioGroup(
            label = sprintf "%s. %s" i18n.t.CategoryGroupLabel i18n.t.CategoryScrollHint,
            testId = A11ySlug.testId "todo" "new-category-group",
            children = [|
                LC.ScrollView(
                    scroll = LibClient.Components.ScrollView.Scroll.Horizontal,
                    restoreScroll = LibClient.Components.ScrollView.RestoreScroll.No,
                    showsHorizontalScrollIndicatorOnNative = true,
                    styles = [| Styles.categoryScrollContent |],
                    children = pills
                )
            |]
        )

    [<Component>]
    static member TodoSwipeShell(
            todo: Todo,
            palette: SemanticPalette,
            isOpen: bool,
            onOpenChange: bool -> unit,
            onDelete: unit -> unit,
            rowContent: ReactElement)
        : ReactElement =
        let deleteWidth = SwipeGesture.deleteWidth
        let openThreshold = SwipeGesture.openThreshold

        let isDraggingHook = Hooks.useState false
        let rowWidthRef = Hooks.useRef 300
        let restOffsetRef = Hooks.useRef 0
        let translateXRef = Hooks.useRef (AnimatedValue.Create 0.0)
        let maybeAnimationRef: IRefHook<Option<Animation>> = Hooks.useRef None
        let panStartBaseRef = Hooks.useRef 0
        let gestureActiveRef = Hooks.useRef false
        let lastDragOffsetRef = Hooks.useRef 0
        let lastGestureStateRef = Hooks.useRef None

        let stopAnimation () =
            maybeAnimationRef.current
            |> Option.iter (fun animation ->
                maybeAnimationRef.current <- None
                animation.Stop())

        let animateTo (target: int) (onComplete: Option<unit -> unit>) =
            stopAnimation ()
            let animation =
                Animation.Timing(
                    translateXRef.current,
                    toValue = double target,
                    duration = TimeSpan.FromMilliseconds 220
                )
            maybeAnimationRef.current <- Some animation
            animation.Start (fun () ->
                if maybeAnimationRef.current = Some animation then
                    maybeAnimationRef.current <- None
                    restOffsetRef.current <- target
                    onComplete |> Option.iter (fun fn -> fn ()))

        Hooks.useEffect(
            (fun () ->
                if isDraggingHook.current then
                    ()
                elif isOpen then
                    animateTo -deleteWidth None
                else
                    animateTo 0 None
                ()),
            [| isOpen |]
        )

        let settleFromOffset (offset: int) =
            let rowWidth = max rowWidthRef.current deleteWidth
            let fullDeleteThreshold = -(double rowWidth * SwipeGesture.fullDeleteRatio |> int)

            if offset <= fullDeleteThreshold then
                onDelete ()
            elif offset < -openThreshold then
                onOpenChange true
                restOffsetRef.current <- -deleteWidth
                animateTo -deleteWidth None
            else
                onOpenChange false
                restOffsetRef.current <- 0
                animateTo 0 None

        let onPanHorizontal (rawGs: ReactXP.Components.GestureView.PanGestureState) =
            let gs = SwipeGesture.currentOrLastDatafulGestureState lastGestureStateRef rawGs
            let delta = SwipeGesture.panDelta gs

            if gs.isComplete then
                let wasActive = gestureActiveRef.current
                gestureActiveRef.current <- false
                isDraggingHook.update false

                if not wasActive && abs delta < SwipeGesture.activationThreshold then
                    animateTo restOffsetRef.current None
                else
                    let offset =
                        if wasActive then
                            lastDragOffsetRef.current
                        else
                            SwipeGesture.clampOffset rowWidthRef.current (panStartBaseRef.current + delta)

                    settleFromOffset offset

                lastGestureStateRef.current <- None
            else
                if delta > 0 && not isOpen then
                    Noop
                elif abs delta < SwipeGesture.activationThreshold && not isOpen then
                    Noop
                else
                    if not gestureActiveRef.current then
                        gestureActiveRef.current <- true
                        panStartBaseRef.current <- (if isOpen then -deleteWidth else 0)

                    isDraggingHook.update true
                    let offset =
                        SwipeGesture.clampOffset rowWidthRef.current (panStartBaseRef.current + delta)
                    lastDragOffsetRef.current <- offset
                    translateXRef.current.SetValue (double offset)

        let gradientVisible = isOpen || isDraggingHook.current
        let deleteButtonState =
            ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> onDelete ()))

        LC.With.ReducedMotion (fun reduceMotion ->
            if reduceMotion then
                RX.View(
                    styles = [| Styles.swipeReducedMotionRow |],
                    children = [|
                        RX.View(styles = [| Styles.swipeReducedMotionContent |], children = [| rowContent |])
                        RX.View(
                            styles = [| Styles.swipeReducedMotionDelete |],
                            children = [|
                                LC.TextButton(
                                    label = i18n.t.SwipeDeleteLabel,
                                    styles = [| Styles.swipeDeleteButtonText |],
                                    state = deleteButtonState,
                                    testId = todoItemTestId todo "delete"
                                )
                            |]
                        )
                    |]
                )
            else
                LC.With.Layout (fun (onLayoutOption, maybeLayout) ->
                    maybeLayout |> Option.iter (fun layout -> rowWidthRef.current <- layout.Width)

                    RX.View(
                        ?onLayout = onLayoutOption,
                        styles = [| Styles.swipeRowHost |],
                        children = [|
                            RX.View(
                                importantForAccessibility = LibClient.Accessibility.ImportantForAccessibility.No,
                                styles = [| Styles.swipeGradientOverlay gradientVisible |],
                                children = [||]
                            )
                            RX.View(
                                importantForAccessibility =
                                    (if isOpen then
                                        LibClient.Accessibility.ImportantForAccessibility.Auto
                                     else
                                        LibClient.Accessibility.ImportantForAccessibility.NoHideDescendants),
                                styles = [| Styles.swipeDeleteSlot |],
                                children = [|
                                    LC.TextButton(
                                        label = i18n.t.SwipeDeleteLabel,
                                        styles = [| Styles.swipeDeleteButtonText |],
                                        state =
                                            ButtonHighLevelStateFactory.MakeLowLevel (
                                                ButtonLowLevelState.Actionable (fun _ ->
                                                    if isOpen then
                                                        onDelete ()
                                                    else
                                                        onOpenChange true)),
                                        testId = todoItemTestId todo "delete"
                                    )
                                |]
                            )
                            RX.AnimatableView(
                                styles = [| Styles.swipeContentAnimated (AnimatableValue.Value translateXRef.current) palette |],
                                children = [|
                                    RX.GestureView(
                                        preferredPan = ReactXP.Components.GestureView.PreferredPanGesture.Horizontal,
                                        panPixelThreshold = 10.0,
                                        onPanHorizontal = onPanHorizontal,
                                        children = [| rowContent |]
                                    )
                                |]
                            )
                        |]
                    )
                )
        )

    static member TodoList(
            listKey: string,
            listFilter: TodoListFilter,
            searchTerm: Option<NonemptyString>,
            palette: SemanticPalette,
            appearance: AppearanceMode,
            useCompactUI: bool,
            swipeOpenId: Option<TodoId>,
            setSwipeOpenId: Option<TodoId> -> unit,
            makeExecutor: MakeExecutor,
            onMutated: unit -> unit,
            editingId: Option<TodoId>,
            setEditingId: Option<TodoId> -> unit)
        : ReactElement =
        UiSubject.With.Subjects(
            key = listKey,
            service = AppTodo.AppServices.services().Todo,
            by = By.Indexed (queryForFilterAndSearch listFilter searchTerm),
            useCache = UseCache.IfReasonablyFresh,
            treatFetchingSomeAsAvailable = true,
            whenAvailable =
                (fun subjects ->
                    let todos =
                        subjects
                        |> Subjects.available
                        |> filterTodosClientSide listFilter

                    let openCount =
                        subjects
                        |> Subjects.available
                        |> Seq.filter (fun t -> t.ArchivedOn.IsNone && not t.Done)
                        |> Seq.length

                    let doneCount =
                        subjects
                        |> Subjects.available
                        |> Seq.filter (fun t -> t.ArchivedOn.IsNone && t.Done)
                        |> Seq.length

                    if List.isEmpty todos then
                        LC.InfoMessage(
                            level = InfoMessage.Level.Info,
                            message =
                                match searchTerm with
                                | None -> i18n.t.EmptyList
                                | Some _ -> i18n.t.EmptySearch
                        )
                    else
                        RX.View(
                            children = [|
                                RX.View(
                                    styles = [| Styles.listHeader |],
                                    children =
                                        tellReactArrayKeysAreOkay [|
                                            RX.View(
                                                accessibilityRole = AccessibilityRole.Status,
                                                accessibilityLiveRegion = AccessibilityLiveRegion.Polite,
                                                accessibilityLabel = i18n.Format(i18n.t.StatsFormat, openCount, doneCount),
                                                styles = [| Styles.statsRow |],
                                                children =
                                                    tellReactArrayKeysAreOkay [|
                                                        Helpers.StatChip(
                                                            palette,
                                                            i18n.Format(i18n.t.StatOpenFormat, openCount),
                                                            testId = A11ySlug.testId "todo" "stats-open")
                                                        Helpers.StatChip(
                                                            palette,
                                                            i18n.Format(i18n.t.StatDoneFormat, doneCount),
                                                            testId = A11ySlug.testId "todo" "stats-done")
                                                    |]
                                            )
                                        |]
                                )
                                RX.View(
                                    accessibilityRole = AccessibilityRole.List,
                                    accessibilityLabel = i18n.Format(i18n.t.ListCountFormat, List.length todos),
                                    styles = [| Styles.list |],
                                    testId = A11ySlug.testId "todo" "list",
                                    children =
                                        [| castAsElementAckingKeysWarning (
                                            todos
                                            |> List.map (fun todo ->
                                                Helpers.TodoRow(
                                                    todo = todo,
                                                    palette = palette,
                                                    appearance = appearance,
                                                    useCompactUI = useCompactUI,
                                                    swipeOpenId = swipeOpenId,
                                                    setSwipeOpenId = setSwipeOpenId,
                                                    makeExecutor = makeExecutor,
                                                    onMutated = onMutated,
                                                    editingId = editingId,
                                                    setEditingId = setEditingId
                                                ))
                                            |> List.toArray
                                        ) |]
                                )
                            |]
                        )),
            whenFetching =
                (fun _ ->
                    LC.InfoMessage(
                        level = InfoMessage.Level.Info,
                        message = i18n.t.LoadingList
                    )),
            whenFailed =
                (fun failure ->
                    LC.InfoMessage(
                        level = InfoMessage.Level.Caution,
                        message = i18n.Format(i18n.t.LoadFailedFormat, failure.DisplayReason)
                    ))
        )

    [<Component>]
    static member StatChip(palette: SemanticPalette, label: string, ?testId: string) : ReactElement =
        RX.View(
            ?testId = testId,
            importantForAccessibility = LibClient.Accessibility.ImportantForAccessibility.No,
            styles = [| Styles.statChip palette |],
            children = [|
                LC.Text(styles = [| Styles.statChipText palette |], value = label)
            |]
        )

    [<Component>]
    static member TodoRow(
            todo: Todo,
            palette: SemanticPalette,
            appearance: AppearanceMode,
            useCompactUI: bool,
            swipeOpenId: Option<TodoId>,
            setSwipeOpenId: Option<TodoId> -> unit,
            makeExecutor: MakeExecutor,
            onMutated: unit -> unit,
            editingId: Option<TodoId>,
            setEditingId: Option<TodoId> -> unit)
        : ReactElement =
        let executor = makeExecutor ("todo-" + (todo.Id :> SubjectId).IdString)
        let isEditing = editingId = Some todo.Id
        let editTitleHook = Hooks.useState todo.Title

        Hooks.useEffect(
            (fun () ->
                if not isEditing then
                    editTitleHook.update todo.Title
                ()),
            [| todo.Title; isEditing |]
        )

        let runAction (action: unit -> UDActionResult) =
            executor.MaybeExecute (fun () ->
                async {
                    let! result = action ()
                    if Result.isOk result then onMutated()
                    return result |> Result.map ignore
                })

        let metaChip chipBg chipBorder chipText (chipLabel: string) (spokenLabel: string) =
            RX.View(
                accessibilityLabel = spokenLabel,
                styles = [| Styles.metaChip chipBg chipBorder |],
                children = [|
                    LC.Text(
                        styles = [| Styles.metaChipText chipText |],
                        value = chipLabel
                    )
                |]
            )

        let metaChips =
            let priorityBg, priorityBorder, priorityText =
                Styles.priorityChipColors palette todo.Priority

            let dueBg, dueBorder, dueText =
                Styles.dueChipColors palette

            [
                metaChip priorityBg priorityBorder priorityText (priorityLabel todo.Priority) (metaPrioritySpoken todo.Priority)
            ]
            @ (
                match todo.Category with
                | Some category ->
                    let catBg, catBorder, catText = Styles.categoryChipColorsByCategory palette (Some category)
                    [ metaChip catBg catBorder catText (categoryLabel category) (metaCategorySpoken category) ]
                | None -> []
            )
            @ (
                match todo.DueOn with
                | Some dueOn ->
                    [ metaChip dueBg dueBorder dueText (i18n.Format(i18n.t.MetaDueChipFormat, formatDueOn dueOn)) (metaDueSpoken dueOn) ]
                | None -> []
            )
            |> List.toArray

        let rowLabel = rowSummaryLabel todo
        let confirmDeleteHook = Hooks.useState false
        let isSwipeOpen = swipeOpenId = Some todo.Id

        let setSwipeOpen (open_: bool) =
            if open_ then
                setSwipeOpenId (Some todo.Id)
            elif isSwipeOpen then
                setSwipeOpenId None

        let deleteTodoAction () =
            runAction (fun () ->
                async {
                    let! result = deleteTodo todo.Id
                    if Result.isOk result then
                        LC.LiveRegion.announce (i18n.Format(i18n.t.DeletedFormat, todo.Title.Value)) LibClient.Accessibility.AccessibilityLiveRegion.Polite
                        setSwipeOpenId None
                    return result |> Result.map ignore
                })
            |> ignore

        let rowActionButtons =
            tellReactArrayKeysAreOkay [|
                if isEditing then
                    LC.TextButton(
                        label = i18n.t.Save,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ ->
                            runAction (fun () -> setTodoTitle todo.Id editTitleHook.current) |> ignore
                            setEditingId None)),
                        testId = todoItemTestId todo "save"
                    )
                elif useCompactUI then
                    LC.IconButton(
                        icon = Icon.Pencil,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> setEditingId (Some todo.Id))),
                        label = todoActionLabel todo i18n.t.EditActionFormat,
                        testId = todoItemTestId todo "edit",
                        styles = [| Styles.actionIconButton palette |]
                    )
                else
                    LC.TextButton(
                        label = todoActionLabel todo i18n.t.EditActionFormat,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> setEditingId (Some todo.Id))),
                        testId = todoItemTestId todo "edit"
                    )
                if not useCompactUI && todo.Done && todo.ArchivedOn.IsNone then
                    LC.TextButton(
                        label = todoActionLabel todo i18n.t.ArchiveActionFormat,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ ->
                            runAction (fun () -> archiveTodo todo.Id) |> ignore)),
                        testId = todoItemTestId todo "archive"
                    )
                if not useCompactUI && confirmDeleteHook.current then
                    LC.TextButton(
                        label = i18n.t.Cancel,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> confirmDeleteHook.update false)),
                        testId = todoItemTestId todo "delete-cancel"
                    )
                    LC.TextButton(
                        label = todoActionLabel todo i18n.t.ConfirmDeleteFormat,
                        state = ButtonHighLevelStateFactory.Make (fun () -> async {
                            let! result = deleteTodo todo.Id
                            if Result.isOk result then
                                LC.LiveRegion.announce (i18n.Format(i18n.t.DeletedFormat, todo.Title.Value)) LibClient.Accessibility.AccessibilityLiveRegion.Polite
                                confirmDeleteHook.update false
                                onMutated()
                            return result |> Result.map ignore
                        }, executor),
                        testId = todoItemTestId todo "delete-confirm"
                    )
                elif not useCompactUI then
                    LC.TextButton(
                        label = todoActionLabel todo i18n.t.DeleteActionFormat,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> confirmDeleteHook.update true)),
                        testId = todoItemTestId todo "delete"
                    )
            |]

        let titleContent =
            if isEditing then
                LC.Input.Text(
                    value = Some editTitleHook.current,
                    onChange = (fun v -> editTitleHook.update (v |> Option.defaultValue todo.Title)),
                    validity = Valid,
                    placeholder = editTitleLabel todo,
                    accessibilityLabel = editTitleLabel todo,
                    onEnterKeyPress =
                        (fun _ ->
                            runAction (fun () -> setTodoTitle todo.Id editTitleHook.current) |> ignore
                            setEditingId None),
                    testId = todoItemTestId todo "title-edit",
                    styles = [| Styles.inputFlex |]
                )
            else
                LC.Text(
                    styles = [| if todo.Done then Styles.titleTextDone palette else Styles.titleTextActive palette |],
                    value = todo.Title.Value
                )

        let rowSurface =
            RX.View(
                styles = [| Styles.todoRowSurface palette appearance; Styles.todoRow palette todo.Done useCompactUI |],
                children =
                    tellReactArrayKeysAreOkay [|
                        RX.View(
                            styles = [| Styles.todoMetaRow |],
                            children = tellReactArrayKeysAreOkay metaChips
                        )
                        RX.View(
                            styles = [| Styles.todoBodyRow |],
                            children =
                                Array.append
                                    (tellReactArrayKeysAreOkay [|
                                        LC.Input.Checkbox(
                                            value = Some todo.Done,
                                            onChange = (fun _ -> runAction (fun () -> toggleTodo todo.Id) |> ignore),
                                            validity = Valid,
                                            accessibilityLabel = toggleCheckboxLabel todo,
                                            testId = todoItemTestId todo "toggle"
                                        )
                                        RX.View(
                                            styles = [| Styles.todoContent |],
                                            children = [| titleContent |]
                                        )
                                    |])
                                    rowActionButtons
                        )
                    |]
            )

        let rowBody =
            if useCompactUI && not isEditing then
                Helpers.TodoSwipeShell(
                    todo = todo,
                    palette = palette,
                    isOpen = isSwipeOpen,
                    onOpenChange = setSwipeOpen,
                    onDelete = deleteTodoAction,
                    rowContent = rowSurface
                )
            else
                rowSurface

        RX.View(
            testId = todoItemTestId todo "row",
            accessibilityRole = AccessibilityRole.ListItem,
            accessibilityLabel = rowLabel,
            styles = [| Styles.todoRowOuter palette appearance |],
            children = [| rowBody |]
        )

type Ui.Route with
    [<Component>]
    static member Todos () : ReactElement =
        let titleInput = Hooks.useState None
        let searchInput = Hooks.useState None
        let dueInput = Hooks.useState None
        let priorityHook = Hooks.useState TodoPriority.Medium
        let categoryHook = Hooks.useState None
        let listFilterHook = Hooks.useState TodoListFilter.All
        let listVersion = Hooks.useState 0
        let appearanceHook = Hooks.useState AppearanceMode.Light
        let editingIdHook = Hooks.useState None
        let swipeOpenIdHook = Hooks.useState None

        Hooks.useEffect(
            (fun () ->
                async {
                    let! mode = AppearanceStorage.loadAsync()
                    appearanceHook.update mode
                }
                |> startSafely
            ),
            [||]
        )

        let bumpList () = listVersion.update (listVersion.current + 1)

        let setAppearance (mode: AppearanceMode) =
            appearanceHook.update mode
            AppearanceStorage.save mode

        LC.With.ScreenSize (fun screenSize ->
            let isHandheld = screenSize = ScreenSize.Handheld
            let palette = SemanticPalette.forMode appearanceHook.current
            let tabTheme = Styles.tabsTheme palette
#if EGGSHELL_PLATFORM_IS_WEB
            let usePhoneChrome = true
#else
            let usePhoneChrome = isHandheld
#endif
            let useCompactTabs = usePhoneChrome || isHandheld

            // Re-theme inputs/pickers for the active appearance before they render.
            AppTodo.ComponentsTheme.applyInputThemes palette

            let searchKey =
                searchInput.current
                |> Option.map (fun (s: NonemptyString) -> s.Value)
                |> Option.defaultValue ""
            let listKey =
                sprintf "%i-%s-%s" listVersion.current searchKey (filterLabel listFilterHook.current)

            LC.With.GlobalExecutor (fun makeExecutor ->
                element {
                    let addExecutor = makeExecutor "add-todo"

                    let addAction () : UDActionResult =
                        async {
                            match titleInput.current with
                            | None ->
                                LC.LiveRegion.announce i18n.t.EnterTitle LibClient.Accessibility.AccessibilityLiveRegion.Polite
                                return Error i18n.t.EnterTitle
                            | Some title ->
                                let dueOn =
                                    dueInput.current
                                    |> Option.bind (fun (s: NonemptyString) -> parseDueOnInput s.Value)
                                let! result = addTodo title priorityHook.current categoryHook.current dueOn
                                if Result.isOk result then
                                    LC.LiveRegion.announce (i18n.Format(i18n.t.AddedFormat, title.Value)) LibClient.Accessibility.AccessibilityLiveRegion.Polite
                                    titleInput.update None
                                    dueInput.update None
                                    priorityHook.update TodoPriority.Medium
                                    categoryHook.update None
                                    bumpList()
                                return result
                        }

                    let cardContent =
                        RX.View(
                            styles = [| Styles.cardShell palette usePhoneChrome |],
                            testId = A11ySlug.testId "todo" "card",
                            children = [|
                                RX.View(
                                    styles = [| Styles.card palette usePhoneChrome |],
                                    children = [|
                                        LC.Column(
                                            gap = 20,
                                            children = [|
                                                RX.View(
                                                    styles = [| Styles.headerRow |],
                                                    children =
                                                        tellReactArrayKeysAreOkay [|
                                                        LC.Column(
                                                            gap = 0,
                                                            crossAxisAlignment = LC.CrossAxisAlignment.Stretch,
                                                            styles = [| Styles.headerTitleBlock |],
                                                            children = [|
                                                                LC.Heading(
                                                                    level = Heading.Level.Primary,
                                                                    children = [|
                                                                        LC.Text(
                                                                            styles = [| Styles.headingText palette |],
                                                                            value = i18n.t.PageTitle
                                                                        )
                                                                    |]
                                                                )
                                                                LC.Text(
                                                                    styles = [| Styles.subtitle palette |],
                                                                    value = i18n.t.PageSubtitle
                                                                )
                                                            |]
                                                        )
                                                        RX.View(
                                                            styles = [| Styles.headerActions |],
                                                            children = [|
                                                                Helpers.ThemeToggle(
                                                                    palette = palette,
                                                                    current = appearanceHook.current,
                                                                    onSelect = setAppearance
                                                                )
                                                            |]
                                                        )
                                                    |]
                                                )

                                                Helpers.FilterTabs(
                                                    palette = palette,
                                                    tabTheme = tabTheme,
                                                    current = listFilterHook.current,
                                                    isHandheld = useCompactTabs,
                                                    onSelect = (fun f ->
                                                        listFilterHook.update f
                                                        editingIdHook.update None)
                                                )

                                                RX.View(
                                                    accessibilityLabel = i18n.t.ActiveFiltersLabel,
                                                    styles = [| Styles.subFiltersRow |],
                                                    children =
                                                        tellReactArrayKeysAreOkay [|
                                                            RX.View(
                                                                styles = [| Styles.subFilterPill palette.CategoryGreenSoft |],
                                                                children = [|
                                                                    LC.Text(
                                                                        styles = [| Styles.subFilterPillText palette.CategoryGreenText |],
                                                                        value = i18n.Format(i18n.t.ViewFilterFormat, filterLabel listFilterHook.current)
                                                                    )
                                                                |]
                                                            )
                                                            if usePhoneChrome then
                                                                RX.View(
                                                                    styles = [| Styles.subFilterPill palette.CategoryBlueSoft |],
                                                                    children = [|
                                                                        LC.Text(
                                                                            styles = [| Styles.subFilterPillText palette.CategoryBlueText |],
                                                                            value = i18n.t.DeviceHandheld
                                                                        )
                                                                    |]
                                                                )
                                                        |]
                                                )

                                                LC.Group(
                                                    label = i18n.t.ComposerGroupLabel,
                                                    testId = A11ySlug.testId "todo" "composer",
                                                    children = [|
                                                RX.View(
                                                    styles = [| Styles.composerPanel palette useCompactTabs |],
                                                    children = [|
                                                        LC.Column(
                                                            gap = 12,
                                                            styles = [| Styles.composerGrid useCompactTabs |],
                                                            children = [|
                                                                RX.View(
                                                                    styles = [| Styles.fieldStack |],
                                                                    children = [|
                                                                        Helpers.FieldLabel(palette, i18n.t.TitleLabel)
                                                                        LC.Input.Text(
                                                                            value = titleInput.current,
                                                                            onChange = titleInput.update,
                                                                            validity = Valid,
                                                                            placeholder = i18n.t.TitlePlaceholder,
                                                                            accessibilityLabel = i18n.t.TitleLabel,
                                                                            testId = A11ySlug.testId "todo" "new-title"
                                                                        )
                                                                    |]
                                                                )
                                                                RX.View(
                                                                    styles = [| Styles.fieldStack |],
                                                                    accessibilityLabel = i18n.t.PriorityFieldLabel,
                                                                    children = [|
                                                                        Helpers.FieldLabel(palette, i18n.t.PriorityFieldLabel)
                                                                        LC.Input.Picker(
                                                                            items = Static (OrderedSet.ofList allPriorities, priorityLabel),
                                                                            itemView = PropItemViewFactory.Make priorityLabel,
                                                                            value = SelectableValue.ExactlyOne (Some priorityHook.current, priorityHook.update),
                                                                            validity = Valid,
                                                                            showSearchBar = false,
                                                                            testId = A11ySlug.testId "todo" "new-priority"
                                                                        )
                                                                    |]
                                                                )
                                                                Helpers.NewCategoryPicker(
                                                                    palette = palette,
                                                                    selected = categoryHook.current,
                                                                    onSelect = categoryHook.update
                                                                )
                                                                LC.Button(
                                                                    label = i18n.t.AddButtonMobile,
                                                                    state = ButtonHighLevelStateFactory.Make (addAction, addExecutor),
                                                                    styles = [| Styles.addButton |],
                                                                    testId = A11ySlug.testId "todo" "add-mobile"
                                                                )
                                                            |]
                                                        )
                                                    |]
                                                )
                                                    |]
                                                )

                                                LC.Group(
                                                    label = i18n.t.SearchLabel,
                                                    testId = A11ySlug.testId "todo" "search-group",
                                                    children = [|
                                                RX.View(
                                                    styles = [| Styles.searchField |],
                                                    children = [|
                                                        RX.View(
                                                            styles = [| Styles.searchInputWrap palette |],
                                                            children = [|
                                                                LC.Input.Text(
                                                                    value = searchInput.current,
                                                                    onChange = searchInput.update,
                                                                    validity = Valid,
                                                                    placeholder = i18n.t.SearchPlaceholder,
                                                                    accessibilityLabel = i18n.t.SearchLabel,
                                                                    accessibilityRole = AccessibilityRole.Search,
                                                                    prefixIcon = Icon.MagnifyingGlass,
                                                                    theme =
                                                                        (fun t ->
                                                                            { t with
                                                                                BorderRadius = 999
                                                                                EditableBackgroundColor = palette.SearchBackground
                                                                                TheVerticalPadding = 10
                                                                            }),
                                                                    styles = [| Styles.searchInput palette |],
                                                                    testId = A11ySlug.testId "todo" "search"
                                                                )
                                                            |]
                                                        )
                                                    |]
                                                )
                                                    |]
                                                )

                                                LC.Group(
                                                    label = sprintf "%s: %s" i18n.t.ListGroupLabel (filterLabel listFilterHook.current),
                                                    testId = A11ySlug.testId "todo" "list-group",
                                                    children = [|
                                                Helpers.TodoList(
                                                    listKey = listKey,
                                                    listFilter = listFilterHook.current,
                                                    searchTerm = searchInput.current,
                                                    palette = palette,
                                                    appearance = appearanceHook.current,
                                                    useCompactUI = useCompactTabs,
                                                    swipeOpenId = swipeOpenIdHook.current,
                                                    setSwipeOpenId = swipeOpenIdHook.update,
                                                    makeExecutor = makeExecutor,
                                                    onMutated = bumpList,
                                                    editingId = editingIdHook.current,
                                                    setEditingId = editingIdHook.update
                                                )
                                                    |]
                                                )
                                            |]
                                        )
                                    |]
                                )
                            |]
                        )

                    RX.View(
                        styles = [| Styles.page palette usePhoneChrome |],
                        testId = A11ySlug.testId "todo" "page",
                        children = [|
                            if usePhoneChrome then
                                RX.ScrollView(
                                    styles = [| Styles.pageScroll |],
                                    showsVerticalScrollIndicator = true,
                                    children = [|
                                        RX.View(
                                            styles = [| Styles.pageScrollContent |],
                                            children = [| cardContent |]
                                        )
                                    |]
                                )
                            else
                                cardContent
                        |]
                    )
                }
            )
        )
