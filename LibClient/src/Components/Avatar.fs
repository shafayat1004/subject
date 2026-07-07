[<AutoOpen>]
module LibClient.Components.Avatar

open Fable.React

open LibClient
open LibClient.Services.ImageService

open Rn.Components
open Rn.Styles

module LC =
    module Avatar =
        type Theme = {
            Size: int
        }

        let ofUrl = LibClient.Services.ImageService.ImageSource.ofUrl

open LC.Avatar

[<RequireQualifiedAccess>]
module private Styles =
    let viewTheme =
        ViewStyles.Memoize(
            fun (theme: Theme) ->
                makeViewStyles {
                    flex 0
                    Overflow.Hidden
                    height theme.Size
                    width theme.Size
                    borderRadius (theme.Size / 2)
                }
        )

    let image =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Avatar(
            source: ImageSource,
            ?styles: array<ViewStyles>,
            ?theme: Theme -> Theme,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme

        LC.With.Layout(
            (fun (maybeOnLayout, maybeLayout) ->
                Rn.View(
                    styles =
                        [|
                            Styles.viewTheme theTheme
                            yield! (styles |> Option.defaultValue [||])
                        |],
                    ?onLayout = maybeOnLayout,
                    children =
                        elements {
                            Rn.Image(
                                source = source,
                                styles = [| Styles.image |],
                                resizeMode = Image.ResizeMode.Cover,
                                size = Size.FromParentLayout maybeLayout
                            )
                        }
                )),
            initialOnly = true
        )