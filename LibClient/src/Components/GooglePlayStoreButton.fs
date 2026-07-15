[<AutoOpen>]
module LibClient.Components.GooglePlayStoreButton

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.LocalImages

open Rn.Styles
open Rn.Components

[<RequireQualifiedAccess>]
module private Styles =
    let desktopImage =
        makeViewStyles {
            size 145 47
        }

    let handheldImage =
        makeViewStyles {
            size 145 40
        }

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member GooglePlayStoreButton(onPress: ReactEvent.Action -> unit, ?styles: array<ViewStyles>, ?key: string) : ReactElement =
        key |> ignore

        LC.With.ScreenSize(
            fun screenSize ->
                Rn.View(
                    styles = (styles |> Option.defaultValue [||]),
                    children =
                        elements {
                            Rn.Image(
                                styles =
                                    [|
                                        if screenSize = Responsive.ScreenSize.Desktop then
                                            Styles.desktopImage
                                        else
                                            Styles.handheldImage
                                    |],
                                source     = localImage "/libs/LibClient/images/google-play.png",
                                resizeMode = Image.ResizeMode.Contain,
                                size       = Size.FromStyles
                            )

                            LC.Pressable(
                                onPress       = onPress,
                                label         = "Get it on Google Play",
                                testId        = "google-play-store-button",
                                role          = AccessibilityRole.Link,
                                overlay       = true,
                                componentName = "LC.GooglePlayStoreButton"
                            )
                        }
                )
        )
