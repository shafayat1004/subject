[<AutoOpen>]
module AppEggShellGallery.Components.Route_Docs

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open LibRouter.Components
open ThirdParty.Showdown.Components
open ThirdParty.Showdown.Components.Constructors
open AppEggShellGallery.AppServices
open AppEggShellGallery.RenderHelpers

module dom = Fable.React.Standard

do
    Rn.LegacyStyles.Css.addCss """
.url-unsorted-directory-structure_md code {
    font-size:   12px;
    line-height: 14px;
}
"""

type Ui.Route with
    [<Component>]
    static member Docs(pstoreKey: string, markdownUrl: string) : ReactElement =
        element {
            LC.SetPageMetadata(title = "Docs")
            LR.Route(
                scroll   = LibRouter.Components.Route.Vertical,
                children = [|
                    LC.Section.Padded(
                        children = [|
                            #if EGGSHELL_PLATFORM_IS_WEB
                            dom.div
                                [ ClassName (sprintf "url-%s" (markdownUrl.Replace("/", "-").Replace(".", "_"))) ]
                                [|
                                    Showdown.MarkdownViewer(
                                        source            = docMarkdownSource markdownUrl,
                                        globalLinkHandler = "globalMarkdownLinkHandler",
                                        showdownConverter = showdownConverterWithSyntaxHighlighting
                                    )
                                |]
                            #else
                            // Native has no docs server; render the bundled markdown directly.
                            // globalLinkHandler routes internal doc links back into the app.
                            Showdown.MarkdownViewer(
                                source            = docMarkdownSource markdownUrl,
                                globalLinkHandler = "globalMarkdownLinkHandler",
                                showdownConverter = showdownConverterWithSyntaxHighlighting
                            )
                            #endif
                        |]
                    )
                |]
            )
        }
