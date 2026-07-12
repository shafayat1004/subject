[<AutoOpen>]
module LibClient.Components.Legacy_Sidebar_Filler

open Fable.React
open LibClient
open Rn.Components
open Rn.Styles

module LC =
    module Legacy =
        module Sidebar =
            module Filler =
                type Theme = {
                    BottomBorderColor: Color
                }

open LC.Legacy.Sidebar.Filler

[<RequireQualifiedAccess>]
module private Styles =
    let view (theme: Theme) =
        makeViewStyles {
            flex              1
            borderBottomWidth 1
            borderColor       theme.BottomBorderColor
            backgroundColor   (Color.Grey "f9")
        }

type LibClient.Components.Constructors.LC.Legacy.Sidebar with
    [<Component>]
    static member Filler(
            ?children: ReactChildrenProp,
            ?hackWeDoNotSupportProplessComponents: bool,
            ?theme:    Theme -> Theme,
            ?key:      string
        ) : ReactElement =
        key      |> ignore
        children |> ignore
        hackWeDoNotSupportProplessComponents |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme
        Rn.View(styles = [| Styles.view theTheme |])
