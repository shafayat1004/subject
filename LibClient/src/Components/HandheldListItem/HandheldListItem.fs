[<AutoOpen>]
module LibClient.Components.HandheldListItem

open Fable.React
open Browser.Types

open LibClient

open ReactXP.Components
open ReactXP.Styles

// Public types are nested under `module LC = module HandheldListItem` (the Tab.fs pattern) so their
// union cases (Disabled/InProgress/Actionable/Text/Icon/Number/...) are NOT leaked into the global
// namespace by [<AutoOpen>] — otherwise they collide with other components' same-named cases
// (ButtonLowLevelState, FloatingActionButtonStyles.State) used unqualified elsewhere. See LEARNINGS.md.
module LC =
    module HandheldListItem =
        type State =
        | Actionable of OnPress: (Browser.Types.Event -> unit)
        | InProgress
        | Disabled
        with
            member this.GetName : string =
                unionCaseName this

        type Label =
        | Children
        | Text of string

        type Right =
        | Icon          of (int -> LibClient.Icons.Icon)
        | Number        of int
        | NumberAndIcon of int * (int -> LibClient.Icons.Icon)

open LC.HandheldListItem

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.Center
            minHeight    42
            padding      8
            borderBottom 1 (Color.Grey "ee")
            borderTop    1 (Color.Grey "ee")
            marginTop    -1 // to collapse adjacent borders
        }

    let leftIcon  = makeViewStyles { flex 0 }
    let label     = makeViewStyles { FlexDirection.Row; flex 1 }
    let right     = makeViewStyles { FlexDirection.Row; AlignItems.Center; flex 0 }
    let rightIcon = makeViewStyles { marginLeft 12 }

    let number =
        makeViewStyles {
            paddingHV       6 2
            borderRadius    20
            backgroundColor (Color.Grey "dd")
        }

    let numberText = makeTextStyles { color Color.White }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member HandheldListItem(
            label:          Label,
            state:          State,
            ?children:      array<ReactElement>,
            ?leftIcon:      int -> LibClient.Icons.Icon,
            ?right:         Right,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key |> ignore
        let children = defaultArg children [||]

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let onPress : Option<PointerEvent -> unit> =
            match state with
            | Actionable onPress -> Some (fun (e: PointerEvent) -> e.stopPropagation(); onPress e)
            | _                  -> None

        RX.View(
            styles   = [| Styles.view; yield! legacyViewStyles |],
            ?onPress = onPress,
            children =
                [|
                    (match leftIcon with
                     | Some icon -> RX.View(styles = [| Styles.leftIcon |], children = [| (icon 20 :> ReactElement) |])
                     | None      -> noElement)

                    RX.View(
                        styles   = [| Styles.label |],
                        children =
                            [|
                                (match label with
                                 | Label.Children  -> castAsElement children
                                 | Label.Text text -> LC.Text text)
                            |]
                    )

                    (match right with
                     | Some right ->
                        RX.View(
                            styles   = [| Styles.right |],
                            children =
                                (match right with
                                 | Right.Number number ->
                                     [| RX.View(styles = [| Styles.number |], children = [| (LC.Text(string number, styles = [| Styles.numberText |])) |]) |]
                                 | Right.Icon icon ->
                                     [| RX.View(styles = [| Styles.rightIcon |], children = [| (icon 32 :> ReactElement) |]) |]
                                 | Right.NumberAndIcon (number, icon) ->
                                     [|
                                         RX.View(styles = [| Styles.number |],    children = [| (LC.Text(string number, styles = [| Styles.numberText |])) |])
                                         RX.View(styles = [| Styles.rightIcon |], children = [| (icon 32 :> ReactElement) |])
                                     |])
                        )
                     | None -> noElement)
                |]
        )
