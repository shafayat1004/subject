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
            UiAdmin.GridCell (isFirstColumn = true, children = [| LC.HeaderCell(label = "Word") |])
            UiAdmin.GridCell [| LC.HeaderCell(label = "Character Count") |]
            UiAdmin.GridCell [| LC.HeaderCell(label = "Unique Character Count") |]
        }

    let makeWordRow (word: string) =
        element {
            UiAdmin.GridCell (isFirstColumn = true, children = [| LC.Text word |])
            UiAdmin.GridCell [| LC.Text (string word.Length) |]
            UiAdmin.GridCell [| LC.Text (string (Demo.uniqueCharacterCount word)) |]
        }

    let fruitHeaders =
        element {
            UiAdmin.GridCell (isFirstColumn = true, children = [| LC.HeaderCell(label = "Name") |])
            UiAdmin.GridCell [| LC.HeaderCell(label = "Color") |]
            UiAdmin.GridCell [| LC.HeaderCell(label = "Taste") |]
            UiAdmin.GridCell [| LC.HeaderCell(label = "Price") |]
        }

    let makeFruitRow ((name, color, taste, price): Demo.RowData) =
        element {
            UiAdmin.GridCell (isFirstColumn = true, children = [| LC.Text name |])
            UiAdmin.GridCell [| LC.Text color |]
            UiAdmin.GridCell [| LC.Text taste |]
            UiAdmin.GridCell [| LC.Text (string price) |]
        }

    let staticRows =
        lazy (
            [
                ("Mango", "Orange", "Sweet", 15)
                ("Kiwi", "Green", "Sweet and sour", 12)
                ("Lemon", "Yellow", "Sour", 8)
                ("Apple", "Green", "Sweet", 11)
            ]
            |> List.mapi (fun index row ->
                UiAdmin.GridRow (index, makeFruitRow row)
            )
            |> Array.ofList
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
            headers = Samples.wordHeaders
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
                                headers = Samples.fruitHeaders
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
                                headers = Samples.fruitHeaders
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
