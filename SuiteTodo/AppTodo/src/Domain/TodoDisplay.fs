module AppTodo.TodoDisplay

open System
open AppTodo.I18nGlobal
open AppTodo.TodoQueries
open SuiteTodo.Types

let priorityLabel (priority: TodoPriority) =
    match priority with
    | TodoPriority.Low -> i18n.t.PriorityLow
    | TodoPriority.Medium -> i18n.t.PriorityMedium
    | TodoPriority.High -> i18n.t.PriorityHigh

let categoryLabel (category: TodoCategory) =
    match category with
    | TodoCategory.Work -> i18n.t.CategoryWork
    | TodoCategory.Personal -> i18n.t.CategoryPersonal
    | TodoCategory.Shopping -> i18n.t.CategoryShopping
    | TodoCategory.Health -> i18n.t.CategoryHealth
    | TodoCategory.Other -> i18n.t.CategoryOther

let filterLabel (filter: TodoListFilter) =
    match filter with
    | TodoListFilter.Open -> i18n.t.FilterOpen
    | TodoListFilter.Done -> i18n.t.FilterDone
    | TodoListFilter.All -> i18n.t.FilterAll
    | TodoListFilter.Archived -> i18n.t.FilterArchived

let todoItemSlug (todo: Todo) =
    (todo.Id :> SubjectId).IdString.Replace("-", "").Substring(0, 8).ToLowerInvariant()

let todoItemTestId (todo: Todo) (suffix: string) =
    sprintf "todo-item-%s-%s" (todoItemSlug todo) suffix

let todoActionLabel (todo: Todo) (actionFormat: string) =
    i18n.Format(actionFormat, todo.Title.Value)

let editTitleLabel (todo: Todo) =
    i18n.Format(i18n.t.EditTitleLabelFormat, todo.Title.Value)

let toggleCheckboxLabel (todo: Todo) =
    if todo.Done then
        i18n.Format(i18n.t.ToggleOpenFormat, todo.Title.Value)
    else
        i18n.Format(i18n.t.ToggleDoneFormat, todo.Title.Value)

let rowSummaryLabel (todo: Todo) =
    let status = if todo.Done then i18n.t.RowDone else i18n.t.RowOpen
    i18n.Format(i18n.t.RowSummaryFormat, todo.Title.Value, status)

let metaPrioritySpoken (priority: TodoPriority) =
    i18n.Format(i18n.t.MetaPriorityFormat, priorityLabel priority)

let metaCategorySpoken (category: TodoCategory) =
    i18n.Format(i18n.t.MetaCategoryFormat, categoryLabel category)

let metaDueSpoken (dueOn: DateTimeOffset) =
    i18n.Format(i18n.t.MetaDueSpokenFormat, formatDueOn dueOn)

let formatDueOn (dueOn: DateTimeOffset) =
    dueOn.ToLocalTime().ToString("MMM d")

let parseDueOnInput (raw: string) : Option<DateTimeOffset> =
    if String.IsNullOrWhiteSpace raw then
        None
    else
        match DateTimeOffset.TryParse(raw.Trim()) with
        | true, dt -> Some dt
        | false, _ -> None

let allCategories : list<TodoCategory> =
    [ TodoCategory.Work; TodoCategory.Personal; TodoCategory.Shopping; TodoCategory.Health; TodoCategory.Other ]

let allPriorities : list<TodoPriority> =
    [ TodoPriority.Low; TodoPriority.Medium; TodoPriority.High ]

let priorityScore (priority: TodoPriority) =
    match priority with
    | TodoPriority.High -> 3
    | TodoPriority.Medium -> 2
    | TodoPriority.Low -> 1
