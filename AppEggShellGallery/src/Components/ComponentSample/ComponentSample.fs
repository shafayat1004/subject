[<AutoOpen>]
module AppEggShellGallery.Components.ComponentSample

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open LibClient.Responsive
open Rn.Components
open Rn.Styles
open ThirdParty.SyntaxHighlighter.Components
open AppEggShellGallery

module dom = Fable.React.Standard

// Aliases for ~ access and legacy fully-qualified references.
let Render = SyntaxHighlighter.Language.Render
let Fsharp = SyntaxHighlighter.Language.Fsharp

type Code =
| SingleBlock of SyntaxHighlighter.Language * ReactElement
| Children    of ReactElement

let singleBlock (language: SyntaxHighlighter.Language) (children: ReactElement) : Code =
    SingleBlock (language, children)

type VerticalAlignment =
| Top
| Middle
with
    member this.Class : string =
        unionCaseName this

type Layout =
| SideBySide
| CodeBelowSamples

[<RequireQualifiedAccess>]
module private Styles =
    let visuals =
        makeViewStyles {
            paddingVertical 16
            minWidth 300
            Overflow.Visible
        }

    let codeAndNotes =
        makeViewStyles {
            marginLeft 40
            paddingVertical 30
            minWidth 300
        }

    let code =
        makeViewStyles {
            JustifyContent.Center
            Overflow.Visible
        }

do
    Rn.LegacyStyles.Css.addCss """

.aesg-ContentComponent-table .cs-heading {
    border-top:    24px solid transparent;
    border-bottom: 2px  solid transparent;
}

.aesg-ContentComponent-table .vertical-align-top {
    vertical-align: top;
}

.aesg-ContentComponent-table .vertical-align-middle {
    vertical-align: middle;
}

.aesg-ContentComponent-table .layout-code-below-samples {
    display: block;
}

/* Handheld (<900px): stack visuals above code instead of side-by-side.
   Matches the LibClient Responsive breakpoint (width < 900 = Handheld).
   The horizontal ScrollView wrapper is removed on handheld in ComponentContent.fs,
   so there is no overflow-y:hidden container to clip the taller stacked rows. */
@media (max-width: 899px) {
    .aesg-ContentComponent-table tr,
    .aesg-ContentComponent-table td {
        display:    block;
        box-sizing: border-box;
        max-width:  100%;
    }
    /* Override the inline margin-left (40px) and min-width (300px) that Rn.View
       applies in the side-by-side code cell — not needed when stacked. */
    .aesg-ContentComponent-table td > div {
        margin-left: 0 !important;
        min-width:   0 !important;
    }
}

"""

let private visualsTdClass (verticalAlignment: VerticalAlignment) (layout: Layout) =
    sprintf " %s %s %s"
        (if verticalAlignment = VerticalAlignment.Middle then "vertical-align-middle" else "")
        (if verticalAlignment = VerticalAlignment.Top then "vertical-align-top" else "")
        (if layout = Layout.CodeBelowSamples then "layout-code-below-samples" else "")

let private codeTdClass (layout: Layout) =
    sprintf " %s" (if layout = Layout.CodeBelowSamples then "layout-code-below-samples" else "")

let private renderCode (code: Code) =
    match code with
    | SingleBlock (language, codeElement) ->
        Ui.Code(language = language, children = [| codeElement |])
    | Children children ->
        children

let private renderNotes (notes: ReactElement) =
    if notes = noElement then
        noElement
    else
        Rn.View(children = [| notes |])

let private renderCodeAndNotes (code: Code) (notes: ReactElement) =
    Rn.View(
        styles = [| Styles.codeAndNotes |],
        children =
            [|
                Rn.View(
                    styles   = [| Styles.code |],
                    children = [| renderCode code |]
                )
                renderNotes notes
            |]
    )

let private renderVisuals (visuals: ReactElement) =
    // Automation testId: scopes audit recipes to demo visuals (web `[data-testid]`, Android resource-id).
    // Sidebar nav testIds: eggshell-sidebar-menu, sidebar-blade-*, sidebar-component-{CaseName} (SidebarContent.fs).
    Rn.View(
        testId = "aesg-sample-visuals",
        styles = [| Styles.visuals |],
        children =
            [|
                LC.With.Context(
                    context = SampleVisualsScreenSize.sampleVisualsScreenSizeContext,
                    ``with`` =
                        fun (sampleVisualsScreenSize: LibClient.Responsive.ScreenSize) ->
                            LC.ForceContext(
                                value    = sampleVisualsScreenSize,
                                context  = LibClient.Responsive.screenSizeContext,
                                children = [| visuals |]
                            )
                )
            |]
    )

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member ComponentSample(
            visuals:            ReactElement,
            code:               Code,
            ?children:          ReactChildrenProp,
            ?notes:             ReactElement,
            ?verticalAlignment: VerticalAlignment,
            ?layout:            Layout,
            ?heading:           string,
            ?key:               string,
            ?xLegacyStyles:     List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)

        let notes = defaultArg notes noElement
        let verticalAlignment = defaultArg verticalAlignment VerticalAlignment.Middle
        let layout = defaultArg layout Layout.SideBySide

        #if EGGSHELL_PLATFORM_IS_WEB
        castAsElementAckingKeysWarning
            [|
                heading
                |> Option.map (fun headingText ->
                    dom.tr
                        [ ClassName "cs-heading" ]
                        [|
                            dom.td
                                [ ColSpan 2 ]
                                [|
                                    tertiaryHeading headingText
                                |]
                        |]
                )
                |> Option.defaultValue noElement

                dom.tr
                    []
                    [|
                        dom.td
                            [ ClassName (visualsTdClass verticalAlignment layout) ]
                            [|
                                renderVisuals visuals
                            |]
                        dom.td
                            [ ClassName (codeTdClass layout) ]
                            [|
                                renderCodeAndNotes code notes
                            |]
                    |]
            |]
        #else
        castAsElementAckingKeysWarning
            [|
                heading
                |> Option.map (fun headingText ->
                    tertiaryHeading headingText
                )
                |> Option.defaultValue noElement

                Rn.View(
                    testId   = "aesg-sample-visuals",
                    children = [| visuals |]
                )

                renderCodeAndNotes code notes
            |]
        #endif
