[<AutoOpen>]
module AppTodo.Components.Route_Todos

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

type private Helpers =
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
        let segment (label: string) (mode: AppearanceMode) (testSuffix: string) =
            let isActive = current = mode
            let segmentColor = if isActive then Color.White else palette.TextSecondary
            RX.View(
                styles = [| Styles.themeSegment palette isActive |],
                children = [|
                    LC.TextButton(
                        label = label,
                        role = AccessibilityRole.Radio,
                        accessibilityState = AccessibilityStateRecord.selected isActive,
                        styles = [| Styles.themeSegmentText segmentColor |],
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> onSelect mode)),
                        testId = A11ySlug.testId "todo" ("theme-" + testSuffix)
                    )
                |]
            )

        LC.RadioGroup(
            label = i18n.t.ThemeGroupLabel,
            testId = A11ySlug.testId "todo" "theme-toggle",
            children =
                tellReactArrayKeysAreOkay [|
                    RX.View(
                        styles = [| Styles.themeToggleTrack palette |],
                        children =
                            tellReactArrayKeysAreOkay [|
                                segment i18n.t.ThemeLight AppearanceMode.Light "light"
                                segment i18n.t.ThemeDark AppearanceMode.Dark "dark"
                            |]
                    )
                |]
        )

    [<Component>]
    static member NewCategoryPicker(
            palette: SemanticPalette,
            selected: Option<TodoCategory>,
            onSelect: Option<TodoCategory> -> unit)
        : ReactElement =
        RX.ScrollView(
            horizontal = true,
            showsHorizontalScrollIndicator = true,
            styles = [| Styles.categoryScroll |],
            children = [|
                LC.RadioGroup(
                    label = sprintf "%s. %s" i18n.t.CategoryGroupLabel i18n.t.CategoryScrollHint,
                    testId = A11ySlug.testId "todo" "new-category-group",
                    children = [|
                        RX.View(
                            styles = [| Styles.categoryScrollContent |],
                            children =
                                tellReactArrayKeysAreOkay [|
                            let noneBg, noneBorder, _ = Styles.categoryChipColorsByCategory palette None
                            Helpers.CategoryPill(
                                bg = noneBg,
                                border = noneBorder,
                                label = i18n.t.CategoryNone,
                                isSelected = selected.IsNone,
                                testId = A11ySlug.testId "todo" "new-category-none",
                                onPress = (fun _ -> onSelect None)
                            )
                            yield!
                                allCategories
                                |> List.map (fun category ->
                                    let catBg, catBorder, _ = Styles.categoryChipColorsByCategory palette (Some category)
                                    Helpers.CategoryPill(
                                        bg = catBg,
                                        border = catBorder,
                                        label = categoryLabel category,
                                        isSelected = (selected = Some category),
                                        onPress = (fun _ -> onSelect (Some category)),
                                        testId =
                                            A11ySlug.testId
                                                "todo"
                                                ("new-category-" + (categoryLabel category).ToLower())
                                    ))
                        |]
                        )
                    |]
                )
            |]
        )

    static member TodoList(
            listKey: string,
            listFilter: TodoListFilter,
            searchTerm: Option<NonemptyString>,
            palette: SemanticPalette,
            isHandheld: bool,
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
                                                    isHandheld = isHandheld,
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
            isHandheld: bool,
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
                else
                    LC.TextButton(
                        label = todoActionLabel todo i18n.t.EditActionFormat,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> setEditingId (Some todo.Id))),
                        testId = todoItemTestId todo "edit"
                    )
                if not isHandheld && todo.Done && todo.ArchivedOn.IsNone then
                    LC.TextButton(
                        label = todoActionLabel todo i18n.t.ArchiveActionFormat,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ ->
                            runAction (fun () -> archiveTodo todo.Id) |> ignore)),
                        testId = todoItemTestId todo "archive"
                    )
                if confirmDeleteHook.current then
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
                else
                    LC.TextButton(
                        label = todoActionLabel todo i18n.t.DeleteActionFormat,
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> confirmDeleteHook.update true)),
                        testId = todoItemTestId todo "delete"
                    )
            |]

        RX.View(
            testId = todoItemTestId todo "row",
            accessibilityRole = AccessibilityRole.ListItem,
            accessibilityLabel = rowLabel,
            styles = [| Styles.todoRow palette todo.Done isHandheld |],
            children =
                if isHandheld then
                    tellReactArrayKeysAreOkay [|
                        RX.View(
                            styles = [| Styles.todoRowTop |],
                            children = tellReactArrayKeysAreOkay [|
                                LC.Input.Checkbox(
                                    value = Some todo.Done,
                                    onChange = (fun _ -> runAction (fun () -> toggleTodo todo.Id) |> ignore),
                                    validity = Valid,
                                    accessibilityLabel = toggleCheckboxLabel todo,
                                    testId = todoItemTestId todo "toggle"
                                )
                                RX.View(
                                    styles = [| Styles.todoRowBody |],
                                    children = tellReactArrayKeysAreOkay [|
                                        if isEditing then
                                            LC.Input.Text(
                                                value = Some editTitleHook.current,
                                                onChange = (fun v -> editTitleHook.update (v |> Option.defaultValue todo.Title)),
                                                validity = Valid,
                                                label = editTitleLabel todo,
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
                                        RX.View(
                                            styles = [| Styles.todoMetaRow |],
                                            children = tellReactArrayKeysAreOkay metaChips
                                        )
                                    |]
                                )
                            |]
                        )
                        RX.View(
                            styles = [| Styles.rowActionsHandheld |],
                            children = rowActionButtons
                        )
                    |]
                else
                    tellReactArrayKeysAreOkay [|
                        LC.Input.Checkbox(
                            value = Some todo.Done,
                            onChange = (fun _ -> runAction (fun () -> toggleTodo todo.Id) |> ignore),
                            validity = Valid,
                            accessibilityLabel = toggleCheckboxLabel todo,
                            testId = todoItemTestId todo "toggle"
                        )
                        RX.View(
                            styles = [| Styles.todoRowBody |],
                            children = tellReactArrayKeysAreOkay [|
                                if isEditing then
                                    LC.Input.Text(
                                        value = Some editTitleHook.current,
                                        onChange = (fun v -> editTitleHook.update (v |> Option.defaultValue todo.Title)),
                                        validity = Valid,
                                        label = editTitleLabel todo,
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
                                RX.View(
                                    styles = [| Styles.todoMetaRow |],
                                    children = tellReactArrayKeysAreOkay metaChips
                                )
                            |]
                        )
                        RX.View(
                            styles = [| Styles.rowActions |],
                            children = rowActionButtons
                        )
                    |]
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
                            styles = [| Styles.cardShell isHandheld |],
                            testId = A11ySlug.testId "todo" "card",
                            children = [|
                                RX.View(
                                    styles = [| Styles.card palette isHandheld |],
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
                                                                Helpers.LanguageToggle()
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
                                                    isHandheld = isHandheld,
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
                                                        |]
                                                )

                                                LC.Group(
                                                    label = i18n.t.ComposerGroupLabel,
                                                    testId = A11ySlug.testId "todo" "composer",
                                                    children = [|
                                                RX.View(
                                                    styles = [| Styles.composerPanel palette isHandheld |],
                                                    children = [|
                                                        LC.Column(
                                                            gap = 12,
                                                            styles = [| Styles.composerGrid isHandheld |],
                                                            children = [|
                                                                RX.View(
                                                                    styles = [| Styles.composerRow isHandheld |],
                                                                    children =
                                                                        tellReactArrayKeysAreOkay [|
                                                                            RX.View(
                                                                                styles = [| Styles.composerCell isHandheld |],
                                                                                children = [|
                                                                                    LC.Input.Text(
                                                                                        value = titleInput.current,
                                                                                        onChange = titleInput.update,
                                                                                        validity = Valid,
                                                                                        label = i18n.t.TitleLabel,
                                                                                        placeholder = i18n.t.TitlePlaceholder,
                                                                                        testId = A11ySlug.testId "todo" "new-title"
                                                                                    )
                                                                                |]
                                                                            )
                                                                            if not isHandheld then
                                                                                LC.Button(
                                                                                    label = i18n.t.AddButton,
                                                                                    state = ButtonHighLevelStateFactory.Make (addAction, addExecutor),
                                                                                    styles = [| Styles.addButton |],
                                                                                    testId = A11ySlug.testId "todo" "add"
                                                                                )
                                                                        |]
                                                                )

                                                                RX.View(
                                                                    styles = [| Styles.composerRow isHandheld |],
                                                                    children =
                                                                        tellReactArrayKeysAreOkay [|
                                                                            LC.Input.Picker(
                                                                                items = Static (OrderedSet.ofList allPriorities, priorityLabel),
                                                                                itemView = PropItemViewFactory.Make priorityLabel,
                                                                                value = SelectableValue.ExactlyOne (Some priorityHook.current, priorityHook.update),
                                                                                validity = Valid,
                                                                                label = i18n.t.PriorityFieldLabel,
                                                                                showSearchBar = false,
                                                                                styles = [| Styles.composerCell isHandheld |],
                                                                                testId = A11ySlug.testId "todo" "new-priority"
                                                                            )
                                                                            RX.View(
                                                                                styles = [| Styles.composerCell isHandheld |],
                                                                                children = [|
                                                                                    LC.Input.Text(
                                                                                        value = dueInput.current,
                                                                                        onChange = dueInput.update,
                                                                                        validity = Valid,
                                                                                        label = i18n.t.DueLabel,
                                                                                        placeholder = i18n.t.DuePlaceholder,
                                                                                        testId = A11ySlug.testId "todo" "new-due"
                                                                                    )
                                                                                |]
                                                                            )
                                                                        |]
                                                                )

                                                                Helpers.NewCategoryPicker(
                                                                    palette = palette,
                                                                    selected = categoryHook.current,
                                                                    onSelect = categoryHook.update
                                                                )

                                                                if isHandheld then
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
                                                        LC.Input.Text(
                                                            value = searchInput.current,
                                                            onChange = searchInput.update,
                                                            validity = Valid,
                                                            placeholder = i18n.t.SearchPlaceholder,
                                                            accessibilityLabel = i18n.t.SearchLabel,
                                                            accessibilityRole = AccessibilityRole.Search,
                                                            prefixIcon = Icon.MagnifyingGlass,
                                                            testId = A11ySlug.testId "todo" "search"
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
                                                    isHandheld = isHandheld,
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
                        styles = [| Styles.page palette isHandheld |],
                        testId = A11ySlug.testId "todo" "page",
                        children = [|
                            if isHandheld then
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
