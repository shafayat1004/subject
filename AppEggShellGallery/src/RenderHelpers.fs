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

/// Resolve a markdown link href against the folder of the document that contains it, the same
/// way GitHub (and any standard markdown renderer) resolves relative links -- so `./sibling.md`,
/// `../other-section/page.md`, and bare `page.md` hrefs work identically in plain repo browsing
/// and in the gallery. Both `currentDocPath` and `href` are root-relative to public-dev/docs
/// (e.g. "modernization/index.md"); an in-page anchor on `href` (e.g. "page.md#section") is
/// preserved on the result. A leading `/` on `href` is docs-root-relative, matching
/// `check-doc-links.mjs`.
let resolveRelativeDocPath (currentDocPath: string) (href: string) : string =
    let hrefPath, fragment =
        match href.Split('#') |> List.ofArray with
        | []               -> "", None
        | [ hrefPath ]     -> hrefPath, None
        | hrefPath :: rest -> hrefPath, Some (String.concat "#" rest)

    let baseDirSegments =
        if hrefPath.StartsWith "/" then
            []
        else
            match currentDocPath.LastIndexOf '/' with
            | -1        -> []
            | lastSlash -> currentDocPath.Substring(0, lastSlash).Split('/') |> List.ofArray

    let resolvedSegments =
        hrefPath.TrimStart('/').Split('/')
        |> Array.fold (fun (acc: string list) segment ->
            match segment with
            | "" | "."                  -> acc
            | ".." when not acc.IsEmpty -> acc |> List.take (acc.Length - 1)
            | ".."                      -> acc
            | segment                   -> acc @ [ segment ]
        ) baseDirSegments

    let resolvedPath = String.concat "/" resolvedSegments
    match fragment with
    | Some fragment -> resolvedPath + "#" + fragment
    | None          -> resolvedPath

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
