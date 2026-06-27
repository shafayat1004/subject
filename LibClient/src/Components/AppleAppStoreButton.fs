[<AutoOpen>]
module LibClient.Components.AppleAppStoreButton

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.LocalImages

open ReactXP.Styles
open ReactXP.Components

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
    static member AppleAppStoreButton(onPress: ReactEvent.Action -> unit, ?styles: array<ViewStyles>, ?key: string) : ReactElement =
        key |> ignore

        LC.With.ScreenSize(
            fun screenSize ->
                RX.View(
                    styles = (styles |> Option.defaultValue [||]),
                    children =
                        elements {
                            RX.Image(
                                styles =
                                    [|
                                        if screenSize = Responsive.ScreenSize.Desktop then
                                            Styles.desktopImage
                                        else
                                            Styles.handheldImage
                                    |],
                                source = localImage "/libs/LibClient/images/app-store.png",
                                resizeMode = Image.ResizeMode.Contain,
                                size = Size.FromStyles
                            )

                            LC.Pressable(
                                onPress = onPress,
                                label = "Download on the App Store",
                                testId = "apple-app-store-button",
                                role = AccessibilityRole.Link,
                                overlay = true,
                                componentName = "LC.AppleAppStoreButton"
                            )
                        }
                )
        )