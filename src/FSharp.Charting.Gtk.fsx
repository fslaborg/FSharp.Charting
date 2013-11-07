

#nowarn "211"
#nowarn "40"

#r "gtk-sharp.dll"
#r "gdk-sharp.dll"
#r "atk-sharp.dll"
#r "glib-sharp.dll"
#I "../bin"
#I "../../../packages/FSharp.Charting.Gtk.0.90.5/lib/net40"
#I "../../../packages/FSharp.Charting.Gtk/lib/net40"
#I "../../packages/FSharp.Charting.Gtk.0.90.5/lib/net40"
#I "../../packages/FSharp.Charting.Gtk/lib/net40"
#r "OxyPlot.dll"
#r "OxyPlot.GtkSharp.dll"
#r "FSharp.Charting.Gtk.dll"

open System

// Workaround bug http://stackoverflow.com/questions/13885454/mono-on-osx-couldnt-find-gtksharpglue-2-dll
if Environment.OSVersion.Platform = System.PlatformID.MacOSX then 
    let prevDynLoadPath = Environment.GetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH")
    let newDynLoadPath =  "/Library/Frameworks/Mono.framework/Versions/Current/lib" + (match prevDynLoadPath with null -> "" | s -> ":" + s) + ":/usr/lib"
    System.Environment.SetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH", newDynLoadPath)

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

