// NOTE there's a lot more available in the ReactXP PopupOptions which we are
// not exposing here. We can add this functionality when needed.
[<AutoOpen>]
module LibClient.Components.Popup

open System

open Fable.Core
open Fable.Core.JsInterop
open Fable.React

open LibClient

type ConnectorInternalCallbacks = {
    Show:    ReactElement -> unit
    Hide:    unit -> unit
    IsShown: unit -> bool
}

type Connector() =
    let mutable maybeCallbacks: Option<ConnectorInternalCallbacks> = None
    let mutable onDismissCallbacks: List<unit -> unit> = []

    member _.SetCallbacks(callbacks: ConnectorInternalCallbacks) : unit =
        maybeCallbacks <- Some callbacks

    member _.ClearCallbacks () : unit =
        maybeCallbacks <- None

    member _.Show(anchor: ReactElement) : unit =
        maybeCallbacks |> Option.sideEffect (fun callbacks -> callbacks.Show anchor)

    member _.Hide() : unit =
        maybeCallbacks |> Option.sideEffect (fun callbacks -> callbacks.Hide())

    member _.IsShown() : bool =
        maybeCallbacks
        |> Option.map (fun callbacks -> callbacks.IsShown())
        // pretty safe to say it's not shown if the callbacks are not set
        |> Option.getOrElse false

    member _.OnDismiss (fn: unit -> unit) : unit =
        onDismissCallbacks <- fn :: onDismissCallbacks

    member _.CallOnDismissCallbacks () : unit =
        onDismissCallbacks |> List.iter (fun fn -> fn ())

[<StringEnum>]
type Position =
| Top
| Right
| Bottom
| Left
| Context

module private Styles =
    // This component renders noElement — no styles needed.
    ()

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Popup(
            render:    unit -> ReactElement,
            connector: Connector,
            ?id:       string,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:      string
        ) : ReactElement =
        key           |> ignore
        xLegacyStyles |> ignore

        // Stable popup id — computed once on first render and held in a ref.
        let popupIdRef = Hooks.useRef (id |> Option.getOrElse (sprintf "popup-%i" (Random().Next())))

        Hooks.useEffectDisposable (
            (fun () ->
                let popupId = popupIdRef.current

                connector.SetCallbacks
                    {
                        Show = (fun (anchor: ReactElement) ->
                            let options =
                                ReactXP.Popup.popupShowOptions
                                    (fun () -> anchor :> obj)
                                    (fun (_anchorPosition: obj) (_anchorOffset: int) (_popupWidth: int) (_popupHeight: int) ->
                                        render ())
                                    (fun () -> connector.CallOnDismissCallbacks ())
                            ReactXP.Popup.show(options, popupId)
                        )
                        Hide = (fun () ->
                            ReactXP.Popup.dismiss(popupId)
                        )
                        IsShown = (fun () ->
                            ReactXP.Popup.isDisplayed(popupId)
                        )
                    }

                { new IDisposable with
                    member _.Dispose() =
                        connector.ClearCallbacks ()
                }
            ),
            [||] // run only once — on mount; dispose clears callbacks on unmount
        )

        noElement
