[<AutoOpen>]
module LibClient.Components.Section_Padded

open Fable.React

open LibClient
open LibClient.Responsive

open ReactXP.Components
open ReactXP.Styles

// Responsive component: padding varies by screen size. The old version expressed this via the
// `{screenSize.Class}` class + responsive style blocks; the modern form reads the screen size from
// LC.With.ScreenSize and branches inside the style CE. See LEARNINGS.md.

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        ViewStyles.Memoize(
            fun (screenSize: ScreenSize) ->
                makeViewStyles {
                    match screenSize with
                    | ScreenSize.Desktop  -> padding 24
                    | ScreenSize.Handheld -> padding 16
                }
        )

type LibClient.Components.Constructors.LC.Section with
    [<Component>]
    static member Padded(
            children:       array<ReactElement>,
            ?styles:        array<ViewStyles>,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key |> ignore

        let legacyViewStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some legacyStyles ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        LC.With.ScreenSize (fun screenSize ->
            RX.View(
                styles =
                    [|
                        Styles.view screenSize
                        yield! legacyViewStyles
                        yield! (defaultArg styles [||])
                    |],
                children = children
            )
        )
