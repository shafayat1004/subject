[<AutoOpen>]
module LibClient.Components.AppShell_TopLevelErrorMessage

open Fable.React
open Fable.Core
open LibClient
open LibClient.Icons
open LibClient.Responsive
open Rn.Components
open Rn.Styles

[<Emit("window.location.reload()")>]
let private jsWindowLocationReload () : unit = jsNative

[<RequireQualifiedAccess>]
module private Styles =
    let view =
        makeViewStyles {
            flex 1
            padding 80
            FlexDirection.Column
            AlignItems.Center
            JustifyContent.Center
            backgroundColor Color.White
        }

    let center = makeViewStyles { AlignItems.Center }

    let minHeightOverride =
        ViewStyles.Memoize(fun (minH: int) -> makeViewStyles { minHeight minH })

    let icon =
        TextStyles.Memoize(fun (screenSize: ScreenSize) ->
            makeTextStyles {
                match screenSize with
                | ScreenSize.Desktop -> fontSize 320
                | ScreenSize.Handheld ->
                    fontSize 220
                    marginTop -100
            })

    let errorHeading =
        makeTextStyles {
            marginTop 20
            color (Color.Hex "#db5f5f")
            FontWeight.W400
        }

    let errorTitle =
        makeTextStyles {
            marginTop 15
            TextAlign.Center
        }

    let errorSubtitle =
        makeTextStyles {
            marginTop 10
            marginBottom 20
            TextAlign.Center
        }

    let button = makeViewStyles { marginBottom 50 }

type LibClient.Components.Constructors.LC.AppShell with
    [<Component>]
    static member TopLevelErrorMessage
        (error: System.Exception, retry: unit -> unit, ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>, ?key: string) : ReactElement =
        key |> ignore
        xLegacyStyles |> ignore

        let reload _ =
            if Rn.Runtime.isWeb () then
                jsWindowLocationReload ()
            else
                retry ()

        let genericError (screenSize: ScreenSize) =
            castAsElement
                [| LC.Icon(icon = Icon.Error, styles = [| Styles.icon screenSize |])
                   LC.Heading(children = [| LC.UiText "Oops!" |], styles = [| Styles.errorHeading |])
                   LC.Heading(
                       level = Heading.Secondary,
                       children = [| LC.UiText "Something went wrong" |],
                       styles = [| Styles.errorTitle |]
                   )
                   LC.Heading(
                       level = Heading.Tertiary,
                       children = [| LC.UiText "Please try again later." |],
                       styles = [| Styles.errorSubtitle |]
                   ) |]

        LC.With.ScreenSize(fun screenSize ->
            LC.With.Layout(fun (onLayoutOption, maybeLayout) ->
                let viewStyles =
                    match maybeLayout with
                    | Some layout -> [| Styles.view; Styles.minHeightOverride layout.Height |]
                    | None -> [| Styles.view |]

                Rn.View(
                    ?onLayout = onLayoutOption,
                    styles = viewStyles,
                    children =
                        [| Rn.ScrollView(
                               vertical = true,
                               children =
                                   [| Rn.View(
                                          styles = [| Styles.center |],
                                          children =
                                              [| match error with
                                                 | AsyncDataException AsyncDataFailure.NetworkFailure ->
                                                     LC.Icon(
                                                         icon = Icon.NoNetwork,
                                                         styles = [| Styles.icon screenSize |]
                                                     )

                                                     LC.Heading(
                                                         children = [| LC.UiText "Internet Problem" |],
                                                         styles = [| Styles.errorHeading |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Secondary,
                                                         children =
                                                             [| LC.UiText "Unable to connect to the internet." |],
                                                         styles = [| Styles.errorTitle |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Tertiary,
                                                         children =
                                                             [| LC.UiText
                                                                    "Please make sure you are connected to the internet and reload." |],
                                                         styles = [| Styles.errorSubtitle |]
                                                     )
                                                 | AsyncDataException(AsyncDataFailure.RequestFailure(RequestFailure.ClientError(statusCode,
                                                                                                                                 response))) ->
                                                     LC.Icon(
                                                         icon = Icon.ServerError,
                                                         styles = [| Styles.icon screenSize |]
                                                     )

                                                     LC.Heading(
                                                         children = [| LC.UiText "Request Failed" |],
                                                         styles = [| Styles.errorHeading |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Secondary,
                                                         children = [| LC.UiText "App request failed!" |],
                                                         styles = [| Styles.errorTitle |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Tertiary,
                                                         children =
                                                             [| LC.UiText
                                                                    "Please try to reload. If the problem remains, please update the app to latest version." |],
                                                         styles = [| Styles.errorSubtitle |]
                                                     )

                                                     LC.InfoMessage(
                                                         message =
                                                             (statusCode.ToString() + " - " + response.ToString())
                                                     )
                                                 | AsyncDataException(AsyncDataFailure.RequestFailure(RequestFailure.ServerError(statusCode,
                                                                                                                                 response))) ->
                                                     LC.Icon(
                                                         icon = Icon.ServerError,
                                                         styles = [| Styles.icon screenSize |]
                                                     )

                                                     LC.Heading(
                                                         children = [| LC.UiText "Server Error" |],
                                                         styles = [| Styles.errorHeading |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Secondary,
                                                         children =
                                                             [| LC.UiText "There seems to be problem with our server!" |],
                                                         styles = [| Styles.errorTitle |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Tertiary,
                                                         children =
                                                             [| LC.UiText
                                                                    "Please try to reload. If the problem remains contact support" |],
                                                         styles = [| Styles.errorSubtitle |]
                                                     )

                                                     LC.InfoMessage(
                                                         message =
                                                             (statusCode.ToString() + " - " + response.ToString())
                                                     )
                                                 | AsyncDataException(AsyncDataFailure.RequestFailure(RequestFailure.Unknown(statusCode,
                                                                                                                             response))) ->
                                                     LC.Icon(
                                                         icon = Icon.ServerError,
                                                         styles = [| Styles.icon screenSize |]
                                                     )

                                                     LC.Heading(
                                                         children = [| LC.UiText "Unknown Server Error" |],
                                                         styles = [| Styles.errorHeading |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Secondary,
                                                         children =
                                                             [| LC.UiText "There seems to be problem with our server!" |],
                                                         styles = [| Styles.errorTitle |]
                                                     )

                                                     LC.Heading(
                                                         level = Heading.Tertiary,
                                                         children =
                                                             [| LC.UiText
                                                                    "Please try to reload. If the problem remains contact support" |],
                                                         styles = [| Styles.errorSubtitle |]
                                                     )

                                                     LC.InfoMessage(
                                                         message =
                                                             (statusCode.ToString() + " - " + response.ToString())
                                                     )
                                                 | AsyncDataException(AsyncDataFailure.UserReadableFailure message) ->
                                                     LC.Icon(icon = Icon.Error, styles = [| Styles.icon screenSize |])

                                                     LC.Heading(
                                                         level = Heading.Secondary,
                                                         children = [| LC.UiText message |],
                                                         styles = [| Styles.errorTitle |]
                                                     )
                                                 | _ -> genericError screenSize

                                                 LC.Button(
                                                     state =
                                                         Button.PropStateFactory.MakeLowLevel(
                                                             Button.Actionable reload
                                                         ),
                                                     level = Button.Level.Secondary,
                                                     label = "Reload",
                                                     styles = [| Styles.button |]
                                                 ) |]
                                      ) |]
                           ) |]
                )))
