[<AutoOpen>]
module AppEggShellGallery.Components.Content_QueryGrid

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Form_Base.Types
open LibUiAdmin.Components
open AppEggShellGallery.Components
open AppEggShellGallery.Components.ComponentSample

[<RequireQualifiedAccess>]
type Field =
| Substring
| MinLength

type Query = {
    Substring: string
    MinLength: Option<PositiveInteger>
} with
    member this.Predicate (candidate: string) : bool =
        if candidate.Contains this.Substring then
            match this.MinLength with
            | Some minLength -> candidate.Length >= minLength.Value
            | None           -> true
        else
            false

type Acc = {
    Substring: Option<NonemptyString>
    MinLength: LibClient.Components.Input.PositiveInteger.Value
} with
    static member Empty : Acc = {
        Substring = None
        MinLength = LibClient.Components.Input.PositiveInteger.empty
    }

    interface AbstractAcc<Field, Query> with
        member this.Validate () : Result<Query, ValidationErrors<Field>> = resultful {
            let! minLength = Forms.GetOptionalFieldValue (Field.MinLength, this.MinLength.Result)
            return {
                Substring = this.Substring |> NonemptyString.optionToString
                MinLength = minLength
            }
        }

module private Sample =
    let columnWidths = Content_Grid.Demo.ColumnWidths.word
    let columnTotalUnits = Content_Grid.Demo.ColumnWidths.wordTotal

    let headers =
        element {
            UiAdmin.GridCell (columnIndex = 0, widthUnits = columnWidths.[0], columnTotalUnits = columnTotalUnits, isFirstColumn = true, children = [| LC.HeaderCell(label = "Word") |])
            UiAdmin.GridCell (columnIndex = 1, widthUnits = columnWidths.[1], columnTotalUnits = columnTotalUnits, children = [| LC.HeaderCell(label = "Character Count") |])
            UiAdmin.GridCell (columnIndex = 2, widthUnits = columnWidths.[2], columnTotalUnits = columnTotalUnits, children = [| LC.HeaderCell(label = "Unique Character Count") |])
        }

    let makeRow (word: string, _, _refresh) =
        element {
            UiAdmin.GridCell (columnIndex = 0, widthUnits = columnWidths.[0], columnTotalUnits = columnTotalUnits, isFirstColumn = true, children = [| LC.Text (word, styles = [| GridCellStyles.text |]) |])
            UiAdmin.GridCell (columnIndex = 1, widthUnits = columnWidths.[1], columnTotalUnits = columnTotalUnits, children = [| LC.Text (string word.Length, styles = [| GridCellStyles.text |]) |])
            UiAdmin.GridCell (columnIndex = 2, widthUnits = columnWidths.[2], columnTotalUnits = columnTotalUnits, children = [| LC.Text (string (Content_Grid.Demo.uniqueCharacterCount word), styles = [| GridCellStyles.text |]) |])
        }

    let queryForm (form: FormHandle<Field, Acc, Query>) =
        element {
            LC.Input.Text(
                label    = "Substring",
                validity = form.FieldValidity Field.Substring,
                value    = form.Acc.Substring,
                onChange = fun value -> form.UpdateAcc (fun acc -> { acc with Substring = value })
            )
            LC.Input.PositiveInteger(
                label    = "MinLength",
                validity = form.FieldValidity Field.MinLength,
                value    = form.Acc.MinLength,
                onChange = fun value -> form.UpdateAcc (fun acc -> { acc with MinLength = value })
            )
        }

module private QueryGridDemo =
    [<Component>]
    let Render () : ReactElement =
        let pageHook =
            Hooks.useState (
                Page.BlankPage (PositiveInteger.ofLiteral 10)
            )

        let executeQuery (queryPage: QueryPage<Query>) =
            async {
                do! Async.Sleep 1000
                let query = queryPage.Query
                return
                    Content_Grid.Demo.words
                    |> List.filter query.Predicate
                    |> Content_Grid.Demo.skipAtMost ((queryPage.PageNumber.Value - 1) * queryPage.PageSize.Value)
                    |> Content_Grid.Demo.takeAtMost queryPage.PageSize.Value
                    |> Seq.ofList
                    |> Available
            }

        UiAdmin.QueryGrid(
            mode            = OneTime executeQuery,
            page            = pageHook.current,
            onPageChange    = pageHook.update,
            initialQueryAcc = Acc.Empty,
            headers         = Sample.headers,
            row             = Sample.makeRow,
            queryForm       = Sample.queryForm
        )

type Ui.Content with
    [<Component>]
    static member QueryGrid () : ReactElement =
        Ui.ComponentContent(
            displayName = "QueryGrid",
            props       = ComponentContent.ForFullyQualifiedName "LibUiAdmin.Components.QueryGrid",
            notes =
                LC.Text
                    "QueryGrid is a paginated Grid that is type parametrized by 'Query, taking as props a chunk of UI through which the user inputs the query, and a query execution function.",
            a11y =
                Ui.A11yPanel(
                    componentName  = "QueryGrid",
                    role           = "table/grid with query form",
                    namePattern    = "Query inputs and column headers label the grid; results announced via child content",
                    stateNotes     = "Loading and empty states surfaced by AsyncData wrapper; pagination controls are buttons",
                    scalesWithFont = true,
                    contrastNotes  = "Query form and grid text colors meet WCAG AA",
                    deferredTags   = ["[web-only] table semantics"]
                ),
            samples =
                element {
                    LC.Fragment(
                        key = "query-grid-demo",
                        children =
                            [|
                                Ui.ComponentSample(
                                    verticalAlignment = VerticalAlignment.Top,
                                    visuals           = QueryGridDemo.Render(),
                                    code =
                                        ComponentSample.Children(
                                            element {
                                                Ui.Code(
                                                    language = Fsharp,
                                                    children =
                                                        [| LC.Text """
// This is run-of-the-mill form definition: a DU of fields, the result type that
// a successfully validated form produces (Query), and an accumulator (Acc) that
// implements AbstractAcc (from LibClient.Components.Form_Base.Types).
// (you can stub this out in VSCode using the `formacc` snippet)
[<RequireQualifiedAccess>]
type Field =
| Substring
| MinLength

type Query = {
    Substring: string
    MinLength: Option<PositiveInteger>
} with
    // this predicate is the only non-standard thing here, it's a helper
    // method that we use inside executeQuery below
    member this.Predicate (candidate: string) : bool =
        if candidate.Contains this.Substring then
            match this.MinLength with
            | Some minLength -> candidate.Length >= minLength.Value
            | None           -> true
        else
            false

type Acc = {
    Substring: Option<NonemptyString>
    MinLength: LibClient.Components.Input.PositiveInteger.Value
} with
    static member Empty : Acc = { Substring = None; MinLength = LibClient.Components.Input.PositiveInteger.empty }

    interface AbstractAcc<Field, Query> with
        member this.Validate () : Result<Query, ValidationErrors<Field>> = ...
""" |]
                                                )
                                                Ui.Code(
                                                    language = Fsharp,
                                                    children =
                                                        [| LC.Text """
// This code is essentially emulating a backend server that processes the query.
let executeQuery (queryPage: QueryPage<Query>) =
    async {
        do! Async.Sleep 1000
        let query = queryPage.Query
        return
            words
            |> List.filter query.Predicate
            |> skipAtMost ((queryPage.PageNumber.Value - 1) * queryPage.PageSize.Value)
            |> takeAtMost queryPage.PageSize.Value
            |> Seq.ofList
            |> Available
    }
""" |]
                                                )
                                                Ui.Code(
                                                    language = Fsharp,
                                                    children =
                                                        [| LC.Text """
let pageHook = Hooks.useState (Page.BlankPage (PositiveInteger.ofLiteral 10))

UiAdmin.QueryGrid(
    mode = OneTime executeQuery,
    page = pageHook.current,
    onPageChange = pageHook.update,
    initialQueryAcc = Acc.Empty,
    headers = headers,
    row = makeRow, // 'Item * Option<QueryPage<'Query>> * (unit -> unit) -> ReactElement
    queryForm = queryForm
)
""" |]
                                                )
                                            }
                                        )
                                )
                            |]
                    )
                }
        )
