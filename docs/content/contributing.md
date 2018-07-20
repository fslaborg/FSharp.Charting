Contributing to FSharp.Charting
===========================

This page should provide you with some basic information if you're thinking about
contributing to the FSharp.Charting project. 

* This page can be edited by sending a pull request to FSharp.Charting on GitHub, please
  [edit here](https://github.com/fslaborg/FSharp.Charting/blob/master/docs/content/contributing.md).

* If you want to discuss a feature (a good idea!), or if you want to look at 
  suggestions how you might contribute, check out the
  [Issue list](https://github.com/fslaborg/FSharp.Charting/issues) on GitHub.

## Solution files

The root directory contains a number of Visual Studio solutions (`*.sln`) files 
that group the projects in the main logical groups:

* **FSharp.Charting.sln** contains the main projects that implement most of the FSharp.Charting
  functionality (such as runtime and design-time type provider libraries). If you want
  to contribute code that is not quite ready yet, but looks interesting, then please
  add it to the experimental projects.

* **FSharp.Charting.Tests.sln** is a library with tests for FSharp.Charting and it also contains
  the content of this web site (as `*.fsx` and `*.md`) files. Look here if you want
  to edit the documentation!

## Documentation

The documentation for the FSharp.Charting library is automatically generated using the
[F# Formatting](https://github.com/tpetricek/FSharp.Formatting) library. It turns
`*.md` (Markdown with embedded code snippets) and `*.fsx` files (F# script file with
embedded Markdown documentation) to a nice HTML documentation.

* The template for the pages is in the `tools` directory
  [on GitHub](https://github.com/fslaborg/FSharp.Charting/tree/master/docs/tools).

* The code for all the documents (including this one) can be found in the `examples` directory
  [on GitHub](https://github.com/fslaborg/FSharp.Charting/tree/master/docs/content). If you
  find a bug or add a new feature, make sure you document it!

* Images are not automatically generated - just use 'Save Image As' on a chart and add the PNG to the `images` folder.

* If you want to build the documentation, simply run the `build.fsx` script
  ([GitHub link](https://github.com/fslaborg/FSharp.Charting/blob/master/docs/tools/generate.fsx)) which
  builds the documentation.

* The final documentation will be pushed to the `gh-pages` branch by the maintainers of the `fsharp`
  repository for the library. 
