[<AutoOpen>]
module AppEggShellGallery.Components.Content_QueryGrid

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Form.Base.Types
open LibUiAdmin.Components
open AppEggShellGallery.Components
open AppEggShellGallery.Components.ComponentSample
open AppEggShellGallery.Components.Content_Grid

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

module private Demo =
    let headers =
        element {
            UiAdmin.GridCell [| LC.HeaderCell(label = "Word") |]
            UiAdmin.GridCell [| LC.HeaderCell(label = "Character Count") |]
            UiAdmin.GridCell [| LC.HeaderCell(label = "Unique Character Count") |]
        }

    let makeRow (word: string, _, _refresh) =
        element {
            UiAdmin.GridCell [| LC.Text word |]
            UiAdmin.GridCell [| LC.Text (string word.Length) |]
            UiAdmin.GridCell [| LC.Text (string (Demo.uniqueCharacterCount word)) |]
        }

    let queryForm (form: LibUiAdmin.Components.Legacy.QueryGrid.FormHandle<Field, Acc, Query>) =
        element {
            LC.Input.Text(
                label = "Substring",
                validity = form.FieldValidity Field.Substring,
                value = form.Acc.Substring,
                onChange = fun value -> form.UpdateAcc (fun acc -> { acc with Substring = value })
            )
            LC.Input.PositiveInteger(
                label = "MinLength",
                validity = form.FieldValidity Field.MinLength,
                value = form.Acc.MinLength,
                onChange = fun value -> form.UpdateAcc (fun acc -> { acc with MinLength = value })
            )
        }

module private LegacyQueryGridDemo =
    [<Component>]
    let Render () : ReactElement =
        let executeQuery (query: Query) (pageSize: PositiveInteger) (pageNumber: PositiveInteger) (_order: LibUiAdmin.Components.Legacy.QueryGrid.Order) =
            async {
                do! Async.Sleep 1000
                return
                    Demo.words
                    |> List.filter query.Predicate
                    |> Demo.skipAtMost ((pageNumber.Value - 1) * pageSize.Value)
                    |> Demo.takeAtMost pageSize.Value
                    |> Seq.ofList
                    |> Available
            }

        UiAdmin.Legacy.QueryGrid(
            mode = LibUiAdmin.Components.Legacy.QueryGrid.OneTime executeQuery,
            initialQueryAcc = Acc.Empty,
            headers = Demo.headers,
            makeRow = Demo.makeRow,
            queryForm = Demo.queryForm
        )

type Ui.Content with
    [<Component>]
    static member QueryGrid () : ReactElement =
        Ui.ComponentContent(
            displayName = "QueryGrid",
            props = ComponentContent.ForFullyQualifiedName "LibUiAdmin.Components.QueryGrid",
            notes =
                LC.Text
                    "QueryGrid is a paginated Grid that is type parametrized by 'Query, taking as props a chunk of UI through which the user inputs the query, and a query execution function.",
            samples =
                element {
                    Ui.ComponentSample(
                        verticalAlignment = VerticalAlignment.Top,
                        visuals = LegacyQueryGridDemo.Render(),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = Fsharp,
                                        children =
                                            [| LC.Text """
// Form types: Field DU, Query result, Acc implementing AbstractAcc
type Field = | Substring | MinLength
type Query = { Substring: string; MinLength: Option<PositiveInteger> }
type Acc = { ... } with interface AbstractAcc<Field, Query> with ...
""" |]
                                    )
                                    Ui.Code(
                                        language = Fsharp,
                                        children =
                                            [| LC.Text """
UiAdmin.Legacy.QueryGrid(
    mode = OneTime executeQuery,
    initialQueryAcc = Acc.Empty,
    page = page,
    onPageChange = setPage,
    headers = headers,
    makeRow = makeRow,
    queryForm = queryForm
)
""" |]
                                    )
                                }
                            )
                    )
                }
        )
