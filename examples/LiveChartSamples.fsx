(** 

This sample shows a collection of 'LiveChart's, which update as data changes. The input data comes from IObservable (or IEvent) sources.
The data can be created using Rx, F# Event combinators or F# Observable combinators.

The samples are not yet indivudally documented.

In this sample, some extra event combinators are used to generate reactive data. 
The EventEx-0.1.fsx file can be found at https://raw.github.com/fsharp/FSharp.Charting/master/examples/EventEx-0.1.fsx

*)

// On Mac OSX use packages/FSharp.Charting.Gtk.0.90.6/FSharp.Charting.Gtk.fsx
#load "packages/FSharp.Charting.0.90.6/FSharp.Charting.fsx"
#load "EventEx-0.1.fsx"

open FSharp.Charting
open System
open System.Drawing

let timeSeriesData = [ for x in 0 .. 99 -> (DateTime.Now.AddDays (float x),sin(float x / 10.0)) ]
let rnd = new System.Random()
let rand() = rnd.NextDouble()

//let form = new System.Windows.Forms.Form(Visible=true,TopMost=true)
//let incData = form.MouseMove |> Event.map (fun e -> e.Y) |> Event.sampled 30 
//let evData = form.MouseMove |> Event.map (fun e -> e.Y) |> Event.sampled 30 |> Event.windowTimeInterval 3000

let data = [ for x in 0 .. 99 -> (x,x*x) ]
let data2 = [ for x in 0 .. 99 -> (x,sin(float x / 10.0)) ]
let data3 = [ for x in 0 .. 99 -> (x,cos(float x / 10.0)) ]
let incData = Event.clock 10 |> Event.map (fun x -> (x, x.Millisecond))

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

// Cycle through two data sets of the same type
LiveChart.Line (Event.cycle 1000 [data2; data3])

LiveChart.LineIncremental(incData,Name="MouseMove").WithXAxis(Enabled=false).WithYAxis(Enabled=false)
LiveChart.FastLineIncremental(incData,Name="MouseMove").WithXAxis(Enabled=false).WithYAxis(Enabled=false)

LiveChart.Line(evData,Name="MouseMove").WithXAxis(Enabled=false).WithYAxis(Enabled=false)
LiveChart.Line(evData2,Name="Clock").WithXAxis(Enabled=false).WithYAxis(Enabled=false)
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

