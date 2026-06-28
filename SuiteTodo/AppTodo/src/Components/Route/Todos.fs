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
    let page = makeViewStyles {
        flex 1
        paddingVertical 48
        paddingHorizontal 24
        AlignItems.Center
        backgroundColor (Color.Hex "#f3f5f8")
    }

    let card = makeViewStyles {
        widthPercent 100
        padding 28
        backgroundColor Color.White
        borderRadius 12
        borderWidth 1
        borderColor (Color.Hex "#e4e8ee")
    }

    let subtitle = makeTextStyles {
        marginTop 4
        marginBottom 8
        color (Color.Grey "66")
        fontSize 14
    }

    let inputFlex = makeViewStyles {
        flex 1
        minWidth 0
    }

    let list = makeViewStyles {
        gap 8
        marginTop 8
    }

    let todoRow = makeViewStyles {
        FlexDirection.Row
        AlignItems.Center
        gap 12
        paddingVertical 12
        paddingHorizontal 16
        backgroundColor (Color.Hex "#fafbfc")
        borderRadius 8
        borderWidth 1
        borderColor (Color.Hex "#e8ecf1")
    }

    let todoTitleWrap = makeViewStyles {
        flex 1
        minWidth 0
    }

    let titleTextActive = makeTextStyles {
        color (Color.Hex "#1a1f2e")
        fontSize 15
    }

    let titleTextDone = makeTextStyles {
        textDecorationLine TextDecorationLine.LineThrough
        color (Color.Grey "99")
        fontSize 15
    }

    let listSection = makeViewStyles {
        marginTop 8
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
            styles = [| Styles.todoRow |],
            children = [|
                LC.Input.Checkbox(
                    value = Some todo.Done,
                    onChange = (fun _ -> executor.MaybeExecute toggleAction |> ignore),
                    validity = Valid,
                    accessibilityLabel = todo.Title.Value,
                    testId = A11ySlug.testId "todo-item" "toggle"
                )
                RX.View(
                    styles = [| Styles.todoTitleWrap |],
                    children = [|
                        LC.Text(
                            styles = [| if todo.Done then Styles.titleTextDone else Styles.titleTextActive |],
                            value = todo.Title.Value
                        )
                    |]
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

                RX.View(
                    styles = [| Styles.page |],
                    children = [|
                        LC.Constrained(
                            maxWidth = 560,
                            child =
                                RX.View(
                                    styles = [| Styles.card |],
                                    children = [|
                                        LC.Column(
                                            gap = 20,
                                            children = [|
                                                LC.Column(
                                                    gap = 0,
                                                    crossAxisAlignment = LC.CrossAxisAlignment.Stretch,
                                                    children = [|
                                                        LC.Heading(
                                                            level = Heading.Level.Primary,
                                                            children = [| LC.Text "Todos" |]
                                                        )
                                                        LC.Text(
                                                            styles = [| Styles.subtitle |],
                                                            value = "Stay on top of what needs doing."
                                                        )
                                                    |]
                                                )

                                                LC.Row(
                                                    gap = 12,
                                                    crossAxisAlignment = LC.CrossAxisAlignment.Center,
                                                    children = [|
                                                        LC.Input.Text(
                                                            value = titleInput.current,
                                                            onChange = titleInput.update,
                                                            validity = Valid,
                                                            placeholder = "What needs doing?",
                                                            styles = [| Styles.inputFlex |],
                                                            testId = A11ySlug.testId "todo" "new-title"
                                                        )
                                                        LC.Button(
                                                            label = "Add",
                                                            state = ButtonHighLevelStateFactory.Make (addAction, addExecutor),
                                                            testId = A11ySlug.testId "todo" "add"
                                                        )
                                                    |]
                                                )

                                                LC.Input.Text(
                                                    value = searchInput.current,
                                                    onChange = searchInput.update,
                                                    validity = Valid,
                                                    placeholder = "Search todos...",
                                                    styles = [| Styles.inputFlex |],
                                                    testId = A11ySlug.testId "todo" "search"
                                                )

                                                RX.View(
                                                    styles = [| Styles.listSection |],
                                                    children = [|
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
                                            |]
                                        )
                                    |]
                                )
                        )
                    |]
                )
            }
        )
