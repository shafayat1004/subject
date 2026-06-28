module LibClient.Components.Input.PickerInternals.Dialog

open Fable.React

open LibLangFsharp
open LibClient
open LibClient.Dialogs
open LibClient.Accessibility
open LibClient.Components
open LibClient.Icons
open LibClient.Components.Input.PickerModel
open LibClient.Components.Dialog.Base
open LibClient.Components.Dialog.Shell.WhiteRounded
open LibClient.Responsive

open ReactXP.Components
open ReactXP.Styles

type Parameters<'Item when 'Item : comparison> = {
    Placeholder:   string option
    Model:         PickerModel<'Item>
    ItemView:      PickerItemView<'Item>
    HideDeferred:  Deferred<unit>
    ShowSearchBar: bool
}

[<RequireQualifiedAccess>]
module private Styles =
    let textInput =
        ViewStyles.Memoize (fun (screenSize: ScreenSize) ->
            makeViewStyles {
                paddingHV 12 4
                marginBottom 10
                borderRadius 4
                borderWidth 1
                borderColor (Color.Grey "cc")

                match screenSize with
                | ScreenSize.Desktop -> height 46
                | ScreenSize.Handheld -> height 40
            })

    let textInputFont =
        TextStyles.Memoize (fun (screenSize: ScreenSize) ->
            makeTextStyles {
                match screenSize with
                | ScreenSize.Desktop -> fontSize 20
                | ScreenSize.Handheld -> fontSize 16
            })

    let itemList =
        makeViewStyles {
            Overflow.VisibleForScrolling
        }

    let item =
        makeViewStyles {
            paddingLeft 16
            paddingRight 8
            paddingVertical 12
            FlexDirection.Row
        }

    let itemSelectedness =
        makeViewStyles {
            width 24
            flex 0
        }

    let itemSelectedIcon =
        makeTextStyles {
            fontSize 16
            color (Color.Grey "cc")
        }

    let itemBody =
        makeViewStyles {
            flex 1
        }

    let pressableOverlay =
        makeViewStyles {
            opacity 0.
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
        (onToggle: int -> 'Item -> ReactEvent.Action -> unit)
        (items: seq<'Item>)
        : ReactElement =
        let itemLabel (item: 'Item) =
            match itemView with
            | PickerItemView.Default toItemInfo -> (toItemInfo item).Label
            | PickerItemView.Custom _           -> "Select item"

        LC.ItemList(
            style = ItemList.Raw,
            items = items,
            styles = [| Styles.itemList |],
            whenNonempty =
                fun items ->
                    castAsElement (
                        items
                        |> Seq.mapi (fun index item ->
                            RX.View(
                                styles = [| Styles.item |],
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
                                                        LC.UiText(value = (toItemInfo item).Label)
                                                    | PickerItemView.Custom render ->
                                                        render item
                                                |]
                                        )
                                        LC.Pressable(
                                            onPress = onToggle index item,
                                            label = itemLabel item,
                                            testId = A11ySlug.testId "picker-item" (itemLabel item),
                                            role = AccessibilityRole.Button,
                                            overlay = true,
                                            styles = [| Styles.pressableOverlay |],
                                            componentName = "LC.Input.PickerInternals.Dialog.Toggle"
                                        )
                                    |]
                            )
                        )
                        |> Array.ofSeq
                    )
        )

type private DialogContent<'Item when 'Item : comparison> =
    [<Component>]
    static member Render(
            dialogProps: DialogProps<Parameters<'Item>, unit>,
            parameters: Parameters<'Item>
        ) : ReactElement =
        let modelStateHook = Hooks.useState (parameters.Model.GetState())

        Hooks.useEffectDisposable(
            (fun () ->
                let subscription =
                    parameters.Model.SubscribeOnStateUpdate (fun update ->
                        modelStateHook.update update.Next
                    )
                { new System.IDisposable with
                    member _.Dispose() = subscription.Off() }
            ),
            [| box parameters.Model |]
        )

        Hooks.useEffect(
            (fun () ->
                async {
                    do! parameters.HideDeferred.Value
                    Dialogs.tryCancel dialogProps (fun () -> Async.Of true) DialogCloseMethod.HistoryBack ReactEvent.Action.NonUserOriginatingAction
                } |> startSafely
            ),
            [| box parameters.HideDeferred |]
        )

        let modelState = modelStateHook.current

        let tryCancel (_e: ReactEvent.Action) : unit =
            parameters.Model.HandleInputEvent ListWasHidden

        let onToggle (index: int) (item: 'Item) (_e: ReactEvent.Action) : unit =
            parameters.Model.HandleInputEvent (Toggle (index, item))

        let onQueryChange (value: Option<NonemptyString>) : unit =
            parameters.Model.HandleInputEvent (QueryChange value)

        let renderWhenAvailable (items: List<'Item>) : ReactElement =
            Helpers.renderItems modelState parameters.ItemView onToggle (List.toSeq items)

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    LC.Dialog.Shell.WhiteRounded.Raw(
                        position = Raw.DialogPosition.Top,
                        canClose = When ([ OnEscape; OnBackground; OnCloseButton ], tryCancel),
                        children =
                            [|
                                if parameters.ShowSearchBar then
                                    LC.With.Ref(
                                        onInitialize = (fun (input: ITextInputRef) -> input.requestFocus()),
                                        ``with`` =
                                            (fun (bindInput, _) ->
                                                RX.View(
                                                    onPress = (fun e -> e.stopPropagation()),
                                                    children =
                                                        [|
                                                            RX.TextInput(
                                                                ``ref`` = bindInput,
                                                                styles = [| Styles.textInput screenSize |],
                                                                value = (modelState.MaybeQuery |> NonemptyString.optionToString),
                                                                onChangeText = (NonemptyString.ofString >> onQueryChange),
                                                                ?placeholder = parameters.Placeholder
                                                            )
                                                        |]
                                                ))
                                    )
                                else
                                    noElement

                                RX.ScrollView(
                                    vertical = true,
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

                                if modelState.Value.CanSelectMultiple then
                                    LC.Buttons(
                                        children =
                                            elements {
                                                LC.Button(
                                                    label = "Done",
                                                    state = Button.PropStateFactory.MakeLowLevel (Button.Actionable tryCancel)
                                                )
                                            }
                                    )
                                else
                                    noElement
                            |]
                    )
        )

let Open
    (itemView: PickerItemView<'Item>)
    (model: PickerModel<'Item>)
    (hideDeferred: Deferred<unit>)
    (placeholder: string option)
    (showSearchBar: bool)
    (close: DialogCloseMethod -> ReactEvent.Action -> unit)
    : ReactElement =

    doOpen<Parameters<'Item>, unit>
        "PickerInternals.Dialog"
        {
            Placeholder   = placeholder
            Model         = model
            ItemView      = itemView
            HideDeferred  = hideDeferred
            ShowSearchBar = showSearchBar
        }
        (fun dialogProps _ ->
            DialogContent.Render(dialogProps, dialogProps.Parameters)
        )
        {
            OnResult      = ignore
            MaybeOnCancel = None
        }
        close
