(** 
# FSharp.Charting: Point and Line Charts

*Summary:* This example shows how to create line and point charts in F#. 


This example looks at how to create line and point charts from F#. 

A line or a point chart can be created using the Chart.Line and Chart.Point functions. When generating a 
very large number of points or lines, it is better to use Chart.FastLine and Chart.FastPoint. These are special types 
of charts that do not support as many visual features, but are more efficient.

When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive 
invokes a handler that automatically shows the created chart.

## A Simple Line Chart

The following example calls the Chart.Line function with a list of X and Y values as tuples. The snippet generates 
values of a simple function, f(x)=x^2. The values of the function are generated for x ranging from 1 to 100. The chart generated is 
shown below.

<div>
    <img src="images/Squares.png" alt="Line Chart">
</div>


*)

// On Mac OSX use packages/FSharp.Charting.Gtk.0.90.5/FSharp.Charting.Gtk.fsx
#load "packages/FSharp.Charting.0.90.5/FSharp.Charting.fsx"

open FSharp.Charting
open System

// Drawing graph of a 'square' function 
Chart.Line [ for x in 1.0 .. 100.0 -> (x, x ** 2.0) ]

(**


## Pipelining into Chart.Line

The following example generates a list containing both X and Y values. 
*)

// Generates 2D curve using list of tuples
let curvyData = [ for i in 0.0 .. 0.02 .. 2.0 * Math.PI -> (sin i, cos i * sin i) ] 

curvyData |> Chart.Line

(**


## Specifying only Y values

The following example below shows that you may also simply give a set of Y values, rather than (X,Y) value pairs.
*)

// Generates 2D curve using only Y values
Chart.Line [ for x in 1.0 .. 100.0 -> x * x * sin x ]


(**

It uses a sequence expression ranging
from 0 to 2π with a step size 0.02. This produces a large number of points, so the snippet uses the Chart.Line 
function to draw the chart. When using a single list as the data source, it is also possible to elegantly use pipelining (|> operator).

<div>
    <img src="images/IC523424.png" alt="Sample Line Chart">
</div>

## A Point Chart

The following example shows how to generate a scatter plot. It uses a list to specify the X- and Y-coordinates of the points. 
*)

// Draw scatter plot  of points
let rnd = new Random()
let rand() = rnd.NextDouble()
let randomPoints = [ for i in 0 .. 1000 -> rand(), rand() ]

Chart.Point randomPoints

(**


## Specifying Minimums, Maximums and other properties on a Line Chart

The following example shows how to set the "Name", Y-Axis minimum properties on the chart, and use log-distribution for the X axis.
*)


let highData = [ for x in 1.0 .. 100.0 -> (x, 3000.0 + x ** 2.0) ]

Chart.Line(highData,Name="Rates").WithYAxis(Min=2000.0).WithXAxis(Log=true)

(**


*)

(**
## Combining Line Charts

The following example shows how to combine several line charts and give each data set a name. A legend is added
automatically when names are used for data sets.

<div>
    <img src="images/IC36812.png" alt="Multiple Line Chart">
</div>


*)
let futureDate numDays = DateTime.Today.AddDays(float numDays)

let expectedIncome = [ for x in 1 .. 100 -> (futureDate x, 1000.0 + rand() * 100.0 * exp (float x / 40.0) ) ]
let expectedExpenses = [ for x in 1 .. 100 -> (futureDate x, rand() * 500.0 * sin (float x / 50.0) ) ]
let computedProfit = (expectedIncome, expectedExpenses) ||> List.map2 (fun (d1,i) (d2,e) -> (d1, i - e))

Chart.Line(expectedIncome,Name="Income")
Chart.Line(expectedExpenses,Name="Expenses")
Chart.Line(computedProfit,Name="Profit")

Chart.Combine(
   [ Chart.Line(expectedIncome,Name="Income")
     Chart.Line(expectedExpenses,Name="Expenses") 
     Chart.Line(computedProfit,Name="Profit") ])

