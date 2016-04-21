
namespace FSharp.Charting

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Collections.Specialized
    open System.Collections.ObjectModel
    open System.Reflection
    open System.Runtime.InteropServices
    open OxyPlot
    open OxyPlot.Series

    type internal INotifyEnumerableInternal<'T> =
        inherit IEnumerable<'T>
        inherit INotifyCollectionChanged

    module internal Seq = 
        /// Evaluate once only. Unlike Seq.cache this evaluates once to an array all at once.
        let once (source: seq<'T>) = 
            let data = lazy Seq.toArray source
            { new IEnumerable<'T> with
                  member x.GetEnumerator() = (data.Force() :> seq<'T>).GetEnumerator() 
              interface IEnumerable with
                  member x.GetEnumerator() = (data.Force() :> IEnumerable).GetEnumerator() }

    module internal NotifySeq = 
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
        let notifyOrOnce (obs:seq<'T>) =       
            match box obs with 
            | :? INotifyEnumerableInternal<'T>  as n -> n
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

        // TODO: only start on connect + proper replay + disconnect, OR use weak connection to source
        let ofObservableIncremental (source:IObservable<'T>) : INotifyEnumerableInternal<'T> = 
            let obs = new ObservableCollection<'T>()
            source |> Observable.add (fun x -> obs.Add(x))
            ofObservableCollection obs

        // TODO: only start on connect + proper replay + disconnect, OR use weak connection to source
        let ofObservableReplacing (source:IObservable<#seq<'T>>) : INotifyEnumerableInternal<'T> = 
            let coll, update = replacing ()
            source |> Observable.add (fun elems -> update (Seq.toArray elems))
            coll

    /// A primitive value for a point on a chart. An abbreviation for the IConvertible type.
    type value = System.IConvertible
    type key = System.IComparable

    [<RequireQualifiedAccess>]
    type internal ChartValueType = Date | Time | DateTime | Auto | Int32 | UInt32 | Double | String | Int64 | UInt64 | Single

    module internal KeyConversions = 
        // Mapping from known runtime types to 'ChartValueType' that is used for the X axis
        // (we try to infer the axis type so that it is displayed nicely)
        let knownTypes = 
          dict [ typeof<int>, ChartValueType.Int32; typeof<int64>, ChartValueType.Int64
                 typeof<uint32>, ChartValueType.UInt32; typeof<uint64>, ChartValueType.UInt64
                 typeof<float>, ChartValueType.Double; typeof<float32>, ChartValueType.Single
                 typeof<string>, ChartValueType.String ]

        let internal registeredConvertors = System.Collections.Generic.Dictionary<_, key -> key>()
        let tryGetConvertor (typ:System.Type) =
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

        /// An implementation type for items on a chart. This type should not be used directly.
        type public LineChartItem(X: key, Y: value) = 
            member __.X = X 
            member __.Y = Y 

        /// An implementation type for items on a chart. This type should not be used directly.
        type public AreaChartItem(X: key, Y: value) = 
            member __.X = X 
            member __.Y = Y 

        /// An implementation type for items on a chart. This type should not be used directly.
        type public PieChartItem(Label: string, Value: value) = 
            member __.Label = Label
            member __.Value = Value

        /// An implementation type for items on a chart. This type should not be used directly.
        type public ScatterChartItem(X: value, Y: value, Size: value, Tag: obj) = 
            member __.X = X
            member __.Y = Y
            member __.Size = Size
            member __.Tag = Tag

#if INCOMPLETE_API
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

#endif
        // ----------------------------------------------------------------------------------
        // Utilities for converting data

        // Converts Y value of a chart (defines the type too)
        let culture = System.Globalization.CultureInfo.InvariantCulture

        let keyToString (x:key) = match box x with | :? IConvertible as a -> a.ToString() | _ -> Convert.ToString(x)
        let keyToDouble (x:key) = match box x with | :? IConvertible as a -> a.ToDouble(culture) | _ -> Convert.ToDouble(x)
        let valueToDouble (x:value) = x.ToDouble(culture)

        open System.Collections.Specialized
        
(*
        let internal convertKeys (selector:_ -> key) (transform:(key -> key) -> _) data = 
          let hasConvertor = ref None
          let convertor = ref None
          data |> NotifySeq.map (fun v ->
            if hasConvertor.Value.IsNone then
              convertor := KeyConversions.tryGetConvertor ((selector v).GetType())
              hasConvertor := Some(convertor.Value.IsSome)
            match convertor.Value with
            | Some conv -> transform conv v
            | _ -> v)

        let internal convertKeys1of2 data = 
          data |> NotifySeq.map (fun (k, v) -> k :> key, v)          
               |> convertKeys fst (fun kf (k, v) -> kf k, v) 
        let internal convertKeys1of3 data = 
          data |> NotifySeq.map (fun (k, v1, v2) -> k :> key, v1, v2)          
               |> convertKeys (fun (k, _, _) -> k) (fun kf (k, v1, v2) -> kf k, v1, v2) 
        let internal convertKeys1of4 data = 
          data |> NotifySeq.map (fun (k, v1, v2, v3) -> k :> key, v1, v2, v3)          
               |> convertKeys (fun (k, _, _, _) -> k) (fun kf (k, v1, v2, v3) -> kf k, v1, v2, v3) 
        let internal convertKeys1of5 data = 
          data |> NotifySeq.map (fun (k, v1, v2, v3, v4) -> k :> key, v1, v2, v3, v4)          
               |> convertKeys (fun (k, _, _, _, _) -> k) (fun kf (k, v1, v2, v3, v4) -> kf k, v1, v2, v3, v4) 
        let internal convertKeys1of7 data = 
          data |> NotifySeq.map (fun (k, v1, v2, v3, v4, v5, v6) -> k :> key, v1, v2, v3, v4, v5, v6)          
               |> convertKeys (fun (k, _, _, _, _, _, _) -> k) (fun kf (k, v1, v2, v3, v4, v5, v6) -> kf k, v1, v2, v3, v4, v5, v6) 

        // In most cases, we can use static type - if it is primitive type and it is not
        // mapped to something else
        let internal getChartValueTypeStatic<'K when 'K :> key>(witness:seq<'K>) =
          ignore(witness)
          match KeyConversions.knownTypes.TryGetValue(typeof<'K>) with
          | true, typ -> Some typ
          | _ -> None

        let internal getChartValueTypeDynamic xTypeOpt values = 
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
*)

        // ----------------------------------------------------------------------------------
        // Data operations
          
        let internal binData data lowerBound upperBound intervals =
            seq{lowerBound .. (upperBound - lowerBound)/intervals .. upperBound }
            |> Seq.pairwise
            |> Seq.map (fun (l,u) -> 
                                    let cnt = data |> Seq.filter (fun e -> e >= l && e < u) |> Seq.length
                                    {LowerBound = l; UpperBound = u; Count = cnt}
                        )

        let internal allowNull x = match x with None -> null | Some v -> v

        let internal listen data = NotifySeq.notifyOrOnce data

            //let xTypeOpt = getChartValueTypeStatic (Seq.map fst data)
            //let data = convertKeys1of2 (NotifySeq.notifyOrOnce data)
            //let xType = getChartValueTypeDynamic xTypeOpt (Seq.map fst data)
        let internal mergeLabels labels data = 
            match labels with 
            | None -> data |> NotifySeq.map (fun p -> (p,None))
            | Some labels ->  NotifySeq.zip data (listen labels) |> NotifySeq.map (fun (p,l) -> (p,Some l))


        let internal makeItems dataPointF data =  data |> NotifySeq.map dataPointF

        let internal indexData data = 
            data |> NotifySeq.notifyOrOnce |> NotifySeq.mapi (fun i y -> (i, y)) :> seq<_>

        let internal indexData2 data = 
            data |> NotifySeq.notifyOrOnce |> NotifySeq.mapi (fun i (y1,y2) -> (i, y1, y2)) :> seq<_>


        type GenericChart internal (model : PlotModel) =  
            member internal this.Model = model
            member internal this.Name = model.Title
            member internal this.ChartTypeName = model.Title

            static member internal Create (data:seq<_>, series : ItemsSeries) = 
                let model = new PlotModel()
                model.Series.Add(series)
                series.ItemsSource <- data
                let chart = GenericChart(model)
                match data with 
                | :? INotifyCollectionChanged as i -> 
                      let rec handler = NotifyCollectionChangedEventHandler(fun _ _ -> 
                        series.ItemsSource <- data
                        match model.PlotView with 
                        | null -> ()
                        | _ ->  
                           // An exception will indicate form is no longer working, e.g. shutdown or disposed, so disconnect
                           try model.InvalidatePlot(true) 
                           with _ -> i.CollectionChanged.RemoveHandler handler)
                      i.CollectionChanged.AddHandler handler
                | _ -> ()
                chart


#if INCOMPLETE_API
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
`
            /// Save the chart as an image in the specified image format
            member public x.SaveChartAs(filename : string, format : ChartImageFormat) =
                x.Chart.SaveImage(filename, format  |> int |> enum)
#endif
            

    open ChartTypes

    type internal Helpers() = 

        /// Use a DateTime axis if the input key data is DateTime
        static member ApplyStaticAxis(xty, pos, ?gap) = (fun (ch:('T :> GenericChart)) -> 
            let model = ch.Model
            match model.DefaultXAxis with 
            | null -> 
                if xty = typeof<System.DateTime> then 
                    model.Axes.Add (Axes.DateTimeAxis(Position=pos))
                if xty = typeof<System.TimeSpan> then 
                    model.Axes.Add (Axes.TimeSpanAxis(Position=pos))
                if xty = typeof<string> then 
                    match gap with
                    | Some g -> 
                        let a = Axes.CategoryAxis(Position=pos)
                        a.GapWidth <- g
                        model.Axes.Add (a)
                    | _ -> model.Axes.Add (Axes.CategoryAxis(Position=pos))
            | _ -> ()
            ch)

        static member ApplyStyles(?Color, ?Name, ?Title, ?AxisXTitle, ?AxisYTitle) = (fun (ch:('T :> GenericChart)) -> 
          
            let model = ch.Model
            let seriesIter f = for s in model.Series do f s
            let ensureDefaultAxis (X) =
                match model.Axes |> Seq.tryFind (fun x -> (if X then x.IsHorizontal() else x.IsVertical()) && x.IsXyAxis()) with 
                | None -> 
                    let axis = Axes.LinearAxis(if X then Axes.AxisPosition.Bottom else Axes.AxisPosition.Left)
                    model.Axes.Add axis
                    axis :> Axes.Axis
                | Some a -> a
            let ensureDefaultXAxis () = ensureDefaultAxis true
            let ensureDefaultYAxis () = ensureDefaultAxis false
            Color |> Option.iter (fun c -> seriesIter (function 
                 | :? AreaSeries as s -> s.Fill <- c 
                 | :? LineSeries as s -> s.Color <- c 
                 // TODO: ScatterSeries color | :? ScatterSeries as s -> s. <- c 
                 | _ -> ()))
            Name |> Option.iter (fun t -> for s in model.Series do s.Title <- t)
            Title |> Option.iter (fun t -> model.Title <- t)
            AxisXTitle |> Option.iter (fun t -> ensureDefaultXAxis().Title <- t)
            AxisYTitle |> Option.iter (fun t -> ensureDefaultYAxis().Title <- t)
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
        static member Area(data:seq<('key :> key) * #value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
           GenericChart.Create(data |> listen |> mergeLabels Labels |> makeItems (fun ((k,v),_labelOpt) -> AreaChartItem(k, v)), AreaSeries(DataFieldX="X",DataFieldY="Y",Fill=AreaSeries().ActualColor))
             |> Helpers.ApplyStaticAxis(typeof<'key>, Axes.AxisPosition.Bottom)
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Emphasizes the degree of change over time and shows the relationship of the parts to a whole.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Area(data:seq<#value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
           Chart.Area(indexData data,?Name=Name,?Title=Title,?Labels=Labels, ?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Illustrates comparisons among individual items</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Bar(data:seq<('key :> key)*#value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
           GenericChart.Create(data |> listen |> mergeLabels Labels |> makeItems (fun ((_k,v),_labelOpt) -> BarItem(valueToDouble v)), BarSeries(ValueField="Value"))
             |> Helpers.ApplyStaticAxis(typeof<'key>, Axes.AxisPosition.Left)
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Illustrates comparisons among individual items</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Bar(data:seq<#value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
           Chart.Bar(indexData data,?Name=Name,?Title=Title,?Labels=Labels, ?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


#if INCOMPLETE_API

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

#endif
        /// <summary>A variation of the Point chart type, where the data points are replaced by bubbles of different sizes.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>

(*
        /// <param name="BubbleMaxSize">The maximum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleMinSize">The minimum size of the bubble radius as a percentage of the chart area size. Any integer from 0 to 100.</param>
        /// <param name="BubbleScaleMax">The maximum bubble size, which is a percentage of the chart area that is set by BubbleMaxSize. Any double.</param>
        /// <param name="BubbleScaleMin">The minimum bubble size, which is a percentage of the chart area that is set by BubbleMinSize. Any double.</param>
        /// <param name="UseSizeForLabel">Use the bubble size as the data point label.</param>
*)
        static member Bubble(data:seq<#value * #value * #value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle, ?MarkerSize) = 
           let data = data |> listen 
           let maxSize = lazy (data |> Seq.maxBy (fun (_x,_y,sz) -> valueToDouble sz) |> (fun (_x,_y,sz) -> valueToDouble sz))
           let data = data |> mergeLabels Labels 
           let data = data |> makeItems (fun ((x,y,sz),lab) -> ScatterChartItem(x,y,Size=(max (valueToDouble sz / maxSize.Value * 20.0) 0.1),Tag=allowNull lab))
           GenericChart.Create(data, ScatterSeries(DataFieldX="X",DataFieldY="Y",DataFieldSize="Size",DataFieldTag="Tag",MarkerType=MarkerType.Circle, MarkerStroke= defaultArg Color (ScatterSeries().MarkerStroke), MarkerSize= defaultArg MarkerSize (ScatterSeries().MarkerSize) ))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle )
            //Chart.ConfigureBubble(c,BubbleMaxSize,BubbleMinSize,BubbleScaleMax,BubbleScaleMin,UseSizeForLabel)
            //c

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
        static member Bubble(data:seq<#value * #value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle, ?MarkerSize) = 
           Chart.Bubble(indexData2 data,?Name=Name,?Title=Title,?Labels=Labels,?Color=Color,?XTitle=XTitle,?YTitle=YTitle, ?MarkerSize=MarkerSize)

#if INCOMPLETE_API
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
#endif

        /// <summary>Uses a sequence of columns to compare values across categories.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="ColumnWidth">The width of columns versus whitespace as a percentage.</param>
        static member Column(data:seq<('key :> key)*#value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?ColumnWidth:float) = 
           let gap = match ColumnWidth with 
                     | Some columnWidth -> Some (1.0 - columnWidth)
                     | _ -> None
           GenericChart.Create(data |> listen |> mergeLabels Labels |> makeItems (fun ((_k,v),_labelOpt) -> ColumnItem(valueToDouble v)), ColumnSeries(ValueField="Value"))
             |> Helpers.ApplyStaticAxis(typeof<'key>, Axes.AxisPosition.Bottom, ?gap = gap)
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Uses a sequence of columns to compare values across categories.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        /// <param name="ColumnWidth">The width of columns versus whitespace as a percentage.</param>
        static member Column(data:seq<#value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle, ?ColumnWidth) = 
           Chart.Column(indexData data,?Name=Name,?Title=Title,?Labels=Labels, ?Color=Color,?XTitle=XTitle,?YTitle=YTitle, ?ColumnWidth=ColumnWidth)


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
            Chart.Column(data'',?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle, ?ColumnWidth=Some 0.95)

#if INCOMPLETE_API
        /// <summary>Similar to the Pie chart type, except that it has a hole in the center.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Doughnut(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> DoughnutChart ())
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> GenericChart(SeriesChartType.FastLine))
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> GenericChart(SeriesChartType.FastPoint))
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> FunnelChart () )
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

        /// <summary>Displays a series of connecting vertical lines where the thickness and direction of the lines are dependent on the action of the price value.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Kagi(data,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle) = 
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> KagiChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Displays a series of connecting vertical lines where the thickness and direction of the lines are dependent on the action of the price value.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Kagi(data,?Name,?Title,?Labels,?Color,?XTitle,?YTitle) = 
            GenericChart.Create(mergeDataAndLabelsForY data Labels, fun () -> KagiChart () )
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
#endif

        /// <summary>Illustrates trends in data with the passing of time.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Line(data:seq<('key :> key) * #value>,?Name,?Title,?Color,?XTitle,?YTitle) = 
           GenericChart.Create(data |> listen |> makeItems (fun (x,y) -> LineChartItem(x,y)), LineSeries(DataFieldX="X",DataFieldY="Y"))
             |> Helpers.ApplyStaticAxis(typeof<'key>, Axes.AxisPosition.Bottom)
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Illustrates trends in data with the passing of time.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Line(data:seq<#value>,?Name,?Title,?Color,?XTitle,?YTitle) = 
           Chart.Line(indexData data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)


        /// <summary>Shows how proportions of data, shown as pie-shaped pieces, contribute to the data as a whole.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pie(data,?Name,?Title,?Color,?XTitle,?YTitle) = 
           GenericChart.Create(data |> listen |> makeItems (fun (x,y) -> PieChartItem(keyToString x,y)), PieSeries(LabelField="Label",ValueField="Value"))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Shows how proportions of data, shown as pie-shaped pieces, contribute to the data as a whole.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Pie(data:seq<#value>,?Name,?Title,?Color,?XTitle,?YTitle) = 
           Chart.Pie(indexData data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle)

        /// <summary>Uses points to represent data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Point(data:seq<#value*#value>,?Name,?Title,?Labels, ?Color,?XTitle,?YTitle,?MarkerSize) = 
           GenericChart.Create(data |> listen |> mergeLabels Labels |> makeItems (fun ((x,y),lab) -> ScatterChartItem(x,y,Size=3.0,Tag=allowNull lab)), ScatterSeries(DataFieldX="X",DataFieldY="Y",DataFieldSize="Size",DataFieldTag="Tag",MarkerType=MarkerType.Circle, MarkerStroke= defaultArg Color (ScatterSeries().MarkerStroke), MarkerSize= defaultArg MarkerSize (ScatterSeries().MarkerSize)))
             |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

        /// <summary>Uses points to represent data points.</summary>
        /// <param name="data">The data for the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
        /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member Point(data:seq<#value>,?Name,?Title,?Labels,?Color,?XTitle,?YTitle,?MarkerSize) = 
           Chart.Point(indexData data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?MarkerSize=MarkerSize)

#if INCOMPLETE_API
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> PolarChart () )
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> PyramidChart () )
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> RadarChart () )
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> RenkoChart ())
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> GenericChart(SeriesChartType.Spline))
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> GenericChart(SeriesChartType.SplineArea) )
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> GenericChart(SeriesChartType.StepLine) )
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> ThreeLineBreakChart () )
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


#endif
    /// Contains static methods to construct charts whose data source is an event or observable which 
    /// updates the entire data set.
    type LiveChart() = 

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

#if INCOMPLETE_API
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


#endif

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
        static member Bubble(data:IObservable<#seq<#key * #value * #value>>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle,?MarkerSize) = 
            Chart.Bubble(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?MarkerSize=MarkerSize)


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
        static member BubbleIncremental(data:IObservable<#key * #value * #value>,?Name,?Title (* ,?Labels *) , ?Color,?XTitle,?YTitle,?MarkerSize) = 
            Chart.Bubble(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title,?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?MarkerSize=MarkerSize)

#if INCOMPLETE_API
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
#endif

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
            Chart.Column(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle, ?ColumnWidth=ColumnWidth)

#if INCOMPLETE_API
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


#endif

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
        static member Point(data:IObservable<#seq<#key * #value>>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?MarkerSize) = 
            Chart.Point(NotifySeq.ofObservableReplacing data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?MarkerSize=MarkerSize)


        /// <summary>An incrementally updating chart which uses points to represent data points.</summary>
        /// <param name="data">The data for the chart. Each observation adds a data element to the chart.</param>
        /// <param name="Name">The name of the data set.</param>
        /// <param name="Title">The title of the chart.</param>
    //    /// <param name="Labels">The labels that match the data.</param>
        /// <param name="Color">The color for the data.</param>
        /// <param name="XTitle">The title of the X-axis.</param>
        /// <param name="YTitle">The title of the Y-axis.</param>
        static member PointIncremental(data:IObservable<#key * #value>,?Name,?Title,(* ?Labels, *) ?Color,?XTitle,?YTitle,?MarkerSize) = 
            Chart.Point(NotifySeq.ofObservableIncremental data,?Name=Name,?Title=Title(* ,?Labels=Labels *),?Color=Color,?XTitle=XTitle,?YTitle=YTitle,?MarkerSize=MarkerSize)

#if INCOMPLETE_API
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
            GenericChart.Create(data |> mergeLabels Labels |> makeItems, fun () -> GenericChart(SeriesChartType.StackedColumn) )
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

#endif

    [<AutoOpen>]
    [<Obsolete("Do not open this module, it is internal only")>]
    module _ChartStyleExtensions = 

        let internal createCounter() = let count = ref 0 in (fun () -> incr count; !count)

        let internal dict = new Dictionary<string, unit -> int>()

        let internal ProvideTitle (chart:ChartTypes.GenericChart) = 
                let defaultName = 
                    if String.IsNullOrEmpty chart.Name then chart.ChartTypeName  + " Chart"
                    else chart.Name

                match dict.ContainsKey(defaultName) with
                | true -> sprintf "%s (%i)" defaultName (dict.[defaultName]())
                | false -> dict.Add(defaultName, createCounter()); defaultName


        type ChartTypes.GenericChart with
#if INCOMPLETE_API
            /// <summary>Apply styling to the X Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
            member ch.WithXAxis
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisXLogarithmic=Log,?AxisXEnabled=Enabled, ?AxisXArrowStyle=ArrowStyle, ?AxisXLabelStyle=LabelStyle, (* ?AxisXIsMarginVisible=IsMarginVisible, *) ?AxisXMaximum=Max, ?AxisXMinimum=Min, ?AxisXMajorGrid=MajorGrid, ?AxisXMinorGrid=MinorGrid, ?AxisXMajorTickMark=MajorTickMark, ?AxisXMinorTickMark=MinorTickMark, 
                                          ?AxisXTitle=Title, ?AxisXTitleAlignment=TitleAlignment, ?AxisXTitleFont=titleFont, ?AxisXTitleForeColor=TitleColor, ?AxisXToolTip=ToolTip) 

            /// <summary>Apply styling to the Y Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
            member ch.WithYAxis
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisYLogarithmic=Log,?AxisYEnabled=Enabled,?AxisYArrowStyle=ArrowStyle,  ?AxisYLabelStyle=LabelStyle, (* ?AxisYIsMarginVisible=IsMarginVisible, *) ?AxisYMaximum=Max, ?AxisYMinimum=Min, ?AxisYMajorGrid=MajorGrid, ?AxisYMinorGrid=MinorGrid, ?AxisYMajorTickMark=MajorTickMark, ?AxisYMinorTickMark=MinorTickMark, ?AxisYTitle=Title, ?AxisYTitleAlignment=TitleAlignment, ?AxisYTitleFont=titleFont, ?AxisYTitleForeColor=TitleColor, ?AxisYToolTip=ToolTip) 

            /// <summary>Apply styling to the second X axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
            member ch.WithXAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisX2Logarithmic=Log,?AxisX2Enabled=Enabled, ?AxisX2ArrowStyle=ArrowStyle, ?AxisX2LabelStyle=LabelStyle, (* ?AxisX2IsMarginVisible=IsMarginVisible, *) ?AxisX2Maximum=Max, ?AxisX2Minimum=Min, ?AxisX2MajorGrid=MajorGrid, ?AxisX2MinorGrid=MinorGrid, ?AxisX2MajorTickMark=MajorTickMark, ?AxisX2MinorTickMark=MinorTickMark, ?AxisX2Title=Title, ?AxisX2TitleAlignment=TitleAlignment, ?AxisX2TitleFont=titleFont, ?AxisX2TitleForeColor=TitleColor, ?AxisX2ToolTip=ToolTip) 

            /// <summary>Apply styling to the second Y axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
            member ch.WithYAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark,
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                ch |> Helpers.ApplyStyles(?AxisY2Logarithmic=Log,?AxisY2Enabled=Enabled, ?AxisY2ArrowStyle=ArrowStyle, ?AxisY2LabelStyle=LabelStyle, (* ?AxisY2IsMarginVisible=IsMarginVisible, *)?AxisY2Maximum=Max, ?AxisY2Minimum=Min, ?AxisY2MajorGrid=MajorGrid, ?AxisY2MinorGrid=MinorGrid, ?AxisY2MajorTickMark=MajorTickMark, ?AxisY2MinorTickMark=MinorTickMark, ?AxisY2Title=Title, ?AxisY2TitleAlignment=TitleAlignment, ?AxisY2TitleFont=titleFont, ?AxisY2TitleForeColor=TitleColor, ?AxisY2ToolTip=ToolTip) 

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


#endif
            /// Display the chart in a new Gtk.Window()
            member ch.ShowChart () =
                let plot = new OxyPlot.GtkSharp.PlotView(Model = ch.Model )
                let win = new Gtk.Window(ProvideTitle ch)
                plot.SetSizeRequest(700, 500)
                win.SetSizeRequest(700, 500)
                win.Add(plot)
                plot.Show()
                win.Show()
                win.Present()

#if INCOMPLETE_API
        type Chart with 

            /// <summary>Apply styling to the X Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
            static member WithXAxis
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisXLogarithmic=Log,?AxisXEnabled=Enabled, ?AxisXArrowStyle=ArrowStyle, ?AxisXLabelStyle=LabelStyle, (* ?AxisXIsMarginVisible=IsMarginVisible, *) ?AxisXMaximum=Max, ?AxisXMinimum=Min, ?AxisXMajorGrid=MajorGrid, ?AxisXMinorGrid=MinorGrid, ?AxisXMajorTickMark=MajorTickMark, ?AxisXMinorTickMark=MinorTickMark, 
                                          ?AxisXTitle=Title, ?AxisXTitleAlignment=TitleAlignment, ?AxisXTitleFont=titleFont, ?AxisXTitleForeColor=TitleColor, ?AxisXToolTip=ToolTip) 

            /// <summary>Apply styling to the Y Axis</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisYLogarithmic=Log,?AxisYEnabled=Enabled,?AxisYArrowStyle=ArrowStyle,  ?AxisYLabelStyle=LabelStyle, (* ?AxisYIsMarginVisible=IsMarginVisible, *) ?AxisYMaximum=Max, ?AxisYMinimum=Min, ?AxisYMajorGrid=MajorGrid, ?AxisYMinorGrid=MinorGrid, ?AxisYMajorTickMark=MajorTickMark, ?AxisYMinorTickMark=MinorTickMark, ?AxisYTitle=Title, ?AxisYTitleAlignment=TitleAlignment, ?AxisYTitleFont=titleFont, ?AxisYTitleForeColor=TitleColor, ?AxisYToolTip=ToolTip) 

            /// <summary>Apply styling to the second X axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
            static member WithXAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, 
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisX2Logarithmic=Log,?AxisX2Enabled=Enabled, ?AxisX2ArrowStyle=ArrowStyle, ?AxisX2LabelStyle=LabelStyle, (* ?AxisX2IsMarginVisible=IsMarginVisible, *) ?AxisX2Maximum=Max, ?AxisX2Minimum=Min, ?AxisX2MajorGrid=MajorGrid, ?AxisX2MinorGrid=MinorGrid, ?AxisX2MajorTickMark=MajorTickMark, ?AxisX2MinorTickMark=MinorTickMark, ?AxisX2Title=Title, ?AxisX2TitleAlignment=TitleAlignment, ?AxisX2TitleFont=titleFont, ?AxisX2TitleForeColor=TitleColor, ?AxisX2ToolTip=ToolTip) 

            /// <summary>Apply styling to the second Y axis, if present</summary>
            /// <param name="Enabled">If false, disables the axis</param>
            /// <param name="Title">The title of the axis</param>
            /// <param name="Max">The maximum value for the axis</param>
            /// <param name="Max">The minimum value for the axis</param>
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
            static member WithYAxis2
                (?Enabled, ?Title, ?Max, ?Min, ?Log, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle,(* ?IsMarginVisible, *) ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark,
                 ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

                let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
                fun (ch : #GenericChart) ->
                ch |> Helpers.ApplyStyles(?AxisY2Logarithmic=Log,?AxisY2Enabled=Enabled, ?AxisY2ArrowStyle=ArrowStyle, ?AxisY2LabelStyle=LabelStyle, (* ?AxisY2IsMarginVisible=IsMarginVisible, *)?AxisY2Maximum=Max, ?AxisY2Minimum=Min, ?AxisY2MajorGrid=MajorGrid, ?AxisY2MinorGrid=MinorGrid, ?AxisY2MajorTickMark=MajorTickMark, ?AxisY2MinorTickMark=MinorTickMark, ?AxisY2Title=Title, ?AxisY2TitleAlignment=TitleAlignment, ?AxisY2TitleFont=titleFont, ?AxisY2TitleForeColor=TitleColor, ?AxisY2ToolTip=ToolTip) 

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


#endif
