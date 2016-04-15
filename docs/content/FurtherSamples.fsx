(*** hide ***)
#I "../../bin"
(** 
# F# Charting: Further Samples

This is a collection of additional samples for F# Charting. 

The samples are not yet individually documented but may be useful to try.

*)

// On Mac OSX use FSharp.Charting.Gtk.fsx
#I "packages/FSharp.Charting"
#load "FSharp.Charting.fsx"

open FSharp.Charting
open System
open System.Drawing

let data = [ for x in 0 .. 99 -> (x,x*x) ]
let data2 = [ for x in 0 .. 99 -> (x,sin(float x / 10.0)) ]
let data3 = [ for x in 0 .. 99 -> (x,cos(float x / 10.0)) ]
let timeSeriesData = 
  [ for x in 0 .. 99 -> (DateTime.Now.AddDays (float x),sin(float x / 10.0)) ]

let rnd = new System.Random()
let rand() = rnd.NextDouble()
let pointsWithSizes = 
  [ for i in 0 .. 30 -> (rand() * 10.0, rand() * 10.0, rand() / 100.0) ]
let pointsWithSizes2 = 
  [ for i in 0 .. 10 -> (rand() * 10.0, rand() * 10.0, rand() / 100.0) ]

let timeHighLowOpenClose = 
    [ for i in 0 .. 10 ->
         let mid = rand() * 10.0 
         (DateTime.Now.AddDays (float i), mid + 0.5, mid - 0.5, mid + 0.25, mid - 0.25) ]
let timedPointsWithSizes = 
    [ for i in 0 .. 30 -> (DateTime.Now.AddDays(rand() * 10.0), rand() * 10.0, rand() / 100.0) ]

Chart.Line(data).WithXAxis(MajorGrid=ChartTypes.Grid(Enabled=false))

Chart.Line [ DateTime.Now, 1; DateTime.Now.AddDays(1.0), 10 ]
Chart.Line [ for h in 1 .. 50 -> DateTime.Now.AddHours(float h), sqrt (float h) ]
Chart.Line [ for h in 1 .. 50 -> DateTime.Now.AddMinutes(float h), sqrt (float h) ]

Chart.Line(data,Title="Test Title")
Chart.Line(data,Title="Test Title").WithTitle(InsideArea=false)
Chart.Line(data,Title="Test Title").WithTitle(InsideArea=true)

Chart.Line(data,Title="Test Title")
   |> Chart.WithTitle(InsideArea=true)

Chart.Line(data,Name="Test Data")
   |> Chart.WithXAxis(Enabled=true,Title="X Axis")

Chart.Line(data,Name="Test Data") 
   |> Chart.WithXAxis(Enabled=false,Title="X Axis")

Chart.Line(data,Name="Test Data")
  .WithXAxis(Enabled=false,Title="X Axis")

Chart.Line(data,Name="Test Data")
  .WithXAxis(Enabled=true,Title="X Axis",Max=10.0, Min=0.0)
  .WithYAxis(Max=100.0,Min=0.0)

Chart.Line(data,Name="Test Data").WithLegend(Title="Hello")
Chart.Line(data,Name="Test Data").WithLegend(Title="Hello",Enabled=false)

Chart.Line(data,Name="Test Data").With3D()

// TODO: x/y axis labels are a bit small by default
Chart.Line(data,Name="Test Data",XTitle="hello", YTitle="goodbye")

Chart.Line(data,Name="Test Data").WithXAxis(Title="XXX")
Chart.Line(data,Name="Test Data").WithXAxis(Title="XXX",Max=10.0,Min=4.0)
                                 .WithYAxis(Title="YYY",Max=100.0,Min=4.0,Log=true)


Chart.Combine [ Chart.Line(data,Name="Test Data 1 With Long Name")
                Chart.Line(data2,Name="Test Data 2") ]                 
   |> Chart.WithLegend(Enabled=true,Title="Hello",Docking=ChartTypes.Docking.Left)

Chart.Combine [ Chart.Line(data,Name="Test Data 1")
                Chart.Line(data2,Name="Test Data 2") ]                 
   |> Chart.WithLegend(Docking=ChartTypes.Docking.Left, InsideArea=true)
   
Chart.Combine [ Chart.Line(data,Name="Test Data 1")
                Chart.Line(data2,Name="Test Data 2") ]                 
   |> Chart.WithLegend(InsideArea=true)
   

Chart.Rows 
     [ Chart.Line(data,Title="Chart 1", Name="Test Data 1")
       Chart.Line(data2,Title="Chart 2", Name="Test Data 2") ]                 
   |> Chart.WithLegend(Title="Hello",Docking=ChartTypes.Docking.Left)

 // TODO: this title and docking left doesn't work
Chart.Columns
     [ Chart.Line(data,Name="Test Data 1")
       Chart.Line(data2,Name="Test Data 2")]
   |> Chart.WithLegend(Title="Hello",Docking=ChartTypes.Docking.Left)


Chart.Combine [ Chart.Line(data,Name="Test Data 1")
                Chart.Line(data2,Name="Test Data 2") ]                 
   |> Chart.WithLegend(Title="Hello",Docking=ChartTypes.Docking.Bottom)

Chart.Line(data,Name="Test Data")
Chart.Line(data,Name="Test Data").WithLegend(Enabled=false)
Chart.Line(data,Name="Test Data").WithLegend(InsideArea=true)
Chart.Line(data,Name="Test Data").WithLegend(InsideArea=false)
Chart.Line(data).WithLegend().CopyAsBitmap()

Chart.Line(data)

Chart.Line(data,Name="Test Data").WithLegend(InsideArea=false)

Chart.Area(data)
Chart.Area(timeSeriesData)
Chart.Line(data)
Chart.Bar(data)
Chart.Bar(timeSeriesData)


Chart.Spline(data)
Chart.Spline(timeSeriesData)


Chart.Bubble(pointsWithSizes)
Chart.Bubble(pointsWithSizes)
  .WithMarkers(Style=ChartTypes.MarkerStyle.Star10)
Chart.Bubble(pointsWithSizes)
  .WithMarkers(Style=ChartTypes.MarkerStyle.Diamond)
Chart.Bubble(pointsWithSizes)
  .WithMarkers(Style=ChartTypes.MarkerStyle.Cross,Color=Color.Red)
Chart.Bubble(pointsWithSizes)
  .WithMarkers(Style=ChartTypes.MarkerStyle.Cross,Color=Color.Red,MaxPixelPointWidth=3)
Chart.Bubble(pointsWithSizes)
  .WithMarkers(Style=ChartTypes.MarkerStyle.Cross,Size=3)
Chart.Bubble(pointsWithSizes)
  .WithMarkers(Style=ChartTypes.MarkerStyle.Cross,PointWidth=0.1)
Chart.Bubble(pointsWithSizes)
  .WithMarkers(Style=ChartTypes.MarkerStyle.Cross,PixelPointWidth=3)

Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Circle)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Square)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Star6)

Chart.Combine [ Chart.Bubble(pointsWithSizes,UseSizeForLabel=true) .WithMarkers(Style=ChartTypes.MarkerStyle.Circle)
                Chart.Bubble(pointsWithSizes2).WithMarkers(Style=ChartTypes.MarkerStyle.Star10) ]

Chart.Bubble(timedPointsWithSizes)

Chart.Candlestick(timeHighLowOpenClose)

Chart.Column(data)
Chart.Column(timeSeriesData)
Chart.Pie(Name="Pie", data=[ for i in 0 .. 10 -> i, i*i ])
Chart.Pie(Name="Pie", data=timeSeriesData)
Chart.Doughnut(data=[ for i in 0 .. 10 -> i, i*i ])
Chart.Doughnut(timeSeriesData)
Chart.FastPoint [ for x in 1 .. 10000 -> (rand(), rand()) ]
Chart.FastPoint timeSeriesData

Chart.Polar ([ for x in 1 .. 100 -> (360.0*rand(), rand()) ] |> Seq.sortBy fst)
Chart.Pyramid ([ for x in 1 .. 100 -> (360.0*rand(), rand()) ] |> Seq.sortBy fst)
Chart.Radar ([ for x in 1 .. 100 -> (360.0*rand(), rand()) ] |> Seq.sortBy fst)
Chart.Range ([ for x in 1.0 .. 10.0 -> (x, x + rand(), x-rand()) ])
Chart.RangeBar ([ for x in 1.0 .. 10.0 -> (x, x + rand(), x-rand()) ])
Chart.RangeColumn ([ for x in 1.0 .. 10.0 -> (x, x + rand(), x-rand()) ])
Chart.SplineArea ([ for x in 1.0 .. 10.0 -> (x, x + rand()) ])
Chart.SplineRange ([ for x in 1.0 .. 10.0 -> (x, x + rand(), x - rand()) ])
Chart.StackedBar ([ [ for x in 1.0 .. 10.0 -> (x, x + rand()) ]; 
                    [ for x in 1.0 .. 10.0 -> (x, x + rand()) ] ])
Chart.StackedColumn ([ [ for x in 1.0 .. 10.0 -> (x, x + rand()) ]; 
                       [ for x in 1.0 .. 10.0 -> (x, x + rand()) ] ])

Chart.StackedArea ([ [ for x in 1.0 .. 10.0 -> (x, x + rand()) ]; 
                     [ for x in 1.0 .. 10.0 -> (x, x + rand()) ] ])

Chart.StackedArea ([ [ for x in 1.0 .. 10.0 -> (DateTime.Now.AddDays x, x + rand()) ]; 
                     [ for x in 1.0 .. 10.0 -> (DateTime.Now.AddDays x, x + rand()) ] ])

Chart.StepLine(data,Name="Test Data").WithLegend(InsideArea=false)
Chart.StepLine(timeSeriesData,Name="Test Data").WithLegend(InsideArea=false)
Chart.Line(data,Name="SomeData").WithDataPointLabels(PointToolTip="Hello, I am #SERIESNAME") 

Chart.Stock(timeHighLowOpenClose)
Chart.ThreeLineBreak(data,Name="SomeData").WithDataPointLabels(PointToolTip="Hello, I am #SERIESNAME") 

Chart.Histogram([for x in 1 .. 100 -> rand()*10.],LowerBound=0.,UpperBound=10.,Intervals=10.)

// Example of .ApplyToChart() used to alter the settings on the window chart and to access the chart child objects.
// This can normally be done manually, in the chart property grid (right click the chart, then "Show Property Grid"). 
// This is useful when you want to try out carious settings first. But once you know what you want, .ApplyToChart() 
// allows programmatic access to the window properties. The two examples below are: IsUserSelectionEnabled essentially 
// allows zooming in and out along the given axes, and the longer fiddly example below does the same work as .WithDataPointLabels() 
// but across all series objects.
[ Chart.Column(data); 
  Chart.Column(data2) |> Chart.WithSeries.AxisType( YAxisType = Windows.Forms.DataVisualization.Charting.AxisType.Secondary ) ]
|> Chart.Combine
|> fun c -> c.WithLegend()
             .ApplyToChart( fun c -> c.ChartAreas.[0].CursorX.IsUserSelectionEnabled <- true )
             .ApplyToChart( fun c -> let _ = [0 .. c.Series.Count-1] |> List.map ( fun s -> c.Series.[ s ].ToolTip <- "#SERIESNAME (#VALX, #VAL{0:00000})" ) in () )