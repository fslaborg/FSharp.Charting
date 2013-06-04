(** 

This is a collection of additional samples for FSharp.Charting. 

The samples are not yet indivudally documented but may be useful to try.

*)

#load "../bin/FSharp.Charting.fsx"

open FSharp.Charting
open System
open System.Drawing

let data = [ for x in 0 .. 99 -> (x,x*x) ]
let data2 = [ for x in 0 .. 99 -> (x,sin(float x / 10.0)) ]
let data3 = [ for x in 0 .. 99 -> (x,cos(float x / 10.0)) ]
let timeSeriesData = [ for x in 0 .. 99 -> (DateTime.Now.AddDays (float x),sin(float x / 10.0)) ]
let rnd = new System.Random()
let rand() = rnd.NextDouble()
let pointsWithSizes = [ for i in 0 .. 30 -> (rand() * 10.0, rand() * 10.0, rand() / 100.0) ]
let pointsWithSizes2 = [ for i in 0 .. 10 -> (rand() * 10.0, rand() * 10.0, rand() / 100.0) ]

let timeHighLowOpenClose = 
    [ for i in 0 .. 10 ->
         let mid = rand() * 10.0 
         (DateTime.Now.AddDays (float i), mid + 0.5, mid - 0.5, mid + 0.25, mid - 0.25) ]
let timedPointsWithSizes = 
    [ for i in 0 .. 30 -> (DateTime.Now.AddDays(rand() * 10.0), rand() * 10.0, rand() / 100.0) ]

let prices =
  [ 26.24,25.80,26.22,25.95; 26.40,26.18,26.26,26.20
    26.37,26.04,26.11,26.08; 26.78,26.15,26.60,26.16
    26.86,26.51,26.69,26.58; 26.95,26.50,26.91,26.55
    27.06,26.50,26.64,26.77; 26.86,26.43,26.53,26.59
    27.10,26.52,26.78,26.59; 27.21,26.99,27.13,27.06
    27.37,26.91,26.97,27.21; 27.07,26.60,27.05,27.02
    27.33,26.95,27.04,26.96; 27.27,26.95,27.21,27.23
    27.81,27.07,27.76,27.25; 27.94,27.29,27.93,27.50
    28.26,27.91,28.19,27.97; 28.34,28.05,28.10,28.28
    28.34,27.79,27.80,28.20; 27.84,27.51,27.70,27.77 ]


Chart.Line(data,Title="Test Title")
Chart.Line(data,Title="Test Title").WithTitle(InsideArea=false)
Chart.Line(data,Title="Test Title").WithTitle(InsideArea=true)

Chart.Line(data,Title="Test Title")
   |> Chart.WithTitle(InsideArea=true)

Chart.Line(data,Name="Test Data")
   |> Chart.WithXAxis(Enabled=true,Title="X Axis")

Chart.Line(data,Name="Test Data") 
   |> Chart.WithXAxis(Enabled=false,Title="X Axis")

Chart.Line(data,Name="Test Data").WithXAxis(Enabled=false,Title="X Axis")
Chart.Line(data,Name="Test Data").WithXAxis(Enabled=true,Title="X Axis",Max=10.0, Min=0.0).WithYAxis(Max=100.0,Min=0.0)

Chart.Line(data,Name="Test Data").WithLegend(Title="Hello")
Chart.Line(data,Name="Test Data").WithLegend(Title="Hello",Enabled=false)

Chart.Line(data,Name="Test Data").With3D()

// TODO: x/y axis labels are a bit small by default
Chart.Line(data,Name="Test Data",XTitle="hello", YTitle="goodbye")

Chart.Line(data,Name="Test Data").WithXAxis(Title="XXX")
Chart.Line(data,Name="Test Data").WithXAxis(Title="XXX",Max=10.0,Min=4.0).WithYAxis(Title="YYY",Max=100.0,Min=4.0,Log=true)


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

Chart.Columns
     [ Chart.Line(data,Name="Test Data 1")
       Chart.Line(data2,Name="Test Data 2")]
   |> Chart.WithLegend(Title="Hello",Docking=ChartTypes.Docking.Left)  // TODO: this title and docking left doesn't work


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
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Star10)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Diamond)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Cross,Color=Color.Red)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Cross,Color=Color.Red,MaxPixelPointWidth=3)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Cross,Size=3)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Cross,PointWidth=0.1)
Chart.Bubble(pointsWithSizes).WithMarkers(Style=ChartTypes.MarkerStyle.Cross,PixelPointWidth=3)

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


