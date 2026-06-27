module AppEggShellGallery.Components.Code

open LibClient
open Fable.Core.JsInterop
open Fable.Core
open ThirdParty.SyntaxHighlighter.Components

// aliases for ~ access
let Render = SyntaxHighlighter.Language.Render
let Fsharp = SyntaxHighlighter.Language.Fsharp


type Props = (* GenerateMakeFunction *) {
    Language:               SyntaxHighlighter.Language
    Heading:                string option              // defaultWithAutoWrap None
    StripLeadingWhitespace: bool                       // default true


    key: string option // defaultWithAutoWrap JsUndefined
}

type Estate = {
    ProcessedSource: Result<string, string>
}

let private isNullOrUndefined (value: obj) =
    isNull value || value = JS.undefined

// for the life of me I couldn't find access to the JS `typeof` operator
let private isJsRuntimeString (value: obj) : bool =
    not (isNullOrUndefined value) && value?toLocaleLowerCase <> JS.undefined

[<Emit("Array.isArray($0)")>]
let private isJsArray (value: obj) : bool = jsNative

let rec private tryExtractStringChildren (source: obj) (maxDepth: int) : Result<string, string> =
    if isNullOrUndefined source then
        Error "no children"
    elif isJsRuntimeString source then
        Ok (string source)
    elif maxDepth <= 0 then
        Error "children are nested too deeply"
    elif isJsArray source then
        let children = source :?> obj[]
        if children.Length = 0 then
            Error "empty children"
        else
            tryExtractStringChildren children.[0] (maxDepth - 1)
    else
        let propsObj = source?props
        if isNullOrUndefined propsObj then
            Error "children are of an unknown type"
        else
            tryExtractStringChildren propsObj?children (maxDepth - 1)

// naive implementation, assumes that the first line's leading
// whitespace is what should be stripped from every line
let private stripLeadingWhitespace (source: string) : string =
    let lines =
        source.Split "\n"
        |> List.ofArray
        |> List.skipWhile (fun line -> line.TrimStart().Length = 0)
        |> List.rev
        |> List.skipWhile (fun line -> line.TrimStart().Length = 0)
        |> List.rev
        // FIXME yes, I'm really not inspired right now and can't think
        // of an easier way to trim from the end

    match lines with
    | [] -> source
    | head :: _ ->
        let headLeadingWhitespace = head.Substring (0, head.Length - head.TrimStart().Length)
        match headLeadingWhitespace with
        | "" -> source
        | _ ->
            // Don't think we can make this tail recursive...
            let rec trimStartLines (remainingLines: List<string>) : Result<List<string>, unit> =
                match remainingLines with
                | [] -> Ok []
                | head :: tail ->
                    match head = "" || head.StartsWith headLeadingWhitespace with
                    | true  ->
                        trimStartLines tail
                        |> Result.map (fun trimmedTail ->
                            let trimmedHead = if head = "" then "" else head.Substring headLeadingWhitespace.Length
                            trimmedHead :: trimmedTail
                        )
                    | false ->
                        // we hit a line that doesn't share the leading whitespace
                        // of the first line, so abort the whole attempt to strip
                        // leading whitespace
                        Error ()

            trimStartLines lines
            |> Result.toOption
            |> Option.getOrElse lines
            |> String.concat "\n"

let private processSource (reactProps: obj) (props: Props) : Result<string, string> =
    tryExtractStringChildren reactProps?children (* maxDepth *) 5
    |> Result.map (fun source ->
        match props.StripLeadingWhitespace with
        | true  -> stripLeadingWhitespace source
        | false -> source
    )

type Code(_initialProps) =
    inherit EstatefulComponent<Props, Estate, Actions, Code>("AppEggShellGallery.Components.Code", _initialProps, Actions, hasStyles = true)

    let updateProcessedSource (this: Code) =
        let processedSource = processSource (this.props :> obj) this.props
        this.SetEstate(fun _ _ -> { ProcessedSource = processedSource })

    override _.GetInitialEstate(_initialProps: Props) : Estate =
        // React children are not available on the Props record during construction.
        { ProcessedSource = Ok "" }

    override this.ComponentDidMount() =
        base.ComponentDidMount()
        updateProcessedSource this

    override this.ComponentDidUpdate(_prevProps: Props, _prevState: Estate) =
        base.ComponentDidUpdate(_prevProps, _prevState)
        updateProcessedSource this

and Actions(_this: Code) =
    class end

let Make = makeConstructor<Code, _, _>

// Unfortunately necessary boilerplate
type Pstate = NoPstate
