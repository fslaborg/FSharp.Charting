(** 
# FSharp.Charting: Point and Line Charts

*Summary:* This example shows how to create line and point charts in F#. The example draws a graph of a function, creates a scatter plot and draws a 2D spline defined by a function.

This example looks at how to create line and point charts from F#. It presents two versions of the example. The first one uses the FSharpChart library that is available as a free download and the second uses the .NET Chart Controls directly. For more information about loading the two libraries from F#, refer to the  section at the end of the page.

This example demonstrates how to draw the graph of a simple mathematical function (such as sin(x), x2, or similar). This can be done by generating a collection containing Y values of the function. The article also shows how to draw a scatter plot from a collection of X and Y values and how to draw a 2D curve. An example of a curve drawn using a line chart is shown in Figure 1.

A line or a point chart can be created using the Chart.Line and Chart.Point functions. When generating a 
very large number of points or lines, it is better to use Chart.FastLine and Chart.FastPoint. These are special types 
of charts that do not support as many visual features, but are more efficient.

All functions are overloaded and can be called with various types of parameters. When called with a list 
containing just Y values, the chart automatically uses the sequence 1, 2, 3… for the X values. Alternatively, 
it is possible to provide a list containing both X and Y values as a tuple, which gives a way to draw 2D 
curves and scatter plots as well. Here are three examples:

*)

#load "../bin/FSharp.Charting.fsx"

open FSharp.Charting
open System

// Drawing graph of a 'square' function 
Chart.Line [ for x in 1.0 .. 100.0 -> (x, x ** 2.0) ]

// Generates 2D curve using list of tuples
[ for i in 0.0 .. 0.02 .. 2.0 * Math.PI -> (sin i, cos i * sin i) ] 
    |> Chart.Line

// Draw scatter plot  of points
let rnd = new Random()
let rand() = rnd.NextDouble()
let randomPoints = [ for i in 0 .. 1000 -> rand(), rand() ]

Chart.Point randomPoints

(**
When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive 
invokes a handler that automatically shows the created chart.

The first example calls the Chart.Line function with a list of X and Y values as tuples. The snippet generates 
values of a simple function, f(x)=x^2. The values of the function are generated for x ranging from 1 to 100.

The second example generates a list containing both X and Y values. It uses a sequence expression ranging
from 0 to 2π with a step size 0.02. This produces a large number of points, so the snippet uses the Chart.Line 
function to draw the chart. When using a single list as the data source, it is also possible to elegantly use pipelining (|> operator).

Finally, the last example shows how to generate a scatter plot. It uses a list to specify the X- and Y-coordinates of the points. 

*)

