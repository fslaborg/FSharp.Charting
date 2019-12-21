module FSharpChartingCoreTest

open NUnit.Framework
open FsUnit

open System.Threading
open FSharp.Charting.Core
open FSharp.Charting.Core.ChartTypes

[<SetUp>]
let Setup () = ()

[<Test>]
let ``Test that chart tests exist``() = 
    1 |> should equal 1

// This test just tests that some simple chart specifications compile, giving some coverage of the overloads.
[<Test>]
let ``Test that specifications compile and work``() = 
    //let check (c:GenericChart) = c.CopyAsBitmap() |> ignore
    
    let mutable forms = []
    let check (c:GenericChart) = do let form = c.ShowChart() in forms <- form :: forms
    let checkAndWait (c:GenericChart) = Chart.Show c
    let closeForms () = for form in forms do Thread.Sleep 555; form.Close()
    
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
    
    // Bug: it is really hard to see the data here, see https://github.com/fslaborg/FSharp.Charting/issues/14
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

    //Basic tests on a couple of charts that IsMarginVisible can be set on all axes.
    let checkChartWithMarginVisible chart visible =
        chart
        |> Chart.WithXAxis (IsMarginVisible = visible)
        |> Chart.WithYAxis (IsMarginVisible = visible)
        |> Chart.WithXAxis2 (IsMarginVisible = visible)
        |> Chart.WithYAxis2 (IsMarginVisible = visible)
        |> check

    let checkChartWithBothMarginVisibilities chart = 
        checkChartWithMarginVisible chart true
        checkChartWithMarginVisible chart false

    checkChartWithBothMarginVisibilities (Chart.Line [ 0 .. 10 ])
    checkChartWithBothMarginVisibilities (Chart.Point [ 0 .. 10 ])

    let checkSave chart filename =
        System.IO.File.Exists(filename)
        |> should equal false
        chart
        |> Chart.Save filename
        System.IO.File.Exists(filename)
        |> should equal true
        System.IO.File.Delete(filename)

    checkSave (Chart.Line [ 0 .. 10 ]) "chart.png"
    
    let rnd = System.Random()
    let rand() = rnd.NextDouble()
    let randomPoints = [for i in 0 .. 1000 -> 10.0 * rand(), 10.0 * rand()]
    randomPoints
    |> Chart.Point
    |> Chart.WithTitle "Please close this chart manually \n Other charts will close automatically"
    |> checkAndWait
    closeForms()
