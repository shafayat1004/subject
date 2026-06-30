[<AutoOpen>]
module LibClient.Components.Input.PickerInternals.Field

open Fable.Core.JsInterop
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
                type Theme =
                    { BorderLabelColor: Color
                      BorderLabelFocusColor: Color
                      BorderLabelInvalidColor: Color
                      TextColor: Color
                      InvalidReasonColor: Color
                      PlaceholderColor: Color
                      TheVerticalPadding: int
                      IconSize: int
                      BackgroundColor: Color
                      BorderRadius: int
                      LabelBackgroundColor: Color }

type Theme = LC.Input.PickerInternals.Field.Theme

open LC.Input.PickerInternals.Field

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        ViewStyles.Memoize(fun (hasLabel: bool) ->
            makeViewStyles {
                Overflow.Visible

                if hasLabel then
                    marginTop 6
            })

    let handheldFullWidthTapArea =
        makeViewStyles {
            flex 1
            AlignSelf.Stretch
        }

    let private outlineColorFor (theTheme: Theme) (isInvalid: bool) (isFocused: bool) =
        if isInvalid then theTheme.BorderLabelInvalidColor
        elif isFocused then theTheme.BorderLabelFocusColor
        else theTheme.BorderLabelColor

    let border =
        ViewStyles.Memoize(fun (verticalPadding: int) (cornerRadius: int) (fillColor: Color) (outline: Color) ->
            makeViewStyles {
                borderWidth 1
                borderRadius cornerRadius
                paddingHorizontal 10
                paddingVertical verticalPadding
                backgroundColor fillColor
                AlignItems.Center
                FlexDirection.RowReverse
                JustifyContent.SpaceBetween
                borderColor outline
                Overflow.Hidden
            })

    let fieldValueArea =
        makeViewStyles {
            flex 1
            minWidth 0
            Position.Relative
            minHeight 21
        }

    let pickerValuesOverlay =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            FlexDirection.Row
            AlignItems.Center
            paddingHorizontal 2
        }

    let webFieldTextInput =
        makeViewStyles {
            flex 1
            minWidth 0
#if EGGSHELL_PLATFORM_IS_WEB
            backgroundColor Color.Transparent
            borderWidth 0
            padding 0
            margin 0
#endif
        }

    let hiddenTextInput =
        makeViewStyles {
            opacity 0
            width 1
            height 1
            Position.Absolute
            top (-1000)
            left 0
        }

    let borderFor (theTheme: Theme) (isInvalid: bool) (isFocused: bool) =
        border
            theTheme.TheVerticalPadding
            theTheme.BorderRadius
            theTheme.BackgroundColor
            (outlineColorFor theTheme isInvalid isFocused)

    let pickerValues =
        makeViewStyles {
            FlexDirection.Row
            flex 1
            AlignSelf.Stretch
            AlignItems.Center
            heightPercent 100
        }

    let pickerActions = makeViewStyles { FlexDirection.Row }

    let tag =
        ViewStyles.Memoize(fun (isHighlighted: bool) ->
            makeViewStyles {
                marginRight 4
                borderRadius 3
                borderWidth 1
                borderColor (Color.Grey "e8")
                backgroundColor (Color.Grey "e8")
                FlexDirection.Row

                if isHighlighted then
                    borderColor Color.DevRed
            })

    let tagText = makeViewStyles { paddingHorizontal 6 }

    let textInput = makeViewStyles { flex 1 }

    let selectedItem =
        TextStyles.Memoize(fun (textColor: Color) ->
            makeTextStyles {
                flex 1
                color textColor
            })

    let icon =
        TextStyles.Memoize(fun (iconSize: int) (textColor: Color) ->
            makeTextStyles {
                fontSize iconSize
                color textColor
            })

    let invalidReason =
        TextStyles.Memoize(fun (reasonColor: Color) ->
            makeTextStyles {
                fontSize 12
                color reasonColor
            })

    let label =
        ViewStyles.Memoize(fun (labelBg: Color) ->
            makeViewStyles {
                Position.Absolute
                top -6
                left 10
                paddingHorizontal 3
                backgroundColor labelBg
            })

    let labelText =
        TextStyles.Memoize(fun (labelColor: Color) ->
            makeTextStyles {
                fontSize 12
                FontWeight.W700
                color labelColor
            })

    let labelTextFor (theTheme: Theme) (isInvalid: bool) (isFocused: bool) =
        labelText (outlineColorFor theTheme isInvalid isFocused)

    let pressableOverlay = makeViewStyles { opacity 0. }

[<RequireQualifiedAccess>]
module private Actions =
    let showItemSelector (model: PickerModel<'Item>) (_e: ReactEvent.Action) : unit = model.HandleInputEvent ShowList

    let onKeyPress (model: PickerModel<'Item>) (maybeTextInput: Option<ITextInputRef>) (e: KeyboardEvent) : unit =
        match e.key with
        | KeyboardEvent.Key.Backspace -> Backspace
        | KeyboardEvent.Key.ArrowUp -> ArrowUp
        | KeyboardEvent.Key.ArrowDown -> ArrowDown
        | KeyboardEvent.Key.Enter -> Enter
        | KeyboardEvent.Key.Tab -> Tab
        | _ -> ResetDeleteState
        |> model.HandleInputEvent

        match e.key with
        | KeyboardEvent.Key.ArrowUp
        | KeyboardEvent.Key.ArrowDown -> e.preventDefault ()
        | KeyboardEvent.Key.Enter -> maybeTextInput |> Option.sideEffect (fun textInput -> textInput.blur ())
        | _ -> Noop

    let requestFocus
        (maybeTextInput: IRefHook<Option<ITextInputRef>>)
        (isFocusedHook: IStateHook<bool>)
        (_e: ReactEvent.Action)
        : unit =
        maybeTextInput.current
        |> Option.sideEffect (fun textInput ->
            isFocusedHook.update true
            textInput.requestFocus ())

    let shouldShowSelectedValue (modelState: PickerState<'Item>) (isFocused: bool) : bool =
        modelState.IsListVisible = false && not isFocused

module private RenderHelpers =
    let renderSelectedValue<'Item when 'Item: comparison>
        (theTheme: Theme)
        (value: SelectableValue<'Item>)
        (itemView: PickerItemView<'Item>)
        (modelState: PickerState<'Item>)
        (onUnselect: 'Item -> ReactEvent.Action -> unit)
        (resolvedTestId: string)
        : ReactElement =
        let renderItem (item: 'Item) =
            match itemView with
            | PickerItemView.Default toItemInfo ->
                LC.UiText(value = (toItemInfo item).Label, styles = [| Styles.selectedItem theTheme.TextColor |])
            | PickerItemView.Custom render -> render item

        let itemLabel (item: 'Item) =
            match itemView with
            | PickerItemView.Default toItemInfo -> (toItemInfo item).Label
            | PickerItemView.Custom _ -> "item"

        match value with
        | AtMostOne(maybeSelectedValue, _) ->
            match maybeSelectedValue with
            | Some item -> renderItem item
            | None -> noElement
        | ExactlyOne(maybeSelectedValue, _) ->
            match maybeSelectedValue with
            | Some item ->
                match itemView with
                | PickerItemView.Default toItemInfo ->
                    LC.UiText(
                        value = (toItemInfo item).Label,
                        styles = [| Styles.selectedItem theTheme.TextColor |],
                        numberOfLines = 1,
                        ellipsizeMode = EllipsizeMode.Tail
                    )
                | PickerItemView.Custom render -> render item
            | None -> noElement
        | AtLeastOne(maybeSelectedValues, _)
        | Any(maybeSelectedValues, _) ->
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
                               | _ -> false

                        RX.View(
                            styles = [| Styles.tag (modelState.DeleteState = DeleteState.Selected item) |],
                            children =
                                [| RX.View(styles = [| Styles.tagText |], children = [| renderItem item |])
                                   if showUnselect then
                                       RX.View(
                                           children =
                                               [| LC.Icon(
                                                      icon = Icon.X,
                                                      styles = [| Styles.icon theTheme.IconSize theTheme.TextColor |]
                                                  )
                                                  LC.Pressable(
                                                      onPress = onUnselect item,
                                                      label = "Remove",
                                                      testId =
                                                          A11ySlug.testId
                                                              (sprintf "%s-unselect" resolvedTestId)
                                                              (itemLabel item),
                                                      role = AccessibilityRole.Button,
                                                      overlay = true,
                                                      styles = [| Styles.pressableOverlay |],
                                                      componentName = "LC.Input.PickerInternals.Field.Unselect"
                                                  ) |]
                                       )
                                   else
                                       noElement |]
                        ))
                    |> Array.ofSeq
                )

type LibClient.Components.Constructors.LC.Input.PickerInternals with
    [<Component>]
    static member Field<'Item when 'Item: comparison>
        (
            model: PickerModel<'Item>,
            value: SelectableValue<'Item>,
            validity: InputValidity,
            itemView: PickerItemView<'Item>,
            ?label: string,
            ?placeholder: string,
            ?testId: string,
            ?styles: array<ViewStyles>,
            ?theme: Theme -> Theme,
            ?key: string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore

        let theTheme = Themes.GetMaybeUpdatedWith theme

        let legacyStyles: array<ViewStyles> =
            match xLegacyStyles with
            | Some ls ->
                match ReactXP.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                | [] -> [||]
                | styles ->
                    [| ReactXP.LegacyStyles.Runtime.prepareStylesForPassingToReactXpComponent<ViewStyles>
                           "ReactXP.Components.View"
                           styles |]
            | None -> [||]

        let modelStateHook = Hooks.useState (model.GetState())
        let maybeQueryHook = Hooks.useState (model.GetState().MaybeQuery)
        let isFocusedHook = Hooks.useState false
        let maybeTextInputRef = Hooks.useRef<Option<ITextInputRef>> None

        Hooks.useEffectDisposable (
            (fun () ->
                let subscription =
                    model.SubscribeOnStateUpdate(fun update ->
                        modelStateHook.update update.Next
                        maybeQueryHook.update update.Next.MaybeQuery)

                { new System.IDisposable with
                    member _.Dispose() = subscription.Off() }),
            [| box model |]
        )

        let modelState = modelStateHook.current
        let maybeQuery = maybeQueryHook.current
        let isFocused = isFocusedHook.current

        let showClearButton =
            match value with
            | AtMostOne(maybeSelectedValue, _) -> maybeSelectedValue.IsSome
            | Any(maybeSelectedValues, _) ->
                maybeSelectedValues
                |> Option.map (fun selectedValues -> not selectedValues.IsEmpty)
                |> Option.defaultValue false
            | _ -> false

        let onUnselect (item: 'Item) (_e: ReactEvent.Action) : unit = model.HandleInputEvent(Unselect item)

        let onClear (_e: ReactEvent.Action) : unit =
            model.HandleInputEvent UnselectAllIfAllowed

        let onLayout (e: ReactXP.Types.ViewOnLayoutEvent) : unit =
            if modelState.MaybeFieldWidth <> Some e.width then
                model.HandleInputEvent(FieldWidthChange e.width)

        let bindTextInput (nullableInstance: LibClient.JsInterop.JsNullable<ITextInputRef>) : unit =
            maybeTextInputRef.current <-
                nullableInstance.ToOption
                |> Option.map (fun raw ->
                    let rawObj = raw :> obj

                    { new ITextInputRef with
                        member _.selectAll() : unit =
                            rawObj?focus () |> ignore

                            let len =
                                match rawObj?value with
                                | null -> 0
                                | (s: string) -> s.Length

                            rawObj?setSelection (0, len) |> ignore

                        member _.requestFocus() : unit = rawObj?focus () |> ignore
                        member _.blur() : unit = rawObj?blur () |> ignore })

        let placeholderTextColor = theTheme.PlaceholderColor.ToReactXPString

        let resolvedTestId =
            testId
            |> Option.orElse (label |> Option.map (A11ySlug.testId "input-picker"))
            |> Option.defaultValue "input-picker"

        let openLabel = defaultArg label "Open picker"

        RX.View(
            testId = resolvedTestId,
            onLayout = onLayout,
            styles =
                [| Styles.view label.IsSome
                   yield! legacyStyles
                   yield! (defaultArg styles [||]) |],
            children =
                [| RX.View(
                       styles = [| Styles.borderFor theTheme validity.IsInvalid isFocused |],
                       children =
                           [| RX.View(
                                  styles = [| Styles.pickerActions |],
                                  children =
                                      [| RX.View(
                                             children =
                                                 [| if showClearButton then
                                                        RX.View(
                                                            children =
                                                                [| LC.Icon(
                                                                       icon = Icon.X,
                                                                       styles =
                                                                           [| Styles.icon
                                                                                  theTheme.IconSize
                                                                                  theTheme.TextColor |]
                                                                   )
                                                                   LC.Pressable(
                                                                       onPress = onClear,
                                                                       label = "Clear selection",
                                                                       testId = sprintf "%s-clear" resolvedTestId,
                                                                       role = AccessibilityRole.Button,
                                                                       overlay = true,
                                                                       styles = [| Styles.pressableOverlay |],
                                                                       componentName =
                                                                           "LC.Input.PickerInternals.Field.Clear"
                                                                   ) |]
                                                        )
                                                    else
                                                        noElement |]
                                         )
                                         RX.View(
                                             children =
                                                 [| LC.Icon(
                                                        icon = Icon.ChevronDown,
                                                        styles = [| Styles.icon theTheme.IconSize theTheme.TextColor |]
                                                    )
                                                    LC.Pressable(
                                                        onPress = Actions.showItemSelector model,
                                                        label = openLabel,
                                                        testId = sprintf "%s-open" resolvedTestId,
                                                        role = AccessibilityRole.Button,
                                                        overlay = true,
                                                        styles = [| Styles.pressableOverlay |],
                                                        componentName = "LC.Input.PickerInternals.Field.Open"
                                                    ) |]
                                         ) |]
                              )

                              LC.Responsive(
                                  desktop =
                                      (fun _ ->
                                          let placeholderValue =
                                              match (value.IsEmpty, placeholder) with
                                              | (true, Some value) -> value
                                              | _ -> ""

                                          let showSelectedOverlay =
                                              Actions.shouldShowSelectedValue modelState isFocused

                                          let textInputStyles =
                                              if showSelectedOverlay then
                                                  [| Styles.textInput
                                                     Styles.webFieldTextInput
                                                     Styles.hiddenTextInput |]
                                              else
                                                  [| Styles.textInput; Styles.webFieldTextInput |]

                                          RX.View(
                                              styles = [| Styles.fieldValueArea |],
                                              children =
                                                  [| RX.TextInput(
                                                         styles = textInputStyles,
                                                         ``ref`` = bindTextInput,
                                                         value = (maybeQuery |> NonemptyString.optionToString),
                                                         placeholder = placeholderValue,
                                                         placeholderTextColor = placeholderTextColor,
                                                         onFocus = (fun _ -> isFocusedHook.update true),
                                                         onBlur = (fun _ -> isFocusedHook.update false),
                                                         onChangeText =
                                                             (NonemptyString.ofString
                                                              >> fun q ->
                                                                  maybeQueryHook.update q
                                                                  model.HandleInputEvent(QueryChange q)),
                                                         onKeyPress =
                                                             Actions.onKeyPress model maybeTextInputRef.current
                                                     )
                                                     if showSelectedOverlay then
                                                         RX.View(
                                                             styles = [| Styles.pickerValuesOverlay |],
                                                             children =
                                                                 [| RX.View(
                                                                        styles = [| Styles.pickerValues |],
                                                                        children =
                                                                            [| RenderHelpers.renderSelectedValue
                                                                                   theTheme
                                                                                   value
                                                                                   itemView
                                                                                   modelState
                                                                                   onUnselect
                                                                                   resolvedTestId
                                                                               LC.Pressable(
                                                                                   onPress =
                                                                                       (fun e ->
                                                                                           Actions.showItemSelector
                                                                                               model
                                                                                               e

                                                                                           Actions.requestFocus
                                                                                               maybeTextInputRef
                                                                                               isFocusedHook
                                                                                               e),
                                                                                   label = openLabel,
                                                                                   testId =
                                                                                       sprintf
                                                                                           "%s-focus"
                                                                                           resolvedTestId,
                                                                                   role = AccessibilityRole.Button,
                                                                                   overlay = true,
                                                                                   styles =
                                                                                       [| Styles.pressableOverlay |],
                                                                                   componentName =
                                                                                       "LC.Input.PickerInternals.Field.RequestFocus"
                                                                               ) |]
                                                                    ) |]
                                                         )
                                                     else
                                                         noElement |]
                                          )),
                                  handheld =
                                      fun _ ->
                                          let showSelectedOverlay = Actions.shouldShowSelectedValue modelState isFocused

                                          let textInputStyles =
                                              if showSelectedOverlay then
                                                  [| Styles.textInput
                                                     Styles.webFieldTextInput
                                                     Styles.hiddenTextInput |]
                                              else
                                                  [| Styles.textInput; Styles.webFieldTextInput |]

                                          RX.View(
                                              styles = [| Styles.fieldValueArea |],
                                              children =
                                                  [| RX.TextInput(
                                                         styles = textInputStyles,
                                                         editable = false,
                                                         placeholder =
                                                             (match
                                                                 (if modelState.Value.IsEmpty then placeholder else None)
                                                              with
                                                              | Some placeholderValue -> placeholderValue
                                                              | None -> ""),
                                                         placeholderTextColor = placeholderTextColor
                                                     )
                                                     if showSelectedOverlay then
                                                         RX.View(
                                                             styles = [| Styles.pickerValuesOverlay |],
                                                             children =
                                                                 [| RX.View(
                                                                        styles = [| Styles.pickerValues |],
                                                                        children =
                                                                            [| RenderHelpers.renderSelectedValue
                                                                                   theTheme
                                                                                   value
                                                                                   itemView
                                                                                   modelState
                                                                                   onUnselect
                                                                                   resolvedTestId
                                                                               LC.Pressable(
                                                                                   onPress =
                                                                                       Actions.showItemSelector model,
                                                                                   label = openLabel,
                                                                                   testId =
                                                                                       sprintf
                                                                                           "%s-open-handheld"
                                                                                           resolvedTestId,
                                                                                   role = AccessibilityRole.Button,
                                                                                   overlay = true,
                                                                                   styles =
                                                                                       [| Styles.pressableOverlay |],
                                                                                   componentName =
                                                                                       "LC.Input.PickerInternals.Field.OpenHandheld"
                                                                               ) |]
                                                                    ) |]
                                                         )
                                                     else
                                                         RX.View(
                                                             styles = [| Styles.handheldFullWidthTapArea |],
                                                             children =
                                                                 [| LC.Pressable(
                                                                        onPress = Actions.showItemSelector model,
                                                                        label = openLabel,
                                                                        testId =
                                                                            sprintf "%s-open-handheld" resolvedTestId,
                                                                        role = AccessibilityRole.Button,
                                                                        overlay = true,
                                                                        styles = [| Styles.pressableOverlay |],
                                                                        componentName =
                                                                            "LC.Input.PickerInternals.Field.OpenHandheld"
                                                                    ) |]
                                                         ) |]
                                          )
                              )

                              noElement |]
                   )

                   match validity.InvalidReason with
                   | Some reason ->
                       LC.UiText(value = reason, styles = [| Styles.invalidReason theTheme.InvalidReasonColor |])
                   | None -> noElement

                   match label with
                   | Some labelText ->
                       RX.View(
                           styles = [| Styles.label theTheme.LabelBackgroundColor |],
                           children =
                               [| LC.UiText(
                                      value = labelText,
                                      styles = [| Styles.labelTextFor theTheme validity.IsInvalid isFocused |]
                                  ) |]
                       )
                   | None -> noElement |]
        )
