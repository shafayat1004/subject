[<AutoOpen>]
module AppEggShellGallery.Components.Snippets

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles
open ThirdParty.Showdown.Components
open ThirdParty.Showdown.Components.Constructors
open AppEggShellGallery.Colors
open AppEggShellGallery

module dom = Fable.React.Standard

type Scope =
| All
| One of string
with
    member this.Filter (snippet: Scraping.Types.Snippet) : bool =
        match this with
        | All       -> true
        | One scope -> snippet.Scope = scope

[<RequireQualifiedAccess>]
module private Styles =
    let error = makeViewStyles {
        marginVertical 10
    }

    let errorText = makeTextStyles {
        color colors.Caution.Main
    }

    let snippetRow = makeViewStyles {
        marginBottom 24
        paddingBottom 16
        borderBottomWidth 1
        borderColor (Color.Grey "ee")
    }

    let snippetName = makeViewStyles {
        marginBottom 4
    }

    let snippetPrefix = makeTextStyles {
        marginBottom 8
        color (Color.Grey "66")
    }

    let snippetScope = makeTextStyles {
        marginTop 8
        color (Color.Grey "99")
    }

do
    Rn.LegacyStyles.Css.addCss """

.aesg-Snippets-table {
    border-collapse: collapse;
    width:           100%;
}

.aesg-Snippets-table th {
    padding:     0px 8px;
    text-align:  left;
    color:       #bbbbbb;
    font-weight: normal;
}

.aesg-Snippets-table tr:nth-child(even) {
    background-color: #fafafa;
}

.aesg-Snippets-table td {
    padding:        1em 8px;
    color:          #666;
    vertical-align: top;
}

.aesg-Snippets-table td.description {
    padding: 0 8px;
}

.aesg-Snippets-table td.nowrap {
    white-space: nowrap;
}
"""

let private renderError (error: string) : ReactElement =
    Rn.View(
        styles = [| Styles.error |],
        children =
            [|
                LC.Text(error, styles = [| Styles.errorText |])
            |]
    )

let private renderMarkdown (description: string) : ReactElement =
    Showdown.MarkdownViewer(
        source = MarkdownViewer.Code description,
        globalLinkHandler = "globalMarkdownLinkHandler"
    )

#if EGGSHELL_PLATFORM_IS_WEB
let private renderWebTable (snippets: list<Scraping.Types.Snippet>) (scope: Scope) : ReactElement =
    dom.table
        [ ClassName "aesg-Snippets-table dom-user-select-text" ]
        [|
            dom.tbody
                []
                [|
                    dom.tr
                        []
                        [|
                            dom.th [] [| LC.Text "Name" |]
                            dom.th [] [| LC.Text "Prefix" |]
                            dom.th [] [| LC.Text "Description" |]
                            if scope = All then
                                dom.th [] [| LC.Text "Scope" |]
                        |]

                    snippets
                    |> List.filter scope.Filter
                    |> List.map (fun snippet ->
                        dom.tr
                            [ Key snippet.Key ]
                            [|
                                dom.td [ ClassName "nowrap" ] [| LC.Text snippet.Key |]
                                dom.td [ ClassName "nowrap" ] [| LC.Text snippet.Prefix |]
                                dom.td [ ClassName "description" ] [| renderMarkdown snippet.Description |]
                                if scope = All then
                                    dom.td [] [| LC.Text snippet.Scope |]
                            |]
                    )
                    |> List.toArray
                    |> castAsElementAckingKeysWarning
                |]
        |]
#else
let private renderNativeList (snippets: list<Scraping.Types.Snippet>) (scope: Scope) : ReactElement =
    snippets
    |> List.filter scope.Filter
    |> List.map (fun snippet ->
        Rn.View(
            key = snippet.Key,
            styles = [| Styles.snippetRow |],
            children =
                [|
                    Rn.View(
                        styles = [| Styles.snippetName |],
                        children =
                            [|
                                LC.Heading(
                                    level = Heading.Tertiary,
                                    children = [| LC.Text snippet.Key |]
                                )
                            |]
                    )
                    LC.Text(snippet.Prefix, styles = [| Styles.snippetPrefix |])
                    renderMarkdown snippet.Description
                    if scope = All then
                        LC.Text(snippet.Scope, styles = [| Styles.snippetScope |])
                |]
        )
    )
    |> List.toArray
    |> castAsElementAckingKeysWarning
#endif

let private renderSnippetError (error: Scraping.Types.SnippetError) : ReactElement =
    renderError (sprintf "%A" error)

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member Snippets(
            ?children:      ReactChildrenProp,
            ?scope:         Scope,
            ?key:           string,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)
        let scope = defaultArg scope All

        Rn.View(
            children =
                [|
                    match ScrapedData.RenderDslSnippets.renderDslSnippetDataResult with
                    | Ok renderDslSnippetData ->
                        match renderDslSnippetData with
                        | Ok snippets ->
                            #if EGGSHELL_PLATFORM_IS_WEB
                            renderWebTable snippets scope
                            #else
                            renderNativeList snippets scope
                            #endif
                        | Error error ->
                            renderSnippetError error
                    | Error error ->
                        renderError error
                |]
        )
