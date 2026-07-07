[<AutoOpen>]
module AppEggShellGallery.Components.Content_Pre

open Fable.React
open LibClient
open LibClient.Components
open Rn.Styles

module private SampleContent =
    let text =
        """
let x := y

(*
     __//
cd  /.__.\
    \ \/ /
 '__/    \
  \-      )
   \_____/
_____|_|____
 " "
*)
    """

module private Styles =
    let pre =
        makeTextStyles {
            backgroundColor (Color.Grey "42")
            color           Color.White
            padding         10
        }

type Ui.Content with
    [<Component>]
    static member Pre() : ReactElement =
        Ui.ComponentContent(
            displayName = "Pre",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.Pre",
            a11y =
                Ui.A11yPanel(
                    componentName = "LC.Pre",
                    role = "text (preformatted/code block)",
                    namePattern = "text prop content read as preformatted text",
                    stateNotes = "Static text; selectable",
                    scalesWithFont = true,
                    contrastNotes = "Preformatted text color meets WCAG AA on background"
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = LC.Pre(text = SampleContent.text, styles = [| Styles.pre |]),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Pre(
    text = sampleText,
    styles = [| preStyles |]
)
"""
                            )
                    )
                }
        )
