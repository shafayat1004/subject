[<AutoOpen>]
module AppEggShellGallery.Components.ComponentProps

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open LibRenderDSL.Types
open ReactXP.Components
open ReactXP.Styles
open Scraping.Types
open AppEggShellGallery.Colors

module dom = Fable.React.Standard

type XmlParam = {
    Name:        string
    Type:        string
    Default:     Option<string>
    Description: Option<string>
}

type Data = {
    Fields:            Choice<Result<List<TaggedRecordField>, string>, List<XmlParam>>
    MaybeScrapeErrors: Option<NonemptyList<ScrapeError>>
} with
    member this.FieldsWithoutKey : Result<List<XmlParam>, string> =
        match this.Fields with
        | Choice1Of2 fieldsRes ->
            fieldsRes
            |> Result.map (fun fields ->
                fields
                |> List.filterNot (fun field -> field.Name = "key")
                |> List.map (fun taggedRecordField ->
                    match taggedRecordField with
                    | TaggedRecordField.Regular (name, theType) ->
                        {
                            Name        = name
                            Type        = theType
                            Default     = None
                            Description = None
                        }
                    | TaggedRecordField.WithDefault (name, theType, theDefault)
                    | TaggedRecordField.WithDefaultAutoWrapSome (name, theType, theDefault) ->
                        {
                            Name        = name
                            Type        = theType
                            Default     = Some theDefault
                            Description = None
                        }
                )
            )
        | Choice2Of2 xmlParams ->
            xmlParams
            |> List.filterNot (fun param -> param.Name = "key")
            |> Ok

[<RequireQualifiedAccess>]
module private Styles =
    let view = makeViewStyles { marginBottom 16 }
    let contentIndented = makeViewStyles { marginLeft 16 }
    let error = makeViewStyles { marginVertical 10 }
    let errorText = makeTextStyles { color colors.Caution.Main }
    let heading = makeViewStyles { marginTop 8; marginBottom 8 }
    let metaContent = makeTextStyles { color (Color.Grey "cc") }
    let props = makeViewStyles {
        borderBottom 1 (Color.Grey "cc")
        marginBottom 5
        paddingBottom 5
    }

do ReactXP.LegacyStyles.Css.addCss (sprintf """
.aesg-ComponentProps-table {
    border-collapse:             collapse;
    display:                     block;
    overflow-x:                  auto;
    -webkit-overflow-scrolling:  touch;
    width:                       auto;
    max-width:                   100%%;
    /* The table renders inside a flex-column RX.View, which would otherwise stretch it to
       full width; align-self keeps it sized to its content. */
    align-self:                  flex-start;
}

.aesg-ComponentProps-table th {
    padding:       6px 18px 6px 0;
    text-align:    left;
    color:         #999999;
    font-weight:   600;
    font-size:     13px;
    border-bottom: 1px solid #eeeeee;
}

.aesg-ComponentProps-table tr:nth-child(even) {
    background-color: #fafafa;
}

.aesg-ComponentProps-table td {
    padding:        6px 18px 6px 0;
    color:          #666;
    font-family:    monospace;
    vertical-align: top;
}

.aesg-ComponentProps-table td.name {
    color: #000080;
}

.aesg-ComponentProps-table td.type {
    color: #990073;
}

.aesg-ComponentProps-table td.autowrapped {
    font-family: sans-serif;
    color: #cccccc;
    padding-left: 16px;
}

.aesg-ComponentProps-table td.value {
    color: #219161;
}

.aesg-ComponentProps-table td.description {
    color: #999999;
}
""")

let private renderScrapeErrors (errors: NonemptyList<ScrapeError>) : ReactElement =
    errors
    |> NonemptyList.toList
    |> List.map (fun error ->
        RX.View(
            styles = [| Styles.error |],
            children = [| LC.Text(sprintf "%A" error, styles = [| Styles.errorText |]) |]
        )
    )
    |> List.toArray
    |> castAsElement

let private renderFieldsTable (fields: List<XmlParam>) : ReactElement =
    #if EGGSHELL_PLATFORM_IS_WEB
    dom.table
        [ ClassName "aesg-ComponentProps-table dom-user-select-text" ]
        [|
            dom.thead
                []
                [|
                    dom.tr
                        []
                        [|
                            dom.th [] [| LC.Text "Name" |]
                            dom.th [] [| LC.Text "Type" |]
                            dom.th [] [||]
                            dom.th [] [| LC.Text "Default" |]
                            dom.th [] [| LC.Text "Description" |]
                        |]
                |]
            dom.tbody
                []
                [|
                    fields
                    |> List.mapi (fun i field ->
                        dom.tr
                            [ Key (string i) ]
                            [|
                                dom.td [ ClassName "name" ] [| LC.Text field.Name |]
                                dom.td [ ClassName "type" ] [| LC.Text field.Type |]
                                dom.td [] [||]
                                dom.td [ ClassName "value" ] [| LC.Text (field.Default |> Option.getOrElse "") |]
                                dom.td [ ClassName "description" ] [| LC.Text (field.Description |> Option.getOrElse "") |]
                            |]
                    )
                    |> Array.ofList
                    |> castAsElement
                |]
        |]
    #else
    fields
    |> List.mapi (fun i field ->
        RX.View(
            key = string i,
            styles = [| Styles.props |],
            children =
                [|
                    RX.View(children = [| LC.Text ("Name: " + field.Name) |])
                    RX.View(children = [| LC.Text ("Type: " + field.Type) |])
                    RX.View(children = [| LC.Text ("Default: " + (field.Default |> Option.getOrElse "")) |])
                    RX.View(children = [| LC.Text ("Description: " + (field.Description |> Option.getOrElse "")) |])
                |]
        )
    )
    |> Array.ofList
    |> castAsElement
    #endif

let private renderFieldsContent (fieldsResult: Result<List<XmlParam>, string>) : ReactElement =
    match fieldsResult with
    | Ok [] ->
        RX.View(children = [| LC.Text("No props", styles = [| Styles.metaContent |]) |])
    | Ok fields ->
        renderFieldsTable fields
    | Error error ->
        RX.View(
            styles = [| Styles.error |],
            children = [| LC.Text(error, styles = [| Styles.errorText |]) |]
        )

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member ComponentProps(
            data:           Data,
            ?children:      ReactChildrenProp,
            ?heading:       string,
            ?key:           string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        children |> ignore
        xLegacyStyles |> ignore

        RX.View(
            styles = [| Styles.view |],
            children =
                [|
                    heading
                    |> Option.map (fun text ->
                        RX.View(
                            styles = [| Styles.heading |],
                            children =
                                [|
                                    LC.Heading(
                                        level = Heading.Tertiary,
                                        children = [| LC.Text text |]
                                    )
                                |]
                        )
                    )
                    |> Option.defaultValue noElement

                    RX.View(
                        styles = (if Option.isSome heading then [| Styles.contentIndented |] else [||]),
                        children =
                            [|
                                data.MaybeScrapeErrors
                                |> Option.map renderScrapeErrors
                                |> Option.defaultValue noElement

                                renderFieldsContent data.FieldsWithoutKey
                            |]
                    )
                |]
        )
