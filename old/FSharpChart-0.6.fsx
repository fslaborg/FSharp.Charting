// --------------------------------------------------------------------------------------
// Charting API for F# (version 0.60)
// --------------------------------------------------------------------------------------

#nowarn "40"
#r "System.Windows.Forms.DataVisualization.dll"

namespace MSDN.FSharp.Charting

open System
open System.Collections
open System.Collections.Generic
open System.Drawing
open System.Reflection
open System.Runtime.InteropServices
open System.Windows.Forms
open System.Windows.Forms.DataVisualization
open System.Windows.Forms.DataVisualization.Charting

module private ClipboardMetafileHelper =
    [<DllImport("user32.dll")>]
    extern bool OpenClipboard(nativeint hWndNewOwner)
    [<DllImport("user32.dll")>]
    extern bool EmptyClipboard()
    [<DllImport("user32.dll")>]
    extern IntPtr SetClipboardData(uint32 uFormat, nativeint hMem)
    [<DllImport("user32.dll")>]
    extern bool CloseClipboard()
    [<DllImport("gdi32.dll")>]
    extern nativeint CopyEnhMetaFile(nativeint hemfSrc, nativeint hNULL)
    [<DllImport("gdi32.dll")>]
    extern bool DeleteEnhMetaFile(IntPtr hemf)
    
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


module ChartStyles =

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



    type AxisArrowStyle = 
    /// No arrow is used for the relevant axis.
    | None = 0
    /// A triangular arrow is used for the relevant axis.
    | Triangle = 1
    /// A sharp triangular arrow is used for the relevant axis.
    | SharpTriangle = 2
    /// A line-shaped arrow is used for the relevant axis.
    | Lines = 3


    
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

    
    type TextStyle = 
    /// Default text drawing style.
    | Default = 0
    /// Shadow text.
    | Shadow = 1
    /// Embossed text.
    | Emboss = 2
    /// Embedded text.
    | Embed = 3
    /// Framed text.
    | Frame = 4


    type LightStyle = 
    /// No lighting is applied.
    | None = 0
    /// A simplistic lighting style is applied, where the hue of all chart area elements is fixed.
    | Simplistic = 1
    /// A realistic lighting style is applied, where the hue of all chart area elements changes depending on the amount of rotation.
    | Realistic = 2

    
    type Docking = 
    /// Docked to the top of either the chart image or a ChartArea object.
    | Top = 0
    /// Docked to the right of either the chart image or a ChartArea object.
    | Right = 1
    /// Docked to the bottom of either the chart image or a ChartArea object.
    | Bottom = 2
    /// Docked to the left of either the chart image or a ChartArea object.
    | Left = 3

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


      
    /// Specifies the plot area shape in Radar and Polar charts.
    type AreaDrawingStyle = 
        | Circle = 0
        | Polygon = 1

    /// Specifies the placement of the data point label.
    type BarLabelStyle = 
        | Outside = 0
        | Left = 1
        | Right = 2
        | Center = 3

    /// Specifies the text orientation of the axis labels in Radar
    /// and Polar charts.
    type CircularLabelStyle = 
        | Circular = 0
        | Horizontal = 1
        | Radial = 2

    /// Specifies the drawing style of data points.
    type DrawingStyle = 
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

    /// Specifies the value to be used for empty points.
    type EmptyPointValue = 
        | Average = 0
        | Zero = 1

    /// Specifies the appearance of the marker at the center value
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

    /// Specifies the visibility of the upper and lower error values.
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

    /// Specifies the 3D drawing style of the Funnel chart type.
    type Funnel3DDrawingStyle = 
        | CircularBase = 0
        | SquareBase = 1

    /// Specifies the data point label placement of the Funnel chart
    /// type when the FunnelLabelStyle is set to Inside.
    type FunnelInsideLabelAlignment = 
        | Center = 0
        | Top = 1
        | Bottom = 2

    /// Specifies the data point label style of the Funnel chart type.
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

    /// Specifies the style of the Funnel chart type.
    type FunnelStyle = 
        | YIsWidth = 0
        | YIsHeight = 1

    /// Specifies the label position of the data point.
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

    /// Specifies the Y value to use as the data point
    /// label.
    type LabelValueType = 
        | High = 0
        | Low = 1
        | Open = 2
        | Close = 3

    /// Specifies the marker style for open and close values.
    type OpenCloseStyle = 
        | Triangle = 0
        | Line = 1
        | Candlestick = 2

    /// Specifies the drawing style of the data points.
    type PieDrawingStyle = 
        | Default = 0
        | SoftEdge = 1
        | Concave = 2

    /// Specifies the label style of the data points.
    type PieLabelStyle = 
        | Disabled = 0
        | Inside = 1
        | Outside = 2

    /// Specifies the drawing style of the Polar chart type.
    type PolarDrawingStyle = 
        | Line = 0
        | Marker = 1

    /// Specifies the 3D drawing style of the Pyramid chart type.
    type Pyramid3DDrawingStyle = 
        | CircularBase = 0
        | SquareBase = 1

    /// Specifies the placement of the data point labels in the
    /// Pyramid chart when they are placed inside the pyramid.
    type PyramidInsideLabelAlignment = 
        | Center = 0
        | Top = 1
        | Bottom = 2

    /// Specifies the style of data point labels in the Pyramid chart.
    type PyramidLabelStyle = 
        | Inside = 0
        | Outside = 1
        | OutsideInColumn = 2
        | Disabled = 3

    /// Specifies the placement of the data point labels in the
    /// Pyramid chart when the labels are placed outside the pyramid.
    type PyramidOutsideLabelPlacement = 
        | Right = 0
        | Left = 1

    /// Specifies whether the data point value represents a linear height
    /// or the surface of the segment.
    type PyramidValueType = 
        | Linear = 0
        | Surface = 1

    /// Specifies the drawing style of the Radar chart.
    type RadarDrawingStyle = 
        | Area = 0
        | Line = 1
        | Marker = 2

    /// Specifies whether markers for open and close prices are displayed.
    type ShowOpenClose = 
        | Both = 0
        | Open = 1
        | Close = 2

    // --------------------------------------------------------------------------------------


    // Background helpers
    [<RequireQualifiedAccess>]
    type Background = 
        | EmptyColor
        | Gradient of Color * Color * Charting.GradientStyle
        | Solid of Color


module private ChartFormUtilities = 

    open ChartStyles

    let inline applyBackground (obj:^T) back =
        match back with 
        | Background.EmptyColor ->
            (^T : (member set_BackColor : Color -> unit) (obj, Color.Empty))
        | Background.Solid color ->
            (^T : (member set_BackColor : Color -> unit) (obj, color))
        | Background.Gradient(first, second, style) ->
            (^T : (member set_BackColor : Color -> unit) (obj, first))
            (^T : (member set_BackSecondaryColor : Color -> unit) (obj, second))
            (^T : (member set_BackGradientStyle : Charting.GradientStyle -> unit) (obj, style))

    // Default font used when creating styles, titles, and legends
    let DefaultFontForTitles = new Font("Calibri", 16.0f, FontStyle.Regular)
    let DefaultFontForAxisLabels = new Font("Calibri", 12.0f, FontStyle.Regular)
    let DefaultFontForOthers = new Font("Arial Narrow", 10.0f, FontStyle.Regular)
    let DefaultFontForLegend = DefaultFontForOthers
    let DefaultFontForLabels = DefaultFontForOthers
    let DefaultMarginWithLegendOutside = (0.0, 0.0, 15.0, 0.0)
    let DefaultMarginWithLegendInside = (0.0, 0.0, 0.0, 0.0)

    // Type used for defining defaults
    type ChartStyleDefault =
        { ChartType:Charting.SeriesChartType option; ParentType:Type option; ParentParentType:Type option; PropertyName:string; PropertyDefault:obj }

    // Definition of defaults for the chart
    let PropertyDefaults = 
        [ // Define type specific defaults
         
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Line); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="BorderWidth"; PropertyDefault=(box 2) }
          //{ ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="IsDockedInsideChartArea"; PropertyDefault=(box false) }
          { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="BorderColor"; PropertyDefault=(box Color.Black) }
          { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="BorderWidth"; PropertyDefault=(box 1) }
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
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Line); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Spline); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Bar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Column); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Area); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedBar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedColumn); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedArea); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedBar100); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedColumn100); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StackedArea100); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.SplineArea); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Range); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, High=#VALY1, Low=#VALY2") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.RangeBar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "Y=#VALX, High=#VALY1, Low=#VALY2") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.RangeColumn); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, High=#VALY1, Low=#VALY2") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.SplineRange); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, High=#VALY1, Low=#VALY2") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Point); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.PointAndFigure); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, High=#VALY1, Low=#VALY2") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.ThreeLineBreak); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.StepLine); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Pie); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Doughnut); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.BoxPlot); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "Lower Whisker=#VALY1, Upper Whisker=#VALY2, Lower Box=#VALY3, Upper Box=#VALY4") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Candlestick); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "High=#VALY1, Low=#VALY2, Open=#VALY3, Close=#VALY4") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Stock); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "High=#VALY1, Low=#VALY2, Open=#VALY3, Close=#VALY4") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Renko); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Bubble); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VALY1, Size=#VALY2") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.ErrorBar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VALY1, Lower=#VALY2, Upper=#VALY3") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Funnel); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Pyramid); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Kagi); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "X=#VALX, Y=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Polar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "Angle=#VALX, Distance=#VAL") }
          { ChartStyleDefault.ChartType = Some(Charting.SeriesChartType.Radar); ParentParentType=None; ParentType = Some(typeof<Charting.Series>); PropertyName="ToolTip"; PropertyDefault=(box "Point=#VALX, Distance=#VAL") }
          // Define global defaults for fonts
          { ChartStyleDefault.ChartType = None; ParentParentType=Some(typeof<Charting.Axis>); ParentType = Some(typeof<Charting.LabelStyle>); PropertyName="Font"; PropertyDefault=(box DefaultFontForAxisLabels) }
          { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Title>); PropertyName="Font"; PropertyDefault=(box DefaultFontForTitles) }
          { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Title>); PropertyName="Font"; PropertyDefault=(box DefaultFontForTitles) }
          { ChartStyleDefault.ChartType = None; ParentParentType=None; ParentType = Some(typeof<Charting.Legend>); PropertyName="Font"; PropertyDefault=(box DefaultFontForLegend) }
        ]

    let typesToClone = 
        [ typeof<System.Windows.Forms.DataVisualization.Charting.LabelStyle>;
          typeof<System.Windows.Forms.DataVisualization.Charting.Axis>;
          typeof<System.Windows.Forms.DataVisualization.Charting.Grid>; 
          typeof<System.Windows.Forms.DataVisualization.Charting.TickMark>
          typeof<System.Windows.Forms.DataVisualization.Charting.ElementPosition>; 
          typeof<System.Windows.Forms.DataVisualization.Charting.AxisScaleView>; 
          typeof<System.Windows.Forms.DataVisualization.Charting.AxisScrollBar>; ]

    let typesToCopy = [ typeof<Font>; typeof<String> ]

    let applyDefaults (chartType:SeriesChartType, target:'a, targetParentType:Type option, targetType:Type, property:PropertyInfo) = 
        let isMatch propDefault = 
            if String.Equals(propDefault.PropertyName, property.Name) then
                (propDefault.ChartType |> Option.forall (fun seriesType -> chartType = seriesType))
                && (propDefault.ParentType |> Option.forall (fun parentType -> targetType.IsAssignableFrom(parentType) || targetType.IsSubclassOf(parentType)))
                && (propDefault.ParentParentType |> Option.forall (fun parentParentType -> targetParentType |> Option.exists (fun t -> t.IsAssignableFrom(parentParentType) || t.IsSubclassOf(parentParentType))))
            else
                false
        match List.tryFind isMatch PropertyDefaults with
        | Some item -> property.SetValue(target, item.PropertyDefault, [||])
        | _ -> ()

    let applyPropertyDefaults (chartType:SeriesChartType) (target:'a) = 
        let visited = new System.Collections.Generic.Dictionary<_, _>()
        let rec loop targetParent target = 
            if not (visited.ContainsKey target) then
                visited.Add(target, true)
                let targetParentType = match targetParent with None -> None | Some v -> Some (v.GetType())
                let targetType = target.GetType()
                for property in targetType.GetProperties(BindingFlags.Public ||| BindingFlags.Instance) do
                    if property.CanRead then
                        if typesToClone |> Seq.exists ((=) property.PropertyType) then
                            loop (Some target) (property.GetValue(target, [||]))
                        elif property.CanWrite then
                            if property.PropertyType.IsValueType || typesToCopy |> Seq.exists ((=) property.PropertyType) then
                                applyDefaults (chartType, target, targetParentType, targetType, property)
        loop None target

    let applyProperties (target:'a) (source:'a) = 
        let visited = new System.Collections.Generic.Dictionary<_, _>()
        let rec loop target source = 
            if not (visited.ContainsKey target) then
                visited.Add(target, true)
                let ty = target.GetType()
                for p in ty.GetProperties(BindingFlags.Public ||| BindingFlags.Instance) do
                    if p.CanRead then
                        if typesToClone |> Seq.exists ((=) p.PropertyType) then
                            loop (p.GetValue(target, [||])) (p.GetValue(source, [||]))
                        elif p.CanWrite then
                            if p.PropertyType.IsValueType || typesToCopy |> Seq.exists ((=) p.PropertyType) then
                                if p.GetSetMethod().GetParameters().Length <= 1 then
                                    p.SetValue(target, p.GetValue(source, [||]), [||])
        loop target source

    let createCounter() = 
        let count = ref 0
        (fun () -> incr count; !count) 

module ChartData = 

    open ChartFormUtilities
    open ChartStyles

    type public DataPoint(X: IConvertible, Y: IConvertible, Label: string) = 
        member __.Label = Label 
        member __.X = X // System.Convert.ToDouble X 
        member __.Y = Y // System.Convert.ToDouble Y 

    type public TwoXYDataPoint(X: IConvertible, Y1: IConvertible, Y2: IConvertible, Label: string) = 
        member __.Label = Label 
        member __.X = System.Convert.ToDouble X 
        member __.Y1 = Y1 // System.Convert.ToDouble Y1 
        member __.Y2 = Y2 // System.Convert.ToDouble Y2

    type public ThreeXYDataPoint(X: IConvertible, Y1: IConvertible, Y2: IConvertible, Y3: IConvertible, Label: string) = 
        member __.Label = Label 
        member __.X = X // System.Convert.ToDouble X 
        member __.Y1 = Y1 // System.Convert.ToDouble Y1
        member __.Y2 = Y2 // System.Convert.ToDouble Y2
        member __.Y3 = Y3 // System.Convert.ToDouble Y3

    type public FourXYDataPoint(X: IConvertible, Y1: IConvertible, Y2: IConvertible, Y3: IConvertible, Y4: IConvertible, Label: string) = 
        member __.Label = Label 
        member __.X =  X // System.Convert.ToDouble X 
        member __.Y1 = Y1 // System.Convert.ToDouble Y1
        member __.Y2 = Y2 // System.Convert.ToDouble Y2
        member __.Y3 = Y3 // System.Convert.ToDouble Y3
        member __.Y4 = Y4 // System.Convert.ToDouble Y4

    module internal Internal = 
        [<RequireQualifiedAccess>]
        type internal ChartData =
            // general binding
            | Values of IEnumerable * string * string * string
            // single value
            | XYValues of IEnumerable * IEnumerable
            | XYValuesIObservable of int * IObservable<IConvertible * float>
            // multiple values
            | MultiYValues of IEnumerable[]
            | MultiYValuesIObservable of int * IObservable<float[]>
            | XMultiYValues of IEnumerable * IEnumerable[]    
            | XMultiYValuesIObservable of int * IObservable<IConvertible * float[]>

            // stacked values
            | StackedXYValues of seq<IEnumerable * IEnumerable>

            // This one is treated specially (for box plot, we need to add data as a separate series)
            // TODO: This is a bit inconsistent - we should unify the next one with the last 
            // four and then handle boxplot chart type when adding data to the series
            | BoxPlotYArrays of seq<IEnumerable>
            | BoxPlotXYArrays of seq<IConvertible * IEnumerable>
            // A version of BoxPlot arrays with X values is missing (could be useful)
            // Observable version is not supported (probably nobody needs it)
            // (Unifying would sovlve these though)


        // ----------------------------------------------------------------------------------
        // Utilities for working with enumerable and tuples

        let seqmap f (source: seq<'T>) = 
            { new IEnumerable with
                  member x.GetEnumerator() = 
                      let en = source.GetEnumerator()
                      { new IEnumerator with 
                          member x.Current = box (f en.Current)
                          member x.MoveNext() = en.MoveNext()
                          member x.Reset() = en.Reset() } }

        /// Evaluate once only. Unlike Seq.cache this evaluates to an array and returns an IEnumerator that supports Reset without failing.
        /// The Reset method is called by the .NET charting library.
        let once (source: seq<'T>) = 
            let data = lazy Seq.toArray source
            { new IEnumerable<'T> with
                  member x.GetEnumerator() = (data.Force() :> seq<'T>).GetEnumerator() 
              interface IEnumerable with
                  member x.GetEnumerator() = (data.Force() :> IEnumerable).GetEnumerator() }

        // There are quite a few variations - let's have some write-only combinator fun

        let el1of3 (a, _, _) = a 
        let el2of3 (_, a, _) = a 
        let el3of3 (_, _, a) = a 

        let el1of4 (a, _, _, _) = a 
        let el2of4 (_, a, _, _) = a 
        let el3of4 (_, _, a, _) = a 
        let el4of4 (_, _, _, a) = a 

        let el1of5 (a, _, _, _, _) = a 
        let el2of5 (_, a, _, _, _) = a 
        let el3of5 (_, _, a, _, _) = a 
        let el4of5 (_, _, _, a, _) = a 
        let el5of5 (_, _, _, _, a) = a 

        let el1of6 (a, _, _, _, _, _) = a 
        let el2of6 (_, a, _, _, _, _) = a 
        let el3of6 (_, _, a, _, _, _) = a 
        let el4of6 (_, _, _, a, _, _) = a 
        let el5of6 (_, _, _, _, a, _) = a 
        let el6of6 (_, _, _, _, _, a) = a 

        let tuple2 f g (x, y) = f x, g y
        let tuple3 f g h (x, y, z) = f x, g y, h z
        let tuple4 f g h i (x, y, z, w) = f x, g y, h z, i w

        let arr2 (a, b) = [| a; b |]
        let arr3 (a, b, c) = [| a; b; c |]
        let arr4 (a, b, c, d) = [| a; b; c; d |]

        // Converts Y value of a chart (defines the type too)
        let culture = System.Globalization.CultureInfo.InvariantCulture
        let cval (v:System.IConvertible) = v.ToDouble culture
        let cobj (v:System.IConvertible) = v

        // ----------------------------------------------------------------------------------
        // Single Y value

        let oneXYSeqWithLabels (data:seq<_ * _>) labels = 
            let data = once data // evaluate only once and cache
            match labels with 
            | None -> ChartData.XYValues(seqmap (fst >> cobj) data, seqmap (snd >> cval >> box) data)
            | Some labels ->  ChartData.Values( [| for ((x,y),label) in Seq.zip data labels -> DataPoint(X=x,Y=y,Label=label) |], "X", "Y", "Label=Label") 

        // X and Y values as tuples, changing
        let oneXYObs maxPoints (source) = 
          ChartData.XYValuesIObservable(maxPoints, source |> Observable.map (tuple2 cobj cval))

        // ----------------------------------------------------------------------------------
        // Two Y values

        let twoXYSeqWithLabels (data:seq<_ * _>) labels = 
            let data = once data // evaluate only once and cache
            match labels with 
            | None -> ChartData.XMultiYValues(seqmap (fst >> cobj) data, [| seqmap (snd >> fst >> cval >> box) data; seqmap (snd >> snd >> cval >> box) data |])
            | Some labels ->  ChartData.Values( [| for ((x,(y1,y2)),label) in Seq.zip data labels -> TwoXYDataPoint(X=x,Y1=y1,Label=label,Y2=y2) |], "X", "Y1,Y2", "Label=Label") 

        // X and Y values as tuples, changing
        let twoXYObs maxPoints (source) = 
          ChartData.XMultiYValuesIObservable(maxPoints, source |> Observable.map (tuple2 cobj (tuple2 cval cval >> arr2)))

        // ----------------------------------------------------------------------------------
        // Three Y values

        // X and Y values as tuples
        let threeXYSeqWithLabels data labels = 
            let data = once data // evaluate only once and cache
            match labels with 
            | None -> ChartData.XMultiYValues(seqmap (fst >> cobj) data, [| seqmap (snd >> el1of3 >> cval >> box) data; seqmap (snd >> el2of3 >> cval >> box) data; seqmap (snd >> el3of3 >> cval >> box) data |])            
            | Some labels ->  ChartData.Values( [| for ((x,(y1,y2,y3)),label) in Seq.zip data labels -> ThreeXYDataPoint(X=x,Y1=y1,Label=label,Y2=y2,Y3=y3) |], "X", "Y1,Y2,Y3", "Label=Label") 

        // X and Y values as tuples, changing
        let threeXYObs maxPoints (source) = 
            ChartData.XMultiYValuesIObservable(maxPoints, source |> Observable.map (tuple2 cobj (tuple3 cval cval cval >> arr3)))

        // ----------------------------------------------------------------------------------
        // Four Y values

        // X and Y values as tuples
        let fourXYSeqWithLabels data labels = 
            let data = once data // evaluate only once and cache
            match labels with 
            | None -> ChartData.XMultiYValues(seqmap (fst >> cobj) data, [| seqmap (snd >> el1of4 >> cval >> box) data; seqmap (snd >> el2of4 >> cval >> box) data; seqmap (snd >> el3of4 >> cval >> box) data; seqmap (snd >> el4of4 >> cval >> box) data |])
            | Some labels ->  ChartData.Values( [| for ((x,(y1,y2,y3,y4)),label) in Seq.zip data labels -> FourXYDataPoint(X=x,Y1=y1,Y2=y2,Y3=y3,Y4=y4,Label=label) |], "X", "Y1,Y2,Y3,Y4", "Label=Label") 
            

        // X and Y values as tuples, changing
        let fourXYObs maxPoints (source) = 
            ChartData.XMultiYValuesIObservable(maxPoints, source |> Observable.map (tuple2 cobj (tuple4 cval cval cval cval >> arr4)))

        // ----------------------------------------------------------------------------------
        // Six or more values

        // Y values only
        let sixY data = 
            let data = once data // evaluate only once and cache
            ChartData.MultiYValues([| seqmap (el1of6 >> cval >> box) data; seqmap (el2of6 >> cval >> box) data; seqmap (el3of6 >> cval >> box) data; seqmap (el4of6 >> cval >> box) data; seqmap (el5of6 >> cval >> box) data; seqmap (el6of6 >> cval >> box) data |])

        // Y values (for BoxPlot charts)
        let sixYArrBox (data:seq<#IConvertible[]>) = 
            let data = once data // evaluate only once and cache
            ChartData.BoxPlotYArrays (data |> Seq.map (seqmap cval))

        // X and Y values as array (for BoxPlot charts)
        // TODO: labels
        let sixXYArrBoxWithLabels data labels = 
            let data = once data // evaluate only once and cache
            let series = (data |> Seq.map (fun item -> cobj (fst item), seqmap cval (snd item)))
            ChartData.BoxPlotXYArrays(series)

        // ----------------------------------------------------------------------------------
        // Stacked sequence values

        // Sequence of X and Y Values only
        // TODO: labels
        let seqXY (data: seq< #seq<'TX * 'TY>>) = 
            let data = once data // evaluate only once and cache
            let series = (data |> Seq.toList |> List.map (fun item -> item |> Seq.toArray |> Array.unzip |> (fun (itemX,itemY) -> (seqmap cobj itemX, seqmap cval itemY)) ))
            ChartData.StackedXYValues (series)

        // --------------------------------------------------------------------------------------

        let internal bindObservable (chart:Chart, series:Series, maxPoints, values, adder) = 
            series.Points.Clear()
            let rec disp = 
                values |> Observable.subscribe (fun v ->
                    let op () = 
                        try
                            adder series.Points v
                            if maxPoints <> -1 && series.Points.Count > maxPoints then
                                series.Points.RemoveAt(0) 
                        with 
                        | :? NullReferenceException ->
                            disp.Dispose() 
                    if chart.InvokeRequired then
                        chart.Invoke(Action(op)) |> ignore
                    else 
                        op())
            ()


        let internal setSeriesData resetSeries (series:Series) data (chart:Chart) setCustomProperty =             

            let bindBoxPlot values getSeries getLabel (displayLabel:bool) = 
                let labels = chart.ChartAreas.[0].AxisX.CustomLabels
                if resetSeries then
                    labels.Clear()
                    while chart.Series.Count > 1 do chart.Series.RemoveAt(1)                                 
                let name = series.Name
                let seriesNames = 
                    values |> Seq.mapi (fun index series ->
                        let name = getLabel name index series
                        let dataSeries = new Series(name, Enabled = false, ChartType=SeriesChartType.BoxPlot)
                        dataSeries.Points.DataBindY [| getSeries series |]
                        if displayLabel then
                            labels.Add(float (index), float (index + 2), name) |> ignore
                            dataSeries.AxisLabel <- name
                            dataSeries.Label <- name
                        chart.Series.Add dataSeries
                        name )
                let boxPlotSeries = seriesNames |> String.concat ";"
                setCustomProperty("BoxPlotSeries", boxPlotSeries)

            let bindStackedChart values binder =                
                let name = series.Name
                let chartType = series.ChartType       
                while chart.Series.Count > 0 do chart.Series.RemoveAt(0)        
                values |> Seq.iteri (fun index seriesValue ->
                    let name = sprintf "Stacked_%s_%d" name index
                    let dataSeries = new Series(name, Enabled = false, ChartType=chartType)
                    applyProperties dataSeries series
                    dataSeries.Name <- name
                    binder dataSeries seriesValue
                    chart.Series.Add dataSeries)

            match data with 
            | ChartData.Values (vs,xField,yField,otherFields) ->
                series.Points.DataBind(vs,xField,yField,otherFields)
            | ChartData.XYValues(xs, ys) ->
                //let series = 
                //    if resetSeries then
                //        chart.Series.[0].Po
                //        let name = getLabel name index series
                //        let dataSeries = new Series(name, Enabled = false)
                //        dataSeries
                //    else
                //       series

                series.Points.DataBindXY(xs, [| ys |])
            | ChartData.XYValuesIObservable(maxPoints, xys) ->
                bindObservable (chart, series, maxPoints, xys, (fun pts (x, y) -> pts.AddXY(x, y) |> ignore))
            
            // Multiple Y values
            // TODO: Won't work for BoxPlot chart when the array contains more than 6 values
            // (but on the other hand, this will work for all 2/3/4 Y values charts)
            | ChartData.XMultiYValues(xs, yss) -> 
                series.Points.DataBindXY(xs, yss)
            | ChartData.MultiYValues yss ->
                series.Points.DataBindY yss
            | ChartData.MultiYValuesIObservable(maxPoints, yss) ->
                bindObservable (chart, series, maxPoints, yss, (fun pts ys -> pts.AddY(Array.map box ys) |> ignore))
            | ChartData.XMultiYValuesIObservable(maxPoints, yss) ->
                bindObservable (chart, series, maxPoints, yss, (fun pts (x, ys) -> pts.AddXY(x, Array.map box ys) |> ignore))
            
            // Special case for BoxPlot
            | ChartData.BoxPlotYArrays values ->
                bindBoxPlot values id (fun name index value -> sprintf "Boxplot_%s_%d" name index) false
            | ChartData.BoxPlotXYArrays values ->
                bindBoxPlot values snd (fun name index value -> string (fst value)) true

            // Special case for Stacked
            | ChartData.StackedXYValues values ->
                bindStackedChart values (fun (dataSeries:Series) seriesValue -> dataSeries.Points.DataBindXY((fst seriesValue), ([| snd seriesValue |])))



module ChartTypes = 

    open ChartFormUtilities
    open ChartData.Internal
    open ChartData
    open ChartStyles

    [<AbstractClass>]
    type GenericChart(chartType) as self =     
               
        // Events
        let propChangedDataSource = Event<ChartData>()
        let propChangedMargin = Event<float * float * float * float>()
        let propChangedBackground = Event<Background>()
        let propChangedName = Event<string>()
        let propChangedTitle = Event<Title>()
        let propChangedLegend = Event<Legend>()
        let propChangedCustom = Event<string * obj>()

        let customProperties = new Dictionary<_, _>()

        let mutable area = lazy (
            let area = new ChartArea()
            applyPropertyDefaults self.ChartType area
            applyPropertyDefaults self.ChartType area.AxisX
            applyPropertyDefaults self.ChartType area.AxisY
            applyPropertyDefaults self.ChartType area.AxisX2
            applyPropertyDefaults self.ChartType area.AxisY2
            area)

        let mutable series = lazy (
            let series = new Series()
            applyPropertyDefaults self.ChartType series
            series)

        let mutable chart = lazy (
            let ch = new Chart()
            applyPropertyDefaults self.ChartType ch
            ch)

        let mutable title = lazy (
            let title = new Title()
            applyPropertyDefaults self.ChartType title
            title)

        let mutable legend = lazy (
            let legend = new Legend()
            applyPropertyDefaults self.ChartType legend
            legend)

        let mutable name:string = ""

        let evalLazy v =
            let l = lazy v
            l.Force() |> ignore
            l

        let mutable data = ChartData.XYValues ([],[])
        let mutable margin = DefaultMarginWithLegendOutside
        let titles = new ResizeArray<Title>()
        let legends = new ResizeArray<Legend>()

        member internal x.DataSourceChanged = propChangedDataSource.Publish
        member internal x.ChartType = chartType
        member public chart.ChartTypeName = 
            match int chart.ChartType with
            | -1 -> "Combined"
            | _ -> chart.ChartType.ToString()
    
        member internal x.Data with get() = data and set v = data <- v
        member internal x.Chart with get() = chart.Value and set v = chart <- evalLazy v
        
        // deal with properties that raise events
        member internal x.Margin 
                        with get() = margin
                        and set v =
                            margin <- v
                            propChangedMargin.Trigger(v)

        member internal x.Background 
                      with set v =
                                applyBackground chart.Value v
                                propChangedBackground.Trigger(v)

        member x.Name 
                      with get() = name
                      and set v =
                          name <- v
                          propChangedName.Trigger(v)

        member internal x.Title 
                       with get() = title.Value 
                       and set v = 
                           title <- evalLazy v
                           propChangedTitle.Trigger(v)

        member internal x.Legend 
                        with get() = legend.Value
                        and set v =
                            legend <- evalLazy v
                            propChangedLegend.Trigger(v)        

        // other properties
        member internal x.Area 
                      with get() = area.Value
                      and set v =
                          area <- evalLazy v

        member internal x.Series 
                        with get() = series.Value
                        and set v =
                            series <- evalLazy v

        // internal
        member internal x.Titles = titles
        member internal x.LazyTitle = title
        member internal x.Legends = legends

        member internal x.LazyChart = chart
        member internal x.LazyArea = area
        member internal x.LazySeries = series
        
        [<CLIEvent>]
        member internal x.MarginChanged = propChangedMargin.Publish
        [<CLIEvent>]
        member internal x.BackgroundChanged = propChangedBackground.Publish
        [<CLIEvent>]
        member internal x.NameChanged = propChangedName.Publish
        [<CLIEvent>]
        member internal x.TitleChanged = propChangedTitle.Publish
        [<CLIEvent>]
        member internal x.LegendChanged = propChangedLegend.Publish
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

        member public x.CopyChartToClipboard() =
            use ms = new IO.MemoryStream()
            x.Chart.SaveImage(ms, ChartImageFormat.Png |> int |> enum)
            ms.Seek(0L, IO.SeekOrigin.Begin) |> ignore
            Clipboard.SetImage(Bitmap.FromStream ms)

        member public x.CopyChartToClipboardEmf(control:Control) =
            use ms = new IO.MemoryStream()
            x.Chart.SaveImage(ms, ChartImageFormat.Emf |> int |> enum)
            ms.Seek(0L, IO.SeekOrigin.Begin) |> ignore
            use emf = new System.Drawing.Imaging.Metafile(ms)
            ClipboardMetafileHelper.PutEnhMetafileOnClipboard(control.Handle, emf) |> ignore

        member public x.SaveChartAs(filename : string, format : ChartImageFormat) =
            x.Chart.SaveImage(filename, format  |> int |> enum)

        member internal ch.SetDataInternal(data) = 
            propChangedDataSource.Trigger data
            
    type private ColorWrapper(clr:Color) =
        member x.Color = clr
        override x.ToString() =
            if clr.IsEmpty then "Empty" else
            sprintf "%d" (clr.ToArgb()) // clr.R clr.G clr.B

    // ------------------------------------------------------------------------------------
    // Specific chart types for setting custom properties

    /// Displays multiple series of data as stacked areas. The cumulative proportion
    /// of each stacked element is always 100% of the Y
    /// axis.
    type StackedArea100Chart() = 
        inherit GenericChart(SeriesChartType.StackedArea100)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)


    /// Displays multiple series of data as stacked bars. The cumulative
    /// proportion of each stacked element is always 100% of the Y axis.
    type StackedBar100Chart() = 
        inherit GenericChart(SeriesChartType.StackedBar100)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)


        /// Specifies the placement of the data point label.
        member x.BarLabelStyle
            with get() = x.GetCustomProperty<BarLabelStyle>("BarLabelStyle", BarLabelStyle.Outside)
            and set(v) = x.SetCustomProperty<BarLabelStyle>("BarLabelStyle", v)

        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)

        /// Specifies the name of the stacked group.
        member x.StackedGroupName
            with get() = x.GetCustomProperty<string>("StackedGroupName", "")
            and set(v) = x.SetCustomProperty<string>("StackedGroupName", v)


    /// Displays multiple series of data as stacked columns. The cumulative
    /// proportion of each stacked element is always 100% of the
    /// Y axis.
    type StackedColumn100Chart() = 
        inherit GenericChart(SeriesChartType.StackedColumn100)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)


        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)

        /// Specifies the name of the stacked group.
        member x.StackedGroupName
            with get() = x.GetCustomProperty<string>("StackedGroupName", "")
            and set(v) = x.SetCustomProperty<string>("StackedGroupName", v)


    /// Emphasizes the degree of change over time and shows the
    /// relationship of the parts to a whole.
    type AreaChart() = 
        inherit GenericChart(SeriesChartType.Area)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies whether marker lines are displayed when rendered in 3D.
        member x.ShowMarkerLines
            with get() = x.GetCustomProperty<bool>("ShowMarkerLines", false)
            and set(v) = x.SetCustomProperty<bool>("ShowMarkerLines", v)


    /// Illustrates comparisons among individual items.
    type BarChart() = 
        inherit GenericChart(SeriesChartType.Bar)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the placement of the data point label.
        member x.BarLabelStyle
            with get() = x.GetCustomProperty<BarLabelStyle>("BarLabelStyle", BarLabelStyle.Outside)
            and set(v) = x.SetCustomProperty<BarLabelStyle>("BarLabelStyle", v)

        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// Specifies whether series of the same chart type are drawn
        /// next to each other instead of overlapping each other.
        member x.DrawSideBySide
            with get() = x.GetCustomProperty<DrawSideBySide>("DrawSideBySide", DrawSideBySide.Auto)
            and set(v) = x.SetCustomProperty<DrawSideBySide>("DrawSideBySide", v)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)


    /// Consists of one or more box symbols that summarize the
    /// distribution of the data within one or more data sets.
    type BoxPlotChart() = 
        inherit GenericChart(SeriesChartType.BoxPlot)

        /// Set the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(sixXYArrBoxWithLabels data Labels)
        /// <summary>
        ///   Specifies the percentile value of the box of the Box
        ///   Plot chart.
        /// </summary>
        /// <remarks>Any integer from 0 to 50.</remarks>
        member x.Percentile
            with get() = x.GetCustomProperty<int>("BoxPlotPercentile", 25)
            and set(v) = x.SetCustomProperty<int>("BoxPlotPercentile", v)

        /// Specifies whether to display the average value for the Box
        /// Plot chart.
        member x.ShowAverage
            with get() = x.GetCustomProperty<bool>("BoxPlotShowAverage", true)
            and set(v) = x.SetCustomProperty<bool>("BoxPlotShowAverage", v)

        /// Specifies whether to display the median value for the Box
        /// Plot chart.
        member x.ShowMedian
            with get() = x.GetCustomProperty<bool>("BoxPlotShowMedian", true)
            and set(v) = x.SetCustomProperty<bool>("BoxPlotShowMedian", v)

        /// Specifies whether the unusual values value for the Box Plot
        /// chart will be shown.
        member x.ShowUnusualValues
            with get() = x.GetCustomProperty<bool>("BoxPlotShowUnusualValues", true)
            and set(v) = x.SetCustomProperty<bool>("BoxPlotShowUnusualValues", v)

        /// <summary>
        ///   Specifies the percentile value of the whiskers of the Box
        ///   Plot chart.
        /// </summary>
        /// <remarks>Any integer from 0 to 50.</remarks>
        member x.WhiskerPercentile
            with get() = x.GetCustomProperty<int>("BoxPlotWhiskerPercentile", 10)
            and set(v) = x.SetCustomProperty<int>("BoxPlotWhiskerPercentile", v)

        /// Specifies whether series of the same chart type are drawn
        /// next to each other instead of overlapping each other.
        member x.DrawSideBySide
            with get() = x.GetCustomProperty<DrawSideBySide>("DrawSideBySide", DrawSideBySide.Auto)
            and set(v) = x.SetCustomProperty<DrawSideBySide>("DrawSideBySide", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)


    /// A variation of the Point chart type, where the data
    /// points are replaced by bubbles of different sizes.
    type BubbleChart() = 
        inherit GenericChart(SeriesChartType.Bubble)

        /// Set the data on the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(twoXYSeqWithLabels data Labels)

        /// <summary>
        ///   Specifies the maximum size of the bubble radius as a
        ///   percentage of the chart area size.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.BubbleMaxSize
            with get() = x.GetCustomProperty<int>("BubbleMaxSize", 15)
            and set(v) = x.SetCustomProperty<int>("BubbleMaxSize", v)

        /// <summary>
        ///   Specifies the minimum size of the bubble radius as a
        ///   percentage of the chart area size.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.BubbleMinSize
            with get() = x.GetCustomProperty<int>("BubbleMinSize", 3)
            and set(v) = x.SetCustomProperty<int>("BubbleMinSize", v)

        /// <summary>
        ///   Specifies the maximum bubble size, which is a percentage of
        ///   the chart area that is set by BubbleMaxSize.
        /// </summary>
        /// <remarks>Any double.</remarks>
        member x.BubbleScaleMax
            with get() = x.GetCustomProperty<float>("BubbleScaleMax", 15.0)
            and set(v) = x.SetCustomProperty<float>("BubbleScaleMax", v)

        /// <summary>
        ///   Specifies the minimum bubble size, which is a percentage of
        ///   the chart area that is set by BubbleMinSize.
        /// </summary>
        /// <remarks>Any double.</remarks>
        member x.BubbleScaleMin
            with get() = x.GetCustomProperty<float>("BubbleScaleMin", 3.0)
            and set(v) = x.SetCustomProperty<float>("BubbleScaleMin", v)

        /// Specifies whether to use the bubble size as the data
        /// point label.
        member x.BubbleUseSizeForLabel
            with get() = x.GetCustomProperty<bool>("BubbleUseSizeForLabel", false)
            and set(v) = x.SetCustomProperty<bool>("BubbleUseSizeForLabel", v)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)


    /// Used to display stock information using high, low, open and
    /// close values.
    type CandlestickChart() = 
        inherit GenericChart(SeriesChartType.Candlestick)

        /// Set the data on the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(fourXYSeqWithLabels data Labels)

        /// Specifies the Y value to use as the data point
        /// label.
        member x.LabelValueType
            with get() = x.GetCustomProperty<LabelValueType>("LabelValueType", LabelValueType.Close)
            and set(v) = x.SetCustomProperty<LabelValueType>("LabelValueType", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)

        /// Specifies the data point color to use to indicate a
        /// decreasing trend.
        member x.PriceDownColor
            with get() = x.GetCustomProperty<ColorWrapper>("PriceDownColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PriceDownColor", ColorWrapper(v))

        /// Specifies the data point color that indicates an increasing trend.
        member x.PriceUpColor
            with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))


    /// Uses a sequence of columns to compare values across categories.
    type ColumnChart() = 
        inherit GenericChart(SeriesChartType.Column)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// Specifies whether series of the same chart type are drawn
        /// next to each other instead of overlapping each other.
        member x.DrawSideBySide
            with get() = x.GetCustomProperty<DrawSideBySide>("DrawSideBySide", DrawSideBySide.Auto)
            and set(v) = x.SetCustomProperty<DrawSideBySide>("DrawSideBySide", v)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)


    /// Similar to the Pie chart type, except that it has
    /// a hole in the center.
    type DoughnutChart() = 
        inherit GenericChart(SeriesChartType.Doughnut)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// <summary>
        ///   Specifies the 3D label line size as a percentage of
        ///   the default size.
        /// </summary>
        /// <remarks>Any integer from 30 to 200.</remarks>
        member x.LabelLineSize3D
            with get() = x.GetCustomProperty<int>("3DLabelLineSize", 100)
            and set(v) = x.SetCustomProperty<int>("3DLabelLineSize", v)

        /// Specifies the color of the collected pie or doughnut slice.
        member x.CollectedColor
            with get() = x.GetCustomProperty<ColorWrapper>("CollectedColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("CollectedColor", ColorWrapper(v))

        /// Specifies the label of the collected pie slice.
        member x.CollectedLabel
            with get() = x.GetCustomProperty<string>("CollectedLabel", "")
            and set(v) = x.SetCustomProperty<string>("CollectedLabel", v)

        /// Specifies the legend text for the collected pie slice.
        member x.CollectedLegendText
            with get() = x.GetCustomProperty<string>("CollectedLegendText", "")
            and set(v) = x.SetCustomProperty<string>("CollectedLegendText", v)

        /// Specifies whether the collected pie slice will be shown as
        /// exploded.
        member x.CollectedSliceExploded
            with get() = x.GetCustomProperty<bool>("CollectedSliceExploded", true)
            and set(v) = x.SetCustomProperty<bool>("CollectedSliceExploded", v)

        /// <summary>
        ///   Specifies the threshold value for collecting small pie slices.
        /// </summary>
        /// <remarks>Any double between 0 and 100 if CollectedThresholdUsePercent is true; otherwise, any double &gt; 0.</remarks>
        member x.CollectedThreshold
            with get() = x.GetCustomProperty<float>("CollectedThreshold", 0.0)
            and set(v) = x.SetCustomProperty<float>("CollectedThreshold", v)

        /// Specifies whether to use the collected threshold value as a
        /// percentage.
        member x.CollectedThresholdUsePercent
            with get() = x.GetCustomProperty<bool>("CollectedThresholdUsePercent", true)
            and set(v) = x.SetCustomProperty<bool>("CollectedThresholdUsePercent", v)

        /// Specifies the tooltip text of the collected pie slice.
        member x.CollectedToolTip
            with get() = x.GetCustomProperty<string>("CollectedToolTip", "")
            and set(v) = x.SetCustomProperty<string>("CollectedToolTip", v)

        /// <summary>
        ///   Specifies the radius of the doughnut portion in the Doughnut
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
        ///   Specifies the size of the horizontal segment of the callout
        ///   line.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.LabelsHorizontalLineSize
            with get() = x.GetCustomProperty<int>("LabelsHorizontalLineSize", 1)
            and set(v) = x.SetCustomProperty<int>("LabelsHorizontalLineSize", v)

        /// <summary>
        ///   Specifies the size of the radial segment of the callout
        ///   line.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.LabelsRadialLineSize
            with get() = x.GetCustomProperty<int>("LabelsRadialLineSize", 1)
            and set(v) = x.SetCustomProperty<int>("LabelsRadialLineSize", v)

        /// <summary>
        ///   Specifies the minimum pie or doughnut size.
        /// </summary>
        /// <remarks>Any integer from 10 to 70.</remarks>
        member x.MinimumRelativePieSize
            with get() = x.GetCustomProperty<int>("MinimumRelativePieSize", 30)
            and set(v) = x.SetCustomProperty<int>("MinimumRelativePieSize", v)

        /// Specifies the drawing style of the data points.
        member x.PieDrawingStyle
            with get() = x.GetCustomProperty<PieDrawingStyle>("PieDrawingStyle", PieDrawingStyle.Default)
            and set(v) = x.SetCustomProperty<PieDrawingStyle>("PieDrawingStyle", v)

        /// Specifies the label style of the data points.
        member x.PieLabelStyle
            with get() = x.GetCustomProperty<PieLabelStyle>("PieLabelStyle", PieLabelStyle.Inside)
            and set(v) = x.SetCustomProperty<PieLabelStyle>("PieLabelStyle", v)

        /// Specifies the color of the radial and horizontal segments of
        /// the callout lines.
        member x.PieLineColor
            with get() = x.GetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(Color.Black)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(v))

        /// <summary>
        ///   Specifies the angle of the data point in the Pie
        ///   or Doughnut chart.
        /// </summary>
        /// <remarks>Any integer from 0 to 360.</remarks>
        member x.PieStartAngle
            with get() = x.GetCustomProperty<int>("PieStartAngle", 90)
            and set(v) = x.SetCustomProperty<int>("PieStartAngle", v)


    /// Consists of lines with markers that are used to display
    /// statistical information about the data displayed in a graph.
    type ErrorBarChart() = 
        inherit GenericChart(SeriesChartType.ErrorBar)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(threeXYSeqWithLabels data Labels)

        /// Specifies whether series of the same chart type are drawn
        /// next to each other instead of overlapping each other.
        member x.DrawSideBySide
            with get() = x.GetCustomProperty<DrawSideBySide>("DrawSideBySide", DrawSideBySide.Auto)
            and set(v) = x.SetCustomProperty<DrawSideBySide>("DrawSideBySide", v)

        /// Specifies the appearance of the marker at the center value
        /// of the error bar.
        member x.ErrorBarCenterMarkerStyle
            with get() = x.GetCustomProperty<ErrorBarCenterMarkerStyle>("ErrorBarCenterMarkerStyle", ErrorBarCenterMarkerStyle.None)
            and set(v) = x.SetCustomProperty<ErrorBarCenterMarkerStyle>("ErrorBarCenterMarkerStyle", v)

        /// Specifies the name of the series to be used as
        /// the data source for the Error Bar chart calculations.
        member x.ErrorBarSeries
            with get() = x.GetCustomProperty<string>("ErrorBarSeries", "")
            and set(v) = x.SetCustomProperty<string>("ErrorBarSeries", v)

        /// Specifies the visibility of the upper and lower error values.
        member x.ErrorBarStyle
            with get() = x.GetCustomProperty<ErrorBarStyle>("ErrorBarStyle", ErrorBarStyle.Both)
            and set(v) = x.SetCustomProperty<ErrorBarStyle>("ErrorBarStyle", v)

        /// Specifies how the upper and lower error values are calculated
        /// for the center values of the ErrorBarSeries.
        member x.ErrorBarType
            with get() = x.GetCustomProperty<ErrorBarType>("ErrorBarType", ErrorBarType.FixedValue)
            and set(v) = x.SetCustomProperty<ErrorBarType>("ErrorBarType", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)


    /// A variation of the Line chart that significantly reduces the
    /// drawing time of a series that contains a very large
    /// number of data points.
    type FastLineChart() = 
        inherit GenericChart(SeriesChartType.FastLine)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)


    /// A variation of the Point chart type that significantly reduces
    /// the drawing time of a series that contains a very
    /// large number of data points.
    type FastPointChart() = 
        inherit GenericChart(SeriesChartType.FastPoint)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)


    /// Displays in a funnel shape data that equals 100% when
    /// totaled.
    type FunnelChart() = 
        inherit GenericChart(SeriesChartType.Funnel)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)


        /// Specifies the line color of the callout for the data
        /// point labels of Funnel or Pyramid charts.
        member x.CalloutLineColor
            with get() = x.GetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(v))

        /// Specifies the 3D drawing style of the Funnel chart type.
        member x.Funnel3DDrawingStyle
            with get() = x.GetCustomProperty<Funnel3DDrawingStyle>("Funnel3DDrawingStyle", Funnel3DDrawingStyle.SquareBase)
            and set(v) = x.SetCustomProperty<Funnel3DDrawingStyle>("Funnel3DDrawingStyle", v)

        /// <summary>
        ///   Specifies the 3D rotation angle of the Funnel chart type.
        /// </summary>
        /// <remarks>Any integer from -10 to 10.</remarks>
        member x.Funnel3DRotationAngle
            with get() = x.GetCustomProperty<int>("Funnel3DRotationAngle", 5)
            and set(v) = x.SetCustomProperty<int>("Funnel3DRotationAngle", v)

        /// Specifies the data point label placement of the Funnel chart
        /// type when the FunnelLabelStyle is set to Inside.
        member x.FunnelInsideLabelAlignment
            with get() = x.GetCustomProperty<FunnelInsideLabelAlignment>("FunnelInsideLabelAlignment", FunnelInsideLabelAlignment.Center)
            and set(v) = x.SetCustomProperty<FunnelInsideLabelAlignment>("FunnelInsideLabelAlignment", v)

        /// Specifies the data point label style of the Funnel chart
        /// type.
        member x.FunnelLabelStyle
            with get() = x.GetCustomProperty<FunnelLabelStyle>("FunnelLabelStyle", FunnelLabelStyle.OutsideInColumn)
            and set(v) = x.SetCustomProperty<FunnelLabelStyle>("FunnelLabelStyle", v)

        /// <summary>
        ///   Specifies the minimum height of a data point in the
        ///   Funnel chart, measured in relative coordinates.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.FunnelMinPointHeight
            with get() = x.GetCustomProperty<int>("FunnelMinPointHeight", 0)
            and set(v) = x.SetCustomProperty<int>("FunnelMinPointHeight", v)

        /// <summary>
        ///   Specifies the neck height of the Funnel chart type.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.FunnelNeckHeight
            with get() = x.GetCustomProperty<int>("FunnelNeckHeight", 5)
            and set(v) = x.SetCustomProperty<int>("FunnelNeckHeight", v)

        /// <summary>
        ///   Specifies the neck width of the Funnel chart type.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.FunnelNeckWidth
            with get() = x.GetCustomProperty<int>("FunnelNeckWidth", 5)
            and set(v) = x.SetCustomProperty<int>("FunnelNeckWidth", v)

        /// Placement of the data point label in the Funnel chart
        /// when FunnelLabelStyle is set to Outside or OutsideInColumn.
        member x.FunnelOutsideLabelPlacement
            with get() = x.GetCustomProperty<FunnelOutsideLabelPlacement>("FunnelOutsideLabelPlacement", FunnelOutsideLabelPlacement.Right)
            and set(v) = x.SetCustomProperty<FunnelOutsideLabelPlacement>("FunnelOutsideLabelPlacement", v)

        /// <summary>
        ///   Specifies the gap size between the points of a Funnel
        ///   chart, measured in relative coordinates.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.FunnelPointGap
            with get() = x.GetCustomProperty<int>("FunnelPointGap", 0)
            and set(v) = x.SetCustomProperty<int>("FunnelPointGap", v)

        /// Specifies the style of the Funnel chart type.
        member x.FunnelStyle
            with get() = x.GetCustomProperty<FunnelStyle>("FunnelStyle", FunnelStyle.YIsHeight)
            and set(v) = x.SetCustomProperty<FunnelStyle>("FunnelStyle", v)


    /// Displays a series of connecting vertical lines where the thickness
    /// and direction of the lines are dependent on the action
    /// of the price value.
    type KagiChart() = 
        inherit GenericChart(SeriesChartType.Kagi)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies the data point color that indicates an increasing trend.
        member x.PriceUpColor
            with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

        /// Specifies the reversal amount for the chart.
        member x.ReversalAmount
            with get() = x.GetCustomProperty<string>("ReversalAmount", "3%")
            and set(v) = x.SetCustomProperty<string>("ReversalAmount", v)

        /// <summary>
        ///   Specifies the index of the Y value to use to
        ///   plot the Kagi, Renko, or Three Line Break chart, with
        ///   the first Y value at index 0.
        /// </summary>
        /// <remarks>Any positive integer 0.</remarks>
        member x.UsedYValue
            with get() = x.GetCustomProperty<int>("UsedYValue", 0)
            and set(v) = x.SetCustomProperty<int>("UsedYValue", v)


    /// Illustrates trends in data with the passing of time.
    type LineChart() = 
        inherit GenericChart(SeriesChartType.Line)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies whether marker lines are displayed when rendered in 3D.
        member x.ShowMarkerLines
            with get() = x.GetCustomProperty<bool>("ShowMarkerLines", false)
            and set(v) = x.SetCustomProperty<bool>("ShowMarkerLines", v)


    /// Shows how proportions of data, shown as pie-shaped pieces, contribute to
    /// the data as a whole.
    type PieChart() = 
        inherit GenericChart(SeriesChartType.Pie)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)


        /// <summary>
        ///   Specifies the 3D label line size as a percentage of
        ///   the default size.
        /// </summary>
        /// <remarks>Any integer from 30 to 200.</remarks>
        member x.LabelLineSize3D
            with get() = x.GetCustomProperty<int>("3DLabelLineSize", 100)
            and set(v) = x.SetCustomProperty<int>("3DLabelLineSize", v)

        /// Specifies the color of the collected pie or doughnut slice.
        member x.CollectedColor
            with get() = x.GetCustomProperty<ColorWrapper>("CollectedColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("CollectedColor", ColorWrapper(v))

        /// Specifies the label of the collected pie slice.
        member x.CollectedLabel
            with get() = x.GetCustomProperty<string>("CollectedLabel", "")
            and set(v) = x.SetCustomProperty<string>("CollectedLabel", v)

        /// Specifies the legend text for the collected pie slice.
        member x.CollectedLegendText
            with get() = x.GetCustomProperty<string>("CollectedLegendText", "")
            and set(v) = x.SetCustomProperty<string>("CollectedLegendText", v)

        /// Specifies whether the collected pie slice will be shown as
        /// exploded.
        member x.CollectedSliceExploded
            with get() = x.GetCustomProperty<bool>("CollectedSliceExploded", true)
            and set(v) = x.SetCustomProperty<bool>("CollectedSliceExploded", v)

        /// <summary>
        ///   Specifies the threshold value for collecting small pie slices.
        /// </summary>
        /// <remarks>Any double between 0 and 100 if CollectedThresholdUsePercent is true; otherwise, any double &gt; 0.</remarks>
        member x.CollectedThreshold
            with get() = x.GetCustomProperty<float>("CollectedThreshold", 0.0)
            and set(v) = x.SetCustomProperty<float>("CollectedThreshold", v)

        /// Specifies whether to use the collected threshold value as a
        /// percentage.
        member x.CollectedThresholdUsePercent
            with get() = x.GetCustomProperty<bool>("CollectedThresholdUsePercent", true)
            and set(v) = x.SetCustomProperty<bool>("CollectedThresholdUsePercent", v)

        /// Specifies the tooltip text of the collected pie slice.
        member x.CollectedToolTip
            with get() = x.GetCustomProperty<string>("CollectedToolTip", "")
            and set(v) = x.SetCustomProperty<string>("CollectedToolTip", v)

        /// Specifies whether the Pie or Doughnut data point is exploded.
        member x.Exploded
            with get() = x.GetCustomProperty<bool>("Exploded", false)
            and set(v) = x.SetCustomProperty<bool>("Exploded", v)

        /// <summary>
        ///   Specifies the size of the horizontal segment of the callout
        ///   line.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.LabelsHorizontalLineSize
            with get() = x.GetCustomProperty<int>("LabelsHorizontalLineSize", 1)
            and set(v) = x.SetCustomProperty<int>("LabelsHorizontalLineSize", v)

        /// <summary>
        ///   Specifies the size of the radial segment of the callout
        ///   line.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.LabelsRadialLineSize
            with get() = x.GetCustomProperty<int>("LabelsRadialLineSize", 1)
            and set(v) = x.SetCustomProperty<int>("LabelsRadialLineSize", v)

        /// <summary>
        ///   Specifies the minimum pie or doughnut size.
        /// </summary>
        /// <remarks>Any integer from 10 to 70.</remarks>
        member x.MinimumRelativePieSize
            with get() = x.GetCustomProperty<int>("MinimumRelativePieSize", 30)
            and set(v) = x.SetCustomProperty<int>("MinimumRelativePieSize", v)

        /// Specifies the drawing style of the data points.
        member x.PieDrawingStyle
            with get() = x.GetCustomProperty<PieDrawingStyle>("PieDrawingStyle", PieDrawingStyle.Default)
            and set(v) = x.SetCustomProperty<PieDrawingStyle>("PieDrawingStyle", v)

        /// Specifies the label style of the data points.
        member x.PieLabelStyle
            with get() = x.GetCustomProperty<PieLabelStyle>("PieLabelStyle", PieLabelStyle.Inside)
            and set(v) = x.SetCustomProperty<PieLabelStyle>("PieLabelStyle", v)

        /// Specifies the color of the radial and horizontal segments of
        /// the callout lines.
        member x.PieLineColor
            with get() = x.GetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(Color.Black)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PieLineColor", ColorWrapper(v))

        /// <summary>
        ///   Specifies the angle of the data point in the Pie
        ///   or Doughnut chart.
        /// </summary>
        /// <remarks>Any integer from 0 to 360.</remarks>
        member x.PieStartAngle
            with get() = x.GetCustomProperty<int>("PieStartAngle", 90)
            and set(v) = x.SetCustomProperty<int>("PieStartAngle", v)


    /// Uses points to represent data points.
    type PointChart() = 
        inherit GenericChart(SeriesChartType.Point)
        
        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)


    /// Disregards the passage of time and only displays changes in
    /// prices.
    type PointAndFigureChart() = 
        inherit GenericChart(SeriesChartType.PointAndFigure)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(twoXYSeqWithLabels data Labels)


        /// Specifies the box size in the Renko or Point and
        /// Figure charts.
        member x.BoxSize
            with get() = x.GetCustomProperty<string>("BoxSize", "4%")
            and set(v) = x.SetCustomProperty<string>("BoxSize", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies the data point color that indicates an increasing trend.
        member x.PriceUpColor
            with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

        /// Specifies whether the Point and Figure chart should draw the
        /// X and O values proportionally.
        member x.ProportionalSymbols
            with get() = x.GetCustomProperty<bool>("ProportionalSymbols", true)
            and set(v) = x.SetCustomProperty<bool>("ProportionalSymbols", v)

        /// Specifies the reversal amount for the chart.
        member x.ReversalAmount
            with get() = x.GetCustomProperty<string>("ReversalAmount", "3%")
            and set(v) = x.SetCustomProperty<string>("ReversalAmount", v)

        /// <summary>
        ///   Specifies the index of the Y value to use for
        ///   the high price in the Point and Figure chart, with
        ///   the first Y value at index 0.
        /// </summary>
        /// <remarks>Any positive integer 0.</remarks>
        member x.UsedYValueHigh
            with get() = x.GetCustomProperty<int>("UsedYValueHigh", 0)
            and set(v) = x.SetCustomProperty<int>("UsedYValueHigh", v)

        /// <summary>
        ///   Specifies the index of the Y value to use for
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

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the plot area shape in Radar and Polar charts.
        member x.AreaDrawingStyle
            with get() = x.GetCustomProperty<AreaDrawingStyle>("AreaDrawingStyle", AreaDrawingStyle.Circle)
            and set(v) = x.SetCustomProperty<AreaDrawingStyle>("AreaDrawingStyle", v)

        /// Specifies the text orientation of the axis labels in Radar
        /// and Polar charts.
        member x.CircularLabelStyle
            with get() = x.GetCustomProperty<CircularLabelStyle>("CircularLabelStyle", CircularLabelStyle.Horizontal)
            and set(v) = x.SetCustomProperty<CircularLabelStyle>("CircularLabelStyle", v)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// Specifies the drawing style of the Polar chart type.
        member x.PolarDrawingStyle
            with get() = x.GetCustomProperty<PolarDrawingStyle>("PolarDrawingStyle", PolarDrawingStyle.Line)
            and set(v) = x.SetCustomProperty<PolarDrawingStyle>("PolarDrawingStyle", v)


    /// Displays data that, when combined, equals 100%.
    type PyramidChart() = 
        inherit GenericChart(SeriesChartType.Pyramid)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)


        /// Specifies the line color of the callout for the data
        /// point labels of Funnel or Pyramid charts.
        member x.CalloutLineColor
            with get() = x.GetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("CalloutLineColor", ColorWrapper(v))

        /// Specifies the 3D drawing style of the Pyramid chart type.
        member x.Pyramid3DDrawingStyle
            with get() = x.GetCustomProperty<Pyramid3DDrawingStyle>("Pyramid3DDrawingStyle", Pyramid3DDrawingStyle.SquareBase)
            and set(v) = x.SetCustomProperty<Pyramid3DDrawingStyle>("Pyramid3DDrawingStyle", v)

        /// <summary>
        ///   Specifies the 3D rotation angle of the Pyramid chart.
        /// </summary>
        /// <remarks>Any integer from -10 to 10.</remarks>
        member x.Pyramid3DRotationAngle
            with get() = x.GetCustomProperty<int>("Pyramid3DRotationAngle", 5)
            and set(v) = x.SetCustomProperty<int>("Pyramid3DRotationAngle", v)

        /// Specifies the placement of the data point labels in the
        /// Pyramid chart when they are placed inside the pyramid.
        member x.PyramidInsideLabelAlignment
            with get() = x.GetCustomProperty<PyramidInsideLabelAlignment>("PyramidInsideLabelAlignment", PyramidInsideLabelAlignment.Center)
            and set(v) = x.SetCustomProperty<PyramidInsideLabelAlignment>("PyramidInsideLabelAlignment", v)

        /// Specifies the style of data point labels in the Pyramid
        /// chart.
        member x.PyramidLabelStyle
            with get() = x.GetCustomProperty<PyramidLabelStyle>("PyramidLabelStyle", PyramidLabelStyle.OutsideInColumn)
            and set(v) = x.SetCustomProperty<PyramidLabelStyle>("PyramidLabelStyle", v)

        /// <summary>
        ///   Specifies the minimum height of a data point measured in
        ///   relative coordinates.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
        member x.PyramidMinPointHeight
            with get() = x.GetCustomProperty<int>("PyramidMinPointHeight", 0)
            and set(v) = x.SetCustomProperty<int>("PyramidMinPointHeight", v)

        /// Specifies the placement of the data point labels in the
        /// Pyramid chart when the labels are placed outside the pyramid.
        member x.PyramidOutsideLabelPlacement
            with get() = x.GetCustomProperty<PyramidOutsideLabelPlacement>("PyramidOutsideLabelPlacement", PyramidOutsideLabelPlacement.Right)
            and set(v) = x.SetCustomProperty<PyramidOutsideLabelPlacement>("PyramidOutsideLabelPlacement", v)

        /// <summary>
        ///   Specifies the gap size between the data points, measured in
        ///   relative coordinates.
        /// </summary>
        /// <remarks>Any integer from 0 to 100.</remarks>
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

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the plot area shape in Radar and Polar charts.
        member x.AreaDrawingStyle
            with get() = x.GetCustomProperty<AreaDrawingStyle>("AreaDrawingStyle", AreaDrawingStyle.Circle)
            and set(v) = x.SetCustomProperty<AreaDrawingStyle>("AreaDrawingStyle", v)

        /// Specifies the text orientation of the axis labels in Radar
        /// and Polar charts.
        member x.CircularLabelStyle
            with get() = x.GetCustomProperty<CircularLabelStyle>("CircularLabelStyle", CircularLabelStyle.Horizontal)
            and set(v) = x.SetCustomProperty<CircularLabelStyle>("CircularLabelStyle", v)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// Specifies the drawing style of the Radar chart.
        member x.RadarDrawingStyle
            with get() = x.GetCustomProperty<RadarDrawingStyle>("RadarDrawingStyle", RadarDrawingStyle.Area)
            and set(v) = x.SetCustomProperty<RadarDrawingStyle>("RadarDrawingStyle", v)


    /// Displays a range of data by plotting two Y values per data
    /// point, with each Y value being drawn as a line
    /// chart.
    type RangeChart() = 
        inherit GenericChart(SeriesChartType.Range)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(twoXYSeqWithLabels data Labels)


        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies whether marker lines are displayed when rendered in 3D.
        member x.ShowMarkerLines
            with get() = x.GetCustomProperty<bool>("ShowMarkerLines", false)
            and set(v) = x.SetCustomProperty<bool>("ShowMarkerLines", v)


    /// Displays separate events that have beginning and end values.
    type RangeBarChart() = 
        inherit GenericChart(SeriesChartType.RangeBar)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(twoXYSeqWithLabels data Labels)

        /// Specifies the placement of the data point label.
        member x.BarLabelStyle
            with get() = x.GetCustomProperty<BarLabelStyle>("BarLabelStyle", BarLabelStyle.Outside)
            and set(v) = x.SetCustomProperty<BarLabelStyle>("BarLabelStyle", v)

        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// Specifies whether series of the same chart type are drawn
        /// next to each other instead of overlapping each other.
        member x.DrawSideBySide
            with get() = x.GetCustomProperty<DrawSideBySide>("DrawSideBySide", DrawSideBySide.Auto)
            and set(v) = x.SetCustomProperty<DrawSideBySide>("DrawSideBySide", v)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)


    /// Displays a range of data by plotting two Y values
    /// per data point.
    type RangeColumnChart() = 
        inherit GenericChart(SeriesChartType.RangeColumn)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(twoXYSeqWithLabels data Labels)


        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// Specifies whether series of the same chart type are drawn
        /// next to each other instead of overlapping each other.
        member x.DrawSideBySide
            with get() = x.GetCustomProperty<DrawSideBySide>("DrawSideBySide", DrawSideBySide.Auto)
            and set(v) = x.SetCustomProperty<DrawSideBySide>("DrawSideBySide", v)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)


    /// Displays a series of connecting vertical lines where the thickness
    /// and direction of the lines are dependent on the action
    /// of the price value.
    type RenkoChart() = 
        inherit GenericChart(SeriesChartType.Renko)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the box size in the Renko or Point and
        /// Figure charts.
        member x.BoxSize
            with get() = x.GetCustomProperty<string>("BoxSize", "4%")
            and set(v) = x.SetCustomProperty<string>("BoxSize", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies the data point color that indicates an increasing trend.
        member x.PriceUpColor
            with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

        /// <summary>
        ///   Specifies the index of the Y value to use to
        ///   plot the Kagi, Renko, or Three Line Break chart, with
        ///   the first Y value at index 0.
        /// </summary>
        /// <remarks>Any positive integer 0.</remarks>
        member x.UsedYValue
            with get() = x.GetCustomProperty<int>("UsedYValue", 0)
            and set(v) = x.SetCustomProperty<int>("UsedYValue", v)


    /// A Line chart that plots a fitted curve through each
    /// data point in a series.
    type SplineChart() = 
        inherit GenericChart(SeriesChartType.Spline)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the line tension for the drawing of curves between
        ///   data points.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.LineTension
            with get() = x.GetCustomProperty<float>("LineTension", 0.8)
            and set(v) = x.SetCustomProperty<float>("LineTension", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies whether marker lines are displayed when rendered in 3D.
        member x.ShowMarkerLines
            with get() = x.GetCustomProperty<bool>("ShowMarkerLines", false)
            and set(v) = x.SetCustomProperty<bool>("ShowMarkerLines", v)


    /// An Area chart that plots a fitted curve through each
    /// data point in a series.
    type SplineAreaChart() = 
        inherit GenericChart(SeriesChartType.SplineArea)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the line tension for the drawing of curves between
        ///   data points.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.LineTension
            with get() = x.GetCustomProperty<float>("LineTension", 0.8)
            and set(v) = x.SetCustomProperty<float>("LineTension", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies whether marker lines are displayed when rendered in 3D.
        member x.ShowMarkerLines
            with get() = x.GetCustomProperty<bool>("ShowMarkerLines", false)
            and set(v) = x.SetCustomProperty<bool>("ShowMarkerLines", v)


    /// Displays a range of data by plotting two Y values per
    /// data point, with each Y value drawn as a line
    /// chart.
    type SplineRangeChart() = 
        inherit GenericChart(SeriesChartType.SplineRange)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(twoXYSeqWithLabels data Labels)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the line tension for the drawing of curves between
        ///   data points.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.LineTension
            with get() = x.GetCustomProperty<float>("LineTension", 0.8)
            and set(v) = x.SetCustomProperty<float>("LineTension", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies whether marker lines are displayed when rendered in 3D.
        member x.ShowMarkerLines
            with get() = x.GetCustomProperty<bool>("ShowMarkerLines", false)
            and set(v) = x.SetCustomProperty<bool>("ShowMarkerLines", v)


    /// An Area chart that stacks two or more data series
    /// on top of one another.
    type StackedAreaChart() = 
        inherit GenericChart(SeriesChartType.StackedArea)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)


    /// Displays series of the same chart type as stacked bars.
    type StackedBarChart() = 
        inherit GenericChart(SeriesChartType.StackedBar)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the placement of the data point label.
        member x.BarLabelStyle
            with get() = x.GetCustomProperty<BarLabelStyle>("BarLabelStyle", BarLabelStyle.Outside)
            and set(v) = x.SetCustomProperty<BarLabelStyle>("BarLabelStyle", v)

        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)

        /// Specifies the name of the stacked group.
        member x.StackedGroupName
            with get() = x.GetCustomProperty<string>("StackedGroupName", "")
            and set(v) = x.SetCustomProperty<string>("StackedGroupName", v)


    /// Used to compare the contribution of each value to a
    /// total across categories.
    type StackedColumnChart() = 
        inherit GenericChart(SeriesChartType.StackedColumn)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the drawing style of data points.
        member x.DrawingStyle
            with get() = x.GetCustomProperty<DrawingStyle>("DrawingStyle", DrawingStyle.Default)
            and set(v) = x.SetCustomProperty<DrawingStyle>("DrawingStyle", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)

        /// Specifies the name of the stacked group.
        member x.StackedGroupName
            with get() = x.GetCustomProperty<string>("StackedGroupName", "")
            and set(v) = x.SetCustomProperty<string>("StackedGroupName", v)


    /// Similar to the Line chart type, but uses vertical and
    /// horizontal lines to connect the data points in a series
    /// forming a step-like progression.
    type StepLineChart() = 
        inherit GenericChart(SeriesChartType.StepLine)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// Specifies the value to be used for empty points.
        member x.EmptyPointValue
            with get() = x.GetCustomProperty<EmptyPointValue>("EmptyPointValue", EmptyPointValue.Average)
            and set(v) = x.SetCustomProperty<EmptyPointValue>("EmptyPointValue", v)

        /// Specifies the label position of the data point.
        member x.LabelPosition
            with get() = x.GetCustomProperty<LabelPosition>("LabelStyle", LabelPosition.Auto)
            and set(v) = x.SetCustomProperty<LabelPosition>("LabelStyle", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies whether marker lines are displayed when rendered in 3D.
        member x.ShowMarkerLines
            with get() = x.GetCustomProperty<bool>("ShowMarkerLines", false)
            and set(v) = x.SetCustomProperty<bool>("ShowMarkerLines", v)


    /// Displays significant stock price points including the open, close, high,
    /// and low price points.
    type StockChart() = 
        inherit GenericChart(SeriesChartType.Stock)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(fourXYSeqWithLabels data Labels)

        /// Specifies the Y value to use as the data point
        /// label.
        member x.LabelValueType
            with get() = x.GetCustomProperty<LabelValueType>("LabelValueType", LabelValueType.Close)
            and set(v) = x.SetCustomProperty<LabelValueType>("LabelValueType", v)

        /// <summary>
        ///   Specifies the maximum width of the data point in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MaxPixelPointWidth
            with get() = x.GetCustomProperty<int>("MaxPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MaxPixelPointWidth", v)

        /// <summary>
        ///   Specifies the minimum data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.MinPixelPointWidth
            with get() = x.GetCustomProperty<int>("MinPixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("MinPixelPointWidth", v)

        /// Specifies the marker style for open and close values.
        member x.OpenCloseStyle
            with get() = x.GetCustomProperty<OpenCloseStyle>("OpenCloseStyle", OpenCloseStyle.Line)
            and set(v) = x.SetCustomProperty<OpenCloseStyle>("OpenCloseStyle", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// <summary>
        ///   Specifies the data point width in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointWidth
            with get() = x.GetCustomProperty<int>("PixelPointWidth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointWidth", v)

        /// <summary>
        ///   Specifies the relative data point width.
        /// </summary>
        /// <remarks>Any double from 0 to 2.</remarks>
        member x.PointWidth
            with get() = x.GetCustomProperty<float>("PointWidth", 0.8)
            and set(v) = x.SetCustomProperty<float>("PointWidth", v)

        /// Specifies whether markers for open and close prices are displayed.
        member x.ShowOpenClose
            with get() = x.GetCustomProperty<ShowOpenClose>("ShowOpenClose", ShowOpenClose.Both)
            and set(v) = x.SetCustomProperty<ShowOpenClose>("ShowOpenClose", v)


    /// Displays a series of vertical boxes, or lines, that reflect
    /// changes in price values.
    type ThreeLineBreakChart() = 
        inherit GenericChart(SeriesChartType.ThreeLineBreak)

        /// Adjust the data and other configuration parameters for the chart
        member x.SetData(data,?Labels) = x.SetDataInternal(oneXYSeqWithLabels data Labels)

        /// <summary>
        ///   Specifies the number of lines to use in a Three
        ///   Line Break chart.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.NumberOfLinesInBreak
            with get() = x.GetCustomProperty<int>("NumberOfLinesInBreak", 3)
            and set(v) = x.SetCustomProperty<int>("NumberOfLinesInBreak", v)

        /// <summary>
        ///   Specifies the 3D series depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointDepth
            with get() = x.GetCustomProperty<int>("PixelPointDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointDepth", v)

        /// <summary>
        ///   Specifies the 3D gap depth in pixels.
        /// </summary>
        /// <remarks>Any integer &gt; 0</remarks>
        member x.PixelPointGapDepth
            with get() = x.GetCustomProperty<int>("PixelPointGapDepth", 0)
            and set(v) = x.SetCustomProperty<int>("PixelPointGapDepth", v)

        /// Specifies the data point color that indicates an increasing trend.
        member x.PriceUpColor
            with get() = x.GetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(Color.Empty)).Color
            and set(v) = x.SetCustomProperty<ColorWrapper>("PriceUpColor", ColorWrapper(v))

        /// <summary>
        ///   Specifies the index of the Y value to use to
        ///   plot the Kagi, Renko, or Three Line Break chart, with
        ///   the first Y value at index 0.
        /// </summary>
        /// <remarks>Any positive integer 0.</remarks>
        member x.UsedYValue
            with get() = x.GetCustomProperty<int>("UsedYValue", 0)
            and set(v) = x.SetCustomProperty<int>("UsedYValue", v)

    // ------------------------------------------------------------------------------------
    // Special types of charts - combine multiple series & create row/columns

    type internal CombinedChart(charts:seq<GenericChart>) as this =
        inherit GenericChart(enum<SeriesChartType> -1)

        do
            let firstChart = Seq.head charts
            this.Area <- firstChart.Area
            this.Legend <- firstChart.Legend
            this.Margin <- firstChart.Margin

        member x.Charts = charts

    type internal SubplotChart(charts:seq<GenericChart>, orientation:Orientation) = 
        inherit GenericChart(enum<SeriesChartType> -1)
        let r = 1.0 / (charts |> Seq.length |> float)
        let mutable splitSizes = seq { for c in charts -> r }

        member x.SplitSizes with get() = splitSizes and set v = splitSizes <- v
        member x.Orientation = orientation
        member x.Charts = charts

open ChartFormUtilities
open ChartData.Internal
open ChartData
open ChartStyles
open ChartTypes

type private ChartControl(ch:GenericChart) as self = 
    inherit UserControl()

    let seriesCounter = createCounter()
    let areaCounter = createCounter()
    let legendCounter = createCounter()
    let axisCounter = createCounter()
    let disposeActions = ResizeArray<_>()

    let createTitleChange (chart:Chart) (ch:GenericChart) = 
        ch.TitleChanged.Add(function 
                t ->
                if (chart.Titles.Count > 0) then
                    chart.Titles.[0] <- t
                else
                    chart.Titles.Add t)

    let createLegendChange (chart:Chart) (ch:GenericChart) (series:Series list) = 
        ch.LegendChanged.Add(function 
                l ->
                if (chart.Legends.Count > 0) then
                    chart.Legends.[0] <- l
                else
                    chart.Legends.Add l
                if (chart.ChartAreas.Count > 0) then l.DockedToChartArea <- chart.ChartAreas.[0].Name
                for s in series do (s:Series).Legend <- l.Name)

    let createArea (chart:Chart, ch:GenericChart, ((left, top, right, bottom) as pos)) = 

        let setMargin (area:ChartArea) ((left, top, right, bottom) as pos) = 
            area.Position.X <- left
            area.Position.Y <- top 
            area.Position.Width <- right - left
            area.Position.Height <- bottom - top 

        let area = new ChartArea()
        applyPropertyDefaults ch.ChartType area
        applyPropertyDefaults ch.ChartType area.AxisX
        applyPropertyDefaults ch.ChartType area.AxisY
        applyPropertyDefaults ch.ChartType area.AxisX2
        applyPropertyDefaults ch.ChartType area.AxisY2
        chart.ChartAreas.Add area

        if ch.LazyArea.IsValueCreated then
            applyProperties area ch.Area
            applyProperties area.AxisX ch.Area.AxisX
            applyProperties area.AxisX2 ch.Area.AxisX2
            applyProperties area.AxisY ch.Area.AxisY
            applyProperties area.AxisY2 ch.Area.AxisY2
            if (ch.Area.Area3DStyle.Enable3D) then
                applyProperties area.Area3DStyle ch.Area.Area3DStyle

        area.Name <- 
            if ch.LazyArea.IsValueCreated && not (String.IsNullOrEmpty ch.Area.Name) 
            then ch.Area.Name else sprintf "Area_%d" (areaCounter())
        ch.NameChanged.Add(function
            n ->
                area.Name <- 
                    if not (String.IsNullOrEmpty n) 
                    then n else sprintf "Area_%d" (areaCounter()))

        setMargin area pos
        ch.MarginChanged.Add(function
            a ->                
                let (l, t, r, b) = (0.0f, 0.0f, 100.0f, 100.0f)
                let (ml, mt, mr, mb) = a
                let (l, t, r, b) = (l + float32 ml, t + float32 mt, r - float32 mr, b - float32 mb)
                setMargin area (l, t, r, b))


        let processLegends (charts: seq<GenericChart>) (ch:GenericChart) =
            if charts |> Seq.sumBy (fun c -> c.Legends.Count) > 0 then 
              let legend = new Legend()
              applyPropertyDefaults ch.ChartType legend 
              legend.Name <- sprintf "Legend_%d" (legendCounter())
              legend.DockedToChartArea <- area.Name
              chart.Legends.Add legend
              Some legend
            else
              None

        let processTitles (ch:GenericChart) =
            for title in seq { if ch.LazyTitle.IsValueCreated then yield ch.Title 
                               yield! ch.Titles } do
                chart.Titles.Add title
            createTitleChange chart ch
    
        let processSeries (legendOpt : Legend option) (ch:GenericChart) =
            let name = 
                if not (String.IsNullOrEmpty ch.Name) then ch.Name
                else sprintf "GenericChart_Series_%d" (seriesCounter())
            let series = new Series()
            applyPropertyDefaults ch.ChartType series
            if ch.LazySeries.IsValueCreated then
                applyProperties series ch.Series

            
            let eventHandler = Handler<ChartData>(fun _ data ->  setSeriesData true series data ch.Chart ch.SetCustomProperty)
            disposeActions.Add(fun () -> ch.DataSourceChanged.RemoveHandler eventHandler)
            ch.DataSourceChanged.AddHandler eventHandler
            if String.IsNullOrEmpty series.Name then 
                series.Name <- name
            series.ChartType <- ch.ChartType
            series.ChartArea <- area.Name
            

            chart.Series.Add series
            
            // Set data
            setSeriesData false series ch.Data chart ch.SetCustomProperty

            let cult = System.Threading.Thread.CurrentThread.CurrentCulture
            System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.InvariantCulture
            let props = 
              [ for (KeyValue(k, v)) in ch.CustomProperties -> 
                  sprintf "%s=%s" k (v.ToString()) ]
              |> String.concat ", "
            System.Threading.Thread.CurrentThread.CurrentCulture <- cult
            series.CustomProperties <- props

            ch.CustomPropertyChanged.Add(fun (name, value) -> 
                series.SetCustomProperty(name, System.String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value)))

            match legendOpt with 
            | Some legend -> 
                for leg in ch.Legends do
                  let name = legend.Name
                  applyProperties legend leg
                  legend.Name <- name
                  series.Legend <- name
                createLegendChange chart ch [series]
            | None -> ()
            
            series


        match ch with
        | :? CombinedChart as cch ->
            let legend = processLegends cch.Charts ch

            let series = 
                [ for c in cch.Charts do
                    let s = processSeries legend c
                    processTitles c
                    yield s ]
            
            processTitles cch
            series

        | c ->
            let legend = processLegends [ c ] ch
            let series = processSeries legend c
            processTitles c
            [ series ]

    let chart =
        let chart = new Chart()
        applyPropertyDefaults ch.ChartType chart
        self.Controls.Add chart

        let rec loop (subChart:GenericChart) (l, t, r, b) = 
            let (ml, mt, mr, mb) = subChart.Margin
            let (l, t, r, b) = (l + float32 ml, t + float32 mt, r - float32 mr, b - float32 mb)
            match subChart with
            | :? SubplotChart as subplot ->
                for title in subChart.Titles do
                    chart.Titles.Add title
                createTitleChange chart subChart
        
                let total = subplot.SplitSizes |> Seq.sum
                let available = if subplot.Orientation = Orientation.Vertical then b - t else r - l
                let k = float available / total

                let offs = ref 0.0f
                let series = 
                    [ for ch, siz in Seq.zip subplot.Charts subplot.SplitSizes do
                        if subplot.Orientation = Orientation.Vertical then
                          yield! loop ch (l, t + !offs, r, t + !offs + float32 (siz * k))
                        else
                          yield! loop ch (l + !offs, t, l + !offs + float32 (siz * k), b)
                        offs := !offs + float32 (siz * k) ]

                for leg in subChart.Chart.Legends do
                    let legend = new Legend() 
                    applyPropertyDefaults subChart.ChartType legend 
                    applyProperties legend leg
                    legend.Name <- sprintf "Legend_%d" (legendCounter())
                    //legend.DockedToChartArea <- area.Name
                    chart.Legends.Add legend
                    for s in series do (s:Series).Legend <- legend.Name
                createLegendChange chart subChart series

                series
            | _ ->
              createArea (chart, subChart, (l, t, r, b))

        let _series = loop ch (0.0f, 0.0f, 100.0f, 100.0f)
        if ch.LazyChart.IsValueCreated then 
            applyProperties chart ch.Chart
        chart.Dock <- DockStyle.Fill
        ch.Chart <- chart

        chart
  
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
          ch.CopyChartToClipboard())

      miCopyEmf.Click.Add(fun _ ->
          ch.CopyChartToClipboardEmf(self))

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

    override __.Dispose(disposing) = 
        base.Dispose(disposing)
        let actions = disposeActions.ToArray()
        disposeActions.Clear()
        for a in actions do 
            a()
    
type StyleHelper =

    static member Font(?Family:string, ?Size:float, ?Style:FontStyle) =
        let fontSize = 
            match Size with
            | Some(size) -> float32 size
            | None -> DefaultFontForOthers.Size
        let fontStyle = 
            match Style with
            | Some(style) -> style
            | None -> DefaultFontForOthers.Style
        let font =
            match Family with
            | Some name -> new Font(name, fontSize, fontStyle)
            | None -> new Font(DefaultFontForOthers.FontFamily, fontSize, fontStyle)
        font

    static member OptionalFont(?Family:string, ?Size:float, ?Style:FontStyle) =
        match Family, Size, Style with 
        | None,None,None -> None
        | _ -> Some (StyleHelper.Font(?Family=Family,?Size=Size,?Style=Style))

    static member Legend( ?Title, ?Background, ?Alignment, ?Docking, ?InsideArea,?BorderColor, ?BorderWidth, ?BorderDashStyle:DashStyle, ?FontName:string, ?FontStyle:FontStyle, ?FontSize:float) =
          let legend = new Legend()
          InsideArea |> Option.iter legend.set_IsDockedInsideChartArea
          Background |> Option.iter (applyBackground legend)
          Alignment |> Option.iter legend.set_Alignment
          Docking |> Option.iter legend.set_Docking
          Title |> Option.iter legend.set_Title
          BorderColor |> Option.iter legend.set_BorderColor
          BorderDashStyle |> Option.iter (int >> enum >> legend.set_BorderDashStyle)
          BorderWidth |> Option.iter legend.set_BorderWidth
          StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) |> Option.iter legend.set_Font 
          legend



type LabelStyle
        ( ?Angle, ?Color, ?Format, ?Interval, ?IntervalOffset, ?IntervalOffsetType:DateTimeIntervalType, ?IntervalType:DateTimeIntervalType, ?IsEndLabelVisible, ?IsStaggered, ?TruncatedLabels,
          ?FontName:string, ?FontStyle:FontStyle, ?FontSize:float) =
       let labelStyle = new System.Windows.Forms.DataVisualization.Charting.LabelStyle()
       do
          Angle |> Option.iter labelStyle.set_Angle
          Color |> Option.iter labelStyle.set_ForeColor
          Format |> Option.iter labelStyle.set_Format
          Interval |> Option.iter labelStyle.set_Interval
          IntervalOffset |> Option.iter labelStyle.set_IntervalOffset
          IntervalOffsetType |> Option.iter (int >> enum >> labelStyle.set_IntervalOffsetType)
          IntervalType |> Option.iter (int >> enum >> labelStyle.set_IntervalType)
          IsStaggered |> Option.iter labelStyle.set_IsStaggered
          TruncatedLabels |> Option.iter labelStyle.set_TruncatedLabels
          StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) |> Option.iter labelStyle.set_Font 
       member internal x.Style = labelStyle 
       static member Create(?Angle, ?Color, ?Format, ?Interval, ?IntervalOffset, ?IntervalOffsetType, ?IntervalType, ?IsEndLabelVisible, ?IsStaggered, ?TruncatedLabels,?FontName:string, ?FontStyle:FontStyle, ?FontSize:float) =
        LabelStyle( ?Angle=Angle, ?Color=Color,?Format=Format, ?Interval=Interval, ?IntervalOffset=IntervalOffset,?IntervalOffsetType=IntervalOffsetType, ?IntervalType=IntervalType, ?IsEndLabelVisible=IsEndLabelVisible, ?IsStaggered=IsStaggered, ?TruncatedLabels=TruncatedLabels,?FontName=FontName, ?FontStyle=FontStyle, ?FontSize=FontSize)


[<Sealed>]
type Grid( ?Enabled, ?Interval, ?IntervalOffset, ?IntervalOffsetType, ?LineColor, ?LineDashStyle, ?LineWidth) = 
       let grid = new System.Windows.Forms.DataVisualization.Charting.Grid()
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
       let tickMark = new System.Windows.Forms.DataVisualization.Charting.TickMark()
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
          ?AreaBackground,
          ?Name,?Margin,?Background,
          // ?AlignWithChartArea, ?AlignmentOrientation, ?AlignmentStyle,
          ?Enable3D, ?Area3DInclination, ?Area3DIsClustered, ?Area3DIsRightAngleAxes, ?Area3DLightStyle:LightStyle, ?Area3DPerspective, ?Area3DPointDepth, ?Area3DPointGapDepth, ?Area3DRotation, ?Area3DWallWidth,
          ?YAxisType, ?XAxisType,
          ?Color, ?BorderColor, ?BorderWidth,
          ?DataPointLabel, ?DataPointLabelToolTip, ?DataPointToolTip,
          ?MarkerColor, ?MarkerSize, ?MarkerStep, ?MarkerStyle:MarkerStyle, ?MarkerBorderColor, ?MarkerBorderWidth,
          ?Legend,?LegendTitle, ?LegendBackground, ?LegendFont, ?LegendAlignment, ?LegendDocking:Docking, ?LegendInsideArea,
          ?LegendTitleAlignment, ?LegendTitleFont, ?LegendTitleForeColor, ?LegendBorderColor, ?LegendBorderWidth, ?LegendBorderDashStyle:DashStyle,
          ?Title, ?TitleStyle:TextStyle, ?TitleFont, ?TitleBackground, ?TitleColor, ?TitleBorderColor, ?TitleBorderWidth, ?TitleBorderDashStyle:DashStyle, 
          ?TitleOrientation:TextOrientation, ?TitleAlignment, ?TitleDocking:Docking, ?TitleInsideArea) =
          (fun (ch:('T :> GenericChart)) -> 
          
            let convAxisEnabled = function true -> AxisEnabled.True | false -> AxisEnabled.False
            //ch.Area.AxisX <- new Axis(null, AxisName.X)
            let configureAxis (ax:Axis) (vEnabled,vIsLogarithmic,vArrowStyle:AxisArrowStyle option,vLabelStyle,vIsMarginVisible,vMaximum,vMinimum,vMajorGrid,vMinorGrid,vMajorTickMark,vMinorTickMark,vName,vTitle,vTitleAlignment,vTitleFont,vTitleForeColor,vToolTip) = 
                applyPropertyDefaults ch.ChartType ax
                vArrowStyle |> Option.iter (int >> enum >> ax.set_ArrowStyle)
                vEnabled |> Option.iter (convAxisEnabled >> ax.set_Enabled)
                vLabelStyle |> Option.iter (fun (labelStyle:LabelStyle) -> ax.set_LabelStyle labelStyle.Style)
                vIsMarginVisible |> Option.iter ax.set_IsMarginVisible
                vIsLogarithmic |> Option.iter ax.set_IsLogarithmic
                vMaximum |> Option.iter ax.set_Maximum
                vMinimum |> Option.iter ax.set_Minimum
                vMajorGrid |> Option.iter (fun (c:Grid) -> ax.set_MajorGrid c.Handle)
                vMinorGrid |> Option.iter (fun (c:Grid) -> ax.set_MinorGrid c.Handle)
                vMajorTickMark |> Option.iter (fun (c:TickMark) -> ax.set_MajorTickMark c.Handle)
                vMinorTickMark |> Option.iter (fun (c:TickMark) -> ax.set_MinorTickMark c.Handle)
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

            configureAxis ch.Area.AxisX (AxisXEnabled,AxisXLogarithmic,AxisXArrowStyle,AxisXLabelStyle,AxisXIsMarginVisible,AxisXMaximum,AxisXMinimum,AxisXMajorGrid,AxisXMinorGrid,AxisXMajorTickMark,AxisXMinorTickMark,AxisXName,AxisXTitle,AxisXTitleAlignment,AxisXTitleFont,AxisXTitleForeColor,AxisXToolTip) 
            configureAxis ch.Area.AxisY (AxisYEnabled,AxisYLogarithmic,AxisYArrowStyle,AxisYLabelStyle,AxisYIsMarginVisible,AxisYMaximum,AxisYMinimum,AxisYMajorGrid,AxisYMinorGrid,AxisYMajorTickMark,AxisYMinorTickMark,AxisYName,AxisYTitle,AxisYTitleAlignment,AxisYTitleFont,AxisYTitleForeColor,AxisYToolTip) 
            configureAxis ch.Area.AxisX2 (AxisX2Enabled,AxisX2Logarithmic,AxisX2ArrowStyle,AxisX2LabelStyle,AxisX2IsMarginVisible,AxisX2Maximum,AxisX2Minimum,AxisX2MajorGrid,AxisX2MinorGrid,AxisX2MajorTickMark,AxisX2MinorTickMark,AxisX2Name,AxisX2Title,AxisX2TitleAlignment,AxisX2TitleFont,AxisX2TitleForeColor,AxisX2ToolTip) 
            configureAxis ch.Area.AxisY2 (AxisY2Enabled,AxisY2Logarithmic,AxisY2ArrowStyle,AxisY2LabelStyle,AxisY2IsMarginVisible,AxisY2Maximum,AxisY2Minimum,AxisY2MajorGrid,AxisY2MinorGrid,AxisY2MajorTickMark,AxisY2MinorTickMark,AxisY2Name,AxisY2Title,AxisY2TitleAlignment,AxisY2TitleFont,AxisY2TitleForeColor,AxisY2ToolTip) 

            AreaBackground |> Option.iter (applyBackground ch.Area)

            //Name |> Option.iter ch.Area.set_Name
            Name |> Option.iter ch.Series.set_Name
            

            // These are omitted
            //AlignWithChartArea |> Option.iter ch.Area.set_AlignWithChartArea
            //AlignmentStyle |> Option.iter ch.Area.set_AlignmentStyle
            //AlignmentOrientation |> Option.iter ch.Area.set_AlignmentOrientation

            Enable3D |> Option.iter ch.Area.Area3DStyle.set_Enable3D
            Area3DInclination |> Option.iter ch.Area.Area3DStyle.set_Inclination
            Area3DIsClustered |> Option.iter ch.Area.Area3DStyle.set_IsClustered
            Area3DIsRightAngleAxes |> Option.iter ch.Area.Area3DStyle.set_IsRightAngleAxes
            Area3DLightStyle |> Option.iter (int >> enum >> ch.Area.Area3DStyle.set_LightStyle)
            Area3DPerspective |> Option.iter ch.Area.Area3DStyle.set_Perspective
            Area3DPointDepth |> Option.iter ch.Area.Area3DStyle.set_PointDepth
            Area3DPointGapDepth |> Option.iter ch.Area.Area3DStyle.set_PointGapDepth
            Area3DRotation |> Option.iter ch.Area.Area3DStyle.set_Rotation
            Area3DWallWidth |> Option.iter ch.Area.Area3DStyle.set_WallWidth

            YAxisType |> Option.iter ch.Series.set_YAxisType
            XAxisType |> Option.iter ch.Series.set_XAxisType
  
            Color |> Option.iter ch.Series.set_Color
            BorderColor |> Option.iter ch.Series.set_BorderColor
            BorderWidth |> Option.iter ch.Series.set_BorderWidth

            DataPointLabel |> Option.iter ch.Series.set_Label
            DataPointLabelToolTip |> Option.iter ch.Series.set_LabelToolTip
            DataPointToolTip |> Option.iter ch.Series.set_ToolTip

            MarkerBorderColor |> Option.iter ch.Series.set_MarkerBorderColor
            MarkerBorderWidth |> Option.iter ch.Series.set_MarkerBorderWidth
            MarkerColor |> Option.iter ch.Series.set_MarkerColor
            MarkerSize |> Option.iter ch.Series.set_MarkerSize
            MarkerStep |> Option.iter ch.Series.set_MarkerStep
            MarkerStyle |> Option.iter (int >> enum >> ch.Series.set_MarkerStyle)

            let hasLegend = not (String.IsNullOrEmpty ch.Series.Name) && Legend <> Some false 
            if hasLegend then 
                let legend = new Legend()
                applyPropertyDefaults ch.ChartType legend
                LegendInsideArea |> Option.iter legend.set_IsDockedInsideChartArea
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
                ch.Legends.Add legend

            
            Margin |> Option.iter (fun margin -> ch.Margin <- margin) 

            Background |> Option.iter (applyBackground ch.Chart)


            if Title.IsSome then 
                let title = new Title()
                applyPropertyDefaults ch.ChartType title
                Title |> Option.iter title.set_Text 
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
                ch.Titles.Add title
            ch)

/// Provides a set of static methods for creating charts.
type Chart =
    /// Displays multiple series of data as stacked areas. The cumulative proportion
    /// of each stacked element is always 100% of the Y axis.
    static member StackedArea100(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, (fun () -> StackedArea100Chart()))

(*
    /// Displays multiple series of data as stacked areas. The cumulative proportion
    /// of each stacked element is always 100% of the Y axis.
    static member StackedArea100<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<StackedArea100Chart>(oneXYObs (defaultArg MaxPoints -1) data)

*)

    /// Displays multiple series of data as stacked bars. The cumulative
    /// proportion of each stacked element is always 100% of the Y axis.
    static member StackedBar100(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> StackedBar100Chart())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Displays multiple series of data as stacked bars. The cumulative
    /// proportion of each stacked element is always 100% of the Y axis.
    static member StackedBar100<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<StackedBar100Chart>(oneXYObs (defaultArg MaxPoints -1) data)

*)

    /// Displays multiple series of data as stacked columns. The cumulative
    /// proportion of each stacked element is always 100% of the Y axis.
    static member StackedColumn100(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> StackedColumn100Chart())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Displays multiple series of data as stacked columns. The cumulative
    /// proportion of each stacked element is always 100% of the Y axis.
    static member StackedColumn100<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<StackedColumn100Chart>(oneXYObs (defaultArg MaxPoints -1) data)

*)

    /// Emphasizes the degree of change over time and shows the
    /// relationship of the parts to a whole.
    static member Area(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> AreaChart ())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Emphasizes the degree of change over time and shows the
    /// relationship of the parts to a whole.
    static member Area<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<AreaChart>(oneXYObs (defaultArg MaxPoints -1) data)

*)

    /// Illustrates comparisons among individual items.
    static member Bar(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> BarChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Illustrates comparisons among individual items.
    static member Bar<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<BarChart>(oneXYObs (defaultArg MaxPoints -1) data)

*)
    /// Consists of one or more box symbols that summarize the
    /// distribution of the data within one or more data sets.
    static member private ConfigureBoxPlot(c:BoxPlotChart,vPercentile,vShowAverage,vShowMedian,vShowUnusualValues,vWhiskerPercentile) = 
        vPercentile |> Option.iter c.set_Percentile
        vShowAverage |> Option.iter c.set_ShowAverage
        vShowMedian |> Option.iter c.set_ShowMedian
        vShowUnusualValues |> Option.iter c.set_ShowUnusualValues
        vWhiskerPercentile |> Option.iter c.set_WhiskerPercentile

    /// Consists of one or more box symbols that summarize the
    /// distribution of the data within one or more data sets.
    static member BoxPlot(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle,?Percentile,?ShowAverage,?ShowMedian,?ShowUnusualValues,?WhiskerPercentile) = 
        let c = 
         GenericChart.Create<BoxPlotChart>(sixY data, (fun () -> BoxPlotChart ())) 
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
        Chart.ConfigureBoxPlot(c,Percentile,ShowAverage,ShowMedian,ShowUnusualValues,WhiskerPercentile)
        c




(*
    /// Consists of one or more box symbols that summarize the
    /// distribution of the data within one or more data sets.
    static member BoxPlot(data:seq<'TYValue[]>) =
        GenericChart.Create<BoxPlotChart>(sixYArrBox data)
*)

    /// Consists of one or more box symbols that summarize the
    /// distribution of the data within one or more data sets.
    static member BoxPlot(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle,?Percentile,?ShowAverage,?ShowMedian,?ShowUnusualValues,?WhiskerPercentile) = 
        let c = 
         GenericChart.Create<BoxPlotChart>(sixXYArrBoxWithLabels data Labels, fun () -> BoxPlotChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
        Chart.ConfigureBoxPlot(c,Percentile,ShowAverage,ShowMedian,ShowUnusualValues,WhiskerPercentile)
        c

    /// A variation of the Point chart type, where the data
    /// points are replaced by bubbles of different sizes.
    static member Bubble(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(twoXYSeqWithLabels data Labels, fun () -> BubbleChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// A variation of the Point chart type, where the data
    /// points are replaced by bubbles of different sizes.
    static member Bubble<'TX, 'TY1, 'TY2 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2)>, ?MaxPoints) = 
        GenericChart.Create<BubbleChart>(twoXYObs (defaultArg MaxPoints -1) data)
*)

    /// Used to display stock information using high, low, open and
    /// close values.
    static member Candlestick(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(fourXYSeqWithLabels data Labels, fun() -> CandlestickChart() )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Used to display stock information using high, low, open and
    /// close values.
    static member Candlestick<'TX, 'TY1, 'TY2, 'TY3, 'TY4 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TY3 :> IConvertible and 'TY4 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2 * 'TY3 * 'TY4)>, ?MaxPoints) = 
        GenericChart.Create<CandlestickChart>(fourXYObs (defaultArg MaxPoints -1) data)

*)


    /// Uses a sequence of columns to compare values across categories.
    static member Column(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> ColumnChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Uses a sequence of columns to compare values across categories.
    static member Column<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<ColumnChart>(oneXYObs (defaultArg MaxPoints -1) data)

*)


    /// Similar to the Pie chart type, except that it has
    /// a hole in the center.
    static member Doughnut(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> DoughnutChart ())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Similar to the Pie chart type, except that it has
    /// a hole in the center.
    static member Doughnut<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<DoughnutChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)
(*
    /// Consists of lines with markers that are used to display
    /// statistical information about the data displayed in a graph.
    static member ErrorBar<'TY1, 'TY2, 'TY3 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TY3 :> IConvertible>(data: seq<'TY1 * 'TY2 * 'TY3>) = 
        GenericChart.Create<ErrorBarChart>(threeY data)
*)
    /// Consists of lines with markers that are used to display
    /// statistical information about the data displayed in a graph.
    static member ErrorBar(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(threeXYSeqWithLabels data Labels, fun () -> ErrorBarChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Consists of lines with markers that are used to display
    /// statistical information about the data displayed in a graph.
    static member ErrorBar<'TX, 'TY1, 'TY2, 'TY3 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TY3 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2 * 'TY3)>, ?MaxPoints) = 
        GenericChart.Create<ErrorBarChart>(threeXYObs (defaultArg MaxPoints -1) data)

*)

    /// A variation of the Line chart that significantly reduces the
    /// drawing time of a series that contains a very large
    /// number of data points.
    static member FastLine(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> FastLineChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// A variation of the Line chart that significantly reduces the
    /// drawing time of a series that contains a very large
    /// number of data points.
    static member FastLine<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<FastLineChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)

    /// A variation of the Point chart type that significantly reduces
    /// the drawing time of a series that contains a very
    /// large number of data points.
    static member FastPoint(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> FastPointChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// A variation of the Point chart type that significantly reduces
    /// the drawing time of a series that contains a very
    /// large number of data points.
    static member FastPoint<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<FastPointChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)

    /// Displays in a funnel shape data that equals 100% when
    /// totaled.
    static member Funnel(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> FunnelChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Displays in a funnel shape data that equals 100% when
    /// totaled.
    static member Funnel<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<FunnelChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)

    /// Displays a series of connecting vertical lines where the thickness
    /// and direction of the lines are dependent on the action
    /// of the price value.
    static member Kagi(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> KagiChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Displays a series of connecting vertical lines where the thickness
    /// and direction of the lines are dependent on the action
    /// of the price value.
    static member Kagi<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<KagiChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)

    /// Illustrates trends in data with the passing of time.
    static member Line(data:seq<_ * _>,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> LineChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Illustrates trends in data with the passing of time.
    static member Line<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<LineChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)


    /// Shows how proportions of data, shown as pie-shaped pieces, contribute to
    /// the data as a whole.
    static member Pie(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> PieChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Shows how proportions of data, shown as pie-shaped pieces, contribute to
    /// the data as a whole.
    static member Pie<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<PieChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)


    /// Uses points to represent data points.
    static member Point(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle,?MarkerColor,?MarkerSize) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> PointChart ())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle,?MarkerColor=MarkerColor,?MarkerSize=MarkerSize)

(*
    /// Uses points to represent data points.
    static member Point<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<PointChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)


    /// Disregards the passage of time and only displays changes in
    /// prices.
    static member PointAndFigure(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(twoXYSeqWithLabels data Labels, fun () -> PointAndFigureChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Disregards the passage of time and only displays changes in
    /// prices.
    static member PointAndFigure<'TX, 'TY1, 'TY2 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2)>, ?MaxPoints) = 
        GenericChart.Create<PointAndFigureChart>(twoXYObs (defaultArg MaxPoints -1) data)
*)


    /// A circular graph on which data points are displayed using
    /// the angle, and the distance from the center point.
    static member Polar(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> PolarChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// A circular graph on which data points are displayed using
    /// the angle, and the distance from the center point.
    static member Polar<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<PolarChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)


    /// Displays data that, when combined, equals 100%.
    static member Pyramid(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> PyramidChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Displays data that, when combined, equals 100%.
    static member Pyramid<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<PyramidChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)

    /// A circular chart that is used primarily as a data
    /// comparison tool.
    static member Radar(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> RadarChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// A circular chart that is used primarily as a data
    /// comparison tool.
    static member Radar<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<RadarChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)


    /// Displays a range of data by plotting two Y values per data
    /// point, with each Y value being drawn as a line
    /// chart.
    static member Range(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(twoXYSeqWithLabels data Labels, fun () -> RangeChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Displays a range of data by plotting two Y values per data
    /// point, with each Y value being drawn as a line
    /// chart.
    static member Range<'TX, 'TY1, 'TY2 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2)>, ?MaxPoints) = 
        GenericChart.Create<RangeChart>(twoXYObs (defaultArg MaxPoints -1) data)
*)

    /// Displays separate events that have beginning and end values.
    static member RangeBar(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(twoXYSeqWithLabels data Labels, fun () -> RangeBarChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Displays separate events that have beginning and end values.
    static member RangeBar<'TX, 'TY1, 'TY2 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2)>, ?MaxPoints) = 
        GenericChart.Create<RangeBarChart>(twoXYObs (defaultArg MaxPoints -1) data)
*)



    /// Displays a range of data by plotting two Y values
    /// per data point.
    static member RangeColumn(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(twoXYSeqWithLabels data Labels, fun () -> RangeColumnChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Displays a range of data by plotting two Y values
    /// per data point.
    static member RangeColumn<'TX, 'TY1, 'TY2 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2)>, ?MaxPoints) = 
        GenericChart.Create<RangeColumnChart>(twoXYObs (defaultArg MaxPoints -1) data)
*)


    /// Displays a series of connecting vertical lines where the thickness
    /// and direction of the lines are dependent on the action
    /// of the price value.
    static member Renko(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> RenkoChart ())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Displays a series of connecting vertical lines where the thickness
    /// and direction of the lines are dependent on the action
    /// of the price value.
    static member Renko<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<RenkoChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)



    /// A Line chart that plots a fitted curve through each
    /// data point in a series.
    static member Spline(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> SplineChart ())
(*
    /// A Line chart that plots a fitted curve through each
    /// data point in a series.
    static member Spline<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<SplineChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)


    /// An Area chart that plots a fitted curve through each
    /// data point in a series.
    static member SplineArea(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> SplineAreaChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// An Area chart that plots a fitted curve through each
    /// data point in a series.
    static member SplineArea<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<SplineAreaChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)



    /// Displays a range of data by plotting two Y values per
    /// data point, with each Y value drawn as a line
    /// chart.
    static member SplineRange(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(twoXYSeqWithLabels data Labels, fun () -> SplineRangeChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

(*
    /// Displays a range of data by plotting two Y values per
    /// data point, with each Y value drawn as a line
    /// chart.
    static member SplineRange<'TX, 'TY1, 'TY2 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2)>, ?MaxPoints) = 
        GenericChart.Create<SplineRangeChart>(twoXYObs (defaultArg MaxPoints -1) data)
*)


    /// An Area chart that stacks two or more data series
    /// on top of one another.
    static member StackedArea(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> StackedAreaChart ())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// An Area chart that stacks two or more data series
    /// on top of one another.
    static member StackedArea<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<StackedAreaChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)



    /// Displays series of the same chart type as stacked bars.
    static member StackedBar(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> StackedBarChart ())
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Displays series of the same chart type as stacked bars.
    static member StackedBar<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<StackedBarChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)



    /// Used to compare the contribution of each value to a
    /// total across categories.
    static member StackedColumn(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> StackedColumnChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Used to compare the contribution of each value to a
    /// total across categories.
    static member StackedColumn<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<StackedColumnChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)



    /// Similar to the Line chart type, but uses vertical and
    /// horizontal lines to connect the data points in a series
    /// forming a step-like progression.
    static member StepLine(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> StepLineChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Similar to the Line chart type, but uses vertical and
    /// horizontal lines to connect the data points in a series
    /// forming a step-like progression.
    static member StepLine<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<StepLineChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)



    /// Displays significant stock price points including the open, close, high,
    /// and low price points.
    static member Stock(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(fourXYSeqWithLabels data Labels, fun () -> StockChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Displays significant stock price points including the open, close, high,
    /// and low price points.
    static member Stock<'TX, 'TY1, 'TY2, 'TY3, 'TY4 when 'TY1 :> IConvertible and 'TY2 :> IConvertible and 'TY3 :> IConvertible and 'TY4 :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY1 * 'TY2 * 'TY3 * 'TY4)>, ?MaxPoints) = 
        GenericChart.Create<StockChart>(fourXYObs (defaultArg MaxPoints -1) data)
*)



    /// Displays a series of vertical boxes, or lines, that reflect
    /// changes in price values.
    static member ThreeLineBreak(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(oneXYSeqWithLabels data Labels, fun () -> ThreeLineBreakChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)
(*
    /// Displays a series of vertical boxes, or lines, that reflect
    /// changes in price values.
    static member ThreeLineBreak<'TX, 'TY when 'TY :> IConvertible and 'TX :> IConvertible>(data:IObservable<'TX * ('TY)>, ?MaxPoints) = 
        GenericChart.Create<ThreeLineBreakChart>(oneXYObs (defaultArg MaxPoints -1) data)
*)


// --------------------------------------------------------------------------------------
// Inclusions for multiple series for Stacked charts
// --------------------------------------------------------------------------------------

    /// Displays series of the same chart type as stacked bars.
    static member StackedBar(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(seqXY data, fun () -> StackedBarChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

    /// Displays series of the same chart type as stacked bars.
    static member StackedBar100(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(seqXY data, fun () -> StackedBar100Chart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

    /// Displays series of the same chart type as stacked columns.
    static member StackedColumn(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(seqXY data, fun () -> StackedColumnChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

    /// Displays series of the same chart type as stacked columns.
    static member StackedColumn100(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(seqXY data, fun () -> StackedColumn100Chart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

    /// Displays series of the same chart type as stacked areas.
    static member StackedArea(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(seqXY data, fun () -> StackedAreaChart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

    /// Displays series of the same chart type as stacked areas.
    static member StackedArea100(data,?Name,?Title,?Labels,?Color,?Legend,?Enable3D,?XTitle,?YTitle) = 
        GenericChart.Create(seqXY data, fun () -> StackedArea100Chart () )
         |> Helpers.ApplyStyles(?Name=Name,?Title=Title,?Color=Color,?Legend=Legend,?Enable3D=Enable3D,?AxisXTitle=XTitle,?AxisYTitle=YTitle)

    /// Create a combined chart with the given charts placed in rows
    static member Rows charts = 
      SubplotChart(charts, Orientation.Vertical) :> GenericChart

    /// Create a combined chart with the given charts placed in columns
    static member Columns charts = 
      SubplotChart(charts, Orientation.Horizontal) :> GenericChart

    /// Create a combined chart with the given charts merged
    static member Combine charts = 
      CombinedChart charts :> GenericChart


[<AutoOpen>]
module ChartStyleExtensions = 

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
        /// Set up the display parameters of the X Axis
        member ch.AndXAxis
            (?Enabled, ?Title, ?Maximum, ?Minimum, ?Logarithmic, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, ?Name,
             ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

            let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
            ch |> Helpers.ApplyStyles(?AxisXLogarithmic=Logarithmic,?AxisXEnabled=Enabled, ?AxisXArrowStyle=ArrowStyle, ?AxisXLabelStyle=LabelStyle, ?AxisXIsMarginVisible=IsMarginVisible, ?AxisXMaximum=Maximum, ?AxisXMinimum=Minimum, ?AxisXMajorGrid=MajorGrid, ?AxisXMinorGrid=MinorGrid, ?AxisXMajorTickMark=MajorTickMark, ?AxisXMinorTickMark=MinorTickMark, ?AxisXName=Name, ?AxisXTitle=Title, ?AxisXTitleAlignment=TitleAlignment, ?AxisXTitleFont=titleFont, ?AxisXTitleForeColor=TitleColor, ?AxisXToolTip=ToolTip) 

        /// Set up the display parameters of the Y Axis
        member ch.AndYAxis
            (?Enabled, ?Title, ?Maximum, ?Minimum, ?Logarithmic, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, ?Name,
             ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

            let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
            ch |> Helpers.ApplyStyles(?AxisYLogarithmic=Logarithmic,?AxisYEnabled=Enabled,?AxisYArrowStyle=ArrowStyle,  ?AxisYLabelStyle=LabelStyle, ?AxisYIsMarginVisible=IsMarginVisible, ?AxisYMaximum=Maximum, ?AxisYMinimum=Minimum, ?AxisYMajorGrid=MajorGrid, ?AxisYMinorGrid=MinorGrid, ?AxisYMajorTickMark=MajorTickMark, ?AxisYMinorTickMark=MinorTickMark, ?AxisYName=Name, ?AxisYTitle=Title, ?AxisYTitleAlignment=TitleAlignment, ?AxisYTitleFont=titleFont, ?AxisYTitleForeColor=TitleColor, ?AxisYToolTip=ToolTip) 

        /// Set up the display parameters of the second X Axis
        member ch.AndXAxis2
            (?Enabled, ?Title, ?Maximum, ?Minimum, ?Logarithmic, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, ?Name,
             ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

            let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
            ch |> Helpers.ApplyStyles(?AxisX2Logarithmic=Logarithmic,?AxisX2Enabled=Enabled, ?AxisX2ArrowStyle=ArrowStyle, ?AxisX2LabelStyle=LabelStyle, ?AxisX2IsMarginVisible=IsMarginVisible, ?AxisX2Maximum=Maximum, ?AxisX2Minimum=Minimum, ?AxisX2MajorGrid=MajorGrid, ?AxisX2MinorGrid=MinorGrid, ?AxisX2MajorTickMark=MajorTickMark, ?AxisX2MinorTickMark=MinorTickMark, ?AxisX2Name=Name, ?AxisX2Title=Title, ?AxisX2TitleAlignment=TitleAlignment, ?AxisX2TitleFont=titleFont, ?AxisX2TitleForeColor=TitleColor, ?AxisX2ToolTip=ToolTip) 

        /// Set up the display parameters of the second Y Axis
        member ch.AndYAxis2
            (?Enabled, ?Title, ?Maximum, ?Minimum, ?Logarithmic, ?ArrowStyle:AxisArrowStyle, ?LabelStyle:LabelStyle, ?IsMarginVisible, ?MajorGrid:Grid, ?MinorGrid:Grid, ?MajorTickMark:TickMark, ?MinorTickMark:TickMark, ?Name,
             ?TitleAlignment, ?TitleFontName, ?TitleFontSize, ?TitleFontStyle, ?TitleColor, ?ToolTip) =

            let titleFont = StyleHelper.OptionalFont(?Family=TitleFontName, ?Size=TitleFontSize, ?Style=TitleFontStyle) 
            ch |> Helpers.ApplyStyles(?AxisY2Logarithmic=Logarithmic,?AxisY2Enabled=Enabled, ?AxisY2ArrowStyle=ArrowStyle, ?AxisY2LabelStyle=LabelStyle, ?AxisY2IsMarginVisible=IsMarginVisible, ?AxisY2Maximum=Maximum, ?AxisY2Minimum=Minimum, ?AxisY2MajorGrid=MajorGrid, ?AxisY2MinorGrid=MinorGrid, ?AxisY2MajorTickMark=MajorTickMark, ?AxisY2MinorTickMark=MinorTickMark, ?AxisY2Name=Name, ?AxisY2Title=Title, ?AxisY2TitleAlignment=TitleAlignment, ?AxisY2TitleFont=titleFont, ?AxisY2TitleForeColor=TitleColor, ?AxisY2ToolTip=ToolTip) 

        member ch.AndTitle
            (?Text, ?Style, ?FontName, ?FontSize, ?FontStyle, ?Background, ?Color, ?BorderColor, ?BorderWidth, ?BorderDashStyle, 
             ?Orientation, ?Alignment, ?Docking, ?InsideArea) = 
            let font = StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) 
            ch |> Helpers.ApplyStyles(?Title=Text, ?TitleStyle=Style, ?TitleFont=font, ?TitleBackground=Background, ?TitleColor=Color, ?TitleBorderColor=BorderColor, ?TitleBorderWidth=BorderWidth, ?TitleBorderDashStyle=BorderDashStyle, ?TitleOrientation=Orientation, ?TitleAlignment=Alignment, ?TitleDocking=Docking, ?TitleInsideArea=InsideArea)

        member ch.And3D
            (?Inclination, ?IsClustered, ?IsRightAngleAxes, ?LightStyle, ?Perspective, ?PointDepth, ?PointGapDepth, ?Rotation, ?WallWidth) =

            ch |> Helpers.ApplyStyles(Enable3D=true, ?Area3DInclination=Inclination, ?Area3DIsClustered=IsClustered, ?Area3DIsRightAngleAxes=IsRightAngleAxes, ?Area3DLightStyle=LightStyle, ?Area3DPerspective=Perspective, ?Area3DPointDepth=PointDepth, ?Area3DPointGapDepth=PointGapDepth, ?Area3DRotation=Rotation, ?Area3DWallWidth=WallWidth)

        member ch.AndMarkers
            (?Color, ?Size, ?Step, ?Style, ?BorderColor, ?BorderWidth) =
          ch |> Helpers.ApplyStyles(?MarkerColor=Color, ?MarkerSize=Size, ?MarkerStep=Step, ?MarkerStyle=Style, ?MarkerBorderColor=BorderColor, ?MarkerBorderWidth=BorderWidth)

        member ch.AndLegend
          (?Enabled,?Title, ?Background, ?FontName,  ?FontSize, ?FontStyle, ?Alignment, ?Docking, ?InsideArea,
           ?TitleAlignment, ?TitleFont, ?TitleColor, ?BorderColor, ?BorderWidth, ?BorderDashStyle) = 
          let font = StyleHelper.OptionalFont(?Family=FontName, ?Size=FontSize, ?Style=FontStyle) 
          ch |> Helpers.ApplyStyles(?Legend=Enabled,?LegendTitle=Title, ?LegendBackground=Background, ?LegendFont=font, ?LegendAlignment=Alignment, ?LegendDocking=Docking, ?LegendInsideArea=InsideArea,
                                    ?LegendTitleAlignment=TitleAlignment, ?LegendTitleFont=TitleFont, ?LegendTitleForeColor=TitleColor, ?LegendBorderColor=BorderColor, ?LegendBorderWidth=BorderWidth, ?LegendBorderDashStyle=BorderDashStyle)

        member ch.AndDataPointLabels
          (?Label, ?LabelToolTip, ?PointToolTip) = 
          ch |> Helpers.ApplyStyles(?DataPointLabel=Label,?DataPointLabelToolTip=LabelToolTip, ?DataPointToolTip=PointToolTip)
           
        member ch.AndStyling
          (?Name,?Color, ?AreaBackground,?Margin,?Background,?BorderColor, ?BorderWidth (* , ?AlignWithChartArea, ?AlignmentOrientation, ?AlignmentStyle *) ) =
          ch |> Helpers.ApplyStyles(?Name=Name,?Color=Color, ?AreaBackground=AreaBackground,?Margin=Margin,?Background=Background,?BorderColor=BorderColor, ?BorderWidth=BorderWidth (* , ?AlignWithChartArea=AlignWithChartArea , ?AlignmentOrientation=AlignmentOrientation, ?AlignmentStyle=AlignmentStyle *) )


        member ch.ShowChart () =
            let frm = new Form(Visible = true, TopMost = true, Width = 700, Height = 500)
            let ctl = new ChartControl(ch, Dock = DockStyle.Fill)
            frm.Text <- ProvideTitle ch
            frm.Controls.Add(ctl)
            frm.Show()
            ctl.Focus() |> ignore

[<Obsolete("This type is now obsolete. Use 'Chart.*' instead. Do not open System.Windows.Forms.DataVisualization.Charting when using this library.")>]
type FSharpChart =

     member x.Obsolete() = ()

#if INTERACTIVE
module FsiAutoShow = 
    fsi.AddPrinter(fun (ch:GenericChart) -> ch.ShowChart(); "(Chart)")
#endif

