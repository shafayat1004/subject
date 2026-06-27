[<AutoOpen>]
module AppEggShellGallery.Components.ComponentSampleGroup

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open ReactXP.Components

module dom = Fable.React.Standard

do
    ReactXP.LegacyStyles.Css.addCss """

.aesg-ContentComponent-table .csg-heading {
    border-top:    24px solid transparent;
    border-bottom: 2px  solid transparent;
}

.aesg-ContentComponent-table .csg-notes {
    border-top:    24px solid transparent;
    border-bottom: 24px solid transparent;
}

.aesg-ContentComponent-table .csg-vertical-padding {
    height:        36px;
    border-bottom: 24px solid transparent;
    border-top:    24px solid transparent;
}

"""

let private verticalPaddingRow : ReactElement =
    #if EGGSHELL_PLATFORM_IS_WEB
    dom.tr
        [ ClassName "csg-vertical-padding" ]
        [|
            dom.td [ ColSpan 2 ] [||]
        |]
    #else
    noElement
    #endif

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member ComponentSampleGroup(
            samples:        ReactElement,
            ?children:      ReactChildrenProp,
            ?notes:         ReactElement,
            ?heading:       string,
            ?key:           string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)

        let notesElement = defaultArg notes noElement

        #if EGGSHELL_PLATFORM_IS_WEB
        castAsElementAckingKeysWarning
            [|
                if Option.isNone heading && notesElement = noElement then
                    verticalPaddingRow
                else
                    noElement

                heading
                |> Option.map (fun headingText ->
                    dom.tr
                        [ ClassName "csg-heading" ]
                        [|
                            dom.td
                                [ ColSpan 2 ]
                                [|
                                    LC.Heading(
                                        level = Heading.Tertiary,
                                        children = [| LC.Text headingText |]
                                    )
                                |]
                        |]
                )
                |> Option.defaultValue noElement

                if notesElement <> noElement then
                    dom.tr
                        [ ClassName "csg-notes" ]
                        [|
                            dom.td [ ColSpan 2 ] [| notesElement |]
                        |]
                else
                    noElement

                samples

                verticalPaddingRow
            |]
        #else
        castAsElementAckingKeysWarning
            [|
                heading
                |> Option.map (fun headingText ->
                    LC.Heading(
                        level = Heading.Tertiary,
                        children = [| LC.Text headingText |]
                    )
                )
                |> Option.defaultValue noElement

                notesElement
                samples
            |]
        #endif
