FSharp.Charting: The F# Charting Library
========================================

The FSharp.Charting library implements charting suitable for use from F# scripting.

<div>
 <img src="images/IC523409.png" width="30%" >
 <img src="images/IC523435.png" width="30%" >
 <img src="images/IC36812.png" width="30%" >
</div>

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      Install <a href="https://nuget.org/packages/FSharp.Charting">FSharp.Charting using NuGet</a>.
      Run this in the <a href="http://docs.nuget.org/docs/start-here/using-the-package-manager-console">Package Manager Console</a>:
      <pre>PM> Install-Package FSharp.Charting</pre>
      Then reference the 'FSharp.Charting.fsx' script in the package.
    </div>
  </div>
  <div class="span1"></div>
</div>

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      Install <a href="https://nuget.org/packages/FSharp.Charting.Gtk">FSharp.Charting.Gtk using NuGet on OSX</a>.
      Run this in the <a href="http://docs.nuget.org/docs/start-here/using-the-package-manager-console">Package Manager Console</a>:
      <pre>PM> Install-Package FSharp.Charting.Gtk</pre>
      Then reference the 'FSharp.Charting.Gtk.fsx' script in the package.
    </div>
  </div>
  <div class="span1"></div>
</div>

Alternatively, you can download the [source as a ZIP file][source] or as a [binary release as a ZIP file][release].

Features 
--------

* 2D charting.
* Many cross-platform chart types: Area, Bar, Bubble, Column, Line, Point and more.
* Create charts directly from F# data.
* Use either fluent or pipelined chart specifications.
* Create updating 'LiveChart' charts from F# or Rx observables.
* Can be used in conjunction with the [FSharp.Data](http://fsharp.github.io/FSharp.Data) library</a>.

Extra Features (Windows-only)
--------

* Many extra chart types (windows): BoxPlot, Candlestick, Doughnut, ErrorBar, FastLine, FastPoint, Funnel, Kagi and more.
* Pseudo-3D charting.

Documentation
-------------

The documentation is automatically generated from `*.fsx` files in  [the examples folder][examples]. 
If you find a typo, please submit a pull request!

 * [FSharp.Charting](fsharpcharting.html) is the documentation home.

Previous Versions
-----------------

This library is a successor to FSharpChart. The last version of FSharpChart was [version 0.61][fsharpchart61].

Contributing
------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding new public API, please also 
contribute [examples][examples] that can be turned into a documentation.

 * If you want to discuss an issue or feature that you want to add the to the library,
   then you can submit [an issue or feature request][issues] via Github or you can 
   send an email to the [F# open source][fsharp-oss] mailing list.

 * For more information about the library architecture, organization and more
   see the [contributing](contributing.html) page.

### Library philosophy

FSharp.Charting uses simple, declarative chart specifications.

On Windows, FSharp.Charting is implemented using the Data Visualization charting controls 
available on Windows in .NET 4.x.

On OSX, FSharp.Charting is implemented using the OxyPlot.GtkSharp charting library.

### Library future

FSharp.Charting is designed so that the same charting specifications can be supported when 
using different charting implementations. For example, the [Try F#](http://tryfsharp.org)
charts use similar charting specifications, implemented using in HTML5 using Dojo Charts.

The next step we would like to try is to implement cross-platform Javascript-based charting using
embedded browser controls.

Another direction of interest is to bind to charting controls usable by ASP.NET.


### Library license

The library is available under the MIT licence. For more information see the 
[License file][readme] in the GitHub repository. In summary, this means that you can 
use the library for commercial purposes, fork it, modify it as you wish.

  [source]: https://github.com/fsharp/FSharp.Charting/zipball/master
  [release]: https://github.com/fsharp/FSharp.Charting/zipball/release
  [examples]: https://github.com/fsharp/FSharp.Charting/tree/master/examples
  [gh]: https://github.com/fsharp/FSharp.Charting
  [issues]: https://github.com/fsharp/FSharp.Charting/issues
  [readme]: https://github.com/fsharp/FSharp.Charting/blob/master/README.md
  [fsharp-oss]: http://groups.google.com/group/fsharp-opensource
  [fsharpchart61]: http://code.msdn.microsoft.com/windowsdesktop/FSharpChart-b59073f5
