(*** hide ***)
#I "../../bin"
(** 
# F# Charting: Bar and Column Charts

*Summary:* This example shows how to create bar and column charts in F#.

The input data in this example is an F# list of tuples containing continent names and total populations.
The example demonstrates how to display a bar/column chart with names of continents as labels and the 
populations as the values. A sample bar chart is shown below.

A bar or a column chart can be created using the `Chart.Column` and `Chart.Bar` methods.

All methods are overloaded and can be called with various types of parameters. When called with a list 
containing just Y values, the chart automatically uses the sequence 1, 2, 3… for the X values. Alternatively, 
it is possible to provide a list containing both X and Y values as a tuple, which gives a way to draw 2D 
curves and scatter plots as well. Here are three examples:

*)

// On Mac OSX use FSharp.Charting.Gtk.fsx
#I "packages/FSharp.Charting.Gtk"
#load "FSharp.Charting.Gtk.fsx"

open FSharp.Charting
open System

let countryData = 
    [ "Africa", 1033043; 
      "Asia", 4166741; 
      "Europe", 732759; 
      "South America", 588649; 
      "North America", 351659; 
      "Oceania", 35838  ]

(*** define-output:bar ***)
Chart.Bar countryData
(*** include-it:bar ***)

(*** define-output:col ***)
Chart.Column countryData
(*** include-it:col ***)

(*** define-output:bar2 ***)
Chart.Bar [ 0 .. 10 ] 
(*** include-it:bar2 ***)

(**
When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive 
invokes a handler that automatically shows the created chart.

The first example specifies the data source as a single list that contains two-element tuples. The first 
element of the tuple is the X value (category) and the second element is the population. 

The second example creates a Column chart instead of a Bar chart.

The third example below shows that you may also simply give a set of Y values, rather than (X,Y) value pairs.

*)

