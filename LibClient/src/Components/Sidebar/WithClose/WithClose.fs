[<AutoOpen>]
module LibClient.Components.Sidebar_WithClose

open LibClient
open Fable.React

type LibClient.Components.Constructors.LC.Sidebar with
    [<Component>]
    static member WithClose(``with``: (ReactEvent.Action -> unit) -> ReactElement, ?key: string) : ReactElement =
        key |> ignore
        ``with`` (LibClient.Components.AppShell.Content.setSidebarVisibility false)
