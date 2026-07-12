[<AutoOpen>]
module LibClient.Components.ContextMenu.Popup

open Fable.React

open LibClient
open LibClient.Accessibility
open LibClient.Components
open LibClient.ContextMenus.Types
open LibClient.Icons

open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let scrollView =
        makeScrollViewStyles {
            maxHeight 400
            shadow (Color.BlackAlpha 0.3) 5 (0, 2)
        }

    let view =
        makeViewStyles {
            border          1 (Color.Grey "cc")
            backgroundColor Color.White
            Overflow.VisibleForScrolling
        }

    let button (isFirst: bool) =
        makeViewStyles {
            Position.Relative
            FlexDirection.Row
            AlignItems.Center
            Cursor.Pointer
            height            36
            paddingHorizontal 18
            Overflow.VisibleForTapCapture
            if not isFirst then
                borderTop 1 (Color.Grey "cc")
        }

    let buttonText (isSelected: bool) (isCautionary: bool) =
        makeTextStyles {
            if isCautionary then
                color Color.DevRed
            else
                color (Color.Grey "66")
            fontSize 14
            if isSelected then
                FontWeight.Bold
        }

    let itemTestId (label: string) =
        A11ySlug.testId "context-menu-item" label

let private renderMenuItem
        (index: int)
        (label: string)
        (isSelected: bool)
        (isCautionary: bool)
        (onPress: ReactEvent.Action -> unit)
        (hide: unit -> unit)
        (openingEvent: ReactEvent.Action)
        : ReactElement =
    Rn.View(
        key    = sprintf "item-%i" index,
        styles = [| Styles.button (index = 0) |],
        children =
            [|
                LC.UiText(
                    value  = label,
                    styles = [| Styles.buttonText isSelected isCautionary |]
                )
                LC.Pressable(
                    onPress = (fun _ -> hide(); onPress openingEvent),
                    label   = label,
                    role    = AccessibilityRole.MenuItem,
                    state =
                        { AccessibilityStateRecord.empty with
                            Selected = Some isSelected
                        },
                    testId        = Styles.itemTestId label,
                    overlay       = true,
                    componentName = "LC.ContextMenu.Popup"
                )
            |]
    )

let private renderContextMenuPopup
        (items: List<ContextMenuItem>)
        (hide: unit -> unit)
        (openingEvent: ReactEvent.Action)
        (xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles> option)
        : ReactElement =
    let legacyScrollViewStyles : array<ScrollViewStyles> =
        match xLegacyStyles with
        | Some legacyStyles ->
            match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
            | []     -> [||]
            | styles ->
                [|
                    Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ScrollViewStyles>
                        "Rn.Components.ScrollView"
                        styles
                |]
        | None -> [||]

    let itemElements =
        items
        |> List.mapi (fun index item ->
            match item with
            | Divider ->
                Rn.View(key = sprintf "divider-%i" index)
            | Heading text ->
                Rn.View(
                    key      = sprintf "heading-%i" index,
                    children = [| LC.UiText(text, styles = [||]) |]
                )
            | InternalButton (label, isSelected, onPress) ->
                renderMenuItem index label isSelected false onPress hide openingEvent
            | ButtonCautionary (label, onPress) ->
                renderMenuItem index label false true onPress hide openingEvent
        )
        |> Array.ofList

    Rn.ScrollView(
        vertical = true,
        styles   = [| Styles.scrollView; yield! legacyScrollViewStyles |],
        children =
            [|
                Rn.View(
                    styles   = [| Styles.view |],
                    children = itemElements
                )
            |]
    )

let makeContextMenuPopup items hide openingEvent =
    renderContextMenuPopup items hide openingEvent None

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member ContextMenuPopup(
            items:          List<ContextMenuItem>,
            hide:           unit -> unit,
            openingEvent:   ReactEvent.Action,
            ?children:      ReactChildrenProp,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        children |> ignore
        key      |> ignore
        renderContextMenuPopup items hide openingEvent xLegacyStyles
