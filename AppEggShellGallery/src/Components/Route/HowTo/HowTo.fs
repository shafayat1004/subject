[<AutoOpen>]
module AppEggShellGallery.Components.Route_HowTo

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
    static member HowTo(pstoreKey: string, item: HowToItem) : ReactElement =
        element {
            LC.SetPageMetadata(title = "How To")
            LR.Route(
                scroll   = LibRouter.Components.Route.Vertical,
                children = [|
                    LC.Section.Padded(
                        children = [|
                            match item with
                            | HowToItem.Markdown url ->
                                Showdown.MarkdownViewer(
                                    source              = docMarkdownSource url,
                                    globalLinkHandler   = "globalMarkdownLinkHandler",
                                    currentDocPath      = url,
                                    imageUrlTransformer = markdownImageUrlTransformer,
                                    showdownConverter   = showdownConverterWithSyntaxHighlighting
                                )
                        |]
                    )
                |]
            )
        }
