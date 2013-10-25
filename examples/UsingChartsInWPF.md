# Using Charts in WPF applications

Charts specified using FSharp.Charting can be used within WPF applications.

To host a chart object inside a WPF application, use a [WindowsFormHost](http://msdn.microsoft.com/en-us/library/ms751761.aspx) element.
In your XAML code this will appear aas follows:

    <WindowsFormsHost x:Name="WinForm" />

Yo umay need to add a reference to WindowsFormsIngegration.dll.

In your application code, set the Child property of the host to a chart control created from a chart specification:

    open FSharp.Charting
    open FSharp.Charting.ChartTypes
    
    ...
    
       let winForm = window.Root.FindName("WinForm") :?> WindowsFormsHost

       let chart = Chart.Line [ for i in 0 .. 10 -> (i,i*i) ]
       
       winForm.Child <- new ChartControl(chart)


Live and Incremental updating charts can also be used in this way.

       let dataStream = 
           window.Root.MouseMove 
           |> Event.map (fun x -> let p = x.GetPosition(window.Root) in p.X,p.Y )
       
       let chart = LiveChart.LineIncremental (dataStream)
       
       winForm.Child <- new ChartControl(chart)
