[<AutoOpen>]
module LibClient.Components.VerticallyScrollable

open Fable.React

open LibClient

open Rn.Components
open Rn.Styles

// See the gallery docs modernization/render-dsl-retirement.md (conversion recipe). Don't `open Rn.LegacyStyles` (shadows new-dialect rules).
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
            ?middleTestId:     string,
            ?styles:           array<ViewStyles>,
            ?topStyles:        array<ViewStyles>,
            ?middleStyles:     array<ScrollViewStyles>,
            ?bottomStyles:     array<ViewStyles>,
            ?xLegacyStyles:    List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:              string
        ) : ReactElement =
        key |> ignore

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        Rn.View(
            styles =
                [|
                    Styles.view
                    yield! legacyViewStyles
                    yield! (defaultArg styles [||])
                |],
            children =
                [|
                    (match fixedTop with
                     | Some el -> Rn.View(styles = [| Styles.top; yield! (defaultArg topStyles [||]) |], children = [| el |])
                     | None    -> noElement)

                    (match scrollableMiddle with
                     | Some el -> Rn.ScrollView(?testId = middleTestId, vertical = true, styles = [| Styles.middle; yield! (defaultArg middleStyles [||]) |], children = [| el |])
                     | None    -> noElement)

                    (match fixedBottom with
                     | Some el -> Rn.View(styles = [| Styles.bottom; yield! (defaultArg bottomStyles [||]) |], children = [| el |])
                     | None    -> noElement)
                |]
        )
