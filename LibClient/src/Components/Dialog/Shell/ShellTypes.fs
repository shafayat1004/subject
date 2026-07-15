namespace LibClient.Components.Dialog.Shell.WhiteRounded

module Raw =
    type CanClose = LibClient.Components.Dialog.Base.CanClose
    let When  = CanClose.When
    let Never = CanClose.Never

    type CloseAction = LibClient.Components.Dialog.Base.CloseAction
    let OnEscape      = CloseAction.OnEscape
    let OnBackground  = CloseAction.OnBackground
    let OnCloseButton = CloseAction.OnCloseButton

    type DialogPosition =
    | Top
    | Center
    | Bottom

module Base =
    type CanClose = LibClient.Components.Dialog.Base.CanClose
    let When = CanClose.When

    type CloseAction = LibClient.Components.Dialog.Base.CloseAction
    let OnEscape      = CloseAction.OnEscape
    let OnBackground  = CloseAction.OnBackground
    let OnCloseButton = CloseAction.OnCloseButton

module Standard =
    open Fable.React

    [<RequireQualifiedAccess>]
    type Mode =
    | Default
    | InProgress
    | Error of Message: string

    type CanClose = LibClient.Components.Dialog.Base.CanClose
    let When  = CanClose.When
    let Never = CanClose.Never

    type CloseAction = LibClient.Components.Dialog.Base.CloseAction
    let OnEscape      = CloseAction.OnEscape
    let OnBackground  = CloseAction.OnBackground
    let OnCloseButton = CloseAction.OnCloseButton

namespace LibClient.Components.Dialog.Shell

module FullScreen =
    open Fable.React
    open LibClient

    type BackButton =
    | No
    | Yes of OnPress: (ReactEvent.Action -> unit)
    with
        member this.OnPressOption : Option<ReactEvent.Action -> unit> =
            match this with
            | No          -> None
            | Yes onPress -> Some onPress

    type Scroll = NoScroll | Horizontal | Vertical | Both
