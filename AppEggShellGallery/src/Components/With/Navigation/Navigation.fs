[<AutoOpen>]
module AppEggShellGallery.Components.WithNavigation

open Fable.React
open LibClient
open AppEggShellGallery.Navigation

// this component is actually completely unnecessary, it's just here
// to prevent a large number of changes caused by removal with With.Navigation
// and using of the nav value directly

type AppEggShellGallery.Components.Constructors.Ui.With with
    [<Component>]
    static member Navigation(
            ``with``:       AppEggShellGallery.Navigation.Navigation -> ReactElement,
            ?children:      ReactChildrenProp,
            ?key:           string,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        ignore (children, key, xLegacyStyles)

        ``with`` nav
