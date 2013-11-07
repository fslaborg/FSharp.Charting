// --------------------------------------------------------------------------------------
// Builds the documentation from FSX files in the 'examples' directory
// (the documentation is stored in the 'docs' directory)
// --------------------------------------------------------------------------------------

#I "../packages/FSharp.Formatting.1.0.15/lib/net40"
#load "../packages/FSharp.Formatting.1.0.15/literate/literate.fsx"
open System.IO
open FSharp.Literate

let (++) a b = Path.Combine(a, b)
let template = __SOURCE_DIRECTORY__ ++ "template.html"
let sources  = __SOURCE_DIRECTORY__ ++ "../examples"
let output   = __SOURCE_DIRECTORY__ ++ "../docs"

// Root URL for the generated HTML
let root = "http://fsharp.github.com/FSharp.Charting"

// When running locally, you can use your path
//let root = @"file://C:\Tomas\Projects\FSharp.Charting\docs"


// Generate HTML from all FSX files in samples & subdirectories
let build () =
  // Copy all sample data files to the "data" directory
  let copy = [ sources ++ "../tools/content", output ++ "content" ]
  for source, target in copy do
    if Directory.Exists target then Directory.Delete(target, true)
    Directory.CreateDirectory target |> ignore
    for fileInfo in DirectoryInfo(source).EnumerateFiles() do
        fileInfo.CopyTo(target ++ fileInfo.Name) |> ignore

  let lastChange = System.IO.File.ReadAllLines (sources ++ "../RELEASE_NOTES.md") |> Seq.last 
  let ver = lastChange.Split(' ').[1]

  // Set up a deployment that looks a bit like an install of the package
  let deployment1 = sources ++ "packages" ++ "FSharp.Charting." + ver
  let deployment2 = sources ++ "packages" ++ "FSharp.Charting.Gtk." + ver
  for deployment in [deployment1; deployment2] do 
      try Directory.Delete(deployment,true)  with _ -> ()
      Directory.CreateDirectory(deployment ++ "lib" ++ "net40") |> ignore
      for fileInfo in DirectoryInfo(sources ++ "../bin").EnumerateFiles() do
          fileInfo.CopyTo(deployment ++ "lib" ++ "net40" ++ fileInfo.Name) |> ignore
          fileInfo.CopyTo(deployment ++ fileInfo.Name) |> ignore


  for sub in [ "."  ] do
    Literate.ProcessDirectory
      ( sources ++ sub, template, output ++ sub, 
        replacements = [ "root", root ] )

build()