(*** hide ***)
#I "../../bin"
(** 
# F# Charting: Live Animated Charts

*Summary:* This example shows a collection of LiveCharts, which update as data changes. 

The input data comes from `IObservable` (or `IEvent`) sources.
The data can be created using Rx, F# Event combinators or F# Observable combinators.

The samples are not yet individually documented.

In this sample, some extra event combinators are used to generate reactive data. 
The `EventEx-0.1.fsx` file can be found 
[here](https://raw.githubusercontent.com/fsharp/FSharp.Charting/master/docs/content/EventEx-0.1.fsx).

*)

// On Mac OSX use FSharp.Charting.Gtk.fsx
#I "packages/FSharp.Charting.0.90.9"
#load "FSharp.Charting.fsx"
#load "EventEx-0.1.fsx"

open FSharp.Charting
open System
open System.Drawing

(**
The following code generates sample data - both as a list of values
and as an F# event (`IObservable`) that can be passed to live charts:
*)
let timeSeriesData = 
  [ for x in 0 .. 99 -> 
      DateTime.Now.AddDays (float x),sin(float x / 10.0) ]
let rnd = new System.Random()
let rand() = rnd.NextDouble()

let data = [ for x in 0 .. 99 -> (x,x*x) ]
let data2 = [ for x in 0 .. 99 -> (x,sin(float x / 10.0)) ]
let data3 = [ for x in 0 .. 99 -> (x,cos(float x / 10.0)) ]
let incData = Event.clock 10 |> Event.map (fun x -> (x, x.Millisecond))
(**
You can use `Event.cycle` to create a simple live data source that 
iterates over in-memory sequence, or you can use an event as follows:
*)
// Cycle through two data sets of the same type
LiveChart.Line (Event.cycle 1000 [data2; data3])

LiveChart.LineIncremental(incData,Name="MouseMove")
  .WithXAxis(Enabled=false).WithYAxis(Enabled=false)

LiveChart.FastLineIncremental(incData,Name="MouseMove")
  .WithXAxis(Enabled=false).WithYAxis(Enabled=false)

(*** hide ***)

// All this needs some documentation before we can include it on the page....

let incBubbleData = 
    Event.clock 10 
    |> Event.map (fun x -> (rand(), rand(), rand()))

let evData = 
    Event.clock 10 
    |> Event.map (fun x -> (x, x.Millisecond)) 
    |> Event.windowTimeInterval 3000 

let evData2 = 
    Event.clock 20 
    |> Event.map (fun x -> (x, 100.0 + 200.0 * sin (float (x.Ticks / 2000000L)))) 
    |> Event.windowTimeInterval 3000 

let evBubbleData = 
    Event.clock 10 
    |> Event.map (fun x -> (rand(), rand(), rand())) 
    |> Event.sampled 30 
    |> Event.windowTimeInterval 3000  
    |> Event.map (Array.map (fun (_,x) -> x))

let constantLiveTimeSeriesData = Event.clock 30 |> Event.map (fun _ -> timeSeriesData)

LiveChart.Line(evData,Name="MouseMove")
  .WithXAxis(Enabled=false).WithYAxis(Enabled=false)

LiveChart.Line(evData2,Name="Clock")
  .WithXAxis(Enabled=false).WithYAxis(Enabled=false)

LiveChart.Line(evData2,Name="Clock")
    .WithXAxis(Title="abc")
    .WithYAxis(Title="def")

LiveChart.Point(evData2,Name="Clock")
LiveChart.FastPoint(evData2,Name="Clock")
LiveChart.PointIncremental(incData,Name="Clock")
LiveChart.FastPointIncremental(incData,Name="Clock")
LiveChart.BubbleIncremental(incBubbleData,Name="Clock")
LiveChart.Bubble(evBubbleData,Name="Clock")

LiveChart.Line(evData2,Name="Clock")
    .WithXAxis(Title="")
    .WithYAxis(Title="")

LiveChart.Line(evData2).WithTitle("A")

LiveChart.Line(evData,Name="MouseMove")

LiveChart.Line(constantLiveTimeSeriesData)
