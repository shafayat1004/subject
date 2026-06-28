[<AutoOpen>]
module AppTodo.Components.App

open Fable.React
open LibRouter.RoutesSpec
open LibClient
open LibClient.Components
open LibRouter.Components
open AppTodo.Navigation
open AppTodo.Components

type Ui.App with
    [<Component>]
    static member Root () : ReactElement =
        element {
            LR.LogRouteTransitions()
            LC.SetPageMetadata(title = "Todo")

            let maybeNavigationFrame = LR.UseRoute (routesSpec())

            LC.AppShell.Content(
                desktopSidebarStyle = AppShell_Content.DesktopSidebarStyle.Fixed,
                sidebar = noElement,
                topNav = noElement,
                dialogs =
                    LR.Dialogs(
                        nav = nav,
                        dialogsState = navigationState.DialogsState,
                        dialogs =
                            (maybeNavigationFrame
                             |> Option.map NavigationFrame.dialogs
                             |> Option.getOrElse []),
                        makeResultless = (fun _ -> noElement),
                        makeResultful = (fun _ -> noElement)
                    ),
                onError = LC.AppShell.TopLevelErrorMessage,
                content =
                    match maybeNavigationFrame |> Option.map NavigationFrame.route with
                    | None -> LC.InfoMessage(level = InfoMessage.Level.Attention, message = "Route Not Found")
                    | Some Todos -> Ui.Route.Todos()
            )
        }
