[<AutoOpen>]
module AppEggShellGallery.Components.A11yPanel

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles

module dom = Fable.React.Standard

do Rn.LegacyStyles.Css.addCss """
.aesg-a11y-facts {
    border-collapse: collapse;
    align-self:      flex-start;
}
.aesg-a11y-facts td {
    padding:        3px 0;
    vertical-align: top;
    line-height:    1.4;
}
.aesg-a11y-facts td:first-child {
    padding-right: 24px;
    white-space:   nowrap;
}
"""

[<RequireQualifiedAccess>]
module private Styles =
    let row =
        makeViewStyles {
            paddingVertical 4
        }

    let label =
        makeTextStyles {
            FontWeight.Bold
        }

let private factRow (labelText: string) (value: ReactElement) =
    #if EGGSHELL_PLATFORM_IS_WEB
    dom.tr
        []
        [|
            dom.td [] [| LC.Text(labelText, styles = [| Styles.label |]) |]
            dom.td [] [| value |]
        |]
    #else
    Rn.View(
        styles = [| Styles.row |],
        children =
            [|
                LC.Text(labelText, styles = [| Styles.label |])
                value
            |]
    )
    #endif

let private renderFacts (rows: ReactElement array) =
    #if EGGSHELL_PLATFORM_IS_WEB
    dom.table
        [ ClassName "aesg-a11y-facts dom-user-select-text" ]
        [|
            dom.tbody [] rows
        |]
    #else
    Rn.View(children = rows)
    #endif

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member A11yPanel(
            componentName: string,
            role: string,
            namePattern: string,
            stateNotes: string,
            scalesWithFont: bool,
            ?contrastNotes: string,
            ?deferredTags: string list
        ) : ReactElement =
        let fontScaling =
            if scalesWithFont then
                "Scales with OS font size (allowFontScaling enabled)"
            else
                "Does not scale with OS font size"

        let contrastRow =
            contrastNotes
            |> Option.map (fun notes -> factRow "Contrast" (LC.Text notes))
            |> Option.toArray

        let deferredRow =
            deferredTags
            |> Option.defaultValue []
            |> function
                | [] -> [||]
                | tags ->
                    [|
                        factRow
                            "Deferred / platform"
                            (LC.Text(String.concat ", " tags))
                    |]

        renderFacts
            [|
                factRow "Component" (LC.Text componentName)
                factRow "Role" (LC.Text role)
                factRow "Name" (LC.Text namePattern)
                factRow "State" (LC.Text stateNotes)
                factRow "Font scaling" (LC.Text fontScaling)
                yield! contrastRow
                yield! deferredRow
            |]
