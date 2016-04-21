namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FSharp.Charting")>]
[<assembly: AssemblyProductAttribute("FSharp.Charting")>]
[<assembly: AssemblyDescriptionAttribute("A Charting Library for F#")>]
[<assembly: AssemblyVersionAttribute("0.90.13")>]
[<assembly: AssemblyFileVersionAttribute("0.90.13")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.90.13"
