[<AutoOpen>]
module AppEggShellGallery.Components.Route_Subject

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components
open ThirdParty.Showdown.Components
open ThirdParty.Showdown.Components.Constructors
open AppEggShellGallery.AppServices
open AppEggShellGallery.RenderHelpers

type Ui.Route with
    [<Component>]
    static member Subject(pstoreKey: string, markdownUrl: string) : ReactElement =
        element {
            LC.SetPageMetadata(title = "Subject")
            LR.Route(
                scroll   = LibRouter.Components.Route.Vertical,
                children = [|
                    LC.Section.Padded(
                        children = [|
                            Showdown.MarkdownViewer(
                                source              = docMarkdownSource markdownUrl,
                                globalLinkHandler   = "globalMarkdownLinkHandler",
                                currentDocPath      = markdownUrl,
                                imageUrlTransformer = markdownImageUrlTransformer,
                                showdownConverter   = showdownConverterWithSyntaxHighlighting
                            )
                        |]
                    )
                |]
            )
        }
