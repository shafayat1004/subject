module ThirdParty.SyntaxHighlighter.Components.SyntaxHighlighter

// NOTE there are some project-specific tweaks here. Ideally they would sit in the
// project that uses this component, instead of the third party library, but I simply
// wanted to pull out the component as a sample third party wrapper, and didn't want
// to do a full cleanup refactoring.

open Fable.Core
open Fable.Core.JsInterop
open Fable.React

let private rawReactSyntaxHighliter: obj = import "Light" "react-syntax-highlighter"
let private rawLangXml: obj              = importDefault "react-syntax-highlighter/dist/esm/languages/hljs/xml"
let private rawLangFsharp: obj           = importDefault "react-syntax-highlighter/dist/esm/languages/hljs/fsharp"
let private rawStyleDocco: obj           = importDefault "react-syntax-highlighter/dist/esm/styles/hljs/docco"

// Even though according to the docs react-syntax-highlighter supports
// a customStyle prop, it does not work for some unknown reason. So instead
// we overwrite the
let private tweakedStyles = JS.Constructors.Object.assign (createEmpty, rawStyleDocco)
tweakedStyles?hljs?background <- "#ffffff"

rawReactSyntaxHighliter?registerLanguage("render", rawLangXml)
rawReactSyntaxHighliter?registerLanguage("fsharp", rawLangFsharp)

[<StringEnum>]
type Language =
| Render
| Fsharp

type Props = (* GenerateMakeFunction *) {
    Language: Language
    Source:   string
}

[<Emit("typeof $0 === 'string' ? $0 : ''")>]
let private ensureCodeString (_source: obj) : string = jsNative

let Make (props: Props) (_children: array<ReactElement>) : ReactElement =
    let source = ensureCodeString (box props.Source)
    // Pass source as a plain string in props.children only — do not also pass
    // React children via createElement's 3rd arg (wrapComponentTransformingProps
    // merged both and react-syntax-highlighter received a React element).
    ReactBindings.React.createElement(
        rawReactSyntaxHighliter,
        createObj [
            "language" ==> props.Language
            "style"    ==> tweakedStyles
            "children" ==> source
        ],
        [||]
    )
