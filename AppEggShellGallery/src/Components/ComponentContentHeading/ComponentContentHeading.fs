[<AutoOpen>]
module AppEggShellGallery.Components.ComponentContentHeading

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Responsive
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view = makeViewStyles {
        FlexDirection.Row
        AlignItems.Center
    }

    let text =
        TextStyles.Memoize(
            fun (screenSize: ScreenSize) ->
                makeTextStyles {
                    color (Color.Grey "45")
                    match screenSize with
                    | ScreenSize.Desktop  -> fontSize 36
                    | ScreenSize.Handheld -> fontSize 18
                }
        )

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member ComponentContentHeading(
            displayName:    string,
            isResponsive:   bool,
            ?children:      ReactChildrenProp,
            ?key:           string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        children |> ignore
        xLegacyStyles |> ignore

        LC.With.ScreenSize(
            ``with`` =
                (fun screenSize ->
                    RX.View(
                        styles = [| Styles.view |],
                        children =
                            [|
                                LC.Text(displayName, styles = [| Styles.text screenSize |])

                                if isResponsive then
                                    LC.Tag(text = "Responsive")
                                else
                                    noElement
                            |]
                    )
                )
        )
