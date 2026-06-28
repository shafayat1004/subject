[<AutoOpen>]
module AppTodo.Components.Route_Todos

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Components.Input
open LibUiSubject
open LibUiSubject.Components.Constructors
open LibClient.Services.Subscription
open LibUiSubject.Components.With.View
open ReactXP.Components
open ReactXP.Styles
open AppTodo.Actions
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

    let addRow = makeViewStyles {
        FlexDirection.Row
        AlignItems.Center
        gap 12
        marginBottom 16
    }

type private Helpers =
    [<Component>]
    static member TodoRow(
            item: TodoListItem,
            makeExecutor: MakeExecutor,
            onMutated: unit -> unit)
        : ReactElement =
        let executor = makeExecutor ("todo-" + (item.Id :> SubjectId).IdString)

        let toggleAction () : UDActionResult =
            async {
                let! result = toggleTodo item.Id
                if Result.isOk result then onMutated()
                return result
            }

        let deleteAction () : UDActionResult =
            async {
                let! result = deleteTodo item.Id
                if Result.isOk result then onMutated()
                return result
            }

        RX.View(
            styles = [| Styles.row |],
            children = [|
                LC.Input.Checkbox(
                    value = Some item.Done,
                    onChange = (fun _ -> executor.MaybeExecute toggleAction |> ignore),
                    validity = Valid,
                    accessibilityLabel = item.Title.Value,
                    testId = A11ySlug.testId "todo-item" "toggle"
                )
                LC.Text(
                    styles = [| if item.Done then Styles.titleTextDone else Styles.titleTextActive |],
                    value = item.Title.Value
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
        let listVersion = Hooks.useState 0
        let bumpList () = listVersion.update (listVersion.current + 1)

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
                            styles = [| Styles.addRow |],
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

                        UiSubject.With.View(
                            key = string listVersion.current,
                            service = AppTodo.AppServices.services().TodoListView,
                            input = NoInput,
                            useCache = UseCache.No,
                            whenAvailable =
                                (fun output ->
                                    let items =
                                        output.Items
                                        |> List.sortBy (fun item -> item.CreatedOn)

                                    if List.isEmpty items then
                                        LC.InfoMessage(
                                            level = InfoMessage.Level.Info,
                                            message = "No todos yet. Add one above."
                                        )
                                    else
                                        RX.View(
                                            styles = [| Styles.list |],
                                            children =
                                                [| castAsElementAckingKeysWarning (
                                                    items
                                                    |> List.map (fun item ->
                                                        Helpers.TodoRow(
                                                            item = item,
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
