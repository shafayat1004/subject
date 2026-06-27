[<AutoOpen>]
module LibClient.Components.Nav_Top_ShowSidebarButton

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Nav.Top.Item
open LibClient.Icons
open ReactXP.Components

type Badge = LibClient.Output.Badge
let Text  = Badge.Text
let Count = Badge.Count

type LibClient.Components.Constructors.LC.Nav.Top with
    [<Component>]
    static member ShowSidebarButton(
        ?badge:    Badge,
        ?menuIcon: IconConstructor,
        ?key:      string
    ) : ReactElement =
        key |> ignore

        let icon = match menuIcon with Some i -> i | None -> Icon.Menu

        let item =
            match badge with
            | Some badge ->
                LC.Nav.Top.Item(
                    state = Nav.Top.Item.State.Actionable AppShell.Content.toggleSidebarVisibility,
                    style = Nav.Top.Item.Style.With(icon = icon, badge = badge)
                )
            | None ->
                LC.Nav.Top.Item(
                    state = Nav.Top.Item.State.Actionable (AppShell.Content.setSidebarVisibility true),
                    style = Nav.Top.Item.iconOnly icon
                )

        ReactXP.Components.Constructors.RX.View(
            testId = "eggshell-sidebar-menu",
            children = elements { item }
        )
