module AppTodo.TodoDisplay

open System
open AppTodo.TodoQueries
open SuiteTodo.Types

let priorityLabel (priority: TodoPriority) =
    match priority with
    | TodoPriority.Low -> "Low"
    | TodoPriority.Medium -> "Medium"
    | TodoPriority.High -> "High"

let categoryLabel (category: TodoCategory) =
    match category with
    | TodoCategory.Work -> "Work"
    | TodoCategory.Personal -> "Personal"
    | TodoCategory.Shopping -> "Shopping"
    | TodoCategory.Health -> "Health"
    | TodoCategory.Other -> "Other"

let filterLabel (filter: TodoListFilter) =
    match filter with
    | TodoListFilter.Open -> "Open"
    | TodoListFilter.Done -> "Done"
    | TodoListFilter.All -> "All"
    | TodoListFilter.Archived -> "Archived"

let todoItemSlug (todo: Todo) =
    (todo.Id :> SubjectId).IdString.Replace("-", "").Substring(0, 8).ToLowerInvariant()

let todoItemTestId (todo: Todo) (suffix: string) =
    sprintf "todo-item-%s-%s" (todoItemSlug todo) suffix

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
