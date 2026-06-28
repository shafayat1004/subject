[<AutoOpen>]
module AppTodo.Components.Route_Todos

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Input
open LibUiSubject
open LibUiSubject.Components.Constructors
open LibUiSubject.Components.With.Subjects
open LibClient.Services.Subscription
open ReactXP.Components
open ReactXP.Styles
open AppTodo.Actions
open AppTodo.TodoQueries
open SuiteTodo.Types

module private Styles =
    let row = makeViewStyles {
        FlexDirection.Row
        AlignItems.Center
        paddingVertical 8
        gap 12
    }

    let titleTextActive = makeTextStyles {
        color Color.Black
    }

    let titleTextDone = makeTextStyles {
        textDecorationLine TextDecorationLine.LineThrough
        color (Color.Grey "99")
    }

    let list = makeViewStyles {
        gap 4
    }

    let fieldRow = makeViewStyles {
        FlexDirection.Row
        AlignItems.Center
        gap 12
        marginBottom 16
    }

type private Helpers =
    [<Component>]
    static member TodoRow(
            todo: Todo,
            makeExecutor: MakeExecutor,
            onMutated: unit -> unit)
        : ReactElement =
        let executor = makeExecutor ("todo-" + (todo.Id :> SubjectId).IdString)

        let toggleAction () : UDActionResult =
            async {
                let! result = toggleTodo todo.Id
                if Result.isOk result then onMutated()
                return result
            }

        let deleteAction () : UDActionResult =
            async {
                let! result = deleteTodo todo.Id
                if Result.isOk result then onMutated()
                return result
            }

        RX.View(
            styles = [| Styles.row |],
            children = [|
                LC.Input.Checkbox(
                    value = Some todo.Done,
                    onChange = (fun _ -> executor.MaybeExecute toggleAction |> ignore),
                    validity = Valid,
                    accessibilityLabel = todo.Title.Value,
                    testId = A11ySlug.testId "todo-item" "toggle"
                )
                LC.Text(
                    styles = [| if todo.Done then Styles.titleTextDone else Styles.titleTextActive |],
                    value = todo.Title.Value
                )
                LC.TextButton(
                    label = "Delete",
                    state = ButtonHighLevelStateFactory.Make (deleteAction, executor),
                    testId = A11ySlug.testId "todo-item" "delete"
                )
            |]
        )

type Ui.Route with
    [<Component>]
    static member Todos () : ReactElement =
        let titleInput = Hooks.useState None
        let searchInput = Hooks.useState None
        let listVersion = Hooks.useState 0
        let bumpList () = listVersion.update (listVersion.current + 1)

        let indexedQuery = queryForSearchInput searchInput.current
        let searchKey =
            searchInput.current
            |> Option.map (fun s -> s.Value)
            |> Option.defaultValue ""
        let listKey = listVersion.current.ToString() + "-" + searchKey

        LC.Executor.AlertErrors (fun makeExecutor ->
            element {
                let addExecutor = makeExecutor "add-todo"

                let addAction () : UDActionResult =
                    async {
                        match titleInput.current with
                        | None -> return Error "Enter a title"
                        | Some title ->
                            let! result = addTodo title
                            if Result.isOk result then
                                titleInput.update None
                                bumpList()
                            return result
                    }

                LC.Section.Padded(
                    children = [|
                        LC.Heading(
                            level = Heading.Level.Secondary,
                            children = [| LC.Text "Todos" |]
                        )

                        RX.View(
                            styles = [| Styles.fieldRow |],
                            children = [|
                                LC.Input.Text(
                                    value = titleInput.current,
                                    onChange = titleInput.update,
                                    validity = Valid,
                                    placeholder = "What needs doing?",
                                    testId = A11ySlug.testId "todo" "new-title"
                                )
                                LC.Button(
                                    label = "Add",
                                    state = ButtonHighLevelStateFactory.Make (addAction, addExecutor),
                                    testId = A11ySlug.testId "todo" "add"
                                )
                            |]
                        )

                        RX.View(
                            styles = [| Styles.fieldRow |],
                            children = [|
                                LC.Input.Text(
                                    value = searchInput.current,
                                    onChange = searchInput.update,
                                    validity = Valid,
                                    placeholder = "Search titles (full-text)",
                                    testId = A11ySlug.testId "todo" "search"
                                )
                            |]
                        )

                        UiSubject.With.Subjects(
                            key = listKey,
                            service = AppTodo.AppServices.services().Todo,
                            by = By.Indexed indexedQuery,
                            useCache = UseCache.IfReasonablyFresh,
                            treatFetchingSomeAsAvailable = true,
                            whenAvailable =
                                (fun subjects ->
                                    let todos =
                                        subjects
                                        |> Subjects.available
                                        |> Seq.sortBy (fun t -> t.CreatedOn)
                                        |> Seq.toList

                                    if List.isEmpty todos then
                                        LC.InfoMessage(
                                            level = InfoMessage.Level.Info,
                                            message =
                                                match searchInput.current with
                                                | None -> "No todos yet. Add one above."
                                                | Some _ -> "No todos match your search."
                                        )
                                    else
                                        RX.View(
                                            styles = [| Styles.list |],
                                            children =
                                                [| castAsElementAckingKeysWarning (
                                                    todos
                                                    |> List.map (fun todo ->
                                                        Helpers.TodoRow(
                                                            todo = todo,
                                                            makeExecutor = makeExecutor,
                                                            onMutated = bumpList
                                                        ))
                                                    |> List.toArray
                                                ) |]
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
                    |]
                )
            }
        )
