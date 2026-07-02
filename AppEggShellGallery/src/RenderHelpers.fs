module AppEggShellGallery.RenderHelpers

open LibClient
open Fable.Core.JsInterop

let showdownConverterWithSyntaxHighlighting =
    ThirdParty.Showdown.Components.MarkdownViewer.makeCustomShowdownConverter
        (createObj [
            "extensions"          ==> [|importDefault "showdown-highlight"|]
            // GFM pipe tables are off by default in Showdown 2.x; enable them so the
            // many tables across the docs render as real tables (not raw `| a | b |` text).
            "tables"              ==> true
            "ghCompatibleHeaderId" ==> true
            "simplifiedAutoLink"  ==> true
            "strikethrough"       ==> true
            "tasklists"           ==> true
        ])

let markdownImageUrlTransformer (source: ThirdParty.Showdown.Components.MarkdownViewer.Source) (imageUrl: string) : string =
    if imageUrl.StartsWith "./" then
        match source with
        | ThirdParty.Showdown.Components.MarkdownViewer.Source.Url documentUrl ->
            let parts = documentUrl.Split "/" |> List.ofArray
            List.append (parts |> List.take (parts.Length - 1)) [imageUrl.Substring 2]
            |> String.concat "/"
        | _ -> imageUrl
    else
        imageUrl
