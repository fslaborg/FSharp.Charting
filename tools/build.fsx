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
//let root = @"file://C:\Tomas\Projects\FSharp.Data\docs"


// Generate HTML from all FSX files in samples & subdirectories
let build () =
  for sub in [ "."  ] do
    Literate.ProcessDirectory
      ( sources ++ sub, template, output ++ sub, 
        replacements = [ "root", root ] )

build()