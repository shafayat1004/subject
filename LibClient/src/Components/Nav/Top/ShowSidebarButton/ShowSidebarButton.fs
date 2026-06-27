[<AutoOpen>]
module LibClient.Components.Nav_Top_ShowSidebarButton

open Fable.React
open LibClient
open LibClient.Icons
open ReactXP.Components
open ReactXP.Styles

// Badge type and constructors re-exported so callers can use Nav.Top.ShowSidebarButton.Count / .Text
type Badge = LibClient.Output.Badge
let Text  = Badge.Text
let Count = Badge.Count

// Preserve the legacy styles module under the same path Suite apps use:
// LibClient.Components.Nav.Top.ShowSidebarButtonStyles.Theme.One / .ItemWidth
module Nav =
    module Top =
        module ShowSidebarButtonStyles =

            open ReactXP.LegacyStyles

            type Colors = LibClient.Components.Nav.Top.ItemStyles.Colors
            type State  = LibClient.Components.Nav.Top.Item.State

            type (* class to enable named parameters *) Theme() =
                static member Customize = makeCustomize("LibClient.Components.Nav.Top.ShowSidebarButton", lazy [])

                static member One (state: State, baseColors: Colors, hoveredColors: Colors, depressedColors: Colors) : Styles =
                    let blocks : List<ISheetBuildingBlock> = [
                        "topnav-item" ==> (LibClient.Components.Nav.Top.ItemStyles.Theme.Part(state, baseColors, hoveredColors, depressedColors) |> makeSheet)
                    ]
                    blocks |> makeSheet

                static member ItemWidth (itemWidth: int) : List<ISheetBuildingBlock> =
                    [
                        "topnav-item" ==> (LibClient.Components.Nav.Top.ItemStyles.Theme.ItemWidth(itemWidth) |> makeSheet)
                    ]

            let styles = lazy (compile [
                "topnav-item" => [
                    borderWidth 0
                ]
            ])

type LibClient.Components.Constructors.LC.Nav.Top with
    [<Component>]
    static member ShowSidebarButton(
        ?badge:         Badge,
        ?menuIcon:      IconConstructor,
        ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
        ?key:           string
    ) : ReactElement =
        key |> ignore

        let icon = match menuIcon with Some i -> i | None -> Icon.Menu

        match badge with
        | Some badge ->
            LC.Nav.Top.Item(
                state          = (LibClient.Components.Nav.Top.Item.Actionable (AppShell.Content.toggleSidebarVisibility)),
                style          = (LibClient.Components.Nav.Top.Item.Style.With(icon = icon, badge = badge)),
                ?xLegacyStyles = (
                    xLegacyStyles
                    |> Option.bind (fun xs ->
                        let found = ReactXP.LegacyStyles.Runtime.findApplicableStyles xs "topnav-item"
                        if found.IsEmpty then None else Some found
                    )
                )
            )
        | None ->
            LC.Nav.Top.Item(
                state          = (LibClient.Components.Nav.Top.Item.Actionable (AppShell.Content.setSidebarVisibility true)),
                style          = (LibClient.Components.Nav.Top.Item.iconOnly icon),
                ?xLegacyStyles = (
                    xLegacyStyles
                    |> Option.bind (fun xs ->
                        let found = ReactXP.LegacyStyles.Runtime.findApplicableStyles xs "topnav-item"
                        if found.IsEmpty then None else Some found
                    )
                )
            )
