[<AutoOpen>]
module LibClient.Components.Input.PickerInternals.Field

open Fable.React
open Browser.Types

open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.Icons
open LibClient.Components.Input.PickerModel

open ReactXP.Components
open ReactXP.Styles

module LC =
    module Input =
        module PickerInternals =
            module Field =
                type Theme = {
                    BorderLabelColor:        Color
                    BorderLabelFocusColor:   Color
                    BorderLabelInvalidColor: Color
                    TextColor:               Color
                    InvalidReasonColor:      Color
                    PlaceholderColor:        Color
                    TheVerticalPadding:      int
                    IconSize:                int
                }

type Theme = LC.Input.PickerInternals.Field.Theme

open LC.Input.PickerInternals.Field

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            paddingTop 6
        }

    let handheldFullWidthTapArea =
        makeViewStyles {
            flex 1
            AlignSelf.Stretch
        }

    let border (theTheme: Theme) (isInvalid: bool) (isFocused: bool) =
        makeViewStyles {
            borderWidth 1
            borderRadius 4
            paddingHorizontal 10
            paddingVertical theTheme.TheVerticalPadding
            backgroundColor Color.White
            AlignItems.Center
            FlexDirection.RowReverse
            JustifyContent.SpaceBetween
            borderColor (
                if isInvalid then
                    theTheme.BorderLabelInvalidColor
                elif isFocused then
                    theTheme.BorderLabelFocusColor
                else
                    theTheme.BorderLabelColor
            )
        }

    let pickerValues =
        makeViewStyles {
            FlexDirection.Row
            flexGrow 0
            flexShrink 1
        }

    let pickerActions =
        makeViewStyles {
            FlexDirection.Row
        }

    let tag (isHighlighted: bool) =
        makeViewStyles {
            marginRight 4
            borderRadius 3
            borderWidth 1
            borderColor (Color.Grey "e8")
            backgroundColor (Color.Grey "e8")
            FlexDirection.Row

            if isHighlighted then
                borderColor Color.DevRed
        }

    let tagText =
        makeViewStyles {
            paddingHorizontal 6
        }

    let textInput =
        makeViewStyles {
            flex 1
        }

    let selectedItem (theTheme: Theme) =
        makeTextStyles {
            flex 1
            color theTheme.TextColor
        }

    let icon (theTheme: Theme) =
        makeTextStyles {
            fontSize theTheme.IconSize
            color theTheme.TextColor
        }

    let invalidReason (theTheme: Theme) =
        makeTextStyles {
            fontSize 12
            color theTheme.InvalidReasonColor
        }

    let label =
        makeViewStyles {
            Position.Absolute
            top 0
            left 10
            paddingHorizontal 3
            backgroundColor Color.White
        }

    let labelText (theTheme: Theme) (isInvalid: bool) (isFocused: bool) =
        makeTextStyles {
            fontSize 12
            FontWeight.W700
            color (
                if isInvalid then
                    theTheme.BorderLabelInvalidColor
                elif isFocused then
                    theTheme.BorderLabelFocusColor
                else
                    theTheme.BorderLabelColor
            )
        }

    let pressableOverlay =
        makeViewStyles {
            opacity 0.
        }

[<RequireQualifiedAccess>]
module private Actions =
    let showItemSelector (model: PickerModel<'Item>) (_e: ReactEvent.Action) : unit =
        model.HandleInputEvent ShowList

    let onKeyPress (model: PickerModel<'Item>) (maybeTextInput: Option<ITextInputRef>) (e: KeyboardEvent) : unit =
        match e.key with
        | KeyboardEvent.Key.Backspace -> Backspace
        | KeyboardEvent.Key.ArrowUp   -> ArrowUp
        | KeyboardEvent.Key.ArrowDown -> ArrowDown
        | KeyboardEvent.Key.Enter     -> Enter
        | KeyboardEvent.Key.Tab       -> Tab
        | _                           -> ResetDeleteState
        |> model.HandleInputEvent

        match e.key with
        | KeyboardEvent.Key.ArrowUp
        | KeyboardEvent.Key.ArrowDown -> e.preventDefault()
        | KeyboardEvent.Key.Enter     -> maybeTextInput |> Option.sideEffect (fun textInput -> textInput.blur())
        | _                           -> Noop

    let requestFocus
        (model: PickerModel<'Item>)
        (maybeTextInput: IStateHook<Option<ITextInputRef>>)
        (isFocusedHook: IStateHook<bool>)
        (e: ReactEvent.Action)
        : unit =
        showItemSelector model e
        maybeTextInput.current |> Option.sideEffect (fun textInput ->
            isFocusedHook.update true
            textInput.requestFocus()
        )

    let shouldShowSelectedValue (modelState: PickerState<'Item>) (isFocused: bool) : bool =
        modelState.IsListVisible = false && not isFocused

module private RenderHelpers =
    let renderSelectedValue<'Item when 'Item : comparison>
        (theTheme: Theme)
        (value: SelectableValue<'Item>)
        (itemView: PickerItemView<'Item>)
        (modelState: PickerState<'Item>)
        (onUnselect: 'Item -> ReactEvent.Action -> unit)
        : ReactElement =
        let renderItem (item: 'Item) =
            match itemView with
            | PickerItemView.Default toItemInfo ->
                LC.UiText(
                    value = (toItemInfo item).Label,
                    styles = [| Styles.selectedItem theTheme |]
                )
            | PickerItemView.Custom render ->
                render item

        match value with
        | AtMostOne (maybeSelectedValue, _) ->
            match maybeSelectedValue with
            | Some item -> renderItem item
            | None      -> noElement
        | ExactlyOne (maybeSelectedValue, _) ->
            match maybeSelectedValue with
            | Some item ->
                match itemView with
                | PickerItemView.Default toItemInfo ->
                    LC.UiText(
                        value = (toItemInfo item).Label,
                        styles = [| Styles.selectedItem theTheme |],
                        numberOfLines = 1,
                        ellipsizeMode = EllipsizeMode.Tail
                    )
                | PickerItemView.Custom render ->
                    render item
            | None -> noElement
        | AtLeastOne (maybeSelectedValues, _)
        | Any (maybeSelectedValues, _) ->
            match maybeSelectedValues with
            | None -> noElement
            | Some selectedValues ->
                castAsElement (
                    selectedValues.ToSeq
                    |> Seq.map (fun item ->
                        let showUnselect =
                            selectedValues.Count > 1
                            || match value with
                               | Any _ -> true
                               | _     -> false

                        RX.View(
                            styles = [| Styles.tag (modelState.DeleteState = DeleteState.Selected item) |],
                            children =
                                [|
                                    RX.View(
                                        styles = [| Styles.tagText |],
                                        children = [| renderItem item |]
                                    )
                                    if showUnselect then
                                        RX.View(
                                            children =
                                                [|
                                                    LC.Icon(icon = Icon.X, styles = [| Styles.icon theTheme |])
                                                    LC.Pressable(
                                                        onPress = onUnselect item,
                                                        label = "Remove",
                                                        role = AccessibilityRole.Button,
                                                        overlay = true,
                                                        styles = [| Styles.pressableOverlay |],
                                                        componentName = "LC.Input.PickerInternals.Field.Unselect"
                                                    )
                                                |]
                                        )
                                    else
                                        noElement
                                |]
                        )
                    )
                    |> Array.ofSeq
                )

type LibClient.Components.Constructors.LC.Input.PickerInternals with
    [<Component>]
    static member Field<'Item when 'Item : comparison>(
            model: PickerModel<'Item>,
            value: SelectableValue<'Item>,
            validity: InputValidity,
            itemView: PickerItemView<'Item>,
            ?label: string,
            ?placeholder: string,
            ?styles: array<ViewStyles>,
            ?theme: Theme -> Theme,
            ?key: string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme

        let legacyStyles : array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | []     -> [||]
                | styles -> [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles> "ReactXP.Components.View" styles |]
            | None -> [||]

        let modelStateHook = Hooks.useState (model.GetState())
        let maybeQueryHook = Hooks.useState (model.GetState().MaybeQuery)
        let isFocusedHook = Hooks.useState false
        let maybeTextInputHook = Hooks.useState<Option<ITextInputRef>> None

        Hooks.useEffectDisposable(
            (fun () ->
                let subscription =
                    model.SubscribeOnStateUpdate (fun update ->
                        modelStateHook.update update.Next
                        maybeQueryHook.update update.Next.MaybeQuery
                    )
                { new System.IDisposable with
                    member _.Dispose() = subscription.Off() }
            ),
            [| box model |]
        )

        let modelState = modelStateHook.current
        let maybeQuery = maybeQueryHook.current
        let isFocused = isFocusedHook.current

        let showClearButton =
            match value with
            | AtMostOne (maybeSelectedValue, _) -> maybeSelectedValue.IsSome
            | Any (maybeSelectedValues, _) ->
                maybeSelectedValues |> Option.map (fun selectedValues -> not selectedValues.IsEmpty) |> Option.defaultValue false
            | _ -> false

        let onUnselect (item: 'Item) (_e: ReactEvent.Action) : unit =
            model.HandleInputEvent (Unselect item)

        let onClear (_e: ReactEvent.Action) : unit =
            model.HandleInputEvent UnselectAllIfAllowed

        let onLayout (e: ReactXP.Types.ViewOnLayoutEvent) : unit =
            if modelState.MaybeFieldWidth <> Some e.width then
                model.HandleInputEvent (FieldWidthChange e.width)

        let bindTextInput (nullableInstance: LibClient.JsInterop.JsNullable<ITextInputRef>) : unit =
            maybeTextInputHook.update nullableInstance.ToOption

        let placeholderTextColor = theTheme.PlaceholderColor.ToReactXPString

        RX.View(
            onLayout = onLayout,
            styles = [| Styles.view; yield! legacyStyles; yield! (defaultArg styles [||]) |],
            children =
                [|
                    RX.View(
                        styles = [| Styles.border theTheme validity.IsInvalid isFocused |],
                        children =
                            [|
                                RX.View(
                                    styles = [| Styles.pickerActions |],
                                    children =
                                        [|
                                            RX.View(
                                                children =
                                                    [|
                                                        if showClearButton then
                                                            RX.View(
                                                                children =
                                                                    [|
                                                                        LC.Icon(icon = Icon.X, styles = [| Styles.icon theTheme |])
                                                                        LC.Pressable(
                                                                            onPress = onClear,
                                                                            label = "Clear selection",
                                                                            role = AccessibilityRole.Button,
                                                                            overlay = true,
                                                                            styles = [| Styles.pressableOverlay |],
                                                                            componentName = "LC.Input.PickerInternals.Field.Clear"
                                                                        )
                                                                    |]
                                                            )
                                                        else
                                                            noElement
                                                    |]
                                            )
                                            RX.View(
                                                children =
                                                    [|
                                                        LC.Icon(icon = Icon.ChevronDown, styles = [| Styles.icon theTheme |])
                                                        LC.Pressable(
                                                            onPress = Actions.showItemSelector model,
                                                            label = defaultArg label "Open picker",
                                                            role = AccessibilityRole.Button,
                                                            overlay = true,
                                                            styles = [| Styles.pressableOverlay |],
                                                            componentName = "LC.Input.PickerInternals.Field.Open"
                                                        )
                                                    |]
                                            )
                                        |]
                                )

                                LC.Responsive(
                                    desktop =
                                        (fun _ ->
                                            let placeholderValue =
                                                match (value.IsEmpty, placeholder) with
                                                | (true, Some value) -> value
                                                | _                  -> ""

                                            RX.TextInput(
                                                styles = [| Styles.textInput |],
                                                ``ref`` = bindTextInput,
                                                value = (maybeQuery |> NonemptyString.optionToString),
                                                placeholder = placeholderValue,
                                                placeholderTextColor = placeholderTextColor,
                                                onFocus =
                                                    (fun _ ->
                                                        Actions.showItemSelector model ReactEvent.Action.NonUserOriginatingAction
                                                        isFocusedHook.update true),
                                                onBlur = (fun _ -> isFocusedHook.update false),
                                                onChangeText =
                                                    (NonemptyString.ofString >> fun q ->
                                                        maybeQueryHook.update q
                                                        model.HandleInputEvent (QueryChange q)),
                                                onKeyPress = Actions.onKeyPress model maybeTextInputHook.current
                                            )),
                                    handheld =
                                        fun _ ->
                                            RX.View(
                                                styles = [| Styles.handheldFullWidthTapArea |],
                                                children =
                                                    [|
                                                        match (if modelState.Value.IsEmpty then placeholder else None) with
                                                        | Some placeholderValue ->
                                                            RX.TextInput(
                                                                styles = [| Styles.textInput |],
                                                                editable = false,
                                                                placeholder = placeholderValue,
                                                                placeholderTextColor = placeholderTextColor
                                                            )
                                                        | None ->
                                                            RX.TextInput(
                                                                styles = [| Styles.textInput |],
                                                                editable = false,
                                                                placeholderTextColor = placeholderTextColor
                                                            )
                                                        LC.Pressable(
                                                            onPress = Actions.showItemSelector model,
                                                            label = defaultArg label "Open picker",
                                                            role = AccessibilityRole.Button,
                                                            overlay = true,
                                                            styles = [| Styles.pressableOverlay |],
                                                            componentName = "LC.Input.PickerInternals.Field.OpenHandheld"
                                                        )
                                                    |]
                                            )
                                )

                                if Actions.shouldShowSelectedValue modelState isFocused then
                                    RX.View(
                                        styles = [| Styles.pickerValues |],
                                        children =
                                            [|
                                                RenderHelpers.renderSelectedValue theTheme value itemView modelState onUnselect
                                                LC.Pressable(
                                                    onPress =
                                                        Actions.requestFocus model maybeTextInputHook isFocusedHook,
                                                    label = defaultArg label "Focus picker",
                                                    role = AccessibilityRole.Button,
                                                    overlay = true,
                                                    styles = [| Styles.pressableOverlay |],
                                                    componentName = "LC.Input.PickerInternals.Field.RequestFocus"
                                                )
                                            |]
                                    )
                                else
                                    noElement
                            |]
                    )

                    match validity.InvalidReason with
                    | Some reason ->
                        LC.UiText(
                            value = reason,
                            styles = [| Styles.invalidReason theTheme |]
                        )
                    | None -> noElement

                    match label with
                    | Some labelText ->
                        RX.View(
                            styles = [| Styles.label |],
                            children =
                                [|
                                    LC.UiText(
                                        value = labelText,
                                        styles = [| Styles.labelText theTheme validity.IsInvalid isFocused |]
                                    )
                                |]
                        )
                    | None -> noElement
                |]
        )
