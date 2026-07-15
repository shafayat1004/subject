[<AutoOpen>]
module LibClient.LegacyTextWrapping

open Rn.Components

let private isText (maybeParentFullyQualifiedName: Option<string>) : bool =
    match maybeParentFullyQualifiedName with
    | Some "Rn.Components.Text"
    | Some "Rn.Components.UiText"
    | Some "Rn.Components.AniText"
    | Some "Rn.Components.AniUiText"
    | Some "LibClient.Components.Text"
    | Some "LibClient.Components.LegacyText"
    | Some "LibClient.Components.UiText"
            | Some "LibClient.Components.LegacyUiText"
            | Some "AppEggShellGallery.Components.Code" -> true
    | _ -> false

let mutable wrapRawText : string -> Fable.React.ReactElement = fun (text: string) ->
    Rn.Text(children = [|Fable.React.Helpers.str text|])

let makeTextNode (text: string) (_siblingIndex: int, _siblingCount: int, maybeParentFullyQualifiedName: Option<string>) : Fable.React.ReactElement =
    if isText maybeParentFullyQualifiedName then
        Fable.React.Helpers.str text
    else
        wrapRawText text

let makeTextNode2 (maybeParentFullyQualifiedName: Option<string>) (text: string) : Fable.React.ReactElement =
    if isText maybeParentFullyQualifiedName then
        Fable.React.Helpers.str text
    else
        wrapRawText text
