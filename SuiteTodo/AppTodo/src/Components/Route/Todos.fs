[<AutoOpen>]
module AppTodo.Components.Route_Todos

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Input
open LibClient.Components.Input_Picker
open LibClient.Responsive
open LibUiSubject
open LibUiSubject.Components.Constructors
open LibUiSubject.Components.With.Subjects
open LibClient.Services.Subscription
open ReactXP.Components
open ReactXP.Styles
open AppTodo.Actions
open AppTodo.Colors
open AppTodo.TodoDisplay
open AppTodo.TodoQueries
open AppTodo.TodoTheme
open SuiteTodo.Types

module private AppearanceStorage =
    let load () : AppearanceMode =
        #if EGGSHELL_PLATFORM_IS_WEB
        match Browser.Dom.window.localStorage.getItem "apptodo-appearance" with
        | null | "" -> AppearanceMode.Light
        | v when v = "dark" -> AppearanceMode.Dark
        | _ -> AppearanceMode.Light
        #else
        AppearanceMode.Light
        #endif

    let save (mode: AppearanceMode) : unit =
        #if EGGSHELL_PLATFORM_IS_WEB
        let value =
            match mode with
            | AppearanceMode.Light -> "light"
            | AppearanceMode.Dark -> "dark"
        Browser.Dom.window.localStorage.setItem("apptodo-appearance", value)
        #else
        ()
        #endif

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
                children =
                    elements {
                        tellReactArrayKeysAreOkay tabItems
                    }
            )
        else
            LC.Tabs(
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
                    state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable onPress),
                    testId = testId
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
            showsHorizontalScrollIndicator = false,
            styles = [| Styles.categoryScroll |],
            children = [|
                RX.View(
                    styles = [| Styles.categoryScrollContent |],
                    children =
                        tellReactArrayKeysAreOkay [|
                            let noneBg, noneBorder, _ = Styles.categoryChipColorsByCategory palette None
                            Helpers.CategoryPill(
                                bg = noneBg,
                                border = noneBorder,
                                label = "No category",
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
                                | None -> "No todos in this view yet."
                                | Some _ -> "No todos match your search."
                        )
                    else
                        RX.View(
                            children = [|
                                RX.View(
                                    styles = [| Styles.listHeader |],
                                    children =
                                        tellReactArrayKeysAreOkay [|
                                            RX.View(
                                                styles = [| Styles.statsRow |],
                                                children =
                                                    tellReactArrayKeysAreOkay [|
                                                        Helpers.StatChip(
                                                            palette,
                                                            sprintf "%i open" openCount,
                                                            testId = A11ySlug.testId "todo" "stats-open")
                                                        Helpers.StatChip(
                                                            palette,
                                                            sprintf "%i done" doneCount,
                                                            testId = A11ySlug.testId "todo" "stats-done")
                                                    |]
                                            )
                                        |]
                                )
                                RX.View(
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
                        message = "Loading todos..."
                    )),
            whenFailed =
                (fun failure ->
                    LC.InfoMessage(
                        level = InfoMessage.Level.Caution,
                        message = "Failed to load todos: " + failure.DisplayReason
                    ))
        )

    [<Component>]
    static member StatChip(palette: SemanticPalette, label: string, ?testId: string) : ReactElement =
        RX.View(
            ?testId = testId,
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

        let metaChip chipBg chipBorder chipText (chipLabel: string) =
            RX.View(
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
                metaChip priorityBg priorityBorder priorityText (priorityLabel todo.Priority)
            ]
            @ (
                match todo.Category with
                | Some category ->
                    let catBg, catBorder, catText = Styles.categoryChipColorsByCategory palette (Some category)
                    [ metaChip catBg catBorder catText (categoryLabel category) ]
                | None -> []
            )
            @ (
                match todo.DueOn with
                | Some dueOn ->
                    [ metaChip dueBg dueBorder dueText ("Due " + formatDueOn dueOn) ]
                | None -> []
            )
            |> List.toArray

        let rowActionButtons =
            tellReactArrayKeysAreOkay [|
                if isEditing then
                    LC.TextButton(
                        label = "Save",
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ ->
                            runAction (fun () -> setTodoTitle todo.Id editTitleHook.current) |> ignore
                            setEditingId None)),
                        testId = todoItemTestId todo "save"
                    )
                else
                    LC.TextButton(
                        label = "Edit",
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> setEditingId (Some todo.Id))),
                        testId = todoItemTestId todo "edit"
                    )
                if not isHandheld && todo.Done && todo.ArchivedOn.IsNone then
                    LC.TextButton(
                        label = "Archive",
                        state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ ->
                            runAction (fun () -> archiveTodo todo.Id) |> ignore)),
                        testId = todoItemTestId todo "archive"
                    )
                LC.TextButton(
                    label = "Delete",
                    state = ButtonHighLevelStateFactory.Make (fun () -> async {
                        let! result = deleteTodo todo.Id
                        if Result.isOk result then onMutated()
                        return result |> Result.map ignore
                    }, executor),
                    testId = todoItemTestId todo "delete"
                )
            |]

        RX.View(
            testId = todoItemTestId todo "row",
            styles = [| Styles.todoRow palette todo.Done isHandheld |],
            children =
                if isHandheld then
                    tellReactArrayKeysAreOkay [|
                        RX.View(
                            styles = [| Styles.todoRowTop |],
                            children = [|
                                LC.Input.Checkbox(
                                    value = Some todo.Done,
                                    onChange = (fun _ -> runAction (fun () -> toggleTodo todo.Id) |> ignore),
                                    validity = Valid,
                                    accessibilityLabel = sprintf "Mark %s as %s" todo.Title.Value (if todo.Done then "open" else "done"),
                                    testId = todoItemTestId todo "toggle"
                                )
                                RX.View(
                                    styles = [| Styles.todoRowBody |],
                                    children = [|
                                        if isEditing then
                                            LC.Input.Text(
                                                value = Some editTitleHook.current,
                                                onChange = (fun v -> editTitleHook.update (v |> Option.defaultValue todo.Title)),
                                                validity = Valid,
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
                            accessibilityLabel = sprintf "Mark %s as %s" todo.Title.Value (if todo.Done then "open" else "done"),
                            testId = todoItemTestId todo "toggle"
                        )
                        RX.View(
                            styles = [| Styles.todoRowBody |],
                            children = [|
                                if isEditing then
                                    LC.Input.Text(
                                        value = Some editTitleHook.current,
                                        onChange = (fun v -> editTitleHook.update (v |> Option.defaultValue todo.Title)),
                                        validity = Valid,
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
        let appearanceHook = Hooks.useState (AppearanceStorage.load ())
        let editingIdHook = Hooks.useState None

        let bumpList () = listVersion.update (listVersion.current + 1)

        let toggleAppearance () =
            let next =
                match appearanceHook.current with
                | AppearanceMode.Light -> AppearanceMode.Dark
                | AppearanceMode.Dark -> AppearanceMode.Light
            appearanceHook.update next
            AppearanceStorage.save next

        LC.With.ScreenSize (fun screenSize ->
            let isHandheld = screenSize = ScreenSize.Handheld
            let palette = SemanticPalette.forMode appearanceHook.current
            let tabTheme = Styles.tabsTheme palette

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
                            | None -> return Error "Enter a title"
                            | Some title ->
                                let dueOn =
                                    dueInput.current
                                    |> Option.bind (fun (s: NonemptyString) -> parseDueOnInput s.Value)
                                let! result = addTodo title priorityHook.current categoryHook.current dueOn
                                if Result.isOk result then
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
                                                    children = [|
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
                                                                            value = "Todos"
                                                                        )
                                                                    |]
                                                                )
                                                                LC.Text(
                                                                    styles = [| Styles.subtitle palette |],
                                                                    value = "Plan, prioritize, and track work across devices."
                                                                )
                                                            |]
                                                        )
                                                        RX.View(
                                                            styles = [| Styles.headerActions |],
                                                            children = [|
                                                                LC.Button(
                                                                    label =
                                                                        (match appearanceHook.current with
                                                                         | AppearanceMode.Light -> "Dark"
                                                                         | AppearanceMode.Dark -> "Light"),
                                                                    level = Button.Level.Secondary,
                                                                    state = ButtonHighLevelStateFactory.MakeLowLevel (ButtonLowLevelState.Actionable (fun _ -> toggleAppearance ())),
                                                                    testId = A11ySlug.testId "todo" "theme-toggle"
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
                                                    styles = [| Styles.subFiltersRow |],
                                                    children =
                                                        tellReactArrayKeysAreOkay [|
                                                            RX.View(
                                                                styles = [| Styles.subFilterPill palette.CategoryGreenSoft |],
                                                                children = [|
                                                                    LC.Text(
                                                                        styles = [| Styles.subFilterPillText palette.CategoryGreenText |],
                                                                        value = sprintf "View: %s" (filterLabel listFilterHook.current)
                                                                    )
                                                                |]
                                                            )
                                                            if isHandheld then
                                                                RX.View(
                                                                    styles = [| Styles.subFilterPill palette.CategoryBlueSoft |],
                                                                    children = [|
                                                                        LC.Text(
                                                                            styles = [| Styles.subFilterPillText palette.CategoryBlueText |],
                                                                            value = "Handheld layout"
                                                                        )
                                                                    |]
                                                                )
                                                        |]
                                                )

                                                RX.View(
                                                    styles = [| Styles.composerPanel palette isHandheld |],
                                                    children = [|
                                                        LC.Column(
                                                            gap = 12,
                                                            styles = [| Styles.composerGrid isHandheld |],
                                                            children = [|
                                                                LC.Row(
                                                                    gap = 12,
                                                                    crossAxisAlignment = LC.CrossAxisAlignment.Center,
                                                                    styles = [| Styles.composerRow isHandheld |],
                                                                    children = [|
                                                                        LC.Input.Text(
                                                                            value = titleInput.current,
                                                                            onChange = titleInput.update,
                                                                            validity = Valid,
                                                                            placeholder = "What needs doing?",
                                                                            styles = [| Styles.inputFlex |],
                                                                            testId = A11ySlug.testId "todo" "new-title"
                                                                        )
                                                                        if not isHandheld then
                                                                            LC.Button(
                                                                                label = "Add",
                                                                                state = ButtonHighLevelStateFactory.Make (addAction, addExecutor),
                                                                                testId = A11ySlug.testId "todo" "add"
                                                                            )
                                                                    |]
                                                                )

                                                                LC.Row(
                                                                    gap = 12,
                                                                    crossAxisAlignment = LC.CrossAxisAlignment.Stretch,
                                                                    styles = [| Styles.composerRow isHandheld |],
                                                                    children = [|
                                                                        LC.Input.Picker(
                                                                            items = Static (OrderedSet.ofList allPriorities, priorityLabel),
                                                                            itemView = PropItemViewFactory.Make priorityLabel,
                                                                            value = SelectableValue.ExactlyOne (Some priorityHook.current, priorityHook.update),
                                                                            validity = Valid,
                                                                            label = "Priority",
                                                                            showSearchBar = false,
                                                                            testId = A11ySlug.testId "todo" "new-priority"
                                                                        )
                                                                        LC.Input.Text(
                                                                            value = dueInput.current,
                                                                            onChange = dueInput.update,
                                                                            validity = Valid,
                                                                            placeholder = "Due date (optional)",
                                                                            styles = [| Styles.inputFlex |],
                                                                            testId = A11ySlug.testId "todo" "new-due"
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
                                                                        label = "Add todo",
                                                                        state = ButtonHighLevelStateFactory.Make (addAction, addExecutor),
                                                                        testId = A11ySlug.testId "todo" "add-mobile"
                                                                    )
                                                            |]
                                                        )
                                                    |]
                                                )

                                                RX.View(
                                                    styles = [| Styles.searchField palette |],
                                                    children = [|
                                                        LC.Input.Text(
                                                            value = searchInput.current,
                                                            onChange = searchInput.update,
                                                            validity = Valid,
                                                            placeholder = "Search todos...",
                                                            styles = [| Styles.inputFlex |],
                                                            testId = A11ySlug.testId "todo" "search"
                                                        )
                                                    |]
                                                )

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
