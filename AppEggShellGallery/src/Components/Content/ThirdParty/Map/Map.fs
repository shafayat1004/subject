[<AutoOpen>]
module AppEggShellGallery.Components.Content_ThirdParty_Map

open Fable.React
open LibClient
open LibClient.Components
open ThirdParty.Map
open ThirdParty.Map.Components
open AppEggShellGallery.Config

module B = ThirdParty.Map.TypesBuilders
open ReactXP.Components
open ReactXP.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let map = makeViewStyles { height 400 }

let private customStyles =
    [
        {
            FeatureType = MapFeatureType.Landscape
            ElementType = MapElementType.All
            Stylers = [| MapStyler.Color "#d0d3e7" |]
        }
        {
            FeatureType = MapFeatureType.PointsOfInterest
            ElementType = MapElementType.GeometryFill
            Stylers = [| MapStyler.Color "#c6c9cd" |]
        }
        {
            FeatureType = MapFeatureType.PointsOfInterest
            ElementType = MapElementType.LabelsIcon
            Stylers = [| MapStyler.Color "#9298a0" |]
        }
        {
            FeatureType = MapFeatureType.PointsOfInterest
            ElementType = MapElementType.LabelsTextFill
            Stylers = [| MapStyler.Color "#6c717a" |]
        }
        {
            FeatureType = MapFeatureType.PointsOfInterestBusiness
            ElementType = MapElementType.GeometryFill
            Stylers = [| MapStyler.Color "#c6c9cd" |]
        }
        {
            FeatureType = MapFeatureType.Road
            ElementType = MapElementType.All
            Stylers = [| MapStyler.Color "#fafafa" |]
        }
        {
            FeatureType = MapFeatureType.Transit
            ElementType = MapElementType.GeometryFill
            Stylers = [| MapStyler.Color "#9298a0" |]
        }
        {
            FeatureType = MapFeatureType.Transit
            ElementType = MapElementType.LabelsIcon
            Stylers = [| MapStyler.Color "#b0b4ba" |]
        }
        {
            FeatureType = MapFeatureType.Water
            ElementType = MapElementType.All
            Stylers = [| MapStyler.Color "#9cc1f2" |]
        }
    ]

let private getMarkerWithInfoWindow () =
    B.Marker.init MarkerPosition.Centered
    |> B.Marker.withInfoWindow (
        B.InfoWindow.init (fun handle -> Ui.Content.ThirdParty.ToolWindowContent(handle))
    )

type private Helpers =
    [<Component>]
    static member PositionSample () : ReactElement =
        let values =
            Hooks.useState (
                Map.ofList [("a", MapPosition.LatLng (34.8524359, 135.4587936))]
            )

        element {
            ThirdParty.Map.Components.Constructors.Map.Base(
                current().GoogleMapsApiKey,
                position =
                    (values.current.TryFind "a"
                     |> Option.defaultValue (MapPosition.LatLng (0, 0))),
                onPositionChanged = (fun position ->
                    values.update (values.current.AddOrUpdate ("a", position))),
                styles = [| Styles.map |]
            )

            RX.View(children = [| LC.Text (sprintf "Position: %A" (values.current.TryFind "a")) |])
        }

type Ui.Content.ThirdParty with
    [<Component>]
    static member Map () : ReactElement =
        Ui.ComponentContent(
            displayName = "Map",
            props = ComponentContent.ForFullyQualifiedName "ThirdParty.Map.Components.Base",
            a11y =
                Ui.A11yPanel(
                    componentName = "Map",
                    role = "none (interactive map)",
                    namePattern = "Map markers and controls need accessibilityLabel; not fully wired",
                    stateNotes = "Pan/zoom gestures; provide text alternatives for map data",
                    scalesWithFont = false,
                    contrastNotes = "Map styling is visual; pair with textual location info",
                    deferredTags = ["[rnw-blocked] full map a11y"]
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Basics",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    styles = [| makeViewStyles { height 400 } |],
    apiKey = Config.current().GoogleMapsApiKey
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals = Helpers.PositionSample(),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
let values = Hooks.useState (Map.ofList [("a", MapPosition.LatLng (34.8524359, 135.4587936))])

ThirdParty.Map.Components.Constructors.Map.Base(
    styles = [| makeViewStyles { height 400 } |],
    apiKey = Config.current().GoogleMapsApiKey,
    position = values.current.TryFind "a" |> Option.defaultValue (MapPosition.LatLng (0, 0)),
    onPositionChanged = fun position -> values.update (values.current.AddOrUpdate ("a", position))
)
"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Markers",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            position = MapPosition.LatLng (-27.1687243, 152.9308381),
                                            markers =
                                                [
                                                    B.Marker.init MarkerPosition.Centered
                                                    |> B.Marker.withTooltip "Centered Marker"
                                                ]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    styles = [| makeViewStyles { height 400 } |],
    apiKey = Config.current().GoogleMapsApiKey,
    position = MapPosition.LatLng (-27.1687243, 152.9308381),
    markers = [
        Marker.init MarkerPosition.Centered
        |> Marker.withTooltip "Centered Marker"
    ]
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            markers =
                                                [
                                                    MarkerPosition.LatLng (-27.1687243, 152.9308381)
                                                    |> B.Marker.init
                                                    |> B.Marker.withLabel (B.MarkerLabel.init "A")
                                                    MarkerPosition.LatLng (-27.2, 153.0)
                                                    |> B.Marker.init
                                                    |> B.Marker.withLabel (B.MarkerLabel.init "B")
                                                ]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    apiKey = Config.current().GoogleMapsApiKey,
    markers = [
        MarkerPosition.LatLng (-27.1687243, 152.9308381)
        |> Marker.init |> Marker.withLabel (MarkerLabel.init "A")
        MarkerPosition.LatLng (-27.2, 153.0)
        |> Marker.init |> Marker.withLabel (MarkerLabel.init "B")
    ]
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            markers =
                                                [
                                                    MarkerPosition.LatLng (-27.1687243, 152.9308381)
                                                    |> B.Marker.init
                                                    |> B.Marker.withLabel (B.MarkerLabel.init "A")
                                                    |> B.Marker.withImage (
                                                        B.Icon.init "https://developers.google.com/maps/documentation/javascript/examples/full/images/beachflag.png"
                                                        |> B.Icon.withAnchor (0, 32)
                                                        |> Icon
                                                    )
                                                    MarkerPosition.LatLng (-27.2, 153.0)
                                                    |> B.Marker.init
                                                    |> B.Marker.withLabel (B.MarkerLabel.init "B")
                                                    |> B.Marker.withImage (
                                                        B.Symbol.init "M10.453 14.016l6.563-6.609-1.406-1.406-5.156 5.203-2.063-2.109-1.406 1.406zM12 2.016q2.906 0 4.945 2.039t2.039 4.945q0 1.453-0.727 3.328t-1.758 3.516-2.039 3.070-1.711 2.273l-0.75 0.797q-0.281-0.328-0.75-0.867t-1.688-2.156-2.133-3.141-1.664-3.445-0.75-3.375q0-2.906 2.039-4.945t4.945-2.039z"
                                                        |> B.Symbol.withAnchor (15, 30)
                                                        |> B.Symbol.withFillColor "blue"
                                                        |> B.Symbol.withFillOpacity 0.8
                                                        |> B.Symbol.withStrokeColor "red"
                                                        |> B.Symbol.withStrokeOpacity 0.5
                                                        |> Symbol
                                                    )
                                                ]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    apiKey = Config.current().GoogleMapsApiKey,
    markers = [ (* custom icon and symbol markers *) ]
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Shapes",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            position = MapPosition.LatLng (-27.1687243, 152.9308381),
                                            shapes =
                                                [
                                                    [ (-27.1687243, 152.9308381); (-27.1, 153.1); (-27.1, 152.8) ]
                                                    |> B.Polyline.init
                                                    |> B.Polyline.withStrokeColor "#1000CF"
                                                    |> B.Polyline.withStrokeWeight 2
                                                    |> B.Polyline.withStrokeOpacity 0.75
                                                    |> Polyline
                                                ]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    position = MapPosition.LatLng (-27.1687243, 152.9308381),
    shapes = [
        [ (-27.1687243, 152.9308381); (-27.1, 153.1); (-27.1, 152.8) ]
        |> Polyline.init
        |> Polyline.withStrokeColor "#1000CF"
        |> Polyline.withStrokeWeight 2
        |> Polyline.withStrokeOpacity 0.75
        |> Shape.Polyline
    ]
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            position = MapPosition.LatLng (-27.1687243, 152.9308381),
                                            shapes =
                                                [
                                                    [ seq { (-27.1687243, 152.9308381); (-27.1, 153.1); (-27.1, 152.8) } ]
                                                    |> B.Polygon.init
                                                    |> B.Polygon.withStrokeColor "#1000CF"
                                                    |> B.Polygon.withStrokeWeight 2
                                                    |> B.Polygon.withStrokeOpacity 0.75
                                                    |> Polygon
                                                ]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    position = MapPosition.LatLng (-27.1687243, 152.9308381),
    shapes = [
        [ seq { (-27.1687243, 152.9308381); (-27.1, 153.1); (-27.1, 152.8) } ]
        |> Polygon.init
        |> Polygon.withStrokeColor "#1000CF"
        |> Shape.Polygon
    ]
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            position = MapPosition.LatLng (-27.1687243, 152.9308381),
                                            shapes =
                                                [
                                                    B.Circle.init (-27.1687243, 152.9308381) 500
                                                    |> B.Circle.withFillColor "#401020"
                                                    |> B.Circle.withFillOpacity 0.3
                                                    |> B.Circle.withStrokeColor "#1000CF"
                                                    |> B.Circle.withStrokeWeight 2
                                                    |> B.Circle.withStrokeOpacity 0.75
                                                    |> B.Circle.withEditable true
                                                    |> Circle
                                                ]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    position = MapPosition.LatLng (-27.1687243, 152.9308381),
    shapes = [
        Circle.init (CircleCenter.LatLng (-27.1687243, 152.9308381)) 500
        |> Circle.withFillColor "#401020"
        |> Circle.withEditable true
        |> Shape.Circle
    ]
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Info Windows",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            position = MapPosition.LatLng (-27.1687243, 152.9308381),
                                            markers = [ getMarkerWithInfoWindow () ]
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    position = MapPosition.LatLng (-27.1687243, 152.9308381),
    markers = [ getMarkerWithInfoWindow () ]
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Custom Styles",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        ThirdParty.Map.Components.Constructors.Map.Base(
                                            current().GoogleMapsApiKey,
                                            styles = [| Styles.map |],
                                            position = MapPosition.LatLng (-27.1687243, 152.9308381),
                                            mapStyles = customStyles
                                        ),
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
ThirdParty.Map.Components.Constructors.Map.Base(
    position = MapPosition.LatLng (-27.1687243, 152.9308381),
    mapStyles = customStyles
)"""
                                        )
                                )
                            }
                    )
                }
        )
