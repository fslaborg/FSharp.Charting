

#nowarn "211"
#nowarn "40"

// When compiling, a reference to this is needed

#r "FSharp.Compiler.Interactive.Settings.dll"

// On windows we just reference the DLLs. On Mono we must reference them in ../gtk-sharp-2.0 relative
// to the Mono installation.
//
// On Mono OSX/Linux we could just use
//
//#r "../gtk-sharp-2.0/gtk-sharp.dll"
//#r "../gtk-sharp-2.0/gdk-sharp.dll"
//#r "../gtk-sharp-2.0/atk-sharp.dll"
//#r "../gtk-sharp-2.0/glib-sharp.dll"
//
// and on .NET on Windows
//
//#r "gtk-sharp.dll"
//#r "gdk-sharp.dll"
//#r "atk-sharp.dll"
//#r "glib-sharp.dll"

#I "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gtk-sharp-2.0"
#I "/usr/lib/mono/gtk-sharp-2.0"

// In Ubuntu, GTK-sharp libraries are split into various directories
#I "/usr/lib/cli/glib-sharp-2.0/"
#I "/usr/lib/cli/atk-sharp-2.0/"
#I "/usr/lib/cli/gtk-sharp-2.0/"
#I "/usr/lib/cli/gdk-sharp-2.0/"

#r "gtk-sharp.dll"
#r "gdk-sharp.dll"
#r "atk-sharp.dll"
#r "glib-sharp.dll"

// In F# 2.0, 3.0 and 3.1, the resolution of #r paths in #load'd scripts is NOT relative to the directory where the script
// lives. However, the resolution of #I paths is, and the #I paths have local file scope.
//
// This means that using #I __SOURCE_DIRECTORY__ is sufficient to enable local resolution of #r and #I paths within an included script file.

#I __SOURCE_DIRECTORY__
#I "lib/net40"

#r "OxyPlot.dll"
#r "OxyPlot.GtkSharp.dll"
#r "FSharp.Charting.Gtk.dll"

open System
let verifyMac () = 
    try
        use p=new System.Diagnostics.Process()
        p.StartInfo.FileName<-"uname"
        p.StartInfo.Arguments<-"-s"
        p.StartInfo.RedirectStandardOutput<-true
        p.StartInfo.UseShellExecute<-false
        p.StartInfo.CreateNoWindow<-true
        p.Start() |> ignore
        let kernalName=p.StandardOutput.ReadLine()
        p.WaitForExit()
        kernalName="Darwin"
    with
        |_ ->false

let isMac = 
    match Environment.OSVersion.Platform with
        | PlatformID.MacOSX -> true
        | PlatformID.Unix -> verifyMac()
        | _ -> false
// Workaround bug http://stackoverflow.com/questions/13885454/mono-on-osx-couldnt-find-gtksharpglue-2-dll
//
// There is no harm if this code is run more than once.
if isMac then 
    let prevDynLoadPath = Environment.GetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH")
    let newDynLoadPath =  "/Library/Frameworks/Mono.framework/Versions/Current/lib" + (match prevDynLoadPath with null -> "" | s -> ":" + s) + ":/usr/lib"
    System.Environment.SetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH", newDynLoadPath)

// Initialize Gtk. There is no harm if this code is run more than once.
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


// Add an auto-display for things of type "GenericChart"

open FSharp.Charting
module FsiAutoShow = 
    fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) -> ch.ShowChart() |> ignore; "(Chart)")

