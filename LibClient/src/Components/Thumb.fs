[<AutoOpen>]
module LibClient.Components.Thumb

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Services.ImageService

open Rn.Components
open Rn.Styles

module LC =
    module Thumb =
        type For<'T> =
        | Value of 'T * ('T -> ImageSource)
        with
            member this.Source : ImageSource =
                match this with
                | Value (value, toSource) -> toSource value

        type For =
            static member Of(source: ImageSource) : For<ImageSource> = Value (source, identity)

            static member Of(value: 'T, toSource: 'T -> ImageSource) : For<'T> = Value (value, toSource)

        let ofUrl = LibClient.Services.ImageService.ImageSource.ofUrl

        type Corners =
        | Sharp
        | Rounded

        module private Theme =
            let thumb = ViewStyles.Memoize (fun size -> makeViewStyles {
                Position.Relative
                flex 0
                borderWidth 2
                borderColor Color.Transparent
                height size
                width size
            })

            let selected = ViewStyles.Memoize (fun (borderCss: string) -> makeViewStyles {
                borderColor (Color.InternalString borderCss)
            })

        type Theme = {
            Size:                int
            SelectedBorderColor: Color
        }
        with
            member this.Thumb =
                Theme.thumb this.Size

            member this.Selected =
                Theme.selected this.SelectedBorderColor.ToCssString

open LC.Thumb

// TODO: delete after RenderDSL migration
type PropForFactory =
    static member Make (source: ImageSource) : For<ImageSource> = For.Of(source, identity)
    static member Make (value: 'T, toSource: 'T -> ImageSource) : For<'T> = For.Of(value, toSource)

[<RequireQualifiedAccess>]
module private Styles =
    let image =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

    let cornersSharp = makeViewStyles { Noop }

    let cornersRounded = makeViewStyles { borderRadius 12 }

    let cornersFor (corners: Corners) =
        match corners with
        | Corners.Sharp   -> cornersSharp
        | Corners.Rounded -> cornersRounded

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Thumb<'T when 'T: comparison>(
            source:      ImageSource,
            ?isSelected: bool,
            ?onPress:    ReactEvent.Action -> unit,
            ?testId:     string,
            ?styles:     array<ViewStyles>,
            ?theme:      Theme -> Theme,
            ?corners:    Corners,
            ?key:        string
        ) : ReactElement =
        ignore key

        LibClient.Components.Constructors.LC.Thumb(
            ``for``     = For.Of source,
            ?isSelected = isSelected,
            ?onPress    = onPress,
            ?testId     = testId,
            ?styles     = styles,
            ?theme      = theme,
            ?corners    = corners,
            ?key        = key
        )

    [<Component>]
    static member Thumb<'T when 'T: comparison>(
            ``for``:     For<'T>,
            ?isSelected: bool,
            ?onPress:    ReactEvent.Action -> unit,
            ?testId:     string,
            ?styles:     array<ViewStyles>,
            ?theme:      Theme -> Theme,
            ?corners:    Corners,
            ?key:        string
        ) : ReactElement =
        ignore key

        let theTheme   = Themes.GetMaybeUpdatedWith theme
        let isSelected = defaultArg isSelected false
        let corners    = defaultArg corners    Corners.Sharp
        let thumbTestId =
            testId
            |> Option.orElse (
                onPress
                |> Option.map (fun _ -> A11ySlug.testId "thumb" "select")
            )

        LC.With.Layout(
            initialOnly = true,
            ``with`` =
                fun (onLayoutOption, maybeLayout) ->
                    Rn.View (
                        styles =
                            [|
                                theTheme.Thumb

                                if isSelected then
                                    theTheme.Selected

                                Styles.cornersFor corners
                                yield! (styles |> Option.defaultValue [||])
                            |],
                        ?onLayout = onLayoutOption,
                        children =
                            elements {
                                Rn.Image (
                                    styles     = [| Styles.image |],
                                    source     = ``for``.Source,
                                    resizeMode = Rn.Components.Image.ResizeMode.Cover,
                                    size       = Size.FromParentLayout maybeLayout
                                )

                                match onPress with
                                | Some onPress ->
                                    LC.Pressable(
                                        onPress = onPress,
                                        label   = "Select thumbnail",
                                        role    = AccessibilityRole.Button,
                                        state =
                                            { AccessibilityStateRecord.empty with
                                                Selected = Some isSelected
                                            },
                                        ?testId       = thumbTestId,
                                        overlay       = true,
                                        componentName = "LC.Thumb"
                                    )
                                | None ->
                                    noElement
                            }
                    )
        )
