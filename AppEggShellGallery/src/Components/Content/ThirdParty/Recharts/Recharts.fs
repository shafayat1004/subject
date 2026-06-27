[<AutoOpen>]
module AppEggShellGallery.Components.Content_ThirdParty_Recharts

open Fable.React
open LibClient
open LibClient.Components
open ThirdParty.Recharts.Components

let private minChartWidth = 200
let private minChartHeight = 300

type private ChartData = {
    Name:   string
    Value1: float
    Value2: float
    Fill:   Color
}

let private dataSet1 =
    [|
        { Name = "Group A"; Value1 = 42.0; Value2 = 30.5; Fill = Color.Rgb (255, 0, 0) }
        { Name = "Group B"; Value1 = 69.0; Value2 = 88.2; Fill = Color.Rgb (0, 255, 0) }
        { Name = "Group C"; Value1 = 12.1; Value2 = 19.8; Fill = Color.Rgb (0, 0, 255) }
        { Name = "Group D"; Value1 = 133.01; Value2 = 48.75; Fill = Color.Rgb (255, 0, 255) }
    |]

#if EGGSHELL_PLATFORM_IS_WEB
module private WebCharts =
    let lineChartBasic =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.LineChart(
                    children = [| Recharts.Line(dataKey = "Value1") |],
                    data = (dataSet1 |> Array.map box)
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )

    let lineChartFull =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.LineChart(
                    children =
                        [|
                            Recharts.CartesianGrid(strokeDashArray = [| 3.; 3. |])
                            Recharts.XAxis(dataKey = "Name")
                            Recharts.YAxis()
                            Recharts.Tooltip()
                            Recharts.Legend()
                            Recharts.Line(
                                ``type`` = Line.Type.Monotone,
                                dataKey = "Value1",
                                stroke = Line.InternalString "#8884d8"
                            )
                            Recharts.Line(
                                ``type`` = Line.Type.Monotone,
                                dataKey = "Value2",
                                stroke = Line.InternalString "#82ca9d"
                            )
                        |],
                    data = (dataSet1 |> Array.map box)
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )

    let areaChartBasic =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.AreaChart(
                    children = [| Recharts.Area(dataKey = "Value1") |],
                    data = (dataSet1 |> Array.map box)
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )

    let areaChartFull =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.AreaChart(
                    children =
                        [|
                            Recharts.CartesianGrid(strokeDashArray = [| 3.; 3. |])
                            Recharts.XAxis(dataKey = "Name")
                            Recharts.YAxis()
                            Recharts.Tooltip()
                            Recharts.Legend()
                            Recharts.Area(
                                ``type`` = Area.Type.Monotone,
                                dataKey = "Value1",
                                stroke = Area.InternalString "#8884d8",
                                fill = Area.InternalString "#8884d8"
                            )
                            Recharts.Area(
                                ``type`` = Area.Type.Monotone,
                                dataKey = "Value2",
                                stroke = Area.InternalString "#82ca9d",
                                fill = Area.InternalString "#82ca9d"
                            )
                        |],
                    data = (dataSet1 |> Array.map box)
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )

    let areaChartStacked =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.AreaChart(
                    children =
                        [|
                            Recharts.CartesianGrid(strokeDashArray = [| 3.; 3. |])
                            Recharts.XAxis(dataKey = "Name")
                            Recharts.YAxis()
                            Recharts.Tooltip()
                            Recharts.Legend()
                            Recharts.Area(
                                ``type`` = Area.Type.StepAfter,
                                stackId = Area.Number 1,
                                dataKey = "Value1",
                                stroke = Area.InternalString "#8884d8",
                                fill = Area.InternalString "#8884d8"
                            )
                            Recharts.Area(
                                ``type`` = Area.Type.StepAfter,
                                stackId = Area.Number 1,
                                dataKey = "Value2",
                                stroke = Area.InternalString "#82ca9d",
                                fill = Area.InternalString "#82ca9d"
                            )
                        |],
                    data = (dataSet1 |> Array.map box)
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )

    let pieChartBasic =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.PieChart(
                    children =
                        [|
                            Recharts.Pie(
                                nameKey = "Name",
                                dataKey = "Value1",
                                data = (dataSet1 |> Array.map box)
                            )
                        |]
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )

    let pieChartColored =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.PieChart(
                    children =
                        [|
                            Recharts.Pie(
                                children =
                                    (dataSet1
                                     |> Array.map (fun data -> Recharts.Cell(fill = data.Fill))),
                                nameKey = "Name",
                                dataKey = "Value1",
                                data = (dataSet1 |> Array.map box)
                            )
                        |]
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )

    let pieChartDouble =
        Recharts.ResponsiveContainer(
            children = [|
                Recharts.PieChart(
                    children =
                        [|
                            Recharts.Pie(
                                children =
                                    (dataSet1
                                     |> Array.map (fun data -> Recharts.Cell(fill = data.Fill, stroke = Color.White))),
                                nameKey = "Name",
                                dataKey = "Value1",
                                data = (dataSet1 |> Array.map box),
                                cx = Pie.Offset.Percentage 50.,
                                cy = Pie.Offset.Percentage 50.,
                                innerRadius = Pie.Radius.Number 30,
                                outerRadius = Pie.Radius.Number 50
                            )
                            Recharts.Pie(
                                children =
                                    (dataSet1
                                     |> Array.map (fun data -> Recharts.Cell(fill = data.Fill, stroke = Color.White))),
                                nameKey = "Name",
                                dataKey = "Value2",
                                data = (dataSet1 |> Array.map box),
                                cx = Pie.Offset.Percentage 50.,
                                cy = Pie.Offset.Percentage 50.,
                                innerRadius = Pie.Radius.Number 60,
                                outerRadius = Pie.Radius.Number 80
                            )
                            Recharts.Tooltip(isAnimationActive = true, animationEasing = Pie.EaseInOut)
                            Recharts.Legend()
                        |]
                )
            |],
            minWidth = minChartWidth,
            minHeight = minChartHeight
        )
#endif

type Ui.Content.ThirdParty with
    [<Component>]
    static member Recharts () : ReactElement =
        Ui.ComponentContent(
            displayName = "Recharts",
            props =
                ComponentContent.Manual(
                    element {
                        Ui.ScrapedComponentProps(heading = "CartesianGrid", fullyQualifiedName = "ThirdParty.Recharts.Components.CartesianGrid")
                        Ui.ScrapedComponentProps(heading = "Cell", fullyQualifiedName = "ThirdParty.Recharts.Components.Cell")
                        Ui.ScrapedComponentProps(heading = "Legend", fullyQualifiedName = "ThirdParty.Recharts.Components.Legend")
                        Ui.ScrapedComponentProps(heading = "Line", fullyQualifiedName = "ThirdParty.Recharts.Components.Line")
                        Ui.ScrapedComponentProps(heading = "LineChart", fullyQualifiedName = "ThirdParty.Recharts.Components.LineChart")
                        Ui.ScrapedComponentProps(heading = "Pie", fullyQualifiedName = "ThirdParty.Recharts.Components.Pie")
                        Ui.ScrapedComponentProps(heading = "PieChart", fullyQualifiedName = "ThirdParty.Recharts.Components.PieChart")
                        Ui.ScrapedComponentProps(heading = "ResponsiveContainer", fullyQualifiedName = "ThirdParty.Recharts.Components.ResponsiveContainer")
                        Ui.ScrapedComponentProps(heading = "Tooltip", fullyQualifiedName = "ThirdParty.Recharts.Components.Tooltip")
                        Ui.ScrapedComponentProps(heading = "XAxis", fullyQualifiedName = "ThirdParty.Recharts.Components.XAxis")
                        Ui.ScrapedComponentProps(heading = "YAxis", fullyQualifiedName = "ThirdParty.Recharts.Components.YAxis")
                    }
                ),
            samples =
                element {
                    Ui.ComponentSampleGroup(
                        heading = "Line Charts",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.lineChartBasic
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.ResponsiveContainer(minWidth = 200, minHeight = 300, children = [|
    Recharts.LineChart(data = dataSet1 |> Array.map box, children = [|
        Recharts.Line(dataKey = "Value1")
    |])
|])"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.lineChartFull
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.ResponsiveContainer(minWidth = 200, minHeight = 300, children = [|
    Recharts.LineChart(data = dataSet1 |> Array.map box, children = [|
        Recharts.CartesianGrid(strokeDashArray = [| 3.; 3. |])
        Recharts.XAxis(dataKey = "Name")
        Recharts.YAxis()
        Recharts.Tooltip()
        Recharts.Legend()
        Recharts.Line(``type`` = Line.Type.Monotone, dataKey = "Value1", stroke = Line.InternalString "#8884d8")
        Recharts.Line(``type`` = Line.Type.Monotone, dataKey = "Value2", stroke = Line.InternalString "#82ca9d")
    |])
|])"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Area Charts",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.areaChartBasic
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.ResponsiveContainer(minWidth = 200, minHeight = 300, children = [|
    Recharts.AreaChart(data = dataSet1 |> Array.map box, children = [|
        Recharts.Area(dataKey = "Value1")
    |])
|])"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.areaChartFull
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.ResponsiveContainer(minWidth = 200, minHeight = 300, children = [|
    Recharts.AreaChart(data = dataSet1 |> Array.map box, children = [|
        Recharts.CartesianGrid(strokeDashArray = [| 3.; 3. |])
        Recharts.XAxis(dataKey = "Name")
        Recharts.YAxis()
        Recharts.Tooltip()
        Recharts.Legend()
        Recharts.Area(``type`` = Area.Type.Monotone, dataKey = "Value1", stroke = Area.InternalString "#8884d8", fill = Area.InternalString "#8884d8")
        Recharts.Area(``type`` = Area.Type.Monotone, dataKey = "Value2", stroke = Area.InternalString "#82ca9d", fill = Area.InternalString "#82ca9d")
    |])
|])"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.areaChartStacked
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.Area(
    ``type`` = Area.Type.StepAfter,
    stackId = StackId.Number 1,
    dataKey = "Value1",
    stroke = Area.InternalString "#8884d8",
    fill = Area.InternalString "#8884d8"
)"""
                                        )
                                )
                            }
                    )

                    Ui.ComponentSampleGroup(
                        heading = "Pie Charts",
                        samples =
                            element {
                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.pieChartBasic
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.ResponsiveContainer(minWidth = 200, minHeight = 300, children = [|
    Recharts.PieChart(children = [|
        Recharts.Pie(data = dataSet1 |> Array.map box, nameKey = "Name", dataKey = "Value1")
    |])
|])"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.pieChartColored
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.Pie(
    data = dataSet1 |> Array.map box,
    nameKey = "Name",
    dataKey = "Value1",
    children = dataSet1 |> Array.map (fun data -> Recharts.Cell(fill = data.Fill))
)"""
                                        )
                                )

                                Ui.ComponentSample(
                                    visuals =
                                        #if EGGSHELL_PLATFORM_IS_WEB
                                        WebCharts.pieChartDouble
                                        #else
                                        LC.Text "Recharts samples are web-only."
                                        #endif
                                    ,
                                    code =
                                        ComponentSample.SingleBlock(
                                            ComponentSample.Fsharp,
                                            LC.Text """
Recharts.PieChart(children = [|
    Recharts.Pie(data = dataSet1 |> Array.map box, nameKey = "Name", dataKey = "Value1", ...)
    Recharts.Pie(data = dataSet1 |> Array.map box, nameKey = "Name", dataKey = "Value2", ...)
    Recharts.Tooltip(isAnimationActive = true, animationEasing = AnimationEasing.EaseInOut)
    Recharts.Legend()
|])"""
                                        )
                                )
                            }
                    )
                }
        )
