open FSharp.Charting.Core

/// Adapted by Sergei Lopatin from Syme, Don. Expert F# 4.0. Apress. Kindle Edition. 
[<EntryPoint>]
let main argv =
    let rnd = System.Random()
    let rand() = rnd.NextDouble()
    let randomPoints = [for i in 0 .. 10000 -> 10.0 * rand(), 10.0 * rand()]
    randomPoints |> Chart.FastPoint |> Chart.Show
    0
