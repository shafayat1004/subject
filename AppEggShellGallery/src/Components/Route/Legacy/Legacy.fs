[<AutoOpen>]
module AppEggShellGallery.Components.Route_Legacy

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components
open ThirdParty.Showdown.Components
open ThirdParty.Showdown.Components.Constructors
open AppEggShellGallery.AppServices
open AppEggShellGallery.Navigation
open AppEggShellGallery.RenderHelpers

type Ui.Route with
    [<Component>]
    static member Legacy(pstoreKey: string, item: LegacyItem) : ReactElement =
        element {
            LC.SetPageMetadata(title = "Design")
            LR.Route(
                scroll   = LibRouter.Components.Route.Vertical,
                children = [|
                    LC.Section.Padded(
                        children = [|
                            match item with
                            | LegacyItem.Markdown url ->
                                Showdown.MarkdownViewer(
                                    source            = docMarkdownSource url,
                                    globalLinkHandler = "globalMarkdownLinkHandler",
                                    currentDocPath    = url,
                                    showdownConverter = showdownConverterWithSyntaxHighlighting
                                )
                        |]
                    )
                |]
            )
        }
