[<AutoOpen>]
module AppEggShellGallery.Components.AppContext

open Fable.React
open LibClient
open LibClient.Components
open LibRouter.Components
open AppEggShellGallery.Navigation

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member AppContext(
            ?children:      ReactChildrenProp,
            ?key:           string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (key, xLegacyStyles)

        LR.NavigationRouter(
            spec = routesSpec(),
            navigationState = navigationState,
            queue = navigationQueue,
            child =
                LC.AppShell.Context(
                    children = defaultArg children [||]
                )
        )
