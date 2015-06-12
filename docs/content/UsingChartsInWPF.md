# Using Charts in WPF applications

Charts specified using F# Charting can be used within WPF applications.

To host a chart object inside a WPF application, 

 * Add a reference to the FSharp.Charting nuget package
 * Add  references to the following additional DLLs from the base class library:
     * System.Drawing.dll
     * System.Windows.Forms.dll
     * WindowsFormsIntegration.dll
 * Add a [WindowsFormHost](http://msdn.microsoft.com/en-us/library/ms751761.aspx) element to your XAML in your F# or C# application and give the element a name
 
In your XAML code this should appear as ```<WindowsFormsHost x:Name="WinForm" />```

In your application code, set the `Child` property of the host to a chart control created from a chart specification:

<div><pre>
  open FSharp.Charting
  open FSharp.Charting.ChartTypes
    
  // ...
    
  let winForm = window.Root.FindName("WinForm") :?> WindowsFormsHost

  let chart = Chart.Line [ for i in 0 .. 10 -> (i,i*i) ]
       
  winForm.Child <- new ChartControl(chart)
</pre></div>


Live and Incremental updating charts can also be used in this way.

<div><pre>
  let dataStream = 
      window.Root.MouseMove 
      |> Event.map (fun x -> let p = x.GetPosition(window.Root) in p.X,p.Y )
       
  let chart = LiveChart.LineIncremental (dataStream)
      
  winForm.Child <- new ChartControl(chart)
</pre></div>
