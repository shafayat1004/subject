[<AutoOpen>]
module ThirdParty.Map.Components.Native.Map

open Fable.React
open LibClient
open LibClient.Components
open Rn.Components
open Rn.Styles
open ThirdParty.Map.Types
open LibClient.Services.ImageService
open LibClient.LocalImages
open ThirdParty.Map.Components.Constructors
open ThirdParty.Map.Components

let private defaultLatLng = (23.793932, 90.411814)

let private getCoordinate (marker: Marker) : LatLng =
    match marker.Position with
    | MarkerPosition.LatLng latlng -> latlng
    | MarkerPosition.Centered      -> defaultLatLng

let private getMarkerImage (marker: Marker) : ImageSource =
    match marker.Image with
    | Some markerImage ->
        match markerImage with
        | MarkerImage.Icon iconMarker     -> localImage iconMarker.Url
        | MarkerImage.Symbol symbolMarker -> localImage symbolMarker.Path
    | None -> localImage "/libs/ThirdParty/Map/images/marker.png"

module private Styles =
    let view =
        makeViewStyles {
            flex 1
            trbl 0 0 0 0
            Position.Relative
            JustifyContent.Center
        }

    let fullScreen =
        makeViewStyles {
            trbl 0 0 0 0
            Position.Absolute
            JustifyContent.Center
        }

    let image =
        makeViewStyles {
            height 40
            AlignItems.Center
            JustifyContent.Center
        }

type ThirdParty.Map.Components.Constructors.Map.Native with
    [<Component>]
    static member Map(
            apiKey:     string,
            ?value:     LatLng,
            ?zoom:      int,
            ?onChange:  Option<LatLng> -> unit,
            ?fullScreen: bool,
            ?markers:   List<Marker>,
            ?shapes:    List<Shape>,
            ?ref:       (IRefReactNativeMapView -> unit),
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:       string
        ) : ReactElement =
        ignore apiKey
        ignore key

        let fullScreen = defaultArg fullScreen false
        let onChange = defaultArg onChange (fun _ -> ())

        let legacyViewStyles =
            match xLegacyStyles with
            | Some legacyStyles ->
                match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles legacyStyles with
                | []     -> [||]
                | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
            | None -> [||]

        let viewStyles =
            if fullScreen then [| Styles.fullScreen; yield! legacyViewStyles |]
            else [| Styles.view; yield! legacyViewStyles |]

        let prevValueRef = Hooks.useRef value
        Hooks.useEffect(
            (fun () ->
                if prevValueRef.current <> value then
                    onChange value
                prevValueRef.current <- value
            ),
            [| box value |]
        )

        LC.With.Layout(
            ``with`` =
                (fun (onLayoutOption, maybeLayout) ->
                    Rn.View(
                        ?onLayout = onLayoutOption,
                        styles = viewStyles,
                        children =
                            [|
                                Map.Native.ReactNativeMaps(
                                    size = (maybeLayout |> Option.map (fun l -> l.Width, l.Height)),
                                    zoom = zoom,
                                    value = (match value with Some latlng -> latlng | None -> defaultLatLng),
                                    onChange = onChange,
                                    ref = (defaultArg ref (fun _ -> ())),
                                    children =
                                        [|
                                            yield!
                                                markers
                                                |> Option.defaultValue []
                                                |> List.collect (fun marker ->
                                                    match marker.Position with
                                                    | MarkerPosition.LatLng _ ->
                                                        markers
                                                        |> Option.defaultValue []
                                                        |> List.map (fun m ->
                                                            Map.Native.Marker(
                                                                coordinate = getCoordinate m,
                                                                draggable = false,
                                                                image = getMarkerImage m
                                                            )
                                                        )
                                                    | _ -> []
                                                )
                                            yield!
                                                shapes
                                                |> Option.defaultValue []
                                                |> List.map (fun shape ->
                                                    match shape with
                                                    | Shape.Polyline polyline ->
                                                        Map.Native.Polyline(value = polyline)
                                                    | Shape.Circle circle ->
                                                        Map.Native.Circle(circle = circle)
                                                    | Shape.Polygon _ ->
                                                        noElement
                                                )
                                        |]
                                )
                                yield!
                                    markers
                                    |> Option.defaultValue []
                                    |> List.choose (fun marker ->
                                        match marker.Position with
                                        | MarkerPosition.Centered ->
                                            Some (
                                                Rn.Image(
                                                    styles = [| Styles.image |],
                                                    source = localImage "/libs/ThirdParty/Map/images/marker.png",
                                                    size = Image.FromStyles
                                                )
                                            )
                                        | _ -> None
                                    )
                            |]
                    )
                )
        )
