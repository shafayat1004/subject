[<AutoOpen>]
module LibClient.Components.Nav_Top_Image

open Fable.React
open LibClient
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let image =
        makeViewStyles {
            width  42
            height 42
        }

type LibClient.Components.Constructors.LC.Nav.Top with
    [<Component>]
    static member Image(
            source:  LibClient.Services.ImageService.ImageSource,
            ?styles: array<ViewStyles>,
            ?key:    string) : ReactElement =
        key |> ignore

        Rn.Image(
            styles =
                [|
                    Styles.image
                    yield! (styles |> Option.defaultValue [||])
                |],
            source     = source,
            size       = FromStyles,
            resizeMode = Cover
        )
