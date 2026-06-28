[<AutoOpen>]
module AppEggShellGallery.Components.A11yPanel

open Fable.React
open Fable.React.Props
open LibClient
open LibClient.Components
open ReactXP.Components
open ReactXP.Styles

module dom = Fable.React.Standard

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
    RX.View(
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
        [ ClassName "aesg-ContentComponent-table" ]
        [|
            dom.tbody [] rows
        |]
    #else
    RX.View(children = rows)
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
