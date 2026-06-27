[<AutoOpen>]
module AppEggShellGallery.Components.Content_Grid

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open LibClient.RenderHelpers
open LibUiAdmin.Components
open ReactXP.Components
open AppEggShellGallery.Components
open AppEggShellGallery.Components.ComponentSample

module dom = Fable.React.Standard

module Demo =
    type RowData = string * string * string * int

    /// Native column widths in 20px units (from legacy `col-w-*` convention).
    [<RequireQualifiedAccess>]
    module ColumnWidths =
        let word = [14; 7; 9]
        let fruit = [10; 6; 10; 4]

    let fruit: seq<RowData> =
        [
            ("Mango", "Orange", "Sweet", 15)
            ("Kiwi", "Green", "Sweet and sour", 12)
            ("Lemon", "Yellow", "Sour", 8)
            ("Apple", "Green", "Sweet", 11)
        ]
        |> Seq.ofList

    let words =
        [
            "accoutrements"; "acumen"; "anomalistic"; "auspicious"; "bellwether"
            "callipygian"; "circumlocution"; "concupiscent"; "conviviality"; "coruscant"
            "cuddlesome"; "cupidity"; "cynosure"; "ebullient"; "equanimity"
            "excogitate"; "gasconading"; "idiosyncratic"; "luminescent"; "magnanimous"
            "nidificate"; "osculator"; "parsimonious"; "penultimate"; "perfidiousness"
            "perspicacious"; "proficuous"; "remunerative"; "saxicolous"; "sesquipedalian"
            "superabundant"; "unencumbered"; "unparagoned"; "usufruct"; "winebibber"
        ]

    let uniqueCharacterCount (s: string) : int =
        s.ToCharArray() |> Set.ofSeq |> Set.count

    let skipAtMost (n: int) (source: list<'T>) : list<'T> =
        try source |> List.skip n with _ -> List.empty

    let takeAtMost (n: int) (source: list<'T>) : list<'T> =
        try source |> List.take n with _ -> source

module private Samples =
    let wordHeaders =
        element {
            UiAdmin.GridCell (columnIndex = 0, widthUnits = Demo.ColumnWidths.word.[0], isFirstColumn = true, children = [| LC.HeaderCell(label = "Word") |])
            UiAdmin.GridCell (columnIndex = 1, widthUnits = Demo.ColumnWidths.word.[1], children = [| LC.HeaderCell(label = "Character Count") |])
            UiAdmin.GridCell (columnIndex = 2, widthUnits = Demo.ColumnWidths.word.[2], children = [| LC.HeaderCell(label = "Unique Character Count") |])
        }

    let makeWordRow (word: string) =
        element {
            UiAdmin.GridCell (columnIndex = 0, widthUnits = Demo.ColumnWidths.word.[0], isFirstColumn = true, children = [| LC.Text (word, styles = [| GridCellStyles.text |]) |])
            UiAdmin.GridCell (columnIndex = 1, widthUnits = Demo.ColumnWidths.word.[1], children = [| LC.Text (string word.Length, styles = [| GridCellStyles.text |]) |])
            UiAdmin.GridCell (columnIndex = 2, widthUnits = Demo.ColumnWidths.word.[2], children = [| LC.Text (string (Demo.uniqueCharacterCount word), styles = [| GridCellStyles.text |]) |])
        }

    let fruitHeaders =
        element {
            UiAdmin.GridCell (columnIndex = 0, widthUnits = Demo.ColumnWidths.fruit.[0], isFirstColumn = true, children = [| LC.HeaderCell(label = "Name") |])
            UiAdmin.GridCell (columnIndex = 1, widthUnits = Demo.ColumnWidths.fruit.[1], children = [| LC.HeaderCell(label = "Color") |])
            UiAdmin.GridCell (columnIndex = 2, widthUnits = Demo.ColumnWidths.fruit.[2], children = [| LC.HeaderCell(label = "Taste") |])
            UiAdmin.GridCell (columnIndex = 3, widthUnits = Demo.ColumnWidths.fruit.[3], children = [| LC.HeaderCell(label = "Price") |])
        }

    let makeFruitRow ((name, color, taste, price): Demo.RowData) =
        element {
            UiAdmin.GridCell (columnIndex = 0, widthUnits = Demo.ColumnWidths.fruit.[0], isFirstColumn = true, children = [| LC.Text (name, styles = [| GridCellStyles.text |]) |])
            UiAdmin.GridCell (columnIndex = 1, widthUnits = Demo.ColumnWidths.fruit.[1], children = [| LC.Text (color, styles = [| GridCellStyles.text |]) |])
            UiAdmin.GridCell (columnIndex = 2, widthUnits = Demo.ColumnWidths.fruit.[2], children = [| LC.Text (taste, styles = [| GridCellStyles.text |]) |])
            UiAdmin.GridCell (columnIndex = 3, widthUnits = Demo.ColumnWidths.fruit.[3], children = [| LC.Text (string price, styles = [| GridCellStyles.text |]) |])
        }

    let staticRows =
        lazy (
            let rows =
                [
                    ("Mango", "Orange", "Sweet", 15)
                    ("Kiwi", "Green", "Sweet and sour", 12)
                    ("Lemon", "Yellow", "Sour", 8)
                    ("Apple", "Green", "Sweet", 11)
                ]
            let lastIndex = rows.Length - 1

            rows
            |> List.mapi (fun index row ->
                UiAdmin.GridRow (index, makeFruitRow row, showBottomBorder = (index < lastIndex))
            )
            |> List.toArray
            |> castAsElement
        )

module private PaginatedWordsDemo =
    [<Component>]
    let Render () : ReactElement =
        let goToPageRef = Hooks.useRef (fun (_pageSize: PositiveInteger) (_pageNumber: PositiveInteger) (_e: Option<ReactEvent.Action>) -> ())

        let makeInitialPage () : LibUiAdmin.Components.Grid.PaginatedGridData<string> =
            {
                PageNumber          = PositiveInteger.ofLiteral 1
                PageSize            = PositiveInteger.ofLiteral 5
                MaybePageCount      = None
                Items               = Available (Demo.words |> Demo.takeAtMost 5 |> Seq.ofList)
                GoToPage            = goToPageRef.current
                MaybeTotalItemCount = None
            }

        let currentPageHook = Hooks.useState (makeInitialPage ())

        let goToPage (pageSize: PositiveInteger) (pageNumber: PositiveInteger) (_e: Option<ReactEvent.Action>) =
            currentPageHook.update (fun _ ->
                {
                    PageNumber          = pageNumber
                    PageSize            = pageSize
                    MaybePageCount      = None
                    Items               =
                        Available (
                            Demo.words
                            |> Demo.skipAtMost ((pageNumber.Value - 1) * pageSize.Value)
                            |> Demo.takeAtMost pageSize.Value
                            |> Seq.ofList
                        )
                    GoToPage            = goToPageRef.current
                    MaybeTotalItemCount = None
                })

        goToPageRef.current <- goToPage

        UiAdmin.Grid(
            input = LibUiAdmin.Components.Grid.Paginated(currentPageHook.current, Samples.makeWordRow, None),
            headers = Samples.wordHeaders,
            nativeColumnWidthUnits = Demo.ColumnWidths.word
        )

type Ui.Content with
    [<Component>]
    static member Grid () : ReactElement =
        Ui.ComponentContent(
            displayName = "Grid",
            props = ComponentContent.ForFullyQualifiedName "LibUiAdmin.Components.Grid",
            notes =
                LC.Text
                    "The grid is currently fairly basic, we're building it out as we go. If you have needs that are currently not supported, tell Anton and we'll make it happen. Also see QueryGrid and WithSortAndFilter for additional options.",
            samples =
                element {
                    Ui.ComponentSample(
                        heading = "Dynamic asynchronous rows, paginated",
                        verticalAlignment = VerticalAlignment.Top,
                        visuals = PaginatedWordsDemo.Render(),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = Fsharp,
                                        children =
                                            [| LC.Text """
type PaginatedGridData<'T> = {
    PageNumber:     PositiveInteger
    PageSize:       PositiveInteger
    MaybePageCount: Option<UnsignedInteger>
    Items:          AsyncData<seq<'T>>
    GoToPage:       PositiveInteger -> PositiveInteger -> Option<ReactEvent.Action> -> unit
}
""" |]
                                    )
                                    Ui.Code(
                                        language = Render,
                                        children =
                                            [| LC.Text """
UiAdmin.Grid(
    input = Paginated (currentPage, makeWordRow, None),
    headers = wordHeaders
)
""" |]
                                    )
                                }
                            )
                    )

                    Ui.ComponentSample(
                        heading = "Dynamic asynchronous rows, displayed in full",
                        verticalAlignment = VerticalAlignment.Top,
                        visuals =
                            UiAdmin.Grid(
                                input = LibUiAdmin.Components.Grid.Everything(Available Demo.fruit, Samples.makeFruitRow, None),
                                headers = Samples.fruitHeaders,
                                nativeColumnWidthUnits = Demo.ColumnWidths.fruit
                            ),
                        code =
                            ComponentSample.Children(
                                element {
                                    Ui.Code(
                                        language = Fsharp,
                                        children =
                                            [| LC.Text """
let fruit: seq<string * string * string * int> = ...
UiAdmin.Grid(
    input = Everything (Available fruit, makeFruitRow, None),
    headers = fruitHeaders
)
""" |]
                                    )
                                }
                            )
                    )

                    Ui.ComponentSample(
                        heading = "Static, hardcoded rows",
                        verticalAlignment = VerticalAlignment.Top,
                        visuals =
                            UiAdmin.Grid(
                                input = LibUiAdmin.Components.Grid.Static(Samples.staticRows.Value, None),
                                headers = Samples.fruitHeaders,
                                nativeColumnWidthUnits = Demo.ColumnWidths.fruit
                            ),
                        code =
                            ComponentSample.singleBlock Render (
                                LC.Text """
UiAdmin.Grid(
    input = Static (rows, None),
    headers = headers
)
"""
                            )
                    )
                }
        )
