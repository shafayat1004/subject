[<AutoOpen>]
module ThirdParty.Showdown.Components.MarkdownViewer

open Fable.React
open LibClient
open LibClient.Components
open LibClient.ServiceInstances
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
    let props = createObj [
        "style" ==> createObj [
            "whiteSpace"       ==> "normal"
            "cursor"           ==> "text"
            "WebkitUserSelect" ==> "text"
            "MozUserSelect"    ==> "text"
            "msUserSelect"     ==> "text"
            "userSelect"       ==> "text"
            "wordWrap"         ==> "break-word"
            "maxWidth"         ==> "800px"
            "lineHeight"       ==> "1.5"
        ]
        "dangerouslySetInnerHTML" ==> createObj [
            "__html" ==> html
        ]
    ]

    Fable.React.ReactBindings.React.createElement("div", props, [])

    #else

    let renderHtmlRaw: obj = import "default" "react-native-render-html"

    let sourceObj =
        createObj [
            "html" ==> $"<div>{html}</div>"
        ]

    let tagsStyles =
        createObj [
            "div" ==> (createObj [
                "fontFamily" ==> "Montserrat"
                "lineHeight"   ==> 22.5
                "maxWidth"     ==> 800
            ])
        ]

    let props =
        createObj [
            "source"      ==> sourceObj
            "tagsStyles"  ==> tagsStyles
            "systemFonts" ==> [|"Montserrat"|]
            "contentWidth" ==> 800
        ]

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
                        let requestOptions = createObj [
                            "acceptType"         ==> "text/plain"
                            "customResponseType" ==> "text"
                        ]
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
