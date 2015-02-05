module Formatters
#I "../../packages/FSharp.Formatting/lib/net40"
#r "FSharp.Markdown.dll"
#r "FSharp.Literate.dll"
#r "../../packages/FAKE/tools/FakeLib.dll"
#load "../../bin/FSharp.Charting.fsx"

open Fake
open System.IO
open FSharp.Literate
open FSharp.Markdown
open FSharp.Charting

// --------------------------------------------------------------------------------------
// Helper functions etc.
// --------------------------------------------------------------------------------------

open System.Windows.Forms
open FSharp.Charting.ChartTypes

/// Reasonably nice default style for charts
let chartStyle ch =
  let grid = ChartTypes.Grid(LineColor=System.Drawing.Color.LightGray)
  ch 
  |> Chart.WithYAxis(MajorGrid=grid)
  |> Chart.WithXAxis(MajorGrid=grid)

// --------------------------------------------------------------------------------------
// Build FSI evaluator
// --------------------------------------------------------------------------------------

/// Builds FSI evaluator that can render System.Image, F# Charts, series & frames
let createFsiEvaluator root output =

  /// Counter for saving files
  let imageCounter = 
    let count = ref 0
    (fun () -> incr count; !count)

  let transformation (value:obj, typ:System.Type) =
    match value with 
    | :? System.Drawing.Image as img ->
        // Pretty print image - save the image to the "images" directory 
        // and return a DirectImage reference to the appropriate location
        let id = imageCounter().ToString()
        let file = "chart" + id + ".png"
        ensureDirectory (output @@ "images")
        img.Save(output @@ "images" @@ file, System.Drawing.Imaging.ImageFormat.Png) 
        Some [ Paragraph [DirectImage ("Chart", (root + "/images/" + file, None))]  ]

    | :? ChartTypes.GenericChart as ch ->
        // Pretty print F# Chart - save the chart to the "images" directory 
        // and return a DirectImage reference to the appropriate location
        let id = imageCounter().ToString()
        let file = "chart" + id + ".png"
        ensureDirectory (output @@ "images")
      
        // We need to reate host control, but it does not have to be visible
        ( use ctl = new ChartControl(chartStyle ch, Dock = DockStyle.Fill, Width=500, Height=300)
          ch.CopyAsBitmap().Save(output @@ "images" @@ file, System.Drawing.Imaging.ImageFormat.Png) )
        Some [ Paragraph [DirectImage ("Chart", (root + "/images/" + file, None))]  ]

    | _ -> None 
    
  // Create FSI evaluator, register transformations & return
  let fsiEvaluator = FsiEvaluator()
  fsiEvaluator.RegisterTransformation(transformation)
  fsiEvaluator