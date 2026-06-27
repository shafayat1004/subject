module LibClient.ContextMenuPopupHost

open LibClient
open LibClient.Components
open LibClient.ContextMenus.Types

let make (items: List<ContextMenuItem>) (hide: unit -> unit) (openingEvent: ReactEvent.Action) =
    LC.ContextMenuPopup(
        items = items,
        hide = hide,
        openingEvent = openingEvent
    )
