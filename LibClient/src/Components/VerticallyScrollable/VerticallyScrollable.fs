[<AutoOpen>]
module LibClient.Components.VerticallyScrollable

open Fable.React

open LibClient

open ReactXP.Components
open ReactXP.Styles

// See LEARNINGS.md (render-DSL -> F# recipe). Don't `open ReactXP.LegacyStyles` (shadows new-dialect rules).
// Per-section style params (`topStyles`/`middleStyles`/`bottomStyles`) replace the old legacy mechanism
// where a parent styled this component's internal "top"/"middle"/"bottom" blocks via class cascade.

[<RequireQualifiedAccess>]
module private Styles =
    let view   = makeViewStyles       { flex 1 }
    let top    = makeViewStyles       { flex 0 }
    let middle = makeScrollViewStyles { flex 1 }
    let bottom = makeViewStyles       { flex 0 }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member VerticallyScrollable(
            ?fixedTop:         ReactElement,
            ?scrollableMiddle: ReactElement,
            ?fixedBottom:      ReactElement,
            ?styles:           array<ViewStyles>,
            ?topStyles:        array<ViewStyles>,
            ?middleStyles:     array<ScrollViewStyles>,
            ?bottomStyles:     array<ViewStyles>,
            ?xLegacyStyles:    List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:              string
        ) : ReactElement =
        key |> ignore

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        RX.View(
            styles =
                [|
                    Styles.view
                    yield! legacyViewStyles
                    yield! (defaultArg styles [||])
                |],
            children =
                [|
                    (match fixedTop with
                     | Some el -> RX.View(styles = [| Styles.top; yield! (defaultArg topStyles [||]) |], children = [| el |])
                     | None    -> noElement)

                    (match scrollableMiddle with
                     | Some el -> RX.ScrollView(vertical = true, styles = [| Styles.middle; yield! (defaultArg middleStyles [||]) |], children = [| el |])
                     | None    -> noElement)

                    (match fixedBottom with
                     | Some el -> RX.View(styles = [| Styles.bottom; yield! (defaultArg bottomStyles [||]) |], children = [| el |])
                     | None    -> noElement)
                |]
        )
