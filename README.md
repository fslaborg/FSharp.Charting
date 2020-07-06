# FSharp.Charting [![Travis build status](https://travis-ci.org/fslaborg/FSharp.Charting.png)](https://travis-ci.org/fslaborg/FSharp.Charting) [![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/enfmk5gpa6f12tu6/branch/master?svg=true)](https://ci.appveyor.com/project/tpetricek/fsharp-charting/branch/master)

The `FSharp.Charting` library implements charting suitable for use from F# scripting.

See https://fslab.org/FSharp.Charting/ and [other charting libraries for use with F#](https://fsharp.org/guides/data-science/#charting)

## Library license

The library is available under MIT. For more information see the [License file](https://github.com/fslaborg/FSharp.Charting/blob/master/LICENSE.md) in the GitHub repository.

## Maintainer(s)

- [@dsyme](https://github.com/dsyme)
- [@tpetricek](https://github.com/tpetricek)
- [@simra](https://github.com/simra)

The default maintainer account for projects under "fslaborg" is [@fsprojects](https://github.com/fsprojects) - F# Community Project Incubation Space (repo management)

## Releasing

Release packages using:

    set APIKEY=...
    ..\fsharp\.nuget\NuGet.exe push -source https://nuget.org bin\*.nupkg %APIKEY%
