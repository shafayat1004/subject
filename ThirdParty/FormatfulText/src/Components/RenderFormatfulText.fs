[<AutoOpen>]
module ThirdParty.FormatfulText.Components.RenderFormatfulText

// All these modules are required for this component to work in both web and native
open Fable.React
open Fable.Core
open Fable.Core.JsInterop
open ReactXP.Styles
open FormatfulText.TextService

#if EGGSHELL_PLATFORM_IS_WEB
open LibClient.Components
#endif

[<Fable.Core.JS.Pojo>]
type private FormatfulTextDivStyleJs(
    whiteSpace: string,
    cursor: string,
    ``WebkitUserSelect``: string,
    ``MozUserSelect``: string,
    ``msUserSelect``: string,
    userSelect: string,
    wordWrap: string
) =
    member val whiteSpace = whiteSpace
    member val cursor = cursor
    member val ``WebkitUserSelect`` = ``WebkitUserSelect``
    member val ``MozUserSelect`` = ``MozUserSelect``
    member val ``msUserSelect`` = ``msUserSelect``
    member val userSelect = userSelect
    member val wordWrap = wordWrap

[<Fable.Core.JS.Pojo>]
type private SetInnerContentJs(``__html``: string) =
    member val ``__html`` = ``__html``

[<Fable.Core.JS.Pojo>]
type private FormatfulTextDivPropsJs(style: obj, className: string, dangerouslySetInnerHTML: obj) =
    member val style = style
    member val className = className
    member val dangerouslySetInnerHTML = dangerouslySetInnerHTML

[<Fable.Core.JS.Pojo>]
type private RenderHtmlSourceJs(html: string) =
    member val html = html

[<Fable.Core.JS.Pojo>]
type private RenderHtmlDivTagStyleJs(fontFamily: string) =
    member val fontFamily = fontFamily

[<Fable.Core.JS.Pojo>]
type private RenderHtmlTagsStylesJs(div: obj) =
    member val div = div

[<Fable.Core.JS.Pojo>]
type private RenderHtmlPropsJs(source: obj, tagsStyles: obj, systemFonts: string[]) =
    member val source = source
    member val tagsStyles = tagsStyles
    member val systemFonts = systemFonts

type FormatfulText with
    [<Component>]
    static member Render (
        text: FormatfulTextSource,
        ?styles: array<TextStyles>
    ): ReactElement =
        let markup =
            match text with
            | FormatfulTextSource.Html          source        -> source
            | FormatfulTextSource.MaybeMarkdown (_, rendered) -> rendered

        #if EGGSHELL_PLATFORM_IS_WEB
        let innerContentObj = SetInnerContentJs markup |> box
        let props =
            (FormatfulTextDivPropsJs(
                (FormatfulTextDivStyleJs(
                    "normal",
                    "text",
                    "text",
                    "text",
                    "text",
                    "text",
                    "break-word"
                ) |> box),
                "style-hack-if-it-contains-a-pre-tag",
                innerContentObj
            )) |> box

        LC.Text (?styles = styles, children = [|
            Fable.React.ReactBindings.React.createElement("div", props, [])
        |])

        #else

        let renderHtmlRaw: obj = import "default" "react-native-render-html"

        let source =
            (RenderHtmlSourceJs($"<div>{markup}</div>"))
            |> box

        let containerStyles : obj =
            styles
            |> Option.map
                (fun styles ->
                    styles
                    |> Array.fold
                        (fun accStyles currStyles ->
                            let currStyleEntries = Fable.Core.JS.Constructors.Object?entries(currStyles :> obj) :> obj :?> (string * obj)[]

                            [
                                yield! accStyles
                                yield! currStyleEntries
                            ]
                        )
                        [ "fontFamily" ==> "Montserrat" ]
                )
            |> Option.defaultValue []
            |> createObj

        let tagsStyles =
            (RenderHtmlTagsStylesJs(
                (RenderHtmlDivTagStyleJs("Montserrat") |> box)
            )) |> box

        let props =
            (RenderHtmlPropsJs(source, tagsStyles, [|"Montserrat"|]))
            |> box

        Fable.React.ReactBindings.React.createElement(
            renderHtmlRaw,
            props,
            [||]
        )

        #endif
