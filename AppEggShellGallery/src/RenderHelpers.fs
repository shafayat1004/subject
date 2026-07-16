module AppEggShellGallery.RenderHelpers

open LibClient
open LibClient.ServiceInstances
open Fable.Core.JsInterop

/// The MarkdownViewer source for a doc path relative to public-dev/docs
/// (e.g. "modernization/index.md"). Web fetches it over HTTP; native has no server, so it
/// renders the bundled content directly (see DocsContent). This is the single place the two
/// platforms diverge, so every doc route can call it uniformly with no per-route #if.
let docMarkdownSource (relativePath: string) : ThirdParty.Showdown.Components.MarkdownViewer.Source =
    // A link may carry an in-page anchor (e.g. "runbooks/index.md#section"); the fragment is not
    // part of the file path, so drop it before resolving the document.
    let relativePath = relativePath.Split('#').[0]
#if EGGSHELL_PLATFORM_IS_WEB
    ThirdParty.Showdown.Components.MarkdownViewer.Source.Url
        ("/docs/" + relativePath |> services().Http.PrepareInBundleResourceUrl)
#else
    match AppEggShellGallery.DocsContent.tryGet relativePath with
    | Some content -> ThirdParty.Showdown.Components.MarkdownViewer.Source.Code content
    | None ->
        ThirdParty.Showdown.Components.MarkdownViewer.Source.Code
            (sprintf "# Not found\n\nNo bundled doc for `%s`.\n\nRun `node scripts/gen-docs-bundle.js`." relativePath)
#endif

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
