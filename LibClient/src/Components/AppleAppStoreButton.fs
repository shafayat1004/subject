[<AutoOpen>]
module LibClient.Components.AppleAppStoreButton

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.LocalImages

open Rn.Styles
open Rn.Components

[<RequireQualifiedAccess>]
module private Styles =
    let imageContainer =
        makeViewStyles {
            Position.Relative
        }

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
                Rn.View(
                    styles =
                        [|
                            Styles.imageContainer
                            yield! (styles |> Option.defaultValue [||])
                        |],
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
                                source     = localImage "/libs/LibClient/images/app-store.png",
                                resizeMode = Image.ResizeMode.Contain,
                                size       = Size.FromStyles
                            )

                            LC.Pressable(
                                onPress       = onPress,
                                label         = "Download on the App Store",
                                testId        = "apple-app-store-button",
                                role          = AccessibilityRole.Link,
                                overlay       = true,
                                componentName = "LC.AppleAppStoreButton"
                            )
                        }
                )
        )
