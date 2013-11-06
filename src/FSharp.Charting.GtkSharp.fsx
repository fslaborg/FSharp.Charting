

#r "gtk-sharp.dll"
#r "gdk-sharp.dll"
#r "atk-sharp.dll"
#r "glib-sharp.dll"
#I "../bin"
#I "../../packages/FSharp.Charting.0.87/lib/net40"
#I @"C:\projects\oxyplot\Source\OxyPlot\bin\PCL\Debug"
#I @"C:\projects\oxyplot\Source\OxyPlot.GtkSharp\bin\Debug"
#r "OxyPlot.dll"
#r "OxyPlot.GtkSharp.dll"

#nowarn "211"
#nowarn "40"
#load "FSharp.Charting.GtkSharp.fs"

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

