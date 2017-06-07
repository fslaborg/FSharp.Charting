(*** hide ***)
#I "../../bin"
#load "FSharp.Charting.fsx"
open FSharp.Charting
open System
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
let priceSeries = 
    prices |> List.mapi (fun i (hi,lo,op,cl) -> 
        (DateTime.Today.AddDays(float i).ToShortDateString(), hi, lo, op, cl))
let futureDate numDays = DateTime.Today.AddDays(float numDays)
let rnd = new Random()
let rand() = rnd.NextDouble()
let expectedIncome = 
  [ for x in 1 .. 100 -> 
      futureDate x, 1000.0 + rand() * 100.0 * exp (float x / 40.0) ]
let expectedExpenses = 
  [ for x in 1 .. 100 -> 
      futureDate x, rand() * 500.0 * sin (float x / 50.0) ]
let computedProfit = 
  (expectedIncome, expectedExpenses) 
  ||> List.map2 (fun (d1,i) (d2,e) -> (d1, i - e))


(**
F# Charting: Library for Data Visualization
===========================================

The F# Charting library implements charting suitable for use from F# scripting.
Once you load the library as documented in [referencing the library document](ReferencingTheLibrary.html), you 
can use the members of the `Chart` type to easily build charts. The following example creates a candlestick
chart for a time series and sets the range of the Y axis:
*)

(*** define-output:prices ***)
Chart.Candlestick(priceSeries).WithYAxis(Max = 29.0, Min = 25.0)
(*** include-it:prices ***)

(**
The library provides a composable API for creating charts. For example, you can use the 
`Chart.Combine` function to create a chart consisting of multiple line charts. The following
example creates a single area showing sample income, expenses and profit in a single chart:
*)

(*** define-output:income***)
Chart.Combine(
   [ Chart.Line(expectedIncome,Name="Income")
     Chart.Line(expectedExpenses,Name="Expenses") 
     Chart.Line(computedProfit,Name="Profit") ])
(*** include-it:income***)


(**

### How to get F# Charting

 * The Windows version of the library is available as [FSharp.Charting](https://nuget.org/packages/FSharp.Charting) on NuGet
 * The Mac/Linux version of the library is available as [FSharp.Charting.Gtk](https://nuget.org/packages/FSharp.Charting.Gtk) on NuGet
 * Alternatively, you can download the [source as a ZIP file][source] or as a [binary release as a ZIP file][release].

F# Charting features 
--------

* Cross-platform 2D charting and support for pseudo-3D charts on .NET.
* Many cross-platform chart types: Area, Bar, Bubble, Column, Line, Point and more.
* Create charts directly from F# data such as lists and tuples.
* Use either fluent or pipelined chart specifications.
* Create updating 'LiveChart' charts from F# or Rx observables.
* Can be used in conjunction with the [FSharp.Data](http://fsharp.github.io/FSharp.Data) library</a>.
* Many extra chart types (Windows-only): BoxPlot, Candlestick, Doughnut, ErrorBar, FastLine, FastPoint, Funnel, Kagi and more.

Not all features are supported in the FSharp.Charting.Gtk version of the library.
See ``INCOMPLETE_API`` in the implementation.  Contributions to compelte the features are welcome.

Approach, history and future
-----------------------------

This library is a successor to `FSharpChart`. The last version of FSharpChart was [version 0.61][fsharpchart61].

F# Charting uses simple, declarative chart specifications.
On Windows, F# Charting is implemented using the Data Visualization charting controls 
available on Windows in .NET 4.x.
On OSX, F# Charting is implemented using the OxyPlot.GtkSharp charting library.

F# Charting is designed so that the same charting specifications can be supported when 
using different charting implementations. 

### Contributing

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding new public API, please also 
contribute [examples][examples] that can be turned into a documentation.

 * If you want to discuss an issue or feature that you want to add the to the library,
   then you can submit [an issue or feature request][issues] via Github or you can 
   send an email to the [F# open source][fsharp-oss] mailing list.

 * For more information about the library architecture, organization and more
   see the [contributing](contributing.html) page.

 * The documentation is automatically generated from `*.fsx` files in  [the `docs/content` folder][examples]. 
   If you find a typo, please submit a pull request! 


### Library license

The library is available under the MIT licence. For more information see the 
[License file][readme] in the GitHub repository. In summary, this means that you can 
use the library for commercial purposes, fork it, modify it as you wish.

  [source]: https://github.com/fslaborg/FSharp.Charting/zipball/master
  [release]: https://github.com/fslaborg/FSharp.Charting/zipball/release
  [examples]: https://github.com/fslaborg/FSharp.Charting/tree/master/docs/content
  [gh]: https://github.com/fslaborg/FSharp.Charting
  [issues]: https://github.com/fslaborg/FSharp.Charting/issues
  [readme]: https://github.com/fslaborg/FSharp.Charting/blob/master/README.md
  [fsharp-oss]: http://groups.google.com/group/fsharp-opensource
  [fsharpchart61]: http://code.msdn.microsoft.com/windowsdesktop/FSharpChart-b59073f5
*)
