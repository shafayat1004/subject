[<AutoOpen>]
module LibClient.Components.Legacy_TopNav_ShowSidebarButton

open Fable.React
open LibClient
open LibClient.Icons

type LibClient.Components.Constructors.LC.Legacy.TopNav with
    [<Component>]
    static member ShowSidebarButton(onlyOnHandheld: bool, ?children: ReactChildrenProp, ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key           |> ignore
        children      |> ignore
        xLegacyStyles |> ignore
        let button =
            LC.Legacy.TopNav.IconButton(
                state = LC.Legacy.TopNav.IconButtonTypes.Actionable (AppShell_Content.setSidebarVisibility true),
                icon  = Icon.Menu
            )
        LC.Responsive(
            desktop  = (fun _ -> if onlyOnHandheld then noElement else button),
            handheld = (fun _ -> button)
        )
