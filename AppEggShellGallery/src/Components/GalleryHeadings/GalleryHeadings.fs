[<AutoOpen>]
module AppEggShellGallery.Components.GalleryHeadings

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Responsive
open Rn.Styles

// Disambiguate from Sidebar.Heading.Level, Button.Level, etc.
module H = LibClient.Components.Heading

[<RequireQualifiedAccess>]
module private Styles =
    let text =
        TextStyles.Memoize (fun (screenSize: ScreenSize) (level: H.Level) ->
            makeTextStyles {
                color (Color.Grey "45")
                fontSize (
                    match screenSize, level with
                    | ScreenSize.Desktop,  H.Level.Primary   -> 36
                    | ScreenSize.Desktop,  H.Level.Secondary -> 24
                    | ScreenSize.Desktop,  H.Level.Tertiary  -> 14
                    | ScreenSize.Handheld, H.Level.Primary   -> 18
                    | ScreenSize.Handheld, H.Level.Secondary -> 16
                    | ScreenSize.Handheld, H.Level.Tertiary  -> 14
                )
                match screenSize with
                | ScreenSize.Handheld -> FontWeight.W700
                | ScreenSize.Desktop  -> ()
            }
        )

let galleryHeading (level: H.Level) (text: string) : ReactElement =
    LC.With.ScreenSize(
        ``with`` =
            fun screenSize ->
                LC.Heading(
                    level = level,
                    children =
                        [|
                            LC.Text(text, styles = [| Styles.text screenSize level |])
                        |]
                )
    )

let secondaryHeading (text: string) : ReactElement =
    galleryHeading H.Level.Secondary text

let tertiaryHeading (text: string) : ReactElement =
    galleryHeading H.Level.Tertiary text
