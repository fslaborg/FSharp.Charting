// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r @"tools\FAKE\tools\FakeLib.dll"

open System
open System.IO
open Fake 
open Fake.AssemblyInfoFile
open Fake.Git

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let files includes = 
  { BaseDirectories = [__SOURCE_DIRECTORY__]
    Includes = includes
    Excludes = [] } |> Scan

// Information about the project to be used at NuGet and in AssemblyInfo files
let project = "FSharp.Charting"
let authors = ["Carl Nolan, Don Syme, Tomas Petricek"]
let summary = "A Charting Library for F#"
let description = """
  The F# Charting library (FSharp.Charting.dll) is a compositional library for creating charts
  from F#. It is designed to be a great fit for data scripting in F# Interactive, but 
  charts can also be embedded in Windows applications. The library is a wrapper for .NET Chart
  Controls, which are only supported on Windows."""
let tags = "F# FSharpChart charting plotting visualization"

// Information for the ASP.NET build of the project 
let projectAspNet = "FSharp.Charting.AspNet"
let summaryAspNet = summary + " (ASP.NET web forms)"
let tagsAspNet = tags + " ASPNET"
let descriptionAspNet = """
  The F# Charting library (FSharp.Charting.AspNet.dll) is an ASP.NET Web Forms build
  of F# Charting. It can be used for embedding F# Charting visualizations in ASP.NET
  web pages, by reusing the same F# chart specifications."""

// Read additional information from the release notes document
let releaseNotes, version = 
    let lastItem = File.ReadLines "RELEASE_NOTES.md" |> Seq.last
    let firstDash = lastItem.IndexOf('-')
    ( lastItem.Substring(firstDash + 1 ).Trim(), 
      lastItem.Substring(0, firstDash).Trim([|'*'|]).Trim() )

// --------------------------------------------------------------------------------------
// Generate assembly info files with the right version & up-to-date information

Target "AssemblyInfo" (fun _ ->
    [ ("src/AssemblyInfo.fs", "FSharp.Charting", project, summary)
      ( "src/AssemblyInfo.AspNet.fs", "FSharp.Charting.AspNet", projectAspNet, summaryAspNet ) ]
    |> Seq.iter (fun (fileName, title, project, summary) ->
        CreateFSharpAssemblyInfo fileName
           [ Attribute.Title title
             Attribute.Product project
             Attribute.Description summary
             Attribute.Version version
             Attribute.FileVersion version] )
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"]
)

// --------------------------------------------------------------------------------------
// Build library (builds Visual Studio solution, which builds multiple versions
// of the runtime library & desktop + Silverlight version of design time library)

Target "Build" (fun _ ->
    (files ["FSharp.Charting.sln"; "FSharp.Charting.Tests.sln"])
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)
// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target "RunTests" (fun _ ->

    // Will get NUnit.Runner NuGet package if not present
    // (needed to run tests using the 'NUnit' target)
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "./.nuget/NuGet.exe" }))

    let nunitVersion = GetPackageVersion "packages" "NUnit.Runners"
    let nunitPath = sprintf "packages/NUnit.Runners.%s/Tools" nunitVersion

    ActivateFinalTarget "CloseTestRunner"

    (files ["tests/bin/Release/FSharp.Charting.Tests.dll"])
    |> NUnit (fun p ->
        { p with
            ToolPath = nunitPath
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

FinalTarget "CloseTestRunner" (fun _ ->  
    ProcessHelper.killProcess "nunit-agent.exe"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->

    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let descriptionAspNet = descriptionAspNet.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = version
            ReleaseNotes = releaseNotes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        "nuget/FSharp.Charting.nuspec"

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = projectAspNet
            Summary = summaryAspNet
            Description = descriptionAspNet
            Version = version
            ReleaseNotes = releaseNotes
            Tags = tagsAspNet
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        "nuget/FSharp.Charting.AspNet.nuspec"

)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "UpdateDocs" (fun _ ->

    executeFSI "tools" "build.fsx" [] |> ignore

    DeleteDir "gh-pages"
    Repository.clone "" "https://github.com/fsharp/FSharp.Charting.git" "gh-pages"
    Branches.checkoutBranch "gh-pages" "gh-pages"
    CopyRecursive "docs" "gh-pages" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "gh-pages" (sprintf """commit -a -m "Update generated documentation for version %s""" version) |> printfn "%s"
    Branches.push "gh-pages"
)

Target "UpdateBinaries" (fun _ ->

    DeleteDir "release"
    Repository.clone "" "https://github.com/fsharp/FSharp.Charting.git" "release"
    Branches.checkoutBranch "release" "release"
    CopyRecursive "bin" "release/bin" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "release" (sprintf """commit -a -m "Update binaries for version %s""" version) |> printfn "%s"
    Branches.push "release"
)

Target "Release" DoNothing

"UpdateDocs" ==> "Release"
"UpdateBinaries" ==> "Release"

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build target=<Target>' to verride

Target "All" DoNothing

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "NuGet"
  ==> "All"


Run <| getBuildParamOrDefault "target" "All"