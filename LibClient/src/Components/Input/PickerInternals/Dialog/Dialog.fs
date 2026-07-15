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

open Rn.Components
open Rn.Styles

type Parameters<'Item when 'Item: comparison> =
    { Placeholder: string option
      Model: PickerModel<'Item>
      ItemView: PickerItemView<'Item>
      HideDeferred: Deferred<unit>
      ShowSearchBar: bool }

[<RequireQualifiedAccess>]
module private Styles =
    let textInput (fieldTheme: LC.Input.PickerInternals.Field.Theme) (screenSize: ScreenSize) =
        makeViewStyles {
            paddingHV 12 4
            marginBottom 10
            borderRadius fieldTheme.BorderRadius
            borderWidth 1
            borderColor fieldTheme.BorderLabelColor
            backgroundColor fieldTheme.BackgroundColor

            match screenSize with
            | ScreenSize.Desktop -> height 46
            | ScreenSize.Handheld -> height 40
        }

    let textInputFont (fieldTheme: LC.Input.PickerInternals.Field.Theme) (screenSize: ScreenSize) =
        makeTextStyles {
            color fieldTheme.TextColor

            match screenSize with
            | ScreenSize.Desktop -> fontSize 20
            | ScreenSize.Handheld -> fontSize 16
        }

    let itemList =
        makeViewStyles {
            Overflow.VisibleForScrolling
            marginTop 8
        }

    let item (fieldTheme: LC.Input.PickerInternals.Field.Theme) =
        makeViewStyles {
            paddingLeft 16
            paddingRight 8
            paddingVertical 12
            FlexDirection.Row
            borderBottom 1 fieldTheme.BorderLabelColor
        }

    let itemSelectedness =
        makeViewStyles {
            width 24
            flex 0
        }

    let itemSelectedIcon (fieldTheme: LC.Input.PickerInternals.Field.Theme) =
        makeTextStyles {
            fontSize 16
            color fieldTheme.BorderLabelFocusColor
        }

    let itemBody = makeViewStyles { flex 1 }

    let itemLabel (fieldTheme: LC.Input.PickerInternals.Field.Theme) =
        makeTextStyles {
            color fieldTheme.TextColor
            fontSize 16
        }

    // 0.02, not 0: Fabric iOS treats alpha < ~0.01 as non-interactive, so an opacity-0
    // (or 0.01) overlay Pressable never receives taps (react-native #50465).
    let pressableOverlay = makeViewStyles { opacity 0.02 }

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
    let renderItems<'Item when 'Item: comparison>
        (fieldTheme: LC.Input.PickerInternals.Field.Theme)
        (modelState: PickerState<'Item>)
        (itemView: PickerItemView<'Item>)
        (onToggle: int -> 'Item -> ReactEvent.Action -> unit)
        (items: seq<'Item>)
        : ReactElement =
        let itemLabel (item: 'Item) =
            match itemView with
            | PickerItemView.Default toItemInfo -> (toItemInfo item).Label
            | PickerItemView.Custom _ -> "Select item"

        LC.ItemList(
            style = ItemList.Raw,
            items = items,
            styles = [| Styles.itemList |],
            whenNonempty =
                fun items ->
                    castAsElement (
                        items
                        |> Seq.mapi (fun index item ->
                            Rn.View(
                                styles = [| Styles.item fieldTheme |],
                                children =
                                    [| Rn.View(
                                           styles = [| Styles.itemSelectedness |],
                                           children =
                                               [| if modelState.Value.IsSelected item then
                                                      LC.Icon(
                                                          icon = Icon.CheckMark,
                                                          styles = [| Styles.itemSelectedIcon fieldTheme |]
                                                      )
                                                  else
                                                      noElement |]
                                       )
                                       Rn.View(
                                           styles = [| Styles.itemBody |],
                                           children =
                                               [| match itemView with
                                                  | PickerItemView.Default toItemInfo ->
                                                      LC.UiText(
                                                          value = (toItemInfo item).Label,
                                                          styles = [| Styles.itemLabel fieldTheme |]
                                                      )
                                                  | PickerItemView.Custom render -> render item |]
                                       )
                                       LC.Pressable(
                                           onPress = onToggle index item,
                                           label = itemLabel item,
                                           testId = A11ySlug.testId "picker-item" (itemLabel item),
                                           role = AccessibilityRole.Button,
                                           overlay = true,
                                           styles = [| Styles.pressableOverlay |],
                                           componentName = "LC.Input.PickerInternals.Dialog.Toggle"
                                       ) |]
                            ))
                        |> Array.ofSeq
                    )
        )

type private DialogContent<'Item when 'Item: comparison> =
    [<Component>]
    static member Render
        (dialogProps: DialogProps<Parameters<'Item>, unit>, parameters: Parameters<'Item>)
        : ReactElement =
        let modelStateHook = Hooks.useState (parameters.Model.GetState())

        Hooks.useEffectDisposable (
            (fun () ->
                let subscription =
                    parameters.Model.SubscribeOnStateUpdate(fun update -> modelStateHook.update update.Next)

                { new System.IDisposable with
                    member _.Dispose() = subscription.Off() }),
            [| box parameters.Model |]
        )

        let modelState = modelStateHook.current

        let fieldTheme =
            Themes.GetMaybeUpdatedWith
                Option<LC.Input.PickerInternals.Field.Theme -> LC.Input.PickerInternals.Field.Theme>.None

        let closeOnceRef = Hooks.useRef false

        let closeDialog () =
            if not closeOnceRef.current then
                closeOnceRef.current <- true

                Dialogs.tryCancel
                    dialogProps
                    (fun () -> Async.Of true)
                    DialogCloseMethod.HistoryForward
                    ReactEvent.Action.NonUserOriginatingAction

        Hooks.useEffect (
            (fun () ->
                async {
                    do! parameters.HideDeferred.Value
                    closeDialog ()
                }
                |> startSafely),
            [| box parameters.HideDeferred |]
        )

        let dismissDialog (e: ReactEvent.Action) : unit =
            e |> ignore
            parameters.Model.HandleInputEvent ListWasHidden

        let onToggle (index: int) (item: 'Item) (e: ReactEvent.Action) : unit =
            e |> ignore
            parameters.Model.HandleInputEvent(Toggle(index, item))

        let onQueryChange (value: Option<NonemptyString>) : unit =
            parameters.Model.HandleInputEvent(QueryChange value)

        let renderWhenAvailable (items: List<'Item>) : ReactElement =
            Helpers.renderItems fieldTheme modelState parameters.ItemView onToggle (List.toSeq items)

        LC.With.ScreenSize(
            ``with`` =
                fun screenSize ->
                    let position =
                        match screenSize with
                        | ScreenSize.Handheld -> Raw.DialogPosition.Center
                        | ScreenSize.Desktop -> Raw.DialogPosition.Center

                    LC.Dialog.Shell.WhiteRounded.Raw(
                        position = position,
                        canClose = When([ OnEscape; OnBackground; OnCloseButton ], dismissDialog),
                        children =
                            [| if parameters.ShowSearchBar then
                                   LC.With.Ref(
                                       onInitialize = (fun (input: ITextInputRef) -> input.requestFocus ()),
                                       ``with`` =
                                           (fun (bindInput, _) ->
                                               Rn.View(
                                                   onPress = (fun e -> e.stopPropagation ()),
                                                   children =
                                                       [| Rn.TextInput(
                                                              ``ref`` = bindInput,
                                                              styles = [| Styles.textInput fieldTheme screenSize |],
                                                              value =
                                                                  (modelState.MaybeQuery
                                                                   |> NonemptyString.optionToString),
                                                              onChangeText = (NonemptyString.ofString >> onQueryChange),
                                                              ?placeholder = parameters.Placeholder,
                                                              placeholderTextColor =
                                                                  fieldTheme.PlaceholderColor.ToRnString
                                                          ) |]
                                               ))
                                   )
                               else
                                   noElement

                               Rn.ScrollView(
                                   vertical = true,
                                   children =
                                       [| LC.AsyncData(
                                              data = modelState.SelectableItems,
                                              whenAvailable = renderWhenAvailable,
                                              whenFetching =
                                                  fun maybeOldData ->
                                                      match maybeOldData with
                                                      | None ->
                                                          Rn.View(
                                                              styles = [| Styles.activityIndicatorBlock |],
                                                              children =
                                                                  [| Rn.ActivityIndicator(
                                                                         size = ActivityIndicator.Medium,
                                                                         color = "#aaaaaa"
                                                                     ) |]
                                                          )
                                                      | Some oldData ->
                                                          castAsElement
                                                              [| renderWhenAvailable oldData
                                                                 Rn.View(
                                                                     styles = [| Styles.activityIndicatorOverlay |],
                                                                     children =
                                                                         [| Rn.ActivityIndicator(
                                                                                size = ActivityIndicator.Medium,
                                                                                color = "#aaaaaa"
                                                                            ) |]
                                                                 ) |]
                                          ) |]
                               )

                               if modelState.Value.CanSelectMultiple then
                                   LC.Buttons(
                                       children =
                                           elements {
                                               LC.Button(
                                                   label = "Done",
                                                   state =
                                                       Button.PropStateFactory.MakeLowLevel(
                                                           Button.Actionable dismissDialog
                                                       )
                                               )
                                           }
                                   )
                               else
                                   noElement |]
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
        { Placeholder = placeholder
          Model = model
          ItemView = itemView
          HideDeferred = hideDeferred
          ShowSearchBar = showSearchBar }
        (fun dialogProps _ -> DialogContent.Render(dialogProps, dialogProps.Parameters))
        { OnResult = ignore
          MaybeOnCancel = None }
        close
