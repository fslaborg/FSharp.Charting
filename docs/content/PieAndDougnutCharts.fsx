(*** hide ***)
#I "../../bin"
(** 
# F# Charting: Pie and Doughnut Charts

*Summary:* This example shows how to create pie and doughnut charts in F#.

The input data in this example is an F# list of tuples containing the names of political parties and 
their respective numbers of elected candidates. The example demonstrates how to display a 
pie/doughnut chart showing the proportion of seats taken by each party. A 
sample doughnut chart is shown below.

A pie or a doughnut chart can be created using the `Chart.Pie` and `Chart.Doughnut` functions.
When creating pie or doughnut charts, it is usually desirable to provide both labels and 
values. This is done by using a single collection with labels and values as tuple. Here are three examples:

*)

// On Mac OSX use FSharp.Charting.Gtk.fsx
#I "packages/FSharp.Charting"
#load "FSharp.Charting.fsx"

open FSharp.Charting
open System

let electionData = 
  [ "Conservative", 306; "Labour", 258; 
    "Liberal Democrat", 57 ]

(*** define-output:p ***)
Chart.Pie electionData
(*** include-it:p ***)

(*** define-output:d ***)
Chart.Doughnut electionData
(*** include-it:d ***)

(**
When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive 
invokes a handler that automatically shows the created chart.

The first example specifies the data source as a single list that contains two-element tuples. The first 
element of the tuple is the label and the second element is the value. 

The second example creates a doughnut chart instead of a pie chart.

*)