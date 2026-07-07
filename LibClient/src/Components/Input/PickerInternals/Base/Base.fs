[<AutoOpen>]
module LibClient.Components.Input.PickerInternals.Base

open Fable.Core.JsInterop

open Fable.React

open LibLangFsharp
open LibClient
open LibClient.Components
open LibClient.Responsive
open LibClient.Components.Input.PickerModel

open Rn.Components
open Rn.Styles

module private Helpers =
    let createModel<'Item when 'Item: comparison>
        (items: Items<'Item>)
        (value: SelectableValue<'Item>)
        : PickerModel<'Item> =
        let initialState =
            { SelectableItems = WillStartFetchingSoonHack
              Value = value
              MaybeQuery = None
              MaybeHighlightedItemIndex = None
              KeyboardSelectionState = KeyboardSelectionState.NothingSelected
              DeleteState = DeleteState.Idle
              IsListVisible = false
              MaybeFieldWidth = None }

        PickerModel(LibClient.ServiceInstances.services().EventBus, items, initialState)

let renderPickerBase<'Item when 'Item: comparison>
    (
        items: Items<'Item>,
        itemView: PickerItemView<'Item>,
        value: SelectableValue<'Item>,
        validity: InputValidity,
        screenSize: ScreenSize,
        showSearchBar: bool,
        label: string option,
        placeholder: string option,
        testId: string option,
        pickerId: string option,
        styles: ViewStyles array option,
        xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles> option
    ) : ReactElement =
    let popupId = "LibClient.Components.Input.Picker"
    let modelRef = Hooks.useRef (Helpers.createModel items value)
    let maybePopupHideRef = Hooks.useRef<Option<unit -> unit>> None
    let maybeDialogHideRef = Hooks.useRef<Option<unit -> unit>> None
    let anchorRef = Hooks.useRef<obj> null

    Hooks.useEffect ((fun () -> modelRef.current.SetValue value), [| box value |])

    let hideList () : unit =
        maybePopupHideRef.current |> Option.sideEffect (fun hide -> hide ())
        maybeDialogHideRef.current |> Option.sideEffect (fun hide -> hide ())

    let showList () : unit =
        let model = modelRef.current

        match screenSize with
        | ScreenSize.Desktop ->
            let popup = LC.Input.PickerInternals.Popup(model, itemView, ?key = pickerId)

            let options =
                Rn.Popup.popupShowOptions
                    (fun () -> anchorRef.current)
                    (fun (_anchorPosition: obj) (_anchorOffset: int) (_popupWidth: int) (_popupHeight: int) -> popup)
                    (fun () ->
                        maybePopupHideRef.current <- None
                        model.HandleInputEvent ListWasHidden)

            LibClient.JsInterop.runOnNextTick (fun () ->
                // Defer one extra tick so the opening click does not immediately dismiss the popup (web).
                LibClient.JsInterop.runOnNextTick (fun () ->
                    Rn.Popup.show (options, popupId)

                    maybePopupHideRef.current <-
                        Some(fun () ->
                            Rn.Popup.dismiss (popupId)
                            maybePopupHideRef.current <- None)))

        | ScreenSize.Handheld ->
            if maybeDialogHideRef.current.IsSome then
                ()
            else
                let hideDeferred = Deferred<unit>()

                maybeDialogHideRef.current <-
                    Some(fun () ->
                        hideDeferred.Resolve()
                        maybeDialogHideRef.current <- None)

                LibClient.Dialogs.AdHoc.go
                    (LibClient.Components.Input.PickerInternals.Dialog.Open
                        itemView
                        model
                        hideDeferred
                        placeholder
                        showSearchBar)
                    ReactEvent.Action.NonUserOriginatingAction

    Hooks.useEffectDisposable (
        (fun () ->
            let model = modelRef.current

            let subscription =
                model.SubscribeOnStateUpdate(fun update ->
                    match (update.Prev.IsListVisible, update.Next.IsListVisible) with
                    | (false, true) -> showList ()
                    | (true, false) -> hideList ()
                    | _ -> Noop)

            { new System.IDisposable with
                member _.Dispose() =
                    subscription.Off()
                    hideList () }),
        [| box screenSize
           box itemView
           box placeholder
           box showSearchBar
           box pickerId |]
    )

    Rn.View(
        ref = (fun r -> anchorRef.current <- r),
        children =
            [| LC.Input.PickerInternals.Field(
                   model = modelRef.current,
                   ?label = label,
                   ?placeholder = placeholder,
                   ?testId = testId,
                   value = value,
                   validity = validity,
                   itemView = itemView,
                   ?styles = styles,
                   ?xLegacyStyles = xLegacyStyles
               ) |]
    )

type LibClient.Components.Constructors.LC.Input.PickerInternals with
    [<Component>]
    static member Base<'Item when 'Item: comparison>
        (
            items: Items<'Item>,
            itemView: PickerItemView<'Item>,
            value: SelectableValue<'Item>,
            validity: InputValidity,
            screenSize: ScreenSize,
            showSearchBar: bool,
            ?label: string,
            ?placeholder: string,
            ?testId: string,
            ?pickerId: string,
            ?styles: array<ViewStyles>,
            ?key: string,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore

        renderPickerBase (
            items,
            itemView,
            value,
            validity,
            screenSize,
            showSearchBar,
            label,
            placeholder,
            testId,
            pickerId,
            styles,
            xLegacyStyles
        )
