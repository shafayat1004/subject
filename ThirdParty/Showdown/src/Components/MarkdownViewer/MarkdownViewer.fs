[<AutoOpen>]
module ThirdParty.Showdown.Components.MarkdownViewer

open Fable.React
open LibClient
open LibClient.Components
open LibClient.ServiceInstances
open Fable.Core
open Fable.Core.JsInterop
open LibClient.Services.HttpService.Types
open ReactXP.LegacyStyles.Css

do addCss ("""
.markdown-global-link {
    color:  #006ebdfc;
    cursor: pointer;
}
""")

let private showdown: obj = importDefault "showdown"
let defaultShowdownConverter: obj = createNew showdown?Converter ()

let makeCustomShowdownConverter (config: obj) : obj =
    createNew showdown?Converter config

type Source =
| Url  of string
| Code of string

[<Fable.Core.JS.Pojo>]
type private MarkdownDivStyleJs(
    whiteSpace: string,
    cursor: string,
    ``WebkitUserSelect``: string,
    ``MozUserSelect``: string,
    ``msUserSelect``: string,
    userSelect: string,
    wordWrap: string,
    maxWidth: string,
    lineHeight: string
) =
    member val whiteSpace = whiteSpace
    member val cursor = cursor
    member val ``WebkitUserSelect`` = ``WebkitUserSelect``
    member val ``MozUserSelect`` = ``MozUserSelect``
    member val ``msUserSelect`` = ``msUserSelect``
    member val userSelect = userSelect
    member val wordWrap = wordWrap
    member val maxWidth = maxWidth
    member val lineHeight = lineHeight

[<Fable.Core.JS.Pojo>]
type private DangerouslySetInnerHTMLJs(``__html``: string) =
    member val ``__html`` = ``__html``

[<Fable.Core.JS.Pojo>]
type private MarkdownDivPropsJs(style: obj, dangerouslySetInnerHTML: obj) =
    member val style = style
    member val dangerouslySetInnerHTML = dangerouslySetInnerHTML

[<Fable.Core.JS.Pojo>]
type private RenderHtmlSourceJs(html: string) =
    member val html = html

[<Fable.Core.JS.Pojo>]
type private RenderHtmlDivTagStyleJs(fontFamily: string, lineHeight: float, maxWidth: int) =
    member val fontFamily = fontFamily
    member val lineHeight = lineHeight
    member val maxWidth = maxWidth

[<Fable.Core.JS.Pojo>]
type private RenderHtmlTagsStylesJs(div: obj) =
    member val div = div

[<Fable.Core.JS.Pojo>]
type private RenderHtmlPropsJs(source: obj, tagsStyles: obj, systemFonts: string[], contentWidth: int) =
    member val source = source
    member val tagsStyles = tagsStyles
    member val systemFonts = systemFonts
    member val contentWidth = contentWidth

[<Fable.Core.JS.Pojo>]
type private HttpRequestOptionsJs(acceptType: string, customResponseType: string) =
    member val acceptType = acceptType
    member val customResponseType = customResponseType

let private processHtml (converter: obj) (maybeGlobalLinkHandler: Option<string>) (maybeImageUrlTransformer: Option<string -> string>) (source: string) : string =
    let rawHtml: string = converter?makeHtml source

    let linksProcessedHtml =
        #if EGGSHELL_PLATFORM_IS_WEB
        match maybeGlobalLinkHandler with
        | None -> rawHtml
        | Some globalLinkHandler ->
            let regex = System.Text.RegularExpressions.Regex """<a href="([^"]*)">(.*)</a>"""
            regex.Replace(rawHtml, "<a class='markdown-global-link' onclick=\"" + globalLinkHandler + "(event, '$1')\">$2</a>")
        #else
        rawHtml
        #endif

    match maybeImageUrlTransformer with
    | None -> linksProcessedHtml
    | Some imageUrlTransformer ->
        let regex = System.Text.RegularExpressions.Regex """(<img .*src=")([^"]*)(".*/>)"""
        regex.Replace(linksProcessedHtml, System.Text.RegularExpressions.MatchEvaluator(fun theMatch ->
            let beforeUrl = theMatch.Groups.Item(1).Value
            let url       = theMatch.Groups.Item(2).Value
            let afterUrl  = theMatch.Groups.Item(3).Value
            beforeUrl + (imageUrlTransformer url) + afterUrl
        ))

let makeHtml (converter: obj) (maybeGlobalLinkHandler: Option<string>) (maybeImageUrlTransformer: Option<string -> string>) (source: string) : ReactElement =
    let html = processHtml converter maybeGlobalLinkHandler maybeImageUrlTransformer source

    #if EGGSHELL_PLATFORM_IS_WEB
    let props =
        (MarkdownDivPropsJs(
            (MarkdownDivStyleJs(
                "normal",
                "text",
                "text",
                "text",
                "text",
                "text",
                "break-word",
                "800px",
                "1.5"
            ) |> box),
            (DangerouslySetInnerHTMLJs(html) |> box)
        )) |> box

    Fable.React.ReactBindings.React.createElement("div", props, [])

    #else

    let renderHtmlRaw: obj = import "default" "react-native-render-html"

    let sourceObj =
        (RenderHtmlSourceJs($"<div>{html}</div>"))
        |> box

    let tagsStyles =
        (RenderHtmlTagsStylesJs(
            (RenderHtmlDivTagStyleJs("Montserrat", 22.5, 800) |> box)
        )) |> box

    let props =
        (RenderHtmlPropsJs(sourceObj, tagsStyles, [|"Montserrat"|], 800))
        |> box

    Fable.React.ReactBindings.React.createElement(renderHtmlRaw, props, [||])

    #endif

type ThirdParty.Showdown.Components.Constructors.Showdown with
    [<Component>]
    static member MarkdownViewer(
            source:              Source,
            ?globalLinkHandler:  string,
            ?imageUrlTransformer: Source -> string -> string,
            ?showdownConverter:  obj,
            ?key:                string
        ) : ReactElement =
        ignore key

        let showdownConverter = defaultArg showdownConverter defaultShowdownConverter

        let initialSourceCode =
            match source with
            | Url _      -> WillStartFetchingSoonHack
            | Code code  -> Available code

        let sourceCodeHook = Hooks.useState initialSourceCode

        Hooks.useEffect(
            (fun () ->
                match source with
                | Url url ->
                    async {
                        let requestOptions =
                            (HttpRequestOptionsJs("text/plain", "text"))
                            |> box
                        let! response = services().Http.RequestReactXPRaw url HttpAction.Get (Some requestOptions)
                        sourceCodeHook.update (Available (response.body :?> string))
                    } |> startSafely
                | Code _ ->
                    sourceCodeHook.update (Available (match source with Code c -> c | Url _ -> ""))
            ),
            [| box source |]
        )

        LC.AsyncData(
            data = sourceCodeHook.current,
            whenAvailable =
                (fun sourceCode ->
                    makeHtml
                        showdownConverter
                        globalLinkHandler
                        (imageUrlTransformer |> Option.map (fun f -> f source))
                        sourceCode
                )
        )
