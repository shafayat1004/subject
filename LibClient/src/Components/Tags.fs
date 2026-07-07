[<AutoOpen>]
module LibClient.Components.Tags

open Fable.React

open LibClient

open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let tags =
        makeViewStyles {
            FlexDirection.Row
            AlignItems.FlexStart
            AlignContent.FlexStart
            FlexWrap.Wrap
            flex 1
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Tags(
            children: ReactElements,
            ?styles: array<ViewStyles>,
            ?key: string
        ) : ReactElement =
        key |> ignore

        Rn.View(
            styles =
                [|
                    Styles.tags
                    yield! styles |> Option.defaultValue [||]
                |],
            children = children
        )
