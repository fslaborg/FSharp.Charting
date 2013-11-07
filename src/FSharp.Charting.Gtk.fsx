

#nowarn "211"
#nowarn "40"

#r "gtk-sharp.dll"
#r "gdk-sharp.dll"
#r "atk-sharp.dll"
#r "glib-sharp.dll"
#I "../bin"
#I "../../../packages/FSharp.Charting.0.88/lib/net40"
#I "../../../packages/FSharp.Charting/lib/net40"
#I "../../packages/FSharp.Charting.0.88/lib/net40"
#I "../../packages/FSharp.Charting/lib/net40"
#r "OxyPlot.dll"
#r "OxyPlot.GtkSharp.dll"
#r "FSharp.Charting.Gtk.dll"

Gtk.Application.Init()

fsi.EventLoop <- 
 { new Microsoft.FSharp.Compiler.Interactive.IEventLoop with
   member x.Run() = Gtk.Application.Run() |> ignore; false
   member x.Invoke f = 
     let res = ref None
     let evt = new System.Threading.AutoResetEvent(false)
     Gtk.Application.Invoke(new System.EventHandler(fun _ _ ->
       res := Some(f())
       evt.Set() |> ignore ))
     evt.WaitOne() |> ignore
     res.Value.Value 
   member x.ScheduleRestart() = () }

open FSharp.Charting
module FsiAutoShow = 
    fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) -> ch.ShowChart(); "(Chart)")

