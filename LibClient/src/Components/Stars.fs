[<AutoOpen>]
module LibClient.Components.Stars

open Fable.React

open LibClient
open LibClient.Icons

open Rn.Components
open Rn.Styles

module LC =
    module Stars =
        type Theme = {
            OnColor: Color
            OffColor: Color
            IconSize: int
        }

open LC.Stars

[<RequireQualifiedAccess>]
module private Styles =
    let stars =
        makeViewStyles {
            FlexDirection.Row
        }

    let star =
        makeViewStyles {
            flex 0
        }

    let iconTheme =
        TextStyles.Memoize(
            fun (theme: Theme) (isOn: bool) ->
                makeTextStyles {
                    fontSize theme.IconSize

                    color
                        (if isOn then
                            theme.OnColor
                        else
                            theme.OffColor)
                }
        )

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Stars(
            count: int,
            ?total: int,
            ?styles: array<ViewStyles>,
            ?theme: Theme -> Theme,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme
        let total = defaultArg total 5

        Rn.View(
            styles =
                [|
                    Styles.stars
                    yield! (styles |> Option.defaultValue [||])
                |],
            children =
                (
                    [| 1..total |]
                    |> Array.map (fun curr ->
                        let isOn = count > curr - 1

                        Rn.View(
                            styles = [| Styles.star |],
                            children =
                                elements {
                                    LC.Icon(
                                        icon = Icon.Star,
                                        styles =
                                            [|
                                                Styles.iconTheme theTheme isOn
                                            |]
                                    )
                                }
                        )
                    )
                )
        )