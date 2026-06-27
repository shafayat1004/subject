// ---------------------------------------------------------------------------
// Compat shim: keeps Nav.Top.HeadingStyles.Theme.One/All/Rules alive for
// callers not yet migrated (AppCookups/TopNav.fs, DefaultComponentsTheme.fs,
// ComponentRegistration.fs). Delete once all those callers are migrated.
// ---------------------------------------------------------------------------
namespace LibClient.Components.Nav.Top

open LibClient
open LibClient.Responsive
open ReactXP.LegacyStyles

module HeadingStyles =
    type Sizes = {|
        FontSize: int
    |}

    let private baseStyles = lazy (asBlocks [
        "view" => [
            flex 1
            FontWeight.Normal
        ] && [
            ScreenSize.Handheld.Class => [
                AlignSelf.Center
                AlignItems.Center
                TextAlign.Center
            ]
        ]
    ])

    type (* class to enable named parameters *) Theme() =
        static let Customize = makeCustomize("LibClient.Components.Nav.Top.Heading", baseStyles)

        static member All (screenSizeToSizes : ScreenSize -> Sizes, theColor: Color) : unit =
            Customize [
                Theme.Rules (screenSizeToSizes, theColor)
            ]

        static member One (screenSizeToSizes : ScreenSize -> Sizes, theColor: Color) : Styles =
            Theme.Rules (screenSizeToSizes, theColor) |> makeSheet

        static member Rules (screenSizeToSizes: ScreenSize -> Sizes, theColor: Color) : List<ISheetBuildingBlock> =
            [
                "view" ==> LibClient.Components.Heading.HeadingStyles.Theme.One (
                    screenSizeToSizes = (function
                        | ScreenSize.Desktop  -> fun _ -> screenSizeToSizes ScreenSize.Desktop
                        | ScreenSize.Handheld -> fun _ -> screenSizeToSizes ScreenSize.Handheld
                    ),
                    theColor = theColor
                )
            ]

    let styles = lazy (compile (List.concat [
        baseStyles.Value
        Theme.Rules (
            screenSizeToSizes = (function
                | ScreenSize.Desktop  -> {| FontSize = 24; |}
                | ScreenSize.Handheld -> {| FontSize = 24; |}
            ),
            theColor = Color.White
        )
    ]))

// ---------------------------------------------------------------------------
// Component implementation
// ---------------------------------------------------------------------------
namespace LibClient.Components

open Fable.React
open LibClient
open LibClient.Responsive
open ReactXP.Components
open ReactXP.Styles

// NOTE: do NOT `open ReactXP.LegacyStyles` here. Its rule functions shadow the new-dialect ones and
// break the make*Styles computation expressions.

[<AutoOpen>]
module Nav_Top_Heading =

    [<RequireQualifiedAccess>]
    module private Styles =
        let view =
            makeTextStyles {
                flex 1
                FontWeight.Normal
            }

        let viewHandheld =
            makeTextStyles {
                AlignSelf.Center
                AlignItems.Center
                TextAlign.Center
            }

    type LibClient.Components.Constructors.LC.Nav.Top with
        [<Component>]
        static member Heading(
                text:           string,
                ?styles:        array<TextStyles>,
                ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
                ?key:           string
            ) : ReactElement =
            key |> ignore

            // Bridge legacy class-based styles emitted by not-yet-converted render-DSL callers.
            // Safe to remove once all callers pass styles directly.
            let legacyTextStyles : array<TextStyles> =
                match xLegacyStyles with
                | Some ls ->
                    match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                    | []  -> [||]
                    | s   -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<TextStyles> "ReactXP.Components.Text" s |]
                | None -> [||]

            LC.With.ScreenSize (fun screenSize ->
                LC.Heading(
                    children = [| makeTextNode2 (Some "LibClient.Components.Heading") text |],
                    styles   =
                        [|
                            Styles.view
                            match screenSize with
                            | ScreenSize.Handheld -> Styles.viewHandheld
                            | ScreenSize.Desktop  -> ()
                            yield! legacyTextStyles
                            yield! (defaultArg styles [||])
                        |]
                )
            )
