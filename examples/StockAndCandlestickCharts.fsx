(** 
# FSharp.Charting: Stock and Candlestick Charts

*Summary:* This example shows how to create stock and candlestick charts to visualize financial data in F#. 
It looks at how to draw charts, use dates as labels, and specify the range of a chart.

Stock and candlestick charts are designed to visualize stock prices. The data recorded about 
stock prices typically consists of four values representing High, Low, Open, and Close prices. 
The first two are the maximum and minimum price of the stock reached during the day and the 
last two are prices at the start and the end of the day. The examples use the following data 
set (price of MSFT stocks over 10 days):

*)

let prices =
  [ 26.24,25.80,26.22,25.95; 26.40,26.18,26.26,26.20
    26.37,26.04,26.11,26.08; 26.78,26.15,26.60,26.16
    26.86,26.51,26.69,26.58; 26.95,26.50,26.91,26.55
    27.06,26.50,26.64,26.77; 26.86,26.43,26.53,26.59
    27.10,26.52,26.78,26.59; 27.21,26.99,27.13,27.06
    27.37,26.91,26.97,27.21; 27.07,26.60,27.05,27.02
    27.33,26.95,27.04,26.96; 27.27,26.95,27.21,27.23
    27.81,27.07,27.76,27.25; 27.94,27.29,27.93,27.50
    28.26,27.91,28.19,27.97; 28.34,28.05,28.10,28.28
    28.34,27.79,27.80,28.20; 27.84,27.51,27.70,27.77 ]

(**
More information about working with financial data and how to download 
stock prices from the Yahoo Finance portal using F# can be found in [Try F#](http://tryfsharp.org).

Figure 1 visualizes the data set using a candlestick chart.

<div>
    <img src="images/IC523409.png" alt="Sample Financial Chart">
</div>


A stock or a candlestick chart can be created using the `FSharpChart.Stock` and `FSharpChart.Candlestick` methods. Financial charts for
visualizing stocks require four values for drawing each data point (High, Low, Open, and Close price). 
When calling the methods, it is possible to specify the values as a collection 
containing four-element tuples, or five-element tuples (Date, High, Low, Open, and Close price).

*)

// On Mac OSX use packages/FSharp.Charting.Gtk.0.90.6/FSharp.Charting.Gtk.fsx
#load "packages/FSharp.Charting.0.90.6/FSharp.Charting.fsx"

open FSharp.Charting
open System

Chart.Stock(prices)

let pricesWithDates = 
    prices |> List.mapi (fun i (hi,lo,op,cl) -> 
        (DateTime.Today.AddDays(float i).ToShortDateString(), hi, lo, op, cl))

// Candlestick chart price range specified
Chart.Candlestick(pricesWithDates).WithYAxis(Max = 29.0, Min = 25.0)

// Alternative specification using pipelining
Chart.Candlestick(pricesWithDates)
   |> Chart.WithYAxis(Max = 29.0, Min = 25.0)

(**

When using F# Interactive, each of these examples needs to be evaluated separately. This way, F# Interactive invokes a handler that automatically shows the created chart.

The first snippet calls the `Chart.Stock` method with a list containing prices as
four-element tuples. 

The second example adds dates as the labels for the chart. 
This is done using the `List.mapi` function. The lambda function used as an argument returns a tuple 
containing the date and the original tuple of prices. 
The example also demonstrates how to set the price range of the Y axis using the method `WithYAxis`.

The last example shows an elegant way of configuring the Y axis range by using pipelining (the `|>` operator).
The chart specification is passed to a configuration method `Chart.WithYAxis` using pipelining.
This method takes named parameters that allow us to specify the range and other properties of the axis.

*)

