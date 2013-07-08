module FSharp.Charting.Tests.Chart

(*
#load "../bin/FSharp.Charting.fsx"
*)

open NUnit.Framework
open FSharp.Charting
open FSharp.Charting.ChartTypes
open FsUnit


[<Test>]
let ``Test that tests exist``() = 
    1 |> should equal 1

[<Test>]
// This test just tests that some simple chart specifications compile, giving some coverage of the overloads.
let ``Test that chart specifications compile``() = 
    let check (c:GenericChart) = c.CopyAsBitmap() |> ignore
    //let check (c:GenericChart) = c.ShowChart() |> ignore
    Chart.Area [ 0 .. 10 ] |> check 
    Chart.Area [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Bar [ 0 .. 10 ] |> check 
    Chart.Bar [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Bubble [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Bubble [ for x in 0 .. 10 -> x, x*x, x*x ] |> check 
    Chart.Column [ 0 .. 10 ] |> check 
    Chart.Column [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Doughnut [ 0 .. 10 ] |> check 
    Chart.Doughnut [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.ErrorBar [ for x in 0 .. 10 -> x, x, x-1, x+1 ] |> check 
    Chart.FastLine [ 0 .. 10 ] |> check 
    Chart.FastLine [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.FastPoint [ 0 .. 10 ] |> check 
    Chart.FastPoint [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Funnel [ 0 .. 10 ] |> check 
    Chart.Funnel [ for x in 0 .. 10 -> x, x*x ] |> check 
    
    // Bug: it is really hard to see the data here, see https://github.com/fsharp/FSharp.Charting/issues/14
    Chart.Kagi [ 1.1; 3.1; 4.1; 5.1; 3.1; 2.1;  ] |> check 
    Chart.Kagi [ for x in 0 .. 10 -> float x + 0.1, x*x ] |> check 
    
    Chart.Line [ 0 .. 10 ] |> check 
    Chart.Line [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Pie [ 0 .. 10 ] |> check 
    Chart.Pie [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Point [ 0 .. 10 ] |> check 
    Chart.Point [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.PointAndFigure [ for x in 0 .. 10 -> cos (float x), sin (float x) ] |> check 
    Chart.PointAndFigure [ for x in 0 .. 10 -> x, cos (float x), sin (float x) ] |> check 
    Chart.Polar [ 0 .. 10 ] |> check 
    Chart.Polar [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Pyramid [ 0 .. 10 ] |> check 
    Chart.Pyramid [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Radar [ 0 .. 10 ] |> check 
    Chart.Radar [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Range [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Range [ for x in 0 .. 10 -> x, x-1, x+1 ] |> check 
    Chart.RangeBar [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.RangeBar [ for x in 0 .. 10 -> x, x-1, x+1 ] |> check 
    Chart.RangeColumn [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.RangeColumn [ for x in 0 .. 10 -> x, x-1, x+1 ] |> check 
    Chart.Renko [ 0 .. 10 ] |> check 
    Chart.Renko [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.Spline [ 0 .. 10 ] |> check 
    Chart.Spline [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.SplineArea [ 0 .. 10 ] |> check 
    Chart.SplineArea [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.SplineRange [ for x in 0 .. 10 -> x, x*x ] |> check 
    Chart.SplineRange [ for x in 0 .. 10 -> x, x*x, x+4 ] |> check 
    Chart.StackedArea [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] |> check 
    Chart.StackedArea100 [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] |> check 
    Chart.StackedBar [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] |> check 
    Chart.StackedBar100 [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] |> check 
    Chart.StackedColumn [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] |> check 
    Chart.StackedColumn100 [ [ for x in 0 .. 10 -> x, x]; [ for x in 0 .. 10 -> x, x*x] ] |> check 
    Chart.StepLine [for x in 0 .. 10 -> x, x] |> check 
    Chart.BoxPlotFromStatistics( 
            [ ("Result A", -12.7, 11.6, -8.3, 6.4, 0.0, 0.0);
              ("Result B", -6.7, 11.6, -5.0, 5.4, 0.0, 0.0) ]) |> check

    let date n = System.DateTime.Today.AddDays(float n).ToShortDateString()
    let rnd = new System.Random()

    let threeSyntheticDataSets = 
        [ (date 0, [| for i in 0 .. 20 -> float (rnd.Next 20) |])
          (date 1, [| for i in 0 .. 20 -> float (rnd.Next 15 + 2) |])
          (date 2, [| for i in 0 .. 20 -> float (rnd.Next 10 + 5) |]) ]

    Chart.BoxPlotFromData
        ( threeSyntheticDataSets,
        ShowUnusualValues = true, ShowMedian = false,
        ShowAverage = false, WhiskerPercentile = 10) |> check

    Chart.Candlestick [for x in 0 .. 10 -> x+10, x-10, x+5, x-5 ] |> check 
    Chart.Stock [for x in 0 .. 10 -> x+10, x-10, x+5, x-5 ] |> check 
