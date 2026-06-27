[<AutoOpen>]
module LibClient.Components.Heading

open Fable.React

open LibClient
open LibClient.Responsive

open ReactXP.Components
open ReactXP.Styles

// NOTE: do NOT `open ReactXP.LegacyStyles` here. Its rule functions shadow the new-dialect ones and
// break the make*Styles computation expressions. The HeadingStyles compat shim below opens it
// locally inside its own module scope.

type Level =
| Primary
| Secondary
| Tertiary

// ---------------------------------------------------------------------------
// Compat shim: keeps HeadingStyles.Theme.One/All/Rules alive for callers that
// have not yet been migrated (Nav.Top.Heading.styles.fs, AppCookups/ShoppingCart).
// Delete once all those callers are converted to pass styles directly.
// ---------------------------------------------------------------------------
module HeadingStyles =
    open ReactXP.LegacyStyles
    open LibClient.Responsive

    type Sizes = {|
        FontSize: int
    |}

    let private baseStyles = lazy (asBlocks [
        "text && " + ScreenSize.Handheld.Class => [
            FontWeight.W700
        ]
    ])

    type (* class to enable named parameters *) Theme() =
        static let Customize = makeCustomize("LibClient.Components.Heading", baseStyles)

        static member All (screenSizeToSizes : ScreenSize -> Level -> Sizes, theColor: Color) : unit =
            Customize [
                Theme.Rules (screenSizeToSizes, theColor)
            ]

        static member One (screenSizeToSizes : ScreenSize -> Level -> Sizes, theColor: Color) : Styles =
            Theme.Rules (screenSizeToSizes, theColor) |> makeSheet

        static member Rules (screenSizeToSizes: ScreenSize -> Level -> Sizes, theColor: Color) : List<ISheetBuildingBlock> =
            let makeStyles (screenSize: ScreenSize) (level: Level) (sizes: Sizes) : List<ISheetBuildingBlock> =
                [
                    "text && level-" + (level.ToString()) + " && " + screenSize.Class => [
                        fontSize sizes.FontSize
                    ]
                ]

            [
                "text" => [
                    color theColor
                ]
            ] @
            makeStyles ScreenSize.Desktop  Level.Primary   (screenSizeToSizes ScreenSize.Desktop  Level.Primary) @
            makeStyles ScreenSize.Desktop  Level.Secondary (screenSizeToSizes ScreenSize.Desktop  Level.Secondary) @
            makeStyles ScreenSize.Desktop  Level.Tertiary  (screenSizeToSizes ScreenSize.Desktop  Level.Tertiary) @
            makeStyles ScreenSize.Handheld Level.Primary   (screenSizeToSizes ScreenSize.Handheld Level.Primary) @
            makeStyles ScreenSize.Handheld Level.Secondary (screenSizeToSizes ScreenSize.Handheld Level.Secondary) @
            makeStyles ScreenSize.Handheld Level.Tertiary  (screenSizeToSizes ScreenSize.Handheld Level.Tertiary)

    let styles = lazy (compile (List.concat [
        baseStyles.Value
        Theme.Rules (
            screenSizeToSizes = (function
                | ScreenSize.Desktop -> function
                    | Level.Primary   -> {| FontSize = 36 |}
                    | Level.Secondary -> {| FontSize = 24 |}
                    | Level.Tertiary  -> {| FontSize = 14 |}
                | ScreenSize.Handheld -> function
                    | Level.Primary   -> {| FontSize = 18 |}
                    | Level.Secondary -> {| FontSize = 16 |}
                    | Level.Tertiary  -> {| FontSize = 14 |}
            ),
            theColor = Color.Grey "45"
        )
    ]))

// ---------------------------------------------------------------------------
// Modern styles (computed from screen size and level inline)
// ---------------------------------------------------------------------------
[<RequireQualifiedAccess>]
module private Styles =
    let text =
        TextStyles.Memoize(
            fun (screenSize: ScreenSize) (level: Level) ->
                makeTextStyles {
                    color (Color.Grey "45")
                    fontSize (
                        match screenSize, level with
                        | ScreenSize.Desktop,  Level.Primary   -> 36
                        | ScreenSize.Desktop,  Level.Secondary -> 24
                        | ScreenSize.Desktop,  Level.Tertiary  -> 14
                        | ScreenSize.Handheld, Level.Primary   -> 18
                        | ScreenSize.Handheld, Level.Secondary -> 16
                        | ScreenSize.Handheld, Level.Tertiary  -> 14
                    )
                    match screenSize with
                    | ScreenSize.Handheld -> FontWeight.W700
                    | ScreenSize.Desktop  -> ()
                }
        )

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Heading(
            children:       array<ReactElement>,
            ?level:         Level,
            ?styles:        array<TextStyles>,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        key |> ignore
        let theLevel = defaultArg level Level.Primary

        // Bridge legacy class-based styles emitted by not-yet-converted render-DSL callers
        // (e.g. Nav.Top.Heading.Render.fs). Safe to remove once all callers are converted.
        let legacyTextStyles : array<TextStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | []  -> [||]
                | s   -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<TextStyles> "ReactXP.Components.Text" s |]
            | None -> [||]

        LC.With.ScreenSize (fun screenSize ->
            LC.LegacyText(
                children = children,
                styles   =
                    [|
                        Styles.text screenSize theLevel
                        yield! legacyTextStyles
                        yield! (defaultArg styles [||])
                    |]
            )
        )
