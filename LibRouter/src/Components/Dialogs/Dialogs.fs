[<AutoOpen>]
module LibRouter.Components.Dialogs

open Fable.React
open LibClient
open LibClient.Dialogs
open LibClient.SystemDialogs
open LibRouter.RoutesSpec
open LibRouter.Components.With.Navigation
open LibRouter.Components.Constructors
open LibClient.Components
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

    let frame =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
            backgroundColor (Color.BlackAlpha 0.3)
        }

    let sentinel =
        makeViewStyles {
            Position.Absolute
            trbl 0 0 0 0
        }

type LR with
    [<Component>]
    static member Dialogs (
        nav:            Navigation<'Route, 'ResultlessDialog, 'ResultfulDialog>,
        dialogs:        List<NavigationDialog<'ResultlessDialog>>,
        dialogsState:   DialogsState<'ResultfulDialog>,
        makeResultless: ('ResultlessDialog * (DialogCloseMethod -> ReactEvent.Action -> unit)) -> ReactElement,
        makeResultful:  ('ResultfulDialog * (DialogCloseMethod -> ReactEvent.Action -> unit)) -> ReactElement,
        ?key:           string)
        : ReactElement =
        ignore key

        Hooks.useEffectDisposableFn(
            (fun () ->
                AdHoc.provideGoImplementation (fun closeToDialog ->
                    nav.Go closeToDialog
                )
            ),
            (fun () -> ()),
            [| box nav |]
        )

        if dialogs.IsNonempty then
            RX.View(
                styles = [| Styles.view |],
                children =
                    elements {
                        // Sentinel keeps React continuity when dialog count changes.
                        RX.View(
                            styles = [| Styles.sentinel |]
                        )

                        (List.rev dialogs)
                        |> List.map (fun dialog ->
                            RX.View(
                                styles = [| Styles.frame |],
                                children =
                                    [|
                                        match dialog with
                                        | NavigationDialog.Resultless (token, resultlessDialog) ->
                                            makeResultless (resultlessDialog, nav.Close token)
                                        | NavigationDialog.Resultful token ->
                                            match dialogsState.TryGetResultful token with
                                            | Some resultfulDialog ->
                                                makeResultful (resultfulDialog, nav.Close token)
                                            | None -> noElement
                                        | NavigationDialog.AdHoc token ->
                                            match dialogsState.TryGetAdHoc token with
                                            | Some adHocCloseToDialog ->
                                                adHocCloseToDialog (nav.Close token)
                                            | None -> noElement
                                        | NavigationDialog.System token ->
                                            match dialogsState.TryGetSystem token with
                                            | Some systemDialog ->
                                                let close = nav.Close token
                                                match systemDialog with
                                                | Alert (maybeHeading, details) ->
                                                    LibClient.Components.Dialog.Confirm.OpenAsAlert maybeHeading details close
                                                | ImageViewer (sources, initialIndex) ->
                                                    LC.Dialog.OpenImageViewer(sources, close, initialIndex)
                                                | ImageViewerCustom (sources, initialIndex, resizeMode) ->
                                                    LC.Dialog.OpenImageViewer(sources, close, initialIndex, resizeMode)
                                                | ConfirmCustom (maybeHeading, details, buttons) ->
                                                    LibClient.Components.Dialog.Confirm.Open maybeHeading details buttons close
                                                | Confirm (maybeHeading, details, cancelLabel, okLabel, onResult) ->
                                                    LibClient.Components.Dialog.Confirm.OpenSimple maybeHeading details cancelLabel okLabel onResult close
                                                | ConfirmAsync (maybeHeading, details, cancelLabel, okLabel, onConfirm) ->
                                                    LibClient.Components.Dialog.Confirm.OpenSimpleAsync maybeHeading details cancelLabel okLabel onConfirm close
                                                | Prompt (maybeHeading, details, onResult) ->
                                                    LibClient.Components.Dialog.Prompt.Open maybeHeading details onResult close
                                            | None -> noElement
                                    |]
                            )
                        )
                        |> List.toArray
                        |> castAsElement
                    }
            )
        else
            noElement
