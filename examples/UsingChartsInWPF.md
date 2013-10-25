
# Using Charts in WPF applications

Charts specified using FSharp.Charting can be used within WPF applications.

To host a chart object inside a WPF application, use a [WindowsFormHost](http://msdn.microsoft.com/en-us/library/ms751761.aspx) element.
In your XAML code this will appear aas follows:

    <WindowsFormsHost x:Name="WinForm" />

Yo umay need to add a reference to WindowsFormsIngegration.dll.

In your application code, set the Child property of the host to a chart control created from a chart specification:

    open FSharp.Charting
    open FSharp.Charting.ChartTypes
    
    winForm.Child <- new ChartControl(Chart.Line [ for i in 0 .. 10 -> (i,i*i) ])

Live and Incremental updating charts can also be used in this way.

