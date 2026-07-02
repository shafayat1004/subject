// Cluster conversion (strategy A): this consumer is converted together with its producer
// LC.VerticallyScrollable. The old Base.styles.fs styled the producer's internal "top"/"bottom" blocks
// via the legacy `==> VerticallyScrollableStyles.Theme.One*` class cascade; now we pass those styles
// explicitly through the producer's per-section style params. See the gallery docs runbooks/troubleshooting.md.
[<AutoOpen>]
module LibClient.Components.Sidebar_Base

open Fable.React

open LibClient

open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            width            300
            borderColor      (Color.Grey "cc")
            borderLeftWidth  1
            borderRightWidth 1
            backgroundColor  Color.White
        }

    let topSection    = makeViewStyles { borderBottom 1 (Color.Grey "cc") }
    let bottomSection = makeViewStyles { borderTop    1 (Color.Grey "cc") }

type LibClient.Components.Constructors.LC.Sidebar with
    [<Component>]
    static member Base(
            ?fixedTop:         ReactElement,
            ?scrollableMiddle: ReactElement,
            ?fixedBottom:      ReactElement,
            ?styles:           array<ViewStyles>,
            ?xLegacyStyles:    List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:              string
        ) : ReactElement =
        key           |> ignore
        xLegacyStyles |> ignore

        LC.VerticallyScrollable(
            ?fixedTop         = fixedTop,
            ?scrollableMiddle = scrollableMiddle,
            ?fixedBottom      = fixedBottom,
            middleTestId      = "sidebar-scroll-middle",
            styles            = [| Styles.view; yield! (defaultArg styles [||]) |],
            topStyles         = [| Styles.topSection |],
            bottomStyles      = [| Styles.bottomSection |]
        )
