[<AutoOpen>]
module AppEggShellGallery.Components.Route_Settings

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components

type Ui.Route with
    [<Component>]
    static member Settings(pstoreKey: string) : ReactElement =
        LR.Route(
            scroll = LibRouter.Components.Route.Vertical,
            children = [| LC.Text "Settings screen, in theory. Nothing to actually see here." |]
        )
