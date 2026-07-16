[<AutoOpen>]
module LibClient.Components.ThumbCard

open Fable.React
open LibClient.Components.Layout.LC
open Rn.Styles
open Rn.Components
open LibClient.Components
open LibClient
open LibClient.Accessibility

module LC =
    module ThumbCard =
        [<RequireQualifiedAccess>]
        type ThumbPosition =
        | Right
        | Left

open LC.ThumbCard

module private Styles =
    let row = makeViewStyles {
        AlignSelf.Stretch
    }

    let thumb = ViewStyles.Memoize (fun thumbPosition -> makeViewStyles {
        Position.Relative
        AlignSelf.Stretch
        width 112
        minHeight 112
        match thumbPosition with
        | ThumbPosition.Left  -> marginRight 12
        | ThumbPosition.Right -> marginLeft  12
    })

    let image = makeViewStyles {
        Position.Absolute
        trbl 0 0 0 0
    }

type private Helpers =
    [<Component>]
    static member Thumb (
        imageSource:   LibClient.Services.ImageService.ImageSource,
        thumbPosition: ThumbPosition,
        ?onPress:      ReactEvent.Action -> unit,
        ?testId:       string
    ) : ReactElement =
        LC.With.Layout(
            initialOnly = true,
            ``with`` =
                fun (onLayoutOption, maybeLayout) ->
                    Rn.View(
                        styles    = [|Styles.thumb thumbPosition|],
                        ?onLayout = onLayoutOption,
                        children =
                            elements {
                                Rn.Image(
                                    styles     = [| Styles.image |],
                                    source     = imageSource,
                                    resizeMode = Rn.Components.Image.ResizeMode.Cover,
                                    size       = (Size.FromParentLayout maybeLayout)
                                )

                                match onPress with
                                | Some onPress ->
                                    let resolvedTestId =
                                        testId |> Option.orElse (Some (A11ySlug.testId "thumb-card" "Open image"))
                                    LC.Pressable(
                                        onPress       = onPress,
                                        label         = "Open image",
                                        testId        = resolvedTestId.Value,
                                        role          = AccessibilityRole.Button,
                                        overlay       = true,
                                        componentName = "LC.ThumbCard"
                                    )
                                | None ->
                                    noElement
                            }
                    )
        )

    static member CustomCardTheme (theme: LC.Card.Theme) : LC.Card.Theme =
        { theme with Padding = 0; BorderRadius = 12 }

type LC with
    [<Component>]
    static member ThumbCard (
        imageSource:    LibClient.Services.ImageService.ImageSource,
        child:          ReactElement,
        ?thumbPosition: ThumbPosition,
        ?onPress:       ReactEvent.Action -> unit,
        ?label:         string,
        ?testId:        string,
        ?styles:        array<ViewStyles>
    ) : ReactElement =
        let thumbPosition = defaultArg thumbPosition ThumbPosition.Left
        let a11yLabel = defaultArg label "Open card"
        LC.Card (
            theme    = Helpers.CustomCardTheme,
            children = [|
                LC.Row (
                    crossAxisAlignment = CrossAxisAlignment.Stretch,
                    styles             = [|Styles.row|],
                    children =
                        match thumbPosition with
                        | ThumbPosition.Left ->
                            [|
                                Helpers.Thumb (imageSource, thumbPosition)
                                child
                            |]
                        | ThumbPosition.Right ->
                            [|
                                child
                                Helpers.Thumb (imageSource, thumbPosition)
                            |]
                )
            |],
            ?outerStyles = styles,
            ?onPress     = onPress,
            label        = a11yLabel,
            ?testId      = testId
        )
