
// On Mac OSX use packages/FSharp.Charting.Gtk.0.90.9/FSharp.Charting.Gtk.fsx
#load "packages/FSharp.Charting.Gtk.0.90.9/FSharp.Charting.Gtk.fsx"
#load "EventEx-0.1.fsx"

open FSharp.Charting

Chart.Point [ for x in 0 .. 10 -> x*x ] 
Chart.Bubble [ for x in 0 .. 10 -> x, 2.0 ] 
Chart.Bubble [ for x in 0 .. 10 -> x, 3.0 ] 
Chart.Bubble [ for x in 0 .. 10 -> x, x ] 

Chart.Bubble [ for x in 0 .. 10 -> x, x * 100 ] 
// TODO: should auto-scale the bubble size in some way

Chart.Bubble [ for x in 0 .. 10 -> x, x + 1] 
Chart.Bubble [ for x in 0 .. 10 -> x, x*x ] 
Chart.Bubble [ for x in 0 .. 10 -> x, x*x, x*x ] 
Chart.Bubble [ for x in 0 .. 10 -> x, x*x, float (x*x) ] 
Chart.Bubble [ for x in 0 .. 10 -> x, x*x, single  (x*x) ] 

// Check with date time date
Chart.Area [ for i in 0 .. 10 -> (System.DateTime.Now.AddDays(float i), i*i)] 
Chart.Area [ for i in 0 .. 10 -> (string i, i*i)] 

// TODO: needs to show the labels as "categories"
//Chart.Bar [ for i in 0 .. 10 -> ("abc" + string i, i*i)] 

Chart.Line [ for i in 0 .. 10 -> (System.DateTime.Now.AddSeconds(float i), i*i)] 
Chart.Line [ for i in 0 .. 10 -> (System.DateTime.Now.AddMinutes(float i), i*i)] 
Chart.Line [ for i in 0 .. 10 -> (System.DateTime.Now.AddHours(float i), i*i)] 
Chart.Line [ for i in 0 .. 10 -> (System.DateTime.Now.AddDays(float i), i*i)] 
Chart.Line [ for i in 0 .. 10 -> (System.DateTime.Now.AddMonths(i), i*i)] 
Chart.Line [ for i in 0 .. 10 -> (System.DateTime.Now.AddYears(i), i*i)] 
Chart.Line [ for i in 0 .. 10 -> (System.TimeSpan.FromDays(float i), i*i)] 


// TODO: this scatter plot asserts, we can't show DateTime in scatter plots X axis
// Chart.Point [ for i in 0 .. 10 -> (System.DateTime.Now.AddDays(float i), i*i)] 


// TODO: check labels

//let check (c:GenericChart) = c.ShowChart() |> ignore
Chart.Area [ 0 .. 10 ]   // TODO: this doesn't fill the area
Chart.Area ([ 0 .. 10 ],Color=OxyPlot.OxyColors.Blue)   // TODO: this doesn't fill the area
Chart.Area [ for x in 0 .. 10 -> x, x*x ] 
Chart.Bar [ 0 .. 10 ] 
Chart.Bar [ for x in 0 .. 10 -> x, x*x ] 
Chart.Bar [ for x in 0 .. 10 -> float x, x*x ] 
Chart.Bar [ for x in 0 .. 10 -> System.DateTime.Now.AddDays(float x), x*x ]  // TODO, the category is not used
Chart.Bubble [ for x in 0 .. 10 -> x, x*x ] 
Chart.Bubble [ for x in 0 .. 10 -> x, x*x, x*x ] 
Chart.Bubble [ for x in 0 .. 10 -> x, x*x, float (x*x) ] 
Chart.Column [ 0 .. 10 ] 
Chart.Column [ for x in 0 .. 10 -> x, x*x ] 
(*
Chart.Doughnut [ 0 .. 10 ] 
Chart.Doughnut [ for x in 0 .. 10 -> x, x*x ] 
Chart.ErrorBar [ for x in 0 .. 10 -> x, x, x-1, x+1 ] 
Chart.FastLine [ 0 .. 10 ] 
Chart.FastLine [ for x in 0 .. 10 -> x, x*x ] 
Chart.FastPoint [ 0 .. 10 ] 
Chart.FastPoint [ for x in 0 .. 10 -> x, x*x ] 
Chart.Funnel [ 0 .. 10 ] 
Chart.Funnel [ for x in 0 .. 10 -> x, x*x ] 
    
// Bug: it is really hard to see the data here, see https://github.com/fslaborg/FSharp.Charting/issues/14
Chart.Kagi [ 1.1; 3.1; 4.1; 5.1; 3.1; 2.1;  ] 
Chart.Kagi [ for x in 0 .. 10 -> float x + 0.1, x*x ] 
*)
    
Chart.Line [ 0 .. 10 ] 
Chart.Line [ for i in 0 .. 10 -> (System.DateTime.Now.AddDays(float i), i*i)] 
Chart.Line [ for x in 0 .. 10 -> x, x*x ] 
Chart.Pie [ 0 .. 10 ] 
Chart.Pie [ for x in 0 .. 10 -> x, x*x ] 
Chart.Point [ 0 .. 10 ] 
Chart.Point [ for x in 0 .. 10 -> x, x*x ] 
(*
Chart.PointAndFigure [ for x in 0 .. 10 -> cos (float x), sin (float x) ] 
Chart.PointAndFigure [ for x in 0 .. 10 -> x, cos (float x), sin (float x) ] 
Chart.Polar [ 0 .. 10 ] 
Chart.Polar [ for x in 0 .. 10 -> x, x*x ] 
Chart.Pyramid [ 0 .. 10 ] 
Chart.Pyramid [ for x in 0 .. 10 -> x, x*x ] 
Chart.Radar [ 0 .. 10 ] 
Chart.Radar [ for x in 0 .. 10 -> x, x*x ] 
Chart.Range [ for x in 0 .. 10 -> x, x*x ] 
Chart.Range [ for x in 0 .. 10 -> x, x-1, x+1 ] 
Chart.RangeBar [ for x in 0 .. 10 -> x, x*x ] 
Chart.RangeBar [ for x in 0 .. 10 -> x, x-1, x+1 ] 
Chart.RangeColumn [ for x in 0 .. 10 -> x, x*x ] 
Chart.RangeColumn [ for x in 0 .. 10 -> x, x-1, x+1 ] 
Chart.Renko [ 0 .. 10 ] 
Chart.Renko [ for x in 0 .. 10 -> x, x*x ] 
Chart.Spline [ 0 .. 10 ] 
Chart.Spline [ for x in 0 .. 10 -> x, x*x ] 
Chart.SplineArea [ 0 .. 10 ] 
Chart.SplineArea [ for x in 0 .. 10 -> x, x*x ] 
Chart.SplineRange [ for x in 0 .. 10 -> x, x*x ] 
Chart.SplineRange [ for x in 0 .. 10 -> x, x*x, x+4 ] 
Chart.StackedArea [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] 
Chart.StackedArea100 [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] 
Chart.StackedBar [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] 
Chart.StackedBar100 [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] 
Chart.StackedColumn [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] 
Chart.StackedColumn100 [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] 
Chart.StepLine [for x in 0 .. 10 -> x, x] 
Chart.BoxPlotFromStatistics( 
        [ ("Result A", -12.7, 11.6, -8.3, 6.4, 0.0, 0.0);
          ("Result B", -6.7, 11.6, -5.0, 5.4, 0.0, 0.0) ]) |> check

let date n = System.DateTime.Today.AddDays(float n).ToShortDateString()
let rnd = new System.Random()

let threeSyntheticDataSets = 
    [ (date 0, [| for i in 0 .. 20 -> float (rnd.Next 20) |])
      (date 1, [| for i in 0 .. 20 -> float (rnd.Next 15 + 2) |])
      (date 2, [| for i in 0 .. 20 -> float (rnd.Next 10 + 5) |]) ]

Chart.BoxPlotFromData
    ( threeSyntheticDataSets,
    ShowUnusualValues = true, ShowMedian = false,
    ShowAverage = false, WhiskerPercentile = 10) |> check

Chart.Candlestick [for x in 0 .. 10 -> x+10, x-10, x+5, x-5 ] 
Chart.Stock [for x in 0 .. 10 -> x+10, x-10, x+5, x-5 ] 
*)


Chart.Line [ for i in 0.0 .. 10.0 -> i, i*i ]
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], Name="The Curve Of Growth", Color=OxyPlot.OxyColors.Blue)
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], Name="The Curve Of Growth", Color=OxyPlot.OxyColors.Green)
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], Name="The Curve Of Growth")
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], Title="The Only Way is Up")
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], Name="The Curve Of Growth", XTitle="Time")
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], XTitle="Time", YTitle="Growth")
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], Title="The Only Way is Up", XTitle="Time", YTitle="Growth")
Chart.Line ([ for i in 0.0 .. 10.0 -> i, i*i ], XTitle="Time")

//--------------------------------------------------------------------------



#load "EventEx-0.1.fsx"
let rnd = new System.Random()
let rand() = rnd.NextDouble()
let incData = Event.clock 10 |> Event.map (fun x -> (x, x.Millisecond))
let evData = 
    Event.clock 10 
    |> Event.map (fun x -> (x, x.Millisecond)) 
    |> Event.windowTimeInterval 3000 

let evData2 = 
    Event.clock 20 
    |> Event.map (fun x -> (x, 100.0 + 200.0 * sin (float (x.Ticks / 2000000L)))) 
    |> Event.windowTimeInterval 3000 

LiveChart.Line evData  // TODO: this fails because it doesn't like DateTime

let data2 = [ for x in 0 .. 99 -> (x,sin(float x / 10.0)) ]
let data3 = [ for x in 0 .. 99 -> (x,cos(float x / 10.0)) ]
// Cycle through two data sets of the same type
LiveChart.Line (Event.cycle 1000 [data2; data3])
LiveChart.LineIncremental incData
LiveChart.Line evData
LiveChart.Line evData2
LiveChart.Line(evData2,Name="Clock")
LiveChart.Line(evData2,Name="Clock",Title="ABC")

let evBubbleData = 
    Event.clock 10 
    |> Event.map (fun x -> (rand(), rand(), rand() * 10.0)) 
    |> Event.sampled 30 
    |> Event.windowTimeInterval 3000  
    |> Event.map (Array.map (fun (_,x) -> x))

let incBubbleData = 
    Event.clock 10 
    |> Event.map (fun x -> (rand(), rand(), rand() * 10.0))

LiveChart.BubbleIncremental(incBubbleData,Name="Clock")
LiveChart.Bubble(evBubbleData,Name="Clock")

(*
LiveChart.LineIncremental(incData,Name="MouseMove").WithXAxis(Enabled=false).WithYAxis(Enabled=false)
LiveChart.FastLineIncremental(incData,Name="MouseMove").WithXAxis(Enabled=false).WithYAxis(Enabled=false)

LiveChart.Line(evData,Name="MouseMove").WithXAxis(Enabled=false).WithYAxis(Enabled=false)
LiveChart.Line(evData2,Name="Clock").WithXAxis(Enabled=false).WithYAxis(Enabled=false)
LiveChart.Line(evData2,Name="Clock")
    .WithXAxis(Title="abc")
    .WithYAxis(Title="def")

LiveChart.Point(evData2,Name="Clock")
LiveChart.PointIncremental(incData,Name="Clock")
*)

//LiveChart.FastPoint(evData2,Name="Clock")
//LiveChart.FastPointIncremental(incData,Name="Clock")

(*
LiveChart.Line(evData2,Name="Clock")
    .WithXAxis(Title="")
    .WithYAxis(Title="")

LiveChart.Line(evData2).WithTitle("A")

LiveChart.Line(evData,Name="MouseMove")

LiveChart.Line(constantLiveTimeSeriesData)
*)

//--------------------------------------------------------------------------


open Gtk
module Example1 = 
    let win = new Window("Hello")
    let btn = new Button("Button")
    btn.Visible <- true
    btn.Show()
    win.Add(btn)
    win.Present()
