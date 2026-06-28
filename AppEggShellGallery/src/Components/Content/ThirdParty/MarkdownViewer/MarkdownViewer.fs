[<AutoOpen>]
module AppEggShellGallery.Components.Content_ThirdParty_MarkdownViewer

open Fable.React
open LibClient
open LibClient.Components
open ThirdParty.Showdown.Components
open ThirdParty.Showdown.Components.Constructors

type Ui.Content.ThirdParty with
    [<Component>]
    static member MarkdownViewer () : ReactElement =
        Ui.ComponentContent(
            displayName = "MarkdownViewer",
            props = ComponentContent.ForFullyQualifiedName "ThirdParty.Showdown.Components.MarkdownViewer",
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        Showdown.MarkdownViewer(
                                            source = MarkdownViewer.Code """# Hello Markdown

This is **bold** and this is *italic*.

- Item one
- Item two
"""
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Showdown.MarkdownViewer(
    source = MarkdownViewer.Code \"\"\"
# Hello Markdown
This is **bold** and this is *italic*.
\"\"\"
)"""
                                        )
                                )
                            }
                    )
                }
        )
