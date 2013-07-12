(** 
# FSharp.Charting: Referencing the Library

FSharp.Charting is most often used from F# scripts. To use the library 
in a script, load the FSharp.Charting.fsx file, for example from the NuGet package:
*)

#load "packages/FSharp.Charting.0.84/FSharp.Charting.fsx"

(**
You can now create a chart:
*)

open FSharp.Charting

Chart.Line [ for x in 0 .. 10 -> x, x*x ]


(** 
When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive 
invokes a handler that automatically shows the created chart.

Alternatively, you can reference the `FSharp.Charting` DLL directly and manually display the charts using ShowChart():
*)

#r "FSharp.Charting.dll"

open FSharp.Charting

Chart.Line([ for x in 0 .. 10 -> x, x*x ]).ShowChart()


(**
To use the library in a project, either

 * add the `FSharp.Charting` NuGet package, or
 * reference the `FSharp.Charting` dll directly

*)


