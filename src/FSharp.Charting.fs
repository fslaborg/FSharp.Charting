//----------------------------------------------------------------------------
// Copyright (c) 2002-2013 Microsoft Corporation
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.html file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
//
// Originally based on multiple files from fsharppowerpack.codeplex.com.
//
// Modified by Tomas Petricek and other contributors under the Apache 2.0 License
//----------------------------------------------------------------------------

// --------------------------------------------------------------------------------------
// Charting API for F# 
//
// See 'examples' for how to use this DLL.
// --------------------------------------------------------------------------------------

#nowarn "40"

namespace FSharp.Charting

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Collections.Specialized
    open System.Collections.ObjectModel
    open System.Drawing
    open System.Reflection
    open System.Runtime.InteropServices
    open System.Threading
    open System.Windows.Forms
    open System.Windows.Forms.DataVisualization
    open System.Windows.Forms.DataVisualization.Charting

    module private ClipboardMetafileHelper =
        [<DllImport("user32.dll")>]
        extern bool OpenClipboard(nativeint _hWndNewOwner)
        [<DllImport("user32.dll")>]
        extern bool EmptyClipboard()
        [<DllImport("user32.dll")>]
        extern IntPtr SetClipboardData(uint32 _uFormat, nativeint _hMem)
        [<DllImport("user32.dll")>]
        extern bool CloseClipboard()
        [<DllImport("gdi32.dll")>]
        extern nativeint CopyEnhMetaFile(nativeint _hemfSrc, nativeint _hNULL)
        [<DllImport("gdi32.dll")>]
        extern bool DeleteEnhMetaFile(IntPtr _hemf)
    
        // Metafile mf is set to a state that is not valid inside this function.
        let PutEnhMetafileOnClipboard(hWnd, mf : System.Drawing.Imaging.Metafile) =
            let mutable bResult = false
            let hEMF = mf.GetHenhmetafile() // invalidates mf
            if (hEMF <> 0n) then
                let hEMF2 = CopyEnhMetaFile(hEMF, 0n)
                if (hEMF2 <> 0n) then
                    if OpenClipboard(hWnd) && EmptyClipboard() then
                        let hRes = SetClipboardData( 14u (*CF_ENHMETAFILE*), hEMF2 );
                        bResult <- hRes = hEMF2
                        CloseClipboard() |> ignore
                DeleteEnhMetaFile( hEMF ) |> ignore
            bResult

    type private INotifyEnumerableInternal<'T> =
        inherit IEnumerable<'T>
        inherit INotifyCollectionChanged

    module private Seq = 
        /// Evaluate once only. Unlike Seq.cache this evaluates once to an array all at once.
        let once (source: seq<'T>) = 
            let data = lazy Seq.toArray source
            { new IEnumerable<'T> with
                  member x.GetEnumerator() = (data.Force() :> seq<'T>).GetEnumerator() 
              interface IEnumerable with
                  member x.GetEnumerator() = (data.Force() :> IEnumerable).GetEnumerator() }

    module private NotifySeq = 
        /// Returns an IEnumerator that supports Reset without failing.
        /// The Reset method is called by the .NET charting library.
        let ignoreResetEnumeratorG (enum : IEnumerator<'T>) =
            { new IEnumerator<'T> with 
                  member x.Current = enum.Current 
              interface IEnumerator with 
                  member x.Current = (enum :> IEnumerator).Current 
                  member x.MoveNext() = enum.MoveNext()
                  member x.Reset() = ()
              interface IDisposable with 
                  member x.Dispose() = enum.Dispose() }

        /// Returns an IEnumerator that supports Reset without failing.
        /// The Reset method is called by the .NET charting library.
        let ignoreResetEnumerator (enum : IEnumerator) =
            { new IEnumerator with 
                  member x.Current = enum.Current 
                  member x.MoveNext() = enum.MoveNext()
                  member x.Reset() = () }

        /// Returns an INotifyEnumerableInternal that supports Reset without failing.
        /// The Reset method is called by the .NET charting library.
        let ignoreReset (obs : INotifyEnumerableInternal<'T>) =
            { new obj() with 
                  member x.ToString() = "INotifyEnumerableInternal"
              interface INotifyEnumerableInternal<'T> 
              interface IEnumerable<'T> with 
                  member x.GetEnumerator() = (obs :> IEnumerable<_>).GetEnumerator() |> ignoreResetEnumeratorG
              interface IEnumerable with 
                  member x.GetEnumerator() = (obs :> IEnumerable).GetEnumerator() |> ignoreResetEnumerator
              interface INotifyCollectionChanged with 
                  member x.add_CollectionChanged(h) = obs.add_CollectionChanged(h)
                  member x.remove_CollectionChanged(h) = obs.remove_CollectionChanged(h) }

        let noNotify (obs : seq<_>) =
            { new obj() with 
                  member x.ToString() = "INotifyEnumerableInternal"
              interface INotifyEnumerableInternal<'T> 
              interface IEnumerable<'T> with 
                  member x.GetEnumerator() = (obs :> IEnumerable<_>).GetEnumerator() 
              interface IEnumerable with 
                  member x.GetEnumerator() = (obs :> IEnumerable).GetEnumerator() 
              interface INotifyCollectionChanged with 
                  member x.add_CollectionChanged(h) = ()
                  member x.remove_CollectionChanged(h) = () }

        /// Zip two notifying IEnumerables and propagate notifications.
        let zip (obs1 : INotifyEnumerableInternal<'T>) (obs2 : INotifyEnumerableInternal<'U>) =
            let obs = Seq.zip obs1 obs2 
            { new obj() with 
                  member x.ToString() = "INotifyEnumerableInternal"
              interface INotifyEnumerableInternal<'T * 'U> 
              interface IEnumerable<'T * 'U> with 
                  member x.GetEnumerator() = obs.GetEnumerator() 
              interface IEnumerable with 
                  member x.GetEnumerator() = (obs :> IEnumerable).GetEnumerator() 
              interface INotifyCollectionChanged with 
                  member x.add_CollectionChanged(h) = obs1.add_CollectionChanged(h); obs2.add_CollectionChanged(h) 
                  member x.remove_CollectionChanged(h) = obs1.remove_CollectionChanged(h); obs2.remove_CollectionChanged(h) }

        /// Map over a notifying IEnumerable and propagate notifications.
        let map f (obs : INotifyEnumerableInternal<'T>) =
            let newObs = Seq.map f obs
            { new obj() with 
                  member x.ToString() = "INotifyEnumerableInternal"
              interface INotifyEnumerableInternal<'U> 
              interface IEnumerable<'U> with 
                  member x.GetEnumerator() = (newObs :> IEnumerable<_>).GetEnumerator() 
              interface IEnumerable with 
                  member x.GetEnumerator() = (newObs :> IEnumerable).GetEnumerator() 
              interface INotifyCollectionChanged with 
                  member x.add_CollectionChanged(h) = obs.add_CollectionChanged(h)
                  member x.remove_CollectionChanged(h) = obs.remove_CollectionChanged(h) }


        /// Map over a notifying IEnumerable and propagate notifications.
        let mapi f (obs : INotifyEnumerableInternal<'T>) =
            let newObs = Seq.mapi f obs
            { new obj() with 
                  member x.ToString() = "INotifyEnumerableInternal"
              interface INotifyEnumerableInternal<'U> 
              interface IEnumerable<'U> with 
                  member x.GetEnumerator() = (newObs :> IEnumerable<_>).GetEnumerator() 
              interface IEnumerable with 
                  member x.GetEnumerator() = (newObs :> IEnumerable).GetEnumerator() 
              interface INotifyCollectionChanged with 
                  member x.add_CollectionChanged(h) = obs.add_CollectionChanged(h)
                  member x.remove_CollectionChanged(h) = obs.remove_CollectionChanged(h) }

        /// Convert a sequence to an INotifyEnumerableInternal. If the sequence already implicitly supports
        /// notifications then just wrap and propagate those, otherwise evaluate the sequence once and use fixed data
        /// with no notifications.
        let notifyOrOnce (obs:seq<_>) =       
            match box obs with 
            | :? INotifyCollectionChanged as n -> 
              { new obj() with 
                    member x.ToString() = "INotifyEnumerableInternal"
                interface INotifyEnumerableInternal<'T> 
                interface IEnumerable<_> with 
                    member x.GetEnumerator() = (obs :> IEnumerable<_>).GetEnumerator() 
                interface IEnumerable with 
                    member x.GetEnumerator() = (obs :> IEnumerable).GetEnumerator() 
                interface INotifyCollectionChanged with 
                    member x.add_CollectionChanged(h) = n.add_CollectionChanged(h)
                    member x.remove_CollectionChanged(h) = n.remove_CollectionChanged(h) }
            | _ -> noNotify (Seq.once obs)

        /// Convert an ObservableCollection to an INotifyEnumerableInternal. 
        let ofObservableCollection (obs:ObservableCollection<'T>) = 
            { new obj() with 
                  member x.ToString() = "INotifyEnumerableInternal"
              interface INotifyEnumerableInternal<'T> 
              interface IEnumerable<'T> with 
                  member x.GetEnumerator() = (obs :> IEnumerable<'T>).GetEnumerator()
              interface IEnumerable with 
                  member x.GetEnumerator() = (obs :> IEnumerable).GetEnumerator()
              interface INotifyCollectionChanged with 
                  [<CLIEvent>]
                  member x.CollectionChanged = (obs :> INotifyCollectionChanged).CollectionChanged }

        let replacing () = 
            let curr = ref [| |]
            let ev = Event<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>()
            let coll = 
                { new obj() with 
                      member x.ToString() = "INotifyEnumerableInternal from Seq.ofObservableReplacing"
                  interface INotifyEnumerableInternal<'T> 
                  interface IEnumerable<'T> with 
                      member x.GetEnumerator() = (curr.Value :> IEnumerable<'T>).GetEnumerator()
                  interface IEnumerable with 
                      member x.GetEnumerator() = (curr.Value :> IEnumerable).GetEnumerator()
                  interface INotifyCollectionChanged with 
                      [<CLIEvent>]
                      member x.CollectionChanged = ev.Publish }
            let update elems = 
                let evArgs = NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)  // "dramatic change"
                curr := elems; ev.Trigger(curr,evArgs)
            coll, update

        type IObservable<'T> with 
            member source.ObserveOn(ctxt: SynchronizationContext) = 
              { new IObservable<_> with
                  member x.Subscribe(observer) =
                    source.Subscribe
                      ({ new IObserver<_> with
                          member x.OnNext(v) = ctxt.Post(SendOrPostCallback(fun _ -> observer.OnNext(v)), null)
                          member x.OnCompleted() = ctxt.Post(SendOrPostCallback(fun _ -> observer.OnCompleted()),null) 
                          member x.OnError(e) = ctxt.Post(SendOrPostCallback(fun _ -> observer.OnError(e)),null) }) }
                

        // TODO: only start on connect + proper replay + disconnect, OR use weak connection to source
        let ofObservableIncremental (source:IObservable<'T>) : INotifyEnumerableInternal<'T> = 
            let obs = new ObservableCollection<'T>()
            source.ObserveOn(SynchronizationContext.Current) |> Observable.add (fun x -> obs.Add(x))
            ofObservableCollection obs

        // TODO: only start on connect + proper replay + disconnect, OR use weak connection to source
        let ofObservableReplacing (source:IObservable<#seq<'T>>) : INotifyEnumerableInternal<'T> = 
            let coll, update = replacing ()
            source.ObserveOn(SynchronizationContext.Current) |> Observable.add (fun elems -> update (Seq.toArray elems))
            coll


    /// A primitive value for a point on a chart. An abbreviation for the IConvertible type.
    type value = System.IConvertible
    type key = System.IComparable

    module internal KeyConversions = 
        // Mapping from known runtime types to 'ChartValueType' that is used for the X axis
        // (we try to infer the axis type so that it is displayed nicely)
        let knownTypes = 
          dict [ typeof<int>, ChartValueType.Int32; typeof<int64>, ChartValueType.Int64
                 typeof<uint32>, ChartValueType.UInt32; typeof<uint64>, ChartValueType.UInt64
                 typeof<float>, ChartValueType.Double; typeof<float32>, ChartValueType.Single
                 typeof<string>, ChartValueType.String ]

        let private registeredConvertors = System.Collections.Generic.Dictionary<_, key -> key>()
        let tryGetCovnertor (typ:System.Type) =
          match registeredConvertors.TryGetValue(typ) with 
          | true, conv -> Some conv
          | _ -> None
        let registerConvertor<'T when 'T :> key> (f:'T -> key) =
          if knownTypes.ContainsKey(typeof<'T>) then 
            invalidOp "Registering convertors for known primitive types (int, string, DateTime, ...) is not allowed."
          registeredConvertors.Add(typeof<'T>, fun arg -> f (arg :?> 'T))
        
        do registerConvertor (fun (dto:DateTimeOffset) -> dto.DateTime :> key)

    module ChartTypes = 

        /// An implementation of a histogram bin.
        type Bin = { LowerBound: float; UpperBound: float; Count: int}

        /// An implementation type for labelled points on chart. This type should not be used directly.
        type public DataPoint(X: key, Y: value, Label: string) = 
            member __.Label = Label 
            member __.X = X 
            member __.Y = Y 

        /// An implementation type for labelled points on chart. This type should not be used directly.
        type public TwoXYDataPoint(X: key, Y1: value, Y2: value, Label: string) = 
            member __.Label = Label 
            member __.X = X 
            member __.Y1 = Y1 
            member __.Y2 = Y2 

        /// An implementation type for labelled points on chart. This type should not be used directly.
        type public ThreeXYDataPoint(X: key, Y1: value, Y2: value, Y3: value, Label: string) = 
            member __.Label = Label 
            member __.X = X 
            member __.Y1 = Y1 
            member __.Y2 = Y2 
            member __.Y3 = Y3 

        /// An implementation type for labelled points on chart. This type should not be used directly.
        type public FourXYDataPoint(X: key, Y1: value, Y2: value, Y3: value, Y4: value, Label: string) = 
            member __.Label = Label 
            member __.X =  X 
            member __.Y1 = Y1 
            member __.Y2 = Y2 
            member __.Y3 = Y3 
            member __.Y4 = Y4 

        /// An implementation type for labelled points on chart. This type should not be used directly.
        type public SixXYDataPoint(X: key, Y1: value, Y2: value, Y3: value, Y4: value, Y5: value, Y6: value, Label: string) = 
            member __.Label = Label 
            member __.X =  X 
            member __.Y1 = Y1 
            member __.Y2 = Y2 
            member __.Y3 = Y3 
            member __.Y4 = Y4 
            member __.Y5 = Y5 
            member __.Y6 = Y6 

        /// An implementation type for labelled points on chart. This type should not be used directly.
        type public DataSet(X: key, YS: IEnumerable, Label: string) = 
            member __.Label = Label 
            member __.X = X 
            member __.YS = YS

        /// Specifies the image type of the chart.
        type ChartImageFormat = 
        /// A JPEG image format.
        | Jpeg = 0
        /// A PNG image format.
        | Png = 1
        /// A bitmap (BMP) image format.
        | Bmp = 2
        /// A TIFF image format.
        | Tiff = 3
        /// A GIF image format.
        | Gif = 4
        /// A Windows Enhanced Metafile (EMF) image format.
        | Emf = 5 
        /// A Windows Enhanced Metafile Dual (EMF-dual) image format. 
        | EmfDual = 6
        /// A Windows Enhanced Metafile Plus (EMF+) image format. 
        | EmfPlus = 7

        /// An enumeration of axis arrow styles
        type AxisArrowStyle = 
        /// No arrow is used for the relevant axis.
        | None = 0
        /// A triangular arrow is used for the relevant axis.
        | Triangle = 1
        /// A sharp triangular arrow is used for the relevant axis.
        | SharpTriangle = 2
        /// A line-shaped arrow is used for the relevant axis.
        | Lines = 3

        /// Specifies a line style
        type DashStyle = 
        /// The line style is not set.
        | NotSet = 0
        /// A dashed line.
        | Dash = 1
        /// A line with a repeating dash-dot pattern.
        | DashDot = 2
        /// A line a repeating dash-dot-dot pattern.
        | DashDotDot = 3
        /// A line with a repeating dot pattern.
        | Dot = 4
        /// A solid line.
        | Solid = 5
    
        /// Specifies text orientation in a chart element
        type TextOrientation = 
        /// Text orientation is automatically determined, based on the type of chart element in which the text appears.
        | Auto = 0
        /// Text is horizontal.
        | Horizontal = 1
        /// Text is rotated 90 degrees and oriented from top to bottom.
        | Rotated90 = 2
        /// Text is rotated 270 degrees and oriented from bottom to top.
        | Rotated270 = 3
        /// Text characters are not rotated and are positioned one below the other.
        | Stacked = 4

        /// Specifies text drawing styles
        type TextStyle = 
        /// Default text drawing style
        | Default = 0
        /// Shadow text
        | Shadow = 1
        /// Embossed text
        | Emboss = 2
        /// Embedded text
        | Embed = 3
        /// Framed text
        | Frame = 4

        /// Specifies a lighting style for a three-dimensional (3D) chart area
        type LightStyle = 
        /// No lighting is applied.
        | None = 0
        /// A simplistic lighting style is applied, where the hue of all chart area elements is fixed.
        | Simplistic = 1
        /// A realistic lighting style is applied, where the hue of all chart area elements changes depending on the amount of rotation.
        | Realistic = 2
    
        /// Specifies where a chart element, such as a legend or title, will be docked on the chart
        type Docking = 
        /// Docked to the top of either the chart image or a ChartArea object
        | Top = 0
        /// Docked to the right of either the chart image or a ChartArea object
        | Right = 1
        /// Docked to the bottom of either the chart image or a ChartArea object
        | Bottom = 2
        /// Docked to the left of either the chart image or a ChartArea object
        | Left = 3

        /// Specifies a style for markers
        type MarkerStyle = 
          /// No marker is displayed for the series or data point.
          | None = 0
          /// A square marker is displayed.
          | Square = 1
          /// A circular marker is displayed.
          | Circle = 2
          /// A diamond-shaped marker is displayed.
          | Diamond = 3
          /// A triangular marker is displayed.
          | Triangle = 4
          /// A cross-shaped marker is displayed.
          | Cross = 5
          /// A 4-point star-shaped marker is displayed.
          | Star4 = 6 
          /// A 5-point star-shaped marker is displayed.
          | Star5 = 7
          /// A 6-point star-shaped marker is displayed.
          | Star6 = 8
          /// A 10-point star-shaped marker is displayed.
          | Star10 = 9

        /// Specifies an interval type
        type DateTimeIntervalType = 
        /// Automatically determined by the Chart control.
        | Auto = 0
        /// Interval type is in numerical.
        | Number = 1
        /// Interval type is in years.
        | Years = 2
        /// Interval type is in months.
        | Months = 3
        /// Interval type is in weeks.
        | Weeks = 4
        /// Interval type is in days.
        | Days = 5
        /// Interval type is in hours.
        | Hours = 6
        /// Interval type is in minutes.
        | Minutes = 7
        /// Interval type is in seconds.
        | Seconds = 8
        /// Interval type is in milliseconds.
        | Milliseconds = 9
        /// The IntervalType or IntervalOffsetType property is not set. This value is used for grid lines, tick marks, strip lines and axis labels, and indicates that the interval type is being obtained from the Axis object to which the element belongs. Setting this value for an Axis object will have no effect.
        | NotSet = 10

        /// The placement of the data point label.
        type BarLabelPosition = 
            | Outside = 0
            | Left = 1
            | Right = 2
            | Center = 3

        /// The text orientation of the axis labels in Radar
        /// and Polar charts.
        type CircularLabelStyle = 
            | Circular = 0
            | Horizontal = 1
            | Radial = 2

        /// The drawing style of data points.
        type PointStyle = 
            | Cylinder = 0
            | Emboss = 1
            | LightToDark = 2
            | Wedge = 3
            | Default = 4

        /// Specifies whether series of the same chart type are drawn
        /// next to each other instead of overlapping each other.
        type DrawSideBySide = 
            | Auto = 0
            | True = 1
            | False = 2

    (*
        /// The value to be used for empty points.
        type EmptyPointValue = 
            | Average = 0
            | Zero = 1
    *)

        /// The appearance of the marker at the center value
        /// of the error bar.
        type ErrorBarCenterMarkerStyle = 
            | None = 0
            | Line = 1
            | Square = 2
            | Circle = 3
            | Diamond = 4
            | Triangle = 5
            | Cross = 6
            | Star4 = 7
            | Star5 = 8
            | Star6 = 9
            | Star10 = 10

        /// The visibility of the upper and lower error values.
        type ErrorBarStyle = 
            | Both = 0
            | UpperError = 1
            | LowerError = 2

        /// Specifies how the upper and lower error values are calculated
        /// for the center values of the ErrorBarSeries.
        type ErrorBarType = 
            | FixedValue = 0
            | Percentage = 1
            | StandardDeviation = 2
            | StandardError = 3

        /// The 3D drawing style of the Funnel chart type.
        type Funnel3DDrawingStyle = 
            | CircularBase = 0
            | SquareBase = 1

        /// The data point label placement of the Funnel chart
        /// type when the FunnelLabelStyle is set to Inside.
        type FunnelInsideLabelAlignment = 
            | Center = 0
            | Top = 1
            | Bottom = 2

        /// The data point label style of the Funnel chart type.
        type FunnelLabelStyle = 
            | Inside = 0
            | Outside = 1
            | OutsideInColumn = 2
            | Disabled = 3

        /// Placement of the data point label in the Funnel chart
        /// when FunnelLabelStyle is set to Outside or OutsideInColumn.
        type FunnelOutsideLabelPlacement = 
            | Right = 0
            | Left = 1

        /// The style of the Funnel chart type.
        type FunnelStyle = 
            | YIsWidth = 0
            | YIsHeight = 1

        /// The label position of the data point.
        type LabelPosition = 
            | Auto = 0
            | Top = 1
            | Bottom = 2
            | Right = 3
            | Left = 4
            | TopLeft = 5
            | TopRight = 6
            | BottomLeft = 7
            | BottomRight = 8
            | Center = 9

        /// The Y value to use as the data point label.
        type LabelValueType = 
            | High = 0
            | Low = 1
            | Open = 2
            | Close = 3

        /// The marker style for open and close values.
        type OpenCloseStyle = 
            | Triangle = 0
            | Line = 1
            | Candlestick = 2

        /// The drawing style of the data points.
        type PieDrawingStyle = 
            | Default = 0
            | SoftEdge = 1
            | Concave = 2

        /// The label style of the data points.
        type PieLabelStyle = 
            | Disabled = 0
            | Inside = 1
            | Outside = 2

        /// The drawing style of the Polar chart type.
        type PolarDrawingStyle = 
            | Line = 0
            | Marker = 1

        /// The placement of the data point labels in the
        /// Pyramid chart when they are placed inside the pyramid.
        type PyramidInsideLabelAlignment = 
            | Center = 0
            | Top = 1
            | Bottom = 2

        /// The style of data point labels in the Pyramid chart.
        type PyramidLabelStyle = 
            | Inside = 0
            | Outside = 1
            | OutsideInColumn = 2
            | Disabled = 3

        /// The placement of the data point labels in the
        /// Pyramid chart when the labels are placed outside the pyramid.
        type PyramidOutsideLabelPlacement = 
            | Right = 0
            | Left = 1

        /// Specifies whether the data point value represents a linear height
        /// or the surface of the segment.
        type PyramidValueType = 
            | Linear = 0
            | Surface = 1

        /// The drawing style of the Radar chart.
        type RadarDrawingStyle = 
            | Area = 0
            | Line = 1
            | Marker = 2

        /// Specifies whether markers for open and close prices are displayed.
        type ShowOpenClose = 
            | Both = 0
            | Open = 1
            | Close = 2

        // Background helpers
        [<RequireQualifiedAccess>]
        type Background = 
            | EmptyColor
            | Gradient of Color * Color * GradientStyle
            | Solid of Color

        let inline internal applyBackground (obj:^T) back =
            match back with 
            | Background.EmptyColor ->
                (^T : (member set_BackColor : Color -> unit) (obj, Color.Empty))
            | Background.Solid color ->
                (^T : (member set_BackColor : Color -> unit) (obj, color))
            | Background.Gradient(first, second, style) ->
                (^T : (member set_BackColor : Color -> unit) (obj, first))
                (^T : (member set_BackSecondaryColor : Color -> unit) (obj, second))
                (^T : (member set_BackGradientStyle : GradientStyle -> unit) (obj, style))

        // Default font used when creating styles, titles, and legends
        let internal DefaultFontForTitles = new Font("Calibri", 16.0f, FontStyle.Regular)
        let internal DefaultFontForAxisLabels = new Font("Calibri", 12.0f, FontStyle.Regular)
        let internal DefaultFontForOthers = new Font("Arial Narrow", 10.0f, FontStyle.Regular)
        let internal DefaultFontForLegend = DefaultFontForOthers
        let internal DefaultFontForLabels = DefaultFontForOthers
        let internal DefaultExtraMarginForTitleIfPresent = 5  // default extra margin percentage
        let internal DefaultExtraMarginForLegendIfPresent = 15  // default extra margin percentage
        let internal DefaultMarginForEachChart = (2.0, 2.0, 2.0, 2.0)

        // Type used for defining defaults
        type internal ChartStyleDefault =
            { ChartType:Charting.SeriesChartType option;
              ParentType:Type option
              ParentParentType:Type option
              PropertyName:string
              PropertyDefault:obj }

        // Definition of defaults for the chart
        let internal PropertyDefaults = 
            [ // Define type specific defaults
         
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Line); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="BorderWidth"; PropertyDefault=(box 2) }
              // Default Bubbles to circles instead of squares
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Bubble ); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="MarkerStyle"; PropertyDefault=(box Charting.MarkerStyle.Circle) }
              //{ ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="IsDockedInsideChartArea"; PropertyDefault=(box false) }
              { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="BorderColor"; PropertyDefault=(box Color.Black) }
              { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="BorderWidth"; PropertyDefault=(box 1) }
              //{ ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="IsDockedInsideChartArea"; PropertyDefault=(box false) }
    (*
    #VALX	X value of the data point.	No	Yes
    #VALY	Y value of the data point	Yes	Yes
    #SERIESNAME	Series name	No	No
    #LABEL	Data point label	No	No
    #AXISLABEL	Data point axis label	No	No
    #INDEX	Data point index in the series	No	Yes
    #PERCENT	Percent of the data point Y value	Yes	Yes
    #LEGENDTEXT	Series or data point legend text	No	No
    #CUSTOMPROPERTY(XXX)	Series or data point XXX custom property value, where XXX is the name of the custom property.	No	No
    #TOTAL	Total of all Y values in the series	Yes	Yes
    #AVG	Average of all Y values in the series	Yes	Yes
    #MIN	Minimum of all Y values in the series	Yes	Yes
    #MAX	Maximum of all Y values in the series	Yes	Yes
    #FIRST	Y value of the first point in the series	Yes	Yes
    #LAST	Y value of the last point in the series	Yes	Yes

    Objects and Properties where keywords can be used
 
    Series and DataPoint
    •Label 
    •AxisLabel 
    •ToolTip 
    •Url 
    •MapAreaAttributes 
    •PostBackValue 
    •LegendToolTip 
    •LegendMapAreaAttributes 
    •LegendPostBackValue 
    •LegendUrl 
    •LegendText 
    •LabelToolTip 

    Annotation (only if anchored to the data point using SetAnchor method)
    •ToolTip 
    •Url 
    •MapAreaAttributes 
    •PostBackValue 
    •Text (TextAnnotation)
 

    LegendCellColumn (only for legend items automatically created for series or data points): 
    •Text 
    •Tooltip 
    •Url 
    •MapAreaAttributes 
    •PostBackValue
    *)
          
              // Define series ToolTip defaults
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Line); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Spline); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Bar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Column); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Area); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedBar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedColumn); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedArea); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedBar100); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedColumn100); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedArea100); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.SplineArea); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Range); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME X=#VALX, High=#VALY1, Low=#VALY2") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.RangeBar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME Y=#VALX, High=#VALY1, Low=#VALY2") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.RangeColumn); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME X=#VALX, High=#VALY1, Low=#VALY2") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.SplineRange); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME X=#VALX, High=#VALY1, Low=#VALY2") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Point); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.PointAndFigure); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME X=#VALX, High=#VALY1, Low=#VALY2") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.ThreeLineBreak); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StepLine); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Pie); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#VALX: #VAL") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Doughnut); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#VALX: #VAL") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.BoxPlot); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME Lower Whisker=#VALY1, Upper Whisker=#VALY2, Lower Box=#VALY3, Upper Box=#VALY4") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Candlestick); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME High=#VALY1, Low=#VALY2, Open=#VALY3, Close=#VALY4") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Stock); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME High=#VALY1, Low=#VALY2, Open=#VALY3, Close=#VALY4") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Renko); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Bubble); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)Y1, Size=#VALY2") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.ErrorBar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)Y1, Lower=#VALY2, Upper=#VALY3") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Funnel); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Pyramid); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Kagi); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME (#VALX, #VAL)") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Polar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME Angle=#VALX, Distance=#VAL") }
              { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Radar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#SERIESNAME Point=#VALX, Distance=#VAL") }
              // Define global defaults for fonts
              { ChartStyleDefault.ChartType = None; ParentParentType=Some(typeof<Charting.Axis>); ParentType = Some(typeof<Charting.LabelStyle>); PropertyName="Font"; PropertyDefault=(box DefaultFontForAxisLabels) }
              { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Title>); PropertyName="Font"; PropertyDefault=(box DefaultFontForTitles) }
              { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Title>); PropertyName="Font"; PropertyDefault=(box DefaultFontForTitles) }
              { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="Font"; PropertyDefault=(box DefaultFontForLegend) }
            ]

        let internal typesToClone = 
            [ typeof<Charting.LabelStyle>;
              typeof<Charting.Axis>;
              typeof<Charting.Grid>; 
              typeof<Charting.TickMark>
              typeof<Charting.ElementPosition>; 
              typeof<Charting.AxisScaleView>; 
              typeof<Charting.AxisScrollBar>; 
             ]

        let internal typesToCopy = [ typeof<Font>; typeof<String> ]

        let internal applyDefaults (chartType:SeriesChartType, target:'a, targetParentType:Type option, targetType:Type, property:PropertyInfo) = 
            let isMatch propDefault = 
                String.Equals(propDefault.PropertyName, property.Name) &&
                (propDefault.ChartType |> Option.forall (fun seriesType -> chartType = seriesType)) &&
                (propDefault.ParentType |> Option.forall (fun parentType -> targetType.IsAssignableFrom(parentType) || targetType.IsSubclassOf(parentType))) &&
                (propDefault.ParentParentType |> Option.forall (fun parentParentType -> targetParentType |> Option.exists (fun t -> t.IsAssignableFrom(parentParentType) || t.IsSubclassOf(parentParentType))))

            match List.tryFind isMatch PropertyDefaults with
            | Some item -> property.SetValue(target, item.PropertyDefault, [||])
            | _ -> ()

        let internal applyPropertyDefaults (chartType:SeriesChartType) (target:'a) = 
            let visited = new System.Collections.Generic.Dictionary<_, _>()
            let rec layoutSubCharts targetParent target = 
                if not (visited.ContainsKey target) then
                    visited.Add(target, true)
                    let targetParentType = match targetParent with None -> None | Some v -> Some (v.GetType())
                    let targetType = target.GetType()
                    for property in targetType.GetProperties(BindingFlags.Public ||| BindingFlags.Instance) do
                        if property.CanRead then
                            if typesToClone |> Seq.exists ((=) property.PropertyType) then
                                layoutSubCharts (Some target) (property.GetValue(target, [||]))
                            elif property.CanWrite then
                                if property.PropertyType.IsValueType || typesToCopy |> Seq.exists ((=) property.PropertyType) then
                                    applyDefaults (chartType, target, targetParentType, targetType, property)
            layoutSubCharts None target

        let internal applyProperties (target:'a) (source:'a) = 
            let visited = new System.Collections.Generic.Dictionary<_, _>()
            let rec layoutSubCharts target source = 
                if not (visited.ContainsKey target) then
                    visited.Add(target, true)
                    let ty = target.GetType()
                    for p in ty.GetProperties(BindingFlags.Public ||| BindingFlags.Instance) do
                        if p.CanRead then
                            if typesToClone |> Seq.exists ((=) p.PropertyType) then
                                layoutSubCharts (p.GetValue(target, [||])) (p.GetValue(source, [||]))
                            elif p.CanWrite then
                                if p.PropertyType.IsValueType || typesToCopy |> Seq.exists ((=) p.PropertyType) then
                                    if p.GetSetMethod().GetParameters().Length <= 1 then
                                        p.SetValue(target, p.GetValue(source, [||]), [||])
            layoutSubCharts target source

        let internal createCounter() = 
            let count = ref 0
            (fun () -> incr count; !count) 

        [<RequireQualifiedAccess>]
        type internal ChartData =
            // general binding
            | Values of IEnumerable * ChartValueType * string * string * string
            | BoxPlotValues of seq<string * value[]>
            // stacked values
            | StackedXYValues of seq<IEnumerable * IEnumerable>

        let internal binData data lowerBound upperBound intervals =
            seq{lowerBound .. (upperBound - lowerBound)/intervals .. upperBound }
            |> Seq.pairwise
            |> Seq.map (fun (l,u) -> 
                                    let cnt = data |> Seq.filter (fun e -> e >= l && e < u) |> Seq.length
                                    {LowerBound = l; UpperBound = u; Count = cnt}
                        )

        // ----------------------------------------------------------------------------------
        // Utilities for working with enumerable and tuples

        let internal seqmap f (source: seq<'T>) = 
            { new IEnumerable with
                  member x.GetEnumerator() = 
                      let en = source.GetEnumerator()
                      { new IEnumerator with 
                          member x.Current = box (f en.Current)
                          member x.MoveNext() = en.MoveNext()
                          member x.Reset() = en.Reset() } }


        // Converts Y value of a chart (defines the type too)
        let culture = System.Globalization.CultureInfo.InvariantCulture

        let valueToDouble (x:value) = x.ToDouble(culture)

        open System.Collections.Specialized
        
        let private convertKeys (selector:_ -> key) (transform:(key -> key) -> _) data = 
          let hasConvertor = ref None
          let convertor = ref None
          data |> NotifySeq.map (fun v ->
            if hasConvertor.Value.IsNone then
              convertor := KeyConversions.tryGetCovnertor ((selector v).GetType())
              hasConvertor := Some(convertor.Value.IsSome)
            match convertor.Value with
            | Some conv -> transform conv v
            | _ -> v)

        let private convertKeys1of2 data = 
          data |> NotifySeq.map (fun (k, v) -> k :> key, v)          
               |> convertKeys fst (fun kf (k, v) -> kf k, v) 
        let private convertKeys1of3 data = 
          data |> NotifySeq.map (fun (k, v1, v2) -> k :> key, v1, v2)          
               |> convertKeys (fun (k, _, _) -> k) (fun kf (k, v1, v2) -> kf k, v1, v2) 
        let private convertKeys1of4 data = 
          data |> NotifySeq.map (fun (k, v1, v2, v3) -> k :> key, v1, v2, v3)          
               |> convertKeys (fun (k, _, _, _) -> k) (fun kf (k, v1, v2, v3) -> kf k, v1, v2, v3) 
        let private convertKeys1of5 data = 
          data |> NotifySeq.map (fun (k, v1, v2, v3, v4) -> k :> key, v1, v2, v3, v4)          
               |> convertKeys (fun (k, _, _, _, _) -> k) (fun kf (k, v1, v2, v3, v4) -> kf k, v1, v2, v3, v4) 
        let private convertKeys1of7 data = 
          data |> NotifySeq.map (fun (k, v1, v2, v3, v4, v5, v6) -> k :> key, v1, v2, v3, v4, v5, v6)          
               |> convertKeys (fun (k, _, _, _, _, _, _) -> k) (fun kf (k, v1, v2, v3, v4, v5, v6) -> kf k, v1, v2, v3, v4, v5, v6) 

        // In most cases, we can use static type - if it is primitive type and it is not
        // mapped to something else
        let getChartValueTypeStatic<'K when 'K :> key>(witness:seq<'K>) =
          ignore(witness)
          match KeyConversions.knownTypes.TryGetValue(typeof<'K>) with
          | true, typ -> Some typ
          | _ -> None

        let getChartValueTypeDynamic xTypeOpt values = 
          // Only look at the values if we did not find the type using static type
          match xTypeOpt with
          | Some xType -> xType
          | _ ->
            try 
              let v = Seq.head values 
              // For date time, we need to decide if it represents dates or actual time
              // If they all represent the same day, then it is probably time; If they 
              // all represent 12:00am, then it is probably a date...
              let typ = v.GetType() 
              if typ = typeof<DateTime> then                 
                let dates = Seq.map (unbox<DateTime>) values
                let firstDate = ((box v) :?> DateTime).Date
                if dates |> Seq.forall (fun dt -> dt.Date = firstDate) then ChartValueType.Time
                elif dates |> Seq.forall (fun dt -> dt.TimeOfDay = TimeSpan.Zero) then ChartValueType.Date
                else ChartValueType.DateTime
              else 
                // For all other types, we use the 'knownTypes' lookup table
                match KeyConversions.knownTypes.TryGetValue(typ) with
                | true, t -> t
                | _ -> ChartValueType.Auto
            with _ -> ChartValueType.Auto

        // ----------------------------------------------------------------------------------
        // Single Y value
          
        let internal mergeDataAndLabelsForXY (data:seq<#key * #value>) (labels: #seq<string> option) = 
            let xTypeOpt = getChartValueTypeStatic (Seq.map fst data)
            let data = convertKeys1of2 (NotifySeq.notifyOrOnce data)
            let xType = getChartValueTypeDynamic xTypeOpt (Seq.map fst data)
            match labels with 
            | None -> ChartData.Values(NotifySeq.ignoreReset data, xType, "Item1", "Item2", "") 
            | Some labels -> 
                let labels = NotifySeq.notifyOrOnce labels
                let dataAndLabels = 
                  NotifySeq.zip data labels |> NotifySeq.map (fun ((x,y),label) -> DataPoint(X=x,Y=y,Label=label))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y", "Label=Label") 

        let internal mergeDataAndLabelsForY data (labels: #seq<string> option) = 
            let data = NotifySeq.notifyOrOnce data
            mergeDataAndLabelsForXY (data |> NotifySeq.mapi (fun i y -> (i, y))) labels

        // Two Y values
        let internal mergeDataAndLabelsForXY2 data labels = 
            let xTypeOpt = getChartValueTypeStatic (Seq.map (fun (a, _, _) -> a) data)
            let data = convertKeys1of3 (NotifySeq.notifyOrOnce data)
            let xType = getChartValueTypeDynamic xTypeOpt (Seq.map (fun (a, _, _) -> a) data)
            match labels with 
            | None -> 
                let dataAndLabels = data |> NotifySeq.map (fun (x,y1,y2) -> TwoXYDataPoint(X=x,Y1=y1,Label=null,Y2=y2))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2", "")
            | Some labels ->  
                let labels = NotifySeq.notifyOrOnce labels
                let dataAndLabels = NotifySeq.zip data labels |> NotifySeq.map (fun ((x,y1,y2),label) -> TwoXYDataPoint(X=x,Y1=y1,Y2=y2,Label=label))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2", "Label=Label")

        let internal mergeDataAndLabelsForY2 data (labels: #seq<string> option) = 
            let data = NotifySeq.notifyOrOnce data
            mergeDataAndLabelsForXY2 (data |> NotifySeq.mapi (fun i (y1, y2) -> (i, y1, y2))) labels

        // Three Y values
        let internal mergeDataAndLabelsForXY3 data labels = 
            let xTypeOpt = getChartValueTypeStatic (Seq.map (fun (a, _, _, _) -> a) data)
            let data = convertKeys1of4 (NotifySeq.notifyOrOnce data)
            let xType = getChartValueTypeDynamic xTypeOpt (Seq.map (fun (a, _, _, _) -> a) data)
            match labels with 
            | None -> 
                let dataAndLabels = data |> NotifySeq.map (fun (x,y1,y2,y3) -> ThreeXYDataPoint(X=x,Y1=y1,Label=null,Y2=y2,Y3=y3))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2,Y3", "")
            | Some labels ->  
                let labels = NotifySeq.notifyOrOnce labels
                let dataAndLabels = NotifySeq.zip data labels |> NotifySeq.map (fun ((x,y1,y2,y3),label) -> ThreeXYDataPoint(X=x,Y1=y1,Y2=y2,Y3=y3,Label=label))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2,Y3", "Label=Label")


        // One X and Four Y values
        let internal mergeDataAndLabelsForXY4 (data:seq<#key * #value * #value * #value * #value>) labels = 
            let xTypeOpt = getChartValueTypeStatic (Seq.map (fun (a, _, _, _, _) -> a) data)
            let data = convertKeys1of5 (NotifySeq.notifyOrOnce data)
            let xType = getChartValueTypeDynamic xTypeOpt (Seq.map (fun (a, _, _, _, _) -> a) data)
            match labels with 
            | None -> 
                let dataAndLabels = data |> NotifySeq.map (fun (x,y1,y2,y3,y4) -> FourXYDataPoint(X= string (box x),Y1=y1,Label=null,Y2=y2,Y3=y3,Y4=y4))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2,Y3,Y4", "")
            | Some labels ->  
                let labels = NotifySeq.notifyOrOnce labels
                let dataAndLabels = NotifySeq.zip data labels |> NotifySeq.map (fun ((x,y1,y2,y3,y4),label) -> FourXYDataPoint(X= string (box x),Y1=y1,Y2=y2,Y3=y3,Y4=y4,Label=label))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2,Y3,Y4", "Label=Label")

        // Four Y values
        let internal mergeDataAndLabelsForY4 (data:seq<#value * #value * #value * #value>) labels = 
            let data = NotifySeq.notifyOrOnce data // evaluate only once and cache
            match labels with 
            | None -> 
                let dataAndLabels = data |> NotifySeq.mapi (fun i (y1,y2,y3,y4) -> FourXYDataPoint(X=string i,Y1=y1,Label=null,Y2=y2,Y3=y3,Y4=y4))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, ChartValueType.Auto, "X", "Y1,Y2,Y3,Y4", "")
            | Some labels ->  
                let labels = NotifySeq.notifyOrOnce labels
                let dataAndLabels = NotifySeq.zip data labels  |> NotifySeq.mapi (fun i ((y1,y2,y3,y4),label) -> FourXYDataPoint(X=string i,Y1=y1,Y2=y2,Y3=y3,Y4=y4,Label=label))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, ChartValueType.Auto, "X", "Y1,Y2,Y3,Y4", "Label=Label")

        // ----------------------------------------------------------------------------------
        // Six or more values

        // X and Y values 
        let internal mergeDataAndLabelsForXY6 (data:seq<#key * #value * #value * #value * #value * #value * #value>) labels = 
            let xTypeOpt = getChartValueTypeStatic (Seq.map (fun (a, _, _, _, _, _, _) -> a) data)
            let data = convertKeys1of7 (NotifySeq.notifyOrOnce data)
            let xType = getChartValueTypeDynamic xTypeOpt (Seq.map (fun (a, _, _, _, _, _, _) -> a) data)
            match labels with 
            | None -> 
                let dataAndLabels = data |> NotifySeq.map (fun (x,y1,y2,y3,y4,y5,y6) -> SixXYDataPoint(X=string (box x),Y1=y1,Label=null,Y2=y2,Y3=y3,Y4=y4,Y5=y5,Y6=y6))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2,Y3,Y4,Y5,Y6", "")
            | Some labels ->  
                let labels = NotifySeq.notifyOrOnce labels
                let dataAndLabels = NotifySeq.zip data labels  |> NotifySeq.map (fun ((x,y1,y2,y3,y4,y5,y6),label) -> SixXYDataPoint(X=string (box x),Y1=y1,Y2=y2,Y3=y3,Y4=y4,Y5=y5,Y6=y6,Label=label))
                ChartData.Values(NotifySeq.ignoreReset dataAndLabels, xType, "X", "Y1,Y2,Y3,Y4,Y5,Y6", "Label=Label")

        // one X and multiple Y values 
        let internal mergeDataAndLabelsForXYS (data:seq<#key * #seq<#value>>) = 
            let data = convertKeys1of2 (NotifySeq.notifyOrOnce data) // evaluate only once and cache
            let dataAndLabels = data |> NotifySeq.map (fun (x,ys) -> string (box x),(ys |> Array.ofSeq |> Array.map (fun x -> x :> value)))
            ChartData.BoxPlotValues(NotifySeq.ignoreReset dataAndLabels)

        // ----------------------------------------------------------------------------------
        // Stacked sequence values

        // Sequence of sequence of X and Y Values only
        // TODO: labels
        let internal seqXY (data: seq< #seq<#key * #value >>) = 
            let data = NotifySeq.notifyOrOnce data // evaluate only once and cache
            let series = data |> NotifySeq.map (fun items ->
                              let (itemsX,itemsY) = items |> Seq.toArray |> Array.unzip 
                              ((itemsX :> IEnumerable), (itemsY :> IEnumerable)) )
            ChartData.StackedXYValues (series)

        // --------------------------------------------------------------------------------------

        let internal setSeriesData resetSeries (series:Series) data (chart:Chart) setCustomProperty =             

            match data with 
            | ChartData.Values (vs,xType,xField,yField,otherFields) ->
                match vs with 
                | :? INotifyCollectionChanged as i -> 
                      let handler = NotifyCollectionChangedEventHandler(fun _ _ -> 
                        // TODO: better incremental updates.  Must propagate 'Add' change structure through INotifyCollectionChanged etc.
                        series.Points.Clear()
                        series.Points.DataBind(vs,xField,yField,otherFields))
                      i.CollectionChanged.AddHandler handler
                      chart.Disposed.Add(fun _ -> try i.CollectionChanged.RemoveHandler handler with _ -> ())
                | _ -> ()
                series.XValueType <- xType
                series.Points.DataBind(vs,xField,yField,otherFields)

            // Special case for BoxPlot
            | ChartData.BoxPlotValues(values) ->
                let bindBoxPlot() = 
                    let labels = chart.ChartAreas.[0].AxisX.CustomLabels
                    if resetSeries then
                        labels.Clear()
                        while chart.Series.Count > 1 do chart.Series.RemoveAt(1)                                 
                    let name = series.Name
                    let seriesNames = 
                        values |> Seq.mapi (fun index (x,ys) ->
                            let name = sprintf "BoxPlot_%s_%d" name index
                            let dataSeries = new Series(name, Enabled = false, ChartType=SeriesChartType.BoxPlot)
                            dataSeries.Points.DataBindY([| (ys :> IEnumerable) |])
                            // TODO: how to bind the 'X' values in a box plot? Here we fake it via new labels.
                            // TODO: what if the 'X' values contain dates etc.? Here we fake it by converting to strings.
                            //dataSeries.Points.DataBindXY([| for y in ys -> x |], [| (ys :> IEnumerable) |])
                            //if displayLabel then
                            let label = x 
                            labels.Add(float index, float (index + 2), label) |> ignore
                            dataSeries.AxisLabel <- label
                            dataSeries.Label <- label
                            chart.Series.Add dataSeries
                            name )
                    let boxPlotSeries = seriesNames |> String.concat ";"
                    setCustomProperty("BoxPlotSeries", boxPlotSeries)

                match values with 
                | :? INotifyCollectionChanged as i -> 
                      let handler = NotifyCollectionChangedEventHandler(fun _ _ -> bindBoxPlot())
                      i.CollectionChanged.AddHandler handler
                      chart.Disposed.Add(fun _ -> try i.CollectionChanged.RemoveHandler handler with _ -> ())
                | _ -> ()
                bindBoxPlot() 

            // Special case for Stacked
            | ChartData.StackedXYValues values ->
                let bindStackedChart() =                
                    let name = series.Name
                    let chartType = series.ChartType       
                    while chart.Series.Count > 0 do chart.Series.RemoveAt(0)        
                    values |> Seq.iteri (fun index (v1,v2) ->
                        let name = sprintf "Stacked_%s_%d" name index
                        let dataSeries = new Series(name, Enabled = false, ChartType=chartType)
                        applyProperties dataSeries series
                        dataSeries.Name <- name
                        dataSeries.Points.DataBindXY(v1, [| v2 |])
                        chart.Series.Add dataSeries)

                match values with 
                | :? INotifyCollectionChanged as i -> 
                      let handler = NotifyCollectionChangedEventHandler(fun _ _ -> bindStackedChart())
                      i.CollectionChanged.AddHandler handler
                      chart.Disposed.Add(fun _ -> try i.CollectionChanged.RemoveHandler handler with _ -> ())
                | _ -> ()
                bindStackedChart() 

        type GenericChart(chartType) =     
            inherit obj()   
            // Events
            let propChangedCustom = Event<string * obj>()

            let customProperties = new Dictionary<_, _>()

            let area = new ChartArea()
            do applyPropertyDefaults chartType area
               applyPropertyDefaults chartType area.AxisX
               applyPropertyDefaults chartType area.AxisY
               applyPropertyDefaults chartType area.AxisX2
               applyPropertyDefaults chartType area.AxisY2

            let series = new Series()
            do applyPropertyDefaults chartType series

            let mutable chart = lazy (
                let chart = new Chart()
                applyPropertyDefaults chartType chart
                chart)

            let mutable title = None 
            let mutable legend = None 

            let mutable name = ""

            let evalLazy v =
                let l = lazy v
                l.Force() |> ignore
                l

            let mutable data = ChartData.Values (NotifySeq.ignoreReset (NotifySeq.notifyOrOnce []), ChartValueType.Auto, "Item1", "Item2", "") 
            let mutable margin = DefaultMarginForEachChart

            [<Obsolete("This type does not support GetHashCode()")>]
            override x.GetHashCode() = 0
            [<Obsolete("This type does not support Equals(...)")>]
            override x.Equals(_yobj) = false
            [<Obsolete("This type does not support ToString(...)")>]
            override x.ToString() = base.ToString()
            [<Obsolete("This type does not support GetType()")>]
            member x.GetType() = typeof<Chart>

            member internal x.ChartType = chartType
            member internal x.ChartTypeName = 
                match int x.ChartType with
                | -1 -> "Combined"
                | _ -> x.ChartType.ToString()
    
            member internal x.Data with get() = data and set v = data <- v
            member internal x.Chart with get() = chart.Value and set v = chart <- evalLazy v
            member internal x.Margin with get() = margin and set v = margin <- v
            member internal x.Name with get() = name and set v = name <- v

            /// Ensure the chart has a Title
            member internal x.ForceTitle() = 
                match title with 
                | None -> 
                    let t = new Title()
                    title <- Some t
                    applyPropertyDefaults chartType t
                    t
                | Some t -> t

            /// Ensure the chart has a Legend
            member internal x.ForceLegend() = 
                match legend with 
                | None -> 
                    let leg = new Legend()
                    applyPropertyDefaults chartType leg
                    legend <- Some leg
                    leg
                | Some t -> t

            /// Return the object that holds the configuration preoperties for the chart title, if present
            member internal x.TryTitle = title
            member internal x.SetTitle t = title <- Some t
            /// Return the object that holds the configuration preoperties for the chart legend, if present
            member internal x.TryLegend = legend
            member internal x.SetLegend leg = legend <- Some leg
            /// Return the object that holds the configuration preoperties for the chart, if present
            member internal x.TryChart = if chart.IsValueCreated then Some chart.Value else None
            /// Return the object that holds the configuration preoperties for the chart area
            member internal x.Area = area
            /// Return the object that holds the configuration preoperties for the chart series
            member internal x.Series = series
        
            [<CLIEvent>]
            member internal x.CustomPropertyChanged = propChangedCustom.Publish

            member internal x.CustomProperties = 
                customProperties :> seq<_>

            member internal x.GetCustomProperty<'T>(name, def) = 
                match customProperties.TryGetValue name with
                | true, v -> (box v) :?> 'T
                | _ -> def

            member internal x.SetCustomProperty<'T>(name, v:'T) = 
                customProperties.[name] <- box v
                propChangedCustom.Trigger((name, box v))

            static member internal Create(data : ChartData, f: unit -> #GenericChart ) =
                let t = f()
                t.Data <- data
                t

            /// Copy the contents of the chart as a bitmap
            member public x.CopyAsBitmap() =
                use ms = new IO.MemoryStream()
                x.Chart.SaveImage(ms, ChartImageFormat.Png |> int |> enum)
                ms.Seek(0L, IO.SeekOrigin.Begin) |> ignore
                Bitmap.FromStream ms

            /// Copy the contents of the chart to the clipboard
            member public x.CopyChartToClipboard() =
                Clipboard.SetImage(x.CopyAsBitmap())

            /// Copy the contents of the chart to the clipboard as a metafile
            member public x.CopyChartToClipboardEmf(control:Control) =
                use ms = new IO.MemoryStream()
                x.Chart.SaveImage(ms, ChartImageFormat.Emf |> int |> enum)
                ms.Seek(0L, IO.SeekOrigin.Begin) |> ignore
                use emf = new System.Drawing.Imaging.Metafile(ms)
                ClipboardMetafileHelper.PutEnhMetafileOnClipboard(control.Handle, emf) |> ignore

            /// Save the chart as an image in the specified image format
            member public x.SaveChartAs(filename : string, format : ChartImageFormat) =
                x.Chart.SaveImage(filename, format  |> int |> enum)

            
        type private ColorWrapper(clr:Color) =
            member x.Color = clr
            override x.ToString() =
                if clr.IsEmpty then "Empty" else
                sprintf "%d" (clr.ToArgb()) // clr.R clr.G clr.B

        // ------------------------------------------------------------------------------------
        // Specific chart types which also allow setting specific custom properties

        /// Used to display stock information using high, low, open and close values.
        type CandlestickChart() = 
            inherit GenericChart(SeriesChartType.Candlestick)

            /// The Y value to use as the data point
            /// label.
            member x.LabelValueType
                with get() = x.GetCustomProperty<LabelValueType>("LabelValueType", LabelValueType.Close)
                and set(v) = x.SetCustomProperty<LabelValueType>("LabelValueType", v)

            /// The data point color to use to indicate a
            /// decreasing trend.
            member x.PriceDownColor
                with get() = x.GetCustomProperty<ColorWrapper>("PriceDownColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PriceDownColor", ColorWrapper(v))

            /// The data point color that indicates an increasing trend.
            member x.PriceUpColor
                with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

        /// Similar to the Pie chart type, except that it has
        /// a hole in the center.
        type DoughnutChart() = 
            inherit GenericChart(SeriesChartType.Doughnut)

            /// <summary>
            ///   The 3D label line size as a percentage of
            ///   the default size.
            /// </summary>
            /// <remarks>Any integer from 30 to 200.</remarks>
            member x.LabelLineSize3D
                with get() = x.GetCustomProperty<int>("3DLabelLineSize", 100)
                and set(v) = x.SetCustomProperty<int>("3DLabelLineSize", v)

            /// <summary>
            ///   The radius of the doughnut portion in the Doughnut
            ///   chart.
            /// </summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.DoughnutRadius
                with get() = x.GetCustomProperty<int>("DoughnutRadius", 60)
                and set(v) = x.SetCustomProperty<int>("DoughnutRadius", v)

            /// Specifies whether the Pie or Doughnut data point is exploded.
            member x.Exploded
                with get() = x.GetCustomProperty<bool>("Exploded", false)
                and set(v) = x.SetCustomProperty<bool>("Exploded", v)

            /// <summary>
            ///   The size of the horizontal segment of the callout
            ///   line.
            /// </summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.LabelsHorizontalLineSize
                with get() = x.GetCustomProperty<int>("LabelsHorizontalLineSize", 1)
                and set(v) = x.SetCustomProperty<int>("LabelsHorizontalLineSize", v)

            /// <summary>The size of the radial segment of the callout line.</summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.LabelsRadialLineSize
                with get() = x.GetCustomProperty<int>("LabelsRadialLineSize", 1)
                and set(v) = x.SetCustomProperty<int>("LabelsRadialLineSize", v)

            /// <summary>The minimum pie or doughnut size.</summary>
            /// <remarks>Any integer from 10 to 70.</remarks>
            member x.MinimumRelativePieSize
                with get() = x.GetCustomProperty<int>("MinimumRelativePieSize", 30)
                and set(v) = x.SetCustomProperty<int>("MinimumRelativePieSize", v)

            /// The drawing style of the data points.
            member x.PieDrawingStyle
                with get() = x.GetCustomProperty<PieDrawingStyle>("PieDrawingStyle", PieDrawingStyle.Default)
                and set(v) = x.SetCustomProperty<PieDrawingStyle>("PieDrawingStyle", v)

            /// The label style of the data points.
            member x.PieLabelStyle
                with get() = x.GetCustomProperty<PieLabelStyle>("PieLabelStyle", PieLabelStyle.Inside)
                and set(v) = x.SetCustomProperty<PieLabelStyle>("PieLabelStyle", v)

            /// The color of the radial and horizontal segments of
            /// the callout lines.
            member x.PieLineColor
                with get() = x.GetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(Color.Black)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(v))

            /// <summary>The angle of the data point in the Pie or Doughnut chart.</summary>
            /// <remarks>Any integer from 0 to 360.</remarks>
            member x.PieStartAngle
                with get() = x.GetCustomProperty<int>("PieStartAngle", 90)
                and set(v) = x.SetCustomProperty<int>("PieStartAngle", v)


        /// Consists of lines with markers that are used to display
        /// statistical information about the data displayed in a graph.
        type ErrorBarChart() = 
            inherit GenericChart(SeriesChartType.ErrorBar)

            /// The appearance of the marker at the center value of the error bar.
            member x.ErrorBarCenterMarkerStyle
                with get() = x.GetCustomProperty<ErrorBarCenterMarkerStyle>("ErrorBarCenterMarkerStyle", ErrorBarCenterMarkerStyle.None)
                and set(v) = x.SetCustomProperty<ErrorBarCenterMarkerStyle>("ErrorBarCenterMarkerStyle", v)

            /// The name of the series to be used as the data source for the Error Bar chart calculations.
            member x.ErrorBarSeries
                with get() = x.GetCustomProperty<string>("ErrorBarSeries", "")
                and set(v) = x.SetCustomProperty<string>("ErrorBarSeries", v)

            /// The visibility of the upper and lower error values.
            member x.ErrorBarStyle
                with get() = x.GetCustomProperty<ErrorBarStyle>("ErrorBarStyle", ErrorBarStyle.Both)
                and set(v) = x.SetCustomProperty<ErrorBarStyle>("ErrorBarStyle", v)

            /// Specifies how the upper and lower error values are calculated
            /// for the center values of the ErrorBarSeries.
            member x.ErrorBarType
                with get() = x.GetCustomProperty<ErrorBarType>("ErrorBarType", ErrorBarType.FixedValue)
                and set(v) = x.SetCustomProperty<ErrorBarType>("ErrorBarType", v)

        /// Displays in a funnel shape data that equals 100% when
        /// totaled.
        type FunnelChart() = 
            inherit GenericChart(SeriesChartType.Funnel)

            /// The line color of the callout for the data point labels of Funnel or Pyramid charts.
            member x.CalloutLineColor
                with get() = x.GetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(v))

            /// The 3D drawing style of the Funnel chart type.
            member x.Funnel3DDrawingStyle
                with get() = x.GetCustomProperty<Funnel3DDrawingStyle>("Funnel3DDrawingStyle", Funnel3DDrawingStyle.SquareBase)
                and set(v) = x.SetCustomProperty<Funnel3DDrawingStyle>("Funnel3DDrawingStyle", v)

            /// <summary>The 3D rotation angle of the Funnel chart type.</summary>
            /// <remarks>Any integer from -10 to 10.</remarks>
            member x.Funnel3DRotationAngle
                with get() = x.GetCustomProperty<int>("Funnel3DRotationAngle", 5)
                and set(v) = x.SetCustomProperty<int>("Funnel3DRotationAngle", v)

            /// The data point label placement of the Funnel chart type when the FunnelLabelStyle is set to Inside.
            member x.FunnelInsideLabelAlignment
                with get() = x.GetCustomProperty<FunnelInsideLabelAlignment>("FunnelInsideLabelAlignment", FunnelInsideLabelAlignment.Center)
                and set(v) = x.SetCustomProperty<FunnelInsideLabelAlignment>("FunnelInsideLabelAlignment", v)

            /// The data point label style of the Funnel chart type.
            member x.FunnelLabelStyle
                with get() = x.GetCustomProperty<FunnelLabelStyle>("FunnelLabelStyle", FunnelLabelStyle.OutsideInColumn)
                and set(v) = x.SetCustomProperty<FunnelLabelStyle>("FunnelLabelStyle", v)

            /// <summary>The minimum height of a data point in the Funnel chart, measured in relative coordinates.</summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.FunnelMinPointHeight
                with get() = x.GetCustomProperty<int>("FunnelMinPointHeight", 0)
                and set(v) = x.SetCustomProperty<int>("FunnelMinPointHeight", v)

            /// <summary>The neck height of the Funnel chart type.</summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.FunnelNeckHeight
                with get() = x.GetCustomProperty<int>("FunnelNeckHeight", 5)
                and set(v) = x.SetCustomProperty<int>("FunnelNeckHeight", v)

            /// <summary>The neck width of the Funnel chart type.</summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.FunnelNeckWidth
                with get() = x.GetCustomProperty<int>("FunnelNeckWidth", 5)
                and set(v) = x.SetCustomProperty<int>("FunnelNeckWidth", v)

            /// Placement of the data point label in the Funnel chart
            /// when FunnelLabelStyle is set to Outside or OutsideInColumn.
            member x.FunnelOutsideLabelPlacement
                with get() = x.GetCustomProperty<FunnelOutsideLabelPlacement>("FunnelOutsideLabelPlacement", FunnelOutsideLabelPlacement.Right)
                and set(v) = x.SetCustomProperty<FunnelOutsideLabelPlacement>("FunnelOutsideLabelPlacement", v)

            /// <summary>The gap size between the points of a Funnel chart, measured in relative coordinates.</summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.FunnelPointGap
                with get() = x.GetCustomProperty<int>("FunnelPointGap", 0)
                and set(v) = x.SetCustomProperty<int>("FunnelPointGap", v)

            /// The style of the Funnel chart type.
            member x.FunnelStyle
                with get() = x.GetCustomProperty<FunnelStyle>("FunnelStyle", FunnelStyle.YIsHeight)
                and set(v) = x.SetCustomProperty<FunnelStyle>("FunnelStyle", v)


        /// Displays a series of connecting vertical lines where the thickness
        /// and direction of the lines are dependent on the action
        /// of the price value.
        type KagiChart() = 
            inherit GenericChart(SeriesChartType.Kagi)

            /// The data point color that indicates an increasing trend.
            member x.PriceUpColor
                with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

            /// The reversal amount for the chart.
            member x.ReversalAmount
                with get() = x.GetCustomProperty<string>("ReversalAmount", "3%")
                and set(v) = x.SetCustomProperty<string>("ReversalAmount", v)

            /// <summary>The index of the Y value to use to plot the Kagi, Renko, or Three Line Break chart, with the first Y value at index 0.</summary>
            /// <remarks>Any positive integer 0.</remarks>
            member x.UsedYValue
                with get() = x.GetCustomProperty<int>("UsedYValue", 0)
                and set(v) = x.SetCustomProperty<int>("UsedYValue", v)


        /// Shows how proportions of data, shown as pie-shaped pieces, contribute to
        /// the data as a whole.
        type PieChart() = 
            inherit GenericChart(SeriesChartType.Pie)

            /// <summary>
            ///   The 3D label line size as a percentage of
            ///   the default size.
            /// </summary>
            /// <remarks>Any integer from 30 to 200.</remarks>
            member x.LabelLineSize3D
                with get() = x.GetCustomProperty<int>("3DLabelLineSize", 100)
                and set(v) = x.SetCustomProperty<int>("3DLabelLineSize", v)

            /// Specifies whether the Pie or Doughnut data point is exploded.
            member x.Exploded
                with get() = x.GetCustomProperty<bool>("Exploded", false)
                and set(v) = x.SetCustomProperty<bool>("Exploded", v)

            /// The size of the horizontal segment of the callout line. Any integer from 0 to 100.
            member x.LabelsHorizontalLineSize
                with get() = x.GetCustomProperty<int>("LabelsHorizontalLineSize", 1)
                and set(v) = x.SetCustomProperty<int>("LabelsHorizontalLineSize", v)

            /// <summary>The size of the radial segment of the callout line.</summary>
            /// <remarks>Any integer from 0 to 100.</remarks>
            member x.LabelsRadialLineSize
                with get() = x.GetCustomProperty<int>("LabelsRadialLineSize", 1)
                and set(v) = x.SetCustomProperty<int>("LabelsRadialLineSize", v)

            /// <summary>The minimum pie or doughnut size.</summary>
            /// <remarks>Any integer from 10 to 70.</remarks>
            member x.MinimumRelativePieSize
                with get() = x.GetCustomProperty<int>("MinimumRelativePieSize", 30)
                and set(v) = x.SetCustomProperty<int>("MinimumRelativePieSize", v)

            /// The drawing style of the data points.
            member x.PieDrawingStyle
                with get() = x.GetCustomProperty<PieDrawingStyle>("PieDrawingStyle", PieDrawingStyle.Default)
                and set(v) = x.SetCustomProperty<PieDrawingStyle>("PieDrawingStyle", v)

            /// The label style of the data points.
            member x.PieLabelStyle
                with get() = x.GetCustomProperty<PieLabelStyle>("PieLabelStyle", PieLabelStyle.Inside)
                and set(v) = x.SetCustomProperty<PieLabelStyle>("PieLabelStyle", v)

            /// The color of the radial and horizontal segments of
            /// the callout lines.
            member x.PieLineColor
                with get() = x.GetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(Color.Black)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(v))

            /// <summary>The angle of the data point in the Pie or Doughnut chart. </summary>
            /// <remarks>Any integer from 0 to 360.</remarks>
            member x.PieStartAngle
                with get() = x.GetCustomProperty<int>("PieStartAngle", 90)
                and set(v) = x.SetCustomProperty<int>("PieStartAngle", v)

        /// Disregards the passage of time and only displays changes in prices.
        type PointAndFigureChart() = 
            inherit GenericChart(SeriesChartType.PointAndFigure)

            /// The box size in the Renko or Point and Figure charts.
            member x.BoxSize
                with get() = x.GetCustomProperty<string>("BoxSize", "4%")
                and set(v) = x.SetCustomProperty<string>("BoxSize", v)

            /// The data point color that indicates an increasing trend.
            member x.PriceUpColor
                with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

            /// Specifies whether the Point and Figure chart should draw the
            /// X and O values proportionally.
            member x.ProportionalSymbols
                with get() = x.GetCustomProperty<bool>("ProportionalSymbols", true)
                and set(v) = x.SetCustomProperty<bool>("ProportionalSymbols", v)

            /// The reversal amount for the chart.
            member x.ReversalAmount
                with get() = x.GetCustomProperty<string>("ReversalAmount", "3%")
                and set(v) = x.SetCustomProperty<string>("ReversalAmount", v)

            /// <summary>
            ///   The index of the Y value to use for
            ///   the high price in the Point and Figure chart, with
            ///   the first Y value at index 0.
            /// </summary>
            /// <remarks>Any positive integer 0.</remarks>
            member x.UsedYValueHigh
                with get() = x.GetCustomProperty<int>("UsedYValueHigh", 0)
                and set(v) = x.SetCustomProperty<int>("UsedYValueHigh", v)

            /// <summary>
            ///   The index of the Y value to use for
            ///   the low price in the Point and Figure chart, with
            ///   the first Y value at index 0.
            /// </summary>
            /// <remarks>Any positive integer 0.</remarks>
            member x.UsedYValueLow
                with get() = x.GetCustomProperty<int>("UsedYValueLow", 0)
                and set(v) = x.SetCustomProperty<int>("UsedYValueLow", v)


        /// A circular graph on which data points are displayed using
        /// the angle, and the distance from the center point.
        type PolarChart() = 
            inherit GenericChart(SeriesChartType.Polar)

            /// The text orientation of the axis labels in Radar
            /// and Polar charts.
            member x.CircularLabelStyle
                with get() = x.GetCustomProperty<CircularLabelStyle>("CircularLabelStyle", CircularLabelStyle.Horizontal)
                and set(v) = x.SetCustomProperty<CircularLabelStyle>("CircularLabelStyle", v)

            /// The drawing style of the Polar chart type.
            member x.PolarDrawingStyle
                with get() = x.GetCustomProperty<PolarDrawingStyle>("PolarDrawingStyle", PolarDrawingStyle.Line)
                and set(v) = x.SetCustomProperty<PolarDrawingStyle>("PolarDrawingStyle", v)


        /// Displays data that, when combined, equals 100%.
        type PyramidChart() = 
            inherit GenericChart(SeriesChartType.Pyramid)

            /// The line color of the callout for the data
            /// point labels of Funnel or Pyramid charts.
            member x.CalloutLineColor
                with get() = x.GetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(v))

            /// <summary>The 3D rotation angle of the Pyramid chart.</summary>
            /// <remarks>Any integer from -10 to 10.</remarks>
            member x.Pyramid3DRotationAngle
                with get() = x.GetCustomProperty<int>("Pyramid3DRotationAngle", 5)
                and set(v) = x.SetCustomProperty<int>("Pyramid3DRotationAngle", v)

            /// The placement of the data point labels in the
            /// Pyramid chart when they are placed inside the pyramid.
            member x.PyramidInsideLabelAlignment
                with get() = x.GetCustomProperty<PyramidInsideLabelAlignment>("PyramidInsideLabelAlignment", PyramidInsideLabelAlignment.Center)
                and set(v) = x.SetCustomProperty<PyramidInsideLabelAlignment>("PyramidInsideLabelAlignment", v)

            /// The style of data point labels in the Pyramid
            /// chart.
            member x.PyramidLabelStyle
                with get() = x.GetCustomProperty<PyramidLabelStyle>("PyramidLabelStyle", PyramidLabelStyle.OutsideInColumn)
                and set(v) = x.SetCustomProperty<PyramidLabelStyle>("PyramidLabelStyle", v)

            ///   The minimum height of a data point measured in
            ///   relative coordinates. Any integer from 0 to 100.
            member x.PyramidMinPointHeight
                with get() = x.GetCustomProperty<int>("PyramidMinPointHeight", 0)
                and set(v) = x.SetCustomProperty<int>("PyramidMinPointHeight", v)

            /// The placement of the data point labels in the
            /// Pyramid chart when the labels are placed outside the pyramid.
            member x.PyramidOutsideLabelPlacement
                with get() = x.GetCustomProperty<PyramidOutsideLabelPlacement>("PyramidOutsideLabelPlacement", PyramidOutsideLabelPlacement.Right)
                and set(v) = x.SetCustomProperty<PyramidOutsideLabelPlacement>("PyramidOutsideLabelPlacement", v)

            ///   The gap size between the data points, measured in
            ///   relative coordinates. Any integer from 0 to 100.
            member x.PyramidPointGap
                with get() = x.GetCustomProperty<int>("PyramidPointGap", 0)
                and set(v) = x.SetCustomProperty<int>("PyramidPointGap", v)

            /// Specifies whether the data point value represents a linear height
            /// or the surface of the segment.
            member x.PyramidValueType
                with get() = x.GetCustomProperty<PyramidValueType>("PyramidValueType", PyramidValueType.Linear)
                and set(v) = x.SetCustomProperty<PyramidValueType>("PyramidValueType", v)


        /// A circular chart that is used primarily as a data
        /// comparison tool.
        type RadarChart() = 
            inherit GenericChart(SeriesChartType.Radar)

            /// The text orientation of the axis labels in Radar
            /// and Polar charts.
            member x.CircularLabelStyle
                with get() = x.GetCustomProperty<CircularLabelStyle>("CircularLabelStyle", CircularLabelStyle.Horizontal)
                and set(v) = x.SetCustomProperty<CircularLabelStyle>("CircularLabelStyle", v)

            /// The drawing style of the Radar chart.
            member x.RadarDrawingStyle
                with get() = x.GetCustomProperty<RadarDrawingStyle>("RadarDrawingStyle", RadarDrawingStyle.Area)
                and set(v) = x.SetCustomProperty<RadarDrawingStyle>("RadarDrawingStyle", v)

        /// Displays a series of connecting vertical lines where the thickness
        /// and direction of the lines are dependent on the action
        /// of the price value.
        type RenkoChart() = 
            inherit GenericChart(SeriesChartType.Renko)

            /// The box size in the Renko or Point and
            /// Figure charts.
            member x.BoxSize
                with get() = x.GetCustomProperty<string>("BoxSize", "4%")
                and set(v) = x.SetCustomProperty<string>("BoxSize", v)

            /// The data point color that indicates an increasing trend.
            member x.PriceUpColor
                with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

            ///   The index of the Y value to use to
            ///   plot the Kagi, Renko, or Three Line Break chart, with
            ///   the first Y value at index 0. Any positive integer 0.
            member x.UsedYValue
                with get() = x.GetCustomProperty<int>("UsedYValue", 0)
                and set(v) = x.SetCustomProperty<int>("UsedYValue", v)


        /// Displays significant stock price points including the open, close, high,
        /// and low price points.
        type StockChart() = 
            inherit GenericChart(SeriesChartType.Stock)

            /// The Y value to use as the data point label.
            member x.LabelValueType
                with get() = x.GetCustomProperty<LabelValueType>("LabelValueType", LabelValueType.Close)
                and set(v) = x.SetCustomProperty<LabelValueType>("LabelValueType", v)

            /// The marker style for open and close values.
            member x.OpenCloseStyle
                with get() = x.GetCustomProperty<OpenCloseStyle>("OpenCloseStyle", OpenCloseStyle.Line)
                and set(v) = x.SetCustomProperty<OpenCloseStyle>("OpenCloseStyle", v)

            /// Specifies whether markers for open and close prices are displayed.
            member x.ShowOpenClose
                with get() = x.GetCustomProperty<ShowOpenClose>("ShowOpenClose", ShowOpenClose.Both)
                and set(v) = x.SetCustomProperty<ShowOpenClose>("ShowOpenClose", v)

        /// Displays a series of vertical boxes, or lines, that reflect changes in price values.
        type ThreeLineBreakChart() = 
            inherit GenericChart(SeriesChartType.ThreeLineBreak)

            /// The number of lines to use in a Three Line Break chart. Any integer > 0
            member x.NumberOfLinesInBreak
                with get() = x.GetCustomProperty<int>("NumberOfLinesInBreak", 3)
                and set(v) = x.SetCustomProperty<int>("NumberOfLinesInBreak", v)

            /// The data point color that indicates an increasing trend.
            member x.PriceUpColor
                with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
                and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

            /// <summary>
            ///   The index of the Y value to use to
            ///   plot the Kagi, Renko, or Three Line Break chart, with
            ///   the first Y value at index 0.
            /// </summary>
            /// <remarks>Any positive integer 0.</remarks>
            member x.UsedYValue
                with get() = x.GetCustomProperty<int>("UsedYValue", 0)
                and set(v) = x.SetCustomProperty<int>("UsedYValue", v)

        // ------------------------------------------------------------------------------------
        // Special types of charts - combine multiple series & create row/columns

        type internal CombinedChart(charts: GenericChart list) =
            inherit GenericChart(enum<SeriesChartType> -1)

            member x.Charts = charts

        type internal SubplotChart(charts:GenericChart list, orientation:Orientation) = 
            inherit GenericChart(enum<SeriesChartType> -1)
            let r = 1.0 / (charts |> Seq.length |> float)

            member x.SplitSizes = [ for _ in charts -> r ]
            member x.Orientation = orientation
            member x.Charts = charts

        type internal StyleHelper =

            static member internal Font(?Family:string, ?Size:float, ?Style:FontStyle) =
                let fontSize = 
                    match Size with
                    | Some size -> float32 size
                    | None -> DefaultFontForOthers.Size
                let fontStyle = 
                    match Style with
                    | Some style -> style
                    | None -> DefaultFontForOthers.Style
                let font =
                    match Family with
                    | Some name -> new Font(name, fontSize, fontStyle)
                    | None -> new Font(DefaultFontForOthers.FontFamily, fontSize, fontStyle)
                font

            static member internal OptionalFont(?Family:string, ?Size:float, ?Style:FontStyle) =
                match Family, Size, Style with 
                | None,None,None -> None
                | _ -> Some (StyleHelper.Font(?Family=Family,?Size=Size,?Style=Style))

        type LabelStyle
                ( ?Angle, ?Color, ?Format, ?Interval, ?IntervalOffset, ?IntervalOffsetType:DateTimeIntervalType, ?IntervalType:DateTimeIntervalType, ?IsEndLabelVisible, ?IsStaggered, ?TruncatedLabels,
                  ?FontName:string, ?FontStyle:FontStyle, ?FontSize:float) =
            let labelStyle = new Charting.LabelStyle()
            do
              Angle |> Option.iter labelStyle.set_Angle
              Color |> Option.iter labelStyle.set_ForeColor
              Format |> Option.iter labelStyle.set_Format
              Interval |> Option.iter labelStyle.set_Interval
              IntervalOffset |> Option.iter labelStyle.set_IntervalOffset
              IntervalOffsetType |> Option.iter (int >> enum >> labelStyle.set_IntervalOffsetType)
              IntervalType |> Option.iter (int >> enum >> labelStyle.set_IntervalType)
              IsStaggered |> Option.iter labelStyle.set_IsStaggered
              IsEndLabelVisible |> Option.iter labelStyle.set_IsEndLabelVisible
              TruncatedLabels |> Option.iter labelStyle.set_TruncatedLabels
              StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) |> Option.iter labelStyle.set_Font 
            member internal x.Style = labelStyle 
            static member Create(?Angle, ?Color, ?Format, ?Interval, ?IntervalOffset, ?IntervalOffsetType, ?IntervalType, ?IsEndLabelVisible, ?IsStaggered, ?TruncatedLabels,?FontName:string, ?FontStyle:FontStyle, ?FontSize:float) =
                LabelStyle( ?Angle=Angle, ?Color=Color,?Format=Format, ?Interval=Interval, ?IntervalOffset=IntervalOffset,?IntervalOffsetType=IntervalOffsetType, ?IntervalType=IntervalType, ?IsEndLabelVisible=IsEndLabelVisible, ?IsStaggered=IsStaggered, ?TruncatedLabels=TruncatedLabels,?FontName=FontName, ?FontStyle=FontStyle, ?FontSize=FontSize)


        [<Sealed>]
        type Grid( ?Enabled, ?Interval, ?IntervalOffset, ?IntervalOffsetType, ?LineColor, ?LineDashStyle, ?LineWidth) = 
            let grid = new Charting.Grid()
            do
              Enabled |> Option.iter grid.set_Enabled
              Interval |> Option.iter grid.set_Interval
              IntervalOffset |> Option.iter grid.set_IntervalOffset
              IntervalOffsetType |> Option.iter grid.set_IntervalOffsetType
              LineColor |> Option.iter grid.set_LineColor
              LineDashStyle |> Option.iter grid.set_LineDashStyle
              LineWidth |> Option.iter grid.set_LineWidth
            member internal x.Handle = grid

        [<Sealed>]
        type TickMark( ?Size, ?Style, ?Enabled, ?Interval, ?IntervalOffset, ?IntervalOffsetType, ?LineColor, ?LineDashStyle, ?LineWidth) = 
            let tickMark = new Charting.TickMark()
            do
              Enabled |> Option.iter tickMark.set_Enabled
              Interval |> Option.iter tickMark.set_Interval
              IntervalOffset |> Option.iter tickMark.set_IntervalOffset
              IntervalOffsetType |> Option.iter tickMark.set_IntervalOffsetType
              LineColor |> Option.iter tickMark.set_LineColor
              LineDashStyle |> Option.iter tickMark.set_LineDashStyle
              LineWidth |> Option.iter tickMark.set_LineWidth
              Size |> Option.iter tickMark.set_TickMarkStyle
              Style |> Option.iter tickMark.set_Size
            member internal x.Handle = tickMark


        /// A Windows Forms control to host a GenericChart specification
        type ChartControl(srcChart:GenericChart) as self = 
            inherit UserControl()

            let seriesCounter = createCounter()
            let areaCounter = createCounter()
            let legendCounter = createCounter()
            let chart = new Chart()
            do
                applyPropertyDefaults srcChart.ChartType chart
                self.Controls.Add chart

            let setMargin (area:ChartArea) (left, top, right, bottom) = 
                area.Position.X <- left
                area.Position.Y <- top 
                area.Position.Width <- right - left
                area.Position.Height <- bottom - top 

            let createArea(srcSubChart:GenericChart) pos = 
                let area = new ChartArea()
                applyPropertyDefaults srcSubChart.ChartType area
                applyPropertyDefaults srcSubChart.ChartType area.AxisX
                applyPropertyDefaults srcSubChart.ChartType area.AxisY
                applyPropertyDefaults srcSubChart.ChartType area.AxisX2
                applyPropertyDefaults srcSubChart.ChartType area.AxisY2
                chart.ChartAreas.Add area

                let srcArea = srcSubChart.Area 
                applyProperties area srcArea
                applyProperties area.AxisX srcArea.AxisX
                applyProperties area.AxisX2 srcArea.AxisX2
                applyProperties area.AxisY srcArea.AxisY
                applyProperties area.AxisY2 srcArea.AxisY2
                if srcArea.Area3DStyle.Enable3D then
                    applyProperties area.Area3DStyle srcArea.Area3DStyle
                area.Name <- if String.IsNullOrEmpty srcArea.Name then sprintf "Area_%d" (areaCounter()) else srcArea.Name 

                setMargin area pos
                area

            let processLegend (area: ChartArea) priorLegendOpt (srcSubChart:GenericChart) =
                match priorLegendOpt with 
                | Some _ -> priorLegendOpt // only take settings for one legend
                | None -> 
                    match srcSubChart.TryLegend with 
                    | Some srcLegend -> 
                        let legend = new Legend()
                        applyPropertyDefaults srcSubChart.ChartType legend 
                        let name = sprintf "Legend_%d" (legendCounter())
                        //printfn "(before) srcLegend.IsDockedInsideChartArea = %b" srcLegend.IsDockedInsideChartArea
                        //printfn "(before) legend.IsDockedInsideChartArea = %b" legend.IsDockedInsideChartArea
                        //printfn "(before) legend.DockedToChartArea = %s" legend.DockedToChartArea
                        //printfn "(before) legend.InsideArea = %s" legend.InsideChartArea
                        //printfn "(before) legend.Enabled = %b" legend.Enabled
                        applyProperties legend srcLegend
                        legend.DockedToChartArea <- area.Name
                        //printfn "(after) legend.IsDockedInsideChartArea = %b" legend.IsDockedInsideChartArea
                        //printfn "(after) legend.DockedToChartArea = %s" legend.DockedToChartArea
                        //printfn "(after) legend.InsideArea = %s" legend.InsideChartArea
                        //printfn "(after) legend.Enabled = %b" legend.Enabled
                        legend.Name <- name
                        chart.Legends.Add legend
                        Some legend
                    | None -> 
                        None

            // Transfer the title a subchart onto the target chart
            let processTitle (area: ChartArea) priorTitleOpt (srcSubChart:GenericChart) =
                match priorTitleOpt with 
                | Some _ -> priorTitleOpt // only take settings for one title
                | None -> 
                    match srcSubChart.TryTitle with 
                    | Some srcTitle -> 
                        let title = new Title()
                        applyPropertyDefaults srcSubChart.ChartType title 
                        //printfn "(before) srcSubChart.Title.IsDockedInsideChartArea = %b" srcSubChart.Title.IsDockedInsideChartArea
                        //printfn "(before) title.IsDockedInsideChartArea = %b" title.IsDockedInsideChartArea
                        //printfn "(before) title.DockedToChartArea = %s" title.DockedToChartArea
                        applyProperties title srcTitle
                        title.DockedToChartArea <- area.Name
                        //printfn "(after) srcSubChart.Title.IsDockedInsideChartArea = %b" srcSubChart.Title.IsDockedInsideChartArea
                        //printfn "(after) title.IsDockedInsideChartArea = %b" title.IsDockedInsideChartArea
                        //printfn "(after) title.DockedToChartArea = %s" title.DockedToChartArea
                        chart.Titles.Add title 
                        Some title
                    | None -> 
                        None
    
            // Transfer all the series of a source subchart onto the given area of the target chart
            let processSeries (area:ChartArea) (legendOpt : Legend option) (srcSubChart:GenericChart) =
                let name = 
                    if not (String.IsNullOrEmpty srcSubChart.Name) then srcSubChart.Name
                    else sprintf "GenericChart_Series_%d" (seriesCounter())
                let series = new Series()
                applyPropertyDefaults srcSubChart.ChartType series
                applyProperties series srcSubChart.Series

                if String.IsNullOrEmpty series.Name then 
                    series.Name <- name
                series.ChartType <- srcSubChart.ChartType
                series.ChartArea <- area.Name
            
                chart.Series.Add series
            
                // Set data
                setSeriesData false series srcSubChart.Data chart srcSubChart.SetCustomProperty

                let cult = System.Threading.Thread.CurrentThread.CurrentCulture
                System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.InvariantCulture
                let props = 
                  [ for (KeyValue(k, v)) in srcSubChart.CustomProperties -> 
                      sprintf "%s=%s" k (v.ToString()) ]
                  |> String.concat ", "
                System.Threading.Thread.CurrentThread.CurrentCulture <- cult
                series.CustomProperties <- props

                srcSubChart.CustomPropertyChanged.Add(fun (name, value) -> 
                    series.SetCustomProperty(name, System.String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value)))

                match legendOpt with 
                | Some legend -> 
                      series.Legend <- legend.Name
                | None -> ()
            
            do

                // Compute some default extra room for margins if no margins specified
                let rec computeExtraDefaultMargins ((l, t, r, b) as margins)  (srcSubChart:GenericChart) = 
                  if srcSubChart.Margin <> DefaultMarginForEachChart then 
                    margins 
                  else 
                    match srcSubChart with
                    | :? SubplotChart as subplot ->
                         (margins, subplot.Charts) ||> List.fold computeExtraDefaultMargins
                    | _ -> 
                        let legendAndTitleSubCharts = srcSubChart  :: (match srcSubChart with :? CombinedChart as cch -> cch.Charts | _ -> [])
                        let margins =
                            match legendAndTitleSubCharts |> List.tryPick (fun ch -> ch.TryLegend) with
                            | None -> margins
                            | Some leg ->  
                                //printfn "leg.Enabled = %b" leg.Enabled
                                //printfn "leg.IsDockedInsideChartArea = %b" leg.IsDockedInsideChartArea
                                //printfn "leg.Docking = %A" leg.Docking
                                if leg.Enabled && not leg.IsDockedInsideChartArea then 
                                    match leg.Docking with 
                                    | Charting.Docking.Left -> (max l DefaultExtraMarginForLegendIfPresent, t, r, b) 
                                    | Charting.Docking.Right -> (l, t, max r DefaultExtraMarginForLegendIfPresent, b)
                                    | Charting.Docking.Top -> (l, max t DefaultExtraMarginForLegendIfPresent, r, b) 
                                    | Charting.Docking.Bottom -> (l, t, r, max b DefaultExtraMarginForLegendIfPresent)
                                    | _ -> margins
                                else
                                    margins
                        let margins =
                            match legendAndTitleSubCharts |> List.tryPick (fun ch -> ch.TryTitle) with
                            | None -> margins
                            | Some title ->  
                                if not title.IsDockedInsideChartArea then 
                                    match title.Docking with 
                                    | Charting.Docking.Left -> (max l DefaultExtraMarginForTitleIfPresent, t, r, b) 
                                    | Charting.Docking.Right -> (l, t, max r DefaultExtraMarginForTitleIfPresent, b)
                                    | Charting.Docking.Top -> (l, max t DefaultExtraMarginForTitleIfPresent, r, b) 
                                    | Charting.Docking.Bottom -> (l, t, r, max b DefaultExtraMarginForTitleIfPresent)
                                    | _ -> margins
                                else
                                    margins
                        margins

                let rec layoutSubCharts (srcSubChart:GenericChart) (l, t, r, b) = 
                    let (ml, mt, mr, mb) = srcSubChart.Margin
                    let ((l, t, r, b) as pos) = (l + float32 ml, t + float32 mt, r - float32 mr, b - float32 mb)
                    match srcSubChart with
                    | :? SubplotChart as subplot ->
        
                        let total = subplot.SplitSizes |> Seq.sum
                        let available = if subplot.Orientation = Orientation.Vertical then b - t else r - l
                        let k = float available / total

                        let offs = ref 0.0f
                        for ch, siz in Seq.zip subplot.Charts subplot.SplitSizes do
                            if subplot.Orientation = Orientation.Vertical then
                                  layoutSubCharts ch (l, t + !offs, r, t + !offs + float32 (siz * k))
                            else
                                  layoutSubCharts ch (l + !offs, t, l + !offs + float32 (siz * k), b)
                            offs := !offs + float32 (siz * k) 

                    | _ -> 
                        let area = createArea srcSubChart pos
                        let seriesSubCharts = match srcSubChart with :? CombinedChart as cch -> cch.Charts | _ -> [srcSubChart]
                        let legendAndTitleSubCharts = srcSubChart  :: (match srcSubChart with :? CombinedChart as cch -> cch.Charts | _ -> [])
                        let legendOpt = (None, legendAndTitleSubCharts) ||> List.fold (processLegend area)
                        let _titleOpt = (None, legendAndTitleSubCharts) ||> List.fold (processTitle area)

                        for c in seriesSubCharts do
                            processSeries area legendOpt c

                let (dl,dt,dr,db) = computeExtraDefaultMargins (0,0,0,0) srcChart
                layoutSubCharts srcChart (0.0f + float32 dl, 0.0f + float32 dt, 100.0f - float32 dr, 100.0f - float32 db)
                srcChart.TryChart |> Option.iter (applyProperties chart)
                chart.Dock <- DockStyle.Fill
                srcChart.Chart <- chart

            let props = new PropertyGrid(Width = 250, Dock = DockStyle.Right, SelectedObject = chart, Visible = false)
  
            do
              self.Controls.Add chart
              self.Controls.Add props

              let menu = new ContextMenu()
              let dlg = new SaveFileDialog(Filter = "PNG (*.png)|*.png|Bitmap (*.bmp;*.dib)|*.bmp;*.dib|GIF (*.gif)|*.gif|TIFF (*.tiff;*.tif)|*.tiff;*.tif|EMF (*.emf)|*.emf|JPEG (*.jpeg;*.jpg;*.jpe)|*.jpeg;*.jpg;*.jpe|EMF+ (*.emf)|*.emf|EMF+Dual (*.emf)|*.emf")
              let miCopy = new MenuItem("&Copy Image to Clipboard", Shortcut = Shortcut.CtrlC)
              let miCopyEmf = new MenuItem("Copy Image to Clipboard as &EMF", Shortcut = Shortcut.CtrlShiftC)
              let miSave = new MenuItem("&Save Image As..", Shortcut = Shortcut.CtrlS)
              let miEdit = new MenuItem("Show Property &Grid", Shortcut = Shortcut.CtrlG)

              miEdit.Click.Add(fun _ -> 
                  miEdit.Checked <- not miEdit.Checked
                  props.Visible <- miEdit.Checked)

              miCopy.Click.Add(fun _ ->        
                  srcChart.CopyChartToClipboard())

              miCopyEmf.Click.Add(fun _ ->
                  srcChart.CopyChartToClipboardEmf(self))

              miSave.Click.Add(fun _ ->
                  if dlg.ShowDialog() = DialogResult.OK then
                      let fmt = 
                          match dlg.FilterIndex with
                          | 1 -> ChartImageFormat.Png
                          | 2 -> ChartImageFormat.Bmp
                          | 3 -> ChartImageFormat.Gif
                          | 4 -> ChartImageFormat.Tiff
                          | 5 -> ChartImageFormat.Emf
                          | 6 -> ChartImageFormat.Jpeg
                          | 7 -> ChartImageFormat.EmfPlus
                          | 8 -> ChartImageFormat.EmfDual
                          | _ -> ChartImageFormat.Png
                      chart.SaveImage(dlg.FileName, fmt  |> int |> enum) )

              menu.MenuItems.AddRange [| miCopy; miCopyEmf; miSave; miEdit |]
              self.ContextMenu <- menu

    open ChartTypes

    type private Helpers() = 
        static member ApplyStyles
            (?AxisXEnabled:bool,?AxisXLogarithmic:bool, ?AxisXArrowStyle:AxisArrowStyle, ?AxisXLabelStyle:LabelStyle, ?AxisXIsMarginVisible, ?AxisXMaximum, ?AxisXMinimum, ?AxisXMajorGrid:Grid, ?AxisXMinorGrid:Grid, ?AxisXMajorTickMark:TickMark, ?AxisXMinorTickMark:TickMark, ?AxisXName,
              ?AxisXTitle, ?AxisXTitleAlignment, ?AxisXTitleFont, ?AxisXTitleForeColor, ?AxisXToolTip,
              ?AxisYEnabled:bool,?AxisYLogarithmic:bool, ?AxisYArrowStyle:AxisArrowStyle, ?AxisYLabelStyle, ?AxisYIsMarginVisible, ?AxisYMaximum, ?AxisYMinimum, ?AxisYMajorGrid:Grid, ?AxisYMinorGrid:Grid, ?AxisYMajorTickMark:TickMark, ?AxisYMinorTickMark:TickMark, ?AxisYName,
              ?AxisYTitle, ?AxisYTitleAlignment, ?AxisYTitleFont, ?AxisYTitleForeColor, ?AxisYToolTip,
              ?AxisX2Enabled:bool,?AxisX2Logarithmic:bool, ?AxisX2ArrowStyle:AxisArrowStyle, ?AxisX2LabelStyle, ?AxisX2IsMarginVisible, ?AxisX2Maximum, ?AxisX2Minimum, ?AxisX2MajorGrid:Grid, ?AxisX2MinorGrid:Grid, ?AxisX2MajorTickMark:TickMark, ?AxisX2MinorTickMark:TickMark, ?AxisX2Name,
              ?AxisX2Title, ?AxisX2TitleAlignment, ?AxisX2TitleFont, ?AxisX2TitleForeColor, ?AxisX2ToolTip,
              ?AxisY2Enabled:bool,?AxisY2Logarithmic:bool, ?AxisY2ArrowStyle:AxisArrowStyle, ?AxisY2LabelStyle, ?AxisY2IsMarginVisible, ?AxisY2Maximum, ?AxisY2Minimum, ?AxisY2MajorGrid:Grid, ?AxisY2MinorGrid:Grid, ?AxisY2MajorTickMark:TickMark, ?AxisY2MinorTickMark:TickMark, ?AxisY2Name,
              ?AxisY2Title, ?AxisY2TitleAlignment, ?AxisY2TitleFont, ?AxisY2TitleForeColor, ?AxisY2ToolTip,
              ?ShowMarkerLines,?LabelPosition,?SplineLineTension,?PointStyle,?PointWidth,?MaxPixelPointWidth,?MinPixelPointWidth,?PixelPointWidth,
              ?AreaBackground,?StackedGroupName,
              ?Name,?Margin,?Background,
              // ?AlignWithChartArea, ?AlignmentOrientation, ?AlignmentStyle,
              ?Enable3D, ?Area3DInclination, ?Area3DIsClustered, ?Area3DIsRightAngleAxes, ?Area3DLightStyle:LightStyle, ?Area3DPerspective, ?Area3DPointDepth, ?Area3DPointGapDepth, ?Area3DRotation, ?Area3DWallWidth,
              ?YAxisType, ?XAxisType,
              ?Color, ?BorderColor, ?BorderWidth,
              ?DataPointLabel, ?DataPointLabelToolTip, ?DataPointToolTip, ?BarLabelPosition,
              ?MarkerColor, ?MarkerSize, ?MarkerStep, ?MarkerStyle:MarkerStyle, ?MarkerBorderColor, ?MarkerBorderWidth,
              ?LegendEnabled,?LegendTitle, ?LegendBackground, ?LegendFont, ?LegendAlignment, ?LegendDocking:Docking, ?LegendIsDockedInsideArea,
              ?LegendTitleAlignment, ?LegendTitleFont, ?LegendTitleForeColor, ?LegendBorderColor, ?LegendBorderWidth, ?LegendBorderDashStyle:DashStyle,
              ?Title,  ?TitleStyle:TextStyle, ?TitleFont, ?TitleBackground, ?TitleColor, ?TitleBorderColor, ?TitleBorderWidth, ?TitleBorderDashStyle:DashStyle, 
              ?TitleOrientation:TextOrientation, ?TitleAlignment, ?TitleDocking:Docking, ?TitleInsideArea) =
              (fun (ch:('T :> GenericChart)) -> 
          
                let convAxisEnabled = function true -> AxisEnabled.True | false -> AxisEnabled.False
                //area.AxisX <- new Axis(null, AxisName.X)
                let configureAxis (ax:Axis) (vEnabled,vIsLogarithmic,vArrowStyle:AxisArrowStyle option,vLabelStyle,vIsMarginVisible,vMaximum,vMinimum,vMajorGrid,vMinorGrid,vMajorTickMark,vMinorTickMark,vName,vTitle,vTitleAlignment,vTitleFont,vTitleForeColor,vToolTip) = 
                    vArrowStyle |> Option.iter (int >> enum >> ax.set_ArrowStyle)
                    vEnabled |> Option.iter (convAxisEnabled >> ax.set_Enabled)
                    vLabelStyle |> Option.iter (fun (labelStyle:LabelStyle) -> ax.set_LabelStyle labelStyle.Style)
                    vIsMarginVisible |> Option.iter ax.set_IsMarginVisible
                    vIsLogarithmic |> Option.iter ax.set_IsLogarithmic
                    vMaximum |> Option.iter ax.set_Maximum
                    vMinimum |> Option.iter ax.set_Minimum
                    vMajorGrid |> Option.iter (fun (c:Grid) -> ax.set_MajorGrid c.Handle)
                    vMinorGrid |> Option.iter (fun (c:Grid) -> ax.set_MinorGrid c.Handle)
                    vMajorTickMark |> Option.iter (fun (c:TickMark) -> ax.set_MajorTickMark c.Handle; ax.set_Interval c.Handle.Interval)
                    vMinorTickMark |> Option.iter (fun (c:TickMark) -> ax.set_MinorTickMark c.Handle; ax.set_Interval c.Handle.Interval)
                    vName |> Option.iter ax.set_Name
                    vTitle |> Option.iter ax.set_Title
                    vTitleAlignment |> Option.iter ax.set_TitleAlignment
                    vTitleFont |> Option.iter ax.set_TitleFont
                    vTitleForeColor |> Option.iter ax.set_TitleForeColor
                    vToolTip |> Option.iter ax.set_ToolTip
                


    (*
    AxisName Property
    Crossing Property
    CustomLabels Property
    InterlacedColor Property
    Interval Property
    IntervalAutoMode Property
    IntervalOffset Property
    IntervalOffsetType Property
    IntervalType Property
    IsInterlaced Property
    IsLabelAutoFit Property
    IsLogarithmic Property
    IsMarksNextToAxis Property
    IsReversed Property
    IsStartedFromZero Property
    LabelAutoFitMaxFontSize Property
    LabelAutoFitMinFontSize Property
    LabelAutoFitStyle Property
    LineColor Property
    LineDashStyle Property
    LineWidth Property
    LogarithmBase Property
    MapAreaAttributes Property
    MaximumAutoSize Property
    PostBackValue Property
    ScaleBreakStyle Property
    ScaleView Property
    StripLines Property
    TextOrientation Property
    Url Property
    *)

                let area = ch.Area
                let series = ch.Series
                configureAxis area.AxisX (AxisXEnabled,AxisXLogarithmic,AxisXArrowStyle,AxisXLabelStyle,AxisXIsMarginVisible,AxisXMaximum,AxisXMinimum,AxisXMajorGrid,AxisXMinorGrid,AxisXMajorTickMark,AxisXMinorTickMark,AxisXName,AxisXTitle,AxisXTitleAlignment,AxisXTitleFont,AxisXTitleForeColor,AxisXToolTip) 
                configureAxis area.AxisY (AxisYEnabled,AxisYLogarithmic,AxisYArrowStyle,AxisYLabelStyle,AxisYIsMarginVisible,AxisYMaximum,AxisYMinimum,AxisYMajorGrid,AxisYMinorGrid,AxisYMajorTickMark,AxisYMinorTickMark,AxisYName,AxisYTitle,AxisYTitleAlignment,AxisYTitleFont,AxisYTitleForeColor,AxisYToolTip) 
                configureAxis area.AxisX2 (AxisX2Enabled,AxisX2Logarithmic,AxisX2ArrowStyle,AxisX2LabelStyle,AxisX2IsMarginVisible,AxisX2Maximum,AxisX2Minimum,AxisX2MajorGrid,AxisX2MinorGrid,AxisX2MajorTickMark,AxisX2MinorTickMark,AxisX2Name,AxisX2Title,AxisX2TitleAlignment,AxisX2TitleFont,AxisX2TitleForeColor,AxisX2ToolTip) 
                configureAxis area.AxisY2 (AxisY2Enabled,AxisY2Logarithmic,AxisY2ArrowStyle,AxisY2LabelStyle,AxisY2IsMarginVisible,AxisY2Maximum,AxisY2Minimum,AxisY2MajorGrid,AxisY2MinorGrid,AxisY2MajorTickMark,AxisY2MinorTickMark,AxisY2Name,AxisY2Title,AxisY2TitleAlignment,AxisY2TitleFont,AxisY2TitleForeColor,AxisY2ToolTip) 

                AreaBackground |> Option.iter (applyBackground area)

                Name |> Option.iter ch.set_Name
            
                ShowMarkerLines |> Option.iter (fun v -> ch.SetCustomProperty<bool>("ShowMarkerLines", v))
                LabelPosition |> Option.iter (fun v -> ch.SetCustomProperty<LabelPosition>("LabelStyle", v))
                SplineLineTension |> Option.iter (fun v -> ch.SetCustomProperty<float>("LineTension", v))
                PointStyle |> Option.iter (fun v -> ch.SetCustomProperty<PointStyle>("DrawingStyle", v))
                PointWidth |> Option.iter (fun v -> ch.SetCustomProperty<float>("PointWidth", v))
                MaxPixelPointWidth |> Option.iter (fun v -> ch.SetCustomProperty<int>("MaxPixelPointWidth", v))
                MinPixelPointWidth |> Option.iter (fun v -> ch.SetCustomProperty<int>("MinPixelPointWidth", v))
                PixelPointWidth |> Option.iter (fun v -> ch.SetCustomProperty<int>("PixelPointWidth", v))
                BarLabelPosition |> Option.iter (fun v -> ch.SetCustomProperty<BarLabelPosition>("BarLabelStyle", v))
                StackedGroupName |> Option.iter (fun v -> ch.SetCustomProperty<string>("StackedGroupName", v))


                // These are omitted
                //AlignWithChartArea |> Option.iter area.set_AlignWithChartArea
                //AlignmentStyle |> Option.iter area.set_AlignmentStyle
                //AlignmentOrientation |> Option.iter area.set_AlignmentOrientation

                Enable3D |> Option.iter area.Area3DStyle.set_Enable3D
                Area3DInclination |> Option.iter area.Area3DStyle.set_Inclination
                Area3DIsClustered |> Option.iter area.Area3DStyle.set_IsClustered
                Area3DIsRightAngleAxes |> Option.iter area.Area3DStyle.set_IsRightAngleAxes
                Area3DLightStyle |> Option.iter (int >> enum >> area.Area3DStyle.set_LightStyle)
                Area3DPerspective |> Option.iter area.Area3DStyle.set_Perspective
                Area3DPointDepth |> Option.iter area.Area3DStyle.set_PointDepth
                Area3DPointGapDepth |> Option.iter area.Area3DStyle.set_PointGapDepth
                Area3DRotation |> Option.iter area.Area3DStyle.set_Rotation
                Area3DWallWidth |> Option.iter area.Area3DStyle.set_WallWidth

                YAxisType |> Option.iter series.set_YAxisType
                XAxisType |> Option.iter series.set_XAxisType
  
                Color |> Option.iter series.set_Color
                BorderColor |> Option.iter series.set_BorderColor
                BorderWidth |> Option.iter series.set_BorderWidth

                DataPointLabel |> Option.iter series.set_Label
                DataPointLabelToolTip |> Option.iter series.set_LabelToolTip
                DataPointToolTip |> Option.iter series.set_ToolTip

                MarkerBorderColor |> Option.iter series.set_MarkerBorderColor
                MarkerBorderWidth |> Option.iter series.set_MarkerBorderWidth
                MarkerColor |> Option.iter series.set_MarkerColor
                MarkerSize |> Option.iter series.set_MarkerSize
                MarkerStep |> Option.iter series.set_MarkerStep
                MarkerStyle |> Option.iter (int >> enum >> series.set_MarkerStyle)

                let hasLegend = not (String.IsNullOrEmpty series.Name) || LegendEnabled.IsSome
                if hasLegend then 
                    let legend = ch.ForceLegend()
                    //LegendInsideArea |> Option.iter legend.set_InsideChartArea
                    LegendEnabled |> Option.iter legend.set_Enabled
                    LegendIsDockedInsideArea |> Option.iter legend.set_IsDockedInsideChartArea
                    LegendBackground |> Option.iter (applyBackground legend)
                    LegendFont |> Option.iter legend.set_Font
                    LegendAlignment |> Option.iter legend.set_Alignment
                    LegendDocking |> Option.iter (int >> enum >> legend.set_Docking)
                    LegendTitle |> Option.iter legend.set_Title
                    LegendTitleAlignment |> Option.iter legend.set_TitleAlignment
                    LegendTitleFont |> Option.iter legend.set_TitleFont
                    LegendTitleForeColor |> Option.iter legend.set_TitleForeColor
                    LegendBorderColor |> Option.iter legend.set_BorderColor
                    LegendBorderDashStyle |> Option.iter (int >> enum >> legend.set_BorderDashStyle)
                    LegendBorderWidth |> Option.iter legend.set_BorderWidth

            
                Margin |> Option.iter (fun margin -> ch.Margin <- margin) 

                Background |> Option.iter (applyBackground ch.Chart)

                Title |> Option.iter (fun t -> ch.ForceTitle().Text <- t)
                let hasTitle = (match ch.TryTitle with Some t -> not (String.IsNullOrEmpty t.Text) | None -> false)
                if hasTitle then 
                    let title = ch.ForceTitle()
                    TitleColor |> Option.iter title.set_ForeColor
                    TitleBorderColor |> Option.iter title.set_BorderColor
                    TitleBorderDashStyle |> Option.iter (int >> enum >> title.set_BorderDashStyle)
                    TitleBorderWidth |> Option.iter title.set_BorderWidth
                    TitleStyle |> Option.iter (int >> enum >> title.set_TextStyle)
                    TitleOrientation |> Option.iter (int >> enum >> title.set_TextOrientation)
                    TitleInsideArea |> Option.iter title.set_IsDockedInsideChartArea
                    TitleBackground |> Option.iter (applyBackground title)
                    TitleFont |> Option.iter title.set_Font
                    TitleAlignment |> Option.iter title.set_Alignment
                    TitleDocking |> Option.iter (int >> enum >> title.set_Docking)
                ch)

    /// Provides a set of static methods for creating charts.
    type Chart =
        /// Register a function that is used to automatically transform X values (keys) 
        /// of a non-primitive type to one of the types supported by the charting library
        /// (for example, by default DateTimeOffset is converted to DateTime)
        static member RegisterKeyConvertor<'T when 'T :> key>(conversion:'T -> key) =
          KeyConversions.registerConvertor(conversion)

        /// <summary>Emphasizes the degree of change over time and shows the relationship of the parts to a whole.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Area(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.Area))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Emphasizes the degree of change over time and shows the relationship of the parts to a whole.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Area(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.Area))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Illustrates comparisons among individual items</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Bar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.Bar))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Illustrates comparisons among individual items</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Bar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.Bar))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)



        /// <summary>Consists of one or more box symbols that summarize the distribution of the data within one or more data sets.</summary>
        /// <param name="data">The data for the chart, (xValue, Lower whisker, Upper whisker, Lower box, Upper box, Average, Median).</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member BoxPlotFromStatistics(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?Percentile,?ShowAverage,?ShowMedian,?ShowUnusualValues,?WhiskerPercentile) = 
            let c = 
                GenericChart.Create(mergeDataAndLabelsForXY6 data Labels, fun () -> GenericChart(SeriesChartType.BoxPlot))
                |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Percentile |> Option.iter (fun v -> c.SetCustomProperty<int>("BoxPlotPercentile", v))
            ShowAverage |> Option.iter (fun v -> c.SetCustomProperty<bool>("BoxPlotShowAverage", v))
            ShowMedian |> Option.iter (fun v -> c.SetCustomProperty<bool>("BoxPlotShowMedian", v))
            ShowUnusualValues |> Option.iter (fun v -> c.SetCustomProperty<bool>("BoxPlotShowUnusualValues", v))
            WhiskerPercentile |> Option.iter (fun v -> c.SetCustomProperty<int>("BoxPlotWhiskerPercentile", v))
            c

        /// <summary>Consists of one or more box symbols that summarize the distribution of the data within one or more data sets.</summary>
        /// <param name="data">The data for the chart in the form of a sequence of (xValue, yValues).</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member BoxPlotFromData(data,?Name,?Title, (* ?Labels, *) ?Color,?XTitle,?YTitle,?Percentile,?ShowAverage,?ShowMedian,?ShowUnusualValues,?WhiskerPercentile) = 
            let c = 
                GenericChart.Create(mergeDataAndLabelsForXYS data, fun () -> GenericChart(SeriesChartType.BoxPlot))
                |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Percentile |> Option.iter (fun v -> c.SetCustomProperty<int>("BoxPlotPercentile", v))
            ShowAverage |> Option.iter (fun v -> c.SetCustomProperty<bool>("BoxPlotShowAverage", v))
            ShowMedian |> Option.iter (fun v -> c.SetCustomProperty<bool>("BoxPlotShowMedian", v))
            ShowUnusualValues |> Option.iter (fun v -> c.SetCustomProperty<bool>("BoxPlotShowUnusualValues", v))
            WhiskerPercentile |> Option.iter (fun v -> c.SetCustomProperty<int>("BoxPlotWhiskerPercentile", v))
            c

        static member internal ConfigureBubble(c:GenericChart,vBubbleMaxSize,vBubbleMinSize,vBubbleScaleMax,vBubbleScaleMin,vBubbleUseSizeForLabel) = 
            vBubbleMaxSize |> Option.iter (fun v -> c.SetCustomProperty<int>("BubbleMaxSize", v))
            vBubbleMinSize |> Option.iter (fun v -> c.SetCustomProperty<int>("BubbleMinSize", v))
            vBubbleScaleMax |> Option.iter (fun v -> c.SetCustomProperty<float>("BubbleScaleMax", v))
            vBubbleScaleMin |> Option.iter (fun v -> c.SetCustomProperty<float>("BubbleScaleMin", v))
            vBubbleUseSizeForLabel |> Option.iter (fun v -> c.SetCustomProperty<bool>("BubbleUseSizeForLabel", v))

        /// <summary>A variation of the Point chart type, where the data points are replaced by bubbles of different sizes.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="BubbleMaxSize">The maximum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleMinSize">The minimum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleScaleMax">The maximum bubble size, which is a percentage of the chart area that is set by BubbleMaxSize. Any double.</param>
        /// <param name="BubbleScaleMin">The minimum bubble size, which is a percentage of the chart area that is set by BubbleMinSize. Any double.</param>
        /// <param name="UseSizeForLabel">Use the bubble size as the data point label.</param>
        static member Bubble(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?BubbleMaxSize,?BubbleMinSize,?BubbleScaleMax,?BubbleScaleMin,?UseSizeForLabel) = 
            let c = GenericChart.Create(mergeDataAndLabelsForXY2 data Labels, fun () -> GenericChart (SeriesChartType.Bubble) )
                    |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Chart.ConfigureBubble(c,BubbleMaxSize,BubbleMinSize,BubbleScaleMax,BubbleScaleMin,UseSizeForLabel)
            c

        /// <summary>A variation of the Point chart type, where the data points are replaced by bubbles of different sizes.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="BubbleMaxSize">The maximum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleMinSize">The minimum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleScaleMax">The maximum bubble size, which is a percentage of the chart area that is set by BubbleMaxSize. Any double.</param>
        /// <param name="BubbleScaleMin">The minimum bubble size, which is a percentage of the chart area that is set by BubbleMinSize. Any double.</param>
        /// <param name="UseSizeForLabel">Use the bubble size as the data point label.</param>
        static member Bubble(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?BubbleMaxSize,?BubbleMinSize,?BubbleScaleMax,?BubbleScaleMin,?UseSizeForLabel) = 
            let c = GenericChart.Create(mergeDataAndLabelsForY2 data Labels, fun () -> GenericChart (SeriesChartType.Bubble) )
                    |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Chart.ConfigureBubble(c,BubbleMaxSize,BubbleMinSize,BubbleScaleMax,BubbleScaleMin,UseSizeForLabel)
            c

        /// <summary>Used to display stock information using high, low, open and close values.</summary>
        /// <param name="data">The data for the chart as (time, high, low, open, close) tuples.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Candlestick(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY4 data Labels, fun() -> CandlestickChart() )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Used to display stock information using high, low, open and close values.</summary>
        /// <param name="data">The data for the chart as (high, low, open, close) tuples.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Candlestick(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY4 data Labels, fun() -> CandlestickChart() )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        static member internal ConfigureColumn(c:GenericChart, property, vPointWidth) = 
            vPointWidth |> Option.iter (fun v -> c.SetCustomProperty<float>(property, v))

        /// <summary>Uses a sequence of columns to compare values across categories.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="ColumnWidth">The width of columns versus whitespace as a percentage.</param>
        static member Column(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?ColumnWidth) = 
            let c =
                GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.Column))
                |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Chart.ConfigureColumn(c, "PointWidth", ColumnWidth)
            c
            
        /// <summary>Uses a sequence of columns to compare values across categories.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="ColumnWidth">The width of columns versus whitespace as a percentage.</param>
        static member Column(data,?Name,?Title,?Labels,?Color,?XTitle,?YTitle,?ColumnWidth) = 
            let c =
                GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.Column))
                 |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Chart.ConfigureColumn(c, "PointWidth", ColumnWidth)
            c

        /// <summary>Similar to the Pie chart type, except that it has a hole in the center.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Doughnut(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> DoughnutChart ())
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Similar to the Pie chart type, except that it has a hole in the center.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Doughnut(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> DoughnutChart ())
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Consists of lines with markers that are used to display statistical information about the data displayed in a graph.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member ErrorBar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY3 data Labels, fun () -> ErrorBarChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>A variation of the Line chart that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastLine(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.FastLine))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>A variation of the Line chart that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastLine(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.FastLine))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)


        /// <summary>A variation of the Point chart type that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastPoint(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.FastPoint))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>A variation of the Point chart type that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastPoint(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.FastPoint))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays in a funnel shape data that equals 100% when totaled.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Funnel(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> FunnelChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays in a funnel shape data that equals 100% when totaled.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Funnel(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> FunnelChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Generates a Histogram with reasonable defaults.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="LowerBound">The lower bound of the histogram.</param>
        /// <param name="UpperBound">The upper bound of the histogram.</param>
        /// <param name="Intervals">The number of intervals in the histogram.</param>
        static member Histogram(data:seq<#value>,?Name,?Title,?Color,?XTitle,?YTitle, ?LowerBound, ?UpperBound, ?Intervals) = 
            let data' = data |> Seq.map valueToDouble
            let lowerBound = 
                match LowerBound with
                | Some lowerBound -> lowerBound
                | _ -> Seq.min data'
            let upperBound = 
                match UpperBound with
                | Some upperBound -> upperBound
                | _ -> Seq.max data'
            let intervals = 
                match Intervals with
                | Some intervals -> intervals
                | _ -> 30. // corresponds to what ggplot does
            let bins = binData data' lowerBound upperBound intervals
            let data'' = bins |> Seq.map (fun b -> ( sprintf "%.2f" b.LowerBound), b.Count)
            Chart.Column(data'',?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?ColumnWidth=Some 0.95)

        /// <summary>Displays a series of connecting vertical lines where the thickness and direction of the lines are dependent on the action of the price value.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Kagi(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> KagiChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays a series of connecting vertical lines where the thickness and direction of the lines are dependent on the action of the price value.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Kagi(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> KagiChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Illustrates trends in data with the passing of time.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Line(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.Line) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Illustrates trends in data with the passing of time.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Line(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.Line) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)


        /// <summary>Shows how proportions of data, shown as pie-shaped pieces, contribute to the data as a whole.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pie(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> PieChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Shows how proportions of data, shown as pie-shaped pieces, contribute to the data as a whole.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pie(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> PieChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Uses points to represent data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Point(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?MarkerColor,?MarkerSize) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.Point))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?MarkerColor=MarkerColor,?MarkerSize=MarkerSize)

        /// <summary>Uses points to represent data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Point(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?MarkerColor,?MarkerSize) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.Point))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?MarkerColor=MarkerColor,?MarkerSize=MarkerSize)

        /// <summary>Disregards the passage of time and only displays changes in prices.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member PointAndFigure(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY2 data Labels, fun () -> PointAndFigureChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Disregards the passage of time and only displays changes in prices.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member PointAndFigure(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY2 data Labels, fun () -> PointAndFigureChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>A circular graph on which data points are displayed using the angle, and the distance from the center point.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Polar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> PolarChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>A circular graph on which data points are displayed using the angle, and the distance from the center point.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Polar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> PolarChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays data that, when combined, equals 100%.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>

        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pyramid(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> PyramidChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays data that, when combined, equals 100%.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>

        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pyramid(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> PyramidChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// A circular chart that is used primarily as a data
        /// comparison tool.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Radar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> RadarChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// A circular chart that is used primarily as a data
        /// comparison tool.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Radar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> RadarChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// Displays a range of data by plotting two Y values per data
        /// point, with each Y value being drawn as a line
        /// chart.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Range(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY2 data Labels, fun () -> GenericChart(SeriesChartType.Range) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// Displays a range of data by plotting two Y values per data
        /// point, with each Y value being drawn as a line
        /// chart.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Range(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY2 data Labels, fun () -> GenericChart(SeriesChartType.Range) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// Displays separate events that have beginning and end values.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member RangeBar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY2 data Labels, fun () -> GenericChart(SeriesChartType.RangeBar) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)


        /// <summary>
        /// Displays separate events that have beginning and end values.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member RangeBar(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY2 data Labels, fun () -> GenericChart(SeriesChartType.RangeBar) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)


        /// <summary>
        /// Displays a range of data by plotting two Y values
        /// per data point.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member RangeColumn(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY2 data Labels, fun () -> GenericChart(SeriesChartType.RangeColumn))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// Displays a range of data by plotting two Y values
        /// per data point.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member RangeColumn(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY2 data Labels, fun () -> GenericChart(SeriesChartType.RangeColumn))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// Displays a series of connecting vertical lines where the thickness
        /// and direction of the lines are dependent on the action
        /// of the price value.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Renko(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> RenkoChart ())
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// Displays a series of connecting vertical lines where the thickness
        /// and direction of the lines are dependent on the action
        /// of the price value.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Renko(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> RenkoChart ())
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// A Line chart that plots a fitted curve through each
        /// data point in a series.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Spline(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.Spline))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// A Line chart that plots a fitted curve through each
        /// data point in a series.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Spline(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.Spline))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// An Area chart that plots a fitted curve through each
        /// data point in a series.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member SplineArea(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.SplineArea) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// An Area chart that plots a fitted curve through each
        /// data point in a series.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member SplineArea(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.SplineArea) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)


        /// <summary>
        /// Displays a range of data by plotting two Y values per
        /// data point, with each Y value drawn as a line
        /// chart.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member SplineRange(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY2 data Labels, fun () -> GenericChart(SeriesChartType.SplineRange) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>
        /// Displays a range of data by plotting two Y values per
        /// data point, with each Y value drawn as a line
        /// chart.
        /// </summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member SplineRange(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY2 data Labels, fun () -> GenericChart(SeriesChartType.SplineRange) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Similar to the Line chart type, but uses vertical and horizontal lines to connect the data points in a series forming a step-like progression.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member StepLine(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.StepLine) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Similar to the Line chart type, but uses vertical and horizontal lines to connect the data points in a series forming a step-like progression.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member StepLine(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> GenericChart(SeriesChartType.StepLine) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)


        /// <summary>Displays significant stock price points including the high, low, open and close price points.</summary>
        /// <param name="data">The data for the chart as (index, high, low, open, close) tuples.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Stock(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY4 data Labels, fun () -> StockChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays significant stock price points including the high, low, open and close price points.</summary>
        /// <param name="data">The data for the chart, a sequence of (high, low, open, close) tuples.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Stock(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY4 data Labels, fun () -> StockChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)


        /// <summary>Displays a series of vertical boxes, or lines, that reflect changes in price values.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member ThreeLineBreak(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> ThreeLineBreakChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays a series of vertical boxes, or lines, that reflect changes in price values.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member ThreeLineBreak(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> ThreeLineBreakChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays series of the same chart type as stacked bars.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>

        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="StackedGroupName">The name of the stacked group.</param>
        static member StackedBar(data,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?StackedGroupName) = 
            GenericChart.Create(seqXY data, fun () -> GenericChart(SeriesChartType.StackedBar))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?StackedGroupName=StackedGroupName)

        /// <summary>Displays series of the same chart type as stacked bars.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="StackedGroupName">The name of the stacked group.</param>
        static member StackedBar100(data,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?StackedGroupName) = 
            GenericChart.Create(seqXY data, fun () -> GenericChart(SeriesChartType.StackedBar100) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?StackedGroupName=StackedGroupName)

        /// <summary>Displays series of the same chart type as stacked columns.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="StackedGroupName">The name of the stacked group.</param>
        static member StackedColumn(data,?Name,?Title,(* ?Labels, *)?Color,?XTitle,?YTitle,?StackedGroupName) = 
            GenericChart.Create(seqXY data, fun () -> GenericChart(SeriesChartType.StackedColumn) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?StackedGroupName=StackedGroupName)

        /// <summary>Displays series of the same chart type as stacked columns.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="StackedGroupName">The name of the stacked group.</param>
        static member StackedColumn100(data,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?StackedGroupName) = 
            GenericChart.Create(seqXY data, fun () -> GenericChart(SeriesChartType.StackedColumn100))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?StackedGroupName=StackedGroupName)

        /// <summary>Displays series of the same chart type as stacked areas.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="StackedGroupName">The name of the stacked group.</param>
        static member StackedArea(data,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?StackedGroupName) = 
            GenericChart.Create(seqXY data, fun () -> GenericChart(SeriesChartType.StackedArea))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?StackedGroupName=StackedGroupName)

        /// <summary>Displays series of the same chart type as stacked areas.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="StackedGroupName">The name of the stacked group.</param>
        static member StackedArea100(data,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?StackedGroupName) = 
            GenericChart.Create(seqXY data, fun () -> GenericChart(SeriesChartType.StackedArea100))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?StackedGroupName=StackedGroupName)

        /// Create a combined chart with the given charts placed in rows
        static member Rows charts = 
          SubplotChart(List.ofSeq charts, Orientation.Vertical) :> GenericChart

        /// Create a combined chart with the given charts placed in columns
        static member Columns charts = 
          SubplotChart(List.ofSeq charts, Orientation.Horizontal) :> GenericChart

        /// Create a combined chart with the given charts merged
        static member Combine charts = 
          CombinedChart (List.ofSeq charts) :> GenericChart

        /// Display a chart
        static member public Show (chart:GenericChart) =
                use cc = new ChartControl(chart)
                cc.Dock <- DockStyle.Fill
                use f = new Form()
                f.Size <- System.Drawing.Size(800, 600)
                f.Controls.Add cc
                f.ShowDialog() |> ignore

        /// Save a chart to a file in png format
        static member Save filename (chart:GenericChart) =
            use cc = new ChartControl(chart)
            cc.Dock <- DockStyle.Fill
            use f = new Form()
            f.Size <- System.Drawing.Size(800, 600)
            f.Controls.Add cc
            f.Load |> Event.add (fun _ -> chart.SaveChartAs(filename, ChartImageFormat.Png); f.Close()) 
            // Removed Application.Run: Triggers exception in fsi.exe.  See issue https://github.com/fslaborg/FSharp.Charting/issues/38
            // Application.Run f
            f.ShowDialog() |> ignore


    /// Contains static methods to construct charts whose data source is an event or observable which 
    /// updates the entire data set.
    type LiveChart = 

        /// <summary>Emphasizes the degree of change over time and shows the relationship of the parts to a whole.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Area(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Area(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Illustrates comparisons among individual items</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Bar(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Bar(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

(*
        /// <summary>Consists of one or more box symbols that summarize the distribution of the data within one or more data sets./// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>

        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="Percentile">The percentile value of the box of the Box Plot chart. Any integer from 0 to 50.</param>
        /// <param name="ShowAverage">Display the average value</param>
        /// <param name="ShowMedian">Display the median value</param>
        /// <param name="ShowUnusualValues">Display unusual values</param>
        /// <param name="WhiskerPercentile">The percentile value of the whiskers. Any integer from 0 to 50.</param>
        static member BoxPlot(data,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle,?Percentile,?ShowAverage,?ShowMedian,?ShowUnusualValues,?WhiskerPercentile) = 
            let c = 
                 GenericChart.Create(sixY data, (fun () -> GenericChart(SeriesChartType.BoxPlot))) 
                |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Chart.ConfigureBoxPlot(c,Percentile,ShowAverage,ShowMedian,ShowUnusualValues,WhiskerPercentile)
            c

        /// <summary>
        /// Consists of one or more box symbols that summarize the
        /// distribution of the data within one or more data sets.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>

        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member BoxPlot(data,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle,?Percentile,?ShowAverage,?ShowMedian,?ShowUnusualValues,?WhiskerPercentile) = 
            let c = 
                GenericChart.Create(mergeDataAndLabelsForXY6 data Labels, fun () -> GenericChart(SeriesChartType.BoxPlot))
                |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
            Chart.ConfigureBoxPlot(c,Percentile,ShowAverage,ShowMedian,ShowUnusualValues,WhiskerPercentile)
            c
*)


        /// <summary>
        /// A variation of the Point chart type, where the data
        /// points are replaced by bubbles of different sizes.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="BubbleMaxSize">The maximum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleMinSize">The minimum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleScaleMax">The maximum bubble size, which is a percentage of the chart area that is set by BubbleMaxSize. Any double.</param>
        /// <param name="BubbleScaleMin">The minimum bubble size, which is a percentage of the chart area that is set by BubbleMinSize. Any double.</param>
        /// <param name="UseSizeForLabel">Use the bubble size as the data point label.</param>
        static member Bubble(data:IObservable<#seq<#key * #value * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle,?BubbleMaxSize,?BubbleMinSize,?BubbleScaleMax,?BubbleScaleMin,?UseSizeForLabel) = 
            Chart.Bubble(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?BubbleMaxSize=BubbleMaxSize,?BubbleMinSize=BubbleMinSize,?BubbleScaleMax=BubbleScaleMax,?BubbleScaleMin=BubbleScaleMin,?UseSizeForLabel=UseSizeForLabel)


        /// <summary>A variation of the Point chart type, where the data points are replaced by bubbles of different sizes.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="BubbleMaxSize">The maximum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleMinSize">The minimum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleScaleMax">The maximum bubble size, which is a percentage of the chart area that is set by BubbleMaxSize. Any double.</param>
        /// <param name="BubbleScaleMin">The minimum bubble size, which is a percentage of the chart area that is set by BubbleMinSize. Any double.</param>
        /// <param name="UseSizeForLabel">Use the bubble size as the data point label.</param>
        static member BubbleIncremental(data:IObservable<#key * #value * #value>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle,?BubbleMaxSize,?BubbleMinSize,?BubbleScaleMax,?BubbleScaleMin,?UseSizeForLabel) = 
            Chart.Bubble(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?BubbleMaxSize=BubbleMaxSize,?BubbleMinSize=BubbleMinSize,?BubbleScaleMax=BubbleScaleMax,?BubbleScaleMin=BubbleScaleMin,?UseSizeForLabel=UseSizeForLabel)

        /// <summary>Used to display stock information using high, low, open and close values.</summary>
        /// <param name="data">The data for the chart as (high, low, open, close) tuples. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Candlestick(data:IObservable<seq<_ * _ * _ * _>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Candlestick(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Used to display stock information using high, low, open and close values.</summary>
        /// <param name="data">The data for the chart as (time, high, low, open, close) tuples. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Candlestick(data:IObservable<#seq<#key * #value * #value * #value * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Candlestick(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Used to display stock information using high, low, open and close values.</summary>
        /// <param name="data">The data for the chart as (high, low, open, close) tuples. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member CandlestickIncremental(data:IObservable<#key * #value * #value * #value>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Candlestick(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Used to display stock information using high, low, open and close values.</summary>
        /// <param name="data">The data for the chart as (time, high, low, open, close) tuples. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member CandlestickIncremental(data:IObservable<#key * #value * #value * #value * #value>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Candlestick(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>Uses a sequence of columns to compare values across categories.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
     //   /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="ColumnWidth">The width of columns versus whitespace as a percentage.</param>
        static member Column(data:IObservable<#seq<#key * #value>>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?ColumnWidth) = 
            Chart.Column(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?ColumnWidth=ColumnWidth)

        /// <summary>Uses a sequence of columns to compare values across categories.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
    //    /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="ColumnWidth">The width of columns versus whitespace as a percentage.</param>
        static member ColumnIncremental(data:IObservable<#key * #value>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?ColumnWidth) = 
            Chart.Column(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?ColumnWidth=ColumnWidth)

        /// <summary>Similar to the Pie chart type, except that it has a hole in the center.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Doughnut(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Doughnut(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Consists of lines with markers that are used to display statistical information about the data displayed in a graph.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member ErrorBar(data:IObservable<#seq<#key * #value * #value * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.ErrorBar(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>A variation of the Line chart that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastLine(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.FastLine(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>An incremental variation of the Line chart that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastLineIncremental(data:IObservable<#key * #value>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.FastLine(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>A variation of the Point chart type that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastPoint(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.FastPoint(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>A variation of the Point chart type that significantly reduces the drawing time of a series that contains a very large number of data points.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member FastPointIncremental(data:IObservable<#key * #value>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.FastPoint(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Displays in a funnel shape data that equals 100% when totaled.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Funnel(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Funnel(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>
        /// Displays a series of connecting vertical lines where the thickness
        /// and direction of the lines are dependent on the action
        /// of the price value.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Kagi(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Kagi(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)



        /// <summary>An updating chart which illustrates trends in data with the passing of time.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
  //      /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Line(data:IObservable<#seq<#key * #value>>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle) = 
            Chart.Line(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>An incrementally updating chart which illustrates trends in data with the passing of time.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
//        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member LineIncremental(data:IObservable<#key * #value>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle) = 
            Chart.Line(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>An updating chart which uses points to represent data points, where updates replace the entire data set.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
     //   /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pie(data:IObservable<#seq<#key * #value>>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle) = 
            Chart.Pie(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle)



        /// <summary>An incrementally updating chart which uses points to represent data points.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
    //    /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member PieIncremental(data:IObservable<#key * #value>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle) = 
            Chart.Pie(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>An updating chart which uses points to represent data points, where updates replace the entire data set.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
     //   /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Point(data:IObservable<#seq<#key * #value>>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?MarkerColor,?MarkerSize) = 
            Chart.Point(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?MarkerColor=MarkerColor,?MarkerSize=MarkerSize)


        /// <summary>An incrementally updating chart which uses points to represent data points.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
    //    /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member PointIncremental(data:IObservable<#key * #value>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?MarkerColor,?MarkerSize) = 
            Chart.Point(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?MarkerColor=MarkerColor,?MarkerSize=MarkerSize)

        /// <summary>
        /// Disregards the passage of time and only displays changes in prices.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member PointAndFigure(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.PointAndFigure(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>
        /// A circular graph on which data points are displayed using
        /// the angle, and the distance from the center point.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Polar(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Polar(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>
        /// Displays data that, when combined, equals 100%.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pyramid(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Pyramid(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>A circular chart that is used primarily as a data comparison tool.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Radar(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Radar(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Displays a range of data by plotting two Y values per data point, with each Y value being drawn as a line chart.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Range(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Range(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Displays separate events that have beginning and end values.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member RangeBar(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.RangeBar(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>Displays a range of data by plotting two Y values per data point.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member RangeColumn(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.RangeColumn(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Displays a series of connecting vertical lines where the thickness and direction of the lines are dependent on the action of the price value.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Renko(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Renko(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>A Line chart that plots a fitted curve through each data point in a series.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Spline(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Spline(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>An Area chart that plots a fitted curve through each data point in a series.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member SplineArea(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.SplineArea(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>Displays a range of data by plotting two Y values per data point, with each Y value drawn as a line chart.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member SplineRange(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.SplineRange(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>
        /// An Area chart that stacks two or more data series
        /// on top of one another.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member StackedArea(data,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.StackedArea(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

(*

        /// <summary>
        /// Displays series of the same chart type as stacked bars.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member StackedBar(data,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.StackedBar(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>
        /// Used to compare the contribution of each value to a
        /// total across categories.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member StackedColumn(data,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForXY data Labels, fun () -> GenericChart(SeriesChartType.StackedColumn) )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

*)

        /// <summary>
        /// Similar to the Line chart type, but uses vertical and
        /// horizontal lines to connect the data points in a series
        /// forming a step-like progression.
        /// </summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member StepLine(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.StepLine(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>Displays significant stock price points including the high, low, open and close price points.</summary>
        /// <param name="data">The data for the chart as a sequence of (index, high, low, open, close) tuples. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Stock(data:IObservable< #seq<#key * #value * #value * #value * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Stock(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Displays significant stock price points including the high, low, open and close price points.</summary>
        /// <param name="data">The data for the chart as a sequence of (high, low, open, close) tuples. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Stock(data:IObservable< #seq<#key * #value * #value * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.Stock(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>Displays a series of vertical boxes, or lines, that reflect changes in price values.</summary>
        /// <param name="data">The data for the chart. Each observation replaces the entire data on the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        // /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member ThreeLineBreak(data:IObservable<#seq<#key * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle) = 
            Chart.ThreeLineBreak(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


    [<AutoOpen>]
    [<Obsolete("Do not open this module, it is internal only")>]
    module _ChartStyleExtensions = 

        let private createCounter() = 
                let count = ref 0
                (fun () -> incr count; !count)

        let private dict = new Dictionary<string, unit -> int>()

        let private ProvideTitle (chart:GenericChart) = 
                let defaultName = 
                    match String.IsNullOrEmpty(chart.Name) with
                        | true -> chart.ChartTypeName  + " Chart"
                        | false -> chart.Name

                match dict.ContainsKey(defaultName) with
                    | true ->
                        sprintf "%s (%i)" defaultName (dict.[defaultName]())
                    | false ->
                        dict.Add(defaultName, createCounter())
                        defaultName


        type GenericChart with
            /// <summary>Apply styling to the X Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="IsMarginVisible">Determines whether to add a margin to the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            member ch.WithXAxis
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisXLogarithmic=Log,?AxisXEnabled=Enabled, ?AxisXArrowStyle=ArrowStyle, ?AxisXLabelStyle=LabelStyle, ?AxisXIsMarginVisible=IsMarginVisible, ?AxisXMaximum=Max, ?AxisXMinimum=Min, ?AxisXMajorGrid=MajorGrid, ?AxisXMinorGrid=MinorGrid, ?AxisXMajorTickMark=MajorTickMark, ?AxisXMinorTickMark=MinorTickMark, 
                                          ?AxisXTitle=Title, ?AxisXTitleAlignment=TitleAlignment, ?AxisXTitleFont=titleFont, ?AxisXTitleForeColor=TitleColor, ?AxisXToolTip=ToolTip) 

            /// <summary>Apply styling to the Y Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="IsMarginVisible">Determines whether to add a margin to the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            member ch.WithYAxis
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisYLogarithmic=Log,?AxisYEnabled=Enabled,?AxisYArrowStyle=ArrowStyle,  ?AxisYLabelStyle=LabelStyle, ?AxisYIsMarginVisible=IsMarginVisible, ?AxisYMaximum=Max, ?AxisYMinimum=Min, ?AxisYMajorGrid=MajorGrid, ?AxisYMinorGrid=MinorGrid, ?AxisYMajorTickMark=MajorTickMark, ?AxisYMinorTickMark=MinorTickMark, ?AxisYTitle=Title, ?AxisYTitleAlignment=TitleAlignment, ?AxisYTitleFont=titleFont, ?AxisYTitleForeColor=TitleColor, ?AxisYToolTip=ToolTip) 

            /// <summary>Apply styling to the second X axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="IsMarginVisible">Determines whether to add a margin to the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            member ch.WithXAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisX2Logarithmic=Log,?AxisX2Enabled=Enabled, ?AxisX2ArrowStyle=ArrowStyle, ?AxisX2LabelStyle=LabelStyle, ?AxisX2IsMarginVisible=IsMarginVisible, ?AxisX2Maximum=Max, ?AxisX2Minimum=Min, ?AxisX2MajorGrid=MajorGrid, ?AxisX2MinorGrid=MinorGrid, ?AxisX2MajorTickMark=MajorTickMark, ?AxisX2MinorTickMark=MinorTickMark, ?AxisX2Title=Title, ?AxisX2TitleAlignment=TitleAlignment, ?AxisX2TitleFont=titleFont, ?AxisX2TitleForeColor=TitleColor, ?AxisX2ToolTip=ToolTip) 

            /// <summary>Apply styling to the second Y axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="IsMarginVisible">Determines whether to add a margin to the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            member ch.WithYAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark,
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisY2Logarithmic=Log,?AxisY2Enabled=Enabled, ?AxisY2ArrowStyle=ArrowStyle, ?AxisY2LabelStyle=LabelStyle, ?AxisY2IsMarginVisible=IsMarginVisible, ?AxisY2Maximum=Max, ?AxisY2Minimum=Min, ?AxisY2MajorGrid=MajorGrid, ?AxisY2MinorGrid=MinorGrid, ?AxisY2MajorTickMark=MajorTickMark, ?AxisY2MinorTickMark=MinorTickMark, ?AxisY2Title=Title, ?AxisY2TitleAlignment=TitleAlignment, ?AxisY2TitleFont=titleFont, ?AxisY2TitleForeColor=TitleColor, ?AxisY2ToolTip=ToolTip) 

            /// <summary>Apply content and styling to the title, if present</summary>
            /// <param name="InsideArea">If false, locates the title outside the chart area</param>
            /// <param name="Text">The text of the title</param>
            /// <param name="Style">The text style for the title</param>
            /// <param name="FontName">The font name for the title</param>
            /// <param name="FontSize">The font size for the title</param>
            /// <param name="FontStyle">The font style for the title</param>
            /// <param name="Background">The background for the title</param>
            /// <param name="Color">The color for the title</param>
            /// <param name="BorderColor">The border color for the title</param>
            /// <param name="BorderWidth">The border width for the title</param>
            /// <param name="BorderDashStyle">The border dash style for the title</param>
            /// <param name="Orientation">The orientation for the title</param>
            /// <param name="Alignment">The alignment for the title</param>
            /// <param name="Docking">The docking location for the title</param>
            member ch.WithTitle
                (?Text, ?InsideArea, ?Style, ?FontName, ?FontSize, ?FontStyle, ?Background, ?Color, ?BorderColor, ?BorderWidth, ?BorderDashStyle, 
                 ?Orientation, ?Alignment, ?Docking) = 
                let font = StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) 
                ch |> Helpers.ApplyStyles(?Title=Text, ?TitleStyle=Style, ?TitleFont=font, ?TitleBackground=Background, ?TitleColor=Color, ?TitleBorderColor=BorderColor, ?TitleBorderWidth=BorderWidth, ?TitleBorderDashStyle=BorderDashStyle, ?TitleOrientation=Orientation, ?TitleAlignment=Alignment, ?TitleDocking=Docking, ?TitleInsideArea=InsideArea)

            /// <summary>Enables 3D styling for the chart</summary>
            /// <param name="ShowMarkerLines">Specifies whether marker lines are displayed when rendered in 3D.</param>
            member ch.With3D
                (?Inclination, ?IsClustered, ?IsRightAngleAxes, ?LightStyle, ?Perspective, ?PointDepth, ?PointGapDepth, ?ShowMarkerLines, ?Rotation, ?WallWidth) =
                ch |> Helpers.ApplyStyles(Enable3D=true, ?Area3DInclination=Inclination, ?Area3DIsClustered=IsClustered, ?Area3DIsRightAngleAxes=IsRightAngleAxes, ?Area3DLightStyle=LightStyle, ?Area3DPerspective=Perspective, ?Area3DPointDepth=PointDepth, ?Area3DPointGapDepth=PointGapDepth, ?Area3DRotation=Rotation, ?ShowMarkerLines=ShowMarkerLines, ?Area3DWallWidth=WallWidth)

            /// <summary>Add styling to the markers and points of the chart</summary>
            /// <param name="LabelPosition">The relative data point width. Any double from 0 to 2.</param>
            /// <param name="PointStyle">The drawing style of data points.</param>
            /// <param name="MaxPixelPointWidth">The maximum data point width in pixels. Any integer &gt; 0.</param>
            /// <param name="MinPixelPointWidth">The minimum data point width in pixels. Any integer &gt; 0.</param>
            /// <param name="PixelPointWidth">The data point width in pixels. Any integer &gt; 2.</param>
            member ch.WithMarkers
                (?Color, ?Size, ?Step, ?Style, ?BorderColor, ?BorderWidth, ?PointWidth, ?PixelPointWidth, ?PointStyle, ?MaxPixelPointWidth, ?MinPixelPointWidth) =
              ch |> Helpers.ApplyStyles(?MarkerColor=Color, ?MarkerSize=Size, ?MarkerStep=Step, ?MarkerStyle=Style, ?MarkerBorderColor=BorderColor, ?MarkerBorderWidth=BorderWidth,
                                        ?PointWidth=PointWidth,?PointStyle=PointStyle,?MaxPixelPointWidth=MaxPixelPointWidth, ?MinPixelPointWidth=MinPixelPointWidth, ?PixelPointWidth=PixelPointWidth)

            /// <summary>Apply styling to the legend of the chart</summary>
            /// <param name="InsideArea">If false, places the legend outside the chart area</param>
            member ch.WithLegend
              (?Enabled,?Title, ?Background, ?FontName,  ?FontSize, ?FontStyle, ?Alignment, ?Docking, ?InsideArea, 
               ?TitleAlignment, ?TitleFont, ?TitleColor, ?BorderColor, ?BorderWidth, ?BorderDashStyle) = 
              let font = StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) 
              // Specifying AndLegend enables the legend by default
              let legendEnabled = defaultArg Enabled true
              ch |> Helpers.ApplyStyles(LegendEnabled=legendEnabled,?LegendTitle=Title, ?LegendBackground=Background, ?LegendFont=font, ?LegendAlignment=Alignment, ?LegendDocking=Docking, ?LegendIsDockedInsideArea=InsideArea,
                                        ?LegendTitleAlignment=TitleAlignment, ?LegendTitleFont=TitleFont, ?LegendTitleForeColor=TitleColor, ?LegendBorderColor=BorderColor, ?LegendBorderWidth=BorderWidth, ?LegendBorderDashStyle=BorderDashStyle)

            /// <summary>Add data point labels and apply styling to the labels</summary>
            /// <param name="LabelPosition">The relative data point width. Any double from 0 to 2.</param>
            /// <param name="BarLabelPosition">For Bar charts, specifies the placement of the data point label</param>
            member ch.WithDataPointLabels
              (?Label, ?LabelPosition, ?LabelToolTip, ?PointToolTip, ?BarLabelPosition) = 
              ch |> Helpers.ApplyStyles(?DataPointLabel=Label,?DataPointLabelToolTip=LabelToolTip, ?DataPointToolTip=PointToolTip, ?LabelPosition=LabelPosition,?BarLabelPosition=BarLabelPosition)
           
            /// <summary>Apply additional styling to the chart</summary>
            /// <param name="Name">The name of the data series</param>
            /// <param name="Color">The foreground color for the data series</param>
            /// <param name="AreaBackground"></param>
            /// <param name="Margin"></param>
            /// <param name="Background"></param>
            /// <param name="BorderColor">The border color for the data series</param>
            /// <param name="BorderWidth">The border width for the data series</param>
            // TODO: move SplineLineTension to the specific chart types it applies to
            /// <param name="SplineLineTension">The line tension for the drawing of curves between data points. Any double from 0 to 2.</param>
            member ch.WithStyling
              (?Name,?Color, ?AreaBackground,?Margin,?Background,?BorderColor, ?BorderWidth, ?SplineLineTension(* , ?AlignWithChartArea, ?AlignmentOrientation, ?AlignmentStyle *) ) =
              ch |> Helpers.ApplyStyles(?Name=Name,?Color=Color, ?AreaBackground=AreaBackground,?Margin=Margin,?Background=Background,?BorderColor=BorderColor, ?BorderWidth=BorderWidth, ?SplineLineTension=SplineLineTension(* , ?AlignWithChartArea=AlignWithChartArea , ?AlignmentOrientation=AlignmentOrientation, ?AlignmentStyle=AlignmentStyle *) )

    (*
            member ch.WithValues
            /// The value to be used for empty points.
            member x.EmptyPointValue
                with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
                and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)
    *)

            /// Display the chart in a new ChartControl in a new Form()
            member ch.ShowChart () =
                let frm = new Form(Visible = true, TopMost = true, Width = 700, Height = 500)
                let ctl = new ChartControl(ch, Dock = DockStyle.Fill)
                frm.Text <- ProvideTitle ch
                frm.Controls.Add(ctl)
                frm.Show()
                ctl.Focus() |> ignore
                frm

        [<Obsolete("This type is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
        type AreaProperties() = 
            member area.AxisX<'T when 'T :> GenericChart>
              ( ?Enabled, ?LabelStyle, ?IsMarginVisible, ?Maximum, ?Minimum, ?MajorGrid, ?MinorGrid, ?MajorTickMark, ?MinorTickMark, ?Name,
                ?Title, ?TitleAlignment, ?TitleFont, ?TitleForeColor, ?ToolTip) = 
              fun (ch:'T) -> 
                //ch.Area.AxisX <- new Axis(null, AxisName.X)
                Enabled |> Option.iter ch.Area.AxisX.set_Enabled
                LabelStyle |> Option.iter ch.Area.AxisX.set_LabelStyle
                IsMarginVisible |> Option.iter ch.Area.AxisX.set_IsMarginVisible
                Maximum |> Option.iter ch.Area.AxisX.set_Maximum
                Minimum |> Option.iter ch.Area.AxisX.set_Minimum
                MajorGrid |> Option.iter ch.Area.AxisX.set_MajorGrid
                MinorGrid |> Option.iter ch.Area.AxisX.set_MinorGrid
                MajorTickMark |> Option.iter ch.Area.AxisX.set_MajorTickMark
                MinorTickMark |> Option.iter ch.Area.AxisX.set_MinorTickMark
                Name |> Option.iter ch.Area.AxisX.set_Name
                Title |> Option.iter ch.Area.AxisX.set_Title
                TitleAlignment |> Option.iter ch.Area.AxisX.set_TitleAlignment
                TitleFont |> Option.iter ch.Area.AxisX.set_TitleFont
                TitleForeColor |> Option.iter ch.Area.AxisX.set_TitleForeColor
                ToolTip |> Option.iter ch.Area.AxisX.set_ToolTip
                ch

            member area.AxisY<'T when 'T :> GenericChart>
              ( ?Enabled, ?LabelStyle, ?IsMarginVisible, ?Maximum, ?Minimum, ?MajorGrid, ?MinorGrid, ?MajorTickMark, ?MinorTickMark, ?Name,
                ?Title, ?TitleAlignment, ?TitleFont, ?TitleForeColor, ?ToolTip) =
              fun (ch:'T) -> 
                //ch.Area.AxisY <- new Axis(null, AxisName.Y)
                Enabled |> Option.iter ch.Area.AxisY.set_Enabled
                LabelStyle |> Option.iter ch.Area.AxisY.set_LabelStyle
                IsMarginVisible |> Option.iter ch.Area.AxisY.set_IsMarginVisible
                Maximum |> Option.iter ch.Area.AxisY.set_Maximum
                Minimum |> Option.iter ch.Area.AxisY.set_Minimum
                MajorGrid |> Option.iter ch.Area.AxisY.set_MajorGrid
                MinorGrid |> Option.iter ch.Area.AxisY.set_MinorGrid
                MajorTickMark |> Option.iter ch.Area.AxisY.set_MajorTickMark
                MinorTickMark |> Option.iter ch.Area.AxisY.set_MinorTickMark
                Name |> Option.iter ch.Area.AxisY.set_Name
                Title |> Option.iter ch.Area.AxisY.set_Title
                TitleAlignment |> Option.iter ch.Area.AxisY.set_TitleAlignment
                TitleFont |> Option.iter ch.Area.AxisY.set_TitleFont
                TitleForeColor |> Option.iter ch.Area.AxisY.set_TitleForeColor
                ToolTip |> Option.iter ch.Area.AxisY.set_ToolTip
                ch

            member area.AxisX2<'T when 'T :> GenericChart>
              ( ?Enabled, ?LabelStyle, ?IsMarginVisible, ?Maximum, ?Minimum, ?MajorGrid, ?MinorGrid, ?MajorTickMark, ?MinorTickMark, ?Name,
                ?Title, ?TitleAlignment, ?TitleFont, ?TitleForeColor, ?ToolTip) =
              fun (ch:'T) -> 
                //ch.Area.AxisX <- new Axis(null, AxisName.X)
                Enabled |> Option.iter ch.Area.AxisX2.set_Enabled
                LabelStyle |> Option.iter ch.Area.AxisX2.set_LabelStyle
                IsMarginVisible |> Option.iter ch.Area.AxisX2.set_IsMarginVisible
                Maximum |> Option.iter ch.Area.AxisX2.set_Maximum
                Minimum |> Option.iter ch.Area.AxisX2.set_Minimum
                MajorGrid |> Option.iter ch.Area.AxisX2.set_MajorGrid
                MinorGrid |> Option.iter ch.Area.AxisX2.set_MinorGrid
                MajorTickMark |> Option.iter ch.Area.AxisX2.set_MajorTickMark
                MinorTickMark |> Option.iter ch.Area.AxisX2.set_MinorTickMark
                Name |> Option.iter ch.Area.AxisX2.set_Name
                Title |> Option.iter ch.Area.AxisX2.set_Title
                TitleAlignment |> Option.iter ch.Area.AxisX2.set_TitleAlignment
                TitleFont |> Option.iter ch.Area.AxisX2.set_TitleFont
                TitleForeColor |> Option.iter ch.Area.AxisX2.set_TitleForeColor
                ToolTip |> Option.iter ch.Area.AxisX2.set_ToolTip
                ch

            member area.AxisY2<'T when 'T :> GenericChart>
              ( ?Enabled, ?LabelStyle, ?IsMarginVisible, ?Maximum, ?Minimum, ?MajorGrid, ?MinorGrid, ?MajorTickMark, ?MinorTickMark, ?Name,
                ?Title, ?TitleAlignment, ?TitleFont, ?TitleForeColor, ?ToolTip) =
              fun (ch:'T) -> 
                //ch.Area.AxisY <- new Axis(null, AxisName.Y)
                Enabled |> Option.iter ch.Area.AxisY2.set_Enabled
                LabelStyle |> Option.iter ch.Area.AxisY2.set_LabelStyle
                IsMarginVisible |> Option.iter ch.Area.AxisY2.set_IsMarginVisible
                Maximum |> Option.iter ch.Area.AxisY2.set_Maximum
                Minimum |> Option.iter ch.Area.AxisY2.set_Minimum
                MajorGrid |> Option.iter ch.Area.AxisY2.set_MajorGrid
                MinorGrid |> Option.iter ch.Area.AxisY2.set_MinorGrid
                MajorTickMark |> Option.iter ch.Area.AxisY2.set_MajorTickMark
                MinorTickMark |> Option.iter ch.Area.AxisY2.set_MinorTickMark
                Name |> Option.iter ch.Area.AxisY2.set_Name
                Title |> Option.iter ch.Area.AxisY2.set_Title
                TitleAlignment |> Option.iter ch.Area.AxisY2.set_TitleAlignment
                TitleFont |> Option.iter ch.Area.AxisY2.set_TitleFont
                TitleForeColor |> Option.iter ch.Area.AxisY2.set_TitleForeColor
                ToolTip |> Option.iter ch.Area.AxisY2.set_ToolTip
                ch

            member area.Style<'T when 'T :> GenericChart> (?Background) =
              fun (ch:'T) -> 
                Background |> Option.iter (applyBackground ch.Area)
                ch

            member area.Name<'T when 'T :> GenericChart> (?Name) =
              fun (ch:'T) -> 
                Name |> Option.iter ch.Area.set_Name
                ch

            member area.Align<'T when 'T :> GenericChart> (?Area, ?Orientation, ?Style) =
              fun (ch:'T) -> 
                Area |> Option.iter ch.Area.set_AlignWithChartArea
                Style |> Option.iter ch.Area.set_AlignmentStyle
                Orientation |> Option.iter ch.Area.set_AlignmentOrientation
                ch

            member area.Area3DStyle<'T when 'T :> GenericChart> (?Enable3D, ?Inclination, ?IsClustered, ?IsRightAngleAxes, ?LightStyle, ?Perspective, ?PointDepth, ?PointGapDepth, ?Rotation, ?WallWidth) =
              fun (ch:'T) ->
                Enable3D |> Option.iter ch.Area.Area3DStyle.set_Enable3D
                Inclination |> Option.iter ch.Area.Area3DStyle.set_Inclination
                IsClustered |> Option.iter ch.Area.Area3DStyle.set_IsClustered
                IsRightAngleAxes |> Option.iter ch.Area.Area3DStyle.set_IsRightAngleAxes
                LightStyle |> Option.iter ch.Area.Area3DStyle.set_LightStyle
                Perspective |> Option.iter ch.Area.Area3DStyle.set_Perspective
                PointDepth |> Option.iter ch.Area.Area3DStyle.set_PointDepth
                PointGapDepth |> Option.iter ch.Area.Area3DStyle.set_PointGapDepth
                Rotation |> Option.iter ch.Area.Area3DStyle.set_Rotation
                WallWidth |> Option.iter ch.Area.Area3DStyle.set_WallWidth
                ch


        [<Obsolete("This type is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
        type SeriesProperties() = 
  
              member series.AxisType<'T when 'T :> GenericChart> (?YAxisType, ?XAxisType) =
                fun (ch:'T) -> 
                  YAxisType |> Option.iter ch.Series.set_YAxisType
                  XAxisType |> Option.iter ch.Series.set_XAxisType
                  ch
  
              member series.Style<'T when 'T :> GenericChart> (?Color, ?BorderColor, ?BorderWidth) =
                fun (ch:'T) -> 
                  Color |> Option.iter ch.Series.set_Color
                  BorderColor |> Option.iter ch.Series.set_BorderColor
                  BorderWidth |> Option.iter ch.Series.set_BorderWidth
                  ch

              member series.DataPoint<'T when 'T :> GenericChart> (?Label, ?LabelToolTip, ?ToolTip) =
                fun (ch:'T) ->               
                  Label |> Option.iter ch.Series.set_Label
                  LabelToolTip |> Option.iter ch.Series.set_LabelToolTip
                  ToolTip |> Option.iter ch.Series.set_ToolTip
                  ch

              member series.Marker<'T when 'T :> GenericChart> (?Color, ?Size, ?Step, ?Style, ?BorderColor, ?BorderWidth) =
                fun (ch:'T) -> 
                  BorderColor |> Option.iter ch.Series.set_MarkerBorderColor
                  BorderWidth |> Option.iter ch.Series.set_MarkerBorderWidth
                  Color |> Option.iter ch.Series.set_MarkerColor
                  Size |> Option.iter ch.Series.set_MarkerSize
                  Step |> Option.iter ch.Series.set_MarkerStep
                  Style |> Option.iter ch.Series.set_MarkerStyle
                  ch

        type Chart with 

            /// <summary>Apply styling to the X Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="IsMarginVisible">Determines whether to add a margin to the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            static member WithXAxis
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisXLogarithmic=Log,?AxisXEnabled=Enabled, ?AxisXArrowStyle=ArrowStyle, ?AxisXLabelStyle=LabelStyle, ?AxisXIsMarginVisible=IsMarginVisible, ?AxisXMaximum=Max, ?AxisXMinimum=Min, ?AxisXMajorGrid=MajorGrid, ?AxisXMinorGrid=MinorGrid, ?AxisXMajorTickMark=MajorTickMark, ?AxisXMinorTickMark=MinorTickMark, 
                                          ?AxisXTitle=Title, ?AxisXTitleAlignment=TitleAlignment, ?AxisXTitleFont=titleFont, ?AxisXTitleForeColor=TitleColor, ?AxisXToolTip=ToolTip) 

            /// <summary>Apply styling to the Y Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            static member WithYAxis
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisYLogarithmic=Log,?AxisYEnabled=Enabled,?AxisYArrowStyle=ArrowStyle,  ?AxisYLabelStyle=LabelStyle, ?AxisYIsMarginVisible=IsMarginVisible, ?AxisYMaximum=Max, ?AxisYMinimum=Min, ?AxisYMajorGrid=MajorGrid, ?AxisYMinorGrid=MinorGrid, ?AxisYMajorTickMark=MajorTickMark, ?AxisYMinorTickMark=MinorTickMark, ?AxisYTitle=Title, ?AxisYTitleAlignment=TitleAlignment, ?AxisYTitleFont=titleFont, ?AxisYTitleForeColor=TitleColor, ?AxisYToolTip=ToolTip) 

            /// <summary>Apply styling to the second X axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="IsMarginVisible">Determines whether to add a margin to the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            static member WithXAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisX2Logarithmic=Log,?AxisX2Enabled=Enabled, ?AxisX2ArrowStyle=ArrowStyle, ?AxisX2LabelStyle=LabelStyle, ?AxisX2IsMarginVisible=IsMarginVisible, ?AxisX2Maximum=Max, ?AxisX2Minimum=Min, ?AxisX2MajorGrid=MajorGrid, ?AxisX2MinorGrid=MinorGrid, ?AxisX2MajorTickMark=MajorTickMark, ?AxisX2MinorTickMark=MinorTickMark, ?AxisX2Title=Title, ?AxisX2TitleAlignment=TitleAlignment, ?AxisX2TitleFont=titleFont, ?AxisX2TitleForeColor=TitleColor, ?AxisX2ToolTip=ToolTip) 

            /// <summary>Apply styling to the second Y axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Min">The minimum value for the axis</param>
            /// <param name="Log">The axis scale is logarithmic</param>
            /// <param name="ArrowStyle">The arrow style for the axis</param>
            /// <param name="LabelStyle">The label style for the axis</param>
            /// <param name="IsMarginVisible">Determines whether to add a margin to the axis</param>
            /// <param name="MajorGrid">The major grid points to use for the axis</param>
            /// <param name="MinorGrid">The minor grid points to use for the axis</param>
            /// <param name="MajorTickMark">The major tick marks to use for the axis</param>
            /// <param name="MinorTickMark">The minor tick marks to use for the axis</param>
            /// <param name="TitleAlignment">The alignment of the title for the axis</param>
            /// <param name="TitleFontName">The font name for the title of the axis</param>
            /// <param name="TitleFontSize">The font size for the title of the axis</param>
            /// <param name="TitleColor">The color of the title of the axis</param>
            /// <param name="Tooltip">The tooltip to use for the axis</param>
            static member WithYAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark,
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisY2Logarithmic=Log,?AxisY2Enabled=Enabled, ?AxisY2ArrowStyle=ArrowStyle, ?AxisY2LabelStyle=LabelStyle, ?AxisY2IsMarginVisible=IsMarginVisible, ?AxisY2Maximum=Max, ?AxisY2Minimum=Min, ?AxisY2MajorGrid=MajorGrid, ?AxisY2MinorGrid=MinorGrid, ?AxisY2MajorTickMark=MajorTickMark, ?AxisY2MinorTickMark=MinorTickMark, ?AxisY2Title=Title, ?AxisY2TitleAlignment=TitleAlignment, ?AxisY2TitleFont=titleFont, ?AxisY2TitleForeColor=TitleColor, ?AxisY2ToolTip=ToolTip) 

            /// <summary>Apply content and styling to the title, if present</summary>
            /// <param name="InsideArea">If false, locates the title outside the chart area</param>
            /// <param name="Text">The text of the title</param>
            /// <param name="Style">The text style for the title</param>
            /// <param name="FontName">The font name for the title</param>
            /// <param name="FontSize">The font size for the title</param>
            /// <param name="FontStyle">The font style for the title</param>
            /// <param name="Background">The background for the title</param>
            /// <param name="Color">The color for the title</param>
            /// <param name="BorderColor">The border color for the title</param>
            /// <param name="BorderWidth">The border width for the title</param>
            /// <param name="BorderDashStyle">The border dash style for the title</param>
            /// <param name="Orientation">The orientation for the title</param>
            /// <param name="Alignment">The alignment for the title</param>
            /// <param name="Docking">The docking location for the title</param>
            static member WithTitle
                (?Text, ?InsideArea, ?Style, ?FontName, ?FontSize, ?FontStyle, ?Background, ?Color, ?BorderColor, ?BorderWidth, ?BorderDashStyle, 
                 ?Orientation, ?Alignment, ?Docking) = 
                let font = StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?Title=Text, ?TitleStyle=Style, ?TitleFont=font, ?TitleBackground=Background, ?TitleColor=Color, ?TitleBorderColor=BorderColor, ?TitleBorderWidth=BorderWidth, ?TitleBorderDashStyle=BorderDashStyle, ?TitleOrientation=Orientation, ?TitleAlignment=Alignment, ?TitleDocking=Docking, ?TitleInsideArea=InsideArea)

            /// <summary>Enables 3D styling for the chart</summary>
            /// <param name="ShowMarkerLines">Specifies whether marker lines are displayed when rendered in 3D.</param>
            static member With3D
                (?Inclination, ?IsClustered, ?IsRightAngleAxes, ?LightStyle, ?Perspective, ?PointDepth, ?PointGapDepth, ?ShowMarkerLines, ?Rotation, ?WallWidth) =
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(Enable3D=true, ?Area3DInclination=Inclination, ?Area3DIsClustered=IsClustered, ?Area3DIsRightAngleAxes=IsRightAngleAxes, ?Area3DLightStyle=LightStyle, ?Area3DPerspective=Perspective, ?Area3DPointDepth=PointDepth, ?Area3DPointGapDepth=PointGapDepth, ?Area3DRotation=Rotation, ?ShowMarkerLines=ShowMarkerLines, ?Area3DWallWidth=WallWidth)

            /// <summary>Add styling to the markers and points of the chart</summary>
            /// <param name="LabelPosition">The relative data point width. Any double from 0 to 2.</param>
            /// <param name="PointStyle">The drawing style of data points.</param>
            /// <param name="MaxPixelPointWidth">The maximum data point width in pixels. Any integer &gt; 0.</param>
            /// <param name="MinPixelPointWidth">The minimum data point width in pixels. Any integer &gt; 0.</param>
            /// <param name="PixelPointWidth">The data point width in pixels. Any integer &gt; 2.</param>
            static member WithMarkers
                (?Color, ?Size, ?Step, ?Style, ?BorderColor, ?BorderWidth, ?PointWidth, ?PixelPointWidth, ?PointStyle, ?MaxPixelPointWidth, ?MinPixelPointWidth) =
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?MarkerColor=Color, ?MarkerSize=Size, ?MarkerStep=Step, ?MarkerStyle=Style, ?MarkerBorderColor=BorderColor, ?MarkerBorderWidth=BorderWidth,
                                          ?PointWidth=PointWidth,?PointStyle=PointStyle,?MaxPixelPointWidth=MaxPixelPointWidth, ?MinPixelPointWidth=MinPixelPointWidth, ?PixelPointWidth=PixelPointWidth)

            /// <summary>Apply styling to the legend of the chart</summary>
            /// <param name="InsideArea">If false, places the legend outside the chart area</param>
            static member WithLegend
              (?Enabled,?Title, ?Background, ?FontName,  ?FontSize, ?FontStyle, ?Alignment, ?Docking, ?InsideArea, 
               ?TitleAlignment, ?TitleFont, ?TitleColor, ?BorderColor, ?BorderWidth, ?BorderDashStyle) = 
              let font = StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) 
              // Specifying AndLegend enables the legend by default
              let legendEnabled = defaultArg Enabled true
              fun (ch : #GenericChart) ->
              ch |> Helpers.ApplyStyles(LegendEnabled=legendEnabled,?LegendTitle=Title, ?LegendBackground=Background, ?LegendFont=font, ?LegendAlignment=Alignment, ?LegendDocking=Docking, ?LegendIsDockedInsideArea=InsideArea,
                                        ?LegendTitleAlignment=TitleAlignment, ?LegendTitleFont=TitleFont, ?LegendTitleForeColor=TitleColor, ?LegendBorderColor=BorderColor, ?LegendBorderWidth=BorderWidth, ?LegendBorderDashStyle=BorderDashStyle)

            /// <summary>Add data point labels and apply styling to the labels</summary>
            /// <param name="LabelPosition">The relative data point width. Any double from 0 to 2.</param>
            /// <param name="BarLabelPosition">For Bar charts, specifies the placement of the data point label</param>
            static member WithDataPointLabels
              (?Label, ?LabelPosition, ?LabelToolTip, ?PointToolTip, ?BarLabelPosition) = 
              fun (ch : #GenericChart) ->
              ch |> Helpers.ApplyStyles(?DataPointLabel=Label,?DataPointLabelToolTip=LabelToolTip, ?DataPointToolTip=PointToolTip, ?LabelPosition=LabelPosition,?BarLabelPosition=BarLabelPosition)
           
            /// <summary>Apply additional styling to the chart</summary>
            /// <param name="Name">The name of the data series</param>
            /// <param name="Color">The foreground color for the data series</param>
            /// <param name="AreaBackground"></param>
            /// <param name="Margin"></param>
            /// <param name="Background"></param>
            /// <param name="BorderColor">The border color for the data series</param>
            /// <param name="BorderWidth">The border width for the data series</param>
            // TODO: move SplineLineTension to the specific chart types it applies to
            /// <param name="SplineLineTension">The line tension for the drawing of curves between data points. Any double from 0 to 2.</param>
            static member WithStyling
              (?Name,?Color, ?AreaBackground,?Margin,?Background,?BorderColor, ?BorderWidth, ?SplineLineTension(* , ?AlignWithChartArea, ?AlignmentOrientation, ?AlignmentStyle *) ) =
              fun (ch : #GenericChart) ->
              ch |> Helpers.ApplyStyles(?Name=Name,?Color=Color, ?AreaBackground=AreaBackground,?Margin=Margin,?Background=Background,?BorderColor=BorderColor, ?BorderWidth=BorderWidth, ?SplineLineTension=SplineLineTension(* , ?AlignWithChartArea=AlignWithChartArea , ?AlignmentOrientation=AlignmentOrientation, ?AlignmentStyle=AlignmentStyle *) )

            static member WithArea = AreaProperties()
            static member WithSeries = SeriesProperties()

(*
            [<Obsolete("This member is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
            static member WithLegend
                ( ?Title, ?Background, ?Font, ?Alignment, ?Docking, ?InsideArea,
                  ?TitleAlignment, ?TitleFont, ?TitleForeColor, ?BorderColor, ?BorderWidth, ?BorderDashStyle) (ch:GenericChart) =
                let legend = new Legend()
                InsideArea |> Option.iter legend.set_IsDockedInsideChartArea
                Background |> Option.iter (applyBackground legend)
                Font |> Option.iter legend.set_Font
                Alignment |> Option.iter legend.set_Alignment
                Docking |> Option.iter legend.set_Docking
                Title |> Option.iter legend.set_Title
                TitleAlignment |> Option.iter legend.set_TitleAlignment
                TitleFont |> Option.iter legend.set_TitleFont
                TitleForeColor |> Option.iter legend.set_TitleForeColor
                BorderColor |> Option.iter legend.set_BorderColor
                BorderDashStyle |> Option.iter legend.set_BorderDashStyle
                BorderWidth |> Option.iter legend.set_BorderWidth
                ch.SetLegend legend
                ch
*)
      
            static member WithMargin<'T when 'T :> GenericChart> (left, top, right, bottom) =
              fun (ch:'T) ->
                ch.Margin <- (left, top, right, bottom)
                ch

            static member WithStyle<'T when 'T :> GenericChart> (?Background) =
              fun (ch:'T) -> 
                Background |> Option.iter (applyBackground ch.Chart)
                ch

(*
            [<Obsolete("This member is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
            static member WithTitle<'T when 'T :> GenericChart>
                ( ?Text, ?TextStyle, ?Font, ?Background, ?Color, ?BorderColor, ?BorderWidth, ?BorderDashStyle, 
                  ?TextOrientation, ?Alignment, ?Docking, ?InsideArea) =
              fun (ch:'T) ->
                  let title = new Title()
                  Text |> Option.iter title.set_Text 
                  Color |> Option.iter title.set_ForeColor
                  BorderColor |> Option.iter title.set_BorderColor
                  BorderDashStyle |> Option.iter title.set_BorderDashStyle
                  BorderWidth |> Option.iter title.set_BorderWidth
                  TextStyle |> Option.iter title.set_TextStyle
                  TextOrientation |> Option.iter title.set_TextOrientation
                  InsideArea |> Option.iter title.set_IsDockedInsideChartArea
                  Background |> Option.iter (applyBackground title)
                  Font |> Option.iter title.set_Font
                  Alignment |> Option.iter title.set_Alignment
                  Docking |> Option.iter title.set_Docking
                  ch.SetTitle title
                  ch
*)

        let internal FontCreate(fName:string option, fFamily:FontFamily option, fStyle:FontStyle option, fSize:float32 option) =
                let fontSize = 
                    match fSize with
                    | Some(size) -> size
                    | None -> DefaultFontForOthers.Size
                let fontStyle = 
                    match fStyle with
                    | Some(style) -> style
                    | None -> DefaultFontForOthers.Style
                let font =
                    match (fFamily, fName) with
                    | (Some(family), _) -> new Font(family, fontSize, fontStyle)
                    | (_, Some(name)) -> new Font(name, fontSize, fontStyle)
                    | (None, None) -> new Font(DefaultFontForOthers.FontFamily, fontSize, fontStyle)
                font

        [<Obsolete("This type is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
        type StyleHelper =
            [<Obsolete("This member is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
            static member LabelStyle
                ( ?Angle, ?Color, ?Format, ?Interval, ?IntervalOffset, ?IntervalOffsetType, ?IntervalType, ?IsEndLabelVisible, ?IsStaggered, ?TruncatedLabels,
                  ?FontName:string, ?FontFamily:FontFamily, ?FontStyle:FontStyle, ?FontSize:float32) =
                  let labelStyle = new Charting.LabelStyle()
                  Angle |> Option.iter labelStyle.set_Angle
                  Color |> Option.iter labelStyle.set_ForeColor
                  Format |> Option.iter labelStyle.set_Format
                  Interval |> Option.iter labelStyle.set_Interval
                  IntervalOffset |> Option.iter labelStyle.set_IntervalOffset
                  IntervalOffsetType |> Option.iter labelStyle.set_IntervalOffsetType
                  IntervalType |> Option.iter labelStyle.set_IntervalType
                  IsStaggered |> Option.iter labelStyle.set_IsStaggered
                  TruncatedLabels |> Option.iter labelStyle.set_TruncatedLabels
                  IsEndLabelVisible |> Option.iter labelStyle.set_IsEndLabelVisible
                  if FontName.IsSome || FontFamily.IsSome || FontStyle.IsSome || FontSize.IsSome then
                     labelStyle.set_Font (FontCreate(FontName, FontFamily, FontStyle, FontSize))
                  labelStyle

            [<Obsolete("This member is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
            static member Legend
                ( ?Title, ?Background, ?Alignment, ?Docking, ?InsideArea,
                  ?BorderColor, ?BorderWidth, ?BorderDashStyle, 
                  ?FontName:string, ?FontFamily:FontFamily, ?FontStyle:FontStyle, ?FontSize:float32) =
                  let legend = new Legend()
                  InsideArea |> Option.iter legend.set_IsDockedInsideChartArea
                  Background |> Option.iter (applyBackground legend)
                  Alignment |> Option.iter legend.set_Alignment
                  Docking |> Option.iter legend.set_Docking
                  Title |> Option.iter legend.set_Title
                  BorderColor |> Option.iter legend.set_BorderColor
                  BorderDashStyle |> Option.iter legend.set_BorderDashStyle
                  BorderWidth |> Option.iter legend.set_BorderWidth
                  if FontName.IsSome || FontFamily.IsSome || FontStyle.IsSome || FontSize.IsSome then
                      legend.set_Font (FontCreate(FontName, FontFamily, FontStyle, FontSize))
                  legend

            [<Obsolete("This member is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
            static member Title
                ( ?Text, ?TextStyle, ?Background, ?Color, ?BorderColor, ?BorderWidth, ?BorderDashStyle, 
                  ?TextOrientation, ?Alignment, ?Docking, ?InsideArea,
                  ?FontName:string, ?FontFamily:FontFamily, ?FontStyle:FontStyle, ?FontSize:float32) =
                  let title = new Title()
                  Text |> Option.iter title.set_Text 
                  Color |> Option.iter title.set_ForeColor
                  BorderColor |> Option.iter title.set_BorderColor
                  BorderDashStyle |> Option.iter title.set_BorderDashStyle
                  BorderWidth |> Option.iter title.set_BorderWidth
                  TextStyle |> Option.iter title.set_TextStyle
                  TextOrientation |> Option.iter title.set_TextOrientation
                  InsideArea |> Option.iter title.set_IsDockedInsideChartArea
                  Background |> Option.iter (applyBackground title)
                  Alignment |> Option.iter title.set_Alignment
                  Docking |> Option.iter title.set_Docking
                  if FontName.IsSome || FontFamily.IsSome || FontStyle.IsSome || FontSize.IsSome then
                      title.set_Font (FontCreate(FontName, FontFamily, FontStyle, FontSize))
                  title

            [<Obsolete("This type is now obsolete. Use the '.WithXYZ(...)' fluent methods or the 'Chart.WithXYZ(...)' pipeline methods instead.")>]
            static member Font(?FamilyName:string, ?FontFamily:FontFamily, ?FontStyle:FontStyle, ?FontSize:float32) =
                FontCreate(FamilyName, FontFamily, FontStyle, FontSize)

        [<Obsolete("The type FSharpChart is now obsolete. Use 'Chart' instead. Note, do not open System.Windows.Forms.DataVisualization.Charting when using this library, as it also defines a 'Chart' type.")>]
        let FSharpChart = 1

    [<Obsolete("This type is now obsolete. Use 'Chart' instead. Note, do not open System.Windows.Forms.DataVisualization.Charting when using this library, as it also defines a 'Chart' type.")>]
    type FSharpChart = Chart

