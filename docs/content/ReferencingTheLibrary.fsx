(** 
# F# Charting: Referencing the Library

F# Charting is most often used from F# scripts. To use the library 
in a script, load the `FSharp.Charting.fsx` file, for example from the NuGet package:
*)

// On Mac OSX use packages/FSharp.Charting.Gtk.0.90.14/FSharp.Charting.Gtk.fsx
#load "packages/FSharp.Charting/FSharp.Charting.fsx"
(**
You can now create a chart:
*)

open FSharp.Charting

Chart.Line [ for x in 0 .. 10 -> x, x*x ]


(** 
When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive 
invokes a handler that automatically shows the created chart.

Alternatively, you can reference `FSharp.Charting.dll` directly and manually display the charts using `ShowChart`:
*)

// Note, on Mac OSX use FSharp.Charting.Gtk.dll.
#r "FSharp.Charting.dll" 

open FSharp.Charting

Chart.Line([ for x in 0 .. 10 -> x, x*x ]).ShowChart()


(**
To use the library in a project, either

 * add the `FSharp.Charting` NuGet package, or
 * reference `FSharp.Charting.dll` directly

*)


