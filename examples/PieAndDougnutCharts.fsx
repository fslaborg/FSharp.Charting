(** 
# FSharp.Charting: Pie and Doughnut Charts

*Summary:* This example shows how to create pie and doughnut charts in F#.

The input data in this example is an F# list of tuples containing the names of political parties and 
their respective numbers of elected candidates. The example demonstrates how to display a 
pie/doughnut chart showing the proportion of seats taken by each party. A 
sample doughnut chart is shown in Figure 1.

<div>
    <img src="images/IC523435.png" alt="Sample BoxPlot Chart">
</div>


A pie or a doughnut chart can be created using the `Chart.Pie` and `Chart.Doughnut` functions.
When creating pie or doughnut charts, it is usually desirable to provide both labels and 
values. This is done by using a single collection with labels and values as tuple. Here are three examples:

*)

// On Mac OSX use packages/FSharp.Charting.Gtk.0.90.6/FSharp.Charting.Gtk.fsx
#load "packages/FSharp.Charting.0.90.6/FSharp.Charting.fsx"

open FSharp.Charting
open System

let electionData = 
  [ "Conservative", 306; "Labour", 258; 
    "Liberal Democrat", 57 ]

Chart.Pie electionData

Chart.Doughnut electionData


(**
When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive 
invokes a handler that automatically shows the created chart.

The first example specifies the data source as a single list that contains two-element tuples. The first 
element of the tuple is the label and the second element is the value. 

The second example creates a doughnut chart instead of a pie chart.

*)

