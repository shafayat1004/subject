[<AutoOpen>]
module AppEggShellGallery.Components.Code

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles
open ThirdParty.SyntaxHighlighter.Components

module dom = Fable.React.Standard

// Aliases for ~ access and legacy fully-qualified references.
let Render = SyntaxHighlighter.Language.Render
let Fsharp = SyntaxHighlighter.Language.Fsharp

let private isNullOrUndefined (value: obj) =
    isNull value || value = JS.undefined

[<Emit("typeof $0 === 'string'")>]
let private isJsRuntimeString (_value: obj) : bool = jsNative

[<Emit("Array.isArray($0)")>]
let private isJsArray (_value: obj) : bool = jsNative

[<Emit("$0 != null && typeof $0 === 'object' && $0.$$typeof != null")>]
let private isReactElement (_value: obj) : bool = jsNative

let private asPlainString (value: obj) : Result<string, string> =
    if isJsRuntimeString value then
        Ok (string value)
    elif isReactElement value then
        Error "children are a React element, not plain text"
    else
        Error "children are of an unknown type"

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
            children
            |> Array.fold (fun acc child ->
                match acc with
                | Error _ -> acc
                | Ok soFar ->
                    tryExtractStringChildren child (maxDepth - 1)
                    |> Result.map (fun part -> soFar + part)
            ) (Ok "")
            |> Result.bind (fun combined ->
                if combined = "" then Error "empty children" else Ok combined
            )
    else
        let propsObj = source?props
        if isNullOrUndefined propsObj then
            Error "children are of an unknown type"
        else
            let child = propsObj?children
            if isNullOrUndefined child then
                let value = propsObj?value
                if isNullOrUndefined value then
                    Error "children are of an unknown type"
                else
                    asPlainString value
            else
                tryExtractStringChildren child (maxDepth - 1)

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

    match lines with
    | [] -> source
    | head :: _ ->
        let headLeadingWhitespace = head.Substring (0, head.Length - head.TrimStart().Length)
        match headLeadingWhitespace with
        | "" -> source
        | _ ->
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
                    | false -> Error ()

            trimStartLines lines
            |> Result.toOption
            |> Option.getOrElse lines
            |> String.concat "\n"

let private processSource (children: ReactChildrenProp) (shouldStripLeadingWhitespace: bool) : Result<string, string> =
    tryExtractStringChildren (children :> obj) (* maxDepth *) 5
    |> Result.map (fun source ->
        match shouldStripLeadingWhitespace with
        | true  -> stripLeadingWhitespace source
        | false -> source
    )

[<RequireQualifiedAccess>]
module private Styles =
    let view = makeViewStyles { marginVertical 20 }
    let headingText = makeTextStyles { fontSize 14 }

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member Code(
            language:               SyntaxHighlighter.Language,
            ?children:              ReactChildrenProp,
            ?stripLeadingWhitespace: bool,
            ?heading:               string,
            ?key:                   string,
            ?xLegacyStyles:         List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        xLegacyStyles |> ignore

        let childrenArray = children |> Option.defaultValue [||]
        let strip = defaultArg stripLeadingWhitespace true

        Rn.View(
            styles = [| Styles.view |],
            children =
                [|
                    heading
                    |> Option.map (fun text ->
                        LC.Text(text, styles = [| Styles.headingText |])
                    )
                    |> Option.defaultValue noElement

                    match processSource childrenArray strip with
                    | Ok source ->
                        let sourceString =
                            let o = source :> obj
                            if isJsRuntimeString o then string o else ""

                        #if EGGSHELL_PLATFORM_IS_WEB
                        if sourceString = "" then
                            Rn.View(children = [| LC.Text "no code sample text" |])
                        else
                            LC.Pre(text = sourceString)
                        #else
                        LC.Text(sourceString, selectable = true)
                        #endif
                    | Error message ->
                        Rn.View(
                            children = [| LC.Text message |]
                        )
                |]
        )
