[<AutoOpen>]
module ThirdParty.Showdown.Components.MarkdownViewer

open Fable.React
open LibClient
open LibClient.Components
open LibClient.ServiceInstances
open Fable.Core
open Fable.Core.JsInterop
open LibClient.Services.HttpService.Types
open Rn.LegacyStyles.Css

do addCss ("""
.markdown-global-link {
    color:  #006ebdfc;
    cursor: pointer;
}
""")

[<Fable.Core.Emit("globalThis")>]
let private jsGlobalThis: obj = jsNative

let private showdown: obj = importDefault "showdown"
let defaultShowdownConverter: obj = createNew showdown?Converter ()

let makeCustomShowdownConverter (config: obj) : obj =
    createNew showdown?Converter config

type Source =
| Url  of string
| Code of string

[<Fable.Core.JS.Pojo>]
type private MarkdownDivStyleJs(
    whiteSpace:           string,
    cursor:               string,
    ``WebkitUserSelect``: string,
    ``MozUserSelect``:    string,
    ``msUserSelect``:     string,
    userSelect:           string,
    wordWrap:             string,
    maxWidth:             string,
    lineHeight:           string
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
type private MarkdownDivPropsJs(className: string, style: obj, dangerouslySetInnerHTML: obj) =
    member val className = className
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
            "markdown-body",
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
    let dimensions: obj = import "Dimensions" "react-native"

    let sourceObj =
        (RenderHtmlSourceJs($"<div>{html}</div>"))
        |> box

    // react-native-render-html applies no default typography, so without per-tag styles every
    // element (h1..h6, strong, code, li, td) renders as flat body text -- the docs looked
    // unformatted on native. Give each tag a style so headings, emphasis, code, lists,
    // blockquotes and tables get visible hierarchy. NB: real table *grid* layout needs
    // @native-html/table-plugin (a WebView); without it table/tr/td render as stacked blocks,
    // so we at least box + bold the cells for legibility.
    // Headings also need an explicit lineHeight: RN's default line box is too short for the
    // larger heading glyphs, so descenders (y, g, p, q, j) get clipped without one. ~1.3x fontSize.
    let heading (fontSize: int) (marginTop: int) (color: string) =
        createObj [ "fontSize" ==> fontSize; "lineHeight" ==> int (round (float fontSize * 1.3)); "fontWeight" ==> "700"; "marginTop" ==> marginTop; "marginBottom" ==> 8; "color" ==> color ]

    let tagsStyles =
        createObj [
            "div"        ==> createObj [ "fontFamily" ==> "Montserrat"; "color" ==> "#24292e" ]
            "p"          ==> createObj [ "fontSize" ==> 15; "lineHeight" ==> 22; "marginTop" ==> 0; "marginBottom" ==> 12; "color" ==> "#24292e" ]
            "h1"         ==> heading 28 18 "#1a1a1a"
            "h2"         ==> heading 23 16 "#1a1a1a"
            "h3"         ==> heading 19 14 "#1a1a1a"
            "h4"         ==> heading 17 12 "#1a1a1a"
            "h5"         ==> heading 15 10 "#1a1a1a"
            "h6"         ==> heading 14 10 "#57606a"
            "a"          ==> createObj [ "color" ==> "#006ebd"; "textDecorationLine" ==> "none" ]
            "strong"     ==> createObj [ "fontWeight" ==> "700" ]
            "b"          ==> createObj [ "fontWeight" ==> "700" ]
            "em"         ==> createObj [ "fontStyle" ==> "italic" ]
            "code"       ==> createObj [ "fontFamily" ==> "monospace"; "fontSize" ==> 13; "backgroundColor" ==> "#eff1f3"; "color" ==> "#d6336c" ]
            "pre"        ==> createObj [ "fontFamily" ==> "monospace"; "fontSize" ==> 13; "backgroundColor" ==> "#f6f8fa"; "padding" ==> 12; "borderRadius" ==> 6; "marginBottom" ==> 12; "color" ==> "#24292e" ]
            "ul"         ==> createObj [ "marginBottom" ==> 12 ]
            "ol"         ==> createObj [ "marginBottom" ==> 12 ]
            "li"         ==> createObj [ "fontSize" ==> 15; "lineHeight" ==> 22; "marginBottom" ==> 4; "color" ==> "#24292e" ]
            "blockquote" ==> createObj [ "borderLeftWidth" ==> 4; "borderLeftColor" ==> "#d0d7de"; "paddingLeft" ==> 12; "marginBottom" ==> 12; "color" ==> "#57606a" ]
            "hr"         ==> createObj [ "backgroundColor" ==> "#d0d7de"; "height" ==> 1; "marginTop" ==> 16; "marginBottom" ==> 16 ]
            "table"      ==> createObj [ "borderWidth" ==> 1; "borderColor" ==> "#d0d7de"; "marginBottom" ==> 12 ]
            "th"         ==> createObj [ "fontWeight" ==> "700"; "padding" ==> 6; "borderWidth" ==> 1; "borderColor" ==> "#d0d7de"; "backgroundColor" ==> "#f6f8fa" ]
            "td"         ==> createObj [ "padding" ==> 6; "borderWidth" ==> 1; "borderColor" ==> "#d0d7de" ]
        ]

    let baseStyle =
        createObj [ "fontFamily" ==> "Montserrat"; "fontSize" ==> 15; "lineHeight" ==> 22; "color" ==> "#24292e" ]

    // Match the render width to the device so long lines wrap and code/tables don't overflow.
    let windowDimensions: obj = dimensions?get "window"
    let contentWidth = (windowDimensions?width |> unbox<float>) - 40.0

    // Native has no DOM onclick, so links would fall through to react-native-render-html's
    // default anchor renderer (opens the external browser). Route anchor presses to the same
    // global handler the web onclick uses (globalLinkHandler names a fn on globalThis that the
    // app registers) so internal doc links navigate in-app. onPress is (event, href, attribs, target).
    let renderersProps =
        match maybeGlobalLinkHandler with
        | Some handlerName ->
            createObj [
                "a" ==> createObj [
                    // onPress is (event, href, ...). render-html resolves a relative href against an
                    // "about://" base (so "./x.md" -> "about:///x.md"); the app-side handler normalizes
                    // that prefix back to a routable path.
                    // Must type the handler as a callable function: assigning to `obj` and applying it
                    // makes Fable emit a `throw` (an `obj` isn't callable in F#), which surfaced as an
                    // uncaught error on every link tap.
                    "onPress" ==> System.Func<obj, string, unit>(fun _event href ->
                        let handler: string -> unit = jsGlobalThis?(handlerName)
                        if not (isNullOrUndefined (box handler)) then handler href)
                ]
            ]
        | None -> createEmpty

    let props =
        createObj [
            "source"          ==> sourceObj
            "tagsStyles"      ==> tagsStyles
            "baseStyle"       ==> baseStyle
            "systemFonts"     ==> [| "Montserrat" |]
            "contentWidth"    ==> contentWidth
            "renderersProps"  ==> renderersProps
        ]

    Fable.React.ReactBindings.React.createElement(renderHtmlRaw, props, [||])

    #endif

type ThirdParty.Showdown.Components.Constructors.Showdown with
    [<Component>]
    static member MarkdownViewer(
            source:               Source,
            ?globalLinkHandler:   string,
            ?imageUrlTransformer: Source -> string -> string,
            ?showdownConverter:   obj,
            ?key:                 string
        ) : ReactElement =
        ignore key

        let showdownConverter = defaultArg showdownConverter defaultShowdownConverter

        let initialSourceCode =
            match source with
            | Url _     -> WillStartFetchingSoonHack
            | Code code -> Available code

        let sourceCodeHook = Hooks.useState initialSourceCode

        Hooks.useEffect(
            (fun () ->
                match source with
                | Url url ->
                    async {
                        let requestOptions =
                            (HttpRequestOptionsJs("text/plain", "text"))
                            |> box
                        let! response = services().Http.RequestRnRaw url HttpAction.Get (Some requestOptions)
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
