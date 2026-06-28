[<AutoOpen>]
module LibClient.Components.Input.PickerInternals.Popup

open Fable.React

open LibClient
open LibClient.Components
open LibClient.Icons
open LibClient.Components.Input.PickerModel

open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let private scrollViewCache = System.Collections.Generic.Dictionary<int, ScrollViewStyles>()

    let scrollView (fieldWidth: int) : ScrollViewStyles =
        match scrollViewCache.TryGetValue fieldWidth with
        | true, styles -> styles
        | false, _ ->
            let styles =
                makeScrollViewStyles {
                    maxHeight 400
                    shadow (Color.BlackAlpha 0.3) 5 (0, 2)

                    if fieldWidth >= 0 then
                        width (fieldWidth - 4)
                }
            scrollViewCache.[fieldWidth] <- styles
            styles

    let scrollViewFor (maybeFieldWidth: Option<int>) =
        match maybeFieldWidth with
        | Some fieldWidth -> scrollView fieldWidth
        | None            -> scrollView -1

    let view =
        makeViewStyles {
            border 1 (Color.Grey "cc")
            backgroundColor Color.White
            Overflow.VisibleForScrolling
        }

    let item =
        ViewStyles.Memoize (fun (isFirst: bool) (isHighlighted: bool) ->
            makeViewStyles {
                FlexDirection.Row
                AlignItems.Center
                Cursor.Pointer
                paddingHorizontal 18
                paddingLeft 16
                paddingRight 8
                paddingVertical 9
                borderTop 1 (Color.Grey "cc")

                if isFirst then
                    borderTopWidth 0

                if isHighlighted then
                    backgroundColor (Color.Grey "ee")
            })

    let itemSelectedness =
        makeViewStyles {
            width 24
            flex 0
        }

    let itemBody =
        makeViewStyles {
            flex 1
        }

    let itemSelectedIcon =
        makeTextStyles {
            fontSize 16
            color (Color.Grey "cc")
        }

    let itemLabel =
        TextStyles.Memoize (fun (isHighlighted: bool) ->
            makeTextStyles {
                color (
                    if isHighlighted then
                        Color.Grey "33"
                    else
                        Color.Grey "66"
                )
                fontSize 16
            })

    let noItemsMessage =
        makeViewStyles {
            paddingHV 20 20
            AlignItems.Center
        }

    let noItemsMessageText =
        makeTextStyles {
            color (Color.Grey "66")
        }

    let activityIndicatorBlock =
        makeViewStyles {
            FlexDirection.Row
            JustifyContent.Center
            AlignItems.Center
            paddingVertical 12
        }

    let activityIndicatorOverlay =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            FlexDirection.Row
            JustifyContent.Center
            AlignItems.Center
            backgroundColor (Color.WhiteAlpha 0.5)
        }

module private Helpers =
    let renderItems<'Item when 'Item : comparison>
        (modelState: PickerState<'Item>)
        (itemView: PickerItemView<'Item>)
        (onSelect: int -> 'Item -> Browser.Types.Event -> unit)
        (items: List<'Item>)
        : ReactElement =
        match items with
        | [] ->
            RX.View(
                styles = [| Styles.noItemsMessage |],
                children =
                    [|
                        LC.UiText(
                            value = "No items",
                            styles = [| Styles.noItemsMessageText |]
                        )
                    |]
            )
        | nonemptyItems ->
            castAsElement (
                nonemptyItems
                |> List.mapi (fun index item ->
                    let isHighlighted = modelState.MaybeHighlightedItemIndex = Some index

                    RX.View(
                        onPress = onSelect index item,
                        styles = [| Styles.item (index = 0) isHighlighted |],
                        children =
                            [|
                                RX.View(
                                    styles = [| Styles.itemSelectedness |],
                                    children =
                                        [|
                                            if modelState.Value.IsSelected item then
                                                LC.Icon(
                                                    icon = Icon.CheckMark,
                                                    styles = [| Styles.itemSelectedIcon |]
                                                )
                                            else
                                                noElement
                                        |]
                                )
                                RX.View(
                                    styles = [| Styles.itemBody |],
                                    children =
                                        [|
                                            match itemView with
                                            | PickerItemView.Default toItemInfo ->
                                                LC.UiText(
                                                    value = (toItemInfo item).Label,
                                                    styles = [| Styles.itemLabel isHighlighted |]
                                                )
                                            | PickerItemView.Custom render ->
                                                render item
                                        |]
                                )
                            |]
                    )
                )
                |> Array.ofList
            )

type LibClient.Components.Constructors.LC.Input.PickerInternals with
    [<Component>]
    static member Popup<'Item when 'Item : comparison>(
            model: PickerModel<'Item>,
            itemView: PickerItemView<'Item>,
            ?key: string
        ) : ReactElement =
        key |> ignore

        let modelStateHook = Hooks.useState (model.GetState())

        Hooks.useEffectDisposable(
            (fun () ->
                let subscription =
                    model.SubscribeOnStateUpdate (fun update ->
                        modelStateHook.update update.Next
                    )
                { new System.IDisposable with
                    member _.Dispose() = subscription.Off() }
            ),
            [| box model |]
        )

        let modelState = modelStateHook.current

        let onSelect (index: int) (item: 'Item) (_e: Browser.Types.Event) : unit =
            model.HandleInputEvent (Select (index, item))

        let renderWhenAvailable (items: List<'Item>) : ReactElement =
            Helpers.renderItems modelState itemView onSelect items

        RX.ScrollView(
            vertical = true,
            styles = [| Styles.scrollViewFor modelState.MaybeFieldWidth |],
            children =
                [|
                    RX.View(
                        styles = [| Styles.view |],
                        children =
                            [|
                                LC.AsyncData(
                                    data = modelState.SelectableItems,
                                    whenAvailable = renderWhenAvailable,
                                    whenFetching =
                                        fun maybeOldData ->
                                            match maybeOldData with
                                            | None ->
                                                RX.View(
                                                    styles = [| Styles.activityIndicatorBlock |],
                                                    children =
                                                        [|
                                                            RX.ActivityIndicator(
                                                                size = ActivityIndicator.Medium,
                                                                color = "#aaaaaa"
                                                            )
                                                        |]
                                                )
                                            | Some oldData ->
                                                castAsElement [|
                                                    renderWhenAvailable oldData
                                                    RX.View(
                                                        styles = [| Styles.activityIndicatorOverlay |],
                                                        children =
                                                            [|
                                                                RX.ActivityIndicator(
                                                                    size = ActivityIndicator.Medium,
                                                                    color = "#aaaaaa"
                                                                )
                                                            |]
                                                    )
                                                |]
                                )
                            |]
                    )
                |]
        )
