[<AutoOpen>]
module AppEggShellGallery.Components.Route_Tools

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components
open ThirdParty.Showdown.Components
open ThirdParty.Showdown.Components.Constructors
open AppEggShellGallery.AppServices
open AppEggShellGallery.Components.Snippets
open AppEggShellGallery.RenderHelpers

type Ui.Route with
    [<Component>]
    static member Tools(pstoreKey: string, markdownUrl: string) : ReactElement =
        element {
            LC.SetPageMetadata(title = "Tools")
            LR.Route(
                scroll = LibRouter.Components.Route.Vertical,
                children = [|
                    LC.Section.Padded(
                        children = [|
                            Showdown.MarkdownViewer(
                                source = MarkdownViewer.Url ("/docs/" + markdownUrl |> services().Http.PrepareInBundleResourceUrl),
                                globalLinkHandler = "globalMarkdownLinkHandler",
                                showdownConverter = showdownConverterWithSyntaxHighlighting
                            )
                            if markdownUrl = "tools/snippets.md" then
                                element {
                                    Showdown.MarkdownViewer(
                                        source = MarkdownViewer.Code "## RenderDSL scope snippets"
                                    )
                                    Ui.Snippets(scope = Scope.One "renderdsl")
                                    Showdown.MarkdownViewer(
                                        source = MarkdownViewer.Code "## F# scope snippets"
                                    )
                                    Ui.Snippets(scope = Scope.One "fsharp")
                                }
                        |]
                    )
                |]
            )
        }
